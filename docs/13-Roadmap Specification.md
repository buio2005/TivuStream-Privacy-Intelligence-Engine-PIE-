# 13 - Roadmap

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Project Roadmap Specification

**Version:** 1.5.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the official roadmap of the **TivuStream Privacy
Intelligence Engine (PIE)**.

The roadmap identifies the principal phases of development, the objectives of
each milestone, and the progression expected of the project.

---

# Objectives

The roadmap has the following objectives.

* to plan the development;
* to define the milestones;
* to maintain a coherent progression;
* to make the project easier to manage.

---

# Current Status

**Documentation Release:** 1.16.1

**Project Status:** In Development

**Development Status:** In Progress. The repository is not public. Of the Beta
criteria, those on authentication (Specification 18, milestones A1–A7), on
encrypted transport (Specification 19, milestones S1–S4, tested from a laptop
on a home network on 2026-09-26) and on retention (Specification 16 at 1.3.0,
consolidation active since 2026-09-26) are satisfied; the others are not, or
have not been verified: see Release Criteria.

---

# Development Phases

## Phase 1

### Foundation

Objectives.

* completing the documentation;
* defining the architecture;
* defining the APIs;
* defining the Unified Data Model.

**Status:** Completed

---

## Phase 2

### Backend Core

Objectives.

* implementing the Core;
* implementing the Unified Data Model;
* implementing the REST API;
* implementing the configuration system.

**Status:** In Progress

**Where it stands:** The Unified Data Model, the configuration system, the
persistence layer and six read endpoints exist. Authentication (Specification
18), HTTPS (Specification 19) and tiered consolidation (Specification 16) are
built.

---

## Phase 3

### Adapter Layer

Objectives.

* Adapter Manager;
* Technitium Adapter;
* communication with the HTTP APIs;
* normalisation of the data.

**Status:** In Progress

**Where it stands:** The Technitium Adapter (base level, Domain Activity,
source configuration) and communication with the HTTP APIs exist. Missing are
the implementation of the Adapter Manager, of which only the interface exists,
and a test against a second version of Technitium.

---

## Phase 4

### Core Modules

Objectives.

* Threat Engine;
* Device Engine;
* Alert Engine;
* Recommendation Engine;
* NPSS Engine.

**Status:** In Progress

**Where it stands:** The classification engine and the NPSS engine exist.
Missing are the Device, Alert and Recommendation Engines: because of this the
Device Health area is not measurable and fifteen points of the score stay
excluded.

---

## Phase 5

### Frontend

Objectives.

* Dashboard;
* Devices;
* Domains;
* Threats;
* Alerts;
* Recommendations;
* Statistics.

**Status:** In Progress

**Where it stands:** Dashboard and Domains exist. Missing are Devices,
Threats, Alerts, Recommendations and Statistics.

---

## Phase 6

### Reporting

Objectives.

* PDF reports;
* CSV reports;
* JSON export;
* history.

**Status:** Planned

---

## Phase 7

### Testing

Objectives.

* unit tests;
* integration tests;
* performance tests;
* security tests.

**Status:** In Progress

**Where it stands:** Tests exist at the boundaries of the Adapter, the
persistence layer, the API and the Frontend, as well as those of the Core.
Missing are integration, performance and security tests.

---

## Phase 8

### Beta Release

Objectives.

* Beta release;
* gathering feedback;
* fixing defects;
* optimisation.

**Status:** Planned

---

## Phase 9

### Stable Release

Objectives.

* Version 1.0;
* documentation brought up to date;
* public release.

**Status:** Planned

---

# Milestones

| Milestone | Description           | Status      |
| --------- | --------------------- | ----------- |
| M1        | Documentation Release | Completed   |
| M2        | Backend Core          | In Progress |
| M3        | Technitium Adapter    | In Progress |
| M4        | Core Modules          | In Progress |
| M5        | Frontend              | In Progress |
| M6        | Reports               | Planned     |
| M7        | Testing               | In Progress |
| M8        | Beta Release          | Planned     |
| M9        | Stable Release        | Planned     |

## Correspondence with the CHANGELOG

Changelog entries numbered `M5.x` (storage, schema and persistence) and `M6.x`
(Frontend) predate the alignment with this Roadmap and do not match its
numbers.

| Changelog entry | Roadmap milestone |
| --- | --- |
| `M2.x`, `M3.x`, `M4.x` | M2, M3, M4 |
| `M5.x` (persistence) | M2, Backend Core |
| `M6.x` (Frontend) | M5, Frontend |

The history is not rewritten. Future entries use the numbers of this Roadmap.

---

# Version Strategy

The project uses semantic versioning.

Format.

```text id="1ikjlwm"
MAJOR.MINOR.PATCH
```

Example.

```text id="c7i4bn4"
1.0.0
```

---

# Documentation Roadmap

The documentation carries a version independent of the code.

Documentation Release 1.0.0 is the original baseline of the project.

The current Documentation Release is stated in the Current Status section.

Every Specification also carries its own version, updated only when the
document is modified.

The full history is recorded in `CHANGELOG.md`.

---

# Development Principles

Every new feature must:

* respect the architecture defined;
* use the Unified Data Model;
* keep compatibility with the APIs;
* be documented.

---

# Release Criteria

The criteria are **verifiable**: each can be declared satisfied or not
satisfied without interpretation.

A criterion expressed as "the APIs are stable" is not verifiable and does not
distinguish a release that is ready from one that looks ready.

---

## Repository Publication

Making the code public is **not** the same as releasing the product.

| Criterion | Verification |
| --- | --- |
| Documentation in English | No document in `/docs` remains in Italian |
| Licence declared | `LICENSE.md` present, note at the head of the source files |
| Debts declared | The README lists what is missing, in the words of whoever will read it |
| No secret versioned | No token and no credential in the git history |
| Current Status true | The Current Status section of this document describes the state at publication, not the one before it |

The last row exists because this document states that the repository is not
public. That is true while it is written and false the moment it is
published. A status field describes the present and has to be moved with it;
left behind, the first thing a stranger would read is a document declaring
itself private.

The README states explicitly that the project is **not ready for use**, and
for what reasons.

A project founded on declaring what it does not know can also declare what it
is not yet.

---

## Beta Release

The product can be installed and used by someone who did not write it.

| Criterion | Verification |
| --- | --- |
| Authentication | No endpoint returning data about the network or the system answers without valid credentials. `setup` and `login` are the exception: they establish the credentials and return nothing about the network (Specification 18) |
| Encrypted transport | The interface is reachable over HTTPS |
| Installation | Someone outside the project installs it following the documented procedure, on a clean machine |
| Retention | The tiered consolidation defined in Specification 16 is active |
| Tests at the boundaries | The Adapter, the API and the persistence layer each have at least one test |
| No build warning | Backend and Frontend compile clean |
| Second environment | The system has been run on Linux as well as on Windows |

---

## Stable Release

| Criterion | Verification |
| --- | --- |
| Score coverage | No area of the NPSS is left without computable definitions |
| Interface sections | Every navigation entry defined in Specification 09 exists |
| Prolonged run | The system has run without interruption for at least thirty days |
| Second version of the source | The Technitium Adapter has been tested against two different versions |
| Licence notice | The interface shows copyright, absence of warranty, and how to consult the licence |
| No undeclared debt | Every known gap appears in the README or in the CHANGELOG |

---

# Future Evolution

Future evolutions of the project are planned through new versions of this
Roadmap Specification.

---

# Related Specifications

* 01 - Vision
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 10 - Frontend
* 11 - Backend
* 12 - Installation
