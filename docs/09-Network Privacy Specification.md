# 09 - Network Privacy

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Network Privacy Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce **Network Privacy**, la prima applicazione sviluppata utilizzando il Privacy Intelligence Engine (PIE).

Network Privacy rappresenta il livello di presentazione dell'ecosistema PIE e consente all'utente di visualizzare, comprendere e gestire le informazioni prodotte dal Core.

---

# Objectives

L'applicazione ha i seguenti obiettivi.

* presentare informazioni in modo semplice;
* visualizzare lo stato della rete;
* mostrare il Network Privacy & Security Score (NPSS);
* evidenziare minacce e anomalie;
* fornire raccomandazioni operative;
* semplificare l'analisi della rete.

---

# Scope

Network Privacy è un'applicazione.

Non è un motore di analisi.

Non implementa algoritmi di classificazione.

Non comunica direttamente con le Data Sources.

Utilizza esclusivamente le API pubbliche del Privacy Intelligence Engine.

---

# Architecture

```text id="g1nvw8"
Frontend

↓

REST API

↓

Privacy Intelligence Engine

↓

Adapter

↓

Data Source
```

---

# Main Dashboard

La Dashboard rappresenta il punto di ingresso dell'applicazione.

Visualizza una panoramica dello stato della rete.

---

# Primary Widgets

La Dashboard comprende i seguenti componenti.

* Network Privacy & Security Score
* Network Status
* Devices
* Threats
* Alerts
* Recommendations
* Statistics
* Activity Timeline

---

# Navigation

L'applicazione è organizzata nelle seguenti sezioni.

* Dashboard
* Devices
* Domains
* Threats
* Alerts
* Recommendations
* Statistics
* Reports
* Settings

---

# Devices

La sezione Devices visualizza tutti i dispositivi rilevati.

Per ogni dispositivo vengono mostrate le principali informazioni.

* nome;
* indirizzo IP;
* stato;
* attività;
* Alert;
* Threat;
* statistiche.

---

# Domains

La sezione Domains visualizza i domini osservati dal sistema.

Per ogni dominio vengono mostrati.

* categoria;
* reputazione;
* numero di richieste;
* dispositivi coinvolti.

---

# Threats

La sezione Threats presenta tutte le minacce classificate dal Core.

Le informazioni possono essere filtrate e ordinate.

---

# Alerts

La sezione Alerts mostra gli eventi generati automaticamente dal sistema.

Gli Alert sono organizzati per severità.

---

# Recommendations

La sezione Recommendations raccoglie tutti i suggerimenti prodotti dal Core.

Ogni raccomandazione è collegata agli eventi che l'hanno generata.

---

# Statistics

La sezione Statistics presenta dati aggregati relativi alla rete.

Comprende:

* traffico DNS;
* query;
* domini;
* cache;
* dispositivi;
* protocolli.

---

# Reports

L'applicazione consente la generazione di report.

Formati previsti.

* PDF
* CSV
* JSON

---

# Search

L'applicazione include un sistema di ricerca globale.

La ricerca consente di individuare rapidamente:

* dispositivi;
* domini;
* Threat;
* Alert.

---

# Filters

Ogni elenco supporta filtri dinamici.

Esempi.

* categoria;
* severità;
* intervallo temporale;
* dispositivo;
* dominio.

---

# User Experience

L'interfaccia privilegia:

* semplicità;
* chiarezza;
* leggibilità;
* accessibilità.

Le informazioni critiche devono essere immediatamente identificabili.

---

# Data Refresh

Le informazioni visualizzate vengono aggiornate attraverso le REST API.

La frequenza di aggiornamento è configurabile.

---

# Notifications

L'applicazione visualizza gli Alert prodotti dal Core.

La gestione degli Alert rimane di competenza del Privacy Intelligence Engine.

---

# Security

Network Privacy non memorizza credenziali delle Data Sources.

Le comunicazioni avvengono esclusivamente tramite le REST API del Core.

---

# Extensibility

L'interfaccia è progettata per ospitare nuove sezioni senza modificare la struttura principale.

---

# Design Principles

Network Privacy segue i seguenti principi.

* semplicità;
* modularità;
* leggibilità;
* uniformità;
* indipendenza dal backend.

---

# Constraints

Network Privacy:

* non contiene logica di business;
* non esegue analisi;
* non comunica direttamente con i backend;
* utilizza esclusivamente le REST API del Privacy Intelligence Engine.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 06 - API
* 07 - Network Privacy & Security Score
* 08 - Threat Intelligence
* 10 - Frontend
