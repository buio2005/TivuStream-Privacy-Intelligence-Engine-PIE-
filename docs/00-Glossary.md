# 00 - Glossary

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Glossary Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questo documento definisce il glossario ufficiale del progetto.

Ogni termine riportato in questa specifica rappresenta la nomenclatura ufficiale da utilizzare nella documentazione, nel codice sorgente, nelle API e nell'interfaccia utente.

L'obiettivo è garantire coerenza terminologica durante l'intero ciclo di sviluppo.

---

# Naming Convention

Il progetto utilizza esclusivamente i termini definiti in questo documento.

Termini differenti che descrivono lo stesso concetto devono essere evitati.

---

# Official Project Name

**TivuStream Privacy Intelligence Engine**

Acronimo ufficiale:

**PIE**

Questo rappresenta il nome del progetto.

Non deve essere abbreviato in altre forme.

---

# Official Product Name

**Network Privacy**

Network Privacy rappresenta il primo prodotto sviluppato sopra il Privacy Intelligence Engine.

Non rappresenta il nome del progetto.

---

# Official Score Name

**Network Privacy & Security Score**

Acronimo ufficiale:

**NPSS**

NPSS rappresenta il principale indicatore prodotto dal Privacy Intelligence Engine.

Sostituisce il precedente termine "Privacy Score".

---

# Core

Il termine **Core** identifica il motore principale del Privacy Intelligence Engine.

Comprende esclusivamente i moduli responsabili dell'elaborazione dei dati.

---

# Data Source

Qualsiasi sistema esterno che produce dati.

Esempi:

* Technitium DNS Server
* Privacy Assistant
* Pi-hole
* AdGuard Home

---

# Adapter

Componente incaricato di convertire i dati provenienti da una Data Source nel formato interno del progetto.

Ogni Data Source possiede un Adapter dedicato.

---

# Unified Data Model

Modello dati interno utilizzato dal Core.

Tutti i moduli comunicano esclusivamente attraverso questo modello.

---

# Capability

Dichiarazione di ciò che una Data Source è **in grado di fornire**.

Il vocabolario delle capability coincide con i nomi delle entità del Unified Data Model.

Una capability non descrive le funzionalità del prodotto esterno: le funzionalità di un backend costituiscono dati da analizzare, non capacità strutturali.

L'assenza di una capability significa che il dato non è misurabile, non che sia pari a zero.

---

# Storage

Componente del Backend responsabile della conservazione nel tempo delle acquisizioni e dei risultati prodotti dal Core.

Non esegue analisi e non è conosciuto dal Core.

---

# Observation Period

Intervallo temporale fisso al quale sono allineate le acquisizioni.

Un'acquisizione non è un insieme di eventi ma l'osservazione di un intervallo: due osservazioni di intervalli sovrapposti non sono sommabili.

Una nuova osservazione dello stesso periodo sostituisce la precedente. Un periodo concluso è immutabile.

---

# Coverage

Porzione del sistema di valutazione effettivamente osservata nel calcolo del Network Privacy & Security Score.

Si esprime come somma dei punteggi ottenibili di tutte le aree, su un massimo di 100.

Un'area può essere misurata per intero, in parte, o non essere misurabile. La porzione non osservata è esclusa dal calcolo e non può migliorare il punteggio.

Punteggi con copertura differente non sono confrontabili.

---

# Network Snapshot

Rappresentazione completa dello stato della rete in un determinato momento.

Costituisce la base per:

* report;
* confronti temporali;
* analisi storiche.

---

# Device

Qualsiasi dispositivo identificato dal sistema.

Esempi:

* Desktop
* Notebook
* Smartphone
* Smart TV
* NAS
* Router
* IoT

---

# Domain

Dominio osservato dal sistema durante l'analisi.

Un dominio è indipendente dal dispositivo che lo ha contattato.

---

# Domain Activity

Relazione tra un Device e un Domain.

Descrive il comportamento osservato.

---

# Threat

Evento o comportamento classificato come potenzialmente pericoloso.

Le categorie sono definite nella Threat Intelligence Specification.

---

# Alert

Notifica generata automaticamente dal Privacy Intelligence Engine.

Gli Alert possono avere differenti livelli di severità.

---

# Recommendation

Suggerimento prodotto dal sistema in seguito all'analisi.

Ogni Recommendation deve essere giustificata da uno o più eventi.

---

# Event

Messaggio interno utilizzato dai moduli del Core per comunicare tra loro.

Gli Event non vengono esposti direttamente al frontend.

---

# Backend

Software esterno che fornisce dati al Privacy Intelligence Engine.

Technitium rappresenta il primo backend supportato.

---

# Frontend

Interfaccia utente del progetto.

Il Frontend non contiene logica di analisi.

---

# REST API

Interfaccia pubblica del Privacy Intelligence Engine.

Rappresenta l'unico punto di accesso ufficiale ai dati elaborati dal Core.

---

# Dashboard

Interfaccia principale dell'applicazione.

Visualizza esclusivamente informazioni prodotte dal Core.

---

# Core Modules

I moduli che compongono il Core utilizzano tutti il suffisso **Engine**.

Questa forma rappresenta la nomenclatura ufficiale del progetto.

---

# Threat Engine

Modulo responsabile della classificazione delle minacce.

Acronimo suggerito:

**TE**

---

# Device Engine

Modulo responsabile dell'analisi dei dispositivi.

Acronimo suggerito:

**DE**

---

# NPSS Engine

Modulo responsabile del calcolo del Network Privacy & Security Score.

Acronimo suggerito:

**NE**

---

# Alert Engine

Modulo responsabile della generazione degli Alert.

Acronimo suggerito:

**AE**

---

# Recommendation Engine

Modulo responsabile della produzione delle raccomandazioni.

Acronimo suggerito:

**RE**

---

# Threat Intelligence

Materia trattata dal Threat Engine.

Il termine identifica la disciplina, non il componente software.

Come nome di modulo deve essere utilizzato **Threat Engine**.

---

# Acquisition Flow

Percorso seguito dai dati durante l'acquisizione periodica.

```text
Scheduler

↓

Adapter Manager

↓

Adapter

↓

Data Source

↓

Unified Data Model

↓

Core
```

L'acquisizione è orchestrata dall'Adapter Manager.

Il Core non partecipa all'orchestrazione.

---

# Query Flow

Percorso seguito da una richiesta proveniente dal Frontend.

```text
Frontend

↓

REST API

↓

Risultati prodotti dal Core
```

Il Query Flow non attiva alcuna comunicazione verso le Data Sources.

---

# Terminology Rules

All'interno del progetto:

* utilizzare sempre **PIE** per indicare il Privacy Intelligence Engine;
* utilizzare sempre **NPSS** per indicare il Network Privacy & Security Score;
* utilizzare sempre **Data Source** per indicare l'origine dei dati;
* utilizzare sempre **Adapter** per il livello di integrazione;
* utilizzare sempre **Core** per indicare il motore principale;
* utilizzare sempre il suffisso **Engine** per i moduli del Core;
* utilizzare sempre **Frontend** e **Backend** senza traduzioni.

---

# Terms to Avoid

Per garantire uniformità, evitare l'utilizzo dei seguenti termini quando esiste già un termine ufficiale.

| Evitare                                   | Utilizzare                              |
| ----------------------------------------- | --------------------------------------- |
| Privacy Score                             | Network Privacy & Security Score (NPSS) |
| Connector                                 | Adapter                                 |
| Source Provider                           | Data Source                             |
| DNS Engine                                | Backend                                 |
| Main Engine                               | Core                                    |
| Plugin (per integrazioni dati)            | Adapter                                 |
| Threat Intelligence (come nome di modulo) | Threat Engine                           |
| Device Intelligence (come nome di modulo) | Device Engine                           |

---

# Document References

Questo documento costituisce il riferimento terminologico per tutte le Specification presenti nella cartella `docs`.

Ogni nuovo documento dovrà utilizzare esclusivamente la terminologia definita nel presente Glossary.
