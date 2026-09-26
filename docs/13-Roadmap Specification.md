# 13 - Roadmap

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Project Roadmap Specification

**Version:** 1.3.10

**Status:** Approved

**Last Updated:** 2026-09-26

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

**Documentation Release:** 1.12.0

**Project Status:** In Development

**Development Status:** In Progress. Il repository non è pubblico. Dei criteri di Beta è soddisfatto quello sull'autenticazione (Specification 18, milestone A1–A7); gli altri no, o non sono stati verificati: vedi Release Criteria.

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

**Status:** In Progress

**Situazione:** Esistono il Unified Data Model, il sistema di configurazione, la persistenza e sei endpoint di lettura. Mancano l'autenticazione (definita nella Specification 18, milestone A1–A7), HTTPS (la specifica Transport Security non esiste ancora) e il consolidamento a livelli della Specification 16.

---

## Phase 3

### Adapter Layer

Obiettivi.

* Adapter Manager;
* Technitium Adapter;
* comunicazione con le HTTP API;
* normalizzazione dei dati.

**Status:** In Progress

**Situazione:** Esistono il Technitium Adapter (livello base, Domain Activity, configurazione della sorgente) e la comunicazione con le HTTP API. Mancano l'implementazione dell'Adapter Manager, di cui esiste la sola interfaccia, e la prova su una seconda versione di Technitium.

---

## Phase 4

### Core Modules

Obiettivi.

* Threat Engine;
* Device Engine;
* Alert Engine;
* Recommendation Engine;
* NPSS Engine.

**Status:** In Progress

**Situazione:** Esistono il motore di classificazione e il motore NPSS. Mancano Device, Alert e Recommendation Engine: per questo l'area Device Health non è misurabile e quindici punti del punteggio restano esclusi.

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

**Status:** In Progress

**Situazione:** Esistono Dashboard e Domini. Mancano Devices, Threats, Alerts, Recommendations e Statistics.

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

**Status:** In Progress

**Situazione:** Esistono le prove ai confini di Adapter, persistenza, API e Frontend, oltre a quelle del Core. Mancano le prove di integrazione, di prestazioni e di sicurezza.

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
| M2        | Backend Core          | In Progress |
| M3        | Technitium Adapter    | In Progress |
| M4        | Core Modules          | In Progress |
| M5        | Frontend              | In Progress |
| M6        | Reports               | Planned   |
| M7        | Testing               | In Progress |
| M8        | Beta Release          | Planned   |
| M9        | Stable Release        | Planned   |

## Corrispondenza con il CHANGELOG

Le voci del CHANGELOG numerate `M5.x` (storage, schema e persistenza) e `M6.x` (Frontend) precedono l'allineamento con questa Roadmap e non coincidono con i suoi numeri.

| Voce del CHANGELOG | Milestone della Roadmap |
| --- | --- |
| `M2.x`, `M3.x`, `M4.x` | M2, M3, M4 |
| `M5.x` (persistenza) | M2, Backend Core |
| `M6.x` (Frontend) | M5, Frontend |

La cronologia non viene riscritta. Le voci future usano i numeri di questa Roadmap.

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

I criteri sono **verificabili**: ciascuno si può dichiarare soddisfatto o non
soddisfatto senza interpretazione.

Un criterio espresso come «le API sono stabili» non è verificabile e non
distingue un rilascio pronto da uno che sembra pronto.

---

## Repository Publication

Rendere pubblico il codice **non** coincide con rilasciare il prodotto.

| Criterio | Verifica |
| --- | --- |
| Documentazione in inglese | Nessun documento in `/docs` resta in italiano |
| Licenza dichiarata | `LICENSE.md` presente, nota in testa ai sorgenti |
| Debiti dichiarati | Il README elenca ciò che manca, compresa l'assenza di autenticazione |
| Nessun segreto versionato | Nessun token, nessuna credenziale nella cronologia git |

Il README dichiara esplicitamente che il progetto **non è pronto all'uso** e
per quali ragioni.

Un progetto che si fonda sul dichiarare ciò che non sa può dichiarare anche
ciò che non è ancora.

---

## Beta Release

Il prodotto è installabile e utilizzabile da qualcuno che non lo ha scritto.

| Criterio | Verifica |
| --- | --- |
| Autenticazione | Nessun endpoint che restituisca dati sulla rete o sul sistema risponde senza credenziali valide. Fanno eccezione `setup` e `login`, che le stabiliscono e non restituiscono nulla sulla rete (Specification 18) |
| Trasporto cifrato | L'interfaccia è raggiungibile in HTTPS |
| Installazione | Una persona estranea al progetto installa seguendo la procedura documentata, su una macchina pulita |
| Ritenzione | Il consolidamento a livelli previsto dalla Specification 16 è attivo |
| Prove ai confini | Adapter, API e persistenza hanno ciascuno almeno una prova |
| Nessun avviso di compilazione | Backend e Frontend compilano puliti |
| Secondo ambiente | Il sistema è stato eseguito su Linux oltre che su Windows |

---

## Stable Release

| Criterio | Verifica |
| --- | --- |
| Copertura del punteggio | Nessuna area del NPSS resta priva di definizioni calcolabili |
| Sezioni dell'interfaccia | Ogni voce di navigazione prevista dalla Specification 09 esiste |
| Esecuzione prolungata | Il sistema ha girato senza interruzioni per almeno trenta giorni |
| Seconda versione della sorgente | Il Technitium Adapter è stato provato su due versioni differenti |
| Avviso di licenza | L'interfaccia mostra copyright, assenza di garanzia e come consultare la licenza |
| Nessun debito non dichiarato | Ogni lacuna nota compare nel README o nel CHANGELOG |

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
