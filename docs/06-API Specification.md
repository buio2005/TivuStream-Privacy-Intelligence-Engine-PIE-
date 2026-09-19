# 06 - API

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** API Specification

**Version:** 1.3.1

**Status:** Approved

**Last Updated:** 2026-09-19

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

**Endpoint interessati.** `/domains` lo dichiara. `/devices` e `/statistics` presentano la stessa ambiguità e verranno allineati.

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

Restituisce l'elenco dei dispositivi.

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

Restituisce il dettaglio del dominio.

Comprende:

* categoria;
* reputazione;
* frequenza;
* attività per dispositivo, quando la sorgente la offre e il ruolo di chi chiede lo consente.

### Activity Access

Il campo `activityAccess` dichiara che cosa significa l'elenco `activities`.

| Valore | Significato |
| --- | --- |
| `Available` | La sorgente offre l'attività e chi chiede può leggerla. `activities` è l'elenco |
| `Unavailable` | La sorgente non offre l'attività. `activities` è vuoto e non significa nulla |
| `Withheld` | La sorgente la offre, ma il ruolo di chi chiede non la comprende. `activities` è vuoto e non significa nulla |

Un elenco vuoto per mancanza di diritto e un elenco vuoto perché nessun dispositivo ha contattato il dominio sono affermazioni diverse. Il campo esiste perché non vengano presentate allo stesso modo.

---

# Statistics Endpoint

## GET

```
/api/v1/statistics
```

Restituisce le statistiche aggregate della rete.

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
