# 06 - API

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** API Specification

**Version:** 1.6.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the public APIs of the Privacy Intelligence Engine.

The APIs are the only official interface between the Core and the applications
that use the engine.

---

# Objectives

The APIs are designed to be:

* simple;
* coherent;
* independent of the backend;
* versionable;
* easy to extend.

---

# Architecture

The REST API belongs to the Query Flow.

An API request returns only results already processed by the Core.

```text id="jlwmxt"
Frontend Application

↓

REST API

↓

Results produced by the Privacy Intelligence Engine
```

No endpoint triggers communication towards the Adapters or the Data Sources.

Acquiring the data belongs to the Acquisition Flow and is described in the
Architecture Specification.

---

# API Versioning

The APIs carry the version in the path.

Example.

```text id="u4e5vi"
/api/v1/
```

Every incompatible change produces a new Major Version.

---

# Protocol

The APIs use:

* HTTPS
* JSON UTF-8
* REST

---

# Standard Response

Every response uses a common structure.

```json
{
  "success": true,
  "apiVersion": "v1",
  "timestamp": "...",
  "data": {}
}
```

---

# Standard Error

```json
{
  "success": false,
  "error": {
    "code": "...",
    "message": "..."
  }
}
```

An error may carry a `reason` field stating the cause in a form a program can
read, for instance `TooShort` for a rejected password. Where there is none the
field is absent, not `null`.

---

# Observed Period

Responses describing what was observed declare **which period they refer to**.

```json
{
  "period": { "start": "...", "end": "..." },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "domains": []
}
```

The `period` field is absent when no observation has been recorded yet.

The two counts are not redundant. An installation running for six hours that
declared "the last 24 hours" would be stating something false:
`periodsRequested` is the interval asked for, `periodsObserved` is the one
that exists.

The reason is not formal. An empty list without its period is ambiguous:
whoever reads it cannot tell "the network contacted nothing" from "the current
hour has only just begun". The first is a statement about the network, the
second about the moment of looking, and presenting them alike is false
information.

The requirement follows from the Absent Versus Unmeasurable rule of the
Network Privacy Specification.

**Endpoints concerned.** `/domains`, `/domains/{domain}`, `/devices`,
`/statistics` and `/npss` cover the same window, the last twenty-four hours,
and declare it with the same three fields. One window for everything that
describes the network: two pages showing different intervals without saying so
would contradict each other.

---

# Dashboard Endpoint

## GET

```
/api/v1/dashboard
```

Returns the complete summary of the state of the network.

It includes:

* NPSS
* statistics
* devices
* alerts
* threats
* recommendations

---

# NPSS Endpoint

## GET

```
/api/v1/npss
```

Returns the Network Privacy & Security Score.

It comprises:

* the score;
* the breakdown;
* the history;
* the trend;
* the window judged, with `period`, `periodsObserved`, `periodsRequested`,
  like every response describing what was observed (see Observed Period). It
  is the twenty-four hour window ending with the period in which the score was
  produced (NPSS Specification, Evaluation Window).

---

# Devices Endpoint

## GET

```
/api/v1/devices
```

Returns the devices observed in the **last twenty-four hours**, together with
the interval actually covered (see Observed Period).

```json
{
  "period": { "start": "...", "end": "..." },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "devices": [
    {
      "deviceId": "...",
      "hostname": "laptop-maria",
      "ipAddress": "192.168.1.20",
      "macAddress": "aa:bb:cc:dd:ee:ff",
      "vendor": null,
      "operatingSystem": null,
      "identityBasis": "HardwareAddress",
      "status": "Active",
      "firstSeen": "...",
      "lastSeen": "...",
      "observationQuality": "PeriodBounded"
    }
  ]
}
```

### Aggregation

One element per device identifier within the window.

| Property | Rule |
| --- | --- |
| `hostname`, `ipAddress`, `macAddress`, `vendor`, `operatingSystem`, `identityBasis` | From the **most recent** period the device appears in, like the classification of domains |
| `firstSeen` | The earliest among the periods included |
| `lastSeen` | The latest among the periods included |
| `observationQuality` | The least precise among those aggregated |
| `status` | `Active`: a device appears in the window because it produced traffic there |

Ordered by address.

The aggregation is lawful because the identifier is derived deterministically
from the address or from the hardware address. A device recognised by its
network address that changed address within the window appears twice, and
`identityBasis` declares it.

### What The List Does Not Carry

The Device entity of the Data Model comprises `domainActivities` and
`threats`. The list **does not carry them**: today they would travel as empty
lists, and an empty list reads as "no activity, no threats" when it means "not
included in this response". The activity of a device belongs to its detail,
`/devices/{id}`, not yet implemented.

---

## GET

```
/api/v1/devices/{id}
```

Returns the detail of a single device.

---

# Threats Endpoint

## GET

```
/api/v1/threats
```

Returns every threat detected.

It supports filters.

* category;
* severity;
* time range.

---

## GET

```
/api/v1/threats/{id}
```

Returns the detail of a specific threat.

---

# Alerts Endpoint

## GET

```
/api/v1/alerts
```

Returns the Alerts generated by the Core.

It supports filters by:

* severity;
* state;
* category.

---

# Recommendations Endpoint

## GET

```
/api/v1/recommendations
```

Returns every Recommendation the system has produced.

---

# Domains Endpoint

## GET

```
/api/v1/domains
```

Returns the domains observed in the **last twenty-four hours**, together with
the interval actually covered.

```json
{
  "period": { "start": "2026-08-30T22:00:00+00:00", "end": "2026-08-31T22:00:00+00:00" },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "domains": []
}
```

An empty list accompanied by the interval means that no domain was observed in
that span. Without the interval the same response would say nothing
verifiable.

---

### Aggregation

Observation periods are fixed and **do not overlap**, so the occurrences of
the same domain in different periods add up without counting the same traffic
twice.

It is the choice made in the Persistence Specification that makes this sum
lawful. With rolling acquisition windows the aggregation would have been
impossible.

| Property               | Rule                                                        |
| ---------------------- | ------------------------------------------------------------ |
| `occurrences`          | Sum of the periods included                                   |
| `firstSeen`            | The earliest among the periods included                       |
| `lastSeen`             | The latest among the periods included                         |
| `observationQuality`   | The least precise among those aggregated                      |
| Classification         | That of the **most recent** period the domain appears in      |

The last row is a choice and has to be motivated. Every period keeps the
classification it was possible to give then; presenting the most recent one
shows what is known **now**, and the age of the list declared alongside the
category says how recent that "now" is.

Showing the oldest classification, or a synthesis of the different
classifications received, would produce a statement no period ever made.

---

### The Last Hour Is In Progress

The interval returned includes the current period, which has not elapsed.

Its end is therefore an instant in the future, and the interface declares that
the last hour is still in progress rather than presenting it as observed in
full.

---

## GET

```
/api/v1/domains/{domain}
```

Returns the detail of the domain **over the same interval as the list**: the
last twenty-four hours, together with the interval actually covered.

```json
{
  "period": { "start": "2026-08-30T22:00:00+00:00", "end": "2026-08-31T22:00:00+00:00" },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "domain": { "name": "example.com", "category": "Tracking", "occurrences": 42, "...": "..." },
  "activityAccess": "Available",
  "activities": [
    {
      "device": {
        "deviceId": "...",
        "hostname": "laptop-maria",
        "ipAddress": "192.168.1.20",
        "identityBasis": "HardwareAddress"
      },
      "queryCount": 30,
      "blocked": false,
      "protocol": "Udp",
      "firstSeen": "2026-08-31T08:00:00+00:00",
      "lastSeen": "2026-08-31T19:00:00+00:00",
      "observationQuality": "PeriodBounded"
    }
  ]
}
```

It comprises:

* the category, with confidence, list and date of the list;
* the reputation;
* the frequency;
* the activity per device, where the source offers it and the role of the
  requester allows it.

### Same Interval As The List

The detail covers the same interval as `/domains` and declares it with the
same three fields (see Observed Period).

The constraint arose from a defect. Reading only the most recent period, a
domain the list shows because it was contacted ten hours earlier would answer
`DomainNotObserved`: the list asserts the domain was observed, the detail
denies it. Two answers from the same engine do not contradict each other.

`domain` is aggregated by the rules of the Aggregation section of `/domains`,
and therefore matches the row in the list.

A domain absent from the interval answers `404 DomainNotObserved`.

### Activity Aggregation

One element of `activities` for every combination of **device, outcome
(blocked or not) and transport** within the interval. A device that reached
the domain both directly and through a block appears twice, because they are
two different facts.

| Property | Rule |
| --- | --- |
| `queryCount` | Sum of the periods included |
| `firstSeen` | The earliest among the periods included |
| `lastSeen` | The latest among the periods included |
| `observationQuality` | The least precise among those aggregated |

Ordered by `queryCount` descending.

The sum is lawful for the same reason as that of the domains, periods that do
not overlap, and because the identifier of the device is derived
deterministically: the same address, or the same hardware address, produces
the same identifier in every period. How solid that identity is, is declared
by `identityBasis`.

### Device Identification

The identifier of a device tells the reader nothing on its own. Every element
carries `hostname` (where the source provides it), `ipAddress` and
`identityBasis`, taken from the **most recent** period within the interval in
which the device appears: the same rule as the classification.

The detail carries the device information itself rather than deferring to
`/devices`, so that a single response is complete.

A device may appear in the activity without appearing among the devices of any
period in the interval: the source reports them in two different accounts, and
the one for devices is limited. In that case `hostname`, `ipAddress` and
`identityBasis` are all three `null`, and the activity remains: the traffic
happened, who produced it is not known beyond the identifier, and nothing is
assumed.

These fields exist only with `Available`. For a `Viewer` no information about
devices appears in the response.

### Activity Access

The `activityAccess` field declares what the `activities` list means.

| Value | Meaning |
| --- | --- |
| `Available` | The source offers the activity and the requester may read it. `activities` is the list |
| `Unavailable` | The source does not offer the activity. `activities` is empty and means nothing |
| `Withheld` | The source offers it, but the role of the requester does not include it. `activities` is empty and means nothing |

An empty list for want of a right and an empty list because no device
contacted the domain are different statements. The field exists so that they
are not presented alike.

Whether the source offers the activity is established by the most recent
acquisition.

`Available` with an empty list means the source recorded no activity per
device towards this domain within the interval, for instance because the
periods it appears in were acquired when the source did not offer it. The
domain was observed: what is missing is the detail, not the traffic.

---

# Statistics Endpoint

## GET

```
/api/v1/statistics
```

Returns the aggregated statistics of the network over the **last twenty-four
hours**, together with the interval actually covered (see Observed Period).

```json
{
  "period": { "start": "...", "end": "..." },
  "periodsObserved": 6,
  "periodsRequested": 24,
  "statistics": {
    "totalQueries": 1000,
    "blockedQueries": 100,
    "cachedQueries": 400,
    "failedQueries": 9,
    "uniqueDomains": 250,
    "uniqueDomainsQuality": "LowerBound",
    "activeDevices": 3,
    "encryptedQueries": 16,
    "dnssecEnabled": true
  }
}
```

When no period exists within the window, `period` and `statistics` are
`null`. A row of zeros would say the network queried nothing, when nothing was
observed. Nor is "nothing acquired yet" an answer: it is false when
acquisitions older than the window exist.

### Aggregation

| Property | Rule |
| --- | --- |
| `totalQueries`, `blockedQueries`, `cachedQueries`, `failedQueries`, `encryptedQueries` | Sum of the periods included, lawful because they do not overlap |
| `uniqueDomains` | The greater of the highest hourly value and the distinct names recorded within the window |
| `uniqueDomainsQuality` | `LowerBound` when the window covers more than one period; otherwise that of the period |
| `activeDevices` | The greater of the highest hourly value and the distinct identifiers recorded within the window |
| `dnssecEnabled` | From the most recent period: it is configuration, not traffic |

Distinct domains from different hours **do not add up**: the same domain
contacted in two hours would be counted twice. The highest hourly value and
the distinct names kept are both lower bounds (the source returns truncated
lists), and the greater of the two is still a lower bound.

The same reasoning holds for devices, with the reservation about identity: a
device recognised by network address that changes address counts twice,
exactly as in `/devices`.

The score does not change: the NPSS is still computed as described in the NPSS
Specification.

---

# Timeline Endpoint

## GET

```
/api/v1/timeline
```

Returns events ordered chronologically.

---

# Reports Endpoint

## POST

```
/api/v1/reports
```

Generates a new report.

Formats supported.

* PDF
* CSV
* JSON

---

# Sources Endpoint

## GET

```
/api/v1/sources
```

Returns the list of registered Data Sources.

---

# Health Endpoint

## GET

```
/api/v1/health
```

Returns the operational state of the system.

It comprises:

* Core
* Adapter
* Backend
* API

---

# Settings Endpoint

## GET

```
/api/v1/settings
```

Returns the current configuration.

---

## PUT

```
/api/v1/settings
```

Updates the configuration of the system.

---

# Authentication

Authentication is defined by the **Authentication Specification**: local
accounts with a role, a session by cookie, refusal by default.

Every endpoint requires a valid session. **Only `POST /api/v1/setup` and
`POST /api/v1/auth/login` are the exception**, since they serve to obtain one
and return no data about the network or the system.

The endpoints that handle it (`/setup`, `/auth/*`, `/accounts`) and the error
codes it introduces (`SetupRequired`, `AuthenticationRequired`,
`AuthenticationFailed`, `Forbidden`, `PasswordChangeRequired`,
`TransportNotSecure`, `OriginNotAllowed`, `TooManyAttempts` and others) are
described in that document, which is their source.

---

# Authorization

Every endpoint **declares the minimum role** it requires. An endpoint that does
not declare one requires `Administrator`: forgetting a declaration produces a
refusal, not an opening.

What a role may not read is not omitted in silence: the response declares it.
See Activity Access.

---

# Error Handling

Errors are classified into the following categories.

* Validation
* Authentication
* Authorization
* Backend
* Network
* Internal

Every error uses a unique identifying code.

Two codes hold for every endpoint:

| Status | Code | Category | When |
| --- | --- | --- | --- |
| 400 | `RequestUnreadable` | Validation | The body of the request cannot be read: it is not JSON, does not have the expected shape, or contains invalid text |
| 500 | `InternalError` | Internal | A failure the code did not anticipate |

Both use the common structure **in every runtime environment**.
`InternalError` says nothing about the cause or about the request: the
diagnostic page of the framework, which lists the headers and with them the
session cookie, is never shown (Authentication Specification, V11). The cause
goes into the application log, without secrets.

---

# Logging

API requests may be recorded for diagnostic purposes.

Logs must contain no sensitive data.

---

# Rate Limiting

The APIs support configurable limits on the number of requests.

---

# Compatibility

The APIs keep backward compatibility within the same Major Version.

---

# Design Principles

The APIs follow these principles.

* one responsibility per endpoint;
* predictable responses;
* independence from the backend;
* simplicity;
* explicit versioning;
* extensibility.

---

# Constraints

The APIs:

* do not expose the backends directly;
* do not return data that has not been normalised;
* contain no business logic;
* do not trigger the Acquisition Flow;
* are the only official point of access to the Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 09 - Network Privacy
