# 02 - Architecture

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Architecture Specification

**Version:** 1.1.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the general architecture of the Privacy
Intelligence Engine (PIE) and the relations between the principal components
of the system.

---

# Architectural Overview

The architecture is divided into independent layers.

Each layer has specific responsibilities and communicates only through well
defined interfaces.

```text
                    Data Sources
                         │
                         ▼
                     Adapters
                         │
                         ▼
              Unified Data Model
                         │
                         ▼
         Privacy Intelligence Engine
                         │
      ┌──────────────────┼──────────────────┐
      ▼                  ▼                  ▼
Threat Engine     Device Engine      NPSS Engine
      │                  │                  │
      └──────────────────┼──────────────────┘
                         ▼
              Recommendation Engine
                         │
                         ▼
                    REST API Layer
                         │
                         ▼
                 Frontend Applications
```

---

# Architectural Layers

## Data Sources

The layer responsible for producing data.

The Privacy Intelligence Engine does not depend on any specific data source.

---

## Adapter Layer

Every Data Source has its own Adapter.

The Adapter converts the data into the internal format defined by the Unified
Data Model.

---

## Unified Data Model

The Data Model is the common language used by the Core.

Every internal component communicates only through this model.

---

## Privacy Intelligence Engine

The Core of the system.

It coordinates all analysis and produces the objects the applications use.

---

## REST API Layer

Exposes the information processed by the Core.

It is the only official point of access to the data.

---

## Frontend

Displays the information produced by the Core.

The Frontend performs no processing.

In an installation its compiled files are **served by the engine**, on the same
address as the API: one address, one certificate, no rules between different
origins (Transport Security Specification). The Frontend still communicates
with the Core only through the REST API.

---

# Core Components

The Privacy Intelligence Engine is made up of the following modules.

* Threat Engine
* Device Engine
* NPSS Engine
* Recommendation Engine
* Alert Engine

Each module has one specific responsibility.

---

# Data Flow

The system uses two distinct flows.

The two do not overlap and carry different responsibilities.

---

## Acquisition Flow

The periodic acquisition of data.

It is triggered by the Scheduler and orchestrated by the Adapter Manager.

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

Privacy Intelligence Engine

↓

Results
```

The Core receives only data already expressed in the Unified Data Model.

The Core takes no part in orchestrating the acquisition.

---

## Query Flow

A request coming from the Frontend.

```text
Frontend

↓

REST API

↓

Results already produced by the Core
```

The Query Flow triggers no communication towards the Adapters or the Data
Sources.

Requests from the Frontend return only results that have already been
processed.

---

# Responsibilities

## Data Sources

Produce information.

---

## Adapters

Convert the data into the internal format.

---

## Core

Analyses.

Correlates.

Classifies.

Judges.

---

## REST API

Exposes the data.

---

## Frontend

Displays the information.

---

# Communication

The modules of the Core communicate through the shared data model.

No module communicates directly with the Frontend or with external backends.

---

# Scalability

The architecture allows the addition of:

* new Data Sources;
* new Adapters;
* new Engines;
* new applications.

Extending the system does not change the principal architecture.

---

# Backend Independence

The Core holds no dependency on Technitium or on any other provider.

Every integration is implemented solely through its own Adapter.

---

# Design Principles

The architecture follows these principles.

* Modularity
* Independence
* Scalability
* Simplicity
* Extensibility
* Reusability

---

# Architectural Constraints

The following rules are architectural constraints.

* The Frontend communicates only with the REST API.
* The Core does not communicate directly with the Data Sources.
* The Core does not orchestrate the acquisition of data.
* Acquisition is orchestrated by the Adapter Manager and triggered by the
  Scheduler.
* The Query Flow never reaches the Data Sources.
* Every Data Source implements its own Adapter.
* All processing is performed by the Core.
* The Unified Data Model is the only internal data format.
* No business logic is present in the Frontend.
* No presentation logic is present in the Core.

---

# Related Specifications

* 00 - Glossary
* 01 - Vision
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
