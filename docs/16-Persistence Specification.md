# 16 - Persistence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Persistence Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-09-26

---

# Purpose

Questa specifica definisce come il Privacy Intelligence Engine conserva i dati nel tempo.

La conservazione è necessaria a funzionalità già previste da altre Specification: lo storico del Network Privacy & Security Score, il calcolo del trend, lo storico delle minacce, i confronti temporali e la generazione dei report.

---

# Why Persistence Is Required

Le Data Sources non conservano i dati indefinitamente.

Technitium applica una ritenzione propria e, per i log delle query, un limite basato sul numero di record che su una rete reale corrisponde a una frazione di ora.

Ne discende un principio.

> Ciò che non viene conservato al momento dell'acquisizione è perduto in modo definitivo.

PIE non può quindi limitarsi a interrogare la sorgente al momento della richiesta: deve costruire il proprio storico.

---

# What Is Persisted

## Acquisitions

Le acquisizioni convertite nel Unified Data Model.

Comprendono `Statistics`, `Device`, `Domain` e, quando disponibile, `DomainActivity`.

Conservare le acquisizioni consente di **ricalcolare le analisi su dati storici** senza reinterrogare la sorgente, cosa impossibile data la ritenzione dei backend.

Questo mantiene coerente lo storico quando un algoritmo di analisi viene modificato.

---

## Core Results

I risultati prodotti dal Core.

Comprendono `NetworkSnapshot`, `Npss`, `Threat`, `Alert` e `Recommendation`.

Conservarli evita di ricalcolare l'intera analisi a ogni richiesta, requisito rilevante su hardware modesto.

---

## Reference Data

I dati di riferimento che l'installazione possiede, non ciò che la rete ha fatto.

Comprendono `ClassificationList`.

Non appartengono ad alcun Observation Period e **non sono soggetti a ritenzione**: applicare la ritenzione a una lista significherebbe rimuovere lo strumento con cui si classifica anziché un'osservazione invecchiata.

---

## Accounts And Sessions

Chi usa PIE, non ciò che la rete ha fatto. Due entità interne al Backend, estranee al Unified Data Model.

* **account**: nome utente, ruolo, attivo, hash della password con i parametri dell'algoritmo, indicazione che la password va cambiata, istante di creazione;
* **sessione**: hash dell'identificativo, account, creazione, ultimo uso, scadenza.

Non appartengono ad alcun Observation Period e **non sono soggette a ritenzione**: eliminare un account per età lascerebbe fuori chi lo possiede. Le sessioni scadute vengono invece eliminate.

---

## What Is Never Persisted

PIE **non conserva mai il dettaglio della singola interrogazione DNS**.

Il registro puntuale delle interrogazioni costituisce la cronologia di navigazione di ogni dispositivo della rete. L'aggregazione avviene nell'Adapter, prima che il dato raggiunga il Core, e ciò che viene conservato è esclusivamente il risultato aggregato.

Questa è la principale misura di protezione dell'utente prevista dal progetto.

Non vengono inoltre conservati:

* credenziali delle Data Sources in chiaro;
* password degli account in chiaro, identificativi di sessione in chiaro, codice di configurazione iniziale;
* risposte originali dei backend nel loro formato nativo;
* dettagli diagnostici prodotti dai backend.

---

# Observation Periods

Questa sezione definisce il concetto centrale della persistenza.

---

## The Problem

Un'acquisizione **non è un insieme di eventi**: è l'osservazione di un intervallo temporale.

Due osservazioni di intervalli sovrapposti descrivono in parte lo stesso traffico. Non sono sommabili.

Un sistema che acquisisce ogni cinque minuti una finestra di sessanta produce dodici osservazioni all'ora che descrivono in larga parte gli stessi dati. Sommarle produrrebbe valori privi di senso; sceglierne una arbitrariamente scarterebbe informazione.

---

## The Rule

Le acquisizioni sono allineate a **periodi di osservazione fissi**.

Il periodo predefinito è l'**ora solare**.

| Concetto              | Definizione                                                   |
| --------------------- | -------------------------------------------------------------- |
| Periodo               | Intervallo fisso, allineato all'ora                             |
| Periodo corrente      | Quello in cui cade l'istante dell'acquisizione                  |
| Periodo concluso      | Qualunque periodo interamente trascorso                         |

Valgono le seguenti regole.

**Un'acquisizione osserva il periodo corrente.**

**Una nuova osservazione dello stesso periodo sostituisce la precedente**, non vi si aggiunge.

**Un periodo concluso è immutabile.** Una volta trascorso e osservato, non viene più aggiornato.

**Lo storico è la sequenza dei periodi conclusi**, che non si sovrappongono e sono quindi aggregabili.

---

## Consequences

L'idempotenza è garantita per costruzione: riacquisire lo stesso periodo più volte non altera il risultato.

La frequenza di acquisizione diventa un parametro di **freschezza**, non di correttezza. Acquisire ogni cinque minuti aggiorna il periodo corrente più spesso; non produce duplicazione.

La frequenza deve comunque restare **inferiore alla ritenzione della sorgente**, altrimenti un periodo può concludersi prima di essere stato osservato e i suoi dati sono perduti senza segnalazione.

Il periodo di osservazione è configurabile. Periodi più brevi aumentano risoluzione e volume; periodi più lunghi riducono entrambi.

---

# Retention

La ritenzione è **a livelli**.

| Livello              | Contenuto                              | Ritenzione predefinita |
| -------------------- | -------------------------------------- | ---------------------- |
| Periodi di osservazione | Dettaglio orario                    | 30 giorni              |
| Aggregati giornalieri   | Sintesi per giorno                  | 12 mesi                |
| Aggregati mensili       | Sintesi per mese                    | 5 anni                 |

Alla scadenza di un livello i dati vengono consolidati nel livello successivo e il dettaglio viene eliminato.

Questo appiattisce la crescita dello spazio occupato, che altrimenti sarebbe lineare nel tempo.

I valori predefiniti sono configurabili.

Il consolidamento è **irreversibile**: l'utente deve poterlo comprendere prima di ridurre la ritenzione del dettaglio.

I dati di riferimento sono esclusi dalla ritenzione. Non descrivono un momento e non invecchiano insieme alle osservazioni.

---

## Consolidated Periods

Un aggregato giornaliero o mensile è a sua volta un **periodo di osservazione**, più lungo. Vale per esso tutto ciò che vale per i periodi: non si sovrappone ad altri, è immutabile, è aggregabile.

Ogni periodo conserva due informazioni in più, interne allo Storage ed estranee al Unified Data Model:

| Informazione        | Significato                                                        |
| ------------------- | ------------------------------------------------------------------- |
| Livello             | Ora, giorno o mese                                                  |
| Ore osservate       | Quante ore osservate sono confluite nel periodo; 1 per un'ora       |

Le ore osservate sono necessarie all'onestà dello storico. Un giorno di cui sono state osservate tre ore e un giorno osservato per intero hanno la stessa forma: senza questo numero, il primo sembrerebbe un giorno tranquillo. Un'ora mai osservata resta non osservata anche dopo il consolidamento, e non diventa un'ora a zero.

---

## Day And Month Boundaries

Giorni e mesi seguono il **fuso orario locale del computer su cui PIE è in esecuzione**, perché è il giorno della persona che legge. Un giorno che comincia alle due di notte non è il giorno di nessuno.

Gli istanti di inizio e di fine restano conservati in UTC, come ogni altro istante. Nei giorni del cambio d'ora un giorno dura ventitré o venticinque ore: il periodo lo dice, perché conserva inizio e fine, non una durata.

Un cambio di fuso orario del computer non riscrive i periodi già consolidati.

---

## When A Level Is Consolidated

Si consolida sempre un **giorno intero** o un **mese intero**, mai una parte.

| Operazione                    | Quando                                                         |
| ----------------------------- | --------------------------------------------------------------- |
| Ore → giorno                  | Il giorno è terminato da più della ritenzione oraria            |
| Giorni → mese                 | Il mese è terminato da più della ritenzione giornaliera         |
| Eliminazione dei mesi         | Il mese è terminato da più della ritenzione mensile             |

Ogni consolidamento di un giorno o di un mese avviene in **una sola transazione**: si scrive l'aggregato, si elimina il dettaglio. Un'interruzione lascia il dettaglio intatto, e il consolidamento successivo lo riprende. Riconsolidare non produce duplicati, perché il dettaglio già consolidato non esiste più.

Il consolidamento è un'**operazione pianificata**: viene eseguito all'avvio e poi una volta all'ora, mai durante una richiesta.

---

## What A Consolidated Period Contains

Il principio è quello già applicato alla finestra delle ventiquattro ore: ciò che si somma viene sommato, ciò che non si somma viene dichiarato per quello che è.

| Dato                    | Giorno                                                       | Mese                                  |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------- |
| Conteggi delle interrogazioni | Somma esatta                                           | Somma esatta                          |
| Domini distinti, dispositivi attivi | Il maggiore fra il valore più alto di un periodo e i nomi o identificativi distinti conservati; sempre un **limite inferiore** | Come il giorno |
| Stato DNSSEC            | Quello del periodo più recente                                | Come il giorno                        |
| Dispositivi             | Uno per identificativo; identità dal periodo più recente; prima e ultima osservazione estreme | Come il giorno |
| Domini                  | Uno per nome; occorrenze sommate; classificazione dal periodo più recente, con l'età della lista | Come il giorno |
| Attività dispositivo → dominio | Conteggi sommati per dispositivo, dominio, esito e protocollo | **Non conservata** |
| Configurazione della sorgente | Quella del periodo più recente                          | Come il giorno                        |
| Punteggio               | L'ultimo prodotto nel giorno, con i suoi componenti          | L'ultimo prodotto nel mese            |

La qualità di ogni dato consolidato è la **meno precisa** fra quelle dei periodi che lo compongono.

---

## Device Activity Beyond Thirty Days

L'attività per dispositivo e per dominio è la parte dei dati più vicina a una cronologia di navigazione, anche aggregata per ora.

Negli aggregati giornalieri è conservata per dodici mesi, perché consente di rispondere a domande sul singolo dispositivo in un intervallo ancora recente.

Negli aggregati mensili **non è conservata**. Dopo dodici mesi resta noto quali domini la rete ha contattato e quali dispositivi erano presenti, non più quale dispositivo ha contattato quale dominio. Conservarla per cinque anni ne farebbe un archivio della navigazione di ogni persona della casa, a fronte di un uso che nessuna funzionalità prevista richiede.

Questa è una scelta editoriale, approvata come tale.

---

## Score In Consolidated Periods

Un punteggio non si somma e non si media: la media di due punteggi con coperture diverse non misura nulla.

Un periodo consolidato conserva quindi **l'ultimo punteggio prodotto al suo interno**, invariato: valore, stato, trend, copertura, versione dell'algoritmo, istante di produzione, componenti.

Il punteggio valuta le ventiquattro ore che precedono la sua produzione, come stabilisce la Specification 07. L'ultimo punteggio di un giorno valuta quindi quel giorno. L'ultimo punteggio di un mese valuta l'ultimo giorno del mese, **non il mese**, e va presentato come tale quando lo storico sarà mostrato.

Un periodo in cui non è stato prodotto alcun punteggio non ne ha uno. Non se ne calcola uno a posteriori.

---

## Recalculation

Il consolidamento riduce ciò che può essere ricalcolato. Oltre la ritenzione oraria un algoritmo nuovo può essere applicato ai giorni, non alle ore; oltre la ritenzione giornaliera, ai mesi, senza l'attività per dispositivo.

È la conseguenza dichiarata della conservazione del minimo necessario.

---

## Effective Deletion

Il dettaglio eliminato dal consolidamento non deve restare leggibile nel file.

SQLite, per impostazione predefinita, lascia il contenuto delle righe eliminate nelle pagine libere finché non vengono riutilizzate. Lo Storage abilita la **cancellazione sicura** (`secure_delete`), che sovrascrive quel contenuto. Il costo è una scrittura in più al momento dell'eliminazione, trascurabile ai volumi di PIE.

Il file non si riduce: lo spazio liberato viene riutilizzato dalle acquisizioni successive. La crescita si appiattisce; non diventa una diminuzione.

---

## Retention Configuration

| Parametro                         | Predefinito | Minimo |
| --------------------------------- | ----------- | ------ |
| `Storage:Retention:HourlyDays`    | 30          | 2      |
| `Storage:Retention:DailyMonths`   | 12          | 1      |
| `Storage:Retention:MonthlyYears`  | 5           | 1      |

Il minimo della ritenzione oraria protegge la finestra delle ventiquattro ore, che si legge dal dettaglio orario: consolidare un giorno ancora in finestra toglierebbe al punteggio e alle pagine i dati su cui si basano.

Una ritenzione più fine **prevale** su una più grossolana: un mese non viene consolidato, né eliminato, finché contiene dati che un livello più fine deve ancora conservare. Una ritenzione oraria di quattrocento giorni conserva quindi le ore per quattrocento giorni anche con una ritenzione giornaliera di dodici mesi.

Un valore sotto il minimo **impedisce l'avvio**, con un messaggio che dice quale parametro e quale minimo. Un valore corretto in silenzio cambierebbe ciò che viene eliminato senza che la persona lo sappia.

Ridurre una ritenzione ha effetto al consolidamento successivo e non può essere annullato. La documentazione per la persona lo dice prima di spiegare come farlo.

---

# Architectural Placement

La persistenza è responsabilità di un componente del Backend denominato **Storage**.

```text
Adapter Manager

↓

Unified Data Model

↓

Core

↓

Storage
```

Valgono i seguenti vincoli.

**Il Core non conosce lo Storage.** Produce risultati e non sa dove finiscano, coerentemente con il vincolo che non conosca né sorgenti né destinatari.

**Il Unified Data Model non contiene elementi di persistenza.** Nessun attributo di mapping, nessun riferimento a tecnologie di archiviazione, nessuna dipendenza. La conversione fra modello e archiviazione avviene interamente dentro lo Storage.

**Il Query Flow legge dallo Storage**, mai dalle Data Sources.

---

# Technology

## Database

**SQLite**, in un unico file locale.

La scelta è coerente con i principi Local First e Self Hosted: nessun servizio aggiuntivo da installare, nessuna porta da esporre, nessun processo separato da gestire.

Il file risiede in una posizione determinata dalla configurazione, insieme agli altri dati applicativi.

---

## List Files

I domini contenuti nelle liste di classificazione sono conservati **su file**, non nel database.

Ogni lista è un file nella cartella `data/lists/`, accanto al database.

Il file conserva il **formato originale** della lista scaricata. Nessuna conversione, nessuna normalizzazione preventiva.

Le ragioni della separazione.

* Una lista può contenere centinaia di migliaia di domini, che farebbero crescere il database di ordini di grandezza rispetto alle osservazioni.
* Un file di testo è ispezionabile con un editor qualsiasi, mentre una tabella richiede uno strumento SQL. La Specification 08 richiede che l'utente possa verificare perché un dominio è stato classificato.
* Un aggiornamento sostituisce un file, operazione atomica, anziché riscrivere centinaia di migliaia di righe.
* I domini di una lista non sono osservazioni e non hanno un periodo: tenerli fuori dal database evita che la ritenzione li sfiori.

Il database conserva la **descrizione** della lista. Il file conserva il **contenuto**.

Una descrizione priva del file corrispondente indica una lista mai scaricata, e viene dichiarata come tale.

---

## Data Access

L'accesso avviene tramite **SQL esplicito**.

Schema e interrogazioni restano visibili e ispezionabili, senza livelli di comportamento implicito.

La scelta risponde ai principi di Simplicity e Transparency e al divieto di introdurre astrazioni non necessarie.

---

## Dependency

| Voce      | Valore                                                    |
| --------- | ---------------------------------------------------------- |
| Nome      | `Microsoft.Data.Sqlite`                                     |
| Scopo     | Accesso al database SQLite                                  |
| Licenza   | MIT                                                         |
| Manutenzione | Microsoft, parte dell'ecosistema .NET                    |

È la prima dipendenza esterna del progetto.

La versione viene fissata in `Directory.Packages.props`, secondo il Central Package Management già adottato.

---

# Schema Management

Il database possiede una **versione dello schema**, conservata al suo interno.

All'avvio il sistema confronta la versione attesa con quella presente.

| Condizione            | Comportamento                                            |
| --------------------- | --------------------------------------------------------- |
| Versioni coincidenti  | Avvio normale                                              |
| Schema più vecchio    | Migrazione, preceduta da copia di sicurezza                |
| Schema più recente    | Avvio rifiutato con messaggio esplicito                    |

L'ultimo caso indica un tentativo di utilizzare dati prodotti da una versione successiva del software. Procedere comporterebbe corruzione silenziosa.

Le migrazioni sono **esplicite e ordinate**. Non viene generata alcuna migrazione automatica a partire dal modello.

---

# Backup

La Installation Specification prevede una copia di sicurezza prima di ogni aggiornamento.

La copia comprende il database, la configurazione e la cartella delle liste.

Trattandosi di file locali, la copia consiste nella loro duplicazione a servizio fermo.

Le liste sono comunque riscaricabili: la loro assenza da una copia di sicurezza non comporta perdita di osservazioni.

---

# Privacy

Il database contiene dati relativi all'attività di rete dell'utente.

Si applicano le seguenti regole.

* Il contenuto non lascia mai il dispositivo.
* Non viene trasmessa alcuna telemetria.
* La ritenzione è configurabile e dichiarata all'utente.
* L'utente può eliminare i dati conservati.
* L'eliminazione è effettiva, non una marcatura logica.

L'installazione dichiara quali dati vengono conservati, dove risiedono e per quanto tempo, come previsto dalla Installation Specification.

---

# Performance

Su un dispositivo modesto valgono le seguenti priorità.

* Scritture raggruppate anziché per singola entità.
* Interrogazioni di lettura servite da indici espliciti.
* Consolidamento eseguito come operazione pianificata, non durante una richiesta.
* Nessun ricalcolo dell'analisi durante il Query Flow.

---

# Design Principles

La persistenza segue i seguenti principi.

* idempotenza per costruzione;
* immutabilità dei periodi conclusi;
* conservazione del minimo necessario;
* trasparenza dello schema;
* indipendenza del modello dati dalla tecnologia di archiviazione.

---

# Constraints

Lo Storage:

* non esegue analisi;
* non modifica i dati che riceve;
* non è raggiungibile dal Frontend;
* non è conosciuto dal Core;
* non conserva il dettaglio della singola interrogazione.

---

# Related Specifications

* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 11 - Backend
* 12 - Installation
* 14 - License
