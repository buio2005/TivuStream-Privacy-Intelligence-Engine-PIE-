# 07 - Network Privacy & Security Score

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Network Privacy & Security Score (NPSS) Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il **Network Privacy & Security Score (NPSS)**, il principale indicatore prodotto dal Privacy Intelligence Engine.

Il NPSS sintetizza lo stato complessivo della rete attraverso un unico valore numerico ottenuto dall'analisi dei dati elaborati dal Core.

---

# Objectives

Il NPSS deve essere:

* semplice da comprendere;
* coerente;
* riproducibile;
* trasparente;
* indipendente dal backend;
* aggiornato automaticamente.

---

# Score Range

Il punteggio utilizza una scala da **0** a **100**.

| Score    | Status    |
| -------- | --------- |
| 90 – 100 | Excellent |
| 75 – 89  | Good      |
| 60 – 74  | Fair      |
| 40 – 59  | Warning   |
| 0 – 39   | Critical  |

---

# Score Components

Il punteggio è composto da differenti aree di valutazione.

```text id="fvc3pk"
Network Privacy & Security Score

├── DNS Security
├── Privacy Protection
├── Threat Protection
├── Device Health
├── Configuration
└── Network Integrity
```

---

# DNS Security

Valuta la configurazione del servizio DNS.

Indicatori.

* DNSSEC
* DNS Encryption
* Resolver Configuration
* Query Validation
* DNS Errors

---

# Privacy Protection

Valuta il livello di protezione della privacy.

Indicatori.

* Tracker Blocking
* Analytics Detection
* Advertising Domains
* Telemetry Detection
* Privacy Configuration

---

# Threat Protection

Valuta la protezione contro minacce note.

Indicatori.

* Malware
* Phishing
* Cryptomining
* Suspicious Domains
* Threat Intelligence

---

# Device Health

Analizza il comportamento dei dispositivi.

Indicatori.

* attività anomala;
* numero di Alert;
* traffico DNS;
* comportamento generale.

---

# Configuration

Valuta la qualità della configurazione complessiva.

Indicatori.

* configurazione valida;
* servizi disponibili;
* sincronizzazione;
* stato operativo.

---

# Network Integrity

Valuta lo stato generale della rete.

Indicatori.

* errori;
* disponibilità;
* consistenza;
* stabilità.

---

# Score Breakdown

Il sistema conserva il dettaglio del punteggio.

Ogni componente contribuisce al risultato finale.

Esempio.

```text id="c8qqdc"
Overall Score

92

DNS Security

19 /20

Privacy Protection

18 /20

Threat Protection

20 /20

Device Health

17 /20

Configuration

18 /20
```

---

# Score History

Ogni aggiornamento del NPSS viene memorizzato.

Lo storico consente:

* confronti temporali;
* analisi dei trend;
* report periodici.

---

# Score Trend

Il sistema calcola automaticamente la variazione del punteggio.

Stati previsti.

* Improving
* Stable
* Decreasing

---

# Positive Factors

Il punteggio aumenta quando vengono rilevate configurazioni corrette.

Esempi.

* DNSSEC attivo;
* DNS cifrato;
* assenza di malware;
* blocklist aggiornate;
* configurazione valida.

---

# Negative Factors

Il punteggio diminuisce quando vengono rilevate condizioni critiche.

Esempi.

* malware;
* phishing;
* DNSSEC disattivato;
* configurazioni errate;
* Alert critici.

---

# Transparency

Ogni variazione del punteggio deve essere spiegabile.

Il sistema conserva il dettaglio degli indicatori che hanno contribuito al risultato.

---

# Backend Independence

L'algoritmo NPSS utilizza esclusivamente il Unified Data Model.

Non contiene dipendenze dirette dai backend.

---

# Versioning

L'algoritmo possiede una propria versione indipendente.

Le modifiche sostanziali incrementano la Major Version.

---

# Design Principles

Il NPSS segue i seguenti principi.

* semplicità;
* trasparenza;
* uniformità;
* riproducibilità;
* indipendenza;
* aggiornamento continuo.

---

# Constraints

Il NPSS:

* non rappresenta una certificazione di sicurezza;
* non misura esclusivamente la privacy;
* non dipende da una specifica tecnologia;
* rappresenta un indicatore sintetico prodotto dal Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
* 08 - Threat Intelligence
* 09 - Network Privacy
