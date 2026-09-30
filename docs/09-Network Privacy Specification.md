# 09 - Network Privacy

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Network Privacy Specification

**Version:** 1.7.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines **Network Privacy**, the first application built
using the Privacy Intelligence Engine (PIE).

Network Privacy is the presentation layer of the PIE ecosystem and lets the
person see, understand and manage the information produced by the Core.

---

# Objectives

The application has the following objectives.

* to present information simply;
* to show the state of the network;
* to show the Network Privacy & Security Score (NPSS);
* to bring threats and anomalies to attention;
* to provide actionable recommendations;
* to make analysing the network easier.

---

# Scope

Network Privacy is an application.

It is not an analysis engine.

It implements no classification algorithms.

It does not communicate directly with the Data Sources.

It uses only the public APIs of the Privacy Intelligence Engine.

---

# Architecture

Network Privacy operates entirely within the Query Flow.

```text id="g1nvw8"
Frontend

↓

REST API

↓

Results produced by the Privacy Intelligence Engine
```

The application takes no part whatsoever in the Acquisition Flow.

---

# Main Dashboard

The Dashboard is the entry point of the application.

It shows an overview of the state of the network.

---

# Primary Widgets

The Dashboard comprises the following components.

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

The application is organised into the following sections.

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

The Devices section shows every device detected.

For each device the principal information is shown.

* name;
* IP address;
* state;
* activity;
* Alerts;
* Threats;
* statistics.

---

# Domains

The Domains section shows the domains the system observed.

For each domain it shows.

* category;
* reputation;
* number of queries;
* devices involved.

---

# Threats

The Threats section presents every threat classified by the Core.

The information can be filtered and sorted.

---

# Alerts

The Alerts section shows the events the system generated automatically.

Alerts are organised by severity.

---

# Recommendations

The Recommendations section gathers every suggestion produced by the Core.

Each recommendation is linked to the events that generated it.

---

# Statistics

The Statistics section presents aggregated data about the network.

It covers:

* DNS traffic;
* queries;
* domains;
* cache;
* devices;
* protocols.

---

# Reports

The application allows reports to be generated.

Formats planned.

* PDF
* CSV
* JSON

---

# Search

The application includes a global search.

Search makes it quick to find:

* devices;
* domains;
* Threats;
* Alerts.

---

# Filters

Every list supports dynamic filters.

Examples.

* category;
* severity;
* time range;
* device;
* domain.

---

# Honesty of Presentation

The application never presents as measured something that is not.

This requirement carries the same weight as the functional ones.

---

## Absent Versus Unmeasurable

The interface always distinguishes three conditions.

| Condition           | What it means to the person                              |
| ------------------- | --------------------------------------------------------- |
| No result           | It was observed, and nothing came up                       |
| Partly observed     | It was observed in part, the rest is not accessible        |
| Not measurable      | It could not be observed: the source or the configuration does not provide it |

A value of zero, an empty section or a flat graph belong to the first
condition and tell the person that their network is in order.

Using them to represent the other two conditions is false information.

Sections that are not measurable, or observed only in part, are presented in a
visually distinct way, together with what was assessed, what was not, and why.

A partial condition is never presented as a complete one: the interface makes
plain that the result shown refers to a portion of the whole.

---

## Score Coverage

Whenever the coverage of the Network Privacy & Security Score is below 100,
the interface shows it beside the score.

Every area is listed, including those partly measured and those not
measurable, together with the indicators that were assessed and the reason the
others were excluded.

The history marks changes in coverage, because scores with different coverage
are not comparable.

---

## Guided Configuration

Recommendations produced because of a missing capability are presented as
**proposed actions**, not as error notices.

Each proposal states, symmetrically:

* which analysis it would enable;
* what has to be done;
* what it entails, including the unfavourable consequences.

The last point is binding.

Where enabling a feature increases resource consumption, records additional
data, or affects performance, those aspects are declared **before** the person
chooses.

The application does not present a configuration by listing only its benefits.

The person must be able to decline a proposal and carry on using the system
with no limitations beyond those declared.

---

## Qualified Values

A value that declares a quality other than exact is **never presented as
exact**.

| Declared quality | Presentation required                                  |
| ---------------- | -------------------------------------------------------- |
| `LowerBound`     | State that the real value is at least the one shown       |
| `PeriodBounded`  | State the period, not a precise moment                    |
| `Estimated`      | State that it is an inference                             |

Presenting a lower bound as a count, or the beginning of a period as the
moment of an event, is a false statement even when the number shown is
correct.

The interface does not round an uncertain value and present it as exact.

---

## Device Identity

The interface declares on what basis the identity of a device was established.

Where the identity derives from the network address, the person is told that
the device may change identity when the address changes.

Attributing a behaviour to a device is the strongest claim the system makes.
How solid it is must be made visible, not left to be inferred.

---

## Domain Classification

Every classification shown declares **where it comes from and how recent it
is**.

| Element              | Presentation required                                     |
| -------------------- | ----------------------------------------------------------- |
| List it comes from   | Name of the list that produced the classification            |
| Age of the list      | Date of the last successful update of that list              |
| Confidence           | Direct match distinguished from inference on the parent domain |

A domain in the category `Unknown` is presented as **not classified**, never
as safe.

That is the difference between saying we do not know and saying there is
nothing. Only the first is true.

---

## The Wording Belongs To The Interface

The Core produces no sentences. Every factor arrives as a **code with its own
values**, and the interface renders it in words.

It follows that the honesty constraints of this document apply to the
**translation catalogue**, not only to the source code.

The catalogue is part of the product. A translation that wrote "clean
network" in place of "no known tracking" would breach this specification
exactly as the engine would.

The rules below therefore hold for every language offered.

---

## Known, Not Absent

Indicators founded on classification measure what the lists **recognise**.

The interface never presents full marks in those areas as an absence of
tracking or of threats.

| Forbidden           | Required                                      |
| ------------------- | ----------------------------------------------- |
| "No tracking"       | "No **known** tracking"                          |
| "Clean network"     | "No threat recognised by the lists"              |
| "You are protected" | "Nothing known was observed"                     |

The difference is not formal caution. A domain absent from every list may be
harmless or may be a tracker no list knows about, and the system cannot tell
them apart.

Saying "no tracking" would be the one claim in the entire product that the
product cannot support.

---

## Device Visibility

The list of devices comprises **only the devices that use this DNS service**.

A device configured with a resolver of its own, or using encrypted DNS towards
an external service, appears in no statistic. It does not show as having no
activity: it does not show at all.

The interface declares this condition beside the list of devices.

The reason is concrete. Someone watching their own network thinks first of
computers and phones, while televisions, consoles and household appliances are
perceived as objects of use rather than as connected devices. They are also
the ones most likely to carry a resolver hardwired by the manufacturer.

The absence of a device from the list is therefore information, and is to be
presented as such instead of being left to read as good news.

---

## Withheld Score

When coverage is below the minimum, the overall score does not exist and the
interface **puts nothing in its place**.

The breakdown of the areas is presented, with those measured, those partial
and those not measurable, each with its reason.

The condition is to be communicated as a choice the system made, not as a
fault or a load still running: the system holds valid measurements and is
declaring that it has not enough to support an overall judgement.

No provisional figure, no empty bar, no placeholder suggesting a value is on
its way.

---

# User Experience

The interface favours:

* simplicity;
* clarity;
* readability;
* accessibility.

Critical information must be identifiable at once.

---

# Data Refresh

The information displayed is refreshed through the REST API.

How often is configurable.

---

# Notifications

The application displays the Alerts produced by the Core.

Handling Alerts remains the responsibility of the Privacy Intelligence Engine.

---

# Security

Network Privacy does not store credentials of the Data Sources.

Access requires an account with a role. What the role does not include is
declared as withheld and not presented as absent: see the Authentication
Specification.

Communication happens only through the REST API of the Core.

---

# Extensibility

The interface is designed to host new sections without changing its principal
structure.

---

# Design Principles

Network Privacy follows these principles.

* simplicity;
* modularity;
* readability;
* uniformity;
* independence from the backend.

---

# Constraints

Network Privacy:

* contains no business logic;
* performs no analysis;
* does not communicate directly with the backends;
* uses only the REST API of the Privacy Intelligence Engine.

---

# Related Specifications

* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 06 - API
* 07 - Network Privacy & Security Score
* 08 - Threat Intelligence
* 10 - Frontend
