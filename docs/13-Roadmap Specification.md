# 13 - Roadmap

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Project Roadmap Specification

**Version:** 1.0.5

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce la roadmap ufficiale del progetto **TivuStream Privacy Intelligence Engine (PIE)**.

La roadmap identifica le principali fasi di sviluppo, gli obiettivi di ogni milestone e la progressione prevista del progetto.

---

# Objectives

La roadmap ha i seguenti obiettivi.

* pianificare lo sviluppo;
* definire le milestone;
* mantenere una progressione coerente;
* facilitare la gestione del progetto.

---

# Current Status

**Documentation Release:** 1.0.5

**Project Status:** Documentation Completed

**Development Status:** Not Started

---

# Development Phases

## Phase 1

### Foundation

Obiettivi.

* completamento della documentazione;
* definizione dell'architettura;
* definizione delle API;
* definizione del Unified Data Model.

**Status:** Completed

---

## Phase 2

### Backend Core

Obiettivi.

* implementazione del Core;
* implementazione del Unified Data Model;
* implementazione delle REST API;
* implementazione del sistema di configurazione.

**Status:** Planned

---

## Phase 3

### Adapter Layer

Obiettivi.

* Adapter Manager;
* Technitium Adapter;
* comunicazione con le HTTP API;
* normalizzazione dei dati.

**Status:** Planned

---

## Phase 4

### Intelligence Modules

Obiettivi.

* Threat Engine;
* Device Engine;
* Alert Engine;
* Recommendation Engine;
* NPSS Engine.

**Status:** Planned

---

## Phase 5

### Frontend

Obiettivi.

* Dashboard;
* Devices;
* Domains;
* Threats;
* Alerts;
* Recommendations;
* Statistics.

**Status:** Planned

---

## Phase 6

### Reporting

Obiettivi.

* Report PDF;
* Report CSV;
* esportazione JSON;
* storico.

**Status:** Planned

---

## Phase 7

### Testing

Obiettivi.

* unit test;
* integration test;
* performance test;
* security test.

**Status:** Planned

---

## Phase 8

### Beta Release

Obiettivi.

* rilascio Beta;
* raccolta feedback;
* correzione bug;
* ottimizzazioni.

**Status:** Planned

---

## Phase 9

### Stable Release

Obiettivi.

* Versione 1.0;
* documentazione aggiornata;
* rilascio pubblico.

**Status:** Planned

---

# Milestones

| Milestone | Description           | Status    |
| --------- | --------------------- | --------- |
| M1        | Documentation Release | Completed |
| M2        | Backend Core          | Planned   |
| M3        | Technitium Adapter    | Planned   |
| M4        | Intelligence Modules  | Planned   |
| M5        | Frontend              | Planned   |
| M6        | Reports               | Planned   |
| M7        | Testing               | Planned   |
| M8        | Beta Release          | Planned   |
| M9        | Stable Release        | Planned   |

---

# Version Strategy

Il progetto utilizza il versionamento semantico.

Formato.

```text id="1ikjlwm"
MAJOR.MINOR.PATCH
```

Esempio.

```text id="c7i4bn4"
1.0.0
```

---

# Documentation Roadmap

La documentazione segue una versione indipendente dal codice.

Documentation Release 1.0.0 rappresenta la baseline originale del progetto.

La Documentation Release corrente è indicata nella sezione Current Status.

Ogni Specification possiede inoltre una propria versione, aggiornata soltanto quando il documento viene modificato.

Lo storico completo è registrato in `CHANGELOG.md`.

---

# Development Principles

Ogni nuova funzionalità deve:

* rispettare l'architettura definita;
* utilizzare il Unified Data Model;
* mantenere la compatibilità con le API;
* essere documentata.

---

# Release Criteria

Una release può essere considerata stabile quando:

* tutte le milestone previste sono completate;
* i test sono superati;
* la documentazione è aggiornata;
* le API sono stabili.

---

# Future Evolution

Le evoluzioni future del progetto verranno pianificate attraverso nuove versioni della presente Roadmap Specification.

---

# Related Specifications

* 01 - Vision
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 10 - Frontend
* 11 - Backend
* 12 - Installation
