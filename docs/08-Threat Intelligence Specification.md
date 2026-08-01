# 08 - Threat Intelligence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Threat Intelligence Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il sistema di **Threat Intelligence** del Privacy Intelligence Engine.

Il modulo è responsabile dell'identificazione, classificazione e valutazione delle minacce rilevate durante l'analisi dei dati provenienti dalle Data Sources.

---

# Objectives

Il modulo Threat Intelligence ha i seguenti obiettivi.

* identificare domini potenzialmente pericolosi;
* classificare le minacce;
* attribuire un livello di gravità;
* supportare il calcolo del NPSS;
* generare Alert;
* generare Recommendations.

---

# Scope

Threat Intelligence analizza esclusivamente le informazioni presenti nel Unified Data Model.

Non comunica direttamente con le Data Sources.

Non gestisce l'interfaccia utente.

---

# Threat Classification

Ogni dominio osservato viene classificato in una categoria.

Le categorie rappresentano il livello logico utilizzato dall'intero ecosistema PIE.

---

# Primary Categories

## Malware

Domini associati alla distribuzione di software malevolo.

---

## Phishing

Domini progettati per sottrarre credenziali o dati personali.

---

## Tracking

Domini utilizzati per il monitoraggio dell'attività degli utenti.

---

## Advertising

Domini utilizzati per la distribuzione di contenuti pubblicitari.

---

## Analytics

Domini utilizzati per la raccolta di statistiche e dati di utilizzo.

---

## Cryptomining

Domini associati ad attività di mining di criptovalute.

---

## Suspicious

Domini con comportamento anomalo o reputazione incerta.

---

## Social

Domini appartenenti a piattaforme social.

---

## Streaming

Domini dedicati alla distribuzione di contenuti multimediali.

---

## Cloud

Servizi cloud e infrastrutture distribuite.

---

## AI Services

Servizi dedicati all'intelligenza artificiale.

---

## Unknown

Categoria assegnata quando non è possibile classificare il dominio.

---

# Threat Severity

Ogni Threat possiede un livello di severità.

Livelli previsti.

* Informational
* Low
* Medium
* High
* Critical

---

# Confidence Level

Ogni classificazione possiede un livello di affidabilità.

Valori previsti.

* Low
* Medium
* High

Il livello di confidenza permette di distinguere una classificazione certa da una classificazione probabilistica.

---

# Threat Sources

Le classificazioni possono essere ottenute da:

* regole interne;
* blocklist;
* fonti esterne;
* algoritmi di correlazione.

La provenienza della classificazione viene sempre registrata.

---

# Threat Lifecycle

Ogni Threat attraversa un ciclo di vita.

```text id="zq54ga"
Detected

↓

Classified

↓

Evaluated

↓

Alert Generated

↓

Recommendation Generated

↓

Archived
```

---

# Correlation

Threat Intelligence può correlare eventi provenienti da differenti Data Sources.

La correlazione consente di migliorare la precisione della classificazione.

---

# Domain Reputation

Per ogni dominio il sistema mantiene un indice di reputazione.

La reputazione contribuisce alla classificazione della minaccia.

---

# Threat History

Ogni Threat mantiene il proprio storico.

Informazioni registrate.

* prima rilevazione;
* ultima rilevazione;
* numero di occorrenze;
* stato corrente.

---

# Alert Generation

Threat Intelligence può generare Alert quando vengono rilevate condizioni significative.

La severità dell'Alert dipende dalla gravità della minaccia.

---

# Recommendation Generation

Ogni Threat può produrre una o più Recommendations.

Ogni suggerimento è collegato alla minaccia che lo ha generato.

---

# NPSS Integration

Threat Intelligence contribuisce direttamente al calcolo del Network Privacy & Security Score.

La presenza di Threat critici riduce il punteggio complessivo.

---

# Extensibility

Nuove categorie possono essere aggiunte mantenendo la compatibilità con il modello esistente.

Le classificazioni già esistenti non vengono modificate.

---

# Design Principles

Threat Intelligence segue i seguenti principi.

* uniformità;
* modularità;
* trasparenza;
* indipendenza dal backend;
* estendibilità;
* riproducibilità.

---

# Constraints

Threat Intelligence:

* non modifica i dati originali;
* non comunica direttamente con il Frontend;
* utilizza esclusivamente il Unified Data Model;
* non dipende da una specifica Data Source.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 09 - Network Privacy
