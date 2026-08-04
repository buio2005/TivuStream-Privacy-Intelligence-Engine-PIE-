# 08 - Threat Intelligence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Threat Intelligence Specification

**Version:** 1.2.1

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il sistema di **Threat Intelligence** del Privacy Intelligence Engine.

Threat Intelligence identifica la materia trattata.

Il modulo che la implementa è il **Threat Engine**.

Il Threat Engine è responsabile dell'identificazione, classificazione e valutazione delle minacce rilevate durante l'analisi dei dati provenienti dalle Data Sources.

---

# Objectives

Il Threat Engine ha i seguenti obiettivi.

* identificare domini potenzialmente pericolosi;
* classificare le minacce;
* attribuire un livello di gravità;
* supportare il calcolo del NPSS;
* generare Alert;
* generare Recommendations.

---

# Scope

Il Threat Engine analizza esclusivamente le informazioni presenti nel Unified Data Model.

Non comunica direttamente con le Data Sources.

Non gestisce l'interfaccia utente.

---

# Threat Classification

Ogni dominio osservato viene classificato in una categoria.

Le categorie rappresentano il livello logico utilizzato dall'intero ecosistema PIE.

---

# Primary Categories

## Malware

Domini associati alla distribuzione di software malevolo.

---

## Phishing

Domini progettati per sottrarre credenziali o dati personali.

---

## Tracking

Domini utilizzati per il monitoraggio dell'attività degli utenti.

---

## Advertising

Domini utilizzati per la distribuzione di contenuti pubblicitari.

---

## Analytics

Domini utilizzati per la raccolta di statistiche e dati di utilizzo.

---

## Cryptomining

Domini associati ad attività di mining di criptovalute.

---

## Suspicious

Domini con comportamento anomalo o reputazione incerta.

---

## Social

Domini appartenenti a piattaforme social.

---

## Streaming

Domini dedicati alla distribuzione di contenuti multimediali.

---

## Cloud

Servizi cloud e infrastrutture distribuite.

---

## AI Services

Servizi dedicati all'intelligenza artificiale.

---

## Unknown

Categoria assegnata quando non è possibile classificare il dominio.

---

# Threat Severity

Ogni Threat possiede un livello di severità.

Livelli previsti.

* Informational
* Low
* Medium
* High
* Critical

---

# Confidence Level

Ogni classificazione possiede un livello di affidabilità.

Valori previsti.

* Low
* Medium
* High

Il livello di confidenza permette di distinguere una classificazione certa da una classificazione probabilistica.

---

# Threat Sources

Le classificazioni sono ottenute da:

* liste locali, scaricate periodicamente;
* regole interne;
* algoritmi di correlazione.

La provenienza della classificazione viene sempre registrata.

---

## Local Classification Only

La corrispondenza avviene **esclusivamente sul dispositivo**.

I domini contattati dalla rete dell'utente **non vengono mai trasmessi a terzi**, per alcuna finalità, compresa la consultazione di servizi di reputazione.

La motivazione è diretta: un dominio interrogato rivela cosa un dispositivo stava facendo. Consultare un servizio esterno per stabilire se un dominio sia pericoloso significherebbe comunicare a quel servizio la cronologia della rete che si sta proteggendo.

Uno strumento che analizza la privacy non può ottenere i propri risultati riducendola.

Conseguenze accettate.

* Le minacce comparse di recente vengono riconosciute con il ritardo di aggiornamento delle liste.
* L'accuratezza dipende dalla qualità delle liste adottate.
* Il sistema funziona anche in assenza di connessione verso l'esterno.

Le liste vengono scaricate periodicamente. Il download riguarda le liste, mai i domini osservati: nessuna informazione sulla rete dell'utente lascia il dispositivo in quell'occasione.

---

# Classification Lists

Una lista di classificazione associa domini a una categoria.

---

## List Properties

Ogni lista dichiara.

| Proprietà       | Significato                                            |
| --------------- | ------------------------------------------------------ |
| `name`          | Nome della lista                                        |
| `sourceUrl`     | Indirizzo dal quale viene scaricata                     |
| `category`      | Categoria attribuita ai domini che contiene             |
| `licence`       | Licenza della lista                                     |
| `updatedAt`     | Momento dell'ultimo aggiornamento riuscito              |
| `entryCount`    | Numero di domini contenuti                              |
| `enabled`       | Se la lista partecipa alla classificazione              |

La licenza è **obbligatoria**. Una lista priva di licenza dichiarata non viene distribuita con il progetto.

---

## Default Lists

L'insieme delle liste predefinite **non è definito da questa specifica**.

La scelta richiede la verifica della licenza, della manutenzione attiva e della qualità della categorizzazione di ciascuna fonte, e costituisce una decisione da assumere esplicitamente prima del rilascio pubblico.

Il progetto adotta esclusivamente liste la cui licenza ne consenta la distribuzione o il download da parte dell'utente.

---

## User Lists

L'utente può aggiungere, disattivare e rimuovere liste.

Trattandosi di software self-hosted, la scelta delle fonti appartiene a chi lo utilizza.

---

# Matching

La corrispondenza avviene **esclusivamente sul dispositivo**, confrontando i domini osservati con le liste conservate localmente.

---

## Matching Rule

Il confronto procede dal nome completo verso l'alto, rimuovendo una etichetta alla volta.

```text
tracker.ads.example.com
        ads.example.com
            example.com
```

La ricerca si arresta alla prima corrispondenza.

Un dominio elencato in una lista si intende comprensivo dei propri sottodomini: è la convenzione adottata dalle liste stesse, e ignorarla renderebbe inefficace la classificazione.

---

## Confidence

Il livello di confidenza dipende da **come** la corrispondenza è stata ottenuta.

| Corrispondenza                        | Confidenza |
| ------------------------------------- | ---------- |
| Nome completo presente in lista        | `High`     |
| Corrispondenza su un dominio superiore | `Medium`   |

Il secondo caso è un'inferenza: la lista afferma qualcosa sul dominio padre, e il sistema estende l'affermazione al sottodominio osservato.

L'inferenza è ragionevole e resta un'inferenza. Dichiararla con confidenza inferiore permette all'utente di distinguerla da un'affermazione diretta.

---

## Unknown Domains

Un dominio che non compare in alcuna lista riceve la categoria `Unknown`.

`Unknown` significa **non classificato**, non innocuo.

L'interfaccia non presenta mai un dominio non classificato come sicuro: sarebbe un'affermazione che il sistema non ha verificato.

---

# Freshness

Ogni classificazione dichiara **l'età della lista dalla quale proviene**.

La classificazione locale comporta un ritardo nel riconoscimento delle minacce comparse di recente. Il ritardo non viene nascosto: viene misurato e dichiarato, come la copertura del punteggio e la qualità delle misure.

Una classificazione prodotta da una lista aggiornata sei giorni prima è un'informazione diversa da una prodotta il giorno stesso, e il sistema le distingue.

---

# Update Policy

Le liste vengono aggiornate a intervallo configurabile.

---

## Failure Handling

Un aggiornamento non riuscito **non invalida la lista esistente**.

Il sistema continua a utilizzare la versione conservata e ne dichiara l'età crescente.

Una lista non aggiornabile è meno utile di una recente e più utile di nessuna lista.

---

## Offline Operation

In assenza di connettività il sistema continua a classificare con le liste conservate.

Nessuna funzione di analisi dipende dalla disponibilità della rete: l'unica conseguenza dell'assenza di connessione è l'invecchiamento delle liste, dichiarato all'utente.

---

## Storage

Le liste sono conservate localmente in forma ispezionabile.

Il contenuto di ogni lista risiede in un **file di testo**, nel formato in cui è stato scaricato. La descrizione della lista risiede nel database.

L'utente può quindi aprire una lista con un editor qualsiasi, verificare quali domini il sistema considera appartenenti a una categoria, e comprendere il motivo di una classificazione anziché doverla accettare.

La collocazione dei file è definita dalla Persistence Specification.

---

# Threat Lifecycle

Ogni Threat attraversa un ciclo di vita.

```text id="zq54ga"
Detected

↓

Classified

↓

Evaluated

↓

Alert Generated

↓

Recommendation Generated

↓

Archived
```

---

# Correlation

Il Threat Engine può correlare eventi provenienti da differenti Data Sources.

La correlazione consente di migliorare la precisione della classificazione.

---

# Domain Reputation

Per ogni dominio il sistema mantiene un indice di reputazione.

La reputazione contribuisce alla classificazione della minaccia.

---

# Threat History

Ogni Threat mantiene il proprio storico.

Informazioni registrate.

* prima rilevazione;
* ultima rilevazione;
* numero di occorrenze;
* stato corrente.

---

# Alert Generation

Il Threat Engine può generare Alert quando vengono rilevate condizioni significative.

La severità dell'Alert dipende dalla gravità della minaccia.

---

# Recommendation Generation

Ogni Threat può produrre una o più Recommendations.

Ogni suggerimento è collegato alla minaccia che lo ha generato.

---

# NPSS Integration

Il Threat Engine contribuisce direttamente al calcolo del Network Privacy & Security Score.

La presenza di Threat critici riduce il punteggio complessivo.

---

# Extensibility

Nuove categorie possono essere aggiunte mantenendo la compatibilità con il modello esistente.

Le classificazioni già esistenti non vengono modificate.

---

# Design Principles

Il Threat Engine segue i seguenti principi.

* uniformità;
* modularità;
* trasparenza;
* indipendenza dal backend;
* estendibilità;
* riproducibilità.

---

# Constraints

Il Threat Engine:

* non modifica i dati originali;
* non comunica direttamente con il Frontend;
* utilizza esclusivamente il Unified Data Model;
* non dipende da una specifica Data Source.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 09 - Network Privacy
