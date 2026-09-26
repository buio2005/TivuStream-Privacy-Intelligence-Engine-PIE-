# 02 - Architecture

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Architecture Specification

**Version:** 1.1.0

**Status:** Approved

**Last Updated:** 2026-09-26

---

# Purpose

Questa specifica definisce l'architettura generale del Privacy Intelligence Engine (PIE) e le relazioni tra i principali componenti del sistema.

---

# Architectural Overview

L'architettura del progetto è suddivisa in livelli indipendenti.

Ogni livello possiede responsabilità specifiche e comunica esclusivamente attraverso interfacce ben definite.

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
         Privacy Intelligence Engine
                         │
      ┌──────────────────┼──────────────────┐
      ▼                  ▼                  ▼
Threat Engine     Device Engine      NPSS Engine
      │                  │                  │
      └──────────────────┼──────────────────┘
                         ▼
              Recommendation Engine
                         │
                         ▼
                    REST API Layer
                         │
                         ▼
                 Frontend Applications
```

---

# Architectural Layers

## Data Sources

Livello responsabile della produzione dei dati.

Il Privacy Intelligence Engine non dipende da una specifica sorgente dati.

---

## Adapter Layer

Ogni Data Source utilizza un Adapter dedicato.

L'Adapter converte i dati nel formato interno definito dal Unified Data Model.

---

## Unified Data Model

Il Data Model rappresenta il linguaggio comune utilizzato dal Core.

Ogni componente interno comunica esclusivamente attraverso questo modello.

---

## Privacy Intelligence Engine

Il Core del sistema.

Coordina tutte le attività di analisi e produce gli oggetti utilizzati dalle applicazioni.

---

## REST API Layer

Espone le informazioni elaborate dal Core.

Rappresenta l'unico punto di accesso ufficiale ai dati.

---

## Frontend

Visualizza le informazioni prodotte dal Core.

Il Frontend non esegue elaborazioni.

In un'installazione i suoi file compilati sono **serviti dal motore**, sullo stesso indirizzo dell'API: un solo indirizzo, un solo certificato, nessuna regola fra origini diverse (Transport Security Specification). Il Frontend continua a comunicare con il Core esclusivamente attraverso le REST API.

---

# Core Components

Il Privacy Intelligence Engine è composto dai seguenti moduli.

* Threat Engine
* Device Engine
* NPSS Engine
* Recommendation Engine
* Alert Engine

Ogni modulo possiede una responsabilità specifica.

---

# Data Flow

Il sistema utilizza due flussi distinti.

I due flussi non si sovrappongono e possiedono responsabilità differenti.

---

## Acquisition Flow

Rappresenta l'acquisizione periodica dei dati.

Viene innescato dallo Scheduler e orchestrato dall'Adapter Manager.

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

Privacy Intelligence Engine

↓

Risultati
```

Il Core riceve esclusivamente dati già espressi nel Unified Data Model.

Il Core non partecipa all'orchestrazione dell'acquisizione.

---

## Query Flow

Rappresenta una richiesta proveniente dal Frontend.

```text
Frontend

↓

REST API

↓

Risultati prodotti dal Core
```

Il Query Flow non attiva alcuna comunicazione verso gli Adapter o le Data Sources.

Le richieste del Frontend restituiscono esclusivamente risultati già elaborati.

---

# Responsibilities

## Data Sources

Producono informazioni.

---

## Adapters

Convertono i dati nel formato interno.

---

## Core

Analizza.

Correla.

Classifica.

Valuta.

---

## REST API

Espone i dati.

---

## Frontend

Visualizza le informazioni.

---

# Communication

I moduli del Core comunicano attraverso il modello dati condiviso.

Nessun modulo comunica direttamente con il Frontend o con i backend esterni.

---

# Scalability

L'architettura consente l'aggiunta di:

* nuove Data Sources;
* nuovi Adapter;
* nuovi Engine;
* nuove applicazioni.

L'estensione del sistema non modifica l'architettura principale.

---

# Backend Independence

Il Core non contiene dipendenze verso Technitium o altri provider.

Ogni integrazione viene implementata esclusivamente attraverso il relativo Adapter.

---

# Design Principles

L'architettura segue i seguenti principi.

* Modularità
* Indipendenza
* Scalabilità
* Semplicità
* Estendibilità
* Riutilizzabilità

---

# Architectural Constraints

Le seguenti regole costituiscono vincoli architetturali.

* Il Frontend comunica esclusivamente con le REST API.
* Il Core non comunica direttamente con le Data Sources.
* Il Core non orchestra l'acquisizione dei dati.
* L'acquisizione è orchestrata dall'Adapter Manager e innescata dallo Scheduler.
* Il Query Flow non raggiunge mai le Data Sources.
* Ogni Data Source implementa un Adapter dedicato.
* Tutte le elaborazioni vengono eseguite dal Core.
* Il Unified Data Model rappresenta l'unico formato dati interno.
* Nessuna logica di business è presente nel Frontend.
* Nessuna logica di presentazione è presente nel Core.

---

# Related Specifications

* 00 - Glossary
* 01 - Vision
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
