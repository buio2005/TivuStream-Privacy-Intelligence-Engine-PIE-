# 00 - Glossary

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Glossary Specification

**Version:** 1.0.0

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

# Threat Intelligence

Modulo responsabile della classificazione delle minacce.

Acronimo suggerito:

**TI**

---

# Device Intelligence

Modulo responsabile dell'analisi dei dispositivi.

Acronimo suggerito:

**DI**

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

# Terminology Rules

All'interno del progetto:

* utilizzare sempre **PIE** per indicare il Privacy Intelligence Engine;
* utilizzare sempre **NPSS** per indicare il Network Privacy & Security Score;
* utilizzare sempre **Data Source** per indicare l'origine dei dati;
* utilizzare sempre **Adapter** per il livello di integrazione;
* utilizzare sempre **Core** per indicare il motore principale;
* utilizzare sempre **Frontend** e **Backend** senza traduzioni.

---

# Terms to Avoid

Per garantire uniformità, evitare l'utilizzo dei seguenti termini quando esiste già un termine ufficiale.

| Evitare                        | Utilizzare                              |
| ------------------------------ | --------------------------------------- |
| Privacy Score                  | Network Privacy & Security Score (NPSS) |
| Connector                      | Adapter                                 |
| Source Provider                | Data Source                             |
| DNS Engine                     | Backend                                 |
| Main Engine                    | Core                                    |
| Plugin (per integrazioni dati) | Adapter                                 |

---

# Document References

Questo documento costituisce il riferimento terminologico per tutte le Specification presenti nella cartella `docs`.

Ogni nuovo documento dovrà utilizzare esclusivamente la terminologia definita nel presente Glossary.
