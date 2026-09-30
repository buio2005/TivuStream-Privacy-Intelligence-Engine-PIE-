# 00 - Glossary

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Glossary Specification

**Version:** 1.6.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This document defines the official glossary of the project.

Every term recorded here is the official nomenclature to be used in the
documentation, in the source code, in the APIs and in the user interface.

The objective is terminological consistency throughout the development cycle.

---

# Naming Convention

The project uses only the terms defined in this document.

Different terms describing the same concept are to be avoided.

---

# Official Project Name

**TivuStream Privacy Intelligence Engine**

Official acronym:

**PIE**

This is the name of the project.

It is not to be abbreviated in any other form.

---

# Official Product Name

**Network Privacy**

Network Privacy is the first product built on top of the Privacy Intelligence
Engine.

It is not the name of the project.

---

# Official Score Name

**Network Privacy & Security Score**

Official acronym:

**NPSS**

The NPSS is the principal indicator produced by the Privacy Intelligence
Engine.

It replaces the earlier term "Privacy Score".

---

# Core

The term **Core** identifies the main engine of the Privacy Intelligence
Engine.

It comprises only the modules responsible for processing data.

---

# Data Source

Any external system that produces data.

Examples:

* Technitium DNS Server
* Privacy Assistant
* Pi-hole
* AdGuard Home

---

# Adapter

The component that converts data coming from a Data Source into the internal
format of the project.

Each Data Source has its own Adapter.

---

# Unified Data Model

The internal data model used by the Core.

All modules communicate exclusively through this model.

---

# Capability

A declaration of what a Data Source **is able to provide**.

The vocabulary of capabilities coincides with the names of the entities of the
Unified Data Model.

A capability does not describe the features of the external product: the
features a backend offers are data to be analysed, not structural abilities.

The absence of a capability means the data is **not measurable**, not that it
is zero.

---

# Storage

The Backend component responsible for keeping acquisitions and the results
produced by the Core over time.

It performs no analysis and is not known to the Core.

---

# Observation Period

The fixed interval acquisitions are aligned to.

An acquisition is not a set of events but the observation of an interval: two
observations of overlapping intervals cannot be added together.

A further observation of the same period replaces the previous one. An elapsed
period is immutable.

---

# Measurement Quality

A declaration of **how** a value is known.

A value may be exact, a lower bound, bounded to a period, or inferred.

A value known with less precision is qualified — not discarded, and not
rounded to the plausible.

---

# Identity Basis

The ground on which the identity of a device was established.

An identity based on the hardware address is stable; one based on the network
address changes when the address changes.

---

# Coverage

The portion of the evaluation system actually observed while computing the
Network Privacy & Security Score.

It is expressed as the sum of the obtainable points of every area, out of a
maximum of 100.

An area may be measured in full, measured in part, or not measurable at all.
The unobserved portion is excluded from the calculation and cannot improve the
score.

Scores with different coverage are not comparable.

---

# Classification List

A list of domains associated with a category, held locally.

Every list declares where it comes from, its licence, and the moment of its
last successful update.

---

# Classification Freshness

The age of the list a classification comes from.

A local classification grows old. The system declares its age rather than
presenting it as current.

---

# Network Snapshot

A complete representation of the state of the network at a given moment.

It is the basis for:

* reports;
* comparisons over time;
* historical analysis.

---

# Device

Any device identified by the system.

Examples:

* Desktop
* Notebook
* Smartphone
* Smart TV
* NAS
* Router
* IoT

---

# Domain

A domain observed by the system during the analysis.

A domain is independent of the device that contacted it.

---

# Domain Activity

The relation between a Device and a Domain.

It describes the behaviour observed.

---

# Threat

An event or a behaviour classified as potentially dangerous.

The categories are defined in the Threat Intelligence Specification.

---

# Alert

A notification generated automatically by the Privacy Intelligence Engine.

Alerts may carry different levels of severity.

---

# Recommendation

A suggestion produced by the system following an analysis.

Every Recommendation must be justified by one or more events.

---

# Event

An internal message used by the modules of the Core to communicate with one
another.

Events are not exposed directly to the frontend.

---

# Backend

External software that provides data to the Privacy Intelligence Engine.

Technitium is the first backend supported.

---

# Frontend

The user interface of the project.

The Frontend contains no analysis logic.

---

# REST API

The public interface of the Privacy Intelligence Engine.

It is the only official point of access to the data processed by the Core.

---

# Dashboard

The main interface of the application.

It displays only information produced by the Core.

---

# Core Modules

The modules that make up the Core all carry the suffix **Engine**.

This form is the official nomenclature of the project.

---

# Threat Engine

The module responsible for classifying threats.

Suggested acronym:

**TE**

---

# Device Engine

The module responsible for analysing devices.

Suggested acronym:

**DE**

---

# NPSS Engine

The module responsible for computing the Network Privacy & Security Score.

Suggested acronym:

**NE**

---

# Alert Engine

The module responsible for generating Alerts.

Suggested acronym:

**AE**

---

# Recommendation Engine

The module responsible for producing recommendations.

Suggested acronym:

**RE**

---

# Threat Intelligence

The subject matter the Threat Engine deals with.

The term identifies the discipline, not the software component.

As the name of a module, **Threat Engine** is to be used.

---

# Acquisition Flow

The path data follows during periodic acquisition.

```text
Scheduler

↓

Adapter Manager

↓

Adapter

↓

Data Source

↓

Unified Data Model

↓

Core
```

Acquisition is orchestrated by the Adapter Manager.

The Core takes no part in the orchestration.

---

# Query Flow

The path a request coming from the Frontend follows.

```text
Frontend

↓

REST API

↓

Results already produced by the Core
```

The Query Flow triggers no communication towards the Data Sources.

---

# Account

A person who uses the Privacy Intelligence Engine.

It belongs to the Backend and not to the Unified Data Model, which describes
the network being observed and not whoever observes it. It has a user name, a
role, and a password kept only as a hash.

---

# Role

What an Account may read and do.

The roles are **Administrator** and **Viewer**. A Viewer reads aggregated data
and not the data that identifies an individual device.

---

# Session

The period during which an Account, having signed in, is recognised without
presenting the password again.

It is identified by a random value of which the Backend keeps only the hash.

---

# Setup Code

A random code printed on standard output when the installation holds no
Account.

It proves that whoever creates the first Administrator has access to the
machine. It is not kept.

---

# Withheld

Data the source offers but which the Role of the requester does not include.

It differs from **Unavailable**, which the source does not offer, and from an
empty list because nothing happened. The three situations are never presented
alike.

---

# Terminology Rules

Within the project:

* always use **PIE** for the Privacy Intelligence Engine;
* always use **NPSS** for the Network Privacy & Security Score;
* always use **Data Source** for the origin of the data;
* always use **Adapter** for the integration layer;
* always use **Core** for the main engine;
* always use the suffix **Engine** for the modules of the Core;
* always use **Frontend** and **Backend** untranslated.

---

# Terms to Avoid

For the sake of uniformity, avoid the following terms where an official one
already exists.

| Avoid                                   | Use instead                             |
| --------------------------------------- | --------------------------------------- |
| Privacy Score                           | Network Privacy & Security Score (NPSS) |
| Connector                               | Adapter                                 |
| Source Provider                         | Data Source                             |
| DNS Engine                              | Backend                                 |
| Main Engine                             | Core                                    |
| Plugin (for data integrations)          | Adapter                                 |
| Threat Intelligence (as a module name)  | Threat Engine                           |
| Device Intelligence (as a module name)  | Device Engine                           |

---

# Document References

This document is the terminological reference for every Specification in the
`docs` folder.

Every new document is to use only the terminology defined in this Glossary.
