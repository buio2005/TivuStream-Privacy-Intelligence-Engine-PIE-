# AI Development Guide

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** AI Development Guide

**Version:** 1.0.0

**Status:** Official

**Last Updated:** 2026-08-02

---

# Purpose

Questo documento definisce le regole operative che ogni AI Assistant deve seguire durante lo sviluppo del progetto.

L'obiettivo è garantire che tutto il codice prodotto sia coerente con l'architettura, la documentazione e gli standard del Privacy Intelligence Engine.

Le istruzioni contenute in questo documento hanno priorità sulle preferenze predefinite dell'assistente.

---

# Development Philosophy

Il progetto viene sviluppato seguendo un approccio **Documentation First**.

La documentazione rappresenta la fonte ufficiale del progetto.

Il codice implementa quanto definito nelle Specification.

Il codice non definisce l'architettura.

---

# Development Workflow

Ogni sessione di sviluppo segue il seguente ordine.

1. Leggere README.md

2. Leggere AI_DEVELOPMENT_GUIDE.md

3. Leggere le Specification interessate

4. Analizzare il task richiesto

5. Implementare esclusivamente quanto documentato

6. Aggiornare la documentazione se necessario

---

# Documentation Priority

In caso di conflitto valgono le seguenti priorità.

1. AI_DEVELOPMENT_GUIDE.md

2. Specification presenti nella cartella docs

3. README.md

4. Codice esistente

Il codice non modifica la documentazione.

È la documentazione che guida il codice.

---

# AI Responsibilities

L'AI è responsabile di:

- scrivere codice pulito;
- rispettare l'architettura;
- mantenere la modularità;
- evitare duplicazioni;
- proporre miglioramenti motivati.

---

# AI Restrictions

L'AI non deve:

- modificare autonomamente l'architettura;
- rinominare componenti ufficiali;
- introdurre dipendenze non richieste;
- creare nuove cartelle senza autorizzazione;
- modificare le Specification senza richiesta esplicita.

---

# Coding Principles

Ogni implementazione deve rispettare i seguenti principi.

- Single Responsibility
- Separation of Concerns
- Modularity
- Readability
- Simplicity
- Maintainability

---

# Architecture Rules

Il progetto utilizza un'architettura a livelli.

Ogni livello possiede responsabilità specifiche.

Frontend

↓

REST API

↓

Core

↓

Adapter

↓

Data Source

La comunicazione deve sempre rispettare questo flusso.

---

# Backend Rules

Il Backend:

- implementa la logica di business;
- gestisce gli Adapter;
- espone le REST API;
- utilizza il Unified Data Model.

---

# Frontend Rules

Il Frontend:

- visualizza informazioni;
- non contiene logica di business;
- utilizza esclusivamente le REST API.

---

# Core Rules

Il Core:

- analizza;
- classifica;
- correla;
- valuta;
- produce risultati.

Il Core non conosce le Data Sources.

---

# Adapter Rules

Ogni Adapter:

- comunica con una sola Data Source;
- converte i dati;
- non esegue analisi.

---

# Data Model

Ogni componente utilizza esclusivamente il Unified Data Model.

È vietato creare modelli dati paralleli.

---

# API Rules

Le API rappresentano il contratto pubblico del sistema.

Ogni modifica alle API deve mantenere la retrocompatibilità.

---

# Naming Convention

Utilizzare esclusivamente la terminologia definita nel Glossary.

Non introdurre sinonimi.

---

# Dependencies

Prima di introdurre una nuova libreria verificare.

- reale necessità;
- qualità;
- manutenzione;
- compatibilità della licenza.

Preferire sempre le librerie già presenti nel framework.

---

# Error Handling

Ogni errore deve essere.

- gestito;
- registrato;
- comprensibile.

---

# Logging

Ogni operazione significativa deve poter essere registrata.

I log non devono contenere informazioni sensibili.

---

# Security

Ogni implementazione deve considerare.

- validazione input;
- autenticazione;
- autorizzazione;
- protezione delle credenziali;
- HTTPS.

---

# Performance

Ottimizzare il codice solo quando necessario.

Privilegiare sempre.

- chiarezza;
- semplicità;
- manutenibilità.

---

# Code Quality

Il codice prodotto deve essere.

- leggibile;
- commentato solo quando necessario;
- facilmente testabile;
- coerente con il resto del progetto.

---

# Git Workflow

Ogni modifica dovrebbe interessare esclusivamente il task corrente.

Evitare modifiche non correlate.

---

# When Requirements Are Missing

Se una Specification non descrive un comportamento necessario:

- non inventare una soluzione;
- non introdurre nuove architetture;
- chiedere chiarimenti;
- oppure proporre una soluzione chiaramente identificata come proposta.

---

# Suggestions

L'AI può proporre miglioramenti.

Ogni proposta deve essere separata dall'implementazione richiesta.

Le proposte non devono modificare automaticamente il progetto.

---

# Final Check

Prima di considerare completato un task verificare.

- conformità alle Specification;
- rispetto dell'architettura;
- compilazione del codice;
- assenza di duplicazioni;
- coerenza del naming.

---

# Goal

L'obiettivo dell'AI non è soltanto produrre codice funzionante.

L'obiettivo è contribuire allo sviluppo di un progetto coerente, modulare, documentato e facilmente manutenibile nel tempo.

Ogni implementazione deve poter essere ricondotta alle Specification ufficiali del progetto.