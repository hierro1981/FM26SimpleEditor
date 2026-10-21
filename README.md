# FM26 Simple Editor · v14.3

[![Download totali](https://img.shields.io/github/downloads/hierro1981/FM26SimpleEditor/total.svg)](https://github.com/hierro1981/FM26SimpleEditor/releases)

Italiano | [English](README.en.md)

FM26 Simple Editor è un editor locale e non ufficiale per i salvataggi di **Football Manager 26**. Permette di cercare i giocatori per nome, squadra o ID, modificarne attributi, abilità, posizioni e data di nascita quando disponibile, e confrontarli tra loro.

Include un’anteprima dei valori sulla scala 1–20 del gioco e grafici per analizzare l’evoluzione degli attributi. Le modifiche vengono salvate in una **nuova copia verificata** del file `.fm`, preservando il salvataggio originale.

## Scarica l’eseguibile Windows

La compilazione automatica **Build Windows executable** genera `FM26SimpleEditor-win-x64.zip` a ogni push su `main`, su avvio manuale da `main` e per le release pubblicate. Nei branch di sviluppo e nelle pull request non genera EXE o ZIP; anche l’avvio manuale su un branch diverso da `main` viene saltato. Apri [Actions](https://github.com/hierro1981/FM26SimpleEditor/actions/workflows/build-windows.yml), seleziona una compilazione riuscita e scarica **FM26SimpleEditor-win-x64** da **Artifacts** (richiede l’accesso a GitHub; conservazione 30 giorni). Per avviare una compilazione manuale usa **Run workflow**.

Quando viene pubblicata una nuova release basata su un commit che contiene questo workflow, lo ZIP portatile e il setup `FM26SimpleEditor-<versione>-Setup-win-x64.exe` vengono allegati automaticamente alla [release](https://github.com/hierro1981/FM26SimpleEditor/releases). Le release già pubblicate non vengono ricompilate né aggiornate.

Estrai lo ZIP completo e avvia **FM26SimpleEditor.exe**, mantenendo gli script e `requirements.txt` nella stessa cartella. Il pacchetto è per **Windows x64** e richiede **.NET 8 Desktop Runtime x64** installato sul PC; il framework non è incluso. Puoi scaricarlo dalla [pagina ufficiale .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0), scegliendo **.NET Desktop Runtime → Windows → x64**. Il solo runtime .NET generico non basta per Windows Forms. Non servono Visual Studio o l’SDK. Mantieni anche le DLL e i file `.json` dello ZIP accanto all’EXE. Python e le dipendenze vengono verificati e predisposti all’avvio come descritto sotto. Sono inclusi anche licenza, documentazione e sorgenti del commit compilato.

### Installazione guidata (dalle prossime release)

Esegui il file **Setup-win-x64.exe** della release. Il setup installa l'editor per l'utente corrente, crea un collegamento nel menu Start e offre quello sul desktop. Nella pagina delle operazioni facoltative puoi autorizzare separatamente:

- **Python e dipendenze:** cerca un interprete compatibile; se manca, scarica Python **3.13.16 x64 direttamente da python.org**, verifica la firma e lo installa per l'utente corrente, senza WinGet e senza modificare il PATH. Prepara le dipendenze in `%LOCALAPPDATA%\FM26SimpleEditor\runtimes`, usando gli stessi ambienti dei controlli all'avvio.
- **.NET 8 Desktop Runtime x64:** se manca, scarica il runtime ufficiale Microsoft e verifica checksum e firma. Questa installazione puo richiedere il consenso amministrativo di Windows.

Entrambe le opzioni sono inizialmente deselezionate: puoi installare solo l'editor e configurare i prerequisiti successivamente. I download richiedono Internet. Se la configurazione fallisce, il setup segnala il problema e salva i dettagli in `%LOCALAPPDATA%\FM26SimpleEditor\installer-prerequisites.log`; puoi rieseguirlo. Gli aggiornamenti riutilizzano la cartella e preservano la configurazione locale. La disinstallazione rimuove l'editor e i collegamenti, conservando configurazione, ambienti Python e prerequisiti condivisi.

Lo ZIP resta disponibile come versione portatile. Il setup viene compilato e verificato su `main`, ma distribuito automaticamente solo per le nuove release; non viene generato nei branch di sviluppo. Le release precedenti restano invariate.

## Progetto e requisiti tecnici

Progetto C# Windows Forms per Visual Studio 2026, con lettore locale Python. Per compilare richiede Windows, SDK .NET 8 e carico di lavoro **Sviluppo di applicazioni desktop .NET**. Il lettore usa Python 3.12 o successivo e le dipendenze in `requirements.txt`, verificate e predisposte all’avvio.

## Novità della versione 14.3

- **Confronto rapido:** seleziona due giocatori con Ctrl+clic e scegli **Confronta i due giocatori selezionati** dal tasto destro, nella ricerca iniziale, nella rosa di **La mia squadra** o in **Migliori per posizione**. L'azione carica il primo giocatore nell'ordine dell'elenco e apre il confronto con il secondo, anche di un'altra squadra e senza dover aprire prima un giocatore. Entrambi vengono aggiunti all'elenco di confronto.
- Il tasto destro su una riga già selezionata conserva la selezione multipla. Il confronto rapido si abilita solo con due giocatori distinti; le azioni sul singolo restano disponibili con una sola selezione. Restano attivi i controlli sulle modifiche non salvate.
- **Migliori per tattica:** nuovo sotto-tab in **La mia squadra**, con le tattiche della prima squadra lette dal salvataggio e i campi in possesso/non in possesso affiancati. Seleziona uno slot sul campo o nell’elenco e valuta i cinque migliori della rosa filtrata per una fase o entrambe, rispettando posizioni e sottoruoli effettivi.
- Nomi italiani dei **79 sottoruoli** allineati alle risorse linguistiche del gioco, inclusi **AdM = Attaccante di movimento** e **Amn = Attaccante di manovra**. Codici dei ruoli ricavati dagli enum del gioco; lettura supportata per `26.3.2+2329565`, schema tattiche 26. Le combinazioni sconosciute non producono suggerimenti inventati.
- Tattiche in sola lettura; apertura giocatore, elenco di confronto e confronto rapido disponibili anche nella nuova classifica. Stelle e punteggi restano **stime dell’editor**, non la formula ufficiale del gioco. [Lettura, compatibilità e limiti](docs/SavedTactics.md).

## Novità della versione 14.1

- Catalogo di **79 combinazioni uniche sottoruolo/fase FM26** (40 in possesso, 39 non in possesso), filtrate per posizione: attaccante di manovra, centravanti, fulcro del gioco e gli altri ruoli del riferimento FM26 26.3.2. Le etichette italiane sono allineate alle risorse del gioco; il catalogo conserva i nomi inglesi.
- Si possono valutare due sottoruoli anche per la stessa posizione. I menu conservano le scelte durante il cambio lingua e scartano un ruolo se incompatibile con una nuova posizione.
- Aggiunta **PA** accanto alla CA nei migliori per posizione; una PA non leggibile resta **—** e non influenza il punteggio.
- **Criteri e fonti** mostra gli attributi e i pesi effettivi. La fonte ufficiale indica familiarità e attributi del ruolo, ma nelle fonti consultate non pubblica una formula numerica: i profili/pesi e le stelle dell’editor sono quindi stime dichiarate, non valutazioni ufficiali del gioco. [Metodo, provenienza e limiti](docs/TacticalRoles.md).

- Corretto il filtro rose disabilitato al primo ingresso dopo il caricamento. Oltre ai gruppi, ogni sotto-squadra si può scegliere separatamente: nome leggibile quando disponibile, altrimenti slot/ID senza inventare U20/U18.
- Ordinamento iniziale della rosa per posizione (portiere → attaccante, sinistra → centro → destra), poi CA decrescente.
- Fino a **5** candidati per posizione, con stelle derivate dal punteggio (`punteggio / 4`, arrotondato a mezza stella).
- **Ctrl/Shift+clic sul campo** aggiunge/rimuove una seconda posizione; un secondo menu consente la stessa scelta. Con due posizioni, il giocatore deve essere utilizzabile in entrambe: il punteggio è la media dei due profili e la familiarità mostrata comprende entrambe. Passa sul punteggio per i valori separati.

- Sotto-tab **Staff** in **La mia squadra**, caricato su richiesta e aggiornabile senza ricaricare il salvataggio.
- Elenco in sola lettura con ricerca per nome, club o ID, colonne ordinabili, scrollbar e numeri allineati a destra. Include lo staff del club gestito e delle affiliate controllate presenti nel suo elenco squadre, indipendentemente dal filtro delle rose giocatori.
- Anagrafica, club del contratto/staff e club che lo elenca, paga settimanale nella valuta base del salvataggio, date del contratto e CA/PA quando disponibili. I dati assenti sono indicati con **—**, non sostituiti da zero.
- Selezionando una persona si vedono personalità, adattabilità e preferenze decodificate. Le preferenze descrivono uno stile, non la qualità professionale. Per il gestore umano abilità e preferenze possono mancare.
- L’allenatore umano è identificato esplicitamente; per gli altri il ruolo resta **Non identificato**. Gran parte degli attributi professionali e la dirigenza non sono leggibili. Nessuna modifica dello staff è consentita.
- Gli errori del lettore staff restano confinati al suo sotto-tab: rosa, suggerimenti ed editor giocatori continuano a funzionare. Interfaccia e messaggi in italiano e inglese.

## Novità della versione 14

- Nuovo tab **La mia squadra**, con rilevamento del club gestito dal primo allenatore umano del salvataggio.
- Filtro **Solo prima squadra** predefinito, **Altre squadre** o **Tutti i giocatori**, basato sugli ID delle rose e comprensivo delle squadre affiliate presenti nell’elenco del club.
- Elenco con ricerca per nome/ID, colonne ordinabili, scrollbar, prestiti evidenziati e apertura del giocatore o aggiunta al confronto.
- Sotto-tab **Migliori per posizione**: scegli sul campo o nel menu la posizione e visualizza fino ai tre migliori candidati della rosa selezionata.
- Il punteggio usa gli stessi profili del report per ruolo: media degli attributi su scala 1–20, pesi 3/2/1. Richiede familiarità almeno 12/20 e dati completi; CA e PA non alterano il punteggio. A parità contano familiarità, nome e ID. Non è la formula di valutazione del gioco.
- Dopo aver salvato un giocatore la rosa viene riletta all’apertura del tab; **Aggiorna rosa** permette di rileggerla anche manualmente. Le modifiche non salvate non entrano nella classifica.
- Se il gestore è senza contratto o non è riconoscibile, il tab segnala l’assenza senza scegliere un club arbitrario. Con `fmsave 0.5.10` è supportato solo il primo allenatore umano, anche nelle carriere multigiocatore.
- Tutti i nuovi testi sono disponibili in italiano e inglese. Il branch `v14` esegue verifiche DLL/Python senza generare EXE, ZIP o setup.

## Novità della versione 13.1.1

- Badge dei download totali degli allegati alle release in cima a entrambi i README, con collegamento alle release GitHub.
- Note delle versioni ordinate dalla più recente alla più vecchia, in italiano e inglese.

## Novità precedenti della versione 13.1

- Tooltip dello storico con soli nome attributo, valore e data, nei colori delle rispettive serie del grafico.

## Novità precedenti della versione 13

- **Lingua**: elenco Italiano / English al posto della casella; traduce l’interfaccia, le colonne, i filtri, i tooltip, i messaggi, l’anteprima e i report. Il cambio è immediato e conserva giocatore, modifiche e filtri.
- La preferenza viene salvata in `FM26SimpleEditor.config.json` **accanto all’eseguibile**, con contenuto `{"language":"en"}` oppure `{"language":"it"}`. Viene letta prima della preparazione di Python. File assente, non valido o lingua non supportata: italiano. La scrittura è atomica; se la cartella non è scrivibile viene mostrato un avviso e la lingua resta attiva nella sessione.
- Testi in `Resources/Strings.resx` (italiano, risorsa base) e `Resources/Strings.en.resx` (inglese). Le risorse sono compilate; per cambiare o aggiungere una traduzione occorre ricompilare. Distribuire anche la cartella `en` con la DLL delle risorse inglesi.
- **Informazioni…** mostra la versione dell’assembly e il repository in una finestra compatta, senza attribuzione personale nel testo descrittivo. All’apertura controlla l’ultima release stabile pubblica su GitHub, con timeout e possibilità di riprovare. Il confronto è numerico e ignora bozze e prerelease. Se esiste una versione più recente, **Apri la nuova release** apre il browser. Non installa aggiornamenti e non invia dati dei salvataggi.
- I nomi dei giocatori e delle squadre, i percorsi e i log tecnici prodotti da Python, pip o Windows conservano il testo originale. I messaggi applicativi del lettore sono tradotti nell’interfaccia.
- Verifica Windows su DLL e risorse, senza generare EXE o ZIP nel branch.

- Nella scheda giocatore, tre pulsanti con icona copia trasferiscono negli appunti nome, ID pubblico e ID interno, senza etichette o separatori numerici. Il pulsante dell’ID pubblico è disabilitato se manca il dato.
- Lo storico seleziona inizialmente gli attributi fondamentali e importanti della posizione principale. I preset per posizione sono applicabili dalla lista dedicata; la selezione manuale viene mantenuta per lo stesso giocatore. I preset sono indicazioni editoriali, non pesi ufficiali di FM.
- Passando sul grafico compaiono tutti i valori disponibili del periodo più vicino, con nome attributo, valore e data, nei colori delle rispettive serie. I delta sono verdi per i miglioramenti, rossi per i peggioramenti e grigi se invariati; per propensione agli infortuni e scorrettezza i valori inferiori sono migliori. Un periodo mancante non genera un delta con periodi più lontani.
- Il caricamento del giocatore mostra percentuale, fasi effettive e contatore degli attributi letti; la durata delle singole fasi può variare.
- Nello storico interno, **Mostra mesi ricostruiti** aggiunge mese/anno ai campioni, mantenendo disponibile la vista numerata.

## Novità precedenti della versione 12

- Distribuzione Windows più leggera senza framework incluso: richiede .NET 8 Desktop Runtime x64.
- Filtri della ricerca con altezza adattiva: passando da due righe a una, l’elenco recupera subito lo spazio; nelle finestre strette restano scorribili.
- Confronto con disposizioni in campo visibili al primo ingresso e report con nomi dei giocatori, analisi delle caratteristiche e contropartite motivate dagli attributi.

- Confronto con colonne **Gruppo → Attributo → A → Δ → B → Migliore**, frecce verso il giocatore favorito e segno numerico della differenza conservato.
- Nuova sottoscheda **Report per ruolo**, con scelta della posizione cliccando sul campo o tramite elenco, attributi rilevanti, punteggi, familiarità, vantaggi e suggerimento motivato.
- Sigle raggruppate nella colonna **Ruolo principale** per riconoscere i multiposizione: per esempio `D (SC), M, C (C)`. La lingua scelta alterna le sigle italiane e inglesi.
- Verifica C# su Windows con compilazione di sole DLL e test dei calcoli e dei controlli. I branch non generano EXE, ZIP o artefatti scaricabili.

## Novità precedenti della versione 11

- **Prestiti riconoscibili nell’elenco:** righe evidenziate in ocra, anche quando selezionate, con legenda e tooltip che indica squadra attuale e proprietaria. I prestiti con stato non disponibile non vengono colorati come confermati.
- **Squadra di riferimento dei prestiti:** il selettore **Prestiti nella squadra** usa **In cui gioca** come impostazione iniziale; scegli **Proprietaria** per vedere i prestiti tra i giocatori del club proprietario. Colonna Squadra, elenco delle squadre, ricerca per testo e ordinamento usano tutti il riferimento scelto. Se la proprietaria non è leggibile, viene indicata come non disponibile. Il selettore resta visibile anche nascondendo i filtri opzionali; **Azzera filtri** conserva questa preferenza di visualizzazione per la sessione. Anteprima, confronto e stelle restano riferiti alla squadra in cui il calciatore gioca.
- **Salvataggio sempre raggiungibile:** **Salva modifiche** è nella barra superiore, vicino ad **Apri salvataggio**, insieme all’indicazione delle modifiche non salvate. È accessibile da tutte le schede e si abilita quando ci sono modifiche, anche durante l’editing di una cella. I pannelli della barra e della ricerca si ridimensionano al contenuto, riducendo gli spazi vuoti sopra le schede e i filtri. Continua a creare una nuova copia verificata del salvataggio.

## Novità precedenti della versione 10

- **Controllo automatico dell’ambiente all’avvio:** ricerca Python 3.12 o successivo, verifica le versioni delle dipendenze e controlla che le librerie siano importabili.
- **Installazione guidata di Python:** se manca un interprete compatibile, propone Python 3.13 tramite WinGet oppure il sito ufficiale.
- **Dipendenze predisposte automaticamente:** se mancanti o non utilizzabili, vengono installate o riparate in un ambiente dedicato all’editor. Sono disponibili dettagli, annullamento e **Riprova**.
- **Distribuzione Windows pronta all’uso:** GitHub Actions compila e verifica lo ZIP Windows x64; dalla v12 il runtime .NET non è incluso e serve .NET 8 Desktop Runtime x64 installato sul PC e lo allega alle nuove release. Il pacchetto include anche i sorgenti corrispondenti.
- **Documentazione bilingue:** README in italiano e inglese, con istruzioni per il pacchetto compilato e per l’avvio dai sorgenti.

## Novità precedenti della versione 9

**Apertura e salvataggio:** barra con percentuale, descrizione della fase e contatori disponibili. Il catalogo mostra i giocatori elaborati; compressione e scrittura mostrano i MiB; la verifica mostra le sezioni completate. La percentuale complessiva usa intervalli pesati per fase e non è una stima del tempo restante. Può rimanere ferma mentre una fase indivisibile del lettore lavora, ad esempio la decodifica dei giocatori. Non usa un timer per simulare avanzamento. Il 100% viene mostrato solo dopo il successo dei controlli e la preparazione dei dati nell'interfaccia. Un errore non viene presentato come completamento. Le altre brevi letture prive di misure restano indicate come attività in corso.

**Confronto:** aprendo la selezione del secondo giocatore compare un campo di ricerca dentro il menu. Digita una parte del nome, squadra, ID pubblico o interno; i risultati rispettano l'ordine per posizione. Il filtro esterno esistente rimane combinabile con quello del menu. Clic o Invio confermano, Esc o un clic fuori annullano senza cambiare il giocatore selezionato. Puoi aprire il menu con clic, F4 o freccia giù e usare le frecce per muoverti. Quando non ci sono corrispondenze, viene mostrato un messaggio e non è possibile confermare una voce inesistente. La ricerca nel menu non carica un giocatore a ogni carattere.

## Novità precedenti della versione 8

Storico interno da un singolo salvataggio supportato, grafico con selezione multipla degli attributi, tabella con valori e Δ per ogni campo, ricerca e selezione rapida degli attributi, punto attuale opzionale. Vedi la sezione Storico per copertura e date.

## Novità precedenti della versione 7

- Ricerca su due righe: nome/ID sopra, filtro squadre ed elenco sotto. I filtri opzionali hanno un'area di altezza limitata, con scorrimento interno e possibilità di nasconderli. La tabella conserva lo spazio disponibile e la scrollbar orizzontale.
- Posizioni in ordine: GK, SW, DL, DC, DR, WBL, DM, WBR, ML, MC, MR, AML, AMC, AMR, STC. Questo ordine si applica alla familiarità, al filtro ruoli, alla colonna ruolo principale e ai candidati del confronto. Se tutte le familiarità sono 1 o meno, il ruolo è sconosciuto e va in fondo all'ordinamento.
- Etichette degli attributi colorate: viola fondamentale, blu importante, verde utile, sfondo normale per gli altri. Selettore della posizione di riferimento, inizialmente automatico. È una guida editoriale per posizione, non un peso ufficiale FM né una valutazione per compito tattico. I colori delle barre continuano a indicare la qualità del valore.
- Stelle CA e PA su tre riferimenti: squadra, assoluto e campionato disponibile. La scala assoluta è `abilità / 40`, arrotondata a mezza stella e limitata a 0,5–5; zero/PA sconosciuta danno N/D. Il confronto relativo mantiene la formula `2,5 + (abilità − CA media) / 20`, arrotondata a mezza stella e limitata a 0,5–5. Le medie usano i valori originali del file, così restano stabili mentre modifichi il giocatore.
- Il campionato è ricavato da un gruppo di classifica leggibile contenente l'esatto team ID: regola conservativa di divisione con 18–26 squadre e turni per sede pari a squadre meno uno. Si richiede un unico insieme di squadre e almeno 3 giocatori con CA nota. La media riguarda i giocatori assegnati agli esatti team ID di quella classifica. Campionati piccoli, gruppi ambigui o non decodificati restano N/D. Il nome può mancare: in tal caso appare l'ID o “Divisione senza nome”. L'associazione alla competizione è dedotta dal lettore, non garantita dal gioco.
- Campi con proporzioni verticali costanti, punti scalabili e centrati; altezza adattata nel confronto. Colonne del confronto distribuite su tutta la larghezza, con minimi e scorrimento quando necessario. Colonna “Migliore” con A/B e nome; CA e PA indicano anche il vincitore nella riga informativa. Per infortuni e scorrettezza vince il valore più basso.

## Avvio

Estrai questo progetto completo in una nuova cartella. Apri `FM26SimpleEditor.csproj` in Visual Studio, compila e avvia. Gli script Python e `requirements.txt` vengono copiati automaticamente accanto all'eseguibile.

A ogni avvio una finestra verifica l’interprete e le librerie prima di aprire l’editor:

- **Python assente o troppo vecchio:** propone **Installa Python 3.13** tramite WinGet per l’utente corrente. L’installazione parte solo premendo il pulsante, richiede internet e accetta le condizioni dei pacchetti e della sorgente WinGet. In alternativa, **Sito ufficiale Python** permette l’installazione manuale; poi premi **Riprova**, senza riavviare l’editor. Se WinGet manca o fallisce, usa questa alternativa.
- **Python presente:** controlla le versioni richieste di `fmsave` e `zstandard` e i relativi import, compreso `backports.zstd` dove richiesto. Se tutto è pronto, avvia subito l’editor.
- **Dipendenze mancanti, diverse dalle versioni richieste o non utilizzabili:** crea o ripara un ambiente dedicato in `%LOCALAPPDATA%\FM26SimpleEditor\runtimes\python-X.Y` e installa automaticamente da PyPI le dipendenze fissate in `requirements.txt`, comprese quelle transitive. Le librerie globali delle altre applicazioni non vengono modificate. Serve internet solo quando occorre installare o riparare.

La finestra mostra le fasi e i dettagli degli errori, consente di riprovare e permette di uscire durante la preparazione. Un’installazione interrotta viene ricontrollata all’avvio successivo. L’editor parte solo dopo la verifica finale e usa esattamente l’interprete verificato.

Non è obbligatorio avere `python` nel PATH: vengono cercati anche gli ambienti dell’editor, il registro Windows, le installazioni locali e il launcher `py`. Per indicare un interprete preferito, imposta `FM26_PYTHON` al percorso completo prima di avviare Visual Studio o il programma; se non è utilizzabile la ricerca continua. Restano disponibili l’installazione manuale con `python -m pip install -r requirements.txt` e `Installa_dipendenze.cmd`.

## Ricerca, modifica e anteprima

**Ricerca giocatori** cerca per nome, squadra, ID pubblico o ID interno. Nel campo **Filtra squadre** puoi scrivere anche una parte centrale del nome: ad esempio `tri`. L'elenco a destra mostra solo le squadre corrispondenti; puoi sceglierne una oppure lasciare **Tutte le corrispondenze** per cercare in tutte quelle squadre. Fai doppio clic su una riga oppure scegli **Apri scheda giocatore**. Sono mostrati al massimo 2.500 risultati per volta; la ricerca si applica sempre all'intero catalogo e il conteggio riporta tutti i risultati.

### Filtri opzionali combinabili

I filtri sono tutti disattivati all'avvio. Ogni intervallo si attiva con la casella **Usa**; gli estremi sono inclusi. Età, scadenze e stato del contratto usano la data della partita nel salvataggio, mostrata sopra ai risultati.

- **Età**: intervallo iniziale 15–50 anni, modificabile da 0 a 120. La data di nascita non leggibile esclude il giocatore solo quando attivi questo filtro.
- **Posizione**: scegli una posizione e la familiarità minima: **Bravo (15+)** oppure **Può giocare (12+)**. Il secondo include anche i giocatori più bravi. Si tratta della posizione, non di un compito tattico. Nei risultati sono mostrate le posizioni con almeno 12/20 e la relativa valutazione.
- **CA e PA**: intervalli separati, inizialmente 0–200. Il controllo ammette fino a 255 per i dati già potenziati. I potenziali non noti o codificati negativamente sono esclusi soltanto quando filtri per PA o margine di crescita.
- **Contratto**: tutti, senza contratto rilevato, scaduto, scadenza entro 6 mesi, entro 12 mesi, in corso con scadenza nota, oppure dato non disponibile. Entro 12 mesi include i primi 6 mesi. Sono mesi di calendario e includono il giorno iniziale e la data limite; un contratto che termina oggi è ancora considerato in corso.
- **Altezza**: intervallo in centimetri, inizialmente 150–210, modificabile da 100 a 250.
- **Margine minimo PA − CA**: cerca giocatori con una differenza almeno pari al valore scelto.
- **Piede**: sinistro dominante, destro dominante o entrambi forti (almeno 15/20). Dominante significa più forte dell'altro piede; i due criteri possono sovrapporsi.
- **Prestito**: tutti, in prestito, non in prestito o dato non disponibile.

**Senza contratto rilevato** richiede contemporaneamente l'assenza di un contratto letto e di una squadra/club associati. Un giocatore associato a un club ma con contratto non leggibile va fra i **dati non disponibili**, non fra quelli senza contratto. Una scadenza mancante non viene inventata.

Le colonne numeriche sono allineate a destra. I risultati hanno scrollbar orizzontale e verticale quando servono; le colonne hanno larghezze ridimensionabili e tutte le colonne scorrono orizzontalmente. Cliccando un'intestazione si ordina in modo crescente, cliccandola di nuovo in modo decrescente. L'ordinamento è applicato a **tutti** i risultati, prima del limite di 2.500 righe: età, CA, PA e ID sono ordinati numericamente, le date cronologicamente. I valori mancanti restano in fondo in entrambe le direzioni. Il ruolo principale usa la familiarità più alta; a parità prevale la prima posizione nello schema del campo. La sua colonna si ordina da portiere a punta, con sinistra–centro–destra per linea. Dalla v12 visualizza sigle raggruppate per tutte le posizioni con familiarità almeno 15/20; se nessuna raggiunge 15 mostra la migliore conosciuta, oppure — quando non disponibile. Il tooltip elenca le posizioni e i valori esatti; l’ordinamento resta riferito alla posizione con familiarità più alta.

**Mostra filtri opzionali** nasconde o mostra i controlli mantenendo i criteri attivi. Il contatore resta visibile. **Azzera filtri** cancella testo, scelta della squadra e tutti i filtri opzionali. Le ricerche non modificano il salvataggio.

**Modifica giocatore** usa tutto lo spazio della scheda per gli attributi grezzi e la familiarità nelle posizioni. Il selettore **Lingua** alterna l’interfaccia italiana e inglese e applica l'ordine alfabetico della lingua scelta agli attributi; le posizioni seguono lo schema del campo. L'identità del campo nel file non dipende dall'etichetta né dalla sua posizione in tabella. Le traduzioni sono quelle dell'editor e possono differire da alcune diciture della localizzazione ufficiale del gioco.

Gli attributi rimangono sulla scala grezza nominale 1–100. Sono preservati anche i valori potenziati fino a 125 supportati dal lettore. Le posizioni usano 1–20; CA e PA hanno scala nominale 0–200, con supporto ai valori già modificati fino a 255. I codici PA negativi vengono conservati. La data di nascita cambia soltanto attivando la casella dedicata e quando il record della persona è individuato senza ambiguità.

Sopra i numeri degli attributi e delle posizioni sono disegnate barre. Anche CA e PA hanno un indicatore. Rosso corrisponde a 1–5, giallo a 6–10, verde a 11–15 e blu a 16–20; per i valori grezzi la valutazione del colore usa la conversione 1–20. Per propensione agli infortuni e scorrettezza il colore è invertito, dato che un valore basso è favorevole. La lunghezza indica il valore effettivo rispetto alla scala nominale. La scala dei valori non viene cambiata: nell'editing rimane grezza.

**Anteprima nel gioco** è in sola lettura e riflette le modifiche ancora non salvate: attributi 1–20 ordinati e raggruppati, stelle stimate e posizioni sul campo. I valori sono convertiti con `clamp((grezzo + 2) / 5, 1, 20)`, usando divisione intera. Passando sui punti del campo si legge la familiarità esatta. Verde indica 18–20, giallo 15–17, arancio 12–14. I valori inferiori restano attenuati. Gli attributi nascosti sono esclusi di default; si possono mostrare con la casella nell'anteprima.

### Le stelle sono una stima dell'editor

Le stelle di Football Manager dipendono dalla valutazione dello staff e dal contesto. Questo progetto non riproduce quel giudizio né ricava stelle ufficiali dal salvataggio. Calcola una stima trasparente rispetto alla CA media dei giocatori della stessa squadra, oppure dello stesso club quando il riferimento alla squadra manca. Servono almeno tre giocatori con CA nota; altrimenti mostra N/D.

Formula dell'editor: `2,5 + (abilità − CA media) / 20`, arrotondata a mezza stella e limitata a 0,5–5. Una differenza di 20 punti equivale a una stella. Il valore non è una formula di FM. Per il potenziale ignoto o codificato negativamente non vengono inventate stelle. La media usa il file aperto, mentre i valori del giocatore in anteprima includono le modifiche ancora non salvate.

## Confronto fra giocatori

Ogni giocatore aperto viene aggiunto automaticamente all'elenco di confronto, senza duplicati. Dalla ricerca, il tasto destro su una riga offre **Aggiungi all'elenco di confronto** e **Confronta con il giocatore aperto**. Il numero di voci appare nel titolo della scheda **Confronto**.

Apri un giocatore, poi scegli **Confronta…** dalla modifica o vai nella quarta scheda. Il giocatore A è quello aperto, con gli eventuali attributi, posizioni, CA e PA ancora non salvati. Per B puoi usare i compagni della stessa squadra oppure gli altri giocatori dell'elenco, filtrandoli per nome o ID. Se manca il riferimento alla squadra, si usa il club. Puoi rimuovere il selezionato dall'elenco o svuotare l'elenco.

Il confronto è in sola lettura: mostra stelle CA/PA stimate, le due mappe del campo e una tabella con barre, valori e differenza **A − B**. Entrambi sono valutati in stelle rispetto alla squadra di A. Puoi nascondere il campo per dare più spazio agli attributi. La tabella può mostrare scala 1–20 o grezza e includere gli attributi nascosti. Le differenze inferiori a un punto possono sparire sulla scala 1–20: attiva i valori grezzi o passa sulla differenza per leggerle. L'elenco dura nella sessione, viene mantenuto dopo il salvataggio di una copia e si azzera aprendo un altro file.

### Report per ruolo — versione 12

Il report usa i nomi dei giocatori in punteggi, colonne e commenti. L’analisi descrive difesa, costruzione, soluzioni offensive, mobilità, sostegno, duelli fisici/aerei e portiere quando almeno due attributi del profilo selezionato sono disponibili. Segnala differenze medie ponderate di almeno 0,5 punti, cita i vantaggi e le contropartite; non deduce l’indole offensiva dalla sola velocità. I dati incompleti restano esplicitamente parziali. Le disposizioni in campo sono visibili già al primo ingresso nel confronto.

Nel confronto apri **Report per ruolo** e clicca una posizione sul campo; puoi usare anche l’elenco, accessibile da tastiera. La selezione iniziale è il ruolo principale di A. Il punto giallo indica la posizione valutata. Il report usa gli attributi e la familiarità di A comprese le modifiche non salvate, e i valori di B letti dal file. Le mappe doppie lasciano spazio al report quando questa sottoscheda è aperta; le opzioni scala grezza e attributi nascosti riguardano la tabella generale.

È un confronto **per posizione**, con i profili editoriali già usati per colorare gli attributi nell’editing: non riproduce un compito tattico o la formula del motore partita. La media usa i valori 1–20 visibili (`clamp((grezzo + 2) / 5, 1, 20)`, divisione intera) e pesi **3 fondamentale, 2 importante, 1 utile**. CA/PA, stelle e attributi estranei al profilo non entrano nel punteggio. Valori grezzi potenziati vengono limitati a 20; valori assenti, duplicati o non validi non vengono inventati. Punteggi uguali sulla scala del gioco rimangono uguali anche se i grezzi differiscono.

Il suggerimento segue regole esplicite: se manca un attributo del profilo o la familiarità, non propone un preferito; sotto 12/20 per entrambi segnala necessità di adattamento. Se solo uno raggiunge 12/20, lo preferisce per prontezza nella posizione, mostrando comunque i due punteggi senza penalità nascoste. Se entrambi sono almeno a 12, preferisce la media maggiore; uno scarto inferiore a **0,25/20** è considerato equivalente, con eventuale preferenza per chi ha almeno **3 punti** di familiarità in più. La familiarità resta sempre visibile separatamente. Le soglie sono scelte dell’editor, non regole ufficiali di FM.

Il riepilogo mostra copertura degli attributi e fino a tre vantaggi per ciascun giocatore, ordinati per differenza × peso. La tabella riporta rilevanza e valori che giustificano il suggerimento. Si tratta di caratteristiche favorevoli per la posizione scelta, non di una previsione di gol, voti o rendimento in una tattica specifica.

Nella tabella generale, **Δ = A − B** rimane numerico e la freccia punta a **sinistra per A** o **destra per B**. Per propensione agli infortuni e scorrettezza vince il valore più basso: la direzione può quindi essere opposta al segno. `0 =` indica parità sulla scala selezionata. Il tooltip conserva la differenza grezza.

## Storico degli attributi — versione 8

La quinta scheda apre automaticamente **Storico interno · sperimentale** quando riconosce una sequenza nel file aperto. Non è necessario caricare altri salvataggi. Il lettore è limitato alla build 26.3.2 con schema game_db 4000, verificata sui salvataggi di prova: richiede almeno due campioni, struttura ripetuta coerente e un solo candidato. Dati assenti, formati diversi e sequenze ambigue rimangono non disponibili; in questi casi viene proposta la modalità dei salvataggi datati con il motivo del mancato riconoscimento.

Sono stati identificati **47 attributi visibili**, in campioni da 69 byte. Non vengono attribuite CA/PA storiche, valori dei piedi o attributi nascosti ai campioni che non li contengono. La presenza e la copertura dello storico possono variare tra giocatori. Il lettore è sperimentale e non è una garanzia di supporto per altri formati.

**Asse orizzontale dello storico interno:** dalla v13 **Mostra mesi ricostruiti** è attivo quando sequenza, file di origine e data del salvataggio sono univoci. La proiezione assegna i campioni ai mesi completi precedenti al mese del salvataggio: per esempio, 19 campioni con riferimento 10/02/2028 diventano luglio 2026–gennaio 2028. La cadenza mensile è una deduzione confrontata con due salvataggi e con il grafico del gioco: **non è la decodifica delle date individuali né del giorno esatto**. Tabella, tooltip e riepilogo indicano che i mesi sono ricostruiti; la riga Attuali conserva la data effettiva del file. La proiezione non modifica il salvataggio né i campi data dei campioni. Se manca un riferimento univoco, o disattivi l’opzione, restano Campione 1, 2, 3… I valori degli attributi sono quelli memorizzati, senza interpolazione.

A sinistra trovi una **lista cercabile con caselle di selezione**. Puoi mostrare più attributi contemporaneamente; la selezione iniziale segue gli attributi fondamentali e importanti della posizione principale. **Seleziona visibili** aggiunge tutti i risultati del filtro; **Nessuno** svuota l'intera selezione. Il filtro nasconde solo le voci della lista, mantenendo gli attributi già selezionati. Cambiare lingua conserva la selezione per identità del campo.

Ogni attributo ha colore e tratteggio riconoscibili nella legenda scorrevole, una curva e una colonna nella tabella. Passa sul grafico per leggere attributo, valore e campione/data del periodo più vicino, nei colori delle rispettive serie; i percorsi dei file non compaiono nel tooltip. **Scala grezza** alterna i valori originali e la conversione 1–20. **Mostra Δ** aggiunge la variazione rispetto alla precedente osservazione disponibile per ogni attributo. Non c'è un limite arbitrario al numero di attributi selezionabili; per grafici più leggibili conviene confrontare pochi attributi alla volta.

**Includi valori attuali del file** aggiunge un punto e una riga evidenziata “Attuali”, separata concettualmente dai campioni interni. È un confronto con lo stato del file, non un ulteriore mese storico. È disattivabile. Le modifiche non ancora salvate sono escluse. Le colonne scorrono orizzontalmente e il separatore tra grafico e tabella è trascinabile.

Resta disponibile **Confronta salvataggi datati**: **Aggiungi salvataggi…** seleziona fino a 20 altri `.fm` della stessa carriera e passa a questa modalità. Ogni punto usa gli attributi e la data effettiva della partita nel relativo file, con CA/PA se disponibili. Anche qui puoi selezionare più attributi. Le due modalità mantengono assi distinti: i campioni senza data non vengono mescolati con le osservazioni datate. Versioni della stessa data restano punti distinti, senza linee o Δ che suggeriscano crescita nel tempo. Le note di corrispondenza dell'identità sono visibili passando sul nome del file; i file esclusi sono indicati nel messaggio.

Il confronto tra file verifica ID, nome e nascita; una nascita modificata richiede ID pubblico e interno coerenti ed è segnalata. Senza ID pubblico resta richiesta la nascita coerente. Le letture non modificano i salvataggi e salvare un giocatore non riscrive i suoi campioni storici.

## Salvataggio

**Salva modifiche** crea un nuovo `.fm`. La copia verificata diventa il file di lavoro: le modifiche a un secondo giocatore mantengono quelle già salvate al primo. L'originale non può essere scelto come destinazione. Il programma segnala le modifiche non salvate prima di cambiare giocatore o file.

Sono incluse le correzioni precedenti: JSON Unicode, ID pubblico facoltativo, verifica della data e ricostruzione di tutte le voci dell'indice, comprese quelle annidate. La scrittura verifica tutte le sezioni compresse prima e dopo la modifica. Un file con indice già incoerente viene rifiutato: usa un salvataggio originale funzionante.

## Verifica

Dopo aver installato le dipendenze, esegui `python -m unittest discover -s tests -v` per i test della ricerca dei prestiti (squadra attuale/proprietaria, stato ignoto, filtri combinati, ordinamento e limite dei risultati) e del controllo iniziale Python: interprete troppo vecchio, pacchetti assenti o con versione diversa, import non funzionanti, pip assente e requisiti non validi. La finestra Windows, il rilevamento delle installazioni e l’installazione con WinGet richiedono una prova su Windows.

Il lettore e il protocollo sono verificati su salvataggi reali usati per le prove, compresi i giocatori con ID pubblico mancante e nomi Unicode. La verifica della scrittura controlla le modifiche ai campi e l'integrità dell'archivio, anche dopo modifiche successive a giocatori diversi. L'interfaccia Windows e il caricamento effettivo in Football Manager richiedono una prova sul computer Windows.



La verifica C# si esegue su Windows con `dotnet run --project tests/ComparisonChecks/ComparisonChecks.csproj --configuration Release -p:UseAppHost=false`. Il workflow **Validate comparison (DLL only)** esegue i controlli senza produrre EXE o ZIP; la verifica comprende anche l’ordine delle colonne, il cambio della posizione nel report e lo spazio della barra superiore.


