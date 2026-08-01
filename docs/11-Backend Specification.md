# 11 - Backend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Backend Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

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

```text id="1ntz3h"
                Frontend

                    │

                    ▼

              REST API Layer

                    │

      ┌─────────────┼─────────────┐

      ▼             ▼             ▼

 Authentication   Core Engine   Configuration

                    │

      ┌─────────────┼─────────────┐

      ▼             ▼             ▼

 Adapter      Logging Service   Scheduler

                    │

                    ▼

              Data Sources
```

---

# Main Components

Il Backend è composto dai seguenti componenti.

* REST API
* Core Engine
* Adapter Manager
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

Gestisce l'autenticazione degli utenti e dei servizi autorizzati.

L'autenticazione è indipendente dalle Data Sources.

---

# Authorization

Ogni richiesta viene verificata prima dell'elaborazione.

Il Backend determina le autorizzazioni necessarie per ciascun endpoint.

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

# Configuration Files

Il Backend utilizza configurazioni centralizzate.

Le configurazioni devono essere indipendenti dal codice.

---

# Data Flow

```text id="kfz0c6"
REST API

↓

Validation

↓

Authentication

↓

Authorization

↓

Core Engine

↓

Adapter

↓

Data Source
```

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
* comunica con le Data Sources esclusivamente tramite Adapter.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 05 - Data Model
* 06 - API
* 10 - Frontend
