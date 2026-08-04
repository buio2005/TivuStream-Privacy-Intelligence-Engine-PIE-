# Project Context

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Project Context

**Version:** 1.0.2

**Status:** Official

**Last Updated:** 2026-08-02

---

# Purpose

Questo documento fornisce una panoramica completa del progetto.

Ha lo scopo di consentire a sviluppatori e AI Assistant di comprendere rapidamente il contesto, gli obiettivi e la filosofia del Privacy Intelligence Engine senza dover leggere immediatamente tutta la documentazione tecnica.

---

# What is PIE

TivuStream Privacy Intelligence Engine (PIE) è un framework modulare progettato per analizzare, correlare e interpretare dati relativi alla privacy e alla sicurezza delle reti.

PIE non sostituisce strumenti esistenti.

PIE li integra.

L'obiettivo è trasformare dati tecnici complessi in informazioni facilmente comprensibili.

---

# Project Vision

Il progetto nasce dall'idea che la maggior parte degli strumenti dedicati alla sicurezza e alla privacy esponga grandi quantità di dati tecnici senza aiutare realmente l'utente a comprenderne il significato.

PIE introduce un livello di intelligenza che raccoglie informazioni provenienti da differenti sorgenti, le normalizza attraverso un modello dati unificato e restituisce analisi, indicatori e raccomandazioni.

---

# Current Scope

La prima implementazione del progetto è dedicata all'analisi delle reti DNS.

Il primo backend supportato è:

- Technitium DNS Server

L'architettura è comunque progettata per supportare in futuro ulteriori Data Sources senza modificare il Core.

---

# What PIE Is Not

PIE non è:

- un DNS Server;
- un Firewall;
- un IDS;
- un Antivirus;
- un sistema di filtraggio DNS.

Queste funzionalità appartengono ai backend integrati.

PIE si occupa esclusivamente di analizzare e interpretare le informazioni prodotte da tali sistemi.

---

# First Application

La prima applicazione sviluppata sopra PIE è:

**Network Privacy**

Network Privacy rappresenta l'interfaccia utente del progetto.

Visualizza esclusivamente informazioni elaborate dal Privacy Intelligence Engine.

---

# Core Principles

L'intero progetto segue alcuni principi fondamentali.

- Documentation First
- Privacy First
- Local First
- Self Hosted
- Modular Architecture
- Backend Independence
- Simplicity
- Transparency

Ogni decisione tecnica deve rispettare questi principi.

---

# High Level Architecture

Il progetto è organizzato in livelli indipendenti.

```text
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

Ogni livello possiede responsabilità specifiche.

---

# Documentation Structure

La documentazione ufficiale è contenuta nella cartella:

```text
/docs
```

Ogni Specification descrive un singolo aspetto del progetto.

La documentazione rappresenta il riferimento ufficiale per lo sviluppo.

---

# Repository Structure

Il repository è organizzato nelle seguenti aree principali.

```text
backend/
frontend/
docs/
installer/
examples/
resources/
scripts/
tools/
```

Ogni cartella possiede una responsabilità ben definita.

Il Core risiede all'interno di `backend/` come progetto autonomo, isolato dagli Adapter e dall'host delle REST API.

---

# Technology Stack

Lo stack tecnologico adottato dal progetto è il seguente.

## Backend

- ASP.NET Core
- C#
- REST API

## Frontend

- Vue 3
- TypeScript
- Vite
- Pinia

## Database

- SQLite

## Documentation

- Markdown

---

# Development Workflow

Lo sviluppo segue il seguente ordine.

1. Documentazione
2. Architettura
3. Implementazione
4. Test
5. Revisione
6. Documentazione finale

---

# AI Workflow

Ogni AI Assistant deve leggere nell'ordine.

1. README.md

2. PROJECT_CONTEXT.md

3. AI_DEVELOPMENT_GUIDE.md

4. Specification interessate

Solo successivamente può iniziare a produrre codice.

---

# Long-Term Vision

PIE è progettato per diventare un framework capace di integrare molteplici sistemi dedicati alla privacy e alla sicurezza.

L'architettura non è limitata al DNS.

Nel tempo potranno essere sviluppati nuovi Adapter e nuove applicazioni mantenendo invariato il Core.

---

# Project Goal

L'obiettivo finale del progetto è costruire una piattaforma self-hosted capace di offrire una visione semplice, affidabile e unificata dello stato di privacy e sicurezza della rete.

Ogni componente dell'ecosistema dovrà contribuire a questo obiettivo mantenendo modularità, indipendenza e semplicità.

---

# References

Prima di iniziare qualsiasi attività di sviluppo consultare:

- README.md
- AI_DEVELOPMENT_GUIDE.md
- Documentation Release 1.3.0 (/docs)
- CHANGELOG.md

Questi documenti costituiscono il riferimento ufficiale del progetto.