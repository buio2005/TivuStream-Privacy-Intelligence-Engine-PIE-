# TivuStream Privacy Intelligence Engine (PIE)

> **The intelligence layer for privacy-first network analysis.**

---

# TivuStream Privacy Intelligence Engine

TivuStream Privacy Intelligence Engine (PIE) è il progetto alla base di una nuova generazione di strumenti TivuStream dedicati alla privacy, alla sicurezza e all'analisi della rete.

L'obiettivo non è sviluppare un nuovo DNS Server o sostituire software già esistenti, ma costruire un motore capace di raccogliere informazioni provenienti da diverse sorgenti, analizzarle e trasformarle in dati comprensibili, utili e immediatamente fruibili dall'utente.

Il primo modulo ufficiale sviluppato sopra il Privacy Intelligence Engine sarà **Network Privacy**.

---

# Obiettivo del progetto

Realizzare una piattaforma self-hosted che permetta di monitorare e comprendere il comportamento della rete locale attraverso un'interfaccia semplice, moderna e orientata alla privacy.

Il progetto è pensato per utenti che desiderano conoscere lo stato della propria rete senza dover interpretare dati tecnici complessi.

---

# Filosofia

Il progetto segue alcuni principi fondamentali:

* Privacy First
* Local First
* Self Hosted
* Architettura modulare
* Nessuna telemetria
* Massima semplicità per l'utente finale

Ogni funzionalità dovrà contribuire a rendere la privacy più comprensibile e accessibile.

---

# Architettura

Il Privacy Intelligence Engine rappresenta il livello di analisi del sistema.

Le Data Sources raccolgono i dati.

Gli Adapter li convertono nel Unified Data Model.

Il Core li interpreta.

Le applicazioni TivuStream li presentano all'utente.

```text
                Data Sources
                     │
                     ▼
                  Adapters
                     │
                     ▼
            Unified Data Model
                     │
                     ▼
     TivuStream Privacy Intelligence Engine
                     │
      ┌──────────────┼──────────────┐
      ▼              ▼              ▼
 Threat Engine  Device Engine   NPSS Engine
      │              │              │
      └──────────────┼──────────────┘
                     ▼
             Recommendation Engine
                     │
                     ▼
                  REST API
                     │
                     ▼
          TivuStream Applications
```

Il sistema utilizza due flussi distinti.

L'**Acquisition Flow** acquisisce periodicamente i dati dalle Data Sources.

Il **Query Flow** serve le richieste del Frontend restituendo esclusivamente risultati già elaborati.

La descrizione completa è contenuta nella Architecture Specification.

---

# Primo modulo

## Network Privacy

Network Privacy rappresenta la prima applicazione sviluppata utilizzando il Privacy Intelligence Engine.

Il suo compito è analizzare il traffico DNS della rete locale e fornire informazioni semplici riguardo a:

* sicurezza DNS
* privacy della rete
* tracker
* malware
* dispositivi
* attività DNS
* configurazione
* suggerimenti

Per la gestione DNS il progetto utilizza **Technitium DNS Server** come backend.

Technitium rimane il motore DNS.

TivuStream fornisce l'intelligenza, l'analisi e l'interfaccia utente.

---

# Componenti previsti

Il progetto sarà composto da moduli indipendenti.

## Core

I moduli del Core utilizzano tutti il suffisso **Engine**.

* Threat Engine
* Device Engine
* NPSS Engine
* Alert Engine
* Recommendation Engine

## Integration

* Adapter Manager
* Technitium Adapter
* Unified Data Model

## Interface

* REST API
* Network Privacy

---

# Obiettivi principali

* Rendere comprensibili dati complessi.
* Aiutare gli utenti a migliorare la privacy della rete.
* Fornire analisi chiare e immediate.
* Costruire una piattaforma estensibile nel tempo.
* Integrare più sorgenti di dati mantenendo un'unica esperienza utente.

---

# Tecnologie

## Backend

* ASP.NET Core
* C#
* REST API
* SQLite

## Frontend

* Vue 3
* TypeScript
* Pinia
* Vite

## Data Source

* Technitium DNS Server (prima integrazione supportata)

## Piattaforme

* Linux
* Windows
* Docker

Ogni dipendenza introdotta nel progetto viene documentata con nome, versione, licenza e scopo.

---

# Roadmap

| Milestone | Descrizione           | Stato     |
| --------- | --------------------- | --------- |
| M1        | Documentation Release | Completed |
| M2        | Backend Core          | Planned   |
| M3        | Technitium Adapter    | Planned   |
| M4        | Core Modules          | Planned   |
| M5        | Frontend              | Planned   |
| M6        | Reports               | Planned   |
| M7        | Testing               | Planned   |
| M8        | Beta Release          | Planned   |
| M9        | Stable Release        | Planned   |

Il dettaglio delle fasi è contenuto nella Roadmap Specification.

---

# Stato del progetto

**Documentation Release:** 1.3.0

**Project Status:** Documentation Completed

**Development Status:** Not Started

---

# Documentazione

La documentazione tecnica completa è disponibile nella cartella `docs/`.

Ogni documento descrive uno specifico componente dell'architettura e costituisce il riferimento ufficiale per lo sviluppo del progetto.

Il progetto segue un modello **Documentation First**: la documentazione rappresenta la fonte autorevole, il codice la implementa.

Prima di contribuire consultare nell'ordine:

1. `README.md`
2. `PROJECT_CONTEXT.md`
3. `AI_DEVELOPMENT_GUIDE.md`
4. le Specification in `docs/`

Le modifiche alla documentazione sono registrate in `CHANGELOG.md`.

---

# Licenza

La licenza definitiva del progetto non è ancora stata scelta.

La decisione verrà presa prima della prima Beta pubblica.

Le componenti open source integrate mantengono le rispettive licenze originali.

Technitium DNS Server rappresenta un software indipendente: PIE ne utilizza esclusivamente le API pubbliche e non ne costituisce un fork.

La politica completa è descritta nella License Specification.
