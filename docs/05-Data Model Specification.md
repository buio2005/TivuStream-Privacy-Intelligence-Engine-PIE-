# 05 - Data Model

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Data Model Specification

**Version:** 2.2.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the **Unified Data Model** used by the Privacy
Intelligence Engine.

The Data Model is the common language through which every component of the
Core communicates.

Every Adapter converts the data coming from the Data Sources into this format.

---

# Objectives

The Unified Data Model guarantees:

* independence from the backends;
* uniformity of the information;
* simplicity of processing;
* extensibility;
* compatibility between the modules of the Core.

---

# Overview

All information processed by the system is represented through a set of
standardised entities.

```text id="tczvlt"
Data Source

↓

Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine
```

---

# Primary Entities

The data model is composed of the following principal entities.

* DataSource
* NetworkSnapshot
* Statistics
* Device
* Domain
* DomainActivity
* Threat
* SourceConfiguration
* ScoreFactor
* ClassificationList
* Alert
* Recommendation
* Network Privacy & Security Score (NPSS)
* ScoreComponent

---

# DataSource

Represents the origin of the information the system processes.

## Properties

* id
* name
* provider
* version
* status
* capabilities
* lastUpdate

---

## Capabilities

The `capabilities` field declares **which entities of the Unified Data Model
that Data Source is able to provide**.

It does not describe the features of the external product.

This distinction is essential.

* A backend's support for DNSSEC or for DNS over TLS is **data to be
  analysed**, and belongs in `Statistics` or in the configuration.
* A capability is instead **structural** information, which the Core uses to
  know which analyses it can perform.

The vocabulary of capabilities coincides with the names of the entities of the
Unified Data Model.

| Capability            | Meaning                                              |
| --------------------- | ---------------------------------------------------- |
| `Statistics`          | The source provides aggregated statistics             |
| `Device`              | The source allows devices to be identified            |
| `Domain`              | The source exposes the domains observed               |
| `DomainActivity`      | The source relates devices and domains                |
| `SourceConfiguration` | The source exposes its own settings                   |

New capabilities may be added when new entities are introduced.

An absent capability means the data is **not measurable** with that source in
its current configuration.

It does not mean the data is zero.

---

# NetworkSnapshot

Represents the complete state of the network at a given moment.

## Properties

* snapshotId
* timestamp
* sourceId
* statistics
* devices
* alerts
* recommendations
* npss

Every Snapshot is a complete picture of the system.

---

# Statistics

Holds the aggregated statistics of the network.

## Properties

* totalQueries
* blockedQueries
* cachedQueries
* failedQueries
* uniqueDomains
* uniqueDomainsQuality
* activeDevices
* encryptedQueries
* dnssecEnabled

The `uniqueDomains` field is typically a **lower bound**: sources return
truncated lists, so the distinct domains observed are at least that number.

The quality is declared by `uniqueDomainsQuality`.

---

# SourceConfiguration

Represents the settings of the Data Source that bear on privacy and security.

The features a backend offers are **data to be analysed**, distinct from the
capabilities, which declare instead which entities the source is able to
provide.

## Properties

* dnssecValidationEnabled
* encryptedTransports
* queryMinimisationEnabled
* clientSubnetForwardingEnabled
* filteringEnabled
* filterListCount
* filterListUpdateIntervalHours

The properties are expressed in terms independent of any backend.

The `encryptedTransports` field lists the encrypted transports enabled on the
source.

The `clientSubnetForwardingEnabled` field describes a feature that **reduces**
privacy by telling external servers which subnet a query came from. A positive
value worsens the evaluation.

---

# Device

Represents a device identified by the system.

## Properties

* deviceId
* hostname
* ipAddress
* macAddress
* vendor
* operatingSystem
* firstSeen
* lastSeen
* observationQuality
* identityBasis
* status

---

## Associated Objects

Every Device may be associated with:

* DomainActivity
* Threat
* Alert
* Recommendation

---

# Domain

Represents a domain observed during the analysis.

## Properties

* domain
* category
* categoryConfidence
* categorySource
* categorySourceUpdatedAt
* reputation
* firstSeen
* lastSeen
* observationQuality
* occurrences

The properties relating to the category describe **how** the classification
was obtained.

| Property                  | Meaning                                                 |
| ------------------------- | --------------------------------------------------------- |
| `categoryConfidence`      | Reliability of the classification                          |
| `categorySource`          | The list it comes from                                     |
| `categorySourceUpdatedAt` | Last successful update of that list                        |

They are absent when the category is `Unknown`, that is, when the domain was
not classified.

The update date of the list lets the person judge how recent the verdict is,
not only what it says.

The `reputation` field is **optional**.

Reputation is produced by the Threat Engine. An Adapter never assigns it and
leaves it empty.

An absent value means the reputation has not been assessed yet: it is a
statement about the system, not about the domain.

---

# DomainActivity

Represents the interaction between a Device and a Domain.

## Properties

* deviceId
* domain
* queryCount
* blocked
* protocol
* firstSeen
* lastSeen
* observationQuality

---

## Conditional Availability

`DomainActivity` is the only entity whose availability **depends on the Data
Source**.

Some sources provide it natively. Others expose it only after optional
components are enabled. Others do not provide it at all.

A Data Source declares that it can provide it through the `DomainActivity`
capability.

When the capability is absent:

* the Core produces no `DomainActivity` objects;
* the analyses that depend on it are declared not measurable;
* the system generates a Recommendation stating how to make the data
  available.

The absence of the capability is not an error and does not interrupt
processing.

The Core must never replace the missing data with estimated or default values.

---

## Aggregation Criterion

A `DomainActivity` represents a single combination of device, domain,
**outcome** and **protocol**.

Outcome and protocol contribute to the identity of the entity and are not
merged.

A device that reached the same domain both normally and while being blocked
has produced **two distinct facts**, and they are represented by two separate
objects.

Merging them into a single object would force a prevailing outcome to be
chosen, asserting something that did not happen.

The `queryCount`, `firstSeen` and `lastSeen` properties refer to the
combination so defined.

---

# ScoreFactor

Represents a reason that determined the score of an area.

## Properties

* code
* values

The `code` property identifies the statement. The `values` property holds the
values that complete it.

A factor **contains no text**.

The Core produces results, not prose. A sentence already written belongs to
one language, and would make it impossible to present the same result in
several languages without changing the Core.

Numeric values travel as numbers, not as text already formatted: how a
percentage is written belongs to the language, not to the measurement.

The list of codes and their meaning are defined in the NPSS Specification.

---

# ClassificationList

Represents a list used to classify domains.

## Properties

* name
* sourceUrl
* category
* licence
* updatedAt
* entryCount
* enabled

The `licence` property is mandatory: a list with no declared licence is not
distributed with the project.

The `updatedAt` property indicates the last **successful** update. A failed
attempt does not change it: the list held stays valid and simply grows older.

How it works is described in the Threat Intelligence Specification.

---

# Threat

Represents a threat classified by the system.

## Properties

* threatId
* category
* severity
* confidence
* description
* source
* detectedAt

---

# Alert

Represents a significant event produced by the Core.

## Properties

* alertId
* category
* severity
* title
* description
* timestamp
* status

---

# Recommendation

Represents a suggestion generated automatically by the system.

## Properties

* recommendationId
* priority
* title
* description
* generatedAt

---

# Network Privacy & Security Score (NPSS)

Represents the summary score of the state of the network.

## Properties

* overallScore
* status
* trend
* coverage
* algorithmVersion
* generatedAt
* breakdown

The `overallScore` and `status` fields are **optional**.

They are absent when coverage is below the minimum threshold defined by the
NPSS Specification: below that level a summary judgement is not supportable,
and only the detail is presented.

The `coverage` field holds the sum of the `maxScore` of every area, out of a
maximum of 100.

It therefore corresponds to the portion of the evaluation system actually
observed.

A value below 100 means the score was computed over only part of the
indicators provided for.

The `status` field takes the values defined in the Score Range table of the
NPSS Specification.

The `trend` field takes the values Improving, Stable or Decreasing.

The `algorithmVersion` field identifies the version of the algorithm that
produced the score.

---

# ScoreComponent

Represents the contribution of a single area of evaluation to the NPSS.

The `breakdown` collection is made of elements of this type.

## Properties

* component
* state
* score
* maxScore
* weight
* factors

The `component` field corresponds to one of the areas defined in the NPSS
Specification.

The `state` field declares to what extent the area was assessed.

| Value               | Meaning                                                              |
| ------------------- | ---------------------------------------------------------------------- |
| `Measured`          | Every indicator of the area was assessed                                |
| `PartiallyMeasured` | Only some of the indicators were assessed                               |
| `NotMeasurable`     | No indicator can be assessed and `score` is not meaningful              |

---

## Weight and Maximum Score

The `weight` field holds the **nominal weight** of the area, defined by the
NPSS algorithm.

The `maxScore` field holds the **score actually obtainable**, that is, the
portion of the weight corresponding to the indicators really assessed.

| State               | Relation                      |
| ------------------- | ----------------------------- |
| `Measured`          | `maxScore` equal to `weight`  |
| `PartiallyMeasured` | `maxScore` less than `weight` |
| `NotMeasurable`     | `maxScore` equal to zero      |

The unmeasured portion of the weight **contributes neither to the score
obtained nor to the score obtainable**.

It therefore cannot in any case improve the result: what was not observed is
excluded from the calculation, neither estimated nor presumed favourable.

---

When `state` is other than `Measured`, the `factors` field must state which
indicators were assessed and which were not, each with its reason.

The `factors` field holds the factors that determined the score of the area,
in the form of `ScoreFactor`.

Keeping the factors is the requirement that makes every change in the score
explainable.

An area that is not measurable is never represented as an area with a score of
zero.

The conditions describe distinct situations and must remain distinguishable at
every point of the system.

---

# Entity Relationships

```text id="w0d1t9"
NetworkSnapshot

├── Statistics

├── Device[]

│      ├── DomainActivity[]

│      ├── Threat[]

│      ├── Alert[]

│      └── Recommendation[]

├── NPSS

│      └── ScoreComponent[]

├── Alert[]

└── Recommendation[]
```

---

# Ownership of Properties

Not every property of an entity belongs to whoever produces it first.

An Adapter acquires and converts. It does not classify, does not judge, does
not attribute reputations: those are responsibilities of the Core.

A rule follows.

> A property only the Core can populate is optional in the model, and stays
> empty until the Core populates it.

An Adapter must never fill such properties with conventional or placeholder
values: that would amount to asserting something it has not observed.

The exception is where the documentation explicitly defines a fallback value,
such as `Unknown` for the category of a domain.

Properties currently subject to this rule.

| Entity   | Property     | Assigned by   |
| -------- | ------------ | ------------- |
| `Domain` | `reputation` | Threat Engine |

---

## Status Vocabularies

Operational states use defined sets of values.

**DataSource.status**

| Value         | Meaning                                          |
| ------------- | ------------------------------------------------- |
| `Online`      | The source answered correctly                      |
| `Unreachable` | The source cannot be reached or refused            |

It is the only part of the description that only the Adapter can establish.

**Device.status**

| Value      | Meaning                                               |
| ---------- | ------------------------------------------------------ |
| `Active`   | The device produced traffic within the window           |
| `Inactive` | The device produced no traffic within the window        |

The state describes presence, not health. Judging behaviour belongs to the
Device Engine.

Both sets are minimal and may be extended when documented needs arise.

---

# Data Availability

The model distinguishes three conditions that must never be confused.

| Condition           | Meaning                                                    |
| ------------------- | ------------------------------------------------------------ |
| Data present        | The information was acquired and is populated                 |
| Data absent         | The information is measurable and nothing happened            |
| Data not measurable | The source is not able to provide the information             |

A value of zero belongs to the second condition and is a statement about the
real world.

The third condition is instead a statement about the system, and is to be
represented through the absence of the relevant capability, never through a
value.

This distinction follows directly from the Transparency principle.

---

# Measurement Quality

The availability of a piece of data does not exhaust what has to be declared.

A value may be known **with differing precision**, and treating knowledge as
binary forces a choice between two errors: inventing a plausible value, or
discarding information actually held.

The score already distinguishes between an area measured, partly measured and
not measurable. The same criterion applies to the individual value.

---

## Quality Levels

| Quality         | Meaning                                                              |
| --------------- | ---------------------------------------------------------------------- |
| `Exact`         | Measured directly                                                       |
| `LowerBound`    | The real value is at least the one stated, possibly higher               |
| `PeriodBounded` | The event happened within the observation period, the moment unknown     |
| `Estimated`     | Inferred by a method that has to be declared                             |

---

## Principle

> A value known with less precision is **qualified**, not discarded and not
> rounded to the plausible.

Two examples show the difference from binary treatment.

**Unique domains.** The source returns truncated lists. The number of distinct
domains is not "approximate" in any vague sense: it is a **lower bound**, that
is, a precise statement. Presenting it as an exact count is false; omitting it
discards useful data.

**Instant of first observation.** Where the data derives from statistics over
a window, the system knows the event happened **within that period**, not at
which minute. Writing the beginning of the period as though it were the
instant observed is an invention; leaving it empty loses knowledge actually
held.

When the same property becomes available with greater precision, for instance
from query logs, the declared quality changes accordingly and **the person
sees the difference**.

---

## Application

| Entity           | Property                   | Quality declared by          |
| ---------------- | -------------------------- | ---------------------------- |
| `Statistics`     | `uniqueDomains`            | `uniqueDomainsQuality`       |
| `Device`         | `firstSeen`, `lastSeen`    | `observationQuality`         |
| `Domain`         | `firstSeen`, `lastSeen`    | `observationQuality`         |
| `DomainActivity` | `firstSeen`, `lastSeen`    | `observationQuality`         |

A property with no declared quality is to be understood as `Exact`.

The interface never presents a qualified value as though it were exact.

---

# Device Identity

The identity of a device may rest on grounds of differing solidity.

| Basis             | Meaning                                                   |
| ----------------- | ----------------------------------------------------------- |
| `HardwareAddress` | A stable identity, independent of the network address        |
| `NetworkAddress`  | An identity derived from the network address                 |

An identity based on the network address carries two consequences the person
has to know.

* A device that changes address appears as a different device.
* An address reassigned to another device merges two distinct identities.

The `identityBasis` field of `Device` declares on what basis the identity was
established.

Attributing a behaviour to a device is a strong claim. The system must make
plain how solid it is.

---

# Entity Identity

Every entity carries a unique identifier.

Identifiers are independent of the backend in use.

---

# Versioning

The Unified Data Model carries a version of its own.

Incompatible changes increment the Major Version.

Compatible changes increment the Minor Version.

---

# Serialization

The Unified Data Model must be serialisable in the following formats.

* JSON
* CSV
* XML (possible future support)

Serialisation does not change the logical structure of the entities.

---

# Extensibility

New properties may be added while keeping backward compatibility.

New entities must integrate through relations already defined.

---

# Design Principles

The Data Model follows these principles.

* uniformity;
* independence;
* simplicity;
* modularity;
* extensibility;
* reusability.

---

# Constraints

The Unified Data Model:

* contains no business logic;
* contains no presentation logic;
* does not depend on any specific backend;
* is the only data format used by the Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 06 - API
* 07 - Network Privacy & Security Score
