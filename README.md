# FM26 Simple Editor · v15.7.4

[![Download totali](https://img.shields.io/github/downloads/hierro1981/FM26SimpleEditor/total.svg)](https://github.com/hierro1981/FM26SimpleEditor/releases)

[English](README.en.md)

🌐 **Pagina del progetto:** [italiano](https://hierro1981.github.io/FM26SimpleEditor/) · [English](https://hierro1981.github.io/FM26SimpleEditor/en/)

Editor gratuito per **Football Manager 26** su Windows. Apri un salvataggio, cerca un giocatore e modifica attributi, abilità, posizioni e data di nascita. Puoi anche confrontare giocatori, seguirne i progressi e trovare i più adatti ai ruoli della tua squadra e delle sue tattiche. Per lo staff, cerca candidati per incarico o allenamento e valuta i rinnovi dei contratti in scadenza.

## Scarica e avvia

Scarica l’ultima versione dalla pagina [Releases](https://github.com/hierro1981/FM26SimpleEditor/releases). La versione corrente è la **v15.7.4**.

- **Setup, quando presente nella release:** installazione guidata dell’editor, con opzioni per installare anche Python e il runtime .NET se necessari. Le installazioni aggiuntive richiedono il tuo consenso e una connessione Internet.
- **ZIP portatile:** estrai tutto il contenuto in una cartella e avvia **FM26SimpleEditor.exe**. Conserva insieme tutti i file del pacchetto.

Richiede **Windows x64**, **.NET 8 Desktop Runtime x64** e **Python 3.12 o successivo**. Per lo ZIP puoi installare il runtime dalla [pagina ufficiale .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0), scegliendo **.NET Desktop Runtime → Windows → x64**. Non servono Visual Studio o strumenti di sviluppo.

All’avvio l’editor controlla Python e le librerie necessarie: propone l’installazione di Python se manca e prepara automaticamente le librerie mancanti. In caso di problemi puoi riprovare dalla stessa finestra. Dopo la prima configurazione, Internet serve soltanto per installazioni, riparazioni e controllo degli aggiornamenti.

## Uso rapido

1. Apri una **copia di un salvataggio locale funzionante** con **Apri salvataggio**.
2. Cerca un giocatore e aprilo con un doppio clic.
3. Modifica i dati nella scheda **Modifica giocatore**; controlla il risultato in **Anteprima nel gioco**.
4. Premi **Salva modifiche** e scegli il nome della nuova copia `.fm`.
5. Carica la copia in Football Manager per verificare il risultato.

La nuova copia diventa il file di lavoro: le modifiche successive mantengono quelle già salvate. L’editor segnala i cambiamenti non salvati prima di cambiare giocatore o file.

## Cosa puoi fare

### Cercare e modificare giocatori

Cerca per nome, squadra, ID pubblico o ID interno. Il filtro delle squadre accetta anche una parte del nome, per esempio `tri`. Puoi combinare filtri per età, posizione e familiarità, CA, PA, contratto e scadenza, altezza, piede, prestito e margine di crescita.

I giocatori in prestito sono evidenziati; puoi mostrarli sotto la squadra in cui giocano oppure sotto quella proprietaria. Le colonne si possono ordinare e ridimensionare. La ricerca considera l’intero salvataggio e mostra fino a 2.500 risultati alla volta: restringi i filtri se ne trovi di più.

Puoi modificare attributi, familiarità nelle posizioni, abilità attuale e potenziale. La modifica della data di nascita si abilita quando il dato è leggibile senza ambiguità. I pulsanti copia permettono di copiare nome e identificativi.

Nell’editing gli attributi usano la scala originale **1–100**; nell’anteprima trovi la scala **1–20** del gioco. Barre e colori aiutano a leggere i valori e l’importanza degli attributi per la posizione scelta.

### Confrontare due giocatori

Seleziona due giocatori con **Ctrl+clic**, poi scegli il confronto rapido dal tasto destro. Puoi farlo nella ricerca, nella tua rosa e nelle classifiche dei migliori per posizione o tattica.

Puoi anche aggiungere giocatori all’elenco di confronto; ogni giocatore aperto vi entra automaticamente. Il confronto mostra attributi, differenze, posizioni e stelle stimate. Il **report per ruolo** descrive punti di forza, limiti e preferenza per la posizione selezionata.

### Gestire la tua squadra

**La mia squadra** individua il club del primo allenatore umano riconosciuto nel salvataggio. Mostra la prima squadra per impostazione iniziale; puoi scegliere le altre squadre disponibili oppure tutti i giocatori del club.

- **Rosa:** elenco ordinato inizialmente per posizione e abilità attuale.
- **Migliori per posizione:** seleziona posizione e sottoruolo per vedere un numero configurabile di candidati (10 predefiniti). Con **Ctrl/Shift+clic** sul campo puoi richiedere due posizioni; un secondo selettore permette anche due sottoruoli differenti nella stessa posizione.
- **Migliori per tattica:** scegli una tattica della prima squadra e uno slot nei campi affiancati. Valuta i candidati per la fase in possesso, non in possesso o entrambe, con i sottoruoli effettivi della tattica. Il filtro della rosa si applica anche a questi suggerimenti.
- **Staff:** consulta incarichi, identità, contratti, abilità, personalità, preferenze e 25 attributi professionali aggiuntivi su scala 1–20, dove leggibili. Puoi cercare anche per incarico. È una vista in sola lettura; «categoria» segnala quando non è ancora riconosciuta la qualifica specifica.

I suggerimenti richiedono dati completi e una familiarità sufficiente nelle posizioni richieste. Quando scegli entrambe le fasi, un giocatore deve poter ricoprire entrambe. Un ruolo non riconosciuto non produce una raccomandazione generica al suo posto.

### Trovare nuovo staff

In **La mia squadra → Trova staff**, scegli un incarico oppure una delle nove categorie di allenamento. La classifica mostra stelle di idoneità stimate, stelle assolute dalla CA, miglior categoria di allenamento e attributi principali. Seleziona un candidato per leggere i motivi dell’interesse previsto e tutte le stelle di allenamento, affiancati.

I candidati al limite e quelli con informazioni insufficienti restano inclusi; puoi mostrare anche i meno plausibili. Cerca per nome, club, incarico o ID, scegli quanti risultati vedere e, facoltativamente, simula uno stipendio settimanale. **Copia valutazione per feedback** prepara un riepilogo da accompagnare alla risposta ottenuta nel gioco.

L’interesse è una stima sperimentale, non una probabilità o una garanzia di accettazione. Lo stipendio ipotizzato non invia offerte al gioco. La ricerca copre lo staff leggibile collegato ai club, esclude il tuo staff e non include necessariamente tutti i disoccupati. Un contratto non letto non significa che la persona sia libera. Le stelle per incarico sono valutazioni del tool; per i medici non vengono inventate stelle di idoneità.

### Seguire i progressi

La scheda **Storico** permette di visualizzare più attributi insieme, con preset per posizione, colori, dettagli al passaggio del mouse e variazioni tra i punti.

Quando disponibile, lo storico interno funziona con il solo salvataggio aperto. I mesi ricostruiti sono indicati come tali: non rappresentano date esatte decodificate. Puoi anche aggiungere fino a 20 salvataggi della stessa carriera per confrontare i valori alle rispettive date effettive. I cambiamenti non ancora salvati non entrano nello storico.

### Lingua e aggiornamenti

Apri **Opzioni** per scegliere **Italiano** o **English** e il numero di giocatori consigliati per posizione o tattica (10 predefiniti). La scelta viene ricordata automaticamente. La finestra **Informazioni** mostra la versione dell’editor e consente di verificare la presenza di aggiornamenti con un collegamento alla release.

## Compatibilità e limiti

- Usa salvataggi locali funzionanti e conserva una copia originale. Il supporto dipende dalla versione e dal contenuto del salvataggio; una funzione non disponibile viene segnalata nell’interfaccia.
- La lettura automatica delle tattiche è stata verificata su FM26 **26.3.2**. In carriere con più allenatori umani viene considerato soltanto il primo riconosciuto.
- Stelle, punteggi e suggerimenti sono **stime dell’editor**: non riproducono la valutazione ufficiale dello staff del gioco e non prevedono il rendimento in partita. Usa **Criteri e fonti** per vedere su cosa si basa un suggerimento.
- La creazione di nuovi giocatori, i trasferimenti tra squadre, la modifica dello staff e delle tattiche e l’editing live non sono disponibili.

## Novità, dalla versione più recente

### v15.7.4

Questa release riunisce ricerca e pianificazione dello staff, filtri per le giovanili, riconoscimento delle sottosquadre e nuovi scenari per la Top 11 futura.

- **Analisti:** la pianificazione valuta anche la categoria riconosciuta, mostrando punteggi, stelle e alternative quando gli attributi sono completi.
- Per i rinnovi confronta soltanto candidati con la stessa variante contrattuale. Il passaggio a **Trova staff** conserva quel filtro; nella ricerca puoi anche scegliere tutte le varianti.
- La specializzazione esatta rimane esplicitamente non identificata. Se mancano attributi necessari, il motivo indica quali.

### v15.7.3

- **Pianificazione staff:** nel confronto per le sottosquadre, **Lavoro con i giovani** compare in prima riga con valore dello staff attuale, dell’alternativa e differenza colorata. Il punteggio e le stelle mantengono il criterio selezionato.

### v15.7.2

- **Pianificazione staff:** colonne più compatte e adattate al contenuto, intestazioni su più righe e **Criterio di confronto** spostato in fondo.
- Doppio clic su un rinnovo per aprire **Trova staff** con gli stessi criteri del pulsante dedicato.

### v15.7.1

- Ripristinate le etichette **U20, U18** e degli altri gruppi riconoscibili nel selettore della rosa e nelle viste staff. I gruppi dal tipo non ancora identificato restano indicati come **Altra squadra**, con il codice nel tooltip.
- Il riconoscimento controlla i record squadra e confronta più club nello stesso salvataggio: non assegna l’età in base all’ordine degli slot e può adattarsi a codici di tipo diversi quando i riscontri concordano.
- I casi non verificabili mantengono l’indicazione generica. Le alternative della pianificazione tengono conto della destinazione giovanile quando è identificata.

### v15.7

- **Trova staff:** scegli la squadra di destinazione, per esempio Allenatore + U20, Allenatore in seconda + U20 o Preparatore + U18.
- Per allenatori e preparatori delle giovanili, filtro regolabile su **Lavoro con i giovani** (predefinito almeno 12/20), visibile anche nell’elenco. Le stelle di allenamento mantengono lo stesso calcolo.
- Filtro separato sulla **squadra attuale**, dove identificabile: prima squadra, sottosquadre, giovanili, gruppi U e riserve/B.
- Il passaggio dalla prima squadra a una sottosquadra non viene considerato automaticamente un avanzamento: stima dell’interesse al massimo al limite.
- Ripristinato il recupero dello slot letto dal contratto quando non si trova la squadra nel catalogo dei club. I casi con dati contraddittori restano non identificati.

### v15.6.3

- Aggiunta la **Squadra incarico** in Staff, Trova staff e Pianificazione staff, comprese le alternative proposte e le valutazioni copiate.
- Mostra prima squadra, sottosquadra o squadra affiliata in base al contratto. I nomi vengono mostrati quando leggibili; senza un nome verificato, gli altri gruppi restano indicati tramite slot e ID, senza attribuire arbitrariamente etichette U20/U18.
- La ricerca testuale dello staff comprende anche la squadra; il tooltip riporta il riferimento del contratto.

### v15.6.2

- **Pianificazione staff:** rimossa la colonna club dall’elenco dei rinnovi e aggiunte le stelle accanto al nome dell’alternativa proposta.
- Il pulsante **Trova staff per questo criterio** apre la ricerca con lo stesso incarico o categoria di allenamento, filtro sull’incarico attuale e offerta salariale della pianificazione. L’interesse rimane attivo e la ricerca testuale precedente viene azzerata.

### v15.6.1

- Le **Stelle incarico attuale** sostituiscono le precedenti stelle complessive basate sulla CA: uno specialista viene valutato per il lavoro che svolge.
- Per i preparatori usa la categoria di allenamento pertinente; per gli altri incarichi gli attributi specifici del ruolo. Il tooltip indica il criterio e i dati insufficienti rimangono non valutati.
- Calcolo condiviso tra elenco staff, ricerca e criterio predefinito della pianificazione. La CA numerica rimane visibile.

### v15.6

- Nuova scheda **La mia squadra → Pianificazione staff**: mostra i contratti in scadenza entro sei mesi e suggerisce se rinnovare o valutare una sostituzione.
- Confronta il tuo staff con alternative migliori per lo stesso incarico o categoria di allenamento, indicando stelle, attributi e interesse stimato. Puoi cambiare il criterio di confronto e ipotizzare un’offerta salariale.
- Le alternative al limite restano da verificare: prima di rinunciare a un rinnovo, controlla che il sostituto sia disponibile.
- Stima dell’interesse più prudente per chi ha ancora un contratto con un club più prestigioso: scadenza vicina e aumento salariale ipotizzato non bastano a superare il rischio di rifiuto.
- Rimossa la nota generica finale dalle valutazioni dello staff.

### v15.5.1

- Nella ricerca e negli elenchi dello staff, **Preparatore recupero infortunati** e **Responsabile recupero infortunati** sostituiscono le precedenti etichette relative alle scienze motorie, per riconoscere più facilmente gli incarichi del gioco.

### v15.5

- Nella **Top 11** puoi passare dal rendimento attuale al potenziale futuro con lo switch **Futura**. Anche lo scenario futuro cerca la combinazione migliore per tutta la tattica, usando ogni giocatore una sola volta.
- Più spazio per gli elenchi in **Migliori per tattica**: tabella futura più larga, colonna familiarità compatta e spiegazioni spostate in **Criteri e fonti**, con una scheda dedicata.
- La Top 11 futura usa la stessa stima sperimentale della classifica: confronta picchi individuali che possono arrivare in stagioni diverse. I dati mancanti non vengono sostituiti con valori inventati.

### v15.4.2

- Corretta la proiezione futura per tattica: la CA bassa non amplifica più artificialmente tutti gli attributi. La crescita viene aggiunta in base ai punti di CA guadagnabili, mantenendo la stima per età e posizione.
- Ridotte le valutazioni future eccessive; alcuni tratti, come fantasia e determinazione, non aumentano automaticamente per effetto della PA.
- Chiarita la differenza tra punteggio del sottoruolo selezionato e percentuale della migliore posizione di FM Genie Scout. Intestazioni della tabella futura su più righe per distinguere CA stimata e scenari.

### v15.4.1

- La classifica futura in **Migliori per tattica** tiene ora conto dell’età e distingue portieri e giocatori di movimento, invece di ipotizzare il raggiungimento automatico della PA.
- Sono mostrate **CA futura stimata**, crescita residua e scenari prudente/favorevole. Punteggi e stelle futuri usano la nuova stima; la PA resta visibile come tetto teorico.
- È una stima sperimentale che assume allenamento e minuti adeguati, non una promessa di sviluppo. Età o abilità mancanti non vengono inventate.

### v15.4

- In **Migliori per tattica**, alla classifica attuale è affiancata quella del potenziale stimato per lo slot e la fase selezionati, con gli stessi filtri di rosa e prestiti.
- Punteggio e stelle futuri rappresentano uno scenario a PA raggiunta; sono visibili anche punteggio attuale, età e margine PA−CA. La PA sconosciuta non viene inventata.
- Apertura e confronto rapido disponibili in entrambe le tabelle, sempre sui dati reali attuali. Il campo Top 11 resta basato sul rendimento odierno.

### v15.3

- Lettura della reputazione personale dello staff: nazionale, attuale e mondiale, distinta dalla reputazione del club e dalle stelle dalla CA.
- Cinque predisposizioni d’incarico salvate: allenatore, vice, preparatore, fisioterapista e osservatore. Visibili nei dettagli, nelle colonne della ricerca e nel feedback copiato; non garantiscono disponibilità a un’offerta.
- Otto preferenze tattiche aggiuntive, tra cui marcatura, fuorigioco e utilizzo delle sostituzioni. Gli incarichi ancora da mappare e le formule delle percentuali di valutazione restano indicati come non disponibili.

### v15.2.1

- Segnalati i passaggi laterali senza un miglioramento reputazionale netto; uno stipendio ipotizzato più alto non basta a prevedere disponibilità alla trattativa.
- Avviso sui cambi di paese basato sul club attuale. Un’adattabilità alta non implica volontà di trasferirsi e la sola nazionalità diversa non comporta più una penalità.

### v15.2

- Filtro per incarico attuale con **Qualsiasi**, incarichi riconosciuti e **Non identificato**; combinabile con ruolo o categoria di allenamento cercati.
- Avviso più chiaro per i candidati al limite con maggior rischio di rifiuto, che restano visibili per raccogliere feedback.
- Riepilogo copiato arricchito con incarico, contratto e stipendio attuali; punteggi più leggibili.

### v15.1

- Colonna reputazione del club attuale e icona copia accanto al nome di ogni candidato; stelle complessive e miglior allenamento spostati in coda.

- **Ignora interesse (migliori in assoluto)** disattiva il filtro dell’interesse e ordina lo staff per qualità, includendo chi probabilmente rifiuterebbe. La stima dell’interesse resta visibile.

### v15.0

- Nuova sezione **Trova staff**, per incarico o categoria di allenamento, con classifiche, stelle stimate e attributi principali.
- Interesse stimato verso il club umano con motivazioni, casi al limite inclusi e scenario salariale facoltativo.
- Ricerca testuale, colonne ordinabili, dettagli affiancati e copia della valutazione per raccogliere feedback dal gioco.

### v14.6

- **Staff più leggibile:** attributi e stelle di allenamento sempre affiancati, con scrollbar indipendenti. Nell’elenco sono presenti anche le stelle complessive stimate dalla CA, ordinabili.

- **Stelle allenamento (stima):** attributi e nove categorie di allenamento sono sempre affiancati, con stelle, indice e attributi usati. Sono ora leggibili anche Determinazione, Autorità/Disciplina, Gestione del personale, Lavoro con i giovani e Calci piazzati. Pesi di Possesso e Calci piazzati affinati con i confronti disponibili; le stelle restano indicative e possono differire dal gioco.

- **Top 11 per tattica:** un terzo campo mostra la formazione con la migliore somma dei punteggi per sottoruolo, usando ciascun giocatore una sola volta. Rispetta i filtri della rosa e la fase selezionata; segnala gli slot che non è possibile coprire.

- Nel tab **Staff** sono disponibili 25 attributi aggiuntivi su scala 1–20, tra cui allenamento, valutazione di abilità e potenziale, motivazione, fisioterapia e scienze motorie.
- Gli incarichi dello staff vengono letti dal contratto e sono ricercabili e ordinabili. Sono riconosciuti anche preparatori dei portieri, allenatori in seconda, direttori tecnici e preparatori dei calci piazzati; gli analisti mostrano la categoria generale quando la specializzazione non è ancora decodificata.
- Le informazioni dello staff restano in sola lettura; i campi non riconosciuti non vengono mostrati.

### v14.5

- Finestra **Opzioni** per scegliere lingua e numero di giocatori consigliati (10 predefiniti); le preferenze vengono ricordate.
- Possibilità di includere i giocatori dati in prestito nella propria rosa e nei consigli per posizione o tattica.
- Disposizione tattica più fedele alle fasce, sottoruoli più compatti e linguette leggibili anche nelle finestre strette.

### v14.4

- Migliorata la visualizzazione delle tattiche nelle finestre piccole, con scorrimento e meno testo fisso; schede più riconoscibili grazie ai colori.
- README riorganizzati attorno a installazione, utilizzo e funzioni dell’editor; dettagli di configurazione e sviluppo nella documentazione dedicata.
- Migliorato il riconoscimento dei club che giocano in campionati di un’altra nazione e dei relativi collegamenti alle squadre affiliate.

### v14.3

- Confronto rapido di due giocatori selezionati dagli elenchi.
- Nuova scheda **Migliori per tattica**, con campi in possesso/non in possesso e cinque candidati per slot.
- Nomi italiani dei 79 sottoruoli allineati al gioco, compresi **AdM = Attaccante di movimento** e **Amn = Attaccante di manovra**.

### v14.1

- Staff della propria squadra consultabile in una scheda dedicata.
- Migliorata la scelta delle sotto-squadre e l’ordinamento della rosa.
- Cinque candidati per posizione, con stelle e PA; scelta di due posizioni e dei relativi sottoruoli.

### v14

- Nuova area **La mia squadra**, con rosa e suggerimenti per posizione.

### v13.1.1

- Indicatore dei download totali e novità ordinate dalla più recente.

### v13.1

- Tooltip dello storico più leggibili, con attributo, valore e data nei colori della serie.

### v13

- Interfaccia in italiano e inglese, con scelta della lingua ricordata tra gli avvii.
- Finestra Informazioni e controllo degli aggiornamenti.
- Copia di nome e identificativi del giocatore; preset e dettagli interattivi nello storico.

### v12

- Confronto più leggibile, frecce nelle differenze e report per posizione con commento.
- Sigle dei giocatori multiposizione e migliore adattamento dei filtri alla finestra.
- Pacchetto Windows più leggero; richiede il runtime .NET installato.

### v11

- Evidenziazione dei prestiti e scelta della squadra di riferimento.
- Pulsante **Salva modifiche** e avviso dei cambiamenti non salvati vicino ai comandi del file.

### v10

- Controllo e preparazione guidata di Python e delle librerie all’avvio.

### v9

- Avanzamento dettagliato durante apertura e salvataggio.
- Ricerca testuale nel menu del secondo giocatore da confrontare.

### v8

- Storico degli attributi, con più curve e confronto tra salvataggi.

### v7

- Miglioramenti a ricerca, ordinamento, anteprima, confronto e riferimenti delle stelle.

## Problemi e contributi

Segnala i problemi nelle [Issues](https://github.com/hierro1981/FM26SimpleEditor/issues), indicando versione dell’editor, versione di FM e messaggio visualizzato.

Il progetto C# Windows Forms è compatibile con **Visual Studio 2026**. Per compilazione, configurazione, test e dipendenze consulta la [guida allo sviluppo](docs/Development.md). Metodo e limiti dei suggerimenti sono descritti nella [documentazione dei ruoli](docs/TacticalRoles.md); la lettura delle tattiche in [SavedTactics](docs/SavedTactics.md).

## Licenza

Distribuito con licenza [GNU GPL v3](LICENSE). Football Manager è un marchio di Sports Interactive e SEGA; questo progetto è indipendente e non ufficiale.
