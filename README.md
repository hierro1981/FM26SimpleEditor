# FM26 Simple Editor · v14.4

[![Download totali](https://img.shields.io/github/downloads/hierro1981/FM26SimpleEditor/total.svg)](https://github.com/hierro1981/FM26SimpleEditor/releases)

[English](README.en.md)

Editor gratuito per **Football Manager 26** su Windows. Apri un salvataggio, cerca un giocatore e modifica attributi, abilità, posizioni e data di nascita. Puoi anche confrontare giocatori, seguirne i progressi e trovare i più adatti ai ruoli della tua squadra e delle sue tattiche.

## Scarica e avvia

Scarica l’ultima versione dalla pagina [Releases](https://github.com/hierro1981/FM26SimpleEditor/releases). Le novità della v14.4 qui descritte sono in sviluppo e saranno disponibili nel relativo rilascio.

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
- **Migliori per posizione:** seleziona posizione e sottoruolo per vedere fino a cinque candidati. Con **Ctrl/Shift+clic** sul campo puoi richiedere due posizioni; un secondo selettore permette anche due sottoruoli differenti nella stessa posizione.
- **Migliori per tattica:** scegli una tattica della prima squadra e uno slot nei campi affiancati. Valuta i candidati per la fase in possesso, non in possesso o entrambe, con i sottoruoli effettivi della tattica. Il filtro della rosa si applica anche a questi suggerimenti.
- **Staff:** consulta identità, contratti, abilità, personalità e preferenze leggibili. È una vista in sola lettura; incarichi e molti attributi professionali non sono ancora disponibili.

I suggerimenti richiedono dati completi e una familiarità sufficiente nelle posizioni richieste. Quando scegli entrambe le fasi, un giocatore deve poter ricoprire entrambe. Un ruolo non riconosciuto non produce una raccomandazione generica al suo posto.

### Seguire i progressi

La scheda **Storico** permette di visualizzare più attributi insieme, con preset per posizione, colori, dettagli al passaggio del mouse e variazioni tra i punti.

Quando disponibile, lo storico interno funziona con il solo salvataggio aperto. I mesi ricostruiti sono indicati come tali: non rappresentano date esatte decodificate. Puoi anche aggiungere fino a 20 salvataggi della stessa carriera per confrontare i valori alle rispettive date effettive. I cambiamenti non ancora salvati non entrano nello storico.

### Lingua e aggiornamenti

Scegli **Italiano** o **English** dal selettore della lingua. La scelta viene ricordata automaticamente. La finestra **Informazioni** mostra la versione dell’editor e consente di verificare la presenza di aggiornamenti con un collegamento alla release.

## Compatibilità e limiti

- Usa salvataggi locali funzionanti e conserva una copia originale. Il supporto dipende dalla versione e dal contenuto del salvataggio; una funzione non disponibile viene segnalata nell’interfaccia.
- La lettura automatica delle tattiche è stata verificata su FM26 **26.3.2**. In carriere con più allenatori umani viene considerato soltanto il primo riconosciuto.
- Stelle, punteggi e suggerimenti sono **stime dell’editor**: non riproducono la valutazione ufficiale dello staff del gioco e non prevedono il rendimento in partita. Usa **Criteri e fonti** per vedere su cosa si basa un suggerimento.
- La creazione di nuovi giocatori, i trasferimenti tra squadre, la modifica dello staff e delle tattiche e l’editing live non sono disponibili.

## Novità, dalla versione più recente

### v14.4 · in sviluppo

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
