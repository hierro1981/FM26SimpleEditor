def rewrite(source, output, raw, original_index, progress=None):
    """Rebuild the archive and every manifest entry, including nested directories."""
    from operation_progress import report
    import os
    from pathlib import Path
    import struct
    import tempfile
    import zstandard as zstd
    from fmsave._container import walk_frame_headers

    def manifest_records(payload):
        records = []
        for at in range(len(payload) - 60):
            length = struct.unpack_from("<I", payload, at)[0]
            if not 1 <= length <= 256:
                continue
            name = payload[at+4:at+4+length]
            if not name or not all(32 <= value < 127 for value in name):
                continue
            ext_at = at + 4 + length
            extension_length = struct.unpack_from("<I", payload, ext_at)[0]
            if not 2 <= extension_length <= 10:
                continue
            extension = payload[ext_at+4:ext_at+4+extension_length]
            if not extension.startswith(b".") or not all(value in b".abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_" for value in extension):
                continue
            tail = ext_at + 4 + extension_length
            if tail + 40 > len(payload):
                continue
            relative, compressed, uncompressed, _, _ = struct.unpack_from("<5Q", payload, tail)
            if not 1 <= compressed < 2**32 or not 1 <= uncompressed <= 1024**3:
                continue
            records.append((tail, (name+extension).decode("ascii"), relative+26, compressed, uncompressed))
        return records

    def validate(path, blob, trailer_at, payload, check_payloads, low, high, phase):
        records = manifest_records(payload)
        with open(path, "rb") as stream:
            frames = walk_frame_headers(stream, 26, trailer_at, Path(path).name)
        by_offset = {frame.offset: frame for frame in frames}
        if len(records) != len(frames) or len({row[2] for row in records}) != len(records):
            raise ValueError("Indice incompleto o ambiguo: il numero delle voci non coincide con tutte le sezioni del file")
        completed_bytes = 0
        total_bytes = sum(row[4] for row in records)
        for done, (_, name, offset, compressed, uncompressed) in enumerate(records, 1):
            frame = by_offset.get(offset)
            if frame is None or frame.skippable or frame.size != compressed:
                raise ValueError(f"Indice non valido per {name}. Usa un salvataggio originale funzionante.")
            if check_payloads:
                data = zstd.ZstdDecompressor().decompress(blob[offset:offset+compressed], max_output_size=uncompressed+1)
                if len(data) != uncompressed:
                    raise ValueError(f"Dimensione errata per {name}")
            completed_bytes += uncompressed
            report(progress, low + (high-low)*completed_bytes/max(1,total_bytes), phase, done, len(records), "sezioni")
        return records

    report(progress, 0, "Lettura archivio di origine")
    source_path = Path(source)
    target = Path(output)
    if target.resolve() == source_path.resolve():
        raise ValueError("Scegli un nome diverso dal salvataggio originale")
    blob = source_path.read_bytes()
    trailer_start = 9 + struct.unpack_from("<Q", blob, 9)[0]
    if trailer_start != original_index.trailer_offset:
        raise ValueError("Il file di origine e' cambiato: riaprilo prima della modifica")
    trailer_raw = bytearray(zstd.ZstdDecompressor().decompress(blob[trailer_start+9:], max_output_size=64*1024*1024))
    records = validate(source_path, blob, trailer_start, trailer_raw, True, 2, 28, "Verifica delle sezioni originali")
    databases = [row for row in records if row[1] == "game_db.dat"]
    if len(databases) != 1:
        raise ValueError("Il database non compare una sola volta nell'indice")
    _, _, db_start, old_size, db_length = databases[0]
    declared_db = original_index.sections["game_db"]
    if db_start != declared_db.frame_offset or old_size != declared_db.compressed_size:
        raise ValueError("La sezione database non corrisponde all'indice letto")
    if len(raw) != db_length:
        raise ValueError("La modifica ha cambiato la lunghezza del database")
    report(progress, 28, "Compressione del database", 0, len(raw), "byte")
    compressor = zstd.ZstdCompressor(level=3, write_content_size=False).compressobj()
    chunks = []
    block_size = 4*1024*1024
    for at in range(0, len(raw), block_size):
        end = min(len(raw), at+block_size)
        chunks.append(compressor.compress(memoryview(raw)[at:end]))
        report(progress, 28 + 28*end/max(1,len(raw)), "Compressione del database", end, len(raw), "byte")
    chunks.append(compressor.flush())
    new_frame = b"".join(chunks)
    report(progress, 57, "Verifica del database compresso")
    if zstd.ZstdDecompressor().decompress(new_frame, max_output_size=len(raw)) != raw:
        raise ValueError("Verifica della compressione del database fallita")
    delta = len(new_frame) - old_size
    db_end = db_start + old_size
    report(progress, 59, "Ricostruzione di tutte le voci dell’indice")
    for tail, name, offset, compressed, uncompressed in records:
        if name == "game_db.dat":
            struct.pack_into("<QQ", trailer_raw, tail+8, len(new_frame), len(raw))
        elif offset >= db_end:
            struct.pack_into("<Q", trailer_raw, tail, offset - 26 + delta)
    header = bytearray(blob[:26])
    struct.pack_into("<Q", header, 9, trailer_start + delta - 9)
    trailer_header = blob[trailer_start:trailer_start+9]
    new_trailer = trailer_header + zstd.ZstdCompressor(level=3, write_content_size=False).compress(trailer_raw)
    fd, temp_name = tempfile.mkstemp(prefix="fm26_verified_", suffix=".fm", dir=target.parent)
    try:
        with os.fdopen(fd, "wb") as stream:
            parts = [header, memoryview(blob)[26:db_start], new_frame, memoryview(blob)[db_end:trailer_start], new_trailer]
            total_size = sum(len(part) for part in parts)
            written = 0
            for part in parts:
                for at in range(0, len(part), block_size):
                    chunk = part[at:at+block_size]
                    stream.write(chunk)
                    written += len(chunk)
                    report(progress, 60 + 10*written/max(1,total_size), "Scrittura della copia", written, total_size, "byte")
        result_blob = Path(temp_name).read_bytes()
        new_trailer_start = trailer_start + delta
        checked_trailer = zstd.ZstdDecompressor().decompress(result_blob[new_trailer_start+9:], max_output_size=64*1024*1024)
        result_records = validate(temp_name, result_blob, new_trailer_start, checked_trailer, True, 71, 98, "Verifica delle sezioni della copia")
        if len(result_records) != len(records) or checked_trailer != trailer_raw:
            raise ValueError("Verifica completa dell'indice salvato fallita")
        report(progress, 99, "Conferma della copia verificata")
        os.replace(temp_name, target)
        report(progress, 100, "Archivio ricostruito e verificato")
    finally:
        if os.path.exists(temp_name):
            os.unlink(temp_name)
