# 11 - Backend

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Backend Specification

**Version:** 1.4.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the architecture of the Backend of the Privacy
Intelligence Engine.

The Backend is the application layer that hosts the Core, the Adapters and the
REST API, coordinating the whole processing flow of the system.

---

# Objectives

The Backend has the following objectives.

* to acquire data from the Data Sources;
* to coordinate the Core;
* to expose the REST API;
* to guarantee modularity;
* to guarantee scalability;
* to keep independence from the backends supported.

---

# Scope

The Backend comprises:

* the REST API;
* the Adapters;
* the Core Engine;
* internal services;
* configuration;
* authentication;
* logging.

The Frontend is not part of the Backend.

---

# High Level Architecture

The Backend hosts two independent paths.

The Query Flow serves requests from the Frontend.

The Acquisition Flow acquires data from the Data Sources.

```text id="1ntz3h"
        Query Flow                    Acquisition Flow

         Frontend                        Scheduler

             │                               │

             ▼                               ▼

       REST API Layer                  Adapter Manager

             │                               │

             ▼                               ▼

      Authentication                      Adapter

             │                               │

             ▼                               ▼

        Authorization                   Data Sources

             │                               │

             ▼                               ▼

   Processed results              Unified Data Model

                                             │

                                             ▼

                                        Core Engine

                                             │

                                             ▼

                                     Processed results
```

The cross-cutting services Configuration Manager, Logging Service and Report
Service are used by both paths.

---

# Main Components

The Backend is composed of the following components.

* REST API
* Core Engine
* Adapter Manager
* Storage
* Authentication
* Configuration Manager
* Logging Service
* Scheduler
* Report Service

---

# REST API Layer

Exposes every public capability of the system.

Every request is validated before it reaches the Core.

---

# Core Engine

Coordinates every module of the Privacy Intelligence Engine.

It handles:

* analysis;
* classification;
* correlation;
* the NPSS;
* Alerts;
* Recommendations.

---

# Adapter Manager

Manages the life cycle of the Adapters.

Every Adapter communicates with one specific Data Source.

The Adapter Manager also orchestrates the Acquisition Flow.

Its responsibilities comprise:

* registering the Adapters;
* running the acquisition cycle;
* collecting the normalised data;
* delivering the Unified Data Model to the Core Engine.

The Adapter Manager performs no analysis.

---

# Adapter Contract

The contract every Adapter must respect consists of a base interface and a set
of interfaces segregated by capability.

---

## Base Interface

The base interface carries the identity of the Adapter and the description of
the Data Source.

The description comprises the version, the operational state and the
capabilities actually available.

---

## Capability Interfaces

Every capability corresponds to a dedicated interface.

| Interface               | Capability       |
| ----------------------- | ---------------- |
| Statistics Source       | `Statistics`     |
| Device Source           | `Device`         |
| Domain Source           | `Domain`         |
| Domain Activity Source  | `DomainActivity` |

An Adapter implements only the interfaces corresponding to the data it is able
to provide.

This segregation follows the Interface Segregation principle and removes the
need to implement methods that are not supported.

---

## Potential and Effective Capabilities

The interfaces implemented express what an Adapter **can** provide.

The capabilities declared in the description express what the Data Source
**actually** provides in its current configuration.

The two do not necessarily coincide: a Data Source may offer a kind of data
only after an optional component has been enabled.

The following rule holds.

> An Adapter cannot declare a capability whose interface it does not
> implement.

The Adapter Manager checks this condition at registration.

---

## Acquisition Window

Every acquisition receives an explicit interval of time.

The interval is never assumed by the Adapter, since different windows do not
necessarily contain the same information.

The interval also allows incremental acquisition, asking only for what follows
the previous cycle.

---

## Error Handling

Adapters convert the errors of their own Data Source into a dedicated
exception.

The rest of the system never handles errors expressed in the vocabulary of a
specific backend.

Diagnostic detail produced by the Data Source does not pass beyond the
Adapter.

---

# Storage

Keeps acquisitions and the results produced by the Core over time.

Keeping them is necessary because the Data Sources apply a retention of their
own: what is not kept at the moment of acquisition is lost for good.

Responsibilities.

* keeping acquisitions converted into the Unified Data Model;
* keeping the results of the Core;
* applying tiered retention;
* managing the schema version.

Constraints.

* The Storage performs no analysis and does not modify the data it receives.
* The Core does not know the Storage.
* The Unified Data Model contains no element of persistence.
* The Frontend never reaches the Storage directly.

How it works is described in the Persistence Specification.

---

# Configuration Manager

Manages the configuration of the system.

It comprises.

* the backend;
* the Adapters;
* authentication;
* updates;
* general settings.

---

# Authentication

Manages the authentication of users: local accounts with a role, sessions,
initial setup and recovery. The definition is in the **Authentication
Specification**.

Authentication is independent of the Data Sources.

Authorised services, mentioned in an earlier version of this document, are not
provided for until a service exists that uses them.

---

# Authorization

Every request is checked before processing.

Every endpoint declares the minimum role it requires. An endpoint that does
not declare one requires `Administrator`. See the Authentication
Specification.

---

# Logging Service

Centralises the recording of events.

It comprises.

* system events;
* errors;
* administrative activity;
* API events.

---

# Scheduler

Manages scheduled operations.

Examples.

* updating blocklists;
* synchronising data;
* clearing caches;
* generating reports.

---

# Report Service

Generates reports using the data produced by the Core.

Formats provided for.

* PDF
* CSV
* JSON

---

# Solution Structure

The Backend is organised into distinct projects.

Separating them into projects makes the architectural constraints verifiable
at compile time rather than leaving them to discipline alone.

```text
backend/
├── TivuStream.Pie.sln
├── Directory.Build.props
├── src/
│   ├── TivuStream.Pie.Model/
│   ├── TivuStream.Pie.Core/
│   ├── TivuStream.Pie.Adapters/
│   ├── TivuStream.Pie.Adapters.Technitium/
│   ├── TivuStream.Pie.Storage/
│   └── TivuStream.Pie.Api/
└── tests/
    ├── TivuStream.Pie.Core.Tests/
    ├── TivuStream.Pie.Storage.Tests/
    ├── TivuStream.Pie.Adapters.Technitium.Tests/
    └── TivuStream.Pie.Api.Tests/
```

---

## Project Responsibilities

| Project                                    | Responsibility                                      |
| ------------------------------------------ | --------------------------------------------------- |
| `TivuStream.Pie.Model`                     | The Unified Data Model                               |
| `TivuStream.Pie.Core`                      | The Core and its Engines                             |
| `TivuStream.Pie.Adapters`                  | Adapter contracts and the Adapter Manager            |
| `TivuStream.Pie.Adapters.Technitium`       | The Technitium Adapter                               |
| `TivuStream.Pie.Storage`                   | Persistence, schema and migrations                   |
| `TivuStream.Pie.Api`                       | Application host, REST API and composition root      |
| `TivuStream.Pie.Core.Tests`                | Verification of the rules of analysis                |
| `TivuStream.Pie.Storage.Tests`             | Verification of what is read and written             |
| `TivuStream.Pie.Adapters.Technitium.Tests` | Verification of what the source is asked and answers |
| `TivuStream.Pie.Api.Tests`                 | Verification of what an endpoint answers and refuses |

A test project verifies **a rule stated in the Specifications**, not an
implementation detail.

The separation of the test projects follows that of the projects they verify:
a test project references only what it verifies.

---

## Project References

```text
Model            → no reference

Core             → Model

Adapters         → Model

Adapters.Technitium → Adapters

Storage          → Model

Api              → Core, Adapters, Adapters.Technitium, Storage

Core.Tests       → Core

Storage.Tests    → Storage

Adapters.Technitium.Tests → Adapters.Technitium

Api.Tests        → Api
```

The following rules are architectural constraints.

* The `Model` project references no other project.
* The `Core` project references neither the Adapters nor the host.
* The `Adapters` project does not reference the Core.
* Only `Api`, as the composition root, references a concrete Adapter.

Adding a reference in breach of these rules makes the architectural constraint
immediately visible at build time.

---

# Configuration Files

The Backend uses centralised configuration.

Configuration must be independent of the code.

The shared build configuration lives in `Directory.Build.props` and applies to
every project of the Backend.

Warnings are treated as errors, consistently with the quality criteria of the
project.

---

# Data Flow

The Backend handles two distinct flows.

---

## Query Flow

The path a request from the Frontend follows.

```text id="kfz0c6"
REST API

↓

Validation

↓

Authentication

↓

Authorization

↓

Results produced by the Core Engine
```

The Query Flow never reaches the Adapters or the Data Sources.

---

## Acquisition Flow

The path the periodic acquisition of data follows.

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

Core Engine

↓

Results
```

The Adapter Manager orchestrates the acquisition and invokes the Core Engine
with data already expressed in the Unified Data Model.

The Core Engine never invokes an Adapter.

Acquisitions are aligned to fixed observation periods. A further observation
of the same period replaces the previous one rather than adding to it: two
observations of overlapping intervals describe part of the same traffic and
cannot be added together.

The criterion is defined in the Persistence Specification.

---

# Error Handling

Errors are classified.

Categories.

* Validation
* Authentication
* Authorization
* Backend
* Network
* Internal

Every error is recorded by the Logging Service.

---

# Logging

The system records the events needed for operation and diagnosis.

Logs must contain no sensitive information beyond what is strictly necessary.

---

# Performance

The Backend favours.

* modularity;
* incremental processing;
* reuse of data;
* fewer duplicated operations.

---

# Scalability

The architecture allows the addition of:

* new Adapters;
* new Engines;
* new REST APIs;
* new Data Sources.

The Core stays unchanged.

---

# Security

The Backend:

* protects credentials;
* uses HTTPS;
* validates input;
* checks permissions;
* isolates the Data Sources from the Frontend.

---

# Design Principles

The Backend follows these principles.

* modularity;
* independence;
* extensibility;
* simplicity;
* separation of responsibilities.

---

# Constraints

The Backend:

* contains no presentation logic;
* does not depend on any specific backend;
* uses only the Unified Data Model;
* communicates with the Data Sources only through Adapters;
* keeps the Core Engine isolated from the Adapters and the Data Sources.

The project implementing the Core Engine references neither the Adapters nor
the host of the REST API.

This isolation makes the constraint verifiable at compile time.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 05 - Data Model
* 06 - API
* 10 - Frontend
