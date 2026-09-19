# 11 - Backend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Backend Specification

**Version:** 1.4.0

**Status:** Approved

**Last Updated:** 2026-09-19

---

# Purpose

Questa specifica definisce l'architettura del Backend del Privacy Intelligence Engine.

Il Backend rappresenta il livello applicativo che ospita il Core, gli Adapter e le REST API, coordinando l'intero flusso di elaborazione del sistema.

---

# Objectives

Il Backend ha i seguenti obiettivi.

* acquisire dati dalle Data Sources;
* coordinare il Core;
* esporre le REST API;
* garantire modularità;
* garantire scalabilità;
* mantenere l'indipendenza dai backend supportati.

---

# Scope

Il Backend comprende:

* REST API;
* Adapter;
* Core Engine;
* servizi interni;
* configurazione;
* autenticazione;
* logging.

Il Frontend non fa parte del Backend.

---

# High Level Architecture

Il Backend ospita due percorsi indipendenti.

Il Query Flow serve le richieste del Frontend.

L'Acquisition Flow acquisisce i dati dalle Data Sources.

```text id="1ntz3h"
        Query Flow                    Acquisition Flow

         Frontend                        Scheduler

             │                               │

             ▼                               ▼

       REST API Layer                  Adapter Manager

             │                               │

             ▼                               ▼

      Authentication                      Adapter

             │                               │

             ▼                               ▼

        Authorization                   Data Sources

             │                               │

             ▼                               ▼

  Risultati elaborati            Unified Data Model

                                             │

                                             ▼

                                        Core Engine

                                             │

                                             ▼

                                     Risultati elaborati
```

I servizi trasversali Configuration Manager, Logging Service e Report Service sono utilizzati da entrambi i percorsi.

---

# Main Components

Il Backend è composto dai seguenti componenti.

* REST API
* Core Engine
* Adapter Manager
* Storage
* Authentication
* Configuration Manager
* Logging Service
* Scheduler
* Report Service

---

# REST API Layer

Espone tutte le funzionalità pubbliche del sistema.

Ogni richiesta viene validata prima di raggiungere il Core.

---

# Core Engine

Coordina tutti i moduli del Privacy Intelligence Engine.

Gestisce:

* analisi;
* classificazione;
* correlazione;
* NPSS;
* Alert;
* Recommendations.

---

# Adapter Manager

Gestisce il ciclo di vita degli Adapter.

Ogni Adapter comunica con una specifica Data Source.

L'Adapter Manager orchestra inoltre l'Acquisition Flow.

Le sue responsabilità comprendono:

* registrazione degli Adapter;
* esecuzione del ciclo di acquisizione;
* raccolta dei dati normalizzati;
* consegna del Unified Data Model al Core Engine.

L'Adapter Manager non esegue alcuna analisi.

---

# Adapter Contract

Il contratto che ogni Adapter deve rispettare è composto da un'interfaccia di base e da un insieme di interfacce segregate per capacità.

---

## Base Interface

L'interfaccia di base porta l'identità dell'Adapter e la descrizione della Data Source.

La descrizione comprende versione, stato operativo e capacità effettivamente disponibili.

---

## Capability Interfaces

Ogni capacità corrisponde a un'interfaccia dedicata.

| Interfaccia             | Capability       |
| ----------------------- | ---------------- |
| Statistics Source       | `Statistics`     |
| Device Source           | `Device`         |
| Domain Source           | `Domain`         |
| Domain Activity Source  | `DomainActivity` |

Un Adapter implementa esclusivamente le interfacce corrispondenti ai dati che è in grado di fornire.

Questa segregazione risponde al principio di Interface Segregation ed elimina la necessità di implementare metodi non supportati.

---

## Potential and Effective Capabilities

Le interfacce implementate esprimono ciò che un Adapter **può** fornire.

Le capability dichiarate nella descrizione esprimono ciò che la Data Source fornisce **effettivamente** nella sua configurazione corrente.

Le due informazioni non coincidono necessariamente: una Data Source può offrire un tipo di dato soltanto dopo l'attivazione di un componente facoltativo.

Vale la seguente regola.

> Un Adapter non può dichiarare una capability della quale non implementa l'interfaccia.

L'Adapter Manager verifica questa condizione al momento della registrazione.

---

## Acquisition Window

Ogni acquisizione riceve un intervallo temporale esplicito.

L'intervallo non viene mai assunto dall'Adapter, poiché finestre differenti non contengono necessariamente le stesse informazioni.

L'intervallo consente inoltre l'acquisizione incrementale, richiedendo soltanto quanto successivo al ciclo precedente.

---

## Error Handling

Gli Adapter convertono gli errori della propria Data Source in un'eccezione dedicata.

Il resto del sistema non gestisce mai errori espressi nel vocabolario di uno specifico backend.

I dettagli diagnostici prodotti dalla Data Source non oltrepassano l'Adapter.

---

# Storage

Conserva nel tempo le acquisizioni e i risultati prodotti dal Core.

La conservazione è necessaria perché le Data Sources applicano una ritenzione propria: ciò che non viene conservato al momento dell'acquisizione è perduto in modo definitivo.

Responsabilità.

* conservazione delle acquisizioni convertite nel Unified Data Model;
* conservazione dei risultati del Core;
* applicazione della ritenzione a livelli;
* gestione della versione dello schema.

Vincoli.

* Lo Storage non esegue analisi e non modifica i dati che riceve.
* Il Core non conosce lo Storage.
* Il Unified Data Model non contiene alcun elemento di persistenza.
* Il Frontend non raggiunge mai lo Storage direttamente.

Il funzionamento è descritto nella Persistence Specification.

---

# Configuration Manager

Gestisce la configurazione del sistema.

Comprende.

* backend;
* Adapter;
* autenticazione;
* aggiornamenti;
* impostazioni generali.

---

# Authentication

Gestisce l'autenticazione degli utenti: account locali con ruolo, sessioni, configurazione iniziale e recupero. La definizione è nell'**Authentication Specification**.

L'autenticazione è indipendente dalle Data Sources.

I servizi autorizzati, citati in una versione precedente di questo documento, non sono previsti finché non esiste un servizio che li usi.

---

# Authorization

Ogni richiesta viene verificata prima dell'elaborazione.

Ogni endpoint dichiara il ruolo minimo che richiede. Un endpoint che non lo dichiara richiede `Administrator`. Vedi l'Authentication Specification.

---

# Logging Service

Centralizza la registrazione degli eventi.

Comprende.

* eventi di sistema;
* errori;
* attività amministrative;
* eventi API.

---

# Scheduler

Gestisce le operazioni pianificate.

Esempi.

* aggiornamento blocklist;
* sincronizzazione dati;
* pulizia cache;
* generazione report.

---

# Report Service

Genera report utilizzando i dati prodotti dal Core.

Formati previsti.

* PDF
* CSV
* JSON

---

# Solution Structure

Il Backend è organizzato in progetti distinti.

La separazione in progetti rende i vincoli architetturali verificabili in fase di compilazione anziché affidarli alla sola disciplina.

```text
backend/
├── TivuStream.Pie.sln
├── Directory.Build.props
├── src/
│   ├── TivuStream.Pie.Model/
│   ├── TivuStream.Pie.Core/
│   ├── TivuStream.Pie.Adapters/
│   ├── TivuStream.Pie.Adapters.Technitium/
│   ├── TivuStream.Pie.Storage/
│   └── TivuStream.Pie.Api/
└── tests/
    ├── TivuStream.Pie.Core.Tests/
    └── TivuStream.Pie.Storage.Tests/
```

---

## Project Responsibilities

| Progetto                             | Responsabilità                                      |
| ------------------------------------ | --------------------------------------------------- |
| `TivuStream.Pie.Model`               | Unified Data Model                                   |
| `TivuStream.Pie.Core`                | Core e relativi Engine                               |
| `TivuStream.Pie.Adapters`            | Contratti degli Adapter e Adapter Manager            |
| `TivuStream.Pie.Adapters.Technitium` | Technitium Adapter                                   |
| `TivuStream.Pie.Storage`             | Persistenza, schema e migrazioni                     |
| `TivuStream.Pie.Api`                 | Host applicativo, REST API e composition root        |
| `TivuStream.Pie.Core.Tests`          | Verifica delle regole di analisi                     |
| `TivuStream.Pie.Storage.Tests`       | Verifica di ciò che viene letto e scritto            |

Un progetto di test verifica **una regola dichiarata nelle Specification**, non un dettaglio implementativo.

La separazione dei progetti di test segue quella dei progetti verificati: un progetto di test referenzia soltanto ciò che verifica.

---

## Project References

```text
Model            → nessun riferimento

Core             → Model

Adapters         → Model

Adapters.Technitium → Adapters

Storage          → Model

Api              → Core, Adapters, Adapters.Technitium, Storage

Core.Tests       → Core

Storage.Tests    → Storage
```

Le seguenti regole costituiscono vincoli architetturali.

* Il progetto `Model` non referenzia alcun altro progetto.
* Il progetto `Core` non referenzia gli Adapter né l'host.
* Il progetto `Adapters` non referenzia il Core.
* Soltanto `Api`, in quanto composition root, referenzia un Adapter concreto.

L'aggiunta di un riferimento in violazione di queste regole rende il vincolo architetturale immediatamente visibile in fase di build.

---

# Configuration Files

Il Backend utilizza configurazioni centralizzate.

Le configurazioni devono essere indipendenti dal codice.

La configurazione di build condivisa risiede in `Directory.Build.props` e si applica a tutti i progetti del Backend.

I warning sono trattati come errori, in coerenza con i criteri di qualità del progetto.

---

# Data Flow

Il Backend gestisce due flussi distinti.

---

## Query Flow

Percorso seguito da una richiesta proveniente dal Frontend.

```text id="kfz0c6"
REST API

↓

Validation

↓

Authentication

↓

Authorization

↓

Risultati prodotti dal Core Engine
```

Il Query Flow non raggiunge mai gli Adapter né le Data Sources.

---

## Acquisition Flow

Percorso seguito dall'acquisizione periodica dei dati.

```text
Scheduler

↓

Adapter Manager

↓

Adapter

↓

Data Source

↓

Unified Data Model

↓

Core Engine

↓

Risultati
```

L'Adapter Manager orchestra l'acquisizione e invoca il Core Engine fornendo dati già espressi nel Unified Data Model.

Il Core Engine non invoca mai un Adapter.

Le acquisizioni sono allineate a periodi di osservazione fissi. Una nuova osservazione dello stesso periodo sostituisce la precedente anziché aggiungersi: due osservazioni di intervalli sovrapposti descrivono in parte lo stesso traffico e non sono sommabili.

Il criterio è definito nella Persistence Specification.

---

# Error Handling

Gli errori vengono classificati.

Categorie.

* Validation
* Authentication
* Authorization
* Backend
* Network
* Internal

Ogni errore viene registrato dal Logging Service.

---

# Logging

Il sistema registra gli eventi necessari al funzionamento e alla diagnostica.

I log non devono contenere informazioni sensibili non strettamente necessarie.

---

# Performance

Il Backend privilegia.

* modularità;
* elaborazione incrementale;
* riutilizzo dei dati;
* riduzione delle operazioni duplicate.

---

# Scalability

L'architettura permette di aggiungere:

* nuovi Adapter;
* nuovi Engine;
* nuove REST API;
* nuove Data Sources.

Il Core rimane invariato.

---

# Security

Il Backend:

* protegge le credenziali;
* utilizza HTTPS;
* valida gli input;
* controlla i permessi;
* isola le Data Sources dal Frontend.

---

# Design Principles

Il Backend segue i seguenti principi.

* modularità;
* indipendenza;
* estendibilità;
* semplicità;
* separazione delle responsabilità.

---

# Constraints

Il Backend:

* non contiene logica di presentazione;
* non dipende da uno specifico backend;
* utilizza esclusivamente il Unified Data Model;
* comunica con le Data Sources esclusivamente tramite Adapter;
* mantiene il Core Engine isolato dagli Adapter e dalle Data Sources.

Il progetto che implementa il Core Engine non referenzia né gli Adapter né l'host delle REST API.

Questo isolamento rende il vincolo verificabile in fase di compilazione.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 05 - Data Model
* 06 - API
* 10 - Frontend
