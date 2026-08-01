# 03 - Privacy Intelligence Engine

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Core Engine Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il funzionamento del Privacy Intelligence Engine (PIE), il Core del progetto.

Il motore rappresenta il livello di analisi dell'intero ecosistema TivuStream e costituisce il punto centrale attraverso il quale transitano tutte le informazioni elaborate dal sistema.

---

# Overview

Il Privacy Intelligence Engine riceve dati normalizzati provenienti dagli Adapter, li analizza attraverso moduli specializzati e produce informazioni utilizzabili dalle applicazioni.

Il Core non raccoglie dati direttamente.

Il Core non presenta dati all'utente.

Il Core interpreta i dati.

---

# Responsibilities

Il Core è responsabile di:

* normalizzazione logica;
* classificazione;
* correlazione;
* analisi dei dispositivi;
* analisi delle minacce;
* calcolo del NPSS;
* generazione degli Alert;
* generazione delle Recommendations.

---

# Core Pipeline

Ogni dato elaborato segue la medesima pipeline.

```text
Data Source

↓

Adapter

↓

Unified Data Model

↓

Validation

↓

Classification

↓

Correlation

↓

Analysis

↓

Evaluation

↓

Results
```

---

# Processing Stages

## Validation

Verifica la correttezza e la completezza dei dati ricevuti.

Eventuali dati non validi vengono scartati o segnalati.

---

## Classification

Ogni elemento viene classificato secondo le regole definite dal sistema.

Esempi.

* dominio;
* dispositivo;
* minaccia;
* evento.

---

## Correlation

Il motore mette in relazione informazioni provenienti da sorgenti differenti.

La correlazione permette di costruire una visione completa della rete.

---

## Analysis

I moduli del Core elaborano le informazioni normalizzate.

Ogni modulo opera esclusivamente sul Unified Data Model.

---

## Evaluation

Il motore produce indicatori quantitativi.

Tra questi:

* NPSS;
* Device Score;
* Threat Score.

---

## Result Generation

Il Core produce:

* oggetti;
* eventi;
* statistiche;
* alert;
* raccomandazioni.

Questi rappresentano il risultato finale dell'elaborazione.

---

# Core Modules

## Threat Engine

Analizza e classifica le minacce.

---

## Device Engine

Analizza il comportamento dei dispositivi.

---

## NPSS Engine

Calcola il Network Privacy & Security Score.

---

## Alert Engine

Genera eventi significativi.

---

## Recommendation Engine

Produce suggerimenti basati sui risultati dell'analisi.

---

# Internal Communication

I moduli del Core comunicano attraverso il Unified Data Model.

Non esistono dipendenze dirette tra i moduli.

Ogni componente riceve dati elaborati e restituisce nuovi risultati.

---

# Event Model

Il Core utilizza eventi interni per sincronizzare le attività.

Ogni evento possiede almeno:

* identificativo;
* categoria;
* origine;
* timestamp;
* contenuto.

Gli eventi non vengono esposti direttamente alle applicazioni.

---

# Internal Objects

Il Core utilizza esclusivamente le entità definite nella Data Model Specification.

Le principali sono:

* NetworkSnapshot
* Device
* Domain
* DomainActivity
* Threat
* Alert
* Recommendation
* NPSS

---

# Backend Independence

Il Core non contiene codice specifico relativo ai backend.

Ogni integrazione viene gestita dagli Adapter.

Il funzionamento del Core rimane invariato indipendentemente dalla Data Source utilizzata.

---

# Error Handling

Ogni errore interno viene trasformato in un evento standardizzato.

Gli errori non interrompono la pipeline salvo nei casi in cui venga compromessa la consistenza dei dati.

---

# Performance

Il Core privilegia:

* elaborazione incrementale;
* riutilizzo dei dati;
* riduzione delle elaborazioni duplicate;
* modularità.

---

# Scalability

Nuovi Engine possono essere aggiunti senza modificare quelli esistenti.

Ogni nuovo componente deve utilizzare il Unified Data Model.

---

# Design Principles

Il Core segue i seguenti principi.

* indipendenza dal backend;
* responsabilità singola;
* modularità;
* estendibilità;
* uniformità del modello dati;
* assenza di logica di presentazione.

---

# Constraints

Il Core non:

* comunica direttamente con il Frontend;
* comunica direttamente con le Data Sources;
* contiene logica di interfaccia;
* gestisce configurazioni specifiche dei backend.

---

# Related Specifications

* 00 - Glossary
* 01 - Vision
* 02 - Architecture
* 04 - Technitium Integration
* 05 - Data Model
* 06 - API
* 07 - Network Privacy & Security Score
