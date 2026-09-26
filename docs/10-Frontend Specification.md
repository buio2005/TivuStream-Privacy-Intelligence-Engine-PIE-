# 10 - Frontend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Frontend Specification

**Version:** 1.3.1

**Status:** Approved

**Last Updated:** 2026-09-26

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

# Domain Detail

Si raggiunge dal nome di un dominio nell'elenco dei domini, all'indirizzo `/domains/{domain}`. Legge `/api/v1/domains/{domain}`, che copre lo stesso intervallo dell'elenco (API Specification).

## Contenuto

* Il nome del dominio e un ritorno all'elenco.
* L'intervallo, le ore osservate su quelle richieste, e l'ora in corso quando lo è: con le stesse frasi dell'elenco.
* Categoria, confidenza, lista e data della lista, prima e ultima osservazione, interrogazioni nell'intervallo: con le stesse frasi dell'elenco. Un dominio non classificato resta tale, senza colore che possa leggersi come approvazione.
* La reputazione. Oggi nessuna è valutata, perché il Threat Engine non esiste: la pagina lo dice come affermazione sul sistema, non sul dominio.
* L'attività per dispositivo, secondo `activityAccess`.

## Attività per dispositivo

| `activityAccess` | Cosa si mostra |
| --- | --- |
| `Available`, elenco non vuoto | Una riga per elemento: dispositivo, interrogazioni, esito, trasporto, prima e ultima osservazione |
| `Available`, elenco vuoto | `domainDetail.activityEmpty` |
| `Unavailable` | `domainDetail.activityUnavailable` |
| `Withheld` | `domains.activityWithheld`, già nel catalogo |

Nessuno dei tre casi senza righe si presenta come un elenco vuoto senza spiegazione, e nessuno come un errore.

In ogni riga:

* **Dispositivo.** Il nome, quando c'è, con l'indirizzo accanto; altrimenti l'indirizzo. Un dispositivo che la sorgente non ha descritto nell'intervallo (API Specification, Device Identification) si mostra con `domainDetail.deviceUndescribed`, dove `{id}` sono i primi otto caratteri dell'identificativo, così due dispositivi non descritti restano distinti, e con `domainDetail.deviceUndescribedNote`. Quando l'identità poggia sull'indirizzo di rete, la riga lo dichiara con `domainDetail.identityNetworkAddress`: è l'affermazione più forte del sistema, e quanto sia solida si vede invece di essere sottintesa.
* **Esito.** «Bloccato» o «Non bloccato». Non «Risolto»: non essere bloccato non dice che la risposta sia andata a buon fine.
* **Trasporto.** Un nome dal catalogo. Un valore sconosciuto al catalogo si mostra con il proprio identificativo; un valore vuoto è `domainDetail.transportUnknown`.

Le righe non si sommano in un totale per dispositivo: il totale del dominio è già dichiarato sopra, e una somma per dispositivo farebbe credere che l'identità regga fra un indirizzo e l'altro anche quando poggia sull'indirizzo.

## Stati della pagina

| Situazione | Cosa si mostra |
| --- | --- |
| In lettura | Il messaggio di caricamento esistente |
| `404 DomainNotObserved` | `domainDetail.notObserved`, e il ritorno all'elenco |
| Motore non raggiungibile o rifiuto inatteso | `domainDetail.unavailable` con `domains.unavailableReason`: una lettura fallita non dice nulla sulla rete |

## Regole

* I dati del dettaglio stanno in uno store proprio, svuotato alla fine della sessione come gli altri (Authentication Specification, F2).
* Cambiando dominio senza lasciare la pagina, il dettaglio precedente non resta visibile mentre si legge il nuovo.
* **L'indirizzo della pagina contiene il nome del dominio, e la cronologia del browser lo conserva dopo l'uscita.** È un limite dichiarato della regola «nessun dato di rete sopravvive all'uscita»: gli store si svuotano, la cronologia del browser non è dell'interfaccia. Chi entra dopo, con qualunque ruolo, vede comunque l'elenco dei domini; chi usa lo stesso browser senza entrare può leggerli nella cronologia. L'interfaccia non distingue i collegamenti già visitati, così la pagina non segnala quali domini ha aperto chi c'era prima. Se il computer è condiviso, la procedura d'installazione consiglierà un profilo del browser dedicato. Scelta del 2026-09-26: togliere il dominio dall'indirizzo avrebbe impedito il ricaricamento e i preferiti per proteggere da chi ha già il browser in mano.

## Messaggi

| Codice | Italiano | Inglese |
| --- | --- | --- |
| `domainDetail.back` | Torna ai domini osservati | Back to observed domains |
| `domainDetail.reputation` | Reputazione: {value} | Reputation: {value} |
| `domainDetail.reputationUnassessed` | Reputazione non ancora valutata. Il sistema non la calcola ancora: non è un giudizio sul dominio. | Reputation not assessed yet. The system does not compute it yet: this is not a judgement on the domain. |
| `domainDetail.activityTitle` | Attività per dispositivo | Activity by device |
| `domainDetail.activityUnavailable` | La sorgente dati non registra quale dispositivo abbia interrogato un dominio, quindi non si può dire quali lo abbiano fatto. | The data source does not record which device queried a domain, so it cannot be said which ones did. |
| `domainDetail.activityEmpty` | Nessuna attività per dispositivo registrata per questo dominio nell'intervallo. Il dominio è stato osservato: manca il dettaglio, non il traffico. | No activity by device recorded for this domain in the interval. The domain was observed: what is missing is the detail, not the traffic. |
| `domainDetail.device` | Dispositivo | Device |
| `domainDetail.queries` | Interrogazioni | Queries |
| `domainDetail.outcome` | Esito | Outcome |
| `domainDetail.blocked` | Bloccato | Blocked |
| `domainDetail.notBlocked` | Non bloccato | Not blocked |
| `domainDetail.transport` | Trasporto | Transport |
| `domainDetail.transportUnknown` | Non dichiarato | Not stated |
| `domainDetail.seen` | Osservazione | Observed |
| `domainDetail.deviceUndescribed` | Dispositivo {id} | Device {id} |
| `domainDetail.deviceUndescribedNote` | La sorgente non ha descritto questo dispositivo nell'intervallo: se ne conosce solo l'identificativo, non l'indirizzo né il nome. | The source did not describe this device in the interval: only its identifier is known, not its address or name. |
| `domainDetail.identityNetworkAddress` | Riconosciuto dall'indirizzo di rete: se cambia indirizzo compare come un altro dispositivo, e un indirizzo riassegnato unisce due dispositivi. | Recognised by network address: if it changes address it appears as another device, and a reassigned address merges two devices. |
| `domainDetail.notObserved` | Questo dominio non compare fra quelli osservati nelle ultime ventiquattro ore. | This domain is not among those observed in the last twenty-four hours. |
| `domainDetail.unavailable` | Impossibile leggere il dettaglio del dominio | Unable to read the domain detail |
| `transport.Udp` | UDP | UDP |
| `transport.Tcp` | TCP | TCP |
| `transport.Tls` | DNS su TLS | DNS over TLS |
| `transport.Https` | DNS su HTTPS | DNS over HTTPS |
| `transport.Quic` | DNS su QUIC | DNS over QUIC |

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
