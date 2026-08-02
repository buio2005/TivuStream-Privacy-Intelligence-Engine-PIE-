# 06 - API

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** API Specification

**Version:** 1.0.1

**Status:** Approved

**Last Updated:** 2026-08-02

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

Restituisce l'elenco dei domini osservati.

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
* dispositivi coinvolti.

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

Il sistema supporta autenticazione centralizzata.

Le modalità implementative vengono definite durante lo sviluppo del backend.

---

# Authorization

Ogni endpoint verifica i permessi dell'utente prima dell'elaborazione della richiesta.

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
