# 05 - Data Model

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Data Model Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il **Unified Data Model** utilizzato dal Privacy Intelligence Engine.

Il Data Model rappresenta il linguaggio comune attraverso il quale tutti i componenti del Core comunicano tra loro.

Ogni Adapter converte i dati provenienti dalle Data Sources in questo formato.

---

# Objectives

Il Unified Data Model garantisce:

* indipendenza dai backend;
* uniformità delle informazioni;
* semplicità di elaborazione;
* estendibilità;
* compatibilità tra i moduli del Core.

---

# Overview

Tutte le informazioni elaborate dal sistema vengono rappresentate attraverso un insieme di entità standardizzate.

```text id="tczvlt"
Data Source

↓

Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine
```

---

# Primary Entities

Il modello dati è composto dalle seguenti entità principali.

* DataSource
* NetworkSnapshot
* Statistics
* Device
* Domain
* DomainActivity
* Threat
* Alert
* Recommendation
* Network Privacy & Security Score (NPSS)

---

# DataSource

Rappresenta l'origine delle informazioni elaborate dal sistema.

## Properties

* id
* name
* provider
* version
* status
* capabilities
* lastUpdate

---

# NetworkSnapshot

Rappresenta lo stato completo della rete in un determinato momento.

## Properties

* snapshotId
* timestamp
* sourceId
* statistics
* devices
* alerts
* recommendations
* npss

Ogni Snapshot rappresenta un'istantanea completa del sistema.

---

# Statistics

Contiene le statistiche aggregate della rete.

## Properties

* totalQueries
* blockedQueries
* cachedQueries
* failedQueries
* uniqueDomains
* activeDevices
* encryptedQueries
* dnssecEnabled

---

# Device

Rappresenta un dispositivo identificato dal sistema.

## Properties

* deviceId
* hostname
* ipAddress
* macAddress
* vendor
* operatingSystem
* firstSeen
* lastSeen
* status

---

## Associated Objects

Ogni Device può essere associato a:

* DomainActivity
* Threat
* Alert
* Recommendation

---

# Domain

Rappresenta un dominio osservato durante l'analisi.

## Properties

* domain
* category
* reputation
* firstSeen
* lastSeen
* occurrences

---

# DomainActivity

Rappresenta l'interazione tra un Device e un Domain.

## Properties

* deviceId
* domain
* queryCount
* blocked
* protocol
* firstSeen
* lastSeen

---

# Threat

Rappresenta una minaccia classificata dal sistema.

## Properties

* threatId
* category
* severity
* confidence
* description
* source
* detectedAt

---

# Alert

Rappresenta un evento significativo prodotto dal Core.

## Properties

* alertId
* category
* severity
* title
* description
* timestamp
* status

---

# Recommendation

Rappresenta un suggerimento generato automaticamente dal sistema.

## Properties

* recommendationId
* priority
* title
* description
* generatedAt

---

# Network Privacy & Security Score (NPSS)

Rappresenta il punteggio sintetico dello stato della rete.

## Properties

* overallScore
* securityScore
* privacyScore
* protectionScore
* generatedAt
* breakdown

---

# Entity Relationships

```text id="w0d1t9"
NetworkSnapshot

├── Statistics

├── Device[]

│      ├── DomainActivity[]

│      ├── Threat[]

│      ├── Alert[]

│      └── Recommendation[]

├── NPSS

├── Alert[]

└── Recommendation[]
```

---

# Entity Identity

Ogni entità possiede un identificatore univoco.

Gli identificatori sono indipendenti dal backend utilizzato.

---

# Versioning

Il Unified Data Model possiede una propria versione indipendente.

Le modifiche incompatibili incrementano la Major Version.

Le modifiche compatibili incrementano la Minor Version.

---

# Serialization

Il Unified Data Model deve poter essere serializzato nei seguenti formati.

* JSON
* CSV
* XML (eventuale supporto futuro)

La serializzazione non modifica la struttura logica delle entità.

---

# Extensibility

Nuove proprietà possono essere aggiunte mantenendo la retrocompatibilità.

Nuove entità devono integrarsi attraverso relazioni già definite.

---

# Design Principles

Il Data Model segue i seguenti principi.

* uniformità;
* indipendenza;
* semplicità;
* modularità;
* estendibilità;
* riutilizzabilità.

---

# Constraints

Il Unified Data Model:

* non contiene logica di business;
* non contiene logica di presentazione;
* non dipende da uno specifico backend;
* rappresenta l'unico formato dati utilizzato dal Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 06 - API
* 07 - Network Privacy & Security Score
