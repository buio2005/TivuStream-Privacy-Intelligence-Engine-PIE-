# Contributing

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Contribution Guidelines

**Version:** 1.0.0

**Status:** Official

**Last Updated:** 2026-08-02

---

# Purpose

Questo documento descrive le modalità di contribuzione al progetto.

Non introduce requisiti nuovi.

Raccoglie in forma operativa le regole già definite in `AI_DEVELOPMENT_GUIDE.md` e nelle Specification presenti in `docs/`.

In caso di divergenza, la documentazione ufficiale prevale su questo documento.

---

# Development Model

Il progetto segue un modello **Documentation First**.

La documentazione rappresenta la fonte autorevole.

Il codice implementa quanto definito nelle Specification.

Il codice non definisce l'architettura.

---

# Before Contributing

Prima di scrivere codice è necessario leggere nell'ordine:

1. `README.md`
2. `PROJECT_CONTEXT.md`
3. `AI_DEVELOPMENT_GUIDE.md`
4. le Specification interessate in `docs/`

Un contributo che non sia riconducibile a una Specification ufficiale non può essere accettato.

---

# Documentation Priority

In caso di conflitto valgono le seguenti priorità.

1. `AI_DEVELOPMENT_GUIDE.md`
2. Specification in `docs/`
3. `README.md`
4. Codice esistente

---

# Architecture

L'architettura del progetto è fissa.

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

Il sistema utilizza due flussi distinti.

L'**Acquisition Flow** è innescato dallo Scheduler e orchestrato dall'Adapter Manager.

Il **Query Flow** serve le richieste del Frontend e non raggiunge mai le Data Sources.

---

# Architectural Rules

Le seguenti regole non possono essere derogate.

* Il Core non comunica direttamente con le Data Sources.
* Il Core non orchestra l'acquisizione dei dati.
* Ogni Data Source possiede un Adapter dedicato.
* Gli Adapter convertono i dati e non eseguono analisi.
* Il Frontend non contiene logica di business.
* Il Core non contiene logica di presentazione.
* Il Unified Data Model è l'unico formato dati interno.

Il progetto che implementa il Core non referenzia gli Adapter né l'host delle REST API.

Questo isolamento è verificato in fase di compilazione: un riferimento aggiunto per comodità fa fallire la build.

---

# What Is Not Allowed

Un contributo non deve:

* modificare autonomamente l'architettura;
* rinominare componenti ufficiali;
* introdurre dipendenze non approvate;
* creare nuove cartelle senza autorizzazione;
* modificare le Specification senza richiesta esplicita;
* creare modelli dati paralleli al Unified Data Model.

---

# Missing Requirements

Se una Specification non descrive un comportamento necessario:

* non inventare una soluzione;
* non introdurre nuove architetture;
* chiedere chiarimenti;
* oppure proporre una soluzione chiaramente identificata come proposta.

Le proposte restano separate dall'implementazione richiesta.

---

# Terminology

Utilizzare esclusivamente la terminologia definita in `docs/00-Glossary.md`.

Non introdurre sinonimi.

I moduli del Core utilizzano tutti il suffisso **Engine**.

Alcuni esempi di termini da evitare.

| Evitare                                   | Utilizzare                              |
| ----------------------------------------- | --------------------------------------- |
| Privacy Score                             | Network Privacy & Security Score (NPSS) |
| Connector                                 | Adapter                                 |
| Threat Intelligence (come nome di modulo) | Threat Engine                           |

L'elenco completo è contenuto nel Glossary.

---

# Language

La comunicazione con il team avviene nella lingua del progetto.

Il codice sorgente è scritto in inglese.

Sono in inglese identificatori, namespace, classi, metodi, nomi di file e messaggi di commit.

La terminologia ufficiale del progetto non viene tradotta.

---

# Coding Principles

Ogni implementazione rispetta i seguenti principi.

* Single Responsibility
* Separation of Concerns
* Modularity
* Readability
* Simplicity
* Maintainability

Il codice privilegia la leggibilità sulle soluzioni compatte.

Le astrazioni non necessarie vengono evitate.

I componenti restano piccoli.

La logica non viene duplicata.

---

# Repository Structure

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

Il Backend è organizzato come segue.

```text
backend/
├── TivuStream.Pie.sln
├── Directory.Build.props
├── src/
│   ├── TivuStream.Pie.Model/
│   ├── TivuStream.Pie.Core/
│   ├── TivuStream.Pie.Adapters/
│   ├── TivuStream.Pie.Adapters.Technitium/
│   └── TivuStream.Pie.Api/
└── tests/
```

---

# Technology Stack

## Backend

* ASP.NET Core
* C#
* .NET 10
* SQLite

## Frontend

* Vue 3
* TypeScript
* Pinia
* Vite

---

# Code Style

Le convenzioni di formattazione sono definite in `.editorconfig` e vengono applicate automaticamente dagli editor compatibili.

Le terminazioni di riga sono normalizzate da `.gitattributes`.

Nessuna di queste impostazioni va aggirata manualmente.

Le principali convenzioni C#.

* namespace file-scoped;
* direttive `using` esterne al namespace;
* PascalCase per tipi, metodi, proprietà e costanti;
* camelCase per parametri e variabili locali;
* prefisso `_` per i campi privati;
* prefisso `I` per le interfacce.

---

# Prerequisites

Per compilare il Backend è necessario un **.NET SDK 10.x**.

La versione è vincolata da `global.json` nella radice del repository.

Un SDK di major version differente produce un errore esplicito anziché una build silenziosamente diversa.

Verificare l'SDK installato.

```bash
dotnet --list-sdks
```

L'aggiornamento a una major version successiva è una decisione esplicita e comporta la modifica di `global.json` e di `Directory.Build.props`.

---

# Build

La build tratta i warning come errori.

Un contributo che genera warning non è considerato completo.

Ogni versione dell'SDK introduce nuove regole degli analizzatori: `global.json` garantisce che un aggiornamento dell'ambiente non faccia fallire la build di codice non modificato.

Comandi principali.

```bash
dotnet restore backend/TivuStream.Pie.sln
dotnet build backend/TivuStream.Pie.sln
```

---

# Dependencies

Prima di introdurre una libreria esterna verificare:

* reale necessità;
* qualità;
* manutenzione attiva;
* compatibilità della licenza.

Preferire sempre le librerie già presenti nel framework.

Ogni dipendenza introdotta viene documentata con nome, versione, licenza e scopo.

L'introduzione di una nuova dipendenza richiede approvazione esplicita.

---

# Error Handling and Logging

Ogni errore deve essere gestito, registrato e comprensibile.

Ogni operazione significativa deve poter essere registrata.

I log non devono contenere informazioni sensibili.

---

# Security

Ogni implementazione considera:

* validazione dell'input;
* autenticazione;
* autorizzazione;
* protezione delle credenziali;
* HTTPS.

Le credenziali delle Data Sources restano confinate all'interno del relativo Adapter.

Nessuna credenziale, database locale o file di ambiente viene versionato.

---

# Git Workflow

Ogni modifica interessa esclusivamente il task corrente.

Le modifiche non correlate vanno evitate.

I messaggi di commit sono scritti in inglese.

---

# Documentation Changes

Quando l'implementazione rivela un'inconsistenza nella documentazione:

* non modificare silenziosamente l'implementazione per aggirarla;
* segnalare l'inconsistenza;
* proporre un aggiornamento della documentazione;
* attendere approvazione.

Ogni modifica alla documentazione viene registrata in `CHANGELOG.md`.

---

# Before Submitting

Prima di considerare completato un contributo verificare:

* il progetto compila;
* nessun warning;
* architettura rispettata;
* documentazione rispettata;
* naming coerente con il Glossary;
* nessuna logica duplicata;
* nessun codice morto;
* nessuna dipendenza non necessaria.

---

# License

La licenza definitiva del progetto non è ancora stata scelta.

La decisione verrà presa prima della prima Beta pubblica.

I contributi dovranno rispettare la licenza adottata.

Le componenti open source integrate mantengono le rispettive licenze originali.

La politica completa è descritta in `docs/14-License Specification.md`.

---

# References

* `README.md`
* `PROJECT_CONTEXT.md`
* `AI_DEVELOPMENT_GUIDE.md`
* `CHANGELOG.md`
* Specification in `docs/`
