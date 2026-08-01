# 10 - Frontend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Frontend Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce l'architettura e i principi del Frontend utilizzato dalle applicazioni sviluppate sopra il Privacy Intelligence Engine.

Il Frontend rappresenta esclusivamente il livello di presentazione del sistema.

---

# Objectives

Il Frontend ha i seguenti obiettivi.

* visualizzare i dati prodotti dal Core;
* offrire un'interfaccia semplice e moderna;
* garantire un'esperienza utente coerente;
* mantenere la completa separazione dalla logica di business.

---

# Scope

Il Frontend:

* utilizza esclusivamente le REST API del Core;
* non comunica direttamente con le Data Sources;
* non implementa algoritmi di analisi;
* non modifica il Unified Data Model.

---

# Architecture

```text id="h9v7o1"
User

↓

Frontend

↓

REST API

↓

Privacy Intelligence Engine
```

---

# Application Structure

Il Frontend è suddiviso in moduli indipendenti.

* Dashboard
* Devices
* Domains
* Threats
* Alerts
* Recommendations
* Statistics
* Reports
* Settings

Ogni modulo rappresenta una vista indipendente.

---

# Dashboard

La Dashboard costituisce il punto di ingresso dell'applicazione.

Visualizza le informazioni principali prodotte dal Core.

---

# Layout

Il layout deve essere responsivo.

L'interfaccia deve adattarsi a:

* Desktop;
* Tablet;
* Smartphone.

---

# Navigation

La navigazione deve essere semplice e coerente.

Le principali sezioni dell'applicazione devono essere sempre raggiungibili.

---

# Components

Il Frontend utilizza componenti riutilizzabili.

Esempi.

* Cards
* Tables
* Charts
* Timeline
* Filters
* Search
* Notifications
* Dialogs

---

# Data Presentation

Le informazioni vengono presentate privilegiando:

* chiarezza;
* leggibilità;
* sintesi.

Le informazioni tecniche sono disponibili solo quando richieste.

---

# Data Refresh

Il Frontend aggiorna i dati utilizzando le REST API.

L'aggiornamento può essere:

* manuale;
* automatico.

---

# State Management

Lo stato dell'applicazione viene mantenuto localmente.

Il Frontend non modifica i dati prodotti dal Core.

---

# Error Handling

Gli errori vengono presentati in maniera comprensibile.

L'interfaccia evita messaggi tecnici quando non necessari.

---

# Accessibility

L'interfaccia deve rispettare i principali criteri di accessibilità.

Particolare attenzione viene dedicata a:

* contrasto;
* leggibilità;
* navigazione da tastiera;
* responsività.

---

# Internationalization

L'architettura supporta la localizzazione dell'interfaccia.

Le traduzioni vengono gestite separatamente dal codice.

---

# Themes

Il sistema supporta temi grafici configurabili.

La gestione del tema non modifica il comportamento dell'applicazione.

---

# Performance

Il Frontend privilegia:

* caricamento rapido;
* rendering efficiente;
* riduzione delle richieste HTTP;
* riutilizzo dei componenti.

---

# Security

Il Frontend:

* non memorizza credenziali delle Data Sources;
* non espone informazioni sensibili;
* comunica esclusivamente tramite HTTPS.

---

# Extensibility

Nuovi componenti possono essere aggiunti senza modificare quelli esistenti.

L'architettura privilegia la modularità.

---

# Design Principles

Il Frontend segue i seguenti principi.

* semplicità;
* uniformità;
* modularità;
* leggibilità;
* accessibilità;
* indipendenza dal backend.

---

# Constraints

Il Frontend:

* non contiene logica di business;
* non contiene algoritmi di analisi;
* non comunica direttamente con le Data Sources;
* utilizza esclusivamente le REST API del Privacy Intelligence Engine.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 06 - API
* 09 - Network Privacy
* 11 - Backend
