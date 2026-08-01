# 02 - Architecture

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Architecture Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

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

---

# Core Components

Il Privacy Intelligence Engine è composto dai seguenti moduli.

* Threat Engine
* Device Engine
* Network Privacy & Security Score Engine (NPSS)
* Recommendation Engine
* Alert Engine

Ogni modulo possiede una responsabilità specifica.

---

# Data Flow

Il flusso delle informazioni segue sempre lo stesso percorso.

```text
Data Source

↓

Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine

↓

REST API

↓

Frontend
```

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
