# 09 - Network Privacy

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Network Privacy Specification

**Version:** 1.6.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce **Network Privacy**, la prima applicazione sviluppata utilizzando il Privacy Intelligence Engine (PIE).

Network Privacy rappresenta il livello di presentazione dell'ecosistema PIE e consente all'utente di visualizzare, comprendere e gestire le informazioni prodotte dal Core.

---

# Objectives

L'applicazione ha i seguenti obiettivi.

* presentare informazioni in modo semplice;
* visualizzare lo stato della rete;
* mostrare il Network Privacy & Security Score (NPSS);
* evidenziare minacce e anomalie;
* fornire raccomandazioni operative;
* semplificare l'analisi della rete.

---

# Scope

Network Privacy è un'applicazione.

Non è un motore di analisi.

Non implementa algoritmi di classificazione.

Non comunica direttamente con le Data Sources.

Utilizza esclusivamente le API pubbliche del Privacy Intelligence Engine.

---

# Architecture

Network Privacy opera esclusivamente all'interno del Query Flow.

```text id="g1nvw8"
Frontend

↓

REST API

↓

Risultati prodotti dal Privacy Intelligence Engine
```

L'applicazione non partecipa in alcun modo all'Acquisition Flow.

---

# Main Dashboard

La Dashboard rappresenta il punto di ingresso dell'applicazione.

Visualizza una panoramica dello stato della rete.

---

# Primary Widgets

La Dashboard comprende i seguenti componenti.

* Network Privacy & Security Score
* Network Status
* Devices
* Threats
* Alerts
* Recommendations
* Statistics
* Activity Timeline

---

# Navigation

L'applicazione è organizzata nelle seguenti sezioni.

* Dashboard
* Devices
* Domains
* Threats
* Alerts
* Recommendations
* Statistics
* Reports
* Settings

---

# Devices

La sezione Devices visualizza tutti i dispositivi rilevati.

Per ogni dispositivo vengono mostrate le principali informazioni.

* nome;
* indirizzo IP;
* stato;
* attività;
* Alert;
* Threat;
* statistiche.

---

# Domains

La sezione Domains visualizza i domini osservati dal sistema.

Per ogni dominio vengono mostrati.

* categoria;
* reputazione;
* numero di richieste;
* dispositivi coinvolti.

---

# Threats

La sezione Threats presenta tutte le minacce classificate dal Core.

Le informazioni possono essere filtrate e ordinate.

---

# Alerts

La sezione Alerts mostra gli eventi generati automaticamente dal sistema.

Gli Alert sono organizzati per severità.

---

# Recommendations

La sezione Recommendations raccoglie tutti i suggerimenti prodotti dal Core.

Ogni raccomandazione è collegata agli eventi che l'hanno generata.

---

# Statistics

La sezione Statistics presenta dati aggregati relativi alla rete.

Comprende:

* traffico DNS;
* query;
* domini;
* cache;
* dispositivi;
* protocolli.

---

# Reports

L'applicazione consente la generazione di report.

Formati previsti.

* PDF
* CSV
* JSON

---

# Search

L'applicazione include un sistema di ricerca globale.

La ricerca consente di individuare rapidamente:

* dispositivi;
* domini;
* Threat;
* Alert.

---

# Filters

Ogni elenco supporta filtri dinamici.

Esempi.

* categoria;
* severità;
* intervallo temporale;
* dispositivo;
* dominio.

---

# Honesty of Presentation

L'applicazione non presenta mai come misurato un dato che non lo è.

Questo requisito ha la stessa rilevanza dei requisiti funzionali.

---

## Absent Versus Unmeasurable

L'interfaccia distingue sempre tre condizioni.

| Condizione            | Significato per l'utente                                    |
| --------------------- | ------------------------------------------------------------ |
| Nessun risultato      | È stato osservato, non è emerso nulla                         |
| Parzialmente osservato| È stato osservato in parte, il resto non è accessibile        |
| Non misurabile        | Non è stato osservato, la configurazione non lo consente      |

Un valore pari a zero, una sezione vuota o un grafico piatto appartengono alla prima condizione e comunicano all'utente che la sua rete è in ordine.

Utilizzarli per rappresentare le altre due condizioni costituisce un'informazione falsa.

Le sezioni non misurabili o parzialmente osservate vengono presentate in modo visivamente distinto, accompagnate dall'indicazione di cosa è stato valutato, cosa no e per quale motivo.

Una condizione parziale non viene mai presentata come completa: l'interfaccia rende evidente che il risultato mostrato si riferisce a una porzione del fenomeno.

---

## Score Coverage

Quando la copertura del Network Privacy & Security Score è inferiore a 100, l'interfaccia la mostra sempre accanto al punteggio.

Tutte le aree sono elencate, comprese quelle parzialmente misurate e quelle non misurabili, con l'indicazione degli indicatori valutati e del motivo dell'esclusione degli altri.

L'andamento storico segnala le variazioni di copertura, poiché punteggi con copertura differente non sono confrontabili.

---

## Guided Configuration

Le Recommendation prodotte in seguito a una capacità mancante sono presentate come **azioni proposte**, non come avvisi di errore.

Ogni proposta espone in modo simmetrico:

* quale analisi verrebbe abilitata;
* quale intervento è richiesto;
* quali conseguenze comporta, comprese quelle sfavorevoli.

L'ultimo punto è vincolante.

Quando l'attivazione di una funzionalità comporta un aumento del consumo di risorse, la registrazione di dati aggiuntivi o un impatto sulle prestazioni, tali aspetti vengono dichiarati **prima** che l'utente scelga.

L'applicazione non presenta configurazioni elencandone soltanto i benefici.

L'utente deve poter rifiutare una proposta e continuare a utilizzare il sistema senza limitazioni oltre a quelle dichiarate.

---

## Qualified Values

Un valore che dichiara una qualità diversa da esatta **non viene mai presentato come esatto**.

| Qualità dichiarata | Presentazione richiesta                                     |
| ------------------ | ------------------------------------------------------------ |
| `LowerBound`       | Indicare che il valore reale è almeno quello mostrato         |
| `PeriodBounded`    | Indicare il periodo, non un istante preciso                   |
| `Estimated`        | Indicare che si tratta di una deduzione                       |

Presentare un limite inferiore come un conteggio, o l'inizio di un periodo come l'istante di un evento, è un'affermazione falsa anche quando il numero mostrato è corretto.

L'interfaccia non arrotonda un valore incerto presentandolo come esatto.

---

## Device Identity

L'interfaccia dichiara su quale base è stata stabilita l'identità di un dispositivo.

Quando l'identità deriva dall'indirizzo di rete, l'utente viene informato che il dispositivo potrebbe cambiare identità al cambiare dell'indirizzo.

Attribuire un comportamento a un dispositivo è l'affermazione più forte che il sistema produce. La sua solidità va resa visibile, non lasciata intendere.

---

## Domain Classification

Ogni classificazione mostrata dichiara **da dove proviene e quanto è recente**.

| Elemento             | Presentazione richiesta                                       |
| -------------------- | ------------------------------------------------------------- |
| Lista di provenienza  | Nome della lista che ha prodotto la classificazione            |
| Età della lista       | Data dell'ultimo aggiornamento riuscito di quella lista        |
| Confidenza            | Distinzione fra corrispondenza diretta e inferenza sul dominio superiore |

Un dominio con categoria `Unknown` viene presentato come **non classificato**, mai come sicuro.

È la differenza fra dire che non si sa e dire che non c'è nulla. Solo la prima è vera.

---

## The Wording Belongs To The Interface

Il Core non produce frasi. Ogni fattore arriva come **codice con i propri valori**, e l'interfaccia lo rende in parole.

Ne discende che i vincoli di onestà di questo documento si applicano al **catalogo delle traduzioni**, non soltanto al codice sorgente.

Il catalogo è parte del prodotto. Una traduzione che scrivesse «rete pulita» al posto di «nessun tracciamento noto» violerebbe la specifica esattamente quanto lo farebbe il motore.

Le regole seguenti valgono quindi per ogni lingua offerta.

---

## Known, Not Absent

Gli indicatori fondati sulla classificazione misurano ciò che le liste **riconoscono**.

L'interfaccia non presenta mai il punteggio pieno di quelle aree come assenza di tracciamento o di minacce.

| Vietato                        | Richiesto                              |
| ------------------------------ | -------------------------------------- |
| «Nessun tracciamento»           | «Nessun tracciamento **noto**»          |
| «Rete pulita»                   | «Nessuna minaccia riconosciuta dalle liste» |
| «Sei protetto»                  | «Non è stato osservato nulla di noto»   |

La differenza non è prudenza formale. Un dominio assente da ogni lista può essere innocuo oppure un tracciatore che nessuna lista conosce, e il sistema non è in grado di distinguerli.

Dire «nessun tracciamento» sarebbe l'unica affermazione dell'intero prodotto che il prodotto non può sostenere.

---

## Device Visibility

L'elenco dei dispositivi comprende **soltanto i dispositivi che utilizzano questo servizio DNS**.

Un apparecchio configurato con un resolver proprio, o che utilizza DNS cifrato verso un servizio esterno, non compare in alcuna statistica. Non risulta privo di attività: risulta inesistente.

L'interfaccia dichiara questa condizione accanto all'elenco dei dispositivi.

La ragione è concreta. Chi osserva la propria rete pensa anzitutto a computer e telefoni, mentre televisori, console e apparecchi domestici vengono percepiti come oggetti d'uso anziché come dispositivi connessi. Sono anche quelli che più spesso portano un resolver cablato dal produttore.

L'assenza di un apparecchio dall'elenco è quindi un'informazione, e va presentata come tale invece di essere lasciata interpretare come una buona notizia.

---

## Withheld Score

Quando la copertura è inferiore alla soglia minima, il punteggio complessivo non esiste e l'interfaccia **non lo sostituisce con altro**.

Viene presentato il dettaglio delle aree, con quelle misurate, quelle parziali e quelle non misurabili con il relativo motivo.

La condizione va comunicata come una scelta del sistema, non come un guasto o un caricamento in corso: il sistema dispone di misure valide e sta dichiarando di non avere elementi sufficienti per un giudizio complessivo.

Nessun numero provvisorio, nessuna barra vuota, nessun segnaposto che suggerisca un valore in arrivo.

---

# User Experience

L'interfaccia privilegia:

* semplicità;
* chiarezza;
* leggibilità;
* accessibilità.

Le informazioni critiche devono essere immediatamente identificabili.

---

# Data Refresh

Le informazioni visualizzate vengono aggiornate attraverso le REST API.

La frequenza di aggiornamento è configurabile.

---

# Notifications

L'applicazione visualizza gli Alert prodotti dal Core.

La gestione degli Alert rimane di competenza del Privacy Intelligence Engine.

---

# Security

Network Privacy non memorizza credenziali delle Data Sources.

Le comunicazioni avvengono esclusivamente tramite le REST API del Core.

---

# Extensibility

L'interfaccia è progettata per ospitare nuove sezioni senza modificare la struttura principale.

---

# Design Principles

Network Privacy segue i seguenti principi.

* semplicità;
* modularità;
* leggibilità;
* uniformità;
* indipendenza dal backend.

---

# Constraints

Network Privacy:

* non contiene logica di business;
* non esegue analisi;
* non comunica direttamente con i backend;
* utilizza esclusivamente le REST API del Privacy Intelligence Engine.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 06 - API
* 07 - Network Privacy & Security Score
* 08 - Threat Intelligence
* 10 - Frontend
