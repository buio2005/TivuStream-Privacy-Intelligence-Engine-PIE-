# 04 - Technitium Integration

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Technitium Integration Specification

**Version:** 1.4.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce le modalità di integrazione tra il Privacy Intelligence Engine (PIE) e Technitium DNS Server.

Technitium rappresenta il primo backend ufficialmente supportato dal progetto.

---

# Scope

L'integrazione ha lo scopo di acquisire dati dal backend DNS e renderli disponibili al Privacy Intelligence Engine attraverso il relativo Adapter.

La gestione del DNS rimane completamente delegata a Technitium.

---

# Architectural Role

Technitium costituisce una **Data Source**.

Non appartiene al Core del progetto.

Non contiene logica di analisi sviluppata da PIE.

La sua responsabilità termina con la produzione dei dati.

---

# Responsibilities

## Technitium

Technitium gestisce:

* DNS Resolution
* Cache
* DNSSEC
* DNS over HTTPS (DoH)
* DNS over TLS (DoT)
* DNS over QUIC (DoQ)
* Zone Management
* Query Logging
* Statistics
* Blocklists

---

## Privacy Intelligence Engine

PIE gestisce:

* acquisizione dati;
* normalizzazione;
* classificazione;
* correlazione;
* analisi;
* calcolo del NPSS;
* generazione di Alert;
* generazione di Recommendations;
* produzione dei Report.

---

# Integration Architecture

```text id="vtahj0"
Technitium DNS Server

↓

HTTP API

↓

Technitium Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine
```

---

# Adapter Responsibilities

L'Adapter rappresenta l'unico componente autorizzato a comunicare con Technitium.

Le sue responsabilità comprendono:

* autenticazione;
* gestione delle richieste HTTP;
* conversione dei dati;
* gestione degli errori;
* normalizzazione del formato.

L'Adapter non esegue alcuna elaborazione.

---

# Retrieved Data

L'integrazione acquisisce le seguenti informazioni.

## Server Information

* versione;
* stato operativo;
* configurazione.

---

## DNS Statistics

* richieste DNS;
* richieste bloccate;
* cache;
* errori;
* protocolli utilizzati;
* statistiche generali.

---

## Client Information

* indirizzi IP;
* hostname (quando disponibili);
* statistiche di attività.

---

## Domains

* domini osservati;
* domini bloccati;
* frequenza delle richieste.

---

## Blocklists

* stato;
* ultimo aggiornamento;
* numero di domini gestiti.

---

## Logs

Technitium distingue due tipologie di log.

**Log diagnostici del server.** Sempre disponibili. Contengono eventi di sistema e informazioni di funzionamento.

**Log delle query.** Non disponibili in un'installazione predefinita. Richiedono l'attivazione di un componente facoltativo.

La presenza del componente viene rilevata interrogando l'elenco delle applicazioni installate.

L'Adapter dichiara la capability `DomainActivity` **soltanto quando il componente risulta effettivamente installato**. L'implementazione della relativa interfaccia esprime ciò che l'Adapter sa fare; la capability esprime ciò che quella istanza offre in quel momento.

L'aggregazione avviene mentre le pagine dei log vengono lette. Il registro puntuale delle interrogazioni non viene mai trattenuto per intero e non oltrepassa l'Adapter.

---

# Data Availability Levels

L'integrazione con Technitium prevede due livelli di disponibilità dei dati.

La distinzione è strutturale e va dichiarata attraverso le capability della Data Source.

---

## Base Level

Disponibile su qualunque installazione, senza componenti aggiuntivi e senza costi.

Capability dichiarate.

```text
Statistics
Device
Domain
SourceConfiguration
```

Dati acquisibili.

* statistiche aggregate della rete;
* configurazione del servizio DNS, compresi DNSSEC e protocolli cifrati;
* stato delle blocklist;
* elenco dei dispositivi con il relativo volume di traffico;
* elenco dei domini osservati con la relativa frequenza.

Tutti i valori di questo livello sono **esatti**, non stimati.

---

## Extended Level

Richiede l'installazione di un componente facoltativo di Technitium dedicato alla registrazione delle query.

Capability dichiarate.

```text
Statistics
Device
Domain
SourceConfiguration
DomainActivity
```

Dato aggiuntivo.

* correlazione fra dispositivo e dominio.

Questa correlazione è il presupposto dell'attribuzione delle minacce ai dispositivi e della valutazione dell'area Device Health del NPSS.

---

## Rationale

Le API di dashboard di Technitium espongono i dispositivi e i domini come **aggregati indipendenti**.

Indicano quante interrogazioni ha prodotto ciascun dispositivo e quante volte è stato richiesto ciascun dominio, ma non quale dispositivo abbia contattato quale dominio.

La correlazione esiste soltanto nei log delle query.

Questa è una caratteristica specifica di Technitium e non del dominio applicativo: altre Data Sources previste dal progetto espongono il dato nativamente.

Per questo motivo il livello esteso non costituisce un prerequisito del progetto.

---

# Data Conversion

Tutti i dati recuperati vengono convertiti nel formato definito dal Unified Data Model.

Nessun componente del Core utilizza direttamente il formato originale restituito da Technitium.

---

# Backend Independence

Il Core non contiene riferimenti specifici a Technitium.

La sostituzione del backend richiede esclusivamente la realizzazione di un nuovo Adapter.

---

# Error Management

Gli errori restituiti da Technitium vengono convertiti in eventi standardizzati.

Il Core riceve esclusivamente informazioni normalizzate.

---

# Authentication

L'Adapter si autentica utilizzando un **API Token** non scadente, previsto da Technitium per l'automazione.

Un token di sessione ordinario non è adatto: scade per inattività e richiederebbe la gestione del ciclo di vita della sessione all'interno dell'Acquisition Flow.

Il token viene trasmesso nell'intestazione della richiesta.

```text
Authorization: Bearer <token>
```

---

## Least Privilege

Il token deve appartenere a un **utente dedicato con permessi minimi**.

Sono richiesti i permessi di **sola lettura** sulle sezioni **Dashboard** e **Settings**.

Il permesso sulle impostazioni è necessario per acquisire la configurazione del server, dalla quale dipendono le aree DNS Security e Configuration del punteggio. Senza di esso quelle aree risulterebbero in larga parte non misurabili.

Nessun permesso di modifica è richiesto in alcun caso: PIE non altera mai la configurazione della Data Source.

L'utilizzo dell'utente amministrativo è sconsigliato: un token compromesso erediterebbe privilegi non necessari all'acquisizione, compresa la facoltà di modificare il server.

---

## Session Information

```text
GET /api/user/session/get
```

Una sola chiamata restituisce versione del server, nome configurato, stato della validazione DNSSEC e **permessi effettivi del token**.

Questa chiamata è la fonte da utilizzare per la descrizione della Data Source.

Due conseguenze rilevanti.

* Lo stato di DNSSEC è disponibile senza accedere alle impostazioni del server, quindi senza richiedere permessi oltre a quelli dell'acquisizione.
* I permessi restituiti consentono all'Adapter di dichiarare una capability soltanto quando il token è realmente in grado di leggere il dato corrispondente.

Una versione precedente di questa specifica indicava le impostazioni come fonte dello stato DNSSEC. L'indicazione è superata: le impostazioni richiedono permessi più ampi e non sono necessarie al livello base.

---

## Response Shapes

Le API utilizzano **due forme di risposta differenti**.

**Payload annidato.** La maggior parte delle chiamate, comprese tutte quelle del dashboard, racchiude il contenuto in una proprietà dedicata accanto all'esito.

```json
{ "status": "ok", "response": { } }
```

**Payload alla radice.** Le chiamate relative alla sessione restituiscono i propri campi direttamente al primo livello, accanto all'esito.

```json
{ "status": "ok", "info": { } }
```

L'Adapter deve supportare entrambe le forme.

La distinzione non è deducibile dal nome della chiamata e va verificata caso per caso.

---

## Error Semantics

Le API di Technitium **non esprimono l'esito attraverso il codice di stato HTTP**.

Una risposta con esito negativo può presentarsi con codice HTTP 200 e riportare l'errore nel corpo.

L'Adapter deve quindi valutare sempre il contenuto della risposta e non limitarsi al codice di stato.

Le risposte di errore possono contenere dettagli diagnostici del backend. Tali dettagli non devono essere propagati oltre l'Adapter né registrati nei log.

---

# Acquisition Constraints

La ricognizione delle API ha evidenziato tre vincoli che l'Adapter deve rispettare.

---

## Time Window Selection

Le statistiche di Technitium sono aggregate su finestre temporali predefinite.

Finestre differenti **non contengono necessariamente gli stessi dati**: è stato osservato che la finestra predefinita relativa all'ultimo giorno restituisce il dettaglio per dominio vuoto pur riportando un numero di interrogazioni superiore.

L'Adapter non può quindi assumere che una finestra più ampia comprenda quanto contenuto in una più stretta.

La finestra utilizzata per ciascun tipo di dato va scelta in modo esplicito e documentato.

**L'intervallo personalizzato conserva il dettaglio completo.** La verifica su istanza reale ha confrontato le statistiche acquisite con quelle mostrate dalla console del server, riscontrando corrispondenza esatta di conteggi, domini distinti e client.

L'Adapter utilizza pertanto l'intervallo personalizzato, che consente l'acquisizione incrementale senza doversi ricondurre alle finestre predefinite.

---

## Log Rotation

Il componente di registrazione delle query applica una ritenzione basata sul **numero di record**, oltre che sul tempo.

Il limite predefinito corrisponde, su una rete domestica reale, a una frazione di ora di traffico.

Ne discende un vincolo operativo.

* La frequenza di acquisizione deve essere **inferiore al tempo di rotazione del log**.
* Se la frequenza è insufficiente, i dati vengono perduti **senza alcuna segnalazione da parte del backend**.

L'Adapter deve leggere la configurazione del componente e segnalare l'incoerenza fra ritenzione configurata e frequenza di acquisizione.

Questa verifica è obbligatoria: una perdita silenziosa di dati produce analisi plausibili e sbagliate, che è la condizione peggiore per uno strumento di questo tipo.

---

## Failed Queries

Technitium distingue quattro esiti negativi: `totalServerFailure`, `totalRefused`, `totalDropped` e `totalNxDomain`.

Il Unified Data Model prevede un unico contatore `failedQueries`, che comprende i primi tre.

```text
failedQueries = totalServerFailure + totalRefused + totalDropped
```

**NXDOMAIN è escluso.** Indica che il dominio richiesto non esiste: è una risposta corretta del servizio, non un suo malfunzionamento.

Su una rete normale gli NXDOMAIN sono frequenti. Includerli produrrebbe un tasso di errore elevato in assenza di qualsiasi problema, e abbasserebbe indebitamente l'area Network Integrity del punteggio.

---

## Device Identity

Technitium identifica i client esclusivamente tramite indirizzo di rete.

L'Adapter deriva l'identificatore del dispositivo dall'indirizzo in modo deterministico, così che lo stesso dispositivo mantenga la propria identità fra acquisizioni successive.

Ne discende un limite da dichiarare all'utente.

* Un dispositivo che cambia indirizzo compare come dispositivo differente.
* Un indirizzo riassegnato a un altro dispositivo fonde le due identità.

Il limite è inerente all'identificazione per indirizzo. Si attenua utilizzando prenotazioni DHCP, e si supera soltanto disponendo dell'indirizzo hardware, che Technitium espone unicamente quando svolge anche il ruolo di server DHCP.

---

## Truncated Lists

Le chiamate che restituiscono classifiche applicano un limite massimo di elementi.

Non esiste una chiamata che restituisca l'elenco completo dei domini osservati.

I valori che dipendono dalla completezza dell'elenco, come il numero di domini univoci, sono pertanto **approssimati** e devono essere dichiarati tali.

---

# Aggregation Responsibility

Quando il livello esteso è disponibile, l'Adapter **aggrega i log delle query prima di consegnarli al Core**.

Il Core riceve oggetti `DomainActivity` già consolidati e non accede mai alla singola interrogazione.

Questa scelta risponde a tre esigenze.

* **Privacy.** Il registro puntuale delle interrogazioni costituisce la cronologia di navigazione di ogni dispositivo. PIE non lo conserva.
* **Volume.** L'aggregato è di ordini di grandezza inferiore al dato grezzo.
* **Separazione delle responsabilità.** Il Core opera sul Unified Data Model e non su formati specifici di un backend.

---

# Security

Le credenziali di accesso alle API rimangono confinate all'interno dell'Adapter.

Il Frontend non comunica mai direttamente con Technitium.

---

# Compatibility

PIE mantiene la massima compatibilità possibile con le API pubbliche di Technitium.

Non vengono modificate componenti del progetto originale.

Non viene realizzato alcun fork.

---

# Update Strategy

L'evoluzione di Technitium rimane indipendente da quella del Privacy Intelligence Engine.

L'Adapter rappresenta il livello di compatibilità tra le due piattaforme.

---

# Design Principles

L'integrazione segue i seguenti principi.

* utilizzo esclusivo delle API pubbliche;
* assenza di modifiche al codice di Technitium;
* completa separazione tra backend e Core;
* indipendenza architetturale;
* modularità.

---

# Constraints

L'integrazione non introduce:

* dipendenze dirette nel Core;
* logica di analisi;
* personalizzazioni del backend;
* modifiche ai componenti originali di Technitium.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
* 09 - Network Privacy
