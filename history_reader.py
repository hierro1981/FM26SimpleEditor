"""Read-only experimental FM26 26.3.2 attribute-history reader.

Sample dates and the 21-byte record tail are intentionally left undecoded.
"""
import struct

ATTRIBUTE_ORDER = ('aerial_reach', 'command_of_area', 'communication', 'eccentricity', 'handling', 'kicking', 'reflexes', 'rushing_out', 'punching', 'throwing', 'one_on_ones', 'crossing', 'dribbling', 'finishing', 'heading', 'long_shots', 'long_throws', 'marking', 'passing', 'penalty_taking', 'free_kick_taking', 'tackling', 'technique', 'first_touch', 'corners', 'aggression', 'anticipation', 'bravery', 'vision', 'decisions', 'determination', 'flair', 'leadership', 'off_the_ball', 'positioning', 'teamwork', 'work_rate', 'composure', 'concentration', 'acceleration', 'agility', 'balance', 'pace', 'stamina', 'strength', 'jumping_reach', 'natural_fitness')
RECORD_BYTES = 69


def find_history(window):
    """Return a single structurally valid candidate, rejecting ambiguity."""
    if len(window) > 1024 * 1024:
        return [], "Record troppo ampio: storico non identificato in sicurezza."
    candidates = []
    for offset in range(123, max(123, len(window) - 142 + 1)):
        count = struct.unpack_from("<I", window, offset)[0]
        # A single record is insufficient to verify the repeated structure.
        if not 2 <= count <= 1200 or offset + 4 + count * RECORD_BYTES > len(window):
            continue
        starts = [offset + 4 + i * RECORD_BYTES for i in range(count)]
        if not all(window[start] == 1 and all(1 <= value <= 125 for value in window[start+1:start+48]) for start in starts):
            continue
        candidates.append(starts)
        if len(candidates) > 1:
            return [], "Più sequenze candidate: storico interno ambiguo."
    if not candidates:
        return [], "Nessuna sequenza interna riconosciuta (servono almeno 2 campioni)."
    points = [dict(sequence=i+1, date="", ca=None, pa=None,
                   attributes=dict(zip(ATTRIBUTE_ORDER, window[start+1:start+48])))
              for i, start in enumerate(candidates[0])]
    return points, (f"{len(points)} campioni interni · 47 attributi visibili · lettore sperimentale. "
                    "Asse X: ordine dei campioni; date individuali non decodificate. "
                    "La cadenza sembra mensile ma non viene assegnato un calendario. "
                    "CA/PA storica, piedi e attributi nascosti non disponibili.")


def embedded_history(store, player):
    if not str(store.save.info.build).startswith("26.3.2+") or store.save.info.section_schemas.get("game_db") != 4000:
        return [], "Storico interno non disponibile: formato non ancora validato per questa build."
    start, end = store.offsets(player)
    points, message = find_history(store.raw[start:end])
    for point in points:
        point.update(source=str(store.path), name=player.name or "")
    return points, message
