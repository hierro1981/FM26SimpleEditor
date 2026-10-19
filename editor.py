"""Local FM26 editor service. JSON lines are ASCII-safe on Windows."""
import datetime as dt
import calendar
import json
from pathlib import Path
import struct
import sys

import fmsave
from fmsave._container import read_index, read_section
from fmsave._layouts import PersonBlockLayout, find_layout
from fmsave.readers.persons import build_person_block_decoder, iter_marker_run_starts
from fmsave.readers.player_scan import window_end
from archive_rewrite import rewrite
from operation_progress import ProgressReporter, report, scaled
from history_reader import embedded_history


def integer(value, minimum, maximum, label):
    if type(value) is not int or not minimum <= value <= maximum:
        raise ValueError(f"{label}: inserisci un intero da {minimum} a {maximum}")
    return value


def contract_status(player, clock):
    contract = player.contract
    if contract is None:
        unregistered = player.club_uid is None and player.team_club_uid is None and player.team_id is None and player.on_loan is not True
        return "none" if unregistered else "unknown"
    if contract.end is None or clock is None:
        return "unknown"
    return "expired" if contract.end < clock else "active"


POSITION_ORDER = ("GK", "SW", "DL", "DC", "DR", "WBL", "DM", "WBR", "ML", "MC", "MR", "AML", "AMC", "AMR", "STC")


def primary_position(positions):
    return min(POSITION_ORDER, key=lambda key: (-positions.get(key, 0), POSITION_ORDER.index(key))) if max(positions.values(), default=0) > 1 else ""


def player_row(player, clock):
    contract = player.contract
    team = player.club_name or player.team_club_name or ""
    owner = (player.loan_parent_club_name or "") if player.on_loan is True else team
    positions = {key: getattr(player.positions, key.lower()) for key in POSITION_ORDER}
    main = primary_position(positions)
    return dict(uid=player.uid, id=player.unique_id, name=player.name or f"Giocatore {player.uid}",
                team=team, ownerTeam=owner, birth=player.birth_date.isoformat() if player.birth_date else None,
                ca=player.ability.current, pa=player.ability.potential,
                paRange=player.ability.potential_range_code, teamId=player.team_id, clubUid=player.club_uid,
                age=player.age, height=player.height_cm if player.height_cm > 0 else None,
                leftFoot=player.left_foot, rightFoot=player.right_foot, onLoan=player.on_loan,
                positions=positions, mainPosition=main,
                contractEnd=contract.end.isoformat() if contract is not None and contract.end is not None else None,
                contractStatus=contract_status(player, clock))


def add_months(date, months):
    year, month = divmod(date.year * 12 + date.month - 1 + months, 12)
    month += 1
    return dt.date(year, month, min(date.day, calendar.monthrange(year, month)[1]))


def search_rows(rows, request, clock):
    text = str(request.get("text") or "").strip().casefold()
    exact_team = str(request.get("teamExact") or "").strip()
    contains_team = str(request.get("teamContains") or "").strip().casefold()
    loan_team = request.get("loanTeam") or "playing"
    if loan_team not in {"playing", "owner"}:
        raise ValueError("Squadra di riferimento dei prestiti non riconosciuta")

    def team_name(row):
        # Only confirmed loans change teams; an unknown owner stays unknown.
        return row.get("ownerTeam", "") if loan_team == "owner" and row["onLoan"] is True else row["team"]
    ranges = []
    for field, minimum, maximum in (("age", 0, 120), ("ca", 0, 255), ("pa", 0, 255), ("height", 100, 250)):
        low, high = request.get(field + "Min"), request.get(field + "Max")
        if low is None and high is None:
            continue
        low = minimum if low is None else integer(low, minimum, maximum, field)
        high = maximum if high is None else integer(high, minimum, maximum, field)
        if low > high:
            raise ValueError(f"Intervallo invertito: {field}")
        ranges.append((field, low, high))
    position = request.get("position") or ""
    if position and position not in {"GK", "SW", "DL", "DC", "DR", "DM", "ML", "MC", "MR", "AML", "AMC", "AMR", "STC", "WBL", "WBR"}:
        raise ValueError("Posizione non riconosciuta")
    minimum_rating = integer(request.get("positionMin", 15), 1, 20, "Familiarita'")
    growth = request.get("growthMin")
    if growth is not None:
        integer(growth, 0, 255, "Margine di crescita")
    contract = request.get("contract") or "all"
    foot = request.get("foot") or "all"
    loan = request.get("loan") or "all"
    if contract not in {"all", "none", "expired", "6m", "12m", "active", "unknown"}:
        raise ValueError("Stato del contratto non riconosciuto")
    if foot not in {"all", "left", "right", "both"} or loan not in {"all", "yes", "no", "unknown"}:
        raise ValueError("Filtro piede o prestito non riconosciuto")
    if contract in {"6m", "12m"} and clock is None:
        raise ValueError("Data della partita non disponibile per calcolare la scadenza")
    deadline = add_months(clock, 6 if contract == "6m" else 12) if clock is not None and contract in {"6m", "12m"} else None

    def matches(row):
        team = team_name(row)
        if exact_team and team != exact_team:
            return False
        if not exact_team and contains_team and contains_team not in team.casefold():
            return False
        if text and not any(text in str(value).casefold() for value in (row["name"], team, row["uid"], row["id"] if row["id"] is not None else "")):
            return False
        if any(row[field] is None or not low <= row[field] <= high for field, low, high in ranges):
            return False
        if position and row["positions"].get(position, 0) < minimum_rating:
            return False
        if growth is not None and (row["pa"] is None or row["pa"] - row["ca"] < growth):
            return False
        if contract in {"none", "expired", "active", "unknown"} and row["contractStatus"] != contract:
            return False
        if deadline is not None and (row["contractStatus"] != "active" or row["contractEnd"] is None or not clock.isoformat() <= row["contractEnd"] <= deadline.isoformat()):
            return False
        if foot == "left" and not row["leftFoot"] > row["rightFoot"]:
            return False
        if foot == "right" and not row["rightFoot"] > row["leftFoot"]:
            return False
        if foot == "both" and not (row["leftFoot"] >= 15 and row["rightFoot"] >= 15):
            return False
        if loan == "yes" and row["onLoan"] is not True or loan == "no" and row["onLoan"] is not False or loan == "unknown" and row["onLoan"] is not None:
            return False
        return True

    sort = request.get("sort") or "name"
    if sort not in {"name", "team", "age", "ca", "pa", "id", "uid", "contractEnd", "contractStatus", "positions", "mainPosition"}:
        raise ValueError("Colonna di ordinamento non riconosciuta")
    descending = request.get("descending", False)
    if type(descending) is not bool:
        raise ValueError("Direzione di ordinamento non valida")
    found = sorted((row for row in rows if matches(row)), key=lambda row: row["uid"])
    def sort_value(row):
        if sort == "team":
            return team_name(row).casefold() or None
        if sort == "mainPosition":
            key = row.get("mainPosition", primary_position(row["positions"]))
            return POSITION_ORDER.index(key) if key else None
        if sort == "positions":
            return ", ".join(key for key, value in sorted(row["positions"].items(), key=lambda pair: (-pair[1], pair[0])) if value >= 12)
        if sort == "contractStatus":
            return {"active": "In corso", "unknown": "Non disponibile", "expired": "Scaduto", "none": "Senza rilevato"}[row[sort]]
        value = row[sort]
        return value.casefold() if isinstance(value, str) else value
    known = [row for row in found if sort_value(row) is not None]
    missing = [row for row in found if sort_value(row) is None]
    found = sorted(known, key=sort_value, reverse=descending) + missing
    return dict(total=len(found), uids=[row["uid"] for row in found[:2500]])


def snapshot(save, player, source, data=None):
    records = save._context.player_records()
    raw = read_section(save._container_index, "game_db") if data is None else data
    start = records.record_offsets[records.position_by_uid[player.uid]]
    layout = records.layout
    names = iter(layout.attribute_field_order)
    values = {}
    for index in range(layout.attribute_count):
        key = "left_foot" if index == layout.left_foot_index else "right_foot" if index == layout.right_foot_index else next(names)
        values[key] = raw[start+layout.attributes_offset+index]
    if save.info.game_date is None:
        raise ValueError("Data della partita non leggibile: punto storico escluso")
    return dict(date=save.info.game_date.isoformat(), source=str(Path(source).resolve()), name=player.name or "",
                ca=player.ability.current, pa=player.ability.potential, attributes=values)


def history(store, request):
    player = store.player(request.get("uid"))
    points = [snapshot(store.save, player, store.path, store.raw)]
    embedded, embedded_message = embedded_history(store, player)
    warnings = []
    seen = {store.path}
    sources = request.get("sources", [])
    if not isinstance(sources, list) or len(sources) > 20 or any(not isinstance(source, str) for source in sources):
        raise ValueError("Seleziona al massimo 20 salvataggi per volta")
    for source in sources:
        path = Path(source).resolve()
        if path in seen:
            continue
        seen.add(path)
        try:
            with fmsave.open(path) as other:
                candidates = [candidate for candidate in other.players()
                              if (candidate.unique_id == player.unique_id if player.unique_id is not None else candidate.uid == player.uid)
                              and (candidate.name or "").casefold() == (player.name or "").casefold()]
                birth_matches = [candidate for candidate in candidates if candidate.birth_date == player.birth_date]
                note = ""
                if len(birth_matches) == 1:
                    chosen = birth_matches[0]
                elif player.unique_id is not None and len(candidates) == 1 and candidates[0].uid == player.uid:
                    chosen = candidates[0]
                    note = "Nascita diversa: corrispondenza basata su ID pubblico, ID interno e nome."
                else:
                    raise ValueError("Identita' non verificata univocamente (ID, nome e nascita)")
                point = snapshot(other, chosen, path)
                point["identityNote"] = note
                points.append(point)
        except Exception as exc:
            warnings.append(f"{path.name}: {exc}")
    points.sort(key=lambda point: (point["date"], point["source"]))
    return dict(points=points, warnings=warnings, embeddedAvailable=bool(embedded),
                embeddedPoints=embedded, embeddedMessage=embedded_message,
                message="Valori reali dei salvataggi selezionati alle rispettive date della partita.")


class PlayerStore:
    def __init__(self, source, progress=None):
        report(progress, 0, "Lettura intestazione e indice del salvataggio")
        self.path = Path(source).resolve()
        self.save = fmsave.open(self.path)
        try:
            report(progress, 5, "Decompressione del database")
            # One section loan keeps the same bytes available to all validated readers.
            with self.save._context.section("game_db") as database:
                self.raw = database
                report(progress, 16, "Ricerca dei record giocatore")
                self.records = self.save._context.player_records()
                report(progress, 35, "Decodifica giocatori, identità e contratti")
                self.players = list(self.save.players())
            report(progress, 70, "Giocatori decodificati", len(self.players), len(self.players), "giocatori")
            self.by_uid = {player.uid: player for player in self.players}
            if len(self.by_uid) != len(self.players):
                raise ValueError("ID interni duplicati nel salvataggio")
            self.index = self.save._container_index
            # Database and player index were retained from the shared section loan.
            self.layout = self.records.layout
            self.birth_cache = {}
            self.league_cache = {}
            self.league_tables_cache = None
            self.rows = []
            total = len(self.players)
            for done, player in enumerate(self.players, 1):
                self.rows.append(player_row(player, self.save.info.game_date))
                if done % 1000 == 0 or done == total:
                    report(progress, 72 + 27*done/max(1,total), "Preparazione catalogo giocatori", done, total, "giocatori")
            report(progress, 100, "Catalogo verificato", total, total, "giocatori")
        except Exception:
            self.save.close()
            raise

    def close(self):
        self.save.close()

    def summary(self):
        return dict(source=str(self.path), gameDate=self.save.info.game_date.isoformat() if self.save.info.game_date else None,
                    players=self.rows)

    def player(self, uid):
        uid = integer(uid, 0, 4294967295, "ID interno")
        player = self.by_uid.get(uid)
        if player is None:
            raise ValueError("Giocatore non trovato nel salvataggio aperto")
        return player

    def offsets(self, player):
        position = self.records.position_by_uid[player.uid]
        return self.records.record_offsets[position], window_end(self.records, position, len(self.raw))

    def birth_offset(self, player):
        if player.uid in self.birth_cache:
            return self.birth_cache[player.uid]
        if player.birth_date is None or self.save.info.game_date is None:
            result = (None, "Data di nascita non disponibile")
        else:
            start, end = self.offsets(player)
            schema = self.save.info.section_schemas.get("game_db")
            layout = find_layout(PersonBlockLayout, "game_db", schema, self.save.info.build).layout
            context = self.save._context
            decoder = build_person_block_decoder(layout, context.name_pools(), context.club_index(),
                                                  self.save.info.game_date, self.save.info.file_name)
            first = start + layout.window_start_offset + decoder.zero_offset_from_birth
            candidates = []
            for zero_at in iter_marker_run_starts(self.raw, first, end, decoder.marker_table, decoder.marker_needle):
                at = zero_at - decoder.zero_offset_from_birth
                person = decoder._try_candidate(self.raw, at, end)
                if person is not None and person[0] == player.name and person[6] == player.birth_date:
                    candidates.append(at)
            result = (candidates[0], "") if len(candidates) == 1 else (None, "Data non modificabile: record persona non univoco")
        self.birth_cache[player.uid] = result
        return result

    def league_reference(self, player, progress=None):
        report(progress, 0, "Lettura del campionato di riferimento")
        team = player.team_id
        if team in self.league_cache:
            report(progress, 100, "Campionato recuperato dalla cache")
            return self.league_cache[team]
        result = dict(mean=None, count=0, label="Campionato non disponibile: nessuna divisione univoca leggibile")
        try:
            if team is not None:
                if self.league_tables_cache is None:
                    report(progress, 5, "Lettura delle classifiche del salvataggio")
                    self.league_tables_cache = list(self.save.league_tables())
                report(progress, 65, "Identificazione della divisione della squadra")
                # Conservative division-shape rule documented by fmsave; never infer from nation.
                candidates = [table for table in self.league_tables_cache
                              if 18 <= table.club_count <= 26 and table.rows
                              and all(row.rounds_per_venue == table.club_count - 1 for row in table.rows)
                              and any(row.team_id == team for row in table.rows)]
                groups = {tuple(sorted(row.team_id for row in table.rows)): table for table in candidates}
                if len(groups) == 1:
                    teams, table = next(iter(groups.items()))
                    report(progress, 80, "Calcolo del riferimento di abilità del campionato")
                    cohort = [row["ca"] for row in self.rows if row["teamId"] in teams and row["ca"] > 0]
                    if len(cohort) >= 3:
                        label = table.competition_name or (f"Competizione ID {table.competition_id}" if table.competition_id is not None else "Divisione senza nome")
                        result = dict(mean=sum(cohort)/len(cohort), count=len(cohort), label=f"{label} · {len(teams)} squadre · divisione dedotta dalla classifica")
        except Exception as exc:
            result["label"] = "Campionato non disponibile: " + str(exc)
        self.league_cache[team] = result
        report(progress, 100, "Riferimento del campionato completato")
        return result

    def inspect(self, uid, progress=None):
        report(progress, 0, "Identificazione del giocatore")
        player = self.player(uid)
        start, _ = self.offsets(player)
        layout = self.layout
        names = iter(layout.attribute_field_order)
        attributes = []
        report(progress, 10, "Lettura degli attributi del giocatore", 0, layout.attribute_count, "attributi")
        for index in range(layout.attribute_count):
            key = "left_foot" if index == layout.left_foot_index else "right_foot" if index == layout.right_foot_index else next(names)
            attributes.append(dict(index=index, key=key, value=self.raw[start+layout.attributes_offset+index]))
            report(progress, 10+20*(index+1)/layout.attribute_count, "Lettura degli attributi del giocatore", index+1, layout.attribute_count, "attributi")
        report(progress, 30, "Lettura delle posizioni del giocatore")
        positions = [dict(index=index, key=key, value=self.raw[start+layout.ratings_offset+index])
                     for index, key in enumerate(layout.position_codes)]
        report(progress, 40, "Verifica dei dati anagrafici e della data di nascita")
        birth_at, warning = self.birth_offset(player)
        report(progress, 60, "Lettura di abilità attuale e potenziale")
        raw_pa = struct.unpack_from("<h", self.raw, start+layout.potential_ability_offset)[0]
        league = self.league_reference(player, scaled(progress, 65, 95))
        return dict(player=player_row(player, self.save.info.game_date), attributes=attributes, positions=positions,
                    birthEditable=birth_at is not None, birthWarning=warning, rawPa=raw_pa, league=league)

    def save_player(self, request, progress=None):
        report(progress, 0, "Controllo dei dati modificati")
        player = self.player(request.get("uid"))
        expected_id = request.get("id")
        if expected_id is not None:
            integer(expected_id, 0, 4294967295, "ID pubblico")
        if player.unique_id != expected_id:
            raise ValueError("L'ID pubblico non corrisponde al giocatore selezionato")
        maximum_ability = self.layout.edited_ability_maximum or 200
        current = integer(request.get("ca"), 0, maximum_ability, "CA")
        potential = integer(request.get("pa"), -10, maximum_ability, "PA")
        attributes = request.get("attributes")
        positions = request.get("positions")
        for values, count, upper, label in ((attributes, self.layout.attribute_count, 125, "Attributi"),
                                             (positions, self.layout.ratings_count, 20, "Posizioni")):
            if not isinstance(values, list) or len(values) != count:
                raise ValueError(f"Numero di valori non valido: {label}")
            for value in values:
                integer(value, 1, upper, label)
        target = Path(request["output"]).resolve()
        if target == self.path:
            raise ValueError("Scegli un nuovo nome per la copia modificata")
        fingerprint = self.index.fingerprint
        status = self.path.stat()
        if status.st_size != fingerprint.file_size or status.st_mtime_ns != fingerprint.mtime_ns:
            raise ValueError("Il salvataggio e' cambiato sul disco: aprilo nuovamente")
        report(progress, 2, "Applicazione delle modifiche alla copia del database")
        data = bytearray(self.raw)
        start, _ = self.offsets(player)
        new_birth = player.birth_date
        if request.get("changeBirth", False):
            birth_at, warning = self.birth_offset(player)
            if birth_at is None:
                raise ValueError(warning)
            new_birth = dt.date.fromisoformat(request["birth"])
            if not 1850 <= new_birth.year or new_birth > self.save.info.game_date:
                raise ValueError("La nascita deve essere compresa fra il 1850 e la data della partita")
            day, year = struct.unpack_from("<HH", data, birth_at)
            if year != player.birth_date.year or (day & 511) != player.birth_date.timetuple().tm_yday:
                raise ValueError("La data originale nel record non corrisponde")
            struct.pack_into("<HH", data, birth_at, (day & ~511) | new_birth.timetuple().tm_yday, new_birth.year)
        struct.pack_into("<H", data, start+self.layout.current_ability_offset, current)
        struct.pack_into("<h", data, start+self.layout.potential_ability_offset, potential)
        data[start+self.layout.attributes_offset:start+self.layout.attributes_offset+self.layout.attribute_count] = bytes(attributes)
        data[start+self.layout.ratings_offset:start+self.layout.ratings_offset+self.layout.ratings_count] = bytes(positions)
        rewrite(self.path, target, data, self.index, scaled(progress, 3, 68))
        report(progress, 68, "Riapertura e controllo della copia salvata")
        verified = PlayerStore(target, scaled(progress, 68, 93))
        try:
            confirmed = verified.player(player.uid)
            report(progress, 94, "Verifica del giocatore e dei campi salvati")
            detail = verified.inspect(player.uid)
            if (confirmed.unique_id != player.unique_id or confirmed.birth_date != new_birth or
                confirmed.ability.current != current or detail["rawPa"] != potential or
                [field["value"] for field in detail["attributes"]] != attributes or
                [field["value"] for field in detail["positions"]] != positions):
                raise ValueError("Verifica del giocatore salvato non riuscita")
        except Exception:
            verified.close()
            raise
        report(progress, 99, "Tutti i controlli della copia sono completati")
        return verified, dict(source=str(target), detail=detail)


def serve():
    store = None
    try:
        for line in sys.stdin:
            try:
                request = json.loads(line)
                command = request.get("command")
                progress = ProgressReporter() if command in {"open", "save", "inspect"} else None
                if command == "open":
                    new_store = PlayerStore(request["source"], scaled(progress, 0, 95))
                    if store is not None:
                        store.close()
                    store = new_store
                    result = store.summary()
                elif command == "close":
                    break
                elif store is None:
                    raise ValueError("Apri prima un salvataggio")
                elif command == "inspect":
                    result = store.inspect(request.get("uid"), scaled(progress, 0, 95))
                elif command == "search":
                    result = search_rows(store.rows, request, store.save.info.game_date)
                elif command == "history":
                    result = history(store, request)
                elif command == "save":
                    new_store, result = store.save_player(request, progress)
                    store.close()
                    store = new_store
                else:
                    raise ValueError("Comando non riconosciuto")
                report(progress, 100, "Caricamento completato" if command == "open" else "Dati giocatore pronti" if command == "inspect" else "Salvataggio verificato")
                response = dict(ok=True, data=result)
            except Exception as exc:
                response = dict(ok=False, error=f"{type(exc).__name__}: {exc}")
            print(json.dumps(response, ensure_ascii=True, separators=(",", ":")), flush=True)
    finally:
        if store is not None:
            store.close()


if __name__ == "__main__":
    serve()


