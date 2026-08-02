# 12 - Installation

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Installation Specification

**Version:** 1.1.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce i requisiti e il processo di installazione del Privacy Intelligence Engine.

L'obiettivo è garantire una procedura di installazione semplice, ripetibile e indipendente dalla piattaforma.

---

# Objectives

L'installazione deve essere:

* semplice;
* guidata;
* ripetibile;
* sicura;
* facilmente aggiornabile.

---

# Supported Platforms

Le piattaforme supportate sono:

* Linux
* Windows
* Docker

Ulteriori piattaforme potranno essere supportate nelle versioni successive.

---

# Installation Modes

Sono previsti i seguenti metodi di installazione.

## Standard Installation

Installazione completa del sistema.

Comprende:

* Backend
* Frontend
* Core
* REST API

---

## Docker Installation

Installazione tramite container.

Comprende tutti i componenti del progetto.

---

## Development Installation

Installazione destinata allo sviluppo.

Include strumenti aggiuntivi per debugging e test.

---

# System Requirements

## Minimum

* CPU Dual Core
* 4 GB RAM
* 2 GB spazio disponibile
* Connessione di rete

---

## Recommended

* CPU Quad Core
* 8 GB RAM
* SSD
* Connessione Gigabit

---

# Required Components

Per il funzionamento del sistema sono necessari.

* Privacy Intelligence Engine
* almeno una Data Source supportata
* Browser moderno
* HTTPS

---

# Installation Flow

```text id="jpruor"
Environment Check

↓

Dependency Check

↓

Component Installation

↓

Configuration

↓

Connection Test

↓

System Validation

↓

Ready
```

---

# Environment Validation

Prima dell'installazione vengono verificati.

* sistema operativo;
* spazio disponibile;
* permessi;
* rete;
* dipendenze.

---

# Initial Configuration

Durante la configurazione iniziale vengono definiti.

* lingua;
* Data Source;
* parametri di connessione;
* autenticazione;
* impostazioni di rete.

---

# Backend Registration

Ogni Data Source viene registrata attraverso il relativo Adapter.

Il sistema verifica automaticamente la connettività.

---

# Capability Detection

Al termine della registrazione il sistema rileva **quali capacità la Data Source è in grado di offrire** nella sua configurazione corrente.

Il risultato viene presentato all'utente prima del completamento dell'installazione.

Per ciascuna capacità mancante il sistema indica:

* quali analisi non saranno disponibili;
* quale intervento la renderebbe disponibile;
* quali conseguenze comporta tale intervento.

---

# Optional Capabilities

Alcune capacità richiedono componenti facoltativi della Data Source.

Quando tali componenti sono installabili in modo automatico, il sistema può proporne l'installazione durante la configurazione guidata.

La proposta rispetta tre regole.

**Scelta esplicita.** Nessun componente facoltativo viene installato senza una decisione dell'utente.

**Informazione simmetrica.** Vantaggi e costi sono presentati insieme. Se l'attivazione comporta un aumento del consumo di risorse, la registrazione di dati aggiuntivi o un impatto sulle prestazioni, tali aspetti vengono dichiarati prima della scelta.

**Reversibilità.** L'utente può rifiutare la proposta e completare comunque l'installazione, oppure attivare la capacità in un momento successivo.

---

# Retention Configuration

Quando una capacità dipende da un componente che registra dati con una politica di ritenzione propria, il sistema verifica la coerenza fra tale ritenzione e la frequenza di acquisizione configurata.

Una ritenzione inferiore all'intervallo di acquisizione comporta una **perdita di dati non segnalata dal backend**.

Il sistema propone valori coerenti e segnala la condizione quando si verifica.

Questa verifica viene ripetuta a ogni modifica della frequenza di acquisizione.

---

# Privacy Disclosure

Quando una capacità comporta la registrazione di dati aggiuntivi relativi all'attività degli utenti, l'installazione dichiara in modo esplicito:

* quali dati vengono registrati;
* dove risiedono;
* per quanto tempo vengono conservati;
* quali dati vengono conservati da PIE e quali restano nella Data Source.

Il progetto adotta il principio di aggregare il dato al momento dell'acquisizione e di non conservare il dettaglio puntuale all'interno di PIE.

Questa scelta va comunicata all'utente, poiché ne determina l'esposizione effettiva.

---

# Security

Durante l'installazione:

* vengono generate le configurazioni iniziali;
* vengono verificati i certificati;
* vengono protette le credenziali.

---

# Verification

Al termine dell'installazione il sistema esegue:

* verifica del Core;
* verifica delle REST API;
* verifica degli Adapter;
* verifica della Data Source.

---

# Update Process

L'aggiornamento del sistema mantiene:

* configurazioni;
* dati;
* Adapter installati.

L'aggiornamento non modifica la struttura del Unified Data Model.

---

# Backup

Prima di ogni aggiornamento il sistema può creare un backup della configurazione.

Il backup comprende.

* impostazioni;
* configurazione;
* dati applicativi.

---

# Uninstallation

La procedura di rimozione elimina:

* componenti applicativi;
* servizi;
* file temporanei.

Le configurazioni possono essere conservate su richiesta dell'utente.

---

# Logging

Ogni fase dell'installazione viene registrata.

I log facilitano la diagnosi di eventuali problemi.

---

# Error Handling

Ogni errore viene classificato e presentato con una descrizione comprensibile.

---

# Design Principles

L'installazione segue i seguenti principi.

* semplicità;
* ripetibilità;
* sicurezza;
* modularità;
* indipendenza dalla piattaforma.

---

# Constraints

La procedura di installazione:

* non modifica le Data Sources;
* non richiede modifiche ai backend supportati;
* utilizza esclusivamente componenti ufficiali del progetto.

---

# Related Specifications

* 04 - Technitium Integration
* 10 - Frontend
* 11 - Backend
* 13 - Roadmap
