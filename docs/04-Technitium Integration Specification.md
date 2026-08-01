# 04 - Technitium Integration

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Technitium Integration Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce le modalità di integrazione tra il Privacy Intelligence Engine (PIE) e Technitium DNS Server.

Technitium rappresenta il primo backend ufficialmente supportato dal progetto.

---

# Scope

L'integrazione ha lo scopo di acquisire dati dal backend DNS e renderli disponibili al Privacy Intelligence Engine attraverso il relativo Adapter.

La gestione del DNS rimane completamente delegata a Technitium.

---

# Architectural Role

Technitium costituisce una **Data Source**.

Non appartiene al Core del progetto.

Non contiene logica di analisi sviluppata da PIE.

La sua responsabilità termina con la produzione dei dati.

---

# Responsibilities

## Technitium

Technitium gestisce:

* DNS Resolution
* Cache
* DNSSEC
* DNS over HTTPS (DoH)
* DNS over TLS (DoT)
* DNS over QUIC (DoQ)
* Zone Management
* Query Logging
* Statistics
* Blocklists

---

## Privacy Intelligence Engine

PIE gestisce:

* acquisizione dati;
* normalizzazione;
* classificazione;
* correlazione;
* analisi;
* calcolo del NPSS;
* generazione di Alert;
* generazione di Recommendations;
* produzione dei Report.

---

# Integration Architecture

```text id="vtahj0"
Technitium DNS Server

↓

HTTP API

↓

Technitium Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine
```

---

# Adapter Responsibilities

L'Adapter rappresenta l'unico componente autorizzato a comunicare con Technitium.

Le sue responsabilità comprendono:

* autenticazione;
* gestione delle richieste HTTP;
* conversione dei dati;
* gestione degli errori;
* normalizzazione del formato.

L'Adapter non esegue alcuna elaborazione.

---

# Retrieved Data

L'integrazione acquisisce le seguenti informazioni.

## Server Information

* versione;
* stato operativo;
* configurazione.

---

## DNS Statistics

* richieste DNS;
* richieste bloccate;
* cache;
* errori;
* protocolli utilizzati;
* statistiche generali.

---

## Client Information

* indirizzi IP;
* hostname (quando disponibili);
* statistiche di attività.

---

## Domains

* domini osservati;
* domini bloccati;
* frequenza delle richieste.

---

## Blocklists

* stato;
* ultimo aggiornamento;
* numero di domini gestiti.

---

## Logs

Quando disponibili.

* eventi;
* query;
* informazioni diagnostiche.

---

# Data Conversion

Tutti i dati recuperati vengono convertiti nel formato definito dal Unified Data Model.

Nessun componente del Core utilizza direttamente il formato originale restituito da Technitium.

---

# Backend Independence

Il Core non contiene riferimenti specifici a Technitium.

La sostituzione del backend richiede esclusivamente la realizzazione di un nuovo Adapter.

---

# Error Management

Gli errori restituiti da Technitium vengono convertiti in eventi standardizzati.

Il Core riceve esclusivamente informazioni normalizzate.

---

# Security

Le credenziali di accesso alle API rimangono confinate all'interno dell'Adapter.

Il Frontend non comunica mai direttamente con Technitium.

---

# Compatibility

PIE mantiene la massima compatibilità possibile con le API pubbliche di Technitium.

Non vengono modificate componenti del progetto originale.

Non viene realizzato alcun fork.

---

# Update Strategy

L'evoluzione di Technitium rimane indipendente da quella del Privacy Intelligence Engine.

L'Adapter rappresenta il livello di compatibilità tra le due piattaforme.

---

# Design Principles

L'integrazione segue i seguenti principi.

* utilizzo esclusivo delle API pubbliche;
* assenza di modifiche al codice di Technitium;
* completa separazione tra backend e Core;
* indipendenza architetturale;
* modularità.

---

# Constraints

L'integrazione non introduce:

* dipendenze dirette nel Core;
* logica di analisi;
* personalizzazioni del backend;
* modifiche ai componenti originali di Technitium.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
* 09 - Network Privacy
