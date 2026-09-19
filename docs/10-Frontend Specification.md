# 10 - Frontend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Frontend Specification

**Version:** 1.2.0

**Status:** Approved

**Last Updated:** 2026-09-19

---

# Purpose

Questa specifica definisce l'architettura e i principi del Frontend utilizzato dalle applicazioni sviluppate sopra il Privacy Intelligence Engine.

Il Frontend rappresenta esclusivamente il livello di presentazione del sistema.

---

# Objectives

Il Frontend ha i seguenti obiettivi.

* visualizzare i dati prodotti dal Core;
* offrire un'interfaccia semplice e moderna;
* garantire un'esperienza utente coerente;
* mantenere la completa separazione dalla logica di business.

---

# Scope

Il Frontend:

* utilizza esclusivamente le REST API del Core;
* non comunica direttamente con le Data Sources;
* non implementa algoritmi di analisi;
* non modifica il Unified Data Model.

---

# Architecture

```text id="h9v7o1"
User

↓

Frontend

↓

REST API

↓

Privacy Intelligence Engine
```

---

# Application Structure

Il Frontend è suddiviso in moduli indipendenti.

* Dashboard
* Devices
* Domains
* Threats
* Alerts
* Recommendations
* Statistics
* Reports
* Settings

Ogni modulo rappresenta una vista indipendente.

---

# Dashboard

La Dashboard costituisce il punto di ingresso dell'applicazione.

Visualizza le informazioni principali prodotte dal Core.

---

# Layout

Il layout deve essere responsivo.

L'interfaccia deve adattarsi a:

* Desktop;
* Tablet;
* Smartphone.

---

# Navigation

La navigazione deve essere semplice e coerente.

Le principali sezioni dell'applicazione devono essere sempre raggiungibili.

La voce per la gestione degli account è visibile soltanto a un `Administrator`.

---

# Components

Il Frontend utilizza componenti riutilizzabili.

Esempi.

* Cards
* Tables
* Charts
* Timeline
* Filters
* Search
* Notifications
* Dialogs

---

# Data Presentation

Le informazioni vengono presentate privilegiando:

* chiarezza;
* leggibilità;
* sintesi.

Le informazioni tecniche sono disponibili solo quando richieste.

---

# Data Refresh

Il Frontend aggiorna i dati utilizzando le REST API.

L'aggiornamento può essere:

* manuale;
* automatico.

---

# State Management

Lo stato dell'applicazione viene mantenuto localmente.

Il Frontend non modifica i dati prodotti dal Core.

---

# Error Handling

Gli errori vengono presentati in maniera comprensibile.

L'interfaccia evita messaggi tecnici quando non necessari.

---

# Accessibility

L'interfaccia deve rispettare i principali criteri di accessibilità.

Particolare attenzione viene dedicata a:

* contrasto;
* leggibilità;
* navigazione da tastiera;
* responsività.

---

# Internationalization

L'architettura supporta la localizzazione dell'interfaccia.

Le traduzioni vengono gestite separatamente dal codice.

Le lingue offerte sono **italiano e inglese**.

La localizzazione va prevista dalla prima riga del Frontend. Aggiungerla a interfaccia costruita comporta la riscrittura di ogni testo già scritto.

---

## Text Produced From The Backend

Il Backend non trasmette frasi.

I fattori che spiegano il punteggio arrivano come codici con i propri valori, elencati nella NPSS Specification. Il Frontend li rende in parole.

Ne consegue che il catalogo delle traduzioni **contiene affermazioni sul risultato dell'analisi**, non soltanto etichette di interfaccia.

I vincoli di onestà della Network Privacy Specification si applicano integralmente a quel catalogo, in ogni lingua.

Un codice sconosciuto al catalogo viene mostrato come tale, con il proprio identificativo, e non omesso: un fattore che scompare toglierebbe all'utente una ragione del punteggio senza dichiararlo.

---

# Themes

Il sistema supporta temi grafici configurabili.

La gestione del tema non modifica il comportamento dell'applicazione.

---

# Performance

Il Frontend privilegia:

* caricamento rapido;
* rendering efficiente;
* riduzione delle richieste HTTP;
* riutilizzo dei componenti.

---

# Authentication

L'accesso è definito dall'**Authentication Specification**, che contiene anche i testi dei messaggi in entrambe le lingue.

L'applicazione si trova sempre in uno di cinque stati, distinti fra loro: `Checking`, `SetupRequired`, `Unauthenticated`, `Authenticated`, `PasswordChangeRequired`.

Le schermate sono: configurazione iniziale, accesso, cambio password, gestione degli account (solo `Administrator`).

Regole:

* **Nessun dato di rete sopravvive all'uscita.** Alla chiusura della sessione gli store svuotano ogni dato letto.
* Una sessione terminata mentre l'utente era entrato non si presenta come un accesso fallito.
* Credenziali sbagliate, motore non raggiungibile e connessione non sicura sono tre situazioni diverse e producono messaggi diversi. Dire «credenziali errate» quando il motore non ha risposto è un'affermazione falsa.
* Ciò che il ruolo non comprende è mostrato come **trattenuto**: non come assenza e non come errore.

---

# Security

Il Frontend:

* non memorizza credenziali delle Data Sources;
* non conserva password né identificativi di sessione in alcuna memoria persistente: il cookie di sessione non è leggibile dal codice della pagina;
* non espone informazioni sensibili;
* comunica esclusivamente tramite HTTPS.

---

# Extensibility

Nuovi componenti possono essere aggiunti senza modificare quelli esistenti.

L'architettura privilegia la modularità.

---

# Design Principles

Il Frontend segue i seguenti principi.

* semplicità;
* uniformità;
* modularità;
* leggibilità;
* accessibilità;
* indipendenza dal backend.

---

# Constraints

Il Frontend:

* non contiene logica di business;
* non contiene algoritmi di analisi;
* non comunica direttamente con le Data Sources;
* utilizza esclusivamente le REST API del Privacy Intelligence Engine.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 06 - API
* 09 - Network Privacy
* 11 - Backend
