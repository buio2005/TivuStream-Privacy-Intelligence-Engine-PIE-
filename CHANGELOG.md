# Changelog

Tutte le modifiche rilevanti al progetto **TivuStream Privacy Intelligence Engine (PIE)** vengono registrate in questo documento.

La documentazione segue un versionamento indipendente da quello del codice.

Il progetto utilizza il versionamento semantico nel formato `MAJOR.MINOR.PATCH`.

---

## Documentation Release 1.2.0 — 2026-08-04

Definisce il meccanismo di classificazione dei domini, che resta interamente locale e dichiara la propria età.

È la revisione che precede il Threat Engine: nessuna riga di codice viene scritta prima che la regola sia scritta.

### Added

**Liste di classificazione — Specification 08 alla 1.2.0**

Struttura di una lista: nome, indirizzo di origine, categoria, **licenza**, ultimo aggiornamento riuscito, numero di voci, stato di attivazione.

La licenza è obbligatoria. Una lista priva di licenza dichiarata non viene distribuita con il progetto.

**L'insieme delle liste predefinite non è definito da questa specifica.** La scelta richiede la verifica della licenza, della manutenzione e della qualità di ciascuna fonte, ed è una decisione da assumere esplicitamente prima del rilascio pubblico. Definire il meccanismo non autorizza a inventare le fonti.

**Regola di corrispondenza**

Il confronto risale dal nome completo verso il dominio superiore, una etichetta alla volta, e si arresta alla prima corrispondenza.

La confidenza dipende da come la corrispondenza è stata ottenuta.

| Corrispondenza                        | Confidenza |
| ------------------------------------- | ---------- |
| Nome completo presente in lista        | `High`     |
| Corrispondenza su un dominio superiore | `Medium`   |

Il secondo caso è un'inferenza. Resta ragionevole e resta un'inferenza, e viene dichiarata come tale.

**Freschezza dichiarata**

Ogni classificazione dichiara l'età della lista dalla quale proviene.

La classificazione locale comporta un ritardo nel riconoscimento delle minacce recenti. Il ritardo non viene nascosto: viene misurato e dichiarato, come la copertura del punteggio e la qualità delle misure.

**Comportamento in caso di aggiornamento fallito e in assenza di rete**

Un aggiornamento non riuscito non invalida la lista esistente: il sistema continua a usarla e ne dichiara l'età crescente.

Nessuna funzione di analisi dipende dalla disponibilità della rete.

**Entità ClassificationList — Specification 05 alla 2.1.0**

Il modello acquisisce l'entità che descrive una lista, e `Domain` acquisisce `categoryConfidence`, `categorySource` e `categorySourceUpdatedAt`.

Queste proprietà sono assenti quando la categoria è `Unknown`.

**Presentazione — Specification 09 alla 1.4.0**

L'interfaccia dichiara per ogni classificazione la lista di provenienza, la sua età e la confidenza.

Un dominio non classificato viene presentato come **non classificato**, mai come sicuro: è la differenza fra dire che non si sa e dire che non c'è nulla.

**Glossario alla 1.5.0**

Voci `Classification List` e `Classification Freshness`.

### Rationale

La classificazione locale è stata confermata dopo aver considerato l'alternativa.

Consultare un servizio di reputazione esterno migliorerebbe l'accuratezza sulle minacce recenti, e comunicherebbe a quel servizio i domini contattati dalla rete dell'utente. Anche le tecniche che trasmettono soltanto un prefisso del dominio richiedono la rete al momento della classificazione, rivelano quando e quanto una rete è attiva, e restringono l'insieme dei candidati con il ripetersi delle interrogazioni.

Il costo non è tecnico: trasformerebbe una promessa verificabile in una promessa da valutare.

La rigidità viene quindi mantenuta dove protegge qualcosa di concreto, la cronologia di navigazione, e il ritardo che ne deriva viene dichiarato anziché subito in silenzio.

### Known Impact

Le euristiche per i domini non classificati non fanno parte di questa release.

Restano un milestone successivo, e fino ad allora un dominio assente da ogni lista resta `Unknown`.

---

## Milestone M4.2 — Allineamento alla 1.1.0 e primi test — 2026-08-04

Il codice recepisce la revisione documentale, e per la prima volta le regole del progetto sono verificate automaticamente.

### Added

**Misure qualificate**

`MeasurementQuality` e `DeviceIdentityBasis` nel modello.

I domini univoci sono dichiarati **limite inferiore**. Gli istanti di osservazione derivati da statistiche su finestra sono dichiarati **vincolati al periodo** anziché presentati come osservazioni.

**Identità dei dispositivi da indirizzo hardware**

L'Adapter legge le assegnazioni DHCP quando la sorgente le fornisce, e ne ricava un'identità stabile fra cambi di indirizzo.

L'acquisizione non fallisce mai: se la sorgente non svolge quel ruolo, o se l'account non può leggere quella sezione, l'identità resta fondata sull'indirizzo di rete e i dispositivi lo dichiarano.

**Progetto di test**

Prima verifica automatica del progetto. Quattordici prove sul motore NPSS, tutte riferite a impegni presi nelle Specification e non a dettagli implementativi.

Fra le proprietà verificate.

* Ciò che non è osservato ha zero punti **ottenibili**, non zero punti.
* Un'area non misurabile conserva il **peso nominale**, altrimenti sparirebbe la traccia di ciò che manca.
* **Togliere dati non può alzare il risultato**: è la proprietà che rende il punteggio non manipolabile per omissione.
* Un'impostazione disattivata abbassa il punteggio ma non la misurabilità: è un fatto sulla rete, non una lacuna nell'osservazione.
* Sotto la soglia non viene prodotto alcun riassunto, ma il breakdown esiste comunque.

Dipendenze approvate, referenziate soltanto dai progetti di test e mai dal prodotto distribuito.

| Nome | Scopo | Licenza |
| --- | --- | --- |
| `xunit` | Verifica automatica | Apache-2.0 |
| `xunit.runner.visualstudio` | Esecuzione delle prove | Apache-2.0 |
| `Microsoft.NET.Test.Sdk` | Piattaforma di test | MIT |

### Changed

**Soglia di copertura applicata**

`Npss.OverallScore` e `Npss.Status` sono ora assenti quando la copertura è inferiore a 60.

Anche il registro operativo lo dichiara: nessun punteggio prodotto, breakdown registrato.

**Algoritmo alla versione 2.0.0**

La migrazione 0006 rimuove i punteggi calcolati con la versione precedente anziché conservarli. Non sono confrontabili con i successivi, e mantenerli produrrebbe uno storico apparentemente continuo. Nessuna misura va perduta: i punteggi derivano dalle acquisizioni, che restano.

### Found

**Un ramo del motore non è raggiungibile**

Scrivendo le prove sul trend è emerso che quel calcolo **non viene mai eseguito**.

Il trend richiede un punteggio complessivo, che richiede copertura 60. Ma le aree dipendenti dalla classificazione dei domini e dal Device Engine valgono sessanta punti su cento, e nessuno dei due esiste: nessun input possibile raggiunge la soglia.

Abbassare la soglia nelle prove avrebbe verificato una regola che il prodotto non applica. La condizione è annotata nel file di test e resterà tale finché il Threat Engine non sarà disponibile.

I test non hanno trovato un difetto: hanno trovato una parte di sistema che nessuno stava esercitando.

---

## Documentation Release 1.1.0 — 2026-08-04

Revisione nata da una valutazione critica del progetto, non da un requisito nuovo.

Tre difetti erano stati rilevati: il punteggio complessivo presentava un giudizio come se fosse una misura, i valori approssimati non venivano dichiarati tali nonostante la specifica lo imponesse, e due proprietà temporali erano valorizzate con dati plausibili anziché osservati.

Nessun codice è stato prodotto in questa release.

### Root Cause

I tre difetti avevano un'unica origine.

Il punteggio trattava già la conoscenza come **graduata**: area misurata, parzialmente misurata, non misurabile.

Il singolo valore era invece trattato in modo **binario**: noto oppure vuoto.

Quella asimmetria costringeva a scegliere fra due errori ogni volta che un dato era noto con precisione ridotta: inventare un valore plausibile, oppure scartare un'informazione realmente posseduta. In due casi era stata scelta la prima strada.

### Added

**Measurement Quality**

Ogni valore può dichiarare come è conosciuto: `Exact`, `LowerBound`, `PeriodBounded`, `Estimated`.

> Un valore conosciuto con minore precisione viene qualificato, non cancellato e non arrotondato al plausibile.

Applicazioni immediate.

* Il numero di domini univoci diventa un **limite inferiore dichiarato**, che è un'affermazione più precisa di "approssimato" e più onesta di un conteggio.
* Gli istanti di prima e ultima osservazione derivati da statistiche su finestra sono dichiarati **vincolati al periodo**. Quando la stessa proprietà arriva dai log delle interrogazioni diventa esatta, e la differenza è visibile all'utente.

Non è una regola aggiuntiva: è la regola del punteggio, estesa al livello che ne era rimasto escluso.

**Identity Basis**

`Device` dichiara se la propria identità si fonda sull'indirizzo hardware, stabile, oppure sull'indirizzo di rete, che cambia.

Attribuire un comportamento a un dispositivo è l'affermazione più forte che il sistema produce, e la sua solidità va resa visibile.

**Acquisizione delle assegnazioni DHCP**

Quando la Data Source svolge anche il ruolo di server DHCP, l'Adapter ne ricava la corrispondenza fra indirizzo hardware e indirizzo di rete.

È un guadagno di accuratezza reale, non una qualifica: l'identità del dispositivo sopravvive ai cambi di indirizzo.

### Changed

**Il punteggio complessivo è dichiarato come giudizio**

Il breakdown è misura: chiunque disponga degli stessi dati ottiene gli stessi valori.

Il numero complessivo richiede di stabilire quanto ciascuna area conti, e quella scelta non discende dai dati.

I pesi diventano una **posizione editoriale dichiarata del progetto**, motivata voce per voce, versionata insieme all'algoritmo e rivedibile. Non derivano da uno standard di settore, perché non ne esiste uno.

Un giudizio presentato come misura è una misura falsa.

**Soglia minima di copertura**

Sotto una copertura di **60** il punteggio complessivo **non viene prodotto**.

`Npss.overallScore` e `Npss.status` diventano opzionali.

Il sistema presenta il solo breakdown e dichiara di non disporre di elementi sufficienti per un giudizio complessivo, pur avendo misure valide da mostrare.

La Specification 09 vieta di sostituirlo con un numero provvisorio, una barra vuota o un segnaposto che suggerisca un valore in arrivo.

È la scelta che distingue maggiormente il progetto: uno strumento che rifiuta di dare un voto quando non ha elementi sufficienti.

**09 - Network Privacy**

L'interfaccia non presenta mai come esatto un valore qualificato diversamente, dichiara la base dell'identità dei dispositivi e comunica il punteggio trattenuto come scelta del sistema, non come guasto.

### Consequence

Con la copertura attuale di 35, il sistema **non produrrà più un punteggio complessivo** finché il Threat Engine non sarà disponibile.

È il comportamento voluto. Il 69 mostrato ieri poggiava su un terzo del sistema di valutazione.

---

## Milestone M4.1 — Configurazione e primo motore del Core — 2026-08-03

Il progetto Core, vuoto dall'inizio, contiene il primo motore. PIE produce un punteggio.

### Added

**Acquisizione della configurazione**

Entità `SourceConfiguration`, interfaccia di capacità corrispondente, lettura delle impostazioni nel Technitium Adapter, migrazione 0004 e persistenza.

Le proprietà sono espresse in termini indipendenti dal backend: nessun nome proprio di Technitium compare nel modello.

La configurazione è conservata **per periodo di osservazione**, non come stato corrente. Se una impostazione cambia e il punteggio ne risente, lo storico saprà dire quando è accaduto.

**La capability si deduce dai permessi del token**

L'Adapter dichiara `SourceConfiguration` soltanto se l'account può leggere le impostazioni del server.

Senza il permesso, il sistema non fallisce: dichiara semplicemente meno, e la copertura del punteggio lo riflette.

**`NpssEngine`**

Primo motore del Core. Calcola DNS Security, Configuration e Network Integrity secondo le definizioni della Specification 07, e dichiara non misurabili le tre aree che dipendono da motori non ancora scritti.

Per ogni area conserva i **fattori** che hanno determinato il punteggio, requisito minimo perché ogni variazione sia spiegabile.

Il motore riceve i dati e non ne cerca: non sa da dove provengano né dove finirà il risultato.

**Migrazione 0005, `ScoreRepository` e `GET /api/v1/npss`**

Il punteggio e il suo dettaglio sono conservati per periodo. Il Query Flow legge, non ricalcola.

### Changed

**`Npss.Trend` diventa opzionale**

Il primo punteggio non ha un predecessore, e punteggi con copertura diversa non sono confrontabili.

Con l'enumerazione precedente sarebbe stato necessario dichiarare `Stable`, che significa "nessuna variazione significativa": un'affermazione che il sistema non può sostenere.

Vale la stessa regola già applicata alla reputazione dei domini: ciò che non si conosce resta vuoto.

**`NpssEngine` riceve il fornitore del tempo**

L'istante non viene letto dall'orologio di sistema. Un motore che legge l'orologio non è verificabile: non sarebbe possibile riprodurne una valutazione a un momento scelto.

### Fixed

**La continuità penalizzava le installazioni recenti**

I periodi attesi erano sempre ventiquattro. Un'istanza avviata da cinque ore risultava con diciannove periodi mancanti e un punteggio ridotto.

Quei periodi non mancavano per un problema della rete: mancavano perché il sistema non stava ancora osservando.

I periodi attesi decorrono ora dalla prima osservazione registrata. Chi osserva da cinque ore senza interruzioni ottiene il punteggio pieno.

Il difetto è emerso alla prima esecuzione reale del motore.

### First Evaluation

Prima valutazione prodotta su dati reali.

| Grandezza      | Valore |
| -------------- | ------ |
| Punteggio      | 69     |
| Stato          | Fair   |
| Copertura      | 35     |
| Trend          | assente, primo punteggio |

Il sistema ha inoltre rilevato la prima condizione reale della configurazione dell'utente: filtraggio dei domini attivo ma **nessuna lista configurata**, quindi privo di effetto.

---

## Documentation Release 1.0.6 — 2026-08-03

Definizione di ciò che il punteggio misura, preliminare alla scrittura del Core.

Nessun codice è stato prodotto in questa release.

### Context

La Specification 07 elencava gli indicatori come **titoli**, non come definizioni. Nessuno indicava come si traduce in punti.

Portarli in codice così com'erano avrebbe significato deciderne il significato dentro un metodo, rendendo il punteggio dipendente da un'interpretazione non verificabile. È l'opposto del principio di Transparency, che richiede che ogni valutazione sia spiegabile.

### Changed

**07 - NPSS**

Ogni indicatore è ora definito in modo calcolabile, con il dato da cui deriva dichiarato.

* **DNS Security** passa da cinque a quattro indicatori da 5 punti: validazione DNSSEC, cifratura del trasporto, configurazione del resolver, errori DNS.
* **Configuration** passa da quattro indicatori sovrapposti a due: raggiungibilità della sorgente e configurazione del filtraggio.
* **Network Integrity** viene ridefinita attorno alla continuità dell'osservazione. Il precedente indicatore sugli errori duplicava DNS Errors.

Rimosso l'indicatore **Query Validation**, privo di significato distinto da DNSSEC Validation. È stato eliminato anziché reinterpretato: un indicatore senza definizione propria avrebbe prodotto punti arbitrari.

L'indicatore sulla cifratura distingue la disponibilità dei trasporti dal loro utilizzo effettivo. Misurare solo la prima premierebbe un'intenzione, misurare solo il secondo ignorerebbe una configurazione corretta.

Dichiarato un limite: la cifratura misurata è quella fra dispositivi e server locale, non fra server e resolver esterni.

**05 - Data Model**

Introdotta l'entità `SourceConfiguration` e la capability corrispondente.

Il modello non aveva alcun posto per la configurazione della sorgente, benché la Specification 04 la elencasse fra i dati da acquisire.

**04 - Technitium Integration**

Il token richiede ora la sola lettura su **Dashboard** e **Settings**.

Il secondo permesso è necessario per acquisire la configurazione, dalla quale dipendono due aree del punteggio. Nessun permesso di modifica è richiesto: PIE non altera mai la configurazione della Data Source.

**08 - Threat Intelligence**

Stabilito che la classificazione avviene **esclusivamente in locale**.

I domini contattati dalla rete non vengono mai trasmessi a terzi, nemmeno per stabilire se siano pericolosi.

Un dominio interrogato rivela cosa un dispositivo stava facendo: consultare un servizio esterno significherebbe comunicargli la cronologia della rete che si sta proteggendo. Uno strumento che analizza la privacy non può ottenere i propri risultati riducendola.

Conseguenze accettate e dichiarate: le minacce recenti vengono riconosciute con il ritardo di aggiornamento delle liste, e l'accuratezza dipende dalla qualità delle liste adottate. In compenso il sistema funziona anche senza connessione.

### Expected Coverage

Con queste definizioni, la copertura del punteggio cresce per gradi.

| Milestone | Aree misurabili                                        | Copertura |
| --------- | ------------------------------------------------------ | --------- |
| M4.1      | DNS Security, Configuration, Network Integrity parziale | circa 35  |
| M4.2      | più Privacy Protection e Threat Protection              | circa 80  |
| M4.3      | più Device Health                                       | circa 100 |

La copertura viene dichiarata all'utente in ogni caso, come previsto dalla specifica.

---

## Milestone M3.6 — Ispezionabilità — 2026-08-03

Correzione di un'incompletezza e introduzione degli endpoint che rendono verificabile ciò che viene conservato.

### Fixed

**I domini bloccati non venivano acquisiti**

`GetDomainsAsync` interrogava soltanto l'elenco dei domini risolti. Technitium tiene quelli bloccati in un elenco separato.

Un dominio bloccato risultava quindi fra le interazioni ma assente dall'elenco dei domini, benché fosse stato osservato. La Specification 05 definisce `Domain` come dominio osservato durante l'analisi, senza distinguere per esito.

Il difetto è emerso da un'osservazione dell'utente sui conteggi riportati nei log: cinque domini a fronte di sei interazioni.

Sono proprio i domini bloccati quelli più significativi per uno strumento di privacy, trattandosi dei tracker e dei domini malevoli che il filtro ha fermato.

### Added

**Endpoint previsti dalla Specification 06**

* `GET /api/v1/devices` — dispositivi dell'ultimo periodo conservato.
* `GET /api/v1/domains` — domini dell'ultimo periodo conservato.
* `GET /api/v1/domains/{domain}` — dettaglio del dominio con i dispositivi coinvolti.

Fino a questo momento i dati venivano scritti nel database senza alcun modo di rileggerli. Per uno strumento che fonda la propria credibilità sulla trasparenza, l'impossibilità di ispezionare ciò che conserva era una lacuna sostanziale.

**Letture nel repository**

Dispositivi, domini e interazioni relative a un dominio dell'ultimo periodo.

### Design Decisions

**Il dettaglio del dominio dichiara se la correlazione è disponibile.**

Il campo `activityAvailable` distingue l'assenza di interazioni dall'assenza della capacità di osservarle.

Senza questa distinzione un elenco vuoto affermerebbe che nessun dispositivo ha raggiunto il dominio, mentre il sistema potrebbe soltanto non essere in grado di saperlo.

---

## Milestone M3.5 — Domain Activity — 2026-08-03

L'Adapter acquisisce la correlazione fra dispositivi e domini, e PIE la conserva.

È la capacità che distingue un motore di analisi da una dashboard: senza di essa una minaccia può essere rilevata ma non attribuita a un dispositivo.

### Added

**`TechnitiumAdapter` implementa `IDomainActivitySource`**

Rileva il componente di registrazione delle query interrogando le applicazioni installate, legge i log in modo paginato e li aggrega.

**Capability condizionata al rilevamento**

`DomainActivity` viene dichiarata soltanto quando il componente risulta installato sull'istanza.

L'implementazione dell'interfaccia esprime ciò che l'Adapter sa fare; la capability esprime ciò che quella istanza offre in quel momento. La distinzione introdotta in M3.2 trova qui il suo primo caso reale.

**Acquisizione condizionata alla capability**

L'`AcquisitionService` richiede la correlazione solo se la Data Source dichiara di poterla fornire. La capability smette di essere descrittiva e diventa portante.

**Migrazione 0003**

Tabella `domain_activity`, agganciata al periodo di osservazione come le altre.

### Design Decisions

**Esito e protocollo non vengono accorpati.**

Una `DomainActivity` rappresenta la combinazione di dispositivo, dominio, esito e protocollo.

Un dispositivo che ha raggiunto lo stesso dominio sia normalmente sia venendo bloccato ha prodotto due fatti distinti. Unirli avrebbe costretto a scegliere un esito prevalente, affermando qualcosa che non è avvenuto.

La chiave primaria della tabella ripete il criterio, così che lo schema non consenta di violarlo.

**Il registro puntuale non viene mai trattenuto per intero.**

L'aggregazione avviene mentre le pagine arrivano. Le singole interrogazioni esistono per il tempo di una pagina e non oltrepassano l'Adapter.

Non è un accorgimento sulla memoria: è il modo concreto di rispettare quanto la Specification 04 dichiara riguardo alla cronologia di navigazione dei dispositivi.

**Limite di lettura.**

L'acquisizione legge al massimo centomila voci per periodo. Il componente di registrazione ne conserva molte meno per impostazione predefinita, quindi il limite non interviene nell'uso normale: esiste perché una ritenzione configurata male non possa trasformare una singola acquisizione in una lettura illimitata.

### Changed

**Registrazione dell'acquisizione**

Il messaggio riporta ora il periodo osservato e il numero di dispositivi, domini e interazioni.

Riporta conteggi e mai i dati stessi: nessun indirizzo, nessun dominio, nulla che appartenga alla rete dell'utente.

**05 - Data Model** — documentato il criterio di aggregazione di `DomainActivity`.

**04 - Technitium Integration** — documentati il rilevamento del componente e il momento in cui avviene l'aggregazione.

---

## Milestone M5.2 — Persistenza delle acquisizioni — 2026-08-03

Le acquisizioni vengono conservate. Il riavvio non azzera più nulla.

### Added

**`ObservationPeriod`**

Intervallo fisso, allineato all'ora solare, che un'acquisizione osserva.

Colloca un istante nel proprio periodo e sa dire se un periodo è già trascorso, quindi immutabile.

**Migrazione 0002**

Tabelle `statistics`, `device` e `domain`, tutte agganciate a un periodo di osservazione ed eliminate insieme a esso.

Applicare la ritenzione consisterà quindi nell'eliminare periodi, e nient'altro.

**`AcquisitionRepository`**

Registra un'osservazione in un'unica transazione: o viene conservata per intero, o non viene conservata affatto.

L'osservazione di un periodo già osservato **sostituisce** la precedente. Il periodo viene rimosso e riscritto, così che nulla della prima osservazione sopravviva accanto alla seconda.

### Changed

**Acquisizione allineata ai periodi**

Chiude il debito registrato come Known Impact nella Documentation Release 1.0.5.

L'`AcquisitionService` non chiede più una finestra mobile degli ultimi sessanta minuti ma il periodo in cui cade l'istante corrente.

I log della sessione precedente mostravano il problema dal vivo: tre acquisizioni consecutive che osservavano intervalli sovrapposti per cinquantanove minuti su sessanta. Scritte così com'erano, avrebbero prodotto decine di righe al giorno che raccontano la stessa ora.

`WindowMinutes` è stato rimosso dalla configurazione: il periodo è fisso e non più negoziabile. `IntervalMinutes` resta e governa soltanto la freschezza del periodo corrente.

**L'acquisizione raccoglie anche dispositivi e domini**

In precedenza venivano richieste le sole statistiche.

Dispositivi e domini vengono conservati ma non ancora riletti: la lettura arriverà con gli endpoint che li presentano.

**Gli endpoint leggono dal database**

`statistics` restituisce l'ultima osservazione conservata anziché lo stato in memoria.

`health` riporta il numero di periodi conservati.

Lo stato in memoria e il database rispondono a domande diverse e restano separati: il primo dice cosa è appena successo, compresi i guasti, il secondo cosa si sa. Un guasto non cancella lo storico, e lo storico non nasconde un guasto.

### Verified

La persistenza è stata verificata con una prova che esclude ogni altra spiegazione.

1. Traffico DNS generato verso l'istanza, acquisito e reso disponibile dagli endpoint.
2. Numero di periodi conservati rimasto a **1** dopo più acquisizioni della stessa ora: ciascuna ha sostituito la precedente anziché accodarsi.
3. Applicazione arrestata, **Data Source spenta**, applicazione riavviata.
4. `health` ha riportato `Failing`, mentre `statistics` ha continuato a restituire gli stessi valori.

Con la sorgente spenta l'acquisizione è impossibile, e il riavvio azzera la memoria. I dati serviti provenivano quindi necessariamente dal database.

La prova ha confermato anche la separazione fra stato e storico: il sistema ha dichiarato di non riuscire a leggere e nello stesso momento ha continuato a servire l'ultimo dato valido.

---

## Milestone M5.1 — Storage, schema e migrazioni — 2026-08-03

Fondamenta della persistenza. Nessun dato applicativo viene ancora scritto.

### Added

**Progetto `TivuStream.Pie.Storage`**

Referenzia il solo Unified Data Model. Il Core non lo conosce.

**`SqliteConnectionFactory`**

Apre le connessioni e crea la cartella del database se assente.

Abilita i vincoli di integrità referenziale su ogni connessione: SQLite li lascia disattivati per impostazione predefinita, e un riferimento non verificato è un riferimento che prima o poi risulterà errato.

**`SchemaMigrator` e `IMigration`**

Migrazioni scritte a mano, ordinate, applicate ciascuna in una propria transazione.

Nessuna migrazione viene generata a partire dal modello: uno schema prodotto automaticamente cambia ogni volta che cambia il modello, che è esattamente ciò che non deve accadere a dati già presenti sulla macchina di una persona.

Una migrazione già applicata non viene mai modificata; un errore si corregge con una migrazione successiva.

**Rifiuto dello schema più recente**

Se il database dichiara una versione superiore a quella attesa, l'avvio viene interrotto con un messaggio esplicito.

Proseguire significherebbe scrivere record che questa versione non comprende, con danni che emergerebbero molto dopo la causa.

**Migrazione 0001**

Crea le tabelle `data_source` e `observation_period`.

Il vincolo di unicità su sorgente e inizio del periodo **rende impossibile violare la regola dei periodi di osservazione**: una regola architetturale diventa un vincolo del database anziché una raccomandazione.

**`MigrationOutcome`**

Riporta versione iniziale, finale e migrazioni applicate. Una modifica alla forma dei dati conservati non viene mai eseguita in silenzio.

### Dependencies

**Prima dipendenza esterna, e primo blocco dell'audit**

`Microsoft.Data.Sqlite` 10.0.0 ha portato con sé `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, segnalata dall'advisory GHSA-2m69-gcr7-jv3q.

Si tratta di CVE-2025-6965: nelle versioni di SQLite precedenti alla 3.50.2 il numero di termini di aggregazione può eccedere le colonne disponibili, con possibile corruzione di memoria. Gravità 7,2.

L'audit delle dipendenze, configurato in M2.1 quando il progetto non aveva ancora alcuna dipendenza, ha rifiutato la compilazione. Il primo pacchetto esterno del progetto è stato anche il primo caso in cui quel controllo è servito.

**Percorso seguito**

1. Aggiornamento a `Microsoft.Data.Sqlite` 10.0.10, l'ultima disponibile. Insufficiente: risolve ancora a 2.1.11.
2. Pinning transitivo centralizzato dell'intera famiglia SQLitePCLRaw a 2.1.12. Le versioni risultano registrate ma NuGet non le applica.
3. **Riferimento diretto** a `SQLitePCLRaw.bundle_e_sqlite3` nel progetto Storage. Un riferimento diretto prevale sempre sulla risoluzione transitiva.

L'audit accetta la 2.1.12. Nessuna deroga è stata necessaria.

| Nome                              | Scopo                       | Licenza    |
| --------------------------------- | --------------------------- | ---------- |
| `Microsoft.Data.Sqlite` 10.0.10   | Accesso al database SQLite  | MIT        |
| `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 | Libreria SQLite nativa | Apache-2.0 |

Il riferimento diretto alla libreria nativa è una forzatura consapevole, annotata nel file di progetto con la propria condizione di uscita: va rimosso quando `Microsoft.Data.Sqlite` aggiornerà la dipendenza per conto proprio.

### Changed

**Host applicativo**

Lo schema viene portato alla versione attesa prima che qualunque richiesta venga servita, e l'esito viene registrato.

L'endpoint di stato riporta ora lo stato dello Storage e la versione dello schema.

Il percorso del database è deliberatamente escluso dalla risposta: l'endpoint non verifica ancora i permessi, e la posizione dei dati di una persona non è un'informazione da fornire a chiunque la chieda.

---

## Documentation Release 1.0.5 — 2026-08-03

Definizione della persistenza, ultima lacuna documentale rilevata durante la revisione iniziale.

Nessun codice è stato prodotto in questa release.

### Added

**16 - Persistence Specification**

Nuovo documento. Definisce cosa viene conservato, per quanto tempo, dove e con quali garanzie.

Elementi principali.

* Si conservano le acquisizioni convertite nel Unified Data Model e i risultati del Core. Le prime consentono di ricalcolare le analisi su dati storici quando un algoritmo cambia, cosa altrimenti impossibile data la ritenzione dei backend.
* **Non viene mai conservato il dettaglio della singola interrogazione DNS.** L'aggregazione avviene nell'Adapter. È la principale misura di protezione dell'utente prevista dal progetto.
* Ritenzione a livelli: dettaglio orario per 30 giorni, aggregati giornalieri per 12 mesi, aggregati mensili per 5 anni. Valori predefiniti e configurabili.
* Schema versionato, con migrazioni esplicite e ordinate. L'avvio viene rifiutato se lo schema è più recente del software, per evitare corruzione silenziosa.
* Il database non lascia mai il dispositivo. Nessuna telemetria. L'eliminazione dei dati è effettiva e non una marcatura logica.

**Periodi di osservazione**

Concetto centrale introdotto da questa specifica.

Un'acquisizione **non è un insieme di eventi ma l'osservazione di un intervallo**. Due osservazioni di intervalli sovrapposti descrivono in parte lo stesso traffico e non sono sommabili.

Con la configurazione attuale, che acquisisce ogni cinque minuti una finestra di sessanta, si sarebbero prodotte dodici osservazioni all'ora largamente ridondanti. Sommarle avrebbe generato valori privi di senso.

Le acquisizioni sono quindi allineate a periodi fissi, per impostazione predefinita l'ora solare. Una nuova osservazione dello stesso periodo **sostituisce** la precedente; un periodo concluso è immutabile; lo storico è la sequenza dei periodi conclusi, che non si sovrappongono e sono aggregabili.

L'idempotenza è garantita per costruzione. La frequenza di acquisizione diventa un parametro di freschezza, non di correttezza.

**Componente Storage**

Aggiunto ai componenti ufficiali del Backend. Nessuno degli otto esistenti riguardava la conservazione dei dati.

Il Core non lo conosce, il Frontend non lo raggiunge, il Unified Data Model resta privo di qualunque elemento di persistenza.

### Dependencies

Approvata la prima dipendenza esterna del progetto.

| Nome                   | Scopo                    | Licenza |
| ---------------------- | ------------------------ | ------- |
| `Microsoft.Data.Sqlite`| Accesso al database SQLite | MIT   |

L'accesso ai dati avviene tramite SQL esplicito. Schema e interrogazioni restano ispezionabili, senza livelli di comportamento implicito.

### Changed

**11 - Backend Specification** — aggiunto il componente Storage con responsabilità e vincoli, e il riferimento ai periodi di osservazione nell'Adapter Manager.

**00 - Glossary** — aggiunti i termini `Storage` e `Observation Period`.

### Known Impact

L'attuale `AcquisitionService` richiede una finestra mobile degli ultimi sessanta minuti. Andrà allineato ai periodi di osservazione fissi al momento dell'implementazione dello Storage.

---

## Milestone M3.4 — Prima fetta verticale — 2026-08-02

Primo percorso completo dal Data Source al browser: Technitium, Adapter, Unified Data Model, REST API.

Nessuna dipendenza esterna introdotta.

### Added

**`AcquisitionService`**

Servizio in background che acquisisce all'avvio e poi a intervallo regolare. È lo Scheduler della Specification 11 in forma minima.

È l'unico componente che raggiunge una Data Source, e non viene mai innescato da una richiesta del Frontend.

**`AcquisitionState`**

Conserva in memoria l'esito dell'ultima acquisizione. Il Query Flow legge da qui e non raggiunge mai la sorgente: è ciò che tiene separati i due flussi.

Lo stato è volatile. La persistenza è un milestone successivo, quindi il riavvio dell'host azzera l'ultima acquisizione.

**`AcquisitionOptions`**

Intervallo fra due acquisizioni e ampiezza della finestra richiesta, tenuti distinti: sovrapporre la finestra evita di perdere dati ai bordi.

**Involucro di risposta**

`ApiResponse` e `ApiError` secondo la forma definita dalla Specification 06.

**Endpoint**

* `GET /api/v1/health` — stato di API, Core, Adapter e Backend, con descrizione della Data Source e momento dell'ultima acquisizione.
* `GET /api/v1/statistics` — statistiche aggregate acquisite.

Il report di stato dichiara `Core: NotImplemented` anziché omettere la sezione: una sezione assente si leggerebbe come una sezione senza nulla da segnalare, che è un'affermazione diversa.

**Configurazione**

`appsettings.Local.json`, escluso da Git, ospita il token. Il file `appsettings.Local.json.example` documenta come ottenerlo.

### Design Decisions

**Le enumerazioni viaggiano come nomi.** Un numero sarebbe privo di significato per chi legge la risposta.

**L'Adapter viene risolto all'interno del blocco protetto.** Un Adapter privo di configurazione fallisce alla creazione, e un host che si arresta perché manca un token sarebbe un modo scadente di segnalarlo. Con questa scelta il sistema resta in piedi e riporta il motivo tramite l'endpoint di stato.

### Fixed

**Due forme di risposta nelle API di Technitium**

La prima esecuzione end-to-end ha rivelato che le API non usano una sola forma di risposta.

La maggior parte delle chiamate annida il contenuto in una proprietà `response`. Le chiamate relative alla sessione restituiscono invece i propri campi alla radice, accanto a `status`.

L'Adapter cercava sempre la proprietà annidata e riceveva un contenuto vuoto da `session/get`.

L'informazione era già presente nei dati raccolti in M3.1, ma era passata inosservata: la ricognizione aveva confrontato i contenuti e non le forme.

Il client supporta ora entrambe le forme. La distinzione è documentata nella Specification 04, con l'avvertenza che non è deducibile dal nome della chiamata.

### Verified

La fetta verticale è stata verificata contro un'istanza reale di Technitium 15.4.

Il traffico generato — cinque domini, tre ripetizioni, due tipi di record — ha prodotto trenta interrogazioni. Le statistiche acquisite tramite REST API corrispondono a quelle mostrate dalla console del server, con l'unica differenza attribuibile a due interrogazioni successive all'acquisizione.

Elementi confermati.

* Il percorso completo da Data Source a REST API attraversa tutti i livelli previsti.
* `session/get` è la fonte corretta per versione e stato DNSSEC.
* Le capability sono dedotte dai permessi del token e non dichiarate a mano.
* `DomainActivity` non viene dichiarata, coerentemente con il fatto che l'Adapter non ne implementa l'interfaccia.
* **L'intervallo personalizzato conserva il dettaglio per dominio.** L'acquisizione incrementale resta quindi praticabile.

Il comportamento in caso di guasto è stato osservato prima della correzione: l'errore è stato catturato, tradotto, memorizzato e riportato dagli endpoint senza arrestare l'host.

### Declared Debt

Due deroghe consapevoli alla Specification 06, ammesse per l'uso locale e da sanare prima di qualunque esposizione in rete.

* Il servizio ascolta in **HTTP** anziché HTTPS.
* **Nessun endpoint verifica i permessi.**

Sono registrate qui come debito dichiarato, non come dimenticanze.

---

## Milestone M3.3 — Technitium Adapter, livello base — 2026-08-02

Prima implementazione che comunica con una Data Source reale.

Copre le tre capacità disponibili su qualunque installazione Technitium: `Statistics`, `Device` e `Domain`.

Nessuna dipendenza esterna introdotta: `HttpClient` e `System.Text.Json` appartengono al framework. `Directory.Packages.props` resta vuoto.

### Added

**`TechnitiumOptions`** — identificativo della Data Source, indirizzo e API Token. Il token resta confinato nell'Adapter.

**`TechnitiumClient`** — trasporto verso le API pubbliche. Autentica la richiesta, legge l'esito **dal corpo della risposta e non dal codice HTTP**, e converte i guasti in `AdapterException`. Traccia di stack e messaggio interno prodotti dal backend non oltrepassano l'Adapter.

**DTO di deserializzazione** — tipi interni che rispecchiano le risposte dell'API, tenuti separati dal Unified Data Model.

**`DeviceIdentity`** — derivazione deterministica dell'identificatore del dispositivo dal suo indirizzo di rete.

**`TechnitiumAdapter`** — implementa `IStatisticsSource`, `IDeviceSource` e `IDomainSource`. Non implementa `IDomainActivitySource`, coerentemente con il fatto che la correlazione richiede un componente facoltativo.

### Design Decisions

**Descrizione della Data Source da `session/get`.** Una sola chiamata fornisce versione, nome, stato DNSSEC e permessi del token. Le capability vengono dichiarate solo quando il token è realmente in grado di leggere il dato corrispondente.

**`FailedQueries` esclude NXDOMAIN.** Un dominio inesistente è una risposta corretta, non un guasto. Includerlo avrebbe prodotto tassi di errore elevati in assenza di problemi, abbassando indebitamente l'area Network Integrity.

**`UniqueDomains` è approssimato.** La sorgente espone solo i domini più frequenti.

**`FirstSeen` e `LastSeen` sono i limiti della finestra acquisita.** La sorgente riporta attività su intervallo, non istanti di prima e ultima osservazione.

**Finestra tradotta in intervallo personalizzato.** Il comportamento di questa modalità rispetto al dettaglio per dominio non è ancora stato verificato: il primo test end-to-end lo mostrerà.

### Changed

**Modello dati**

* `Domain.Reputation` diventa opzionale. È prodotta dal Threat Engine e un Adapter non la assegna mai.
* `DataSource.Status` e `Device.Status` passano da `string` a enumerazioni dedicate.

L'implementazione ha rivelato che il modello obbligava l'Adapter a valorizzare proprietà di competenza del Core. Per compilare sarebbe stato necessario inventare valori.

**05 - Data Model**

Introdotta la sezione Ownership of Properties con la regola corrispondente, e i vocabolari dei due stati.

**04 - Technitium Integration**

Documentati `session/get` come fonte della descrizione, la composizione di `FailedQueries` e la derivazione dell'identità dei dispositivi con i relativi limiti.

Corretta l'indicazione precedente che attribuiva lo stato DNSSEC alle impostazioni del server, che richiedono permessi più ampi del necessario.

---

## Milestone M3.2 — Contratto Adapter — 2026-08-02

Definizione del contratto che ogni Adapter deve rispettare.

Nessuna implementazione: il progetto `TivuStream.Pie.Adapters` contiene esclusivamente interfacce, un tipo per la finestra temporale e un'eccezione dedicata.

### Added

**Interfaccia di base**

`IAdapter` porta l'identità dell'Adapter e la descrizione della Data Source, comprensiva di versione, stato operativo e capability effettivamente disponibili.

La proprietà `Provider` è disponibile senza contattare la Data Source, così che un Adapter resti identificabile anche quando è irraggiungibile.

**Interfacce segregate per capacità**

`IStatisticsSource`, `IDeviceSource`, `IDomainSource`, `IDomainActivitySource`.

Un Adapter implementa soltanto ciò che è in grado di fornire. Non deve implementare metodi non supportati né sollevare eccezioni di non supporto.

**`AcquisitionWindow`**

Intervallo temporale esplicito per ogni acquisizione.

La ricognizione M3.1 ha dimostrato che finestre differenti non contengono necessariamente le stesse informazioni: l'intervallo non può quindi essere assunto dall'Adapter. Consente inoltre l'acquisizione incrementale.

**`AdapterException`**

Gli errori della Data Source vengono convertiti prima di lasciare l'Adapter. Il resto del sistema non gestisce mai errori espressi nel vocabolario di uno specifico backend, e i dettagli diagnostici non oltrepassano l'Adapter.

**`IAdapterManager`**

Registrazione ed elenco degli Adapter.

Il metodo che esegue il ciclo di acquisizione è deliberatamente assente: la sua forma dipende dal punto di ingresso del Core, non ancora definito, e dichiararlo ora significherebbe indovinarlo.

### Design Rationale

Le interfacce implementate esprimono ciò che un Adapter **può** fornire; le capability dichiarate esprimono ciò che la Data Source fornisce **effettivamente** nella configurazione corrente.

Le due informazioni non coincidono: Technitium implementa la correlazione fra dispositivi e domini soltanto dopo l'attivazione di un componente facoltativo.

Vale quindi la regola che un Adapter non può dichiarare una capability della quale non implementa l'interfaccia. La verifica spetta all'Adapter Manager alla registrazione.

### Changed

**11 - Backend Specification**

Documentati il contratto Adapter, la distinzione fra capacità potenziali ed effettive, la finestra di acquisizione e la gestione degli errori.

---

## Milestone M2.2.1 — Allineamento del modello — 2026-08-02

Allineamento del Unified Data Model implementato alla Documentation Release 1.0.4.

### Added

* `ScoreComponentState`, enumerazione con i valori `NotMeasurable`, `PartiallyMeasured` e `Measured`.
* `ScoreComponent.State`.
* `Npss.Coverage`.

### Changed

* `ScoreComponent.Score` e `ScoreComponent.MaxScore` passano da `int` a `decimal`.

La misurazione parziale produce valori frazionari: un'area di peso 15 con un indicatore valutato su quattro ha un punteggio ottenibile di 3,75. Un tipo intero avrebbe imposto un arrotondamento, e arrotondare la porzione osservata avrebbe alterato la copertura dichiarata.

* `ScoreComponent.Weight` resta `int`: è il peso nominale definito dall'algoritmo, sempre intero.
* Precisata nei commenti la semantica di `MaxScore` come punteggio effettivamente ottenibile.

La corrispondenza con la Specification 05 è verificata proprietà per proprietà.

---

## Documentation Release 1.0.4 — 2026-08-02

Introduzione dello stato di misurazione parziale nel Network Privacy & Security Score.

Nessun codice è stato prodotto in questa release.

### Context

La release precedente prevedeva due soli stati per un'area di valutazione: misurata o non misurabile.

La revisione ha evidenziato un caso reale non rappresentabile. Senza Domain Activity l'area Device Health perde il comportamento dei dispositivi ma conserva il volume di traffico, che resta un dato esatto.

Dichiarare l'intera area non misurabile avrebbe significato scartare un'informazione realmente posseduta.

### Changed

**05 - Data Model**

* `ScoreComponent.state` acquisisce il valore `PartiallyMeasured`.
* Definita la relazione fra `weight` e `maxScore`: il primo è il peso nominale dell'area, il secondo il punteggio effettivamente ottenibile in base agli indicatori valutati.
* La porzione di peso non misurata non concorre né al punteggio ottenuto né a quello ottenibile.
* `NPSS.coverage` è ora la somma dei `maxScore` di tutte le aree.

Nessun campo è stato aggiunto al modello: i campi esistenti hanno ricevuto una semantica precisa.

**07 - NPSS**

* Introdotti i tre stati di misurazione.
* Definita la regola del calcolo parziale: `maxScore` proporzionale alla quota di indicatori valutati, con indicatori di pari peso all'interno dell'area salvo indicazione contraria.
* Aggiornato l'esempio con il calcolo completo e verificabile.
* Le aree parzialmente misurate producono una Recommendation come quelle non misurabili.

**09 - Network Privacy**

* L'interfaccia distingue tre condizioni anziché due.
* Una condizione parziale non viene mai presentata come completa.

**00 - Glossary**

* Aggiornata la definizione di `Coverage`.

### Design Rationale

Il criterio adottato è che l'assenza di dati non possa in alcun caso migliorare il punteggio.

La verifica sull'esempio documentato lo conferma: riconoscere la misurazione parziale ha portato la copertura da 85 a 88,75 e il punteggio da 91 a 90.

La conoscenza aumenta, il risultato no. È il comportamento atteso da uno strumento che non deve trarre vantaggio da ciò che non osserva.

---

## Documentation Release 1.0.3 — 2026-08-02

Recepimento dei risultati della ricognizione M3.1 nelle Specification ufficiali.

Il tema comune delle modifiche è la **distinzione fra ciò che il sistema misura e ciò che non è in grado di misurare**, e l'obbligo di comunicare la differenza all'utente.

Nessun codice è stato prodotto in questa release.

### Changed

**05 - Data Model**

* Definita la semantica di `DataSource.Capabilities`: dichiara quali entità del Unified Data Model la sorgente è in grado di fornire. Il vocabolario coincide con i nomi delle entità. Le funzionalità del prodotto esterno non sono capability ma dati da analizzare.
* `DomainActivity` diventa a disponibilità condizionata, dichiarata tramite capability. In sua assenza il Core non produce l'entità, dichiara non misurabili le analisi che ne dipendono e genera una Recommendation. È vietato sostituire il dato mancante con stime o valori predefiniti.
* `ScoreComponent` acquisisce il campo `state`, con valori `Measured` e `NotMeasurable`.
* `NPSS` acquisisce il campo `coverage`.
* Introdotta la sezione Data Availability, che distingue dato presente, dato assente e dato non misurabile.

**07 - NPSS**

* Un'area non misurabile non riceve punteggio zero: assegnare zero equivarrebbe ad affermare una condizione critica inesistente.
* Il punteggio complessivo è calcolato sulle sole aree misurate e normalizzato sui rispettivi pesi.
* Introdotta la copertura, da mostrare sempre quando inferiore a 100.
* Punteggi con copertura differente non sono confrontabili; una variazione di copertura interrompe la serie storica del trend.
* Ogni area non misurabile produce una Recommendation che dichiara anche le conseguenze sfavorevoli dell'intervento proposto.

**04 - Technitium Integration**

* Documentati i due livelli di disponibilità dei dati, base ed esteso, con le capability corrispondenti.
* Autenticazione tramite API Token non scadente, con utente dedicato a permessi minimi.
* Documentato che l'esito delle chiamate non è espresso dal codice di stato HTTP.
* Introdotti i vincoli di acquisizione: scelta esplicita della finestra temporale, frequenza inferiore al tempo di rotazione del log, dichiarazione dei valori approssimati derivanti da elenchi tronchi.
* Stabilito che l'aggregazione dei log avviene nell'Adapter: il Core non accede mai alla singola interrogazione.

**09 - Network Privacy**

* Introdotta la sezione Honesty of Presentation.
* L'interfaccia distingue sempre il dato assente dal dato non misurabile: uno zero o una sezione vuota affermano che la rete è in ordine, e usarli per rappresentare l'indisponibilità è un'informazione falsa.
* La copertura del punteggio è mostrata quando inferiore a 100.
* Le Recommendation di configurazione sono presentate come azioni proposte, con vantaggi e costi esposti in modo simmetrico prima della scelta.

**12 - Installation**

* Rilevamento delle capacità della Data Source al termine della registrazione.
* Proposta di installazione dei componenti facoltativi, con scelta esplicita, informazione simmetrica e reversibilità.
* Verifica di coerenza fra ritenzione del componente e frequenza di acquisizione, ripetuta a ogni modifica della frequenza.
* Dichiarazione esplicita di quali dati vengono registrati, dove risiedono e per quanto tempo.

**00 - Glossary**

* Aggiunti i termini `Capability` e `Coverage`.

### Fixed

**15 - Technitium API Reconnaissance**

Corretta l'ipotesi iniziale secondo cui `DataSource.Capabilities` andasse valorizzato con i flag di configurazione di Technitium. L'ipotesi confondeva le funzionalità del prodotto esterno, che sono dati da analizzare, con le capacità strutturali della sorgente.

---

## Milestone M3.1 — Technitium API Reconnaissance — 2026-08-02

Ricognizione delle API pubbliche di Technitium DNS Server, propedeutica alla progettazione del contratto Adapter.

Nessun codice prodotto. Nessuna Specification modificata.

### Added

**15 - Technitium API Reconnaissance**

Documento di analisi, non una Specification. Stato `Analysis — Not Approved`.

Contiene autenticazione, endpoint confermati, mapping verso il Unified Data Model proprietà per proprietà, punti aperti e sezioni delle API non ancora verificate.

### Findings

**DomainActivity non è costruibile dalle API del dashboard**

`topClients` e `topDomains` sono aggregati indipendenti: indicano quante query ha prodotto ogni client e quante volte è stato richiesto ogni dominio, ma non quale client abbia contattato quale dominio.

La correlazione richiede i Query Logs, che in Technitium dipendono da una DNS App opzionale.

**Statistiche per finestra temporale, non eventi**

Il modello prevede `firstSeen`, `lastSeen` e `occurrences`, che presuppongono osservazione continua. La sorgente espone aggregati su finestre predefinite. La semantica va documentata.

**Liste troncate**

`getTop` restituisce al massimo `limit` elementi. `Statistics.UniqueDomains` non è calcolabile con esattezza.

**`FailedQueries` non ha una definizione univoca**

Technitium distingue server failure, NXDOMAIN, refused e dropped. NXDOMAIN è una risposta legittima: includerlo fra gli errori altererebbe la valutazione di Network Integrity nel NPSS.

**Verifica su istanza reale**

La ricognizione è stata completata interrogando un'istanza reale di Technitium 15.4 in container. Diciotto endpoint interrogati, diciassette con esito positivo.

Risultati principali.

* `Statistics.DnssecEnabled` risolto: `settings/get` → `dnssecValidation`.
* `DataSource.Capabilities` risolto: i flag `enableDnsOver*`, `dnssecValidation`, `qnameMinimization` ed `eDnsClientSubnet` forniscono un vocabolario derivato dalla sorgente.
* `apps/list` vuoto su installazione nuova e `logQueries` disattivato: conferma definitiva che i Query Logs, e quindi Domain Activity, non sono disponibili di serie.
* `blocked/list` e `cache/list` sono navigatori gerarchici, non elenchi piatti: l'enumerazione richiede di percorrere un albero.
* Le risoluzioni tramite DNS Client API non vengono conteggiate nelle statistiche del dashboard.
* Le statistiche sono persistite su file con ritenzione predefinita di 365 giorni.

**Osservazioni su traffico reale**

La ricognizione è stata completata generando traffico DNS effettivo contro l'istanza.

* `DomainActivity.Protocol` risolto: `protocolTypeChartData.labels` espone etichette in PascalCase, valore osservato `Udp`, insieme completo `Udp`, `Tcp`, `Tls`, `Https`, `Quic`.
* Struttura di `topClients` confermata: `name`, `domain` da risoluzione inversa, `hits`, `rateLimited`.
* `apps/listStoreApps` elenca ventisette app, fra cui quattro varianti di Query Logs e Log Exporter. La descrizione ufficiale avverte dell'impatto sul throughput: l'accesso a Domain Activity comporta un costo prestazionale esplicito.

**La finestra temporale non è indifferente**

A parità di stato del server e nello stesso istante, `LastHour` ha restituito il dettaglio per dominio mentre `LastDay` lo ha restituito vuoto, pur riportando più query e mantenendo le statistiche per client.

L'Acquisition Flow non può assumere che una finestra più ampia contenga tutto ciò che contiene una più stretta. Comportamento da confermare con osservazioni prolungate.

**Verifica dell'app Query Logs**

L'app `Query Logs (Sqlite)` 9.1.1 è stata installata sull'istanza di prova e la sua API interrogata con otto combinazioni di parametri. Tutte hanno risposto correttamente.

* Ogni voce contiene contemporaneamente `clientIpAddress` e `qname`: è la correlazione assente dalle API del dashboard.
* Sono supportati i filtri per client, dominio, tipo di record, protocollo e intervallo temporale, oltre alla paginazione con conteggio totale.
* L'estrazione incrementale è quindi possibile: l'Adapter può richiedere le sole voci successive all'ultima acquisizione.
* Tutte le proprietà di `DomainActivity` risultano ottenibili.

**Ritenzione predefinita insufficiente**

La configurazione dell'app prevede `maxLogRecords: 10000`, che su una rete reale corrisponde a meno di un'ora di traffico. Il limite di sette giorni è quindi teorico.

La frequenza di acquisizione deve essere inferiore al tempo di rotazione del log, altrimenti si perdono dati in modo silenzioso.

La scrittura è bufferizzata in memoria con `maxQueueSize: 200000`, quindi asincrona rispetto alla risoluzione: l'impatto sul throughput è minore di quanto l'avvertenza dell'app lasci supporre.

La configurazione è leggibile e modificabile via API, quindi l'installazione guidata può proporre valori adeguati e l'Adapter può segnalare configurazioni incoerenti.

**Campi aperti**

Chiusi `DataSource.Capabilities` e `DomainActivity.Protocol`. Restano `DataSource.Status` e `Device.Status`, che la sorgente non espone e che vanno definiti da PIE.

---

## Milestone M2.2 — Unified Data Model — 2026-08-02

Implementazione delle entità di dominio definite dalla Data Model Specification.

Il modello è indipendente dalla persistenza: nessun ORM, nessun attributo di mapping o di serializzazione, nessuna logica di business, nessuna validazione.

### Added

**Entità del Unified Data Model**

Undici `sealed record` con proprietà `init`, in `TivuStream.Pie.Model/Entities/`.

`DataSource`, `NetworkSnapshot`, `Statistics`, `Device`, `Domain`, `DomainActivity`, `Threat`, `Alert`, `Recommendation`, `Npss`, `ScoreComponent`.

La corrispondenza con l'elenco delle proprietà della Specification 05 è verificata una a una.

**Enumerazioni**

Sei `enum` in `TivuStream.Pie.Model/Enums/`, limitati ai set di valori esplicitamente elencati nella documentazione.

`ThreatCategory` e `ConfidenceLevel` dalla Threat Intelligence Specification, `SeverityLevel` dalla stessa, `ScoreStatus`, `ScoreTrend` e `ScoreComponentType` dalla NPSS Specification.

I campi il cui insieme di valori non è documentato restano di tipo `string`.

**`Directory.Packages.props`**

Central Package Management abilitato prima dell'introduzione del primo pacchetto NuGet, con pinning delle dipendenze transitive.

Nessun pacchetto è referenziato.

**`global.json`**

Vincola la build a un .NET SDK 10.x.

Senza questo file la build utilizza sempre l'SDK più recente installato sulla macchina. Poiché il progetto tratta i warning come errori e ogni SDK introduce nuove regole degli analizzatori, un aggiornamento dell'ambiente potrebbe far fallire la compilazione di codice non modificato.

`rollForward` è impostato su `latestMinor`: gli aggiornamenti di patch e feature band sono accettati, il passaggio a una major version successiva resta una decisione esplicita.

Il prerequisito è documentato in `CONTRIBUTING.md`.

### Fixed

**Configurazione di build**

La prima compilazione ha rivelato che `IDE0005`, la regola sugli `using` non necessari, non può essere applicata in build se il progetto non genera il file di documentazione XML.

La regola era stata attivata in `.editorconfig` senza il prerequisito, e con i warning trattati come errori la build falliva.

* `GenerateDocumentationFile` impostato su `true` in `Directory.Build.props`.
* `CS1591` disattivato: la generazione del file XML non deve trasformarsi nell'obbligo di commentare ogni membro pubblico, requisito che contraddirebbe il principio di commentare solo dove serve.

### Design Decisions

Scelte imposte dal linguaggio e non coperte dalla Specification 05, approvate prima dell'implementazione.

* Identificatori di sistema come `Guid`, per garantire per costruzione l'indipendenza dal backend richiesta dalla specifica.
* Entità come `sealed record` con proprietà `init` e collezioni `IReadOnlyList`: un NetworkSnapshot rappresenta un istante già trascorso e non deve poter essere modificato dopo la produzione.
* Date come `DateTimeOffset`, per identificare un istante senza ambiguità di fuso orario.
* `ThreatCategory.Unknown` con valore 0, poiché la specifica la designa come categoria di fallback.

---

## Milestone M2.1 — Backend Bootstrap — 2026-08-02

Creazione della struttura permanente della solution.

Nessuna logica applicativa è stata implementata: REST API, Unified Data Model, database, Adapter, servizi, Engine e autenticazione restano da realizzare.

### Added

**Struttura della solution**

Cinque progetti .NET 10 sotto `backend/src/`, radice dei namespace `TivuStream.Pie`.

| Progetto                             | Riferimenti                          |
| ------------------------------------ | ------------------------------------ |
| `TivuStream.Pie.Model`               | nessuno                              |
| `TivuStream.Pie.Core`                | Model                                |
| `TivuStream.Pie.Adapters`            | Model                                |
| `TivuStream.Pie.Adapters.Technitium` | Adapters                             |
| `TivuStream.Pie.Api`                 | Core, Adapters, Adapters.Technitium  |

L'assenza di un riferimento da `Core` verso `Adapters` e `Api` rende l'isolamento del Core verificabile in fase di compilazione.

**`Directory.Build.props`**

Configurazione di build condivisa: `net10.0`, nullable abilitato, warning trattati come errori, analizzatori .NET al livello `Recommended`, applicazione delle regole di stile in build, audit delle dipendenze.

Metadati di copyright omessi intenzionalmente: la License Specification rinvia la decisione alla prima Beta pubblica.

**File `.gitkeep`**

Aggiunti in `frontend/`, `installer/`, `examples/`, `resources/`, `scripts/`, `tools/` e `backend/tests/`.

Git non versiona le directory vuote: senza questi file la struttura ufficiale non sarebbe presente dopo un clone.

**CONTRIBUTING.md**

Il file era presente ma vuoto.

Il contenuto è derivato da `AI_DEVELOPMENT_GUIDE.md`, dal Glossary e dalla License Specification.

Non introduce requisiti nuovi.

### Changed

**11 - Backend Specification**

Documentate la struttura della solution, le responsabilità dei singoli progetti e le regole di riferimento tra progetti.

---

## Documentation Release 1.0.2 — 2026-08-02

Preparazione del repository e allineamento del README alla documentazione ufficiale.

Nessun codice applicativo è stato prodotto in questa release.

### Added

**`.gitattributes`**

Il repository era privo di regole sulle terminazioni di riga: i file erano committati con LF e presenti nel working tree con CRLF, per cui ogni documento risultava interamente riscritto a ogni commit.

In un progetto Documentation First questo rendeva illeggibili proprio i diff delle Specification.

* Normalizzazione a LF per tutti i file di testo.
* Eccezioni CRLF per `*.sln`, `*.ps1`, `*.bat` e `*.cmd`.
* Classificazione dei file binari, inclusi i database SQLite.
* Driver di diff `csharp` per i file `*.cs`.

**`.editorconfig`**

Il file era presente ma vuoto.

* Convenzioni di indentazione per C#, TypeScript, Vue, JSON, YAML, XML e Markdown.
* Regole di stile C# orientate alla leggibilità, come richiesto dai Coding Principles.
* Convenzioni di denominazione: PascalCase per tipi e membri, camelCase per parametri e variabili locali, prefisso `_` per i campi privati, prefisso `I` per le interfacce.

**`.gitignore`**

Il file era presente ma vuoto.

* Esclusione degli artefatti di build .NET e Node.
* Esclusione dei database SQLite locali, che contengono dati di rete dell'utente e la cui esclusione discende dal principio Privacy First.
* Esclusione di credenziali, file `.env` e configurazioni locali.
* Esclusione di file di ambiente di sviluppo e di sistema operativo.

### Changed

**README.md**

Il documento descriveva un'architettura difforme da quella ufficiale.

* Diagramma architetturale corretto con i livelli Adapter, Unified Data Model e REST API, in precedenza assenti.
* Aggiunto il riferimento ad Acquisition Flow e Query Flow.
* Rimosso il termine *Privacy Score*, vietato dal Glossary.
* Rimosso il componente *Data Normalizer*, inesistente in ogni Specification.
* Nomenclatura dei moduli allineata al suffisso Engine.
* Stack tecnologico allineato a quello ufficiale: ASP.NET Core, C#, SQLite, Vue 3, TypeScript, Pinia, Vite.
* Roadmap allineata alle nove milestone della Roadmap Specification, in precedenza descritte come sette fasi differenti.
* Stato del progetto e sezione Licenza allineati alle rispettive Specification.

**Versionamento della documentazione**

Il puntatore alla Documentation Release era rimasto a 1.0.0 nonostante l'aggiornamento delle singole Specification.

* Chiarita in Roadmap Specification la distinzione tra Documentation Release e versione del singolo documento.
* Aggiornati i riferimenti in `PROJECT_CONTEXT.md`, README e Roadmap Specification.

Documenti aggiornati: README.md, PROJECT_CONTEXT.md, 13.

---

## Documentation Release 1.0.1 — 2026-08-02

Risoluzione delle inconsistenze architetturali rilevate durante la revisione precedente all'inizio dello sviluppo.

Nessun codice è stato prodotto in questa release.

### Changed

**Acquisition Flow e Query Flow**

Il flusso dei dati era descritto in modo contraddittorio: le Specification 02 e 03 indicavano `Adapter → Unified Data Model → Core`, mentre le Specification 06, 09 e 11 indicavano `Core → Adapter → Data Source`.

I due percorsi sono stati riconosciuti come flussi distinti e documentati separatamente.

* L'**Acquisition Flow** è innescato dallo Scheduler e orchestrato dall'Adapter Manager.
* Il **Query Flow** serve le richieste del Frontend e non raggiunge mai le Data Sources.
* Il Core è dichiarato componente passivo: non orchestra l'acquisizione e non conosce l'origine dei dati.
* Non sono stati introdotti componenti nuovi.

Specification aggiornate: 00, 02, 03, 06, 09, 11.

---

**Network Privacy & Security Score**

La Specification 07 definiva sei aree di valutazione ma ne mostrava cinque nell'esempio di breakdown, lasciando i pesi indefiniti.

La Specification 05 modellava il punteggio con tre scalari non riconducibili alle aree documentate.

* Introdotta la tabella **Score Weights** con sei aree e pesi la cui somma è 100.
* Corretto l'esempio di breakdown, ora comprensivo di tutte le sei aree.
* Sostituite in 05 le proprietà `securityScore`, `privacyScore` e `protectionScore` con `overallScore`, `status`, `trend`, `algorithmVersion` e `breakdown`.
* Introdotta l'entità **ScoreComponent**, che rende ogni variazione del punteggio spiegabile come richiesto dal principio di Transparency.
* Rimossa la proprietà `privacyScore`, in violazione del Glossary.

Specification aggiornate: 05, 07.

---

**Nomenclatura dei moduli del Core**

Gli stessi moduli comparivano con nomi differenti in Specification diverse, in violazione della regola che vieta i sinonimi.

* Adottato il suffisso **Engine** per tutti i moduli del Core: Threat Engine, Device Engine, NPSS Engine, Alert Engine, Recommendation Engine.
* Aggiunta al Glossary la voce **NPSS Engine**, in precedenza assente.
* *Threat Intelligence* e *Device Intelligence* spostati in **Terms to Avoid** come nomi di modulo.
* *Threat Intelligence* resta valido come nome della materia trattata; il titolo della Specification 08 rimane invariato.
* Allineati i riferimenti ai moduli nel corpo della Specification 08 e nella Phase 4 della Roadmap.

Specification aggiornate: 00, 02, 08, 13.

---

### Removed

**Cartella `engine/`**

Residuo di un'analisi architetturale precedente, mai utilizzata e rimasta in root per dimenticanza.

Il Core risiede in `backend/` come progetto autonomo, isolato dagli Adapter e dall'host delle REST API in modo che il vincolo sia verificabile in fase di compilazione.

Documenti aggiornati: PROJECT_CONTEXT.md, 11.

---

## Documentation Release 1.0.0 — 2026-08-02

Baseline ufficiale del progetto.

### Added

* Documentazione completa del progetto in `docs/`, Specification da 00 a 14.
* Definizione dell'architettura, del Unified Data Model e delle REST API.
* Milestone M1 completata. Sviluppo non iniziato.
