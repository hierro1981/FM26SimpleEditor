# Tattiche salvate / Saved tactics

Verifica per v14.3, 20 ottobre 2026.

## Italiano

È stata controllata sia la dipendenza fissata `fmsave 0.5.10`, sia la definizione upstream dei record tattici. Il lettore espone `Save.tactics()`, ma non rende ancora possibile un affidabile **Migliori per tattica** automatico.

| Dato | Stato della lettura |
| --- | --- |
| Squadra e indice del record | Disponibili; ogni sotto-squadra conserva copie proprie, non necessariamente identiche |
| Nome e testo dello stile | Disponibili come testo; significato dello stile non confermato dal lettore |
| Mentalità | Sette codici con nomi verificati |
| Undici slot | Disponibili, con maschere separate in possesso e non in possesso |
| Posizioni in campo | Bit letti, ma tutti etichettati `UNKNOWN`; nessuna corrispondenza verificata con le posizioni visualizzate nel gioco |
| Sottoruoli | Campi grezzi `in_possession_role_bits` e `out_of_possession_role_bits`; l'interpretazione come ruolo è ancora non confermata |
| Tattica attualmente selezionata | Non identificata |

Tre record non vanno assunti automaticamente come le tre tattiche attualmente utilizzate. Non sono utilizzabili come mappatura né l'ordine delle posizioni degli attributi del giocatore, né l'enum delle posizioni nelle partite, né l'ordine del catalogo dei 79 profili dell'editor: sono campi differenti.

Non è stato aggiunto un campo con posizioni o ruoli indovinati, né una classifica spacciata per quella della tattica salvata. La selezione manuale di posizioni e sottoruoli rimane disponibile in **Migliori per posizione**.

Per completare la funzione servono salvataggi e schermate corrispondenti delle tattiche in entrambe le fasi, poi coppie di salvataggi con una sola posizione o un solo sottoruolo cambiato alla volta. La mappatura ottenuta va verificata su più moduli e versioni del gioco, includendo le colonne sinistra/centro/destra e l'associazione degli slot tra le due fasi. La sola posizione naturale del giocatore schierato non dimostra la posizione effettiva della tattica. In questa verifica non erano disponibili salvataggi reali nella cartella di lavoro.

## English

Both pinned `fmsave 0.5.10` and the upstream tactic record definitions were inspected. `Save.tactics()` exposes per-team stored copies, textual names, mentality and eleven slots with separate in-possession/out-of-possession masks. Position bits remain `UNKNOWN`; the proposed role-bit fields are unconfirmed and have no verified role-name mapping. The selected tactic is not identified. Stored copies must not be presented as exactly the three active tactics without further validation.

Player attribute-position order, match position enums and the editor's tactical-role catalogue do not establish a tactic-code mapping. An automatic pitch and role-specific rankings are therefore deferred. Manual position/role recommendations remain available.

Completing this requires matching saves and screenshots for both phases, controlled single-position/single-role changes and validation across multiple formations and supported game versions. No real save files were available in the workspace for this check.

## Sources

- [fmsave tactic models and field verification status](https://github.com/rhiever/fmsave/blob/main/src/fmsave/models/tactics.py)
- [fmsave Save.tactics documentation and entry point](https://github.com/rhiever/fmsave/blob/main/src/fmsave/_save.py)
- Installed dependency: `fmsave==0.5.10`, `models/tactics.py`, `_save.py`, and `_layouts/fm26_26_3_2.py`.
