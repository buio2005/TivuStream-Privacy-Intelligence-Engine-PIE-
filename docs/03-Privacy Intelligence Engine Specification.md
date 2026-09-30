# 03 - Privacy Intelligence Engine

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Core Engine Specification

**Version:** 1.0.1

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines how the Privacy Intelligence Engine (PIE), the Core
of the project, works.

The engine is the analysis layer of the whole TivuStream ecosystem and the
central point through which all processed information passes.

---

# Overview

The Privacy Intelligence Engine receives normalised data from the Adapters,
analyses it through specialised modules, and produces information the
applications can use.

The Core does not gather data itself.

The Core does not present data to the person.

The Core interprets data.

---

# Core Activation

The Core is a passive component.

It starts no processing of its own and does not know where the data it
receives came from.

Running the pipeline is requested by the Adapter Manager at the end of the
Acquisition Flow, which hands the Core a set of data already expressed in the
Unified Data Model.

The Core returns the results of the processing without knowing their
recipient.

This separation guarantees that the Core behaves identically whatever Data
Source is in use.

---

# Responsibilities

The Core is responsible for:

* logical normalisation;
* classification;
* correlation;
* analysis of devices;
* analysis of threats;
* computing the NPSS;
* generating Alerts;
* generating Recommendations.

---

# Core Pipeline

Every piece of data follows the same pipeline.

The pipeline of the Core begins at the Unified Data Model.

The layers before it belong to the Acquisition Flow and are described in the
Architecture Specification.

```text
Unified Data Model

↓

Validation

↓

Classification

↓

Correlation

↓

Analysis

↓

Evaluation

↓

Results
```

---

# Processing Stages

## Validation

Checks that the data received is correct and complete.

Data that is not valid is discarded or reported.

---

## Classification

Every element is classified according to the rules the system defines.

Examples.

* domain;
* device;
* threat;
* event.

---

## Correlation

The engine relates information coming from different sources.

Correlation makes it possible to build a complete view of the network.

---

## Analysis

The modules of the Core process the normalised information.

Every module operates only on the Unified Data Model.

---

## Evaluation

The engine produces quantitative indicators.

Among them:

* NPSS;
* Device Score;
* Threat Score.

---

## Result Generation

The Core produces:

* objects;
* events;
* statistics;
* alerts;
* recommendations.

These are the final result of the processing.

---

# Core Modules

## Threat Engine

Analyses and classifies threats.

---

## Device Engine

Analyses the behaviour of devices.

---

## NPSS Engine

Computes the Network Privacy & Security Score.

---

## Alert Engine

Generates significant events.

---

## Recommendation Engine

Produces suggestions based on the results of the analysis.

---

# Internal Communication

The modules of the Core communicate through the Unified Data Model.

There are no direct dependencies between modules.

Each component receives processed data and returns new results.

---

# Event Model

The Core uses internal events to coordinate its activities.

Every event carries at least:

* an identifier;
* a category;
* an origin;
* a timestamp;
* its content.

Events are not exposed directly to the applications.

---

# Internal Objects

The Core uses only the entities defined in the Data Model Specification.

The principal ones are:

* NetworkSnapshot
* Device
* Domain
* DomainActivity
* Threat
* Alert
* Recommendation
* NPSS

---

# Backend Independence

The Core contains no code specific to any backend.

Every integration is handled by the Adapters.

The Core behaves identically whatever Data Source is in use.

---

# Error Handling

Every internal error is turned into a standardised event.

Errors do not interrupt the pipeline except where the consistency of the data
would be compromised.

---

# Performance

The Core favours:

* incremental processing;
* reuse of data;
* fewer duplicated computations;
* modularity.

---

# Scalability

New Engines can be added without changing the existing ones.

Every new component must use the Unified Data Model.

---

# Design Principles

The Core follows these principles.

* independence from the backend;
* single responsibility;
* modularity;
* extensibility;
* uniformity of the data model;
* absence of presentation logic.

---

# Constraints

The Core does not:

* communicate directly with the Frontend;
* communicate directly with the Data Sources;
* orchestrate the acquisition of data;
* contain interface logic;
* handle configuration specific to a backend.

---

# Related Specifications

* 00 - Glossary
* 01 - Vision
* 02 - Architecture
* 04 - Technitium Integration
* 05 - Data Model
* 06 - API
* 07 - Network Privacy & Security Score
