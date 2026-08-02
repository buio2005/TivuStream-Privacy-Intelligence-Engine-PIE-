# 15 - Technitium API Reconnaissance

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Technitium API Reconnaissance

**Version:** 1.3.0

**Status:** Analysis — Not Approved

**Last Updated:** 2026-08-02

---

# Nature of This Document

Questo documento **non è una Specification**.

È il risultato della ricognizione svolta nel milestone M3.1 sulle API pubbliche di Technitium DNS Server.

Non definisce architettura, non modifica il Unified Data Model e non introduce requisiti.

Il suo scopo è fornire la base fattuale necessaria per progettare il contratto Adapter e per aggiornare, dove necessario, la Technitium Integration Specification.

---

# Verification Status

Le informazioni sono classificate come segue.

| Livello       | Significato                                                            |
| ------------- | ---------------------------------------------------------------------- |
| **Confermato**| Verificato sulla documentazione ufficiale delle API                     |
| **Da verificare** | Non recuperato o dedotto; richiede conferma su un'istanza reale     |

Le fonti utilizzate sono due.

1. La documentazione ufficiale `APIDOCS.md` del repository del progetto. Il recupero si è interrotto prima delle sezioni Settings, Blocked Zones, Cache e Logs.
2. **Un'istanza reale** di Technitium DNS Server **versione 15.4**, eseguita in container e interrogata direttamente. Questa verifica ha coperto proprio le aree mancanti dalla documentazione.

Diciotto endpoint sono stati interrogati: diciassette hanno risposto, uno è fallito per una dipendenza di rete.

Le informazioni derivate dall'istanza reale sono le più affidabili, perché descrivono il comportamento effettivo del prodotto e non la sua descrizione.

---

# Authentication

**Confermato.**

A partire dalla versione 15.0 le API richiedono un bearer token.

```text
Authorization: Bearer <token>
```

Il passaggio del token come parametro `token` in query string o form data resta supportato per retrocompatibilità.

---

## Session Token

```text
GET /api/user/login?user=<user>&pass=<pass>&includeInfo=true
```

Restituisce un token di sessione che **scade** dopo il timeout di inattività dell'utente, per impostazione predefinita 30 minuti.

Con `includeInfo=true` la risposta include anche versione del server, dominio e permessi dell'utente.

---

## API Token

```text
GET /api/user/createToken?user=<user>&pass=<pass>&tokenName=<name>
```

Restituisce un token **non scadente**, pensato esattamente per l'automazione.

Le sue caratteristiche sono rilevanti per PIE.

* Non richiede rinnovo periodico, quindi l'Acquisition Flow non deve gestire il ciclo di vita della sessione.
* Eredita i permessi dell'utente che lo ha creato.
* Non consente di modificare password o profilo dell'utente.

La documentazione ufficiale raccomanda di creare un utente dedicato con permessi limitati.

Per PIE è sufficiente il permesso **Dashboard: View**.

Questa raccomandazione soddisfa direttamente il vincolo della Specification 04 secondo cui le credenziali restano confinate nell'Adapter, e riduce l'impatto di un'eventuale compromissione.

---

## Response Format

**Confermato.**

Ogni risposta è JSON e contiene la proprietà `status`.

| Valore          | Significato                                    |
| --------------- | ---------------------------------------------- |
| `ok`            | Chiamata riuscita                              |
| `error`         | Errore, con `errorMessage` e dettagli di debug |
| `invalid-token` | Sessione scaduta o token non valido            |
| `2fa-required`  | Autenticazione a due fattori richiesta         |

Nota rilevante per l'Adapter: **l'esito non è espresso dal codice di stato HTTP** ma dal corpo della risposta. Un errore applicativo può arrivare con HTTP 200.

Le risposte di errore includono `stackTrace` e `innerErrorMessage`, che non devono essere propagati oltre l'Adapter né finire nei log, in coerenza con le regole sulla registrazione di informazioni sensibili.

---

# Server Information

**Confermato.**

La versione del server si ottiene dalla risposta di `login` con `includeInfo=true`, nel campo `info.version`.

```text
GET /api/dashboard/metrics/json
```

Restituisce `uptimestamp`, `uptimeSeconds` e i contatori di vita del server.

La documentazione ufficiale marca questa chiamata come **sperimentale e soggetta a modifica**. Non va usata come dipendenza primaria.

---

# Statistics

**Confermato.**

```text
GET /api/dashboard/stats/get?type=LastHour&utc=true
```

Parametri principali.

* `type`: `LastHour`, `LastDay`, `LastWeek`, `LastMonth`, `LastYear`, `Custom`
* `start` e `end`: date ISO 8601, solo con `Custom`
* `utc`: restituisce le etichette temporali in UTC

Campi restituiti nell'oggetto `stats`.

```text
totalQueries, totalNoError, totalServerFailure, totalNxDomain,
totalRefused, totalAuthoritative, totalRecursive, totalCached,
totalBlocked, totalDropped, totalClients,
zones, cachedEntries, allowedZones, blockedZones,
allowListZones, blockListZones
```

La risposta contiene inoltre `protocolTypeChartData`, `queryTypeChartData` e versioni ridotte di `topClients`, `topDomains` e `topBlockedDomains`.

---

# Top Statistics

**Confermato.**

```text
GET /api/dashboard/stats/getTop?type=LastHour&statsType=TopClients&limit=1000
```

* `statsType`: `TopClients`, `TopDomains`, `TopBlockedDomains`
* `limit`: predefinito 1000
* `noReverseLookup`: disattiva la risoluzione inversa dei client

Struttura di un elemento `topClients`.

```json
{
  "name": "192.168.10.5",
  "domain": "server1.home",
  "hits": 463,
  "rateLimited": false
}
```

Il campo `domain` è il risultato di una risoluzione inversa e **può essere assente**.

Struttura di un elemento `topDomains` e `topBlockedDomains`.

```json
{
  "name": "edge.microsoft.com",
  "hits": 52
}
```

---

# Query Logs Application

L'app **Query Logs (Sqlite)** versione 9.1.1 è stata installata sull'istanza di prova e verificata.

Il suo `classPath` è `QueryLogsSqlite.App`.

Questa verifica era necessaria perché da essa dipende la possibilità di costruire `DomainActivity`.

---

## Installation

**Confermato.**

```text
GET /api/apps/downloadAndInstall?name=<nome>&url=<url>
```

L'installazione è completamente automatizzabile via API. L'URL del pacchetto proviene da `apps/listStoreApps`.

Il download è di circa 16 MB e richiede connettività verso l'esterno.

---

## Query API

**Confermato.**

```text
GET /api/logs/query?name=<nome app>&classPath=QueryLogsSqlite.App&...
```

La risposta ha la struttura seguente.

```json
{
  "pageNumber": 1,
  "totalPages": 4,
  "totalEntries": 90,
  "entries": [ ... ]
}
```

---

## Entry Structure

**Confermato.**

```json
{
  "rowNumber": 90,
  "timestamp": "2026-08-02T16:39:44.7068085Z",
  "clientIpAddress": "127.0.0.1",
  "protocol": "Udp",
  "responseType": "Blocked",
  "rcode": "NxDomain",
  "qname": "blocked-test.example",
  "qtype": "AAAA",
  "qclass": "IN",
  "answer": null
}
```

Ogni voce contiene contemporaneamente il client e il dominio: è la correlazione che le API del dashboard non forniscono.

---

## Supported Filters

**Tutti verificati e funzionanti.**

| Parametro         | Esito | Verifica                                          |
| ----------------- | ----- | ------------------------------------------------- |
| `pageNumber`      | ✔     | paginazione con `totalPages` e `totalEntries`      |
| `entriesPerPage`  | ✔     | fino a 1000 voci in una sola risposta              |
| `descendingOrder` | ✔     | ordinamento                                        |
| `clientIpAddress` | ✔     | filtro per dispositivo                             |
| `qname`           | ✔     | filtro per dominio, 6 voci su 90 per un dominio    |
| `qtype`           | ✔     | filtro per tipo di record, 45 voci su 90 per `A`   |
| `protocol`        | ✔     | filtro per protocollo di trasporto                 |
| `start` e `end`   | ✔     | filtro per intervallo temporale in ISO 8601 UTC    |

La presenza contemporanea del filtro temporale e del conteggio totale rende possibile l'**estrazione incrementale**: l'Adapter può richiedere soltanto le voci successive all'ultima acquisizione.

---

## Observed Value Sets

Valori osservati sul traffico di prova.

| Campo          | Valori osservati                     | Note                                                        |
| -------------- | ------------------------------------ | ----------------------------------------------------------- |
| `protocol`     | `Udp`                                | stesso vocabolario di `protocolTypeChartData`                |
| `responseType` | `Blocked`, `Cached`, `Recursive`     | coincide con `queryResponseChartData.labels`, che comprende anche `Authoritative` e `Dropped` |
| `rcode`        | `NoError`, `NxDomain`                | codici di risposta DNS                                       |
| `qtype`        | `A`, `AAAA`                          | tipi di record DNS                                           |
| `qclass`       | `IN`                                 |                                                              |

La coerenza fra `responseType` e le etichette del dashboard indica un vocabolario unico in tutto il prodotto.

`responseType: Blocked` accompagnato da `rcode: NxDomain` riflette l'impostazione `blockingType: NxDomain`.

---

## Application Configuration

**Confermato.** Configurazione predefinita, leggibile e modificabile via API.

```json
{
  "enableLogging": true,
  "maxQueueSize": 200000,
  "maxLogDays": 7,
  "maxLogRecords": 10000,
  "enableVacuum": false,
  "useInMemoryDb": false,
  "sqliteDbPath": "querylogs.db"
}
```

Tre elementi hanno conseguenze dirette sul disegno dell'Adapter.

**`maxLogRecords: 10000`** — la ritenzione predefinita è di diecimila record complessivi, non di sette giorni di traffico. Su una rete domestica reale corrisponde a meno di un'ora di attività. Il limite temporale di sette giorni è quindi teorico: prevale quasi sempre il limite sul numero di record.

**`maxQueueSize: 200000`** — la scrittura è bufferizzata in memoria e asincrona rispetto alla risoluzione. L'impatto sul throughput è quindi minore di quanto la sola avvertenza dell'app lasci supporre.

**`enableVacuum: false`** — il file SQLite non viene compattato automaticamente e può non ridursi dopo la cancellazione dei record.

Conseguenze.

* La frequenza di acquisizione deve essere **inferiore al tempo di rotazione del log**, altrimenti si perdono dati in modo silenzioso.
* L'Adapter dovrebbe leggere la configurazione dell'app e segnalare quando la ritenzione è insufficiente rispetto alla frequenza di acquisizione.
* La configurazione è modificabile via API, quindi l'installazione guidata può proporre valori adeguati.

---

# Mapping to the Unified Data Model

Legenda: **✔** disponibile, **~** derivabile, **✘** non ottenibile.

## DataSource

| Proprietà      |   | Origine                                            |
| -------------- | - | -------------------------------------------------- |
| `Version`      | ✔ | `login` → `info.version`                            |
| `Provider`     | ✔ | costante dell'Adapter                               |
| `Name`         | ~ | `login` → `info.dnsServerDomain`                    |
| `LastUpdate`   | ~ | orologio dell'Adapter al termine dell'acquisizione  |
| `Id`           | ~ | generato da PIE, non esiste lato Technitium         |
| `Status`       | ✘ | nessuno stato esplicito esposto dall'API            |
| `Capabilities` | — | vedi nota sotto                                     |

## Statistics

| Proprietà          |   | Origine                                              |
| ------------------ | - | ---------------------------------------------------- |
| `TotalQueries`     | ✔ | `stats.totalQueries`                                  |
| `BlockedQueries`   | ✔ | `stats.totalBlocked`                                  |
| `CachedQueries`    | ✔ | `stats.totalCached`                                   |
| `ActiveDevices`    | ✔ | `stats.totalClients`                                  |
| `FailedQueries`    | ~ | aggregazione ambigua, vedi Open Points                |
| `EncryptedQueries` | ~ | somma dei protocolli cifrati in `protocolTypeChartData` |
| `DnssecEnabled`    | ✔ | `settings/get` → `dnssecValidation`                   |
| `UniqueDomains`    | ✘ | l'API espone solo i primi N domini, non il totale     |

## Device

| Proprietà         |   | Origine                                          |
| ----------------- | - | ------------------------------------------------ |
| `IpAddress`       | ✔ | `topClients[].name`                               |
| `Hostname`        | ~ | `topClients[].domain`, spesso assente             |
| `DeviceId`        | ~ | generato da PIE                                   |
| `Status`          | ~ | derivabile dalla presenza nella finestra          |
| `FirstSeen`       | ✘ | non esposto                                       |
| `LastSeen`        | ✘ | non esposto                                       |
| `MacAddress`      | ✘ | non nel dashboard, forse nelle API DHCP           |
| `Vendor`          | ✘ | non esposto                                       |
| `OperatingSystem` | ✘ | non esposto                                       |

## Domain

| Proprietà     |   | Origine                                        |
| ------------- | - | ---------------------------------------------- |
| `Name`        | ✔ | `topDomains[].name`                             |
| `Occurrences` | ✔ | `topDomains[].hits`                             |
| `Category`    | ✘ | competenza del Threat Engine, non della sorgente |
| `Reputation`  | ✘ | competenza del Threat Engine, non della sorgente |
| `FirstSeen`   | ✘ | non esposto                                     |
| `LastSeen`    | ✘ | non esposto                                     |

## DomainActivity

Nessuna proprietà è ottenibile dalle API del dashboard.

**Tutte** sono ottenibili dall'app Query Logs, quando installata.

| Proprietà    |   | Origine con Query Logs                            |
| ------------ | - | ------------------------------------------------- |
| `DeviceId`   | ~ | risolto da `entries[].clientIpAddress`             |
| `Domain`     | ✔ | `entries[].qname`                                  |
| `QueryCount` | ~ | conteggio delle voci per coppia client e dominio   |
| `Blocked`    | ✔ | `entries[].responseType` uguale a `Blocked`        |
| `Protocol`   | ✔ | `entries[].protocol`                               |
| `FirstSeen`  | ~ | `timestamp` minimo del gruppo                      |
| `LastSeen`   | ~ | `timestamp` massimo del gruppo                     |

L'aggregazione avviene nell'Adapter: il Core riceve `DomainActivity` già consolidata e non vede mai la singola query.

---

# Open Points

Elementi che richiedono una decisione o un aggiornamento della documentazione.

---

## 1. DomainActivity non è costruibile

Questo è il rilievo più significativo della ricognizione.

`DomainActivity` rappresenta la relazione fra un Device e un Domain. Le API del dashboard espongono però `topClients` e `topDomains` come **aggregati separati e indipendenti**: dicono quante query ha fatto ciascun client e quante volte è stato richiesto ciascun dominio, ma non quale client abbia contattato quale dominio.

La correlazione richiede i log delle query.

I log in Technitium non fanno parte del server: richiedono l'installazione di una DNS App opzionale, tipicamente **Query Logs (Sqlite)**. Senza quella app, il dato non esiste.

Con l'app installata il problema è risolto: la verifica ha dimostrato che tutte le proprietà di `DomainActivity` sono ottenibili e che l'estrazione incrementale è supportata.

Conseguenze da valutare.

* La Specification 04 elenca i Logs fra i dati acquisiti con la formula "quando disponibili": la ricognizione conferma che quella cautela era fondata, ma la condizione va resa esplicita.
* Il Glossary definisce Domain Activity come entità di primo livello. Poiché la sorgente primaria non la fornisce di serie, occorre stabilire se sia un dato opzionale o se il supporto ai Query Logs diventi un prerequisito dichiarato.
* Altre Data Sources previste, fra cui Pi-hole e AdGuard Home, espongono i log per client in modo nativo. La limitazione è quindi specifica di Technitium e non del dominio applicativo: renderla un prerequisito generale contraddirebbe il principio di Backend Independence.

---

## 2. Aggregati temporali, non eventi

Technitium espone statistiche **per finestra temporale** — ultima ora, giorno, settimana, mese, anno — non un flusso di eventi.

Il Unified Data Model usa invece `firstSeen`, `lastSeen` e `occurrences`, che presuppongono osservazione continua.

Va documentato come l'Adapter debba interpretare la finestra: se `firstSeen` e `lastSeen` corrispondano ai limiti della finestra interrogata, oppure se sia PIE a mantenere lo storico accumulando snapshot successivi.

La seconda ipotesi appare più coerente con il concetto di NetworkSnapshot, ma è una decisione architetturale che non spetta all'Adapter.

---

## 3. Liste troncate

`getTop` restituisce al massimo `limit` elementi, predefinito 1000.

Non esiste una chiamata che restituisca l'elenco completo dei domini osservati.

Ne consegue che `Statistics.UniqueDomains` non è calcolabile con esattezza e che l'insieme dei Domain è per costruzione parziale.

Va deciso se il valore vada omesso, marcato come approssimato, o calcolato su base diversa.

---

## 4. Definizione di FailedQueries

Technitium distingue `totalServerFailure`, `totalNxDomain`, `totalRefused` e `totalDropped`.

Il Unified Data Model prevede un unico `FailedQueries`.

L'aggregazione non è ovvia: un NXDOMAIN è una risposta legittima, non un errore del servizio. Sommarlo agli errori altererebbe la valutazione della Network Integrity nel calcolo del NPSS.

Va definito quali contatori concorrano a `FailedQueries`.

---

## 5. Campi non definiti, esito della ricognizione

Stato dei quattro campi "esterni" rimasti aperti dopo M2.2.

| Campo                     | Esito                                                                                                    |
| ------------------------- | -------------------------------------------------------------------------------------------------------- |
| `DataSource.Capabilities` | **Risolto, con correzione.** Vedi la nota che segue. |
| `DomainActivity.Protocol` | **Risolto.** `protocolTypeChartData.labels` restituisce etichette in PascalCase, valore osservato `Udp`. L'insieme completo deducibile dai flag di configurazione è `Udp`, `Tcp`, `Tls`, `Https`, `Quic`. |
| `DataSource.Status`       | **Aperto.** Nessuno stato esposto dall'API. Deve essere prodotto dall'Adapter a partire dall'esito della comunicazione. |
| `Device.Status`           | **Aperto.** Nessuno stato esposto. Derivabile dalla presenza del client nella finestra osservata.          |

### Correzione sulla natura di Capabilities

Una prima lettura di questa ricognizione aveva ipotizzato di valorizzare `DataSource.Capabilities` con i flag di configurazione di Technitium, quali `enableDnsOverTls` o `dnssecValidation`.

L'ipotesi era **errata** e confondeva due concetti distinti.

* Le funzionalità del prodotto esterno, come il supporto a DNSSEC o ai protocolli cifrati, sono **dati da analizzare**. Concorrono al calcolo del NPSS e trovano posto in `Statistics` e nella configurazione acquisita.
* Una capability è invece un'informazione **strutturale**: dichiara quali entità del Unified Data Model quella sorgente è in grado di fornire, e serve al Core per sapere quali analisi può eseguire.

Il vocabolario corretto è quindi costituito dai nomi delle entità del modello, come definito nella Data Model Specification.

Per Technitium le capability sono `Statistics`, `Device` e `Domain` al livello base, con l'aggiunta di `DomainActivity` quando è attivo il componente di registrazione delle query.

---

La ricognizione ha chiuso due campi su quattro.

I due rimanenti sono passati da "non definiti" a "non ottenibili dalla sorgente", che è un'informazione diversa e più utile: indica che il vocabolario deve essere definito da PIE, non cercato altrove.

---

## 6. Scelta della finestra temporale

Emerso dalle osservazioni sul traffico reale.

`LastDay` ha restituito `topDomains` vuoto in presenza di traffico, mentre `LastHour` conteneva il dettaglio corretto.

L'Acquisition Flow deve quindi definire quale finestra interrogare per ciascun tipo di dato, e non può assumere che una finestra più ampia contenga tutto ciò che contiene una più stretta.

Questo va confermato con osservazioni prolungate prima di diventare una regola documentata.

---

# Live Instance Findings

Risultati ottenuti interrogando un'istanza reale di Technitium 15.4.

---

## Settings

**Confermato.**

```text
GET /api/settings/get
```

Restituisce oltre centoventi proprietà di configurazione. Le rilevanti per PIE sono le seguenti.

| Proprietà                      | Valore predefinito osservato | Uso in PIE                          |
| ------------------------------ | ---------------------------- | ----------------------------------- |
| `version`                      | `15.4`                       | `DataSource.Version`                |
| `dnsServerDomain`              | nome host del server         | `DataSource.Name`                   |
| `dnssecValidation`             | `true`                       | `Statistics.DnssecEnabled`          |
| `enableDnsOverTls`             | `false`                      | capability                          |
| `enableDnsOverHttps`           | `false`                      | capability                          |
| `enableDnsOverQuic`            | `false`                      | capability                          |
| `enableDnsOverHttp`            | `false`                      | capability                          |
| `enableDnsOverHttp3`           | `false`                      | capability                          |
| `qnameMinimization`            | `true`                       | capability, indicatore di privacy   |
| `eDnsClientSubnet`             | `false`                      | capability, indicatore di privacy   |
| `enableBlocking`               | `true`                       | stato del filtraggio                |
| `blockingType`                 | `NxDomain`                   | modalità di blocco                  |
| `blockListUrls`                | **vuoto**                    | stato delle blocklist               |
| `blockListUpdateIntervalHours` | `24`                         | frequenza di aggiornamento          |
| `logQueries`                   | `false`                      | disponibilità dei log delle query   |
| `enableInMemoryStats`          | `false`                      | le statistiche sono persistite      |
| `maxStatFileDays`              | `365`                        | ritenzione delle statistiche        |
| `recursion`                    | `AllowOnlyForPrivateNetworks`| configurazione del resolver         |

---

## DNS Apps

**Confermato.**

```text
GET /api/apps/list
```

Su un'installazione appena creata restituisce un array **vuoto**.

Nessuna DNS App è installata di serie.

Insieme a `logQueries` impostato su `false`, questo conferma in modo definitivo che i Query Logs non sono disponibili su un'installazione standard.

```text
GET /api/apps/listStoreApps
```

Restituisce **ventisette** app disponibili.

La chiamata **fallisce se il server non ha connettività verso l'esterno**: durante la prima raccolta ha restituito un errore di risoluzione per `go.technitium.com`, e ha funzionato solo dopo la configurazione di un forwarder.

L'Acquisition Flow non deve quindi dipendere da questa chiamata.

Le app rilevanti per il progetto sono le seguenti.

| App                     | Rilevanza                                                   |
| ----------------------- | ----------------------------------------------------------- |
| `Query Logs (Sqlite)`   | Abilita i log delle query, prerequisito di Domain Activity   |
| `Query Logs (MySQL)`    | Variante con database MySQL o MariaDB                        |
| `Query Logs (PostgreSQL)` | Variante con database PostgreSQL                           |
| `Query Logs (SQL Server)` | Variante con Microsoft SQL Server                          |
| `Log Exporter`          | Esporta i log verso file, endpoint HTTP o Syslog             |
| `Advanced Blocking`     | Regole di blocco per gruppi di client                        |
| `DNS Block List (DNSBL)`| Blocco basato su liste DNSBL                                 |

La descrizione ufficiale di `Query Logs (Sqlite)` avverte che il logging delle query ha un impatto sul throughput.

Questo conferma in modo definitivo che l'accesso a Domain Activity richiede l'installazione esplicita di una app e comporta un costo prestazionale che l'utente deve accettare consapevolmente.

---

## Logs

**Confermato.**

```text
GET /api/logs/list
```

Restituisce `logFiles`, con `fileName` e `size`.

Si tratta dei **log diagnostici del server**, non delle query. Le impostazioni indicano `loggingType: File` e `maxLogFileDays: 365`.

---

## Hierarchical Browsing

**Confermato.**

```text
GET /api/blocked/list?domain=&direction=down
GET /api/cache/list?domain=&direction=down
```

Entrambe restituiscono `{ domain, zones, records }`.

Non sono elenchi piatti ma **navigatori gerarchici**: si scende un livello alla volta partendo dalla radice.

Enumerare i domini bloccati o l'intera cache richiede quindi di percorrere un albero con chiamate successive.

Questo ha un impatto diretto sul disegno dell'Adapter e sul costo dell'acquisizione.

---

## Fixed Value Sets

**Confermato.**

`queryResponseChartData.labels` espone un insieme fisso.

```text
Authoritative, Recursive, Cached, Blocked, Dropped
```

`mainChartData.datasets` espone le serie temporali.

```text
Total, No Error, Server Failure, NX Domain, Refused,
Authoritative, Recursive, Cached, Blocked, Dropped, Clients
```

---

## Response Envelope

**Confermato.**

Ogni risposta contiene, oltre a `status`, anche un campo `server` con il nome del server che ha risposto.

---

## DHCP

**Confermato.**

```text
GET /api/dhcp/leases/list
```

Risponde correttamente e restituisce `leases`, vuoto in assenza di uno scope DHCP configurato.

Resta una possibile origine di `Device.MacAddress`, ma solo quando Technitium è utilizzato anche come server DHCP. Non è quindi una fonte su cui l'Adapter possa contare.

---

## DNS Client

**Confermato.**

```text
GET /api/dnsClient/resolve?server=this-server&domain=<domain>&type=A&protocol=Udp
```

Restituisce `{ result, rawResponses }`.

Rilievo importante: le risoluzioni effettuate tramite questa API **non vengono conteggiate nelle statistiche del dashboard**. La cache si popola, ma `totalQueries` resta a zero.

Ne consegue che questa chiamata non è utilizzabile per generare traffico osservabile, né va considerata parte dell'attività di rete misurata.

---

## Traffic Observations

Osservazioni raccolte generando traffico DNS reale contro l'istanza.

---

### Protocol Labels

**Confermato.**

`protocolTypeChartData.labels` restituisce le etichette dei protocolli di trasporto in PascalCase.

Valore osservato con traffico UDP.

```json
{ "labels": ["Udp"], "datasets": [{ "data": [2] }] }
```

Le etichette compaiono solo per i protocolli effettivamente utilizzati. L'insieme completo dei valori possibili si deduce dai flag di configurazione: `Udp`, `Tcp`, `Tls`, `Https`, `Quic`.

Questo chiude il formato di `DomainActivity.Protocol`.

---

### Query Types

**Confermato.**

`queryTypeChartData.labels` restituisce i tipi di record DNS interrogati.

```json
{ "labels": ["A", "AAAA"], "datasets": [{ "data": [1, 1] }] }
```

L'insieme non è fisso: dipende dal traffico osservato.

---

### Top Clients

**Confermato.** Struttura popolata.

```json
{
  "name": "127.0.0.1",
  "domain": "localhost",
  "hits": 7,
  "rateLimited": false
}
```

Il campo `domain` proviene da una risoluzione inversa ed è assente quando non disponibile.

---

### Time Window Behaviour

**Rilievo significativo, da approfondire.**

Con lo stesso stato del server e nello stesso istante, due finestre temporali hanno restituito dati diversi.

| Finestra   | `totalQueries` | `topClients` | `topDomains`         |
| ---------- | -------------- | ------------ | -------------------- |
| `LastHour` | 2              | 1 elemento   | `example.com`, 2 hit |
| `LastDay`  | 7              | 1 elemento   | **vuoto**            |

La finestra più ampia riporta più query e mantiene le statistiche per client, ma **perde completamente il dettaglio per dominio**.

L'ipotesi più plausibile è che le statistiche aggregate su periodi lunghi siano costruite a partire da file consolidati periodicamente, e che il dettaglio per dominio dell'ora in corso non vi sia ancora confluito.

Le conseguenze per l'Adapter sono rilevanti.

* La scelta della finestra temporale **non è indifferente**: determina quali dati esistono davvero.
* Interrogare `LastDay` può restituire zero domini pur in presenza di traffico.
* L'acquisizione dei domini sembra affidabile solo su `LastHour`.

Questo comportamento va confermato con osservazioni su un periodo più lungo prima di essere assunto come regola.

---

### Custom Interval

**Verificato in M3.4.**

L'interrogazione con intervallo personalizzato **conserva il dettaglio per dominio**.

Il confronto fra le statistiche acquisite tramite intervallo personalizzato e quelle mostrate dalla console del server ha riscontrato corrispondenza esatta.

| Grandezza        | Acquisita | Console | Nota                                         |
| ---------------- | --------- | ------- | -------------------------------------------- |
| Query totali     | 30        | 32      | differenza dovuta a due query successive      |
| Query dalla cache| 20        | 22      | stesse due query                              |
| Domini distinti  | 5         | 5       | corrispondenza                                |
| Client attivi    | 1         | 1       | corrispondenza                                |

La perdita del dettaglio per dominio riguarda quindi la finestra predefinita relativa all'ultimo giorno, non l'intervallo personalizzato.

La strategia di acquisizione incrementale resta praticabile.

---

# Impact on Existing Specifications

Nessuna Specification è stata modificata da questo documento.

Le modifiche che la ricognizione suggerisce sono le seguenti, tutte da approvare.

| Specification              | Modifica suggerita                                                            |
| -------------------------- | ------------------------------------------------------------------------------ |
| 04 - Technitium Integration | Dichiarare la dipendenza dei Logs da una DNS App opzionale                       |
| 04 - Technitium Integration | Documentare autenticazione tramite API Token e permesso Dashboard View           |
| 05 - Data Model            | Chiarire la semantica di `firstSeen`, `lastSeen` e `occurrences` su finestre     |
| 05 - Data Model            | Definire quali contatori concorrano a `FailedQueries`                            |
| 09 - Network Privacy       | Valutare la disponibilità condizionata di Domain Activity                        |

---

# Related Specifications

* 00 - Glossary
* 04 - Technitium Integration
* 05 - Data Model
* 11 - Backend

---

# Sources

* Technitium DNS Server API Documentation, repository ufficiale `TechnitiumSoftware/DnsServer`, file `APIDOCS.md`
* Technitium DNS Server, sito ufficiale e sezione Help
