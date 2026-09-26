# 06 - API

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** API Specification

**Version:** 1.5.0

**Status:** Approved

**Last Updated:** 2026-09-26

---

# Purpose

Questa specifica definisce le API pubbliche del Privacy Intelligence Engine.

Le API rappresentano l'unica interfaccia ufficiale tra il Core e le applicazioni che utilizzano il motore.

---

# Objectives

Le API sono progettate per essere:

* semplici;
* coerenti;
* indipendenti dal backend;
* versionabili;
* facilmente estendibili.

---

# Architecture

Le REST API appartengono al Query Flow.

Una richiesta API restituisce esclusivamente risultati già elaborati dal Core.

```text id="jlwmxt"
Frontend Application

↓

REST API

↓

Risultati prodotti dal Privacy Intelligence Engine
```

Nessun endpoint attiva una comunicazione verso gli Adapter o le Data Sources.

L'acquisizione dei dati appartiene all'Acquisition Flow ed è descritta nella Architecture Specification.

---

# API Versioning

Le API utilizzano il versionamento nel percorso.

Esempio.

```text id="u4e5vi"
/api/v1/
```

Ogni modifica incompatibile genera una nuova Major Version.

---

# Protocol

Le API utilizzano:

* HTTPS
* JSON UTF-8
* REST

---

# Standard Response

Ogni risposta utilizza una struttura comune.

```json
{
  "success": true,
  "apiVersion": "v1",
  "timestamp": "...",
  "data": {}
}
```

---

# Standard Error

```json
{
  "success": false,
  "error": {
    "code": "...",
    "message": "..."
  }
}
```

Un errore può portare un campo `reason` che ne precisa il motivo in forma leggibile da un programma, per esempio `TooShort` per una password rifiutata. Quando non c'è, il campo è assente e non `null`.

---

# Observed Period

Le risposte che descrivono ciò che è stato osservato dichiarano **a quale periodo si riferiscono**.

```json
{
  "period": { "start": "...", "end": "..." },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "domains": []
}
```

Il campo `period` è assente quando nessuna osservazione è stata ancora registrata.

I due conteggi non sono ridondanti. Un'installazione accesa da sei ore che dichiarasse «ultime 24 ore» direbbe il falso: `periodsRequested` è l'intervallo chiesto, `periodsObserved` è quello che esiste davvero.

La ragione non è formale. Un elenco vuoto senza il proprio periodo è ambiguo: chi legge non distingue «la rete non ha contattato nulla» da «l'ora in corso è appena cominciata». La prima è un'affermazione sulla rete, la seconda sul momento in cui si guarda, e presentarle allo stesso modo è un'informazione falsa.

Il requisito discende dalla regola Absent Versus Unmeasurable della Network Privacy Specification.

**Endpoint interessati.** `/domains`, `/domains/{domain}`, `/devices` e `/statistics` coprono la stessa finestra, le ultime ventiquattro ore, e la dichiarano con gli stessi tre campi. Una sola finestra per tutto ciò che descrive la rete: due pagine che mostrassero intervalli diversi senza dirlo si contraddirebbero.

---

# Dashboard Endpoint

## GET

```
/api/v1/dashboard
```

Restituisce il riepilogo completo dello stato della rete.

Include:

* NPSS
* statistiche
* dispositivi
* alert
* minacce
* raccomandazioni

---

# NPSS Endpoint

## GET

```
/api/v1/npss
```

Restituisce il Network Privacy & Security Score.

Comprende:

* punteggio;
* dettaglio;
* storico;
* trend.

---

# Devices Endpoint

## GET

```
/api/v1/devices
```

Restituisce i dispositivi osservati nelle **ultime ventiquattro ore**, insieme all'intervallo effettivamente coperto (vedi Observed Period).

```json
{
  "period": { "start": "...", "end": "..." },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "devices": [
    {
      "deviceId": "...",
      "hostname": "laptop-maria",
      "ipAddress": "192.168.1.20",
      "macAddress": "aa:bb:cc:dd:ee:ff",
      "vendor": null,
      "operatingSystem": null,
      "identityBasis": "HardwareAddress",
      "status": "Active",
      "firstSeen": "...",
      "lastSeen": "...",
      "observationQuality": "PeriodBounded"
    }
  ]
}
```

Oggi restituisce i dispositivi del solo periodo più recente, cioè dell'ora in corso, e non lo dice: pochi minuti dopo lo scoccare dell'ora l'elenco è quasi vuoto, ed è indistinguibile da una rete in cui quasi nessun dispositivo è attivo.

### Aggregation

Un elemento per identificativo di dispositivo nella finestra.

| Proprietà | Regola |
| --- | --- |
| `hostname`, `ipAddress`, `macAddress`, `vendor`, `operatingSystem`, `identityBasis` | Dal periodo **più recente** in cui il dispositivo compare, come la classificazione dei domini |
| `firstSeen` | Il più antico fra i periodi inclusi |
| `lastSeen` | Il più recente fra i periodi inclusi |
| `observationQuality` | La qualità meno precisa fra quelle aggregate |
| `status` | `Active`: un dispositivo compare nella finestra perché vi ha prodotto traffico |

Ordinamento per indirizzo, come oggi.

L'aggregazione è lecita perché l'identificativo è derivato in modo deterministico dall'indirizzo o dall'indirizzo hardware. Un dispositivo riconosciuto dall'indirizzo di rete che ha cambiato indirizzo nella finestra compare due volte, e `identityBasis` lo dichiara.

### What The List Does Not Carry

L'entità Device del Data Model comprende `domainActivities` e `threats`. L'elenco **non li porta**: oggi viaggiano come elenchi vuoti, e un elenco vuoto si legge «nessuna attività, nessuna minaccia» quando significa «non compreso in questa risposta». L'attività di un dispositivo appartiene al suo dettaglio, `/devices/{id}`, non ancora implementato.

---

## GET

```
/api/v1/devices/{id}
```

Restituisce il dettaglio di un singolo dispositivo.

---

# Threats Endpoint

## GET

```
/api/v1/threats
```

Restituisce tutte le minacce rilevate.

Supporta filtri.

* categoria;
* severità;
* intervallo temporale.

---

## GET

```
/api/v1/threats/{id}
```

Restituisce il dettaglio di una specifica minaccia.

---

# Alerts Endpoint

## GET

```
/api/v1/alerts
```

Restituisce gli Alert generati dal Core.

Supporta filtri per:

* severità;
* stato;
* categoria.

---

# Recommendations Endpoint

## GET

```
/api/v1/recommendations
```

Restituisce tutte le Recommendations prodotte dal sistema.

---

# Domains Endpoint

## GET

```
/api/v1/domains
```

Restituisce i domini osservati nelle **ultime ventiquattro ore**, insieme all'intervallo effettivamente coperto.

```json
{
  "period": { "start": "2026-08-30T22:00:00+00:00", "end": "2026-08-31T22:00:00+00:00" },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "domains": []
}
```

Un elenco vuoto accompagnato dall'intervallo significa che in quell'arco non è stato osservato alcun dominio. Senza l'intervallo la stessa risposta non direbbe nulla di verificabile.

---

### Aggregation

I periodi di osservazione sono fissi e **non si sovrappongono**, quindi le occorrenze di uno stesso dominio in periodi diversi si sommano senza contare due volte il medesimo traffico.

È la scelta compiuta nella Persistence Specification che rende lecita questa somma. Con finestre mobili di acquisizione l'aggregazione sarebbe stata impossibile.

| Proprietà              | Regola                                                        |
| ---------------------- | -------------------------------------------------------------- |
| `occurrences`          | Somma dei periodi inclusi                                       |
| `firstSeen`            | Il più antico fra i periodi inclusi                             |
| `lastSeen`             | Il più recente fra i periodi inclusi                            |
| `observationQuality`   | La qualità meno precisa fra quelle aggregate                    |
| Classificazione        | Quella del periodo **più recente** in cui il dominio compare    |

L'ultima riga è una scelta e va motivata. Ogni periodo conserva la classificazione che era possibile dare allora; presentando la più recente si mostra ciò che si sa **adesso**, e l'età della lista dichiarata insieme alla categoria dice quanto quel «adesso» sia recente.

Mostrare la classificazione più antica, o una sintesi delle diverse classificazioni ricevute, produrrebbe un'affermazione che nessun periodo ha mai fatto.

---

### The Last Hour Is In Progress

L'intervallo restituito comprende il periodo corrente, che non è concluso.

La sua fine è quindi un istante futuro, e l'interfaccia dichiara che l'ultima ora è ancora in corso anziché presentarla come osservata per intero.

---

## GET

```
/api/v1/domains/{domain}
```

Restituisce il dettaglio del dominio **sullo stesso intervallo dell'elenco**: le ultime ventiquattro ore, insieme all'intervallo effettivamente coperto.

```json
{
  "period": { "start": "2026-08-30T22:00:00+00:00", "end": "2026-08-31T22:00:00+00:00" },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "domain": { "name": "example.com", "category": "Tracking", "occurrences": 42, "...": "..." },
  "activityAccess": "Available",
  "activities": [
    {
      "device": {
        "deviceId": "...",
        "hostname": "laptop-maria",
        "ipAddress": "192.168.1.20",
        "identityBasis": "HardwareAddress"
      },
      "queryCount": 30,
      "blocked": false,
      "protocol": "Udp",
      "firstSeen": "2026-08-31T08:00:00+00:00",
      "lastSeen": "2026-08-31T19:00:00+00:00",
      "observationQuality": "PeriodBounded"
    }
  ]
}
```

Comprende:

* categoria, con confidenza, lista e data della lista;
* reputazione;
* frequenza;
* attività per dispositivo, quando la sorgente la offre e il ruolo di chi chiede lo consente.

### Same Interval As The List

Il dettaglio copre lo stesso intervallo di `/domains` e lo dichiara con gli stessi tre campi (vedi Observed Period).

Il vincolo nasce da un difetto. Leggendo il solo periodo più recente, un dominio che l'elenco mostra perché contattato dieci ore prima risponderebbe `DomainNotObserved`: l'elenco afferma che il dominio è stato osservato, il dettaglio lo nega. Due risposte dello stesso motore non si contraddicono.

`domain` è aggregato con le regole della sezione Aggregation di `/domains`, e quindi coincide con la riga dell'elenco.

Un dominio assente dall'intervallo risponde `404 DomainNotObserved`.

### Activity Aggregation

Un elemento di `activities` per ogni combinazione di **dispositivo, esito (bloccato o no) e trasporto** nell'intervallo. Un dispositivo che ha raggiunto il dominio sia direttamente sia attraverso un blocco compare due volte, perché sono due fatti diversi.

| Proprietà | Regola |
| --- | --- |
| `queryCount` | Somma dei periodi inclusi |
| `firstSeen` | Il più antico fra i periodi inclusi |
| `lastSeen` | Il più recente fra i periodi inclusi |
| `observationQuality` | La qualità meno precisa fra quelle aggregate |

Ordinamento per `queryCount` decrescente.

La somma è lecita per la stessa ragione di quella dei domini, periodi che non si sovrappongono, e perché l'identificativo del dispositivo è derivato in modo deterministico: lo stesso indirizzo, o lo stesso indirizzo hardware, produce lo stesso identificativo in ogni periodo. Quanto quell'identità sia solida lo dichiara `identityBasis`.

### Device Identification

L'identificativo del dispositivo, da solo, non dice nulla a chi legge. Ogni elemento porta con sé `hostname` (quando la sorgente lo fornisce), `ipAddress` e `identityBasis`, presi dal periodo **più recente** dell'intervallo in cui il dispositivo compare: la stessa regola della classificazione.

Il dettaglio non rimanda a `/devices` perché `/devices` descrive il solo periodo più recente: un dispositivo attivo dieci ore prima resterebbe senza nome.

Un dispositivo può comparire nell'attività senza comparire fra i dispositivi di alcun periodo dell'intervallo: la sorgente li riporta in due resoconti diversi, e quello dei dispositivi è limitato. Allora `hostname`, `ipAddress` e `identityBasis` sono `null` tutti e tre, e l'attività resta: il traffico c'è stato, chi l'ha prodotto non è noto oltre l'identificativo, e nulla viene supposto.

Questi campi esistono solo con `Available`. Per un `Viewer` nessuna informazione sui dispositivi compare nella risposta.

### Activity Access

Il campo `activityAccess` dichiara che cosa significa l'elenco `activities`.

| Valore | Significato |
| --- | --- |
| `Available` | La sorgente offre l'attività e chi chiede può leggerla. `activities` è l'elenco |
| `Unavailable` | La sorgente non offre l'attività. `activities` è vuoto e non significa nulla |
| `Withheld` | La sorgente la offre, ma il ruolo di chi chiede non la comprende. `activities` è vuoto e non significa nulla |

Un elenco vuoto per mancanza di diritto e un elenco vuoto perché nessun dispositivo ha contattato il dominio sono affermazioni diverse. Il campo esiste perché non vengano presentate allo stesso modo.

Se la sorgente offra l'attività lo stabilisce l'acquisizione più recente, come oggi.

`Available` con un elenco vuoto significa che la sorgente non ha registrato attività per dispositivo verso questo dominio nell'intervallo, per esempio perché i periodi in cui compare sono stati acquisiti quando non la offriva. Il dominio è stato osservato: manca il dettaglio, non il traffico.

---

# Statistics Endpoint

## GET

```
/api/v1/statistics
```

Restituisce le statistiche aggregate della rete nelle **ultime ventiquattro ore**, insieme all'intervallo effettivamente coperto (vedi Observed Period).

```json
{
  "period": { "start": "...", "end": "..." },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "statistics": {
    "totalQueries": 1000,
    "blockedQueries": 100,
    "cachedQueries": 400,
    "failedQueries": 9,
    "uniqueDomains": 250,
    "uniqueDomainsQuality": "LowerBound",
    "activeDevices": 3,
    "encryptedQueries": 16,
    "dnssecEnabled": true
  }
}
```

Oggi restituisce le statistiche del solo periodo più recente, cioè dell'ora in corso, e non lo dice.

Quando nella finestra non esiste alcun periodo, `period` e `statistics` sono `null`. Una serie di zeri direbbe che la rete non ha interrogato nulla, quando non è stato osservato nulla. Oggi quel caso risponde `503 AcquisitionPending`, «nessuna acquisizione registrata», che è falso quando esistono acquisizioni più vecchie della finestra.

### Aggregation

| Proprietà | Regola |
| --- | --- |
| `totalQueries`, `blockedQueries`, `cachedQueries`, `failedQueries`, `encryptedQueries` | Somma dei periodi inclusi, lecita perché non si sovrappongono |
| `uniqueDomains` | Il maggiore fra il valore orario più alto e i nomi distinti registrati nella finestra |
| `uniqueDomainsQuality` | `LowerBound` quando la finestra comprende più di un periodo; altrimenti quella del periodo |
| `activeDevices` | Il maggiore fra il valore orario più alto e gli identificativi distinti registrati nella finestra |
| `dnssecEnabled` | Dal periodo più recente: è configurazione, non traffico |

I domini distinti di ore diverse **non si sommano**: lo stesso dominio contattato in due ore verrebbe contato due volte. Il valore orario più alto e i nomi distinti conservati sono entrambi limiti inferiori (la sorgente restituisce elenchi troncati), e il maggiore dei due è ancora un limite inferiore.

Per i dispositivi vale lo stesso ragionamento, con la riserva dell'identità: un dispositivo riconosciuto dall'indirizzo di rete che cambia indirizzo conta due volte, esattamente come in `/devices`.

Il punteggio non cambia: il NPSS continua a essere calcolato come descritto nella NPSS Specification.

---

# Timeline Endpoint

## GET

```
/api/v1/timeline
```

Restituisce gli eventi ordinati cronologicamente.

---

# Reports Endpoint

## POST

```
/api/v1/reports
```

Genera un nuovo report.

Formati supportati.

* PDF
* CSV
* JSON

---

# Sources Endpoint

## GET

```
/api/v1/sources
```

Restituisce l'elenco delle Data Sources registrate.

---

# Health Endpoint

## GET

```
/api/v1/health
```

Restituisce lo stato operativo del sistema.

Comprende:

* Core
* Adapter
* Backend
* API

---

# Settings Endpoint

## GET

```
/api/v1/settings
```

Restituisce la configurazione corrente.

---

## PUT

```
/api/v1/settings
```

Aggiorna la configurazione del sistema.

---

# Authentication

L'autenticazione è definita dall'**Authentication Specification**: account locali con ruolo, sessione tramite cookie, rifiuto per impostazione predefinita.

Ogni endpoint richiede una sessione valida. **Fanno eccezione soltanto `POST /api/v1/setup` e `POST /api/v1/auth/login`**, che servono a ottenerla e non restituiscono alcun dato sulla rete o sul sistema.

Gli endpoint che la gestiscono (`/setup`, `/auth/*`, `/accounts`) e i codici di errore che introduce (`SetupRequired`, `AuthenticationRequired`, `AuthenticationFailed`, `Forbidden`, `PasswordChangeRequired`, `TransportNotSecure`, `OriginNotAllowed`, `TooManyAttempts` e altri) sono descritti in quel documento, che ne è la fonte.

---

# Authorization

Ogni endpoint **dichiara il ruolo minimo** che richiede. Un endpoint che non lo dichiara richiede `Administrator`: dimenticare una dichiarazione produce un rifiuto, non un'apertura.

Ciò che un ruolo non può leggere non viene omesso in silenzio: la risposta lo dichiara. Vedi Activity Access.

---

# Error Handling

Gli errori sono classificati nelle seguenti categorie.

* Validation
* Authentication
* Authorization
* Backend
* Network
* Internal

Ogni errore utilizza un codice identificativo univoco.

Due codici valgono per ogni endpoint:

| Stato | Codice | Categoria | Quando |
| --- | --- | --- | --- |
| 400 | `RequestUnreadable` | Validation | Il corpo della richiesta non si può leggere: non è JSON, non ha la forma attesa, contiene testo non valido |
| 500 | `InternalError` | Internal | Un guasto che il codice non ha previsto |

Entrambi usano la struttura comune **in ogni ambiente di esecuzione**. `InternalError` non dice nulla della causa né della richiesta: la pagina diagnostica del framework, che elenca le intestazioni e con esse il cookie di sessione, non viene mai mostrata (Authentication Specification, V11). La causa va nel registro dell'applicazione, senza segreti.

---

# Logging

Le richieste API possono essere registrate a fini diagnostici.

I log non devono contenere dati sensibili.

---

# Rate Limiting

Le API supportano limitazioni configurabili sul numero di richieste.

---

# Compatibility

Le API mantengono la retrocompatibilità all'interno della stessa Major Version.

---

# Design Principles

Le API seguono i seguenti principi.

* una responsabilità per endpoint;
* risposte prevedibili;
* indipendenza dal backend;
* semplicità;
* versionamento esplicito;
* estendibilità.

---

# Constraints

Le API:

* non espongono direttamente i backend;
* non restituiscono dati non normalizzati;
* non contengono logica di business;
* non attivano l'Acquisition Flow;
* rappresentano l'unico punto di accesso ufficiale al Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 09 - Network Privacy
