# 05 - Data Model

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Data Model Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il **Unified Data Model** utilizzato dal Privacy Intelligence Engine.

Il Data Model rappresenta il linguaggio comune attraverso il quale tutti i componenti del Core comunicano tra loro.

Ogni Adapter converte i dati provenienti dalle Data Sources in questo formato.

---

# Objectives

Il Unified Data Model garantisce:

* indipendenza dai backend;
* uniformità delle informazioni;
* semplicità di elaborazione;
* estendibilità;
* compatibilità tra i moduli del Core.

---

# Overview

Tutte le informazioni elaborate dal sistema vengono rappresentate attraverso un insieme di entità standardizzate.

```text id="tczvlt"
Data Source

↓

Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine
```

---

# Primary Entities

Il modello dati è composto dalle seguenti entità principali.

* DataSource
* NetworkSnapshot
* Statistics
* Device
* Domain
* DomainActivity
* Threat
* Alert
* Recommendation
* Network Privacy & Security Score (NPSS)
* ScoreComponent

---

# DataSource

Rappresenta l'origine delle informazioni elaborate dal sistema.

## Properties

* id
* name
* provider
* version
* status
* capabilities
* lastUpdate

---

## Capabilities

Il campo `capabilities` dichiara **quali entità del Unified Data Model quella Data Source è in grado di fornire**.

Non descrive le funzionalità del prodotto esterno.

Questa distinzione è essenziale.

* Il supporto di un backend a DNSSEC o a DNS over TLS è un **dato analizzato**, e trova posto in `Statistics` o nella configurazione.
* Una capability è invece un'informazione **strutturale**, che il Core utilizza per sapere quali analisi può eseguire.

Il vocabolario delle capability coincide con i nomi delle entità del Unified Data Model.

| Capability      | Significato                                          |
| --------------- | ---------------------------------------------------- |
| `Statistics`    | La sorgente fornisce statistiche aggregate            |
| `Device`        | La sorgente permette di identificare i dispositivi    |
| `Domain`        | La sorgente espone i domini osservati                 |
| `DomainActivity`| La sorgente correla dispositivi e domini              |

Nuove capability possono essere aggiunte quando vengono introdotte nuove entità.

Una capability assente significa che il dato **non è misurabile** con quella sorgente nella sua configurazione corrente.

Non significa che il dato sia pari a zero.

---

# NetworkSnapshot

Rappresenta lo stato completo della rete in un determinato momento.

## Properties

* snapshotId
* timestamp
* sourceId
* statistics
* devices
* alerts
* recommendations
* npss

Ogni Snapshot rappresenta un'istantanea completa del sistema.

---

# Statistics

Contiene le statistiche aggregate della rete.

## Properties

* totalQueries
* blockedQueries
* cachedQueries
* failedQueries
* uniqueDomains
* activeDevices
* encryptedQueries
* dnssecEnabled

---

# Device

Rappresenta un dispositivo identificato dal sistema.

## Properties

* deviceId
* hostname
* ipAddress
* macAddress
* vendor
* operatingSystem
* firstSeen
* lastSeen
* status

---

## Associated Objects

Ogni Device può essere associato a:

* DomainActivity
* Threat
* Alert
* Recommendation

---

# Domain

Rappresenta un dominio osservato durante l'analisi.

## Properties

* domain
* category
* reputation
* firstSeen
* lastSeen
* occurrences

Il campo `reputation` è **opzionale**.

La reputazione è prodotta dal Threat Engine. Un Adapter non la assegna mai e la lascia vuota.

Un valore assente significa che la reputazione non è ancora stata valutata: è un'affermazione sul sistema, non sul dominio.

---

# DomainActivity

Rappresenta l'interazione tra un Device e un Domain.

## Properties

* deviceId
* domain
* queryCount
* blocked
* protocol
* firstSeen
* lastSeen

---

## Conditional Availability

`DomainActivity` è l'unica entità la cui disponibilità **dipende dalla Data Source**.

Alcune sorgenti la forniscono nativamente. Altre la espongono solo dopo l'attivazione di componenti facoltativi. Altre ancora non la forniscono affatto.

Una Data Source dichiara di poterla fornire attraverso la capability `DomainActivity`.

Quando la capability è assente:

* il Core non produce oggetti `DomainActivity`;
* le analisi che ne dipendono vengono dichiarate non misurabili;
* il sistema genera una Recommendation che indica come rendere disponibile il dato.

L'assenza della capability non è un errore e non interrompe l'elaborazione.

Il Core non deve mai sostituire il dato mancante con valori stimati o predefiniti.

---

# Threat

Rappresenta una minaccia classificata dal sistema.

## Properties

* threatId
* category
* severity
* confidence
* description
* source
* detectedAt

---

# Alert

Rappresenta un evento significativo prodotto dal Core.

## Properties

* alertId
* category
* severity
* title
* description
* timestamp
* status

---

# Recommendation

Rappresenta un suggerimento generato automaticamente dal sistema.

## Properties

* recommendationId
* priority
* title
* description
* generatedAt

---

# Network Privacy & Security Score (NPSS)

Rappresenta il punteggio sintetico dello stato della rete.

## Properties

* overallScore
* status
* trend
* coverage
* algorithmVersion
* generatedAt
* breakdown

Il campo `coverage` indica la somma dei `maxScore` di tutte le aree, su un massimo di 100.

Corrisponde quindi alla porzione del sistema di valutazione effettivamente osservata.

Un valore inferiore a 100 significa che il punteggio è stato calcolato su una parte soltanto degli indicatori previsti.

Il campo `status` assume i valori definiti nella tabella Score Range della NPSS Specification.

Il campo `trend` assume i valori Improving, Stable o Decreasing.

Il campo `algorithmVersion` identifica la versione dell'algoritmo che ha prodotto il punteggio.

---

# ScoreComponent

Rappresenta il contributo di una singola area di valutazione al NPSS.

La collezione `breakdown` è composta da elementi di questo tipo.

## Properties

* component
* state
* score
* maxScore
* weight
* factors

Il campo `component` corrisponde a una delle aree definite nella NPSS Specification.

Il campo `state` dichiara in che misura l'area è stata valutata.

| Valore              | Significato                                                            |
| ------------------- | ----------------------------------------------------------------------- |
| `Measured`          | Tutti gli indicatori dell'area sono stati valutati                       |
| `PartiallyMeasured` | Solo una parte degli indicatori è stata valutata                         |
| `NotMeasurable`     | Nessun indicatore è valutabile e `score` non è significativo             |

---

## Weight and Maximum Score

Il campo `weight` indica il **peso nominale** dell'area, definito dall'algoritmo NPSS.

Il campo `maxScore` indica il **punteggio effettivamente ottenibile**, cioè la porzione di peso corrispondente agli indicatori realmente valutati.

| Stato               | Relazione                    |
| ------------------- | ---------------------------- |
| `Measured`          | `maxScore` uguale a `weight` |
| `PartiallyMeasured` | `maxScore` minore di `weight`|
| `NotMeasurable`     | `maxScore` uguale a zero     |

La porzione di peso non misurata **non concorre né al punteggio ottenuto né al punteggio ottenibile**.

Non può quindi in alcun caso migliorare il risultato: ciò che non è stato osservato viene escluso dal calcolo, non stimato né presunto favorevole.

---

Quando `state` è diverso da `Measured`, il campo `factors` deve indicare quali indicatori sono stati valutati e quali no, con il relativo motivo.

Il campo `factors` contiene i fattori che hanno determinato il punteggio dell'area.

La conservazione dei factors costituisce il requisito che rende ogni variazione del punteggio spiegabile.

Un'area non misurabile non viene mai rappresentata come area con punteggio zero.

Le condizioni descrivono situazioni distinte e devono restare distinguibili in ogni punto del sistema.

---

# Entity Relationships

```text id="w0d1t9"
NetworkSnapshot

├── Statistics

├── Device[]

│      ├── DomainActivity[]

│      ├── Threat[]

│      ├── Alert[]

│      └── Recommendation[]

├── NPSS

│      └── ScoreComponent[]

├── Alert[]

└── Recommendation[]
```

---

# Ownership of Properties

Non tutte le proprietà di un'entità sono di competenza di chi la produce per primo.

Un Adapter acquisisce e converte. Non classifica, non valuta, non attribuisce reputazioni: quelle sono responsabilità del Core.

Ne discende una regola.

> Una proprietà che soltanto il Core può valorizzare è opzionale nel modello, e resta vuota finché il Core non la valorizza.

Un Adapter non deve mai riempire tali proprietà con valori convenzionali o segnaposto: equivarrebbe ad affermare qualcosa che non ha osservato.

Fanno eccezione i casi in cui la documentazione definisce esplicitamente un valore di ricaduta, come `Unknown` per la categoria di un dominio.

Proprietà attualmente soggette a questa regola.

| Entità   | Proprietà    | Assegnata da  |
| -------- | ------------ | ------------- |
| `Domain` | `reputation` | Threat Engine |

---

## Status Vocabularies

Gli stati operativi utilizzano insiemi di valori definiti.

**DataSource.status**

| Valore        | Significato                                     |
| ------------- | ------------------------------------------------ |
| `Online`      | La sorgente ha risposto correttamente             |
| `Unreachable` | La sorgente non è raggiungibile o ha rifiutato    |

È l'unica parte della descrizione che soltanto l'Adapter può stabilire.

**Device.status**

| Valore     | Significato                                          |
| ---------- | ----------------------------------------------------- |
| `Active`   | Il dispositivo ha prodotto traffico nella finestra     |
| `Inactive` | Il dispositivo non ha prodotto traffico nella finestra |

Lo stato descrive la presenza, non la salute. La valutazione del comportamento appartiene al Device Engine.

Entrambi gli insiemi sono minimi e potranno essere estesi quando emergeranno esigenze documentate.

---

# Data Availability

Il modello distingue tre condizioni che non devono mai essere confuse.

| Condizione        | Significato                                                  |
| ----------------- | ------------------------------------------------------------ |
| Dato presente     | L'informazione è stata acquisita ed è valorizzata             |
| Dato assente      | L'informazione è misurabile ma non si è verificato nulla      |
| Dato non misurabile | La sorgente non è in grado di fornire l'informazione        |

Un valore pari a zero appartiene alla seconda condizione e costituisce un'affermazione sul mondo reale.

La terza condizione è invece un'affermazione sul sistema, e va rappresentata attraverso l'assenza della relativa capability, mai attraverso un valore.

Questa distinzione discende direttamente dal principio di Transparency.

---

# Entity Identity

Ogni entità possiede un identificatore univoco.

Gli identificatori sono indipendenti dal backend utilizzato.

---

# Versioning

Il Unified Data Model possiede una propria versione indipendente.

Le modifiche incompatibili incrementano la Major Version.

Le modifiche compatibili incrementano la Minor Version.

---

# Serialization

Il Unified Data Model deve poter essere serializzato nei seguenti formati.

* JSON
* CSV
* XML (eventuale supporto futuro)

La serializzazione non modifica la struttura logica delle entità.

---

# Extensibility

Nuove proprietà possono essere aggiunte mantenendo la retrocompatibilità.

Nuove entità devono integrarsi attraverso relazioni già definite.

---

# Design Principles

Il Data Model segue i seguenti principi.

* uniformità;
* indipendenza;
* semplicità;
* modularità;
* estendibilità;
* riutilizzabilità.

---

# Constraints

Il Unified Data Model:

* non contiene logica di business;
* non contiene logica di presentazione;
* non dipende da uno specifico backend;
* rappresenta l'unico formato dati utilizzato dal Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 06 - API
* 07 - Network Privacy & Security Score
