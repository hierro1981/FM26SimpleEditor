# Tattiche salvate / Saved tactics · v14.3

## Uso nell’editor

In **La mia squadra → Migliori per tattica**, scegli una delle tattiche lette dalla prima squadra del club gestito. I campi mostrano separatamente **in possesso** e **non in possesso**, con posizione, lato e sottoruolo. Clicca uno slot oppure selezionalo nell’elenco: le due disposizioni evidenziano lo stesso slot.

Il selettore delle fasi permette di valutare solo IP, solo OOP oppure entrambe. La classifica mostra fino a cinque giocatori della rosa scelta dal filtro comune della squadra, con punteggio /20, stelle stimate, familiarità, CA e PA. Con entrambe le fasi il punteggio è la media dei due profili; servono familiarità almeno 12/20 in entrambe le posizioni e tutti gli attributi richiesti. Un ruolo sconosciuto o incompatibile non viene sostituito con un profilo generico. L’apertura del giocatore, l’elenco di confronto e il confronto rapido di due selezioni sono disponibili nella classifica.

Le tattiche sono **in sola lettura**: non si cambia formazione, non si assegnano giocatori e non si modifica il salvataggio. La tattica attualmente attiva non viene dedotta; il menu presenta i record disponibili. La lettura è su richiesta e può essere aggiornata con il pulsante di ricaricamento. Cambiare salvataggio o club invalida i risultati precedenti. Come per il resto di La mia squadra, viene usato il primo allenatore umano riconosciuto.

## Decodifica e compatibilità

Il lettore è limitato a **FM26 `26.3.2+2329565`, sezione `tactics_man` schema 26**. Verifica club gestito, ID della prima squadra, proprietario del blocco, conteggio dei record, undici coppie di slot complete e permutazione valida tra le fasi. Le copie delle giovanili non diventano altre tattiche della prima squadra. Un errore opzionale viene mostrato nel sotto-tab e non impedisce la modifica dei giocatori.

La parola `role_bits` iniziale letta da fmsave non rappresenta sempre il sottoruolo effettivo. Il lettore ricompone invece il valore a 64 bit come:

```text
roleAndDuty = terminal.second_bits_high | (trail_bits << 32)
roleCode = roleAndDuty & ~dutyMask
```

Gli enum IP/OOP gestiti del gioco, incrociati con le 40/39 definizioni UI attive, forniscono i codici in `Data/saved_tactic_codes.json`. I bit interni del compito vengono separati; non sono esposti come scelte di vecchie versioni di FM. Solo un ruolo attivo esatto, un singolo compito e campi terminali supportati ricevono una corrispondenza. Bit sconosciuti, combinazioni ambigue e campi mancanti rimangono sconosciuti. Le maschere delle posizioni sono distinte dall’ordine della familiarità del giocatore; il vecchio sweeper non viene abilitato.

Il wrapper locale accetta il marcatore dei battitori `0b0502` e un limite finito di 4096 byte per i testi: entrambi servono al campione verificato e mantengono i controlli strutturali. Il pacchetto fmsave installato non viene modificato.

La ricerca preliminare ha verificato 90 preset e tre tattiche del campione, per 2.046 posizioni/fasi e 74 dei 79 sottoruoli attivi effettivamente incontrati. I cinque restanti codici sono presenti negli enum e negli asset, ma non sono stati incontrati nei campioni. Non sono ancora disponibili coppie di salvataggi con una sola modifica controllata: il risultato espone ancora `controlledValidation=false` e non promette compatibilità con build diverse. I nomi completi confermano **AdM = Attaccante di movimento / Channel Forward** e **Amn = Attaccante di manovra / Deep-Lying Forward**.

## Punteggi e stelle

La lettura identifica i sottoruoli; **non ricostruisce la formula ufficiale delle stelle**. La classifica continua a usare i profili trasparenti dell’editor descritti in [TacticalRoles.md](TacticalRoles.md). Le etichette italiane sono quelle delle risorse del gioco, ma pesi degli attributi, punteggi e conversione in stelle restano stime. I pesi di selezione della squadra e i dati dei voti partita trovati negli archivi non vengono confusi con l’idoneità al sottoruolo.

## Backend e verifiche

```sh
python tactics_reader.py "carriera.fm" --output tattiche.json
```

Il servizio espone anche `{"command":"my_tactics","clubUid":<id>}` con avanzamento. Il risultato include posizione, lato, ID del catalogo, ID interno, compito e valori grezzi per ogni fase. L’output del comando non può sovrascrivere un file `.fm`.

I test pubblicabili costruiscono record interamente sintetici: verificano tutti i 79 codici, separazione dei compiti, lati centrali, corruzione/troncamento, build sconosciute, proprietario umano, conteggi e copie delle giovanili. **Non vengono distribuiti salvataggi, record estratti dai salvataggi, archivi del gioco o schermate.**

## English

**My squad → Best by tactic** lazily reads the managed first team’s saved tactics and shows separate IP/OOP pitches. Select the same paired slot on the pitch or in the list; rank up to five players from the current squad scope for IP, OOP or both. Each selected phase uses its saved position and named role. Both phases average the two attribute profiles and require playable familiarity and complete attributes in both positions. Unknown or incompatible roles never fall back to generic estimates.

Tactics remain read only. Player opening, comparison-pool actions and two-player quick comparison are available. Club/save changes clear cached results. The active tactic is not inferred. Only the first identified human manager and build `26.3.2+2329565`, schema 26, are currently supported.

The effective 64-bit terminal role/duty mask is decoded against the 79 active managed role enum constants, cross-referenced with UI assets. Initial base-role headers are not used as effective roles. Structural checks and bounded compatibility adjustments protect the optional reader. Numerical role decoding does not reproduce the game’s suitability formula: recommendation weights and stars remain documented editor estimates. Tests use wholly synthetic records; no game archives or save-derived record fixtures are shipped.

## Riferimenti / Sources

- Resource labels, active UI role definitions and managed enums from the installation files supplied for read-only research; original files are not distributed.
- [fmsave structural reader](https://github.com/rhiever/fmsave/blob/main/src/fmsave/readers/tactics.py), pinned dependency `0.5.11`.
- [Il2CppDumper metadata structures](https://github.com/Perfare/Il2CppDumper/blob/master/Il2CppDumper/Il2Cpp/MetadataClass.cs), consulted to read managed enum constants.
