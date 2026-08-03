# 16 - Persistence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Persistence Specification

**Version:** 1.0.0

**Status:** Approved

**Last Updated:** 2026-08-03

---

# Purpose

Questa specifica definisce come il Privacy Intelligence Engine conserva i dati nel tempo.

La conservazione è necessaria a funzionalità già previste da altre Specification: lo storico del Network Privacy & Security Score, il calcolo del trend, lo storico delle minacce, i confronti temporali e la generazione dei report.

---

# Why Persistence Is Required

Le Data Sources non conservano i dati indefinitamente.

Technitium applica una ritenzione propria e, per i log delle query, un limite basato sul numero di record che su una rete reale corrisponde a una frazione di ora.

Ne discende un principio.

> Ciò che non viene conservato al momento dell'acquisizione è perduto in modo definitivo.

PIE non può quindi limitarsi a interrogare la sorgente al momento della richiesta: deve costruire il proprio storico.

---

# What Is Persisted

## Acquisitions

Le acquisizioni convertite nel Unified Data Model.

Comprendono `Statistics`, `Device`, `Domain` e, quando disponibile, `DomainActivity`.

Conservare le acquisizioni consente di **ricalcolare le analisi su dati storici** senza reinterrogare la sorgente, cosa impossibile data la ritenzione dei backend.

Questo mantiene coerente lo storico quando un algoritmo di analisi viene modificato.

---

## Core Results

I risultati prodotti dal Core.

Comprendono `NetworkSnapshot`, `Npss`, `Threat`, `Alert` e `Recommendation`.

Conservarli evita di ricalcolare l'intera analisi a ogni richiesta, requisito rilevante su hardware modesto.

---

## What Is Never Persisted

PIE **non conserva mai il dettaglio della singola interrogazione DNS**.

Il registro puntuale delle interrogazioni costituisce la cronologia di navigazione di ogni dispositivo della rete. L'aggregazione avviene nell'Adapter, prima che il dato raggiunga il Core, e ciò che viene conservato è esclusivamente il risultato aggregato.

Questa è la principale misura di protezione dell'utente prevista dal progetto.

Non vengono inoltre conservati:

* credenziali delle Data Sources in chiaro;
* risposte originali dei backend nel loro formato nativo;
* dettagli diagnostici prodotti dai backend.

---

# Observation Periods

Questa sezione definisce il concetto centrale della persistenza.

---

## The Problem

Un'acquisizione **non è un insieme di eventi**: è l'osservazione di un intervallo temporale.

Due osservazioni di intervalli sovrapposti descrivono in parte lo stesso traffico. Non sono sommabili.

Un sistema che acquisisce ogni cinque minuti una finestra di sessanta produce dodici osservazioni all'ora che descrivono in larga parte gli stessi dati. Sommarle produrrebbe valori privi di senso; sceglierne una arbitrariamente scarterebbe informazione.

---

## The Rule

Le acquisizioni sono allineate a **periodi di osservazione fissi**.

Il periodo predefinito è l'**ora solare**.

| Concetto              | Definizione                                                   |
| --------------------- | -------------------------------------------------------------- |
| Periodo               | Intervallo fisso, allineato all'ora                             |
| Periodo corrente      | Quello in cui cade l'istante dell'acquisizione                  |
| Periodo concluso      | Qualunque periodo interamente trascorso                         |

Valgono le seguenti regole.

**Un'acquisizione osserva il periodo corrente.**

**Una nuova osservazione dello stesso periodo sostituisce la precedente**, non vi si aggiunge.

**Un periodo concluso è immutabile.** Una volta trascorso e osservato, non viene più aggiornato.

**Lo storico è la sequenza dei periodi conclusi**, che non si sovrappongono e sono quindi aggregabili.

---

## Consequences

L'idempotenza è garantita per costruzione: riacquisire lo stesso periodo più volte non altera il risultato.

La frequenza di acquisizione diventa un parametro di **freschezza**, non di correttezza. Acquisire ogni cinque minuti aggiorna il periodo corrente più spesso; non produce duplicazione.

La frequenza deve comunque restare **inferiore alla ritenzione della sorgente**, altrimenti un periodo può concludersi prima di essere stato osservato e i suoi dati sono perduti senza segnalazione.

Il periodo di osservazione è configurabile. Periodi più brevi aumentano risoluzione e volume; periodi più lunghi riducono entrambi.

---

# Retention

La ritenzione è **a livelli**.

| Livello              | Contenuto                              | Ritenzione predefinita |
| -------------------- | -------------------------------------- | ---------------------- |
| Periodi di osservazione | Dettaglio orario                    | 30 giorni              |
| Aggregati giornalieri   | Sintesi per giorno                  | 12 mesi                |
| Aggregati mensili       | Sintesi per mese                    | 5 anni                 |

Alla scadenza di un livello i dati vengono consolidati nel livello successivo e il dettaglio viene eliminato.

Questo appiattisce la crescita dello spazio occupato, che altrimenti sarebbe lineare nel tempo.

I valori predefiniti sono configurabili.

Il consolidamento è **irreversibile**: l'utente deve poterlo comprendere prima di ridurre la ritenzione del dettaglio.

---

# Architectural Placement

La persistenza è responsabilità di un componente del Backend denominato **Storage**.

```text
Adapter Manager

↓

Unified Data Model

↓

Core

↓

Storage
```

Valgono i seguenti vincoli.

**Il Core non conosce lo Storage.** Produce risultati e non sa dove finiscano, coerentemente con il vincolo che non conosca né sorgenti né destinatari.

**Il Unified Data Model non contiene elementi di persistenza.** Nessun attributo di mapping, nessun riferimento a tecnologie di archiviazione, nessuna dipendenza. La conversione fra modello e archiviazione avviene interamente dentro lo Storage.

**Il Query Flow legge dallo Storage**, mai dalle Data Sources.

---

# Technology

## Database

**SQLite**, in un unico file locale.

La scelta è coerente con i principi Local First e Self Hosted: nessun servizio aggiuntivo da installare, nessuna porta da esporre, nessun processo separato da gestire.

Il file risiede in una posizione determinata dalla configurazione, insieme agli altri dati applicativi.

---

## Data Access

L'accesso avviene tramite **SQL esplicito**.

Schema e interrogazioni restano visibili e ispezionabili, senza livelli di comportamento implicito.

La scelta risponde ai principi di Simplicity e Transparency e al divieto di introdurre astrazioni non necessarie.

---

## Dependency

| Voce      | Valore                                                    |
| --------- | ---------------------------------------------------------- |
| Nome      | `Microsoft.Data.Sqlite`                                     |
| Scopo     | Accesso al database SQLite                                  |
| Licenza   | MIT                                                         |
| Manutenzione | Microsoft, parte dell'ecosistema .NET                    |

È la prima dipendenza esterna del progetto.

La versione viene fissata in `Directory.Packages.props`, secondo il Central Package Management già adottato.

---

# Schema Management

Il database possiede una **versione dello schema**, conservata al suo interno.

All'avvio il sistema confronta la versione attesa con quella presente.

| Condizione            | Comportamento                                            |
| --------------------- | --------------------------------------------------------- |
| Versioni coincidenti  | Avvio normale                                              |
| Schema più vecchio    | Migrazione, preceduta da copia di sicurezza                |
| Schema più recente    | Avvio rifiutato con messaggio esplicito                    |

L'ultimo caso indica un tentativo di utilizzare dati prodotti da una versione successiva del software. Procedere comporterebbe corruzione silenziosa.

Le migrazioni sono **esplicite e ordinate**. Non viene generata alcuna migrazione automatica a partire dal modello.

---

# Backup

La Installation Specification prevede una copia di sicurezza prima di ogni aggiornamento.

La copia comprende il database e la configurazione.

Trattandosi di un unico file, la copia consiste nella sua duplicazione a servizio fermo.

---

# Privacy

Il database contiene dati relativi all'attività di rete dell'utente.

Si applicano le seguenti regole.

* Il contenuto non lascia mai il dispositivo.
* Non viene trasmessa alcuna telemetria.
* La ritenzione è configurabile e dichiarata all'utente.
* L'utente può eliminare i dati conservati.
* L'eliminazione è effettiva, non una marcatura logica.

L'installazione dichiara quali dati vengono conservati, dove risiedono e per quanto tempo, come previsto dalla Installation Specification.

---

# Performance

Su un dispositivo modesto valgono le seguenti priorità.

* Scritture raggruppate anziché per singola entità.
* Interrogazioni di lettura servite da indici espliciti.
* Consolidamento eseguito come operazione pianificata, non durante una richiesta.
* Nessun ricalcolo dell'analisi durante il Query Flow.

---

# Design Principles

La persistenza segue i seguenti principi.

* idempotenza per costruzione;
* immutabilità dei periodi conclusi;
* conservazione del minimo necessario;
* trasparenza dello schema;
* indipendenza del modello dati dalla tecnologia di archiviazione.

---

# Constraints

Lo Storage:

* non esegue analisi;
* non modifica i dati che riceve;
* non è raggiungibile dal Frontend;
* non è conosciuto dal Core;
* non conserva il dettaglio della singola interrogazione.

---

# Related Specifications

* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 11 - Backend
* 12 - Installation
* 14 - License
