# Sviluppo e configurazione / Development and configuration

## Italiano

### Progetto e compilazione

Applicazione C# Windows Forms, .NET 8, compatibile con Visual Studio 2026 su Windows con il carico di lavoro **Sviluppo di applicazioni desktop .NET**. Il backend locale Python richiede Python 3.12 o successivo. Aprire `FM26SimpleEditor.csproj`, compilare e avviare. Gli script e i dati necessari vengono copiati automaticamente nell’output.

Le versioni delle dipendenze sono fissate in `requirements.txt`; `fmsave 0.5.11` mantiene il parser riproducibile, mentre la modifica del salvataggio è gestita dal codice dell’editor. Il controllo all’avvio installa o ripara l’ambiente dedicato quando le versioni non corrispondono. Aggiornare la libreria richiede verificare anche gli usi dei suoi moduli interni.

```sh
python -m pip install -r requirements.txt
python -m unittest discover -s tests -v
```

Su Windows, per le verifiche C#/UI:

```sh
dotnet run --project tests/ComparisonChecks/ComparisonChecks.csproj --configuration Release -p:UseAppHost=false
```

Il workflow `validate.yml` esegue i test Python e C#/UI senza creare gli eseguibili dell’app o ZIP. La pubblicazione di pacchetti e setup avviene solo nel flusso delle release, non nei branch di sviluppo. Il pacchetto distribuito è framework-dependent e richiede .NET 8 Desktop Runtime x64.

### Lingua e configurazione locale

`FM26SimpleEditor.config.json` si trova accanto all’eseguibile. Esempi:

```json
{"language":"it"}
```

```json
{"language":"en"}
```

La configurazione viene letta prima della preparazione di Python. File assente, non valido o lingua non supportata: italiano. La scrittura è atomica; se la cartella non è scrivibile, l’interfaccia avvisa e mantiene la scelta nella sessione.

Le risorse sono `Resources/Strings.resx` e `Resources/Strings.en.resx`, compilate nell’app e nella DLL satellite inglese. Per cambiare le traduzioni occorre ricompilare. Nomi, percorsi e messaggi esterni mantengono il testo originale. Il dataset dei sottoruoli contiene le etichette italiane/inglesi del gioco; i pesi restano quelli dell’editor.

### Preparazione dell’ambiente

Il runtime viene cercato negli ambienti dell’editor, nel registro Windows, nelle installazioni locali e tramite il launcher `py`; non è necessario che Python sia nel PATH. `FM26_PYTHON` può indicare un interprete preferito tramite percorso completo. Se non è compatibile, la ricerca prosegue.

Gli ambienti sono sotto `%LOCALAPPDATA%\FM26SimpleEditor\runtimes\python-X.Y`. Le librerie globali delle altre applicazioni non vengono modificate. Sono disponibili l’installazione manuale dai requisiti e `Installa_dipendenze.cmd`.

La finestra di avvio propone Python 3.13 tramite WinGet o sito ufficiale; l’installazione richiede un’azione dell’utente. Il setup usa invece il download diretto di Python 3.13.16 x64 da python.org, verifica la firma e prepara lo stesso ambiente. Le opzioni dei prerequisiti del setup sono inizialmente deselezionate. Il log del setup è `%LOCALAPPDATA%\FM26SimpleEditor\installer-prerequisites.log`.

### Scale, suggerimenti e storico

Gli attributi modificabili conservano la scala originale 1–100, inclusi valori potenziati fino a 125. La visualizzazione 1–20 usa `clamp((raw + 2) / 5, 1, 20)` con divisione intera. CA/PA hanno scala nominale 0–200, con supporto ai valori già modificati fino a 255; i codici negativi della PA sono conservati.

Le formule dei suggerimenti sono in [TacticalRoles.md](TacticalRoles.md), quelle di decodifica delle tattiche in [SavedTactics.md](SavedTactics.md). Le stelle assolute di abilità usano `abilità / 40`; quelle relative usano `2,5 + (abilità − CA media) / 20`, con arrotondamento a mezza stella e limiti 0,5–5. Non sono formule ufficiali FM.

Lo storico interno è sperimentale. I campioni non contengono date individuali decodificate: la proiezione mensile, quando applicabile, li assegna ai mesi completi precedenti al salvataggio. I valori provengono dal file senza interpolazione. Nel confronto di salvataggi distinti vengono usate le date effettive; osservazioni della stessa data non suggeriscono una progressione temporale.

### Stato della ricerca su selezione e staff

Le definizioni UI e gli enum hanno consentito di leggere i sottoruoli effettivi nelle due fasi. La tabella OOP comprende 39 codici di ruolo e 13 ID di attributi; i codici di ruolo sono collegati agli enum, ma gli ID di attributi non sono ancora identificati con certezza. I pesi trovati per selezione squadra, voti partita e allenamento appartengono a contesti diversi: non sono la formula delle stelle di idoneità. Non vanno importati come pesi ufficiali dei suggerimenti.

L’analisi nativa ha collegato il blocco di 52 byte dello staff al suo serializzatore e al getter per indice. L’estensione dell’editor legge 20 attributi aggiuntivi su scala 1–20, oltre a personalità, adattabilità, preferenze, identità e contratti già disponibili tramite `fmsave 0.5.11`. Mapping, verifiche e limiti sono in [StaffAttributes.md](StaffAttributes.md). Il campo grezzo `r4` non identifica l’incarico: la documentazione upstream riporta più incarichi sotto lo stesso codice. Gli indici delle liste staff non vengono promossi a reparti certi sulla base di un solo campione.

Per estendere la lettura servono corrispondenze verificabili tra campi memorizzati e valori mostrati dal gioco, tramite analisi delle funzioni native o confronti controllati. File originali del gioco e salvataggi non vengono distribuiti nel progetto.

Fonti: [release fmsave 0.5.11](https://github.com/rhiever/fmsave/releases/tag/v0.5.11), [modello Staff](https://github.com/rhiever/fmsave/blob/v0.5.11/src/fmsave/models/staff.py), [modelli e limiti delle informazioni](https://github.com/rhiever/fmsave/blob/v0.5.11/docs/guides/what-a-save-holds.md).

## English

Build `FM26SimpleEditor.csproj` on Windows with .NET 8 and the Windows Forms desktop workload in Visual Studio 2026. The local backend requires Python 3.12+. Install pinned dependencies from `requirements.txt`, run Python tests with `python -m unittest discover -s tests -v`, and run Windows UI tests with the `dotnet run` command above. Development branches validate DLLs and do not generate application EXE/ZIP/setup packages.

Language preference lives in `FM26SimpleEditor.config.json` beside the executable, with `{"language":"it"}` or `{"language":"en"}`. Invalid or missing settings default to Italian. Writes are atomic; an unwritable folder leaves the session preference active and displays a warning. Translations are compiled from the base Italian resource and the English satellite resource. Distribute the complete output folder, including `en`.

`FM26_PYTHON` selects a preferred interpreter; registry, local installs, the Python launcher and editor environments are also searched. Environments live under `%LOCALAPPDATA%\FM26SimpleEditor\runtimes\python-X.Y`. Startup can offer Python installation via WinGet or the official website; the release setup uses a signed direct download. Installation and repair use the pinned requirements rather than altering unrelated global packages.

Attribute scale conversion, ability/star estimates and reconstructed monthly history are documented above. Role weights and stars are editor heuristics, not an official FM formula. See [TacticalRoles](TacticalRoles.md) and [SavedTactics](SavedTactics.md) for the implemented methods and compatibility boundaries.

The research identifies all active role codes, but the 13 OOP attribute IDs remain unresolved. Team-picking, match-rating and training weights cannot be substituted for role-suitability weights. Native analysis now links the staff serializer, its 52-byte payload and the indexed getter; the editor extends pinned `fmsave 0.5.11` with 20 additional staff attributes on a 1–20 scale. See [StaffAttributes.md](StaffAttributes.md) for evidence, validation and limits. Job titles remain unknown and raw `r4` is explicitly not a job code.
