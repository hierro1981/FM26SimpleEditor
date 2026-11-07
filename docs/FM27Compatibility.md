# FM27 compatibility / Compatibilità FM27

## Italiano

Supporto sperimentale aggiunto nella v16.0.0-preview.1, con `fmsave==0.10.0`. FM26 rimane supportato. La build FM27 confrontata è `27.0.0+2493426`, mentre upstream registra `27.0.0+2486695` e tratta quella più recente come non verificata. Non viene eliminata la verifica upstream degli schemi né registrata artificiosamente una build come ufficialmente supportata.

Su una carriera prima e dopo la conversione, con identica data di gioco:

- 75.687 giocatori in entrambi i file, stessi UID, CA e PA identiche per tutti.
- Stessi ID squadra e 93 giocatori nella rosa del club allenato; U20/U18 identificati in entrambi.
- Le tre tattiche decodificate coincidono integralmente usando il layout FM27 corretto, compresi posizioni, ruoli e compiti.
- 1.115 campioni degli attributi su 73 giocatori corrispondono tra i file. Il record storico conserva 69 byte e 47 attributi: il marker passa da 1 a 2. Date individuali e CA/PA storiche restano non decodificate.
- Due prove di scrittura su copie, una per FM26 e una per FM27: CA +1 e un attributo grezzo +1. Riapertura e verifica dei dati riuscite; cambiano soltanto due byte nel database. Le altre 81 sezioni decompresse sono identiche alle originali.

### Limiti verificati

La conversione non produce un database semanticamente identico in ogni campo: 1.359 giocatori hanno familiarità nelle posizioni diverse, 13 hanno differenze negli attributi e quattro cambiano nome/nascita. Non vengono corretti automaticamente né usati come prova che il lettore sia necessariamente errato.

Lo staff del club letto passa da 84 a 75 persone: 72 sono comuni e i loro attributi professionali coincidono; 12 delle precedenti persone sono lette sotto altri club e tre persone compaiono soltanto nel file FM27. Le quattro schermate Staff FM27 confermano tutti i 75 nomi letti e non includono le 12 persone precedenti. Nella preview.2 medico e tre dirigenti conservano il riferimento esplicito alla prima squadra anche senza termini contrattuali; vengono riconosciuti i titoli di presidente, dirigenti, medico sociale e capo analista. Le assegnazioni coincidono per 74 persone su 75. Per Gennaro Zumpano il file fornito contiene un solo contratto U18 fino al 30/06/2029, mentre la schermata indica U20 fino al 30/06/2030: occorre un salvataggio corrispondente alla schermata per risolvere la discrepanza, senza forzare valori per nome o ID. Le assegnazioni non vengono ricostruite copiando quelle del save FM26.

La scrittura è abilitata solo per la build FM27 confrontata e schema `game_db=4000`. Altre build che upstream riesce a leggere restano in sola lettura; storico e tattiche specifici non vengono estesi a build sconosciute. I controlli su copie sono strutturali: resta da verificare la ricarica nel gioco. Non è richiesta né effettuata una pubblicazione della release stabile.

## English

Experimental support in v16.0.0-preview.1 uses pinned fmsave 0.10.0 and retains FM26. Paired files share a career/date; tested FM27 build is 27.0.0+2493426, which upstream treats as unverified. Upstream schema checks remain active.

Both files have 75,687 players with matching UIDs and identical CA/PA, matching team IDs and a 93-player managed roster. All three decoded tactics match. Across 73 players, 1,115 internal attribute samples match; their 69-byte records change the leading marker from 1 to 2. Individual dates and historical CA/PA are still undecoded.

One controlled edit per edition changes only CA +1 and one raw attribute +1. Both copies reopen correctly: two database bytes change and all other 81 decompressed sections remain identical. This is structural verification, not proof of successful loading in the game.

Migration also yields differences: 1,359 player position profiles, 13 attribute profiles and four names/birth dates. Club staff read changes from 84 to 75: 72 are shared, with identical professional attributes; 12 old members are read at other clubs and three appear only in FM27. Four FM27 staff screenshots confirm all 75 names and exclude the 12 previous members. Preview.2 preserves explicit first-team references for the doctor and three board members without inventing paid contract terms, and identifies the verified job titles. Squad assignments match for 74 of 75 people. Gennaro Zumpano has a single U18 contract ending 30 June 2029 in the supplied file, whereas the screenshot shows U20 and 30 June 2030. A matching save is required to resolve that discrepancy; no name/ID-specific override is applied. Staff views show an experimental notice; no assignments are copied from the old file as a guess.

Writes are gated to the paired FM27 build and game_db schema 4000. Other readable FM27 builds are read-only; tactics and internal history fail closed for untested builds. Stable release/main are unchanged.

Sources: [fmsave project and coverage](https://pypi.org/project/fmsave/). Private saves and extracted records are not included in this repository.
