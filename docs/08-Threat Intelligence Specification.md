# 08 - Threat Intelligence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Threat Intelligence Specification

**Version:** 1.3.0

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

Il progetto adotta esclusivamente liste la cui licenza ne consenta il download da parte dell'utente.

Le liste predefinite provengono dal **Block List Project**, distribuito in pubblico dominio con licenza Unlicense, nel formato a un dominio per riga.

| Lista            | Categoria      |
| ---------------- | -------------- |
| `ads`            | `Advertising`  |
| `tracking`       | `Tracking`     |
| `malware`        | `Malware`      |
| `phishing`       | `Phishing`     |
| `crypto`         | `Cryptomining` |
| `scam`           | `Suspicious`   |
| `abuse`          | `Suspicious`   |

La ricerca che ha portato a questa scelta, comprese le fonti esaminate e scartate, è documentata separatamente.

### Single Source

Le liste predefinite provengono da **una sola fonte**.

I suoi errori diventano i nostri, e i suoi silenzi diventano `Unknown`.

La condizione non deriva da una preferenza. Fra le fonti liberamente utilizzabili, poche sono segmentate per categoria, e le poche che lo sono si alimentano a vicenda: adottarne due darebbe l'aspetto di pareri indipendenti senza esserlo, e un accordo apparente è peggio di una dipendenza dichiarata.

L'utente può aggiungere fonti proprie in qualunque momento.

### Categories Without A Source

Le categorie `Analytics`, `Social`, `Streaming`, `Cloud` e `AI Services` non hanno alcuna lista predefinita.

I domini che vi apparterrebbero restano `Unknown`.

Nessuna categoria viene attribuita per riempire un vuoto.

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

La ricerca si arresta al primo livello che produce una corrispondenza. Se a quel livello corrispondono più liste, l'esito è determinato dalla sezione Competing Classifications.

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

## Competing Classifications

Un dominio può comparire in **più liste con categorie diverse**. Un dominio pubblicitario che traccia anche l'utente appartiene legittimamente a entrambe.

La corrispondenza si risolve in tre passaggi, applicati in quest'ordine.

### 1. Vince il nome più vicino

Una corrispondenza sul nome completo prevale su una corrispondenza ottenuta risalendo, **qualunque sia la categoria**.

La specificità è un segnale più forte della gravità: una lista che nomina `analytics.example.com` sta dicendo qualcosa su quel nome, mentre una lista che nomina `example.com` sta dicendo qualcosa sul dominio padre. Lasciare che la seconda prevalga significherebbe sostituire un'affermazione diretta con un'inferenza.

### 2. A parità di distanza, vince la categoria più grave

L'ordine di gravità è il seguente.

| Ordine | Categoria      | Natura                      |
| ------ | -------------- | --------------------------- |
| 1      | `Malware`      | Sicurezza                    |
| 2      | `Phishing`     | Sicurezza                    |
| 3      | `Cryptomining` | Sicurezza                    |
| 4      | `Suspicious`   | Sicurezza, non confermata    |
| 5      | `Tracking`     | Privacy                      |
| 6      | `Analytics`    | Privacy                      |
| 7      | `Advertising`  | Privacy                      |
| 8      | `Social`       | Descrittiva                  |
| 9      | `Streaming`    | Descrittiva                  |
| 10     | `Cloud`        | Descrittiva                  |
| 11     | `AI Services`  | Descrittiva                  |
| 12     | `Unknown`      | Assenza di classificazione   |

L'ordine è un **giudizio editoriale dichiarato**, come i pesi del Network Privacy & Security Score. Non deriva da una misura e non pretende di derivarne.

Le ragioni.

* La sicurezza precede la privacy. Un dominio che traccia e distribuisce malware va presentato come minaccia, non come fastidio.
* `Suspicious` precede le categorie di privacy perché segnala un pericolo possibile, sul quale l'utente può agire, mentre `Tracking` segnala un comportamento certo ma di gravità inferiore.
* Fra le categorie di privacy, `Tracking` precede `Analytics`, che precede `Advertising`: la prima riguarda la persona, l'ultima il contenuto.
* Le categorie descrittive non esprimono un giudizio e cedono a qualunque categoria che ne esprima uno.

### 3. A parità di gravità, vince la lista più recente

Quando due liste della stessa categoria rivendicano lo stesso nome, prevale quella aggiornata più di recente.

Una lista mai aggiornata cede a qualunque lista aggiornata. Fra due liste equivalenti anche su questo, prevale quella il cui nome viene prima in ordine alfabetico, affinché lo stesso insieme di liste produca sempre lo stesso risultato.

### What Is Lost

Il sistema mostra **una sola categoria**.

Le altre categorie nelle quali il dominio compare non vengono presentate.

La perdita è reale e viene dichiarata. La classificazione mostrata è quella più grave fra quelle trovate, non l'unica trovata, e l'interfaccia non deve suggerire il contrario.

Rappresentare tutte le categorie di un dominio richiede una modifica del Unified Data Model e resta una possibilità aperta, non una decisione presa.

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
