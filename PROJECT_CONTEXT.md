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

# Recorded Decisions Not Yet Implemented

Decisioni assunte dal proprietario del progetto, da attuare nei milestone indicati.

---

## Bilingual Interface

Il Frontend dovrà offrire l'interfaccia in **italiano e inglese**.

Il vincolo si estende a ogni pagina pubblica del progetto: landing, presentazione, documentazione d'uso.

L'internazionalizzazione va prevista **dalla prima riga del Frontend**. Aggiungerla a interfaccia costruita comporta la riscrittura di ogni testo già scritto.

Attuazione: milestone del Frontend, come requisito e non come aggiunta successiva.

---

## Documentation In English

La documentazione pubblicata su GitHub, `README.md` compreso, sarà **in inglese**.

La ragione è l'accesso a utenti non italiani, coerente con una piattaforma pubblica.

La traduzione riguarda diciassette documenti versionati e va svolta come milestone dedicato, non in modo incrementale: documenti in due lingue diverse nello stesso rilascio renderebbero incerto quale sia la fonte autoritativa.

Il momento opportuno è **prima della pubblicazione del repository**, non prima della fine del Backend.

La comunicazione con il proprietario del progetto resta in italiano.

---

## Plain Language For Our Own Terms

La maggior parte delle persone che useranno lo strumento non ha competenze tecniche.

Le spiegazioni che il prodotto già fornisce sono precise e, per quel pubblico, **opache**: «limite inferiore», «copertura», «minimizzazione del nome interrogato», «trasporto cifrato» sono termini esatti che non spiegano sé stessi.

Serve quindi un livello in più: non una pagina che descrive il funzionamento del prodotto, ma un modo per **spiegare le nostre stesse spiegazioni**.

---

### The Tension To Resolve

Onestà e semplicità qui tirano in direzioni opposte.

«Non ti tracciano» è semplice ed è falso. «Il valore è un limite inferiore» è vero e non arriva a destinazione.

La soluzione **non** è ammorbidire le frasi: le regole della Network Privacy Specification restano intatte, e una formulazione semplificata che perda la qualificazione violerebbe la specifica.

La soluzione è rendere il significato **raggiungibile nel punto in cui il termine compare**, senza obbligare l'utente a cercarlo altrove.

---

### Requirements

* Ogni termine tecnico usato nell'interfaccia possiede una spiegazione breve, in lingua corrente, consultabile dove il termine appare.
* La spiegazione **si aggiunge** alla frase esatta, non la sostituisce.
* Le spiegazioni vivono nel catalogo delle traduzioni, insieme alle frasi che chiariscono, in entrambe le lingue.
* Non è il Glossary di `docs/`, che è scritto per chi sviluppa.

Termini che ne hanno bisogno, elenco iniziale: copertura, limite inferiore, non misurabile, misurata in parte, confidenza, trasporto cifrato, DNSSEC, minimizzazione del nome interrogato, sottorete del client, dominio sospetto, freschezza della lista, dominio non classificato.

---

### What This Is Not

Una sezione «come funziona» scritta per convincere è la scena che il progetto rifiuta di fare.

Se una pagina divulgativa verrà scritta, le regole di onestà vi si applicano integralmente, e non deve riscrivere a mano spiegazioni che il catalogo contiene già: due versioni della stessa affermazione divergono nel tempo senza che nessuno se ne accorga.

---

## Licence Notices

La licenza è stata scelta: **GPL-3.0**. Restano due adempimenti.

**Note nei file sorgente.** La GPL raccomanda una nota di copyright e licenza in testa a ogni file. Va aggiunta come attività dedicata prima della pubblicazione del repository.

**Avviso nell'interfaccia.** La GPL prevede che un'interfaccia interattiva mostri copyright, assenza di garanzia e modo di consultare la licenza. È un requisito del Frontend, da prevedere insieme al bilinguismo.

---

# References

Prima di iniziare qualsiasi attività di sviluppo consultare:

- README.md
- AI_DEVELOPMENT_GUIDE.md
- Documentation Release 1.10.0 (/docs)
- CHANGELOG.md

Questi documenti costituiscono il riferimento ufficiale del progetto.