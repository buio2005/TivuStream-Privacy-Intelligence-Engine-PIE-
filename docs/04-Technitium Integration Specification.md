# 04 - Technitium Integration

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Technitium Integration Specification

**Version:** 1.7.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines how the Privacy Intelligence Engine (PIE) and
Technitium DNS Server are integrated.

Technitium is the first backend officially supported by the project.

---

# Scope

The purpose of the integration is to acquire data from the DNS backend and
make it available to the Privacy Intelligence Engine through the
corresponding Adapter.

The management of DNS remains entirely delegated to Technitium.

---

# Architectural Role

Technitium is a **Data Source**.

It does not belong to the Core of the project.

It contains no logic of analysis developed by PIE.

Its responsibility ends with the production of the data.

---

# Responsibilities

## Technitium

Technitium handles:

* DNS Resolution
* Cache
* DNSSEC
* DNS over HTTPS (DoH)
* DNS over TLS (DoT)
* DNS over QUIC (DoQ)
* Zone Management
* Query Logging
* Statistics
* Blocklists

---

## Privacy Intelligence Engine

PIE handles:

* acquisition of data;
* normalisation;
* classification;
* correlation;
* analysis;
* computation of the NPSS;
* generation of Alerts;
* generation of Recommendations;
* production of Reports.

---

# Integration Architecture

```text id="vtahj0"
Technitium DNS Server

↓

HTTP API

↓

Technitium Adapter

↓

Unified Data Model

↓

Privacy Intelligence Engine
```

---

# Adapter Responsibilities

The Adapter is the only component authorised to communicate with Technitium.

Its responsibilities comprise:

* authentication;
* handling of the HTTP requests;
* conversion of the data;
* handling of errors;
* normalisation of the format.

The Adapter performs no processing.

---

# Retrieved Data

The integration acquires the following information.

## Server Information

* version;
* operational state;
* configuration.

---

## DNS Statistics

* DNS queries;
* blocked queries;
* cache;
* errors;
* protocols used;
* general statistics.

---

## Client Information

* network addresses;
* hostnames (when available);
* activity statistics.

---

## Domains

* domains observed;
* domains blocked;
* frequency of the queries.

---

## Blocklists

* state;
* last update;
* number of domains handled.

---

## Logs

Technitium distinguishes two kinds of log.

**Diagnostic logs of the server.** Always available. They contain system
events and information about operation.

**Query logs.** Not available in a default installation. They require an
optional component to be enabled.

The presence of the component is detected by asking for the list of the
applications installed.

The Adapter declares the `DomainActivity` capability **only when the component
turns out to be actually installed**. The implementation of the corresponding
interface expresses what the Adapter is able to do; the capability expresses
what that instance offers at that moment.

Aggregation happens while the pages of the logs are read. The register of
individual queries is never kept whole and does not pass beyond the Adapter.

---

# Data Availability Levels

The integration with Technitium provides for two levels of data availability.

The distinction is structural and is to be declared through the capabilities
of the Data Source.

---

## Base Level

Available on any installation, with no additional components and at no cost.

Capabilities declared.

```text
Statistics
Device
Domain
SourceConfiguration
```

Data that can be acquired.

* aggregated statistics of the network;
* configuration of the DNS service, including DNSSEC and encrypted protocols;
* state of the blocklists;
* list of the devices with their volume of traffic;
* list of the domains observed with their frequency.

Every value at this level is **exact**, not estimated.

---

## Extended Level

Requires the installation of an optional Technitium component dedicated to
recording queries.

Capabilities declared.

```text
Statistics
Device
Domain
SourceConfiguration
DomainActivity
```

Additional datum.

* the correlation between device and domain.

This correlation is what makes it possible to attribute threats to devices and
to evaluate the Device Health area of the NPSS.

---

## Rationale

The dashboard APIs of Technitium expose devices and domains as **independent
aggregates**.

They say how many queries each device produced and how many times each domain
was asked for, but not which device contacted which domain.

The correlation exists only in the query logs.

This is a characteristic specific to Technitium and not to the application
domain: other Data Sources the project provides for expose the datum natively.

For this reason the extended level is not a prerequisite of the project.

---

# Data Conversion

Every datum retrieved is converted into the format defined by the Unified Data
Model.

No component of the Core uses directly the original format returned by
Technitium.

---

# Backend Independence

The Core contains no reference specific to Technitium.

Replacing the backend requires only the building of a new Adapter.

---

# Error Management

The errors returned by Technitium are converted into standardised events.

The Core receives normalised information only.

---

# Authentication

The Adapter authenticates using a non-expiring **API Token**, provided by
Technitium for automation.

An ordinary session token is not suitable: it expires through inactivity and
would require the life cycle of the session to be managed inside the
Acquisition Flow.

The token is carried in the header of the request.

```text
Authorization: Bearer <token>
```

---

## Least Privilege

The token must belong to a **dedicated user with minimum permissions**.

**Read-only** permissions on the **Dashboard** and **Settings** sections are
required.

The permission on the settings is necessary in order to acquire the
configuration of the server, on which the DNS Security and Configuration areas
of the score depend. Without it those areas would be largely not measurable.

The token must belong to a user **who is part of no group**. In Technitium a
new user ordinarily enters the Everyone group, which grants reading of other
sections: the user would receive more than is declared here.

**Domain Activity requires no further permission.** With Technitium 15.4, a
user with read permissions on Dashboard and Settings alone, outside every
group, reads the list of the installed applications and the logs of the Query
Logs application. Verified in the field on 2026-09-27: 280 queries aggregated
into 12 activities, no error. All that is needed is for the application to be
installed.

It is a behaviour observed on that version, not a guarantee from Technitium.
For this reason, if the list of applications is not readable, the Adapter does
not declare Domain Activity and goes on serving the base level, instead of
refusing the whole Data Source.

No permission to modify is required in any case: PIE never alters the
configuration of the Data Source.

Using the administrative user is discouraged: a compromised token would
inherit privileges unnecessary to acquisition, including the ability to modify
the server.

---

## Session Information

```text
GET /api/user/session/get
```

A single call returns the version of the server, the name configured, the
state of DNSSEC validation and the **effective permissions of the token**.

This call is the source to use for the description of the Data Source.

Two relevant consequences.

* The state of DNSSEC is available without reaching the settings of the
  server, and therefore without requiring permissions beyond those of
  acquisition.
* The permissions returned allow the Adapter to declare a capability only when
  the token is really able to read the corresponding datum.

An earlier version of this specification gave the settings as the source of
the state of DNSSEC. That indication is superseded: the settings require wider
permissions and are not necessary at the base level.

---

## Response Shapes

The APIs use **two different response shapes**.

**Nested payload.** Most calls, including every one of the dashboard, enclose
the content in a dedicated property beside the outcome.

```json
{ "status": "ok", "response": { } }
```

**Payload at the root.** The calls relating to the session return their own
fields directly at the first level, beside the outcome.

```json
{ "status": "ok", "info": { } }
```

The Adapter must support both shapes.

The distinction cannot be deduced from the name of the call and is to be
verified case by case.

---

## Error Semantics

The Technitium APIs **do not express the outcome through the HTTP status
code**.

A response with a negative outcome may come with HTTP code 200 and report the
error in the body.

The Adapter must therefore always evaluate the content of the response and not
limit itself to the status code.

Error responses may contain diagnostic detail from the backend. That detail
must not be propagated beyond the Adapter nor recorded in the logs.

---

# Acquisition Constraints

The reconnaissance of the APIs brought out three constraints the Adapter must
respect.

---

## Time Window Selection

The statistics of Technitium are aggregated over predefined time windows.

Different windows **do not necessarily contain the same data**: it was
observed that the predefined window relating to the last day returns the
detail per domain empty while reporting a higher number of queries.

The Adapter therefore cannot assume that a wider window includes what a
narrower one contains.

The window used for each kind of datum is to be chosen explicitly and
documented.

**The custom interval preserves the complete detail.** Verification on a real
instance compared the statistics acquired with those shown by the console of
the server, and found exact correspondence of counts, distinct domains and
clients.

The Adapter therefore uses the custom interval, which allows incremental
acquisition without having to fall back on the predefined windows.

---

## Log Rotation

The component recording queries applies a retention based on the **number of
records**, as well as on time.

The default limit corresponds, on a real home network, to a fraction of an
hour of traffic.

An operational constraint follows.

* The frequency of acquisition must be **shorter than the rotation time of the
  log**.
* If the frequency is insufficient, data is lost **with no signal whatever
  from the backend**.

The Adapter must read the configuration of the component and signal the
inconsistency between the retention configured and the frequency of
acquisition.

This check is compulsory: a silent loss of data produces analyses that are
plausible and wrong, which is the worst condition for a tool of this kind.

---

## Failed Queries

Technitium distinguishes four negative outcomes: `totalServerFailure`,
`totalRefused`, `totalDropped` and `totalNxDomain`.

The Unified Data Model provides for a single counter `failedQueries`, which
comprises the first three.

```text
failedQueries = totalServerFailure + totalRefused + totalDropped
```

**NXDOMAIN is excluded.** It indicates that the domain asked for does not
exist: it is a correct answer from the service, not a malfunction of it.

On a normal network NXDOMAINs are frequent. Including them would produce a
high error rate in the absence of any problem at all, and would unduly lower
the Network Integrity area of the score.

---

## Device Identity

Technitium identifies clients by network address alone.

The Adapter derives the identifier of the device from the address
deterministically, so that the same device keeps its identity across
successive acquisitions.

A limit to be declared to the user follows.

* A device that changes address appears as a different device.
* An address reassigned to another device merges the two identities.

---

## Hardware Address When Available

When Technitium also plays the role of **DHCP server**, it exposes the leases
in force, which associate a hardware address with a network address.

The Adapter acquires them when available and derives from them an identity of
the device that is **stable across changes of address**.

The `identityBasis` field of the device declares which basis was used.

| Condition                  | Basis declared    |
| -------------------------- | ----------------- |
| A matching DHCP lease       | `HardwareAddress` |
| No matching lease           | `NetworkAddress`  |

The two bases can coexist in the same acquisition: a device with a static
address lives alongside others managed by DHCP, and each declares its own.

**The identity holds for the activity too.** The identifier of
`DomainActivity` is derived by the same rule as that of `Device`: an activity
referring to an address that has a DHCP lease in force carries the identifier
founded on the hardware address, not the one founded on the network address.
If device and activity each chose for themselves, the same device would appear
under two identifiers and the two pieces of information could never be brought
into relation.

An address that had a lease during the period but no longer has one at the
moment of acquisition keeps the weaker basis: it is a declared limit, not an
error.

Attributing a behaviour to a device is a strong statement. The system makes
explicit how solid it is instead of leaving it to be understood.

---

## Truncated Lists

The calls returning rankings apply a maximum limit of elements.

There is no call returning the complete list of the domains observed.

The values that depend on the completeness of the list, such as the number of
unique domains, are therefore **approximate** and must be declared as such.

---

# Aggregation Responsibility

When the extended level is available, the Adapter **aggregates the query logs
before delivering them to the Core**.

The Core receives `DomainActivity` objects already consolidated and never
reaches an individual query.

This choice answers three needs.

* **Privacy.** The register of individual queries is the browsing history of
  every device. PIE does not keep it.
* **Volume.** The aggregate is orders of magnitude smaller than the raw datum.
* **Separation of responsibilities.** The Core works on the Unified Data Model
  and not on formats specific to a backend.

---

# Security

The credentials for reaching the APIs stay confined inside the Adapter.

The Frontend never communicates directly with Technitium.

---

# Compatibility

PIE keeps the greatest possible compatibility with the public APIs of
Technitium.

No component of the original project is modified.

No fork is made.

---

# Update Strategy

The evolution of Technitium remains independent of that of the Privacy
Intelligence Engine.

The Adapter is the compatibility layer between the two platforms.

---

# Design Principles

The integration follows these principles.

* use of the public APIs alone;
* no modification of the code of Technitium;
* complete separation between backend and Core;
* architectural independence;
* modularity.

---

# Constraints

The integration introduces no:

* direct dependency in the Core;
* logic of analysis;
* customisation of the backend;
* modification of the original components of Technitium.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
* 09 - Network Privacy
