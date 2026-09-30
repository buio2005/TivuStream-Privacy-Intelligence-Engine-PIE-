# 10 - Frontend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Frontend Specification

**Version:** 1.3.1

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the architecture and the principles of the Frontend
used by applications built on top of the Privacy Intelligence Engine.

The Frontend is solely the presentation layer of the system.

---

# Objectives

The Frontend has the following objectives.

* to display the data produced by the Core;
* to offer a simple and modern interface;
* to provide a coherent user experience;
* to stay completely separate from business logic.

---

# Scope

The Frontend:

* uses only the REST API of the Core;
* does not communicate directly with the Data Sources;
* implements no analysis algorithms;
* does not modify the Unified Data Model.

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

The Frontend is divided into independent modules.

* Dashboard
* Devices
* Domains
* Threats
* Alerts
* Recommendations
* Statistics
* Reports
* Settings

Each module is an independent view.

---

# Dashboard

The Dashboard is the entry point of the application.

It displays the principal information produced by the Core.

---

# Domain Detail

Reached from the name of a domain in the domain list, at the address
`/domains/{domain}`. It reads `/api/v1/domains/{domain}`, which covers the
same interval as the list (API Specification).

## Content

* The name of the domain and a way back to the list.
* The interval, the hours observed against those requested, and the hour in
  progress when there is one: in the same words as the list.
* Category, confidence, list and date of the list, first and last observation,
  queries within the interval: in the same words as the list. A domain that is
  not classified stays so, with no colour that could read as approval.
* The reputation. None is assessed today, because the Threat Engine does not
  exist: the page says so as a statement about the system, not about the
  domain.
* Activity by device, according to `activityAccess`.

## Activity by device

| `activityAccess` | What is shown |
| --- | --- |
| `Available`, list not empty | One row per item: device, queries, outcome, transport, first and last observation |
| `Available`, list empty | `domainDetail.activityEmpty` |
| `Unavailable` | `domainDetail.activityUnavailable` |
| `Withheld` | `domains.activityWithheld`, already in the catalogue |

None of the three cases without rows is presented as an empty list without
explanation, and none as an error.

In each row:

* **Device.** The name where there is one, with the address beside it;
  otherwise the address. A device the source did not describe within the
  interval (API Specification, Device Identification) is shown with
  `domainDetail.deviceUndescribed`, where `{id}` is the first eight characters
  of the identifier, so that two undescribed devices stay distinct, and with
  `domainDetail.deviceUndescribedNote`. Where the identity rests on the
  network address, the row declares it with
  `domainDetail.identityNetworkAddress`: it is the strongest claim the system
  makes, and how solid it is can be seen rather than assumed.
* **Outcome.** "Blocked" or "Not blocked". Not "Resolved": not being blocked
  does not say the answer succeeded.
* **Transport.** A name from the catalogue. A value the catalogue does not
  know is shown with its own identifier; an empty value is
  `domainDetail.transportUnknown`.

The rows are not added up into a total per device: the total for the domain is
already stated above, and a total per device would suggest that the identity
holds across a change of address even when it rests on the address.

## States of the page

| Situation | What is shown |
| --- | --- |
| Reading | The existing loading message |
| `404 DomainNotObserved` | `domainDetail.notObserved`, and the way back to the list |
| Engine unreachable or unexpected refusal | `domainDetail.unavailable` with `domains.unavailableReason`: a failed read says nothing about the network |

## Rules

* The detail data lives in a store of its own, emptied at the end of the
  session like the others (Authentication Specification, F2).
* Moving to another domain without leaving the page does not leave the
  previous detail visible while the new one is being read.
* **The address of the page contains the name of the domain, and the browser
  history keeps it after signing out.** This is a declared limit of the rule
  "no network data survives signing out": the stores are emptied, the browser
  history does not belong to the interface. Whoever signs in afterwards, with
  any role, sees the domain list anyway; whoever uses the same browser without
  signing in can read them in the history. The interface does not distinguish
  links already visited, so the page does not reveal which domains the
  previous person opened. On a shared computer the installation procedure will
  recommend a dedicated browser profile. Decision of 2026-09-26: removing the
  domain from the address would have broken reloading and bookmarks in order
  to protect against someone who already has the browser in their hands.

## Messages

The Italian column is the catalogue as shown, not a translation of the
English: both are the product.

| Code | Italian | English |
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

The layout must be responsive.

The interface must adapt to:

* Desktop;
* Tablet;
* Smartphone.

---

# Navigation

Navigation must be simple and coherent.

The principal sections of the application must always be reachable.

The entry for managing accounts is visible only to an `Administrator`.

---

# Components

The Frontend uses reusable components.

Examples.

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

Information is presented favouring:

* clarity;
* readability;
* brevity.

Technical information is available only where asked for.

---

# Data Refresh

The Frontend refreshes data using the REST API.

Refreshing may be:

* manual;
* automatic.

---

# State Management

The state of the application is held locally.

The Frontend does not modify the data produced by the Core.

---

# Error Handling

Errors are presented in an understandable way.

The interface avoids technical messages where they are not needed.

---

# Accessibility

The interface must respect the principal criteria of accessibility.

Particular attention is given to:

* contrast;
* readability;
* keyboard navigation;
* responsiveness.

---

# Internationalization

The architecture supports localisation of the interface.

Translations are managed separately from the code.

The languages offered are **Italian and English**.

Localisation is to be provided for from the first line of the Frontend. Adding
it to an interface already built means rewriting every text already written.

---

## Text Produced From The Backend

The Backend transmits no sentences.

The factors that explain the score arrive as codes with their own values,
listed in the NPSS Specification. The Frontend renders them in words.

It follows that the translation catalogue **contains statements about the
result of the analysis**, not merely interface labels.

The honesty constraints of the Network Privacy Specification apply to that
catalogue in full, in every language.

A code the catalogue does not know is shown as such, with its own identifier,
and not omitted: a factor that disappeared would take away a reason for the
score without declaring it.

---

# Themes

The system supports configurable visual themes.

Handling themes does not change the behaviour of the application.

---

# Performance

The Frontend favours:

* fast loading;
* efficient rendering;
* fewer HTTP requests;
* reuse of components.

---

# Authentication

Access is defined by the **Authentication Specification**, which also holds
the text of the messages in both languages.

The application is always in one of five states, distinct from one another:
`Checking`, `SetupRequired`, `Unauthenticated`, `Authenticated`,
`PasswordChangeRequired`.

The screens are: initial setup, sign in, password change, account management
(`Administrator` only).

Rules:

* **No network data survives signing out.** When the session ends, the stores
  empty every piece of data read.
* A session that ended while the person was signed in is not presented as a
  failed sign in.
* Wrong credentials, an unreachable engine and an insecure connection are
  three different situations and produce three different messages. Saying
  "wrong credentials" when the engine did not answer is a false statement.
* What the role does not include is shown as **withheld**: not as an absence
  and not as an error.

---

# Security

The Frontend:

* does not store credentials of the Data Sources;
* keeps no password and no session identifier in any persistent storage: the
  session cookie is not readable by the code of the page;
* exposes no sensitive information;
* communicates only over HTTPS.

---

# Extensibility

New components can be added without changing the existing ones.

The architecture favours modularity.

---

# Design Principles

The Frontend follows these principles.

* simplicity;
* uniformity;
* modularity;
* readability;
* accessibility;
* independence from the backend.

---

# Constraints

The Frontend:

* contains no business logic;
* contains no analysis algorithms;
* does not communicate directly with the Data Sources;
* uses only the REST API of the Privacy Intelligence Engine.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 06 - API
* 09 - Network Privacy
* 11 - Backend
