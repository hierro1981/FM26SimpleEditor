# FM26 Simple Editor · v11 (in sviluppo)

Italiano | [English](README.en.md)

FM26 Simple Editor è un editor locale e non ufficiale per i salvataggi di **Football Manager 26**. Permette di cercare i giocatori per nome, squadra o ID, modificarne attributi, abilità, posizioni e data di nascita quando disponibile, e confrontarli tra loro.

Include un’anteprima dei valori sulla scala 1–20 del gioco e grafici per analizzare l’evoluzione degli attributi. Le modifiche vengono salvate in una **nuova copia verificata** del file `.fm`, preservando il salvataggio originale.

## Scarica l’eseguibile Windows

La compilazione automatica **Build Windows executable** genera `FM26SimpleEditor-win-x64.zip` a ogni push su `main`, su avvio manuale da `main` e per le release pubblicate. Nei branch di sviluppo e nelle pull request non genera EXE o ZIP; anche l’avvio manuale su un branch diverso da `main` viene saltato. Apri [Actions](https://github.com/hierro1981/FM26SimpleEditor/actions/workflows/build-windows.yml), seleziona una compilazione riuscita e scarica **FM26SimpleEditor-win-x64** da **Artifacts** (richiede l’accesso a GitHub; conservazione 30 giorni). Per avviare una compilazione manuale usa **Run workflow**.

Quando viene pubblicata una nuova release basata su un commit che contiene questo workflow, lo ZIP viene allegato automaticamente alla [release](https://github.com/hierro1981/FM26SimpleEditor/releases). Le release già pubblicate non vengono ricompilate né aggiornate.

Estrai lo ZIP completo e avvia **FM26SimpleEditor.exe**, mantenendo gli script e `requirements.txt` nella stessa cartella. Il pacchetto è per **Windows x64**, include il runtime .NET e non richiede Visual Studio o l’SDK. Python e le dipendenze vengono verificati e predisposti all’avvio come descritto sotto. Sono inclusi anche licenza, documentazione e sorgenti del commit compilato.

## Progetto e requisiti tecnici

Progetto C# Windows Forms per Visual Studio 2026, con lettore locale Python. Per compilare richiede Windows, SDK .NET 8 e carico di lavoro **Sviluppo di applicazioni desktop .NET**. Il lettore usa Python 3.12 o successivo e le dipendenze in `requirements.txt`, verificate e predisposte all’avvio.

## Novità precedenti della versione 7

- Ricerca su due righe: nome/ID sopra, filtro squadre ed elenco sotto. I filtri opzionali hanno un'area di altezza limitata, con scorrimento interno e possibilità di nasconderli. La tabella conserva lo spazio disponibile e la scrollbar orizzontale.
- Posizioni in ordine: GK, SW, DL, DC, DR, WBL, DM, WBR, ML, MC, MR, AML, AMC, AMR, STC. Questo ordine si applica alla familiarità, al filtro ruoli, alla colonna ruolo principale e ai candidati del confronto. Se tutte le familiarità sono 1 o meno, il ruolo è sconosciuto e va in fondo all'ordinamento.
- Etichette degli attributi colorate: viola fondamentale, blu importante, verde utile, sfondo normale per gli altri. Selettore della posizione di riferimento, inizialmente automatico. È una guida editoriale per posizione, non un peso ufficiale FM né una valutazione per compito tattico. I colori delle barre continuano a indicare la qualità del valore.
- Stelle CA e PA su tre riferimenti: squadra, assoluto e campionato disponibile. La scala assoluta è `abilità / 40`, arrotondata a mezza stella e limitata a 0,5–5; zero/PA sconosciuta danno N/D. Il confronto relativo mantiene la formula `2,5 + (abilità − CA media) / 20`, arrotondata a mezza stella e limitata a 0,5–5. Le medie usano i valori originali del file, così restano stabili mentre modifichi il giocatore.
- Il campionato è ricavato da un gruppo di classifica leggibile contenente l'esatto team ID: regola conservativa di divisione con 18–26 squadre e turni per sede pari a squadre meno uno. Si richiede un unico insieme di squadre e almeno 3 giocatori con CA nota. La media riguarda i giocatori assegnati agli esatti team ID di quella classifica. Campionati piccoli, gruppi ambigui o non decodificati restano N/D. Il nome può mancare: in tal caso appare l'ID o “Divisione senza nome”. L'associazione alla competizione è dedotta dal lettore, non garantita dal gioco.
- Campi con proporzioni verticali costanti, punti scalabili e centrati; altezza adattata nel confronto. Colonne del confronto distribuite su tutta la larghezza, con minimi e scorrimento quando necessario. Colonna “Migliore” con A/B e nome; CA e PA indicano anche il vincitore nella riga informativa. Per infortuni e scorrettezza vince il valore più basso.

## Novità precedenti della versione 8

Storico interno da un singolo salvataggio supportato, grafico con selezione multipla degli attributi, tabella con valori e Δ per ogni campo, ricerca e selezione rapida degli attributi, punto attuale opzionale. Vedi la sezione Storico per copertura e date.

## Novità precedenti della versione 9

**Apertura e salvataggio:** barra con percentuale, descrizione della fase e contatori disponibili. Il catalogo mostra i giocatori elaborati; compressione e scrittura mostrano i MiB; la verifica mostra le sezioni completate. La percentuale complessiva usa intervalli pesati per fase e non è una stima del tempo restante. Può rimanere ferma mentre una fase indivisibile del lettore lavora, ad esempio la decodifica dei giocatori. Non usa un timer per simulare avanzamento. Il 100% viene mostrato solo dopo il successo dei controlli e la preparazione dei dati nell'interfaccia. Un errore non viene presentato come completamento. Le altre brevi letture prive di misure restano indicate come attività in corso.

**Confronto:** aprendo la selezione del secondo giocatore compare un campo di ricerca dentro il menu. Digita una parte del nome, squadra, ID pubblico o interno; i risultati rispettano l'ordine per posizione. Il filtro esterno esistente rimane combinabile con quello del menu. Clic o Invio confermano, Esc o un clic fuori annullano senza cambiare il giocatore selezionato. Puoi aprire il menu con clic, F4 o freccia giù e usare le frecce per muoverti. Quando non ci sono corrispondenze, viene mostrato un messaggio e non è possibile confermare una voce inesistente. La ricerca nel menu non carica un giocatore a ogni carattere.

## Novità precedenti della versione 10

- **Controllo automatico dell’ambiente all’avvio:** ricerca Python 3.12 o successivo, verifica le versioni delle dipendenze e controlla che le librerie siano importabili.
- **Installazione guidata di Python:** se manca un interprete compatibile, propone Python 3.13 tramite WinGet oppure il sito ufficiale.
- **Dipendenze predisposte automaticamente:** se mancanti o non utilizzabili, vengono installate o riparate in un ambiente dedicato all’editor. Sono disponibili dettagli, annullamento e **Riprova**.
- **Distribuzione Windows pronta all’uso:** GitHub Actions compila e verifica lo ZIP Windows x64 con runtime .NET incluso e lo allega alle nuove release. Il pacchetto include anche i sorgenti corrispondenti.
- **Documentazione bilingue:** README in italiano e inglese, con istruzioni per il pacchetto compilato e per l’avvio dai sorgenti.

## Novità della versione 11 — branch di sviluppo

- **Prestiti riconoscibili nell’elenco:** righe evidenziate in ocra, anche quando selezionate, con legenda e tooltip che indica squadra attuale e proprietaria. I prestiti con stato non disponibile non vengono colorati come confermati.
- **Squadra di riferimento dei prestiti:** il selettore **Prestiti nella squadra** usa **In cui gioca** come impostazione iniziale; scegli **Proprietaria** per vedere i prestiti tra i giocatori del club proprietario. Colonna Squadra, elenco delle squadre, ricerca per testo e ordinamento usano tutti il riferimento scelto. Se la proprietaria non è leggibile, viene indicata come non disponibile. Il selettore resta visibile anche nascondendo i filtri opzionali; **Azzera filtri** conserva questa preferenza di visualizzazione per la sessione. Anteprima, confronto e stelle restano riferiti alla squadra in cui il calciatore gioca.
- **Salvataggio sempre raggiungibile:** **Salva modifiche** è nella barra superiore, vicino ad **Apri salvataggio**, insieme all’indicazione delle modifiche non salvate. È accessibile da tutte le schede e si abilita quando ci sono modifiche, anche durante l’editing di una cella. I pannelli della barra e della ricerca si ridimensionano al contenuto, riducendo gli spazi vuoti sopra le schede e i filtri. Continua a creare una nuova copia verificata del salvataggio.

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

Le colonne numeriche sono allineate a destra. I risultati hanno scrollbar orizzontale e verticale quando servono; le colonne hanno larghezze ridimensionabili e tutte le colonne scorrono orizzontalmente. Cliccando un'intestazione si ordina in modo crescente, cliccandola di nuovo in modo decrescente. L'ordinamento è applicato a **tutti** i risultati, prima del limite di 2.500 righe: età, CA, PA e ID sono ordinati numericamente, le date cronologicamente. I valori mancanti restano in fondo in entrambe le direzioni. Il ruolo principale usa la familiarità più alta; a parità prevale la prima posizione nello schema del campo. La sua colonna si ordina da portiere a punta, con sinistra–centro–destra per linea.

**Mostra filtri opzionali** nasconde o mostra i controlli mantenendo i criteri attivi. Il contatore resta visibile. **Azzera filtri** cancella testo, scelta della squadra e tutti i filtri opzionali. Le ricerche non modificano il salvataggio.

**Modifica giocatore** usa tutto lo spazio della scheda per gli attributi grezzi e la familiarità nelle posizioni. Il selettore **Etichette in italiano** alterna italiano e inglese e applica l'ordine alfabetico della lingua scelta agli attributi; le posizioni seguono lo schema del campo. L'identità del campo nel file non dipende dall'etichetta né dalla sua posizione in tabella. Le traduzioni sono quelle dell'editor e possono differire da alcune diciture della localizzazione ufficiale del gioco.

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

## Storico degli attributi — versione 8

La quinta scheda apre automaticamente **Storico interno · sperimentale** quando riconosce una sequenza nel file aperto. Non è necessario caricare altri salvataggi. Il lettore è limitato alla build 26.3.2 con schema game_db 4000, verificata sui salvataggi di prova: richiede almeno due campioni, struttura ripetuta coerente e un solo candidato. Dati assenti, formati diversi e sequenze ambigue rimangono non disponibili; in questi casi viene proposta la modalità dei salvataggi datati con il motivo del mancato riconoscimento.

Sono stati identificati **47 attributi visibili**, in campioni da 69 byte. Non vengono attribuite CA/PA storiche, valori dei piedi o attributi nascosti ai campioni che non li contengono. La presenza e la copertura dello storico possono variare tra giocatori. Il lettore è sperimentale e non è una garanzia di supporto per altri formati.

**Asse orizzontale dello storico interno:** Campione 1, 2, 3… Le date individuali non sono state decodificate. La cadenza appare mensile nei salvataggi confrontati, ma l'editor non inventa mesi o giorni. Le linee collegano osservazioni registrate e non indicano valori misurati tra un campione e l'altro. La sequenza conserva i valori interni reali senza interpolarli.

A sinistra trovi una **lista cercabile con caselle di selezione**. Puoi mostrare più attributi contemporaneamente; inizialmente sono selezionati dribbling, passaggi e tecnica. **Seleziona visibili** aggiunge tutti i risultati del filtro; **Nessuno** svuota l'intera selezione. Il filtro nasconde solo le voci della lista, mantenendo gli attributi già selezionati. Cambiare lingua conserva la selezione per identità del campo.

Ogni attributo ha colore e tratteggio riconoscibili nella legenda scorrevole, una curva e una colonna nella tabella. Passa sui punti per leggere valori, campione/data e file; punti sovrapposti mostrano insieme le informazioni disponibili. **Scala grezza** alterna i valori originali e la conversione 1–20. **Mostra Δ** aggiunge la variazione rispetto alla precedente osservazione disponibile per ogni attributo. Non c'è un limite arbitrario al numero di attributi selezionabili; per grafici più leggibili conviene confrontare pochi attributi alla volta.

**Includi valori attuali del file** aggiunge un punto e una riga evidenziata “Attuali”, separata concettualmente dai campioni interni. È un confronto con lo stato del file, non un ulteriore mese storico. È disattivabile. Le modifiche non ancora salvate sono escluse. Le colonne scorrono orizzontalmente e il separatore tra grafico e tabella è trascinabile.

Resta disponibile **Confronta salvataggi datati**: **Aggiungi salvataggi…** seleziona fino a 20 altri `.fm` della stessa carriera e passa a questa modalità. Ogni punto usa gli attributi e la data effettiva della partita nel relativo file, con CA/PA se disponibili. Anche qui puoi selezionare più attributi. Le due modalità mantengono assi distinti: i campioni senza data non vengono mescolati con le osservazioni datate. Versioni della stessa data restano punti distinti, senza linee o Δ che suggeriscano crescita nel tempo. Le note di corrispondenza dell'identità sono visibili passando sul nome del file; i file esclusi sono indicati nel messaggio.

Il confronto tra file verifica ID, nome e nascita; una nascita modificata richiede ID pubblico e interno coerenti ed è segnalata. Senza ID pubblico resta richiesta la nascita coerente. Le letture non modificano i salvataggi e salvare un giocatore non riscrive i suoi campioni storici.

## Salvataggio

**Salva modifiche** crea un nuovo `.fm`. La copia verificata diventa il file di lavoro: le modifiche a un secondo giocatore mantengono quelle già salvate al primo. L'originale non può essere scelto come destinazione. Il programma segnala le modifiche non salvate prima di cambiare giocatore o file.

Sono incluse le correzioni precedenti: JSON Unicode, ID pubblico facoltativo, verifica della data e ricostruzione di tutte le voci dell'indice, comprese quelle annidate. La scrittura verifica tutte le sezioni compresse prima e dopo la modifica. Un file con indice già incoerente viene rifiutato: usa un salvataggio originale funzionante.

## Verifica

Dopo aver installato le dipendenze, esegui `python -m unittest discover -s tests -v` per i test della ricerca dei prestiti (squadra attuale/proprietaria, stato ignoto, filtri combinati, ordinamento e limite dei risultati) e del controllo iniziale Python: interprete troppo vecchio, pacchetti assenti o con versione diversa, import non funzionanti, pip assente e requisiti non validi. La finestra Windows, il rilevamento delle installazioni e l’installazione con WinGet richiedono una prova su Windows.

Il lettore e il protocollo sono verificati su salvataggi reali usati per le prove, compresi i giocatori con ID pubblico mancante e nomi Unicode. La verifica della scrittura controlla le modifiche ai campi e l'integrità dell'archivio, anche dopo modifiche successive a giocatori diversi. L'interfaccia Windows e il caricamento effettivo in Football Manager richiedono una prova sul computer Windows.

