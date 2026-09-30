# 16 - Persistence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Persistence Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines how the Privacy Intelligence Engine keeps data over
time.

Keeping data is required by features other Specifications already provide for:
the history of the Network Privacy & Security Score, the trend, the history of
threats, comparisons over time, and the generation of reports.

---

# Why Persistence Is Required

Data Sources do not keep data indefinitely.

Technitium applies a retention of its own and, for query logs, a limit based
on the number of records which on a real network amounts to a fraction of an
hour.

One principle follows.

> What is not kept at the moment of acquisition is lost for good.

PIE cannot therefore query the source at the moment of the request: it must
build its own history.

---

# What Is Persisted

## Acquisitions

Acquisitions converted into the Unified Data Model.

They comprise `Statistics`, `Device`, `Domain` and, where available,
`DomainActivity`.

Keeping acquisitions makes it possible to **recompute analyses over historical
data** without querying the source again, which the retention of the backends
makes impossible.

This keeps the history coherent when an analysis algorithm is changed.

---

## Core Results

The results produced by the Core.

They comprise `NetworkSnapshot`, `Npss`, `Threat`, `Alert` and
`Recommendation`.

Keeping them avoids recomputing the whole analysis on every request, which
matters on modest hardware.

---

## Reference Data

The reference data the installation holds, not what the network did.

It comprises `ClassificationList`.

It belongs to no Observation Period and is **not subject to retention**:
applying retention to a list would remove the means of classifying rather than
an observation that has aged.

---

## Accounts And Sessions

Who uses PIE, not what the network did. Two entities internal to the Backend,
outside the Unified Data Model.

* **account**: user name, role, whether active, password hash with the
  parameters of the algorithm, whether the password must be changed, moment of
  creation;
* **session**: hash of the identifier, account, creation, last use, expiry.

They belong to no Observation Period and are **not subject to retention**:
deleting an account because of its age would lock out whoever owns it. Expired
sessions, on the other hand, are deleted.

---

## What Is Never Persisted

PIE **never keeps the detail of an individual DNS query**.

The per-query log is the browsing history of every device on the network.
Aggregation happens in the Adapter, before the data reaches the Core, and what
is kept is only the aggregated result.

This is the principal protection the project provides for the person using it.

Also never kept:

* credentials of the Data Sources in the clear;
* account passwords in the clear, session identifiers in the clear, the
  initial setup code;
* original responses from the backends in their native format;
* diagnostic detail produced by the backends.

---

# Observation Periods

This section defines the central concept of persistence.

---

## The Problem

An acquisition **is not a set of events**: it is the observation of an
interval of time.

Two observations of overlapping intervals describe part of the same traffic.
They cannot be added together.

A system acquiring a sixty minute window every five minutes produces twelve
observations an hour describing largely the same data. Adding them would
produce meaningless values; picking one arbitrarily would discard information.

---

## The Rule

Acquisitions are aligned to **fixed observation periods**.

The default period is the **clock hour**.

| Concept          | Definition                                        |
| ---------------- | --------------------------------------------------- |
| Period           | A fixed interval, aligned to the hour                |
| Current period   | The one the instant of acquisition falls into        |
| Elapsed period   | Any period entirely in the past                      |

The following rules hold.

**An acquisition observes the current period.**

**A further observation of the same period replaces the previous one**, it does
not add to it.

**An elapsed period is immutable.** Once passed and observed, it is never
updated again.

**The history is the sequence of elapsed periods**, which do not overlap and
can therefore be aggregated.

---

## Consequences

Idempotence is guaranteed by construction: acquiring the same period several
times does not alter the result.

The frequency of acquisition becomes a parameter of **freshness**, not of
correctness. Acquiring every five minutes updates the current period more
often; it produces no duplication.

The frequency must nonetheless stay **below the retention of the source**,
otherwise a period may elapse before it has been observed and its data is lost
without notice.

The observation period is configurable. Shorter periods increase both
resolution and volume; longer ones reduce both.

---

# Retention

Retention is **tiered**.

| Tier                | Content                          | Default retention |
| ------------------- | -------------------------------- | ----------------- |
| Observation periods | Hourly detail                    | 30 days           |
| Daily aggregates    | A summary per day                | 12 months         |
| Monthly aggregates  | A summary per month              | 5 years           |

When a tier expires, the data is consolidated into the next one and the detail
is deleted.

This flattens the growth of the space occupied, which would otherwise be
linear in time.

The defaults are configurable.

Consolidation is **irreversible**: the person must be able to understand that
before reducing the retention of the detail.

Reference data is excluded from retention. It describes no moment and does not
age along with the observations.

---

## Consolidated Periods

A daily or monthly aggregate is itself an **observation period**, a longer one.
Everything that holds for periods holds for it: it does not overlap with
others, it is immutable, it can be aggregated.

Every period keeps two further pieces of information, internal to the Storage
and outside the Unified Data Model:

| Information      | Meaning                                                   |
| ---------------- | ----------------------------------------------------------- |
| Tier             | Hour, day or month                                           |
| Hours observed   | How many observed hours went into the period; 1 for an hour  |

The hours observed are necessary to the honesty of the history. A day of which
three hours were observed and a day observed in full have the same shape:
without this figure, the first would look like a quiet day. An hour never
observed stays unobserved after consolidation, and does not become an hour at
zero.

---

## Day And Month Boundaries

Days and months follow the **local time zone of the computer PIE runs on**,
because it is the day of the person reading. A day that begins at two in the
morning is nobody's day.

The instants of beginning and end are still kept in UTC, like every other
instant. On the days the clocks change a day lasts twenty-three or twenty-five
hours: the period says so, because it keeps a beginning and an end rather than
a duration.

Changing the time zone of the computer does not rewrite periods already
consolidated.

---

## When A Level Is Consolidated

A **whole day** or a **whole month** is always consolidated, never a part.

| Operation           | When                                                      |
| ------------------- | ----------------------------------------------------------- |
| Hours → day         | The day ended longer ago than the hourly retention           |
| Days → month        | The month ended longer ago than the daily retention          |
| Deletion of months  | The month ended longer ago than the monthly retention        |

Consolidating a day or a month happens in **a single transaction**: the
aggregate is written, the detail is deleted. An interruption leaves the detail
intact, and the next consolidation picks it up again. Consolidating twice
produces no duplicates, because detail already consolidated no longer exists.

Consolidation is a **scheduled operation**: it runs at startup and then once an
hour, never during a request.

---

## What A Consolidated Period Contains

The principle is the one already applied to the twenty-four hour window: what
adds up is added, and what does not add up is declared for what it is.

| Data                          | Day                                                          | Month                                 |
| ----------------------------- | ------------------------------------------------------------ | ------------------------------------- |
| Query counts                  | Exact sum                                                    | Exact sum                             |
| Distinct domains, active devices | The greater of the highest value of any period and the distinct names or identifiers kept; always a **lower bound** | As for the day |
| DNSSEC state                  | That of the most recent period                               | As for the day                        |
| Devices                       | One per identifier; identity from the most recent period; earliest and latest observation | As for the day |
| Domains                       | One per name; occurrences summed; classification from the most recent period, with the age of the list | As for the day |
| Device → domain activity      | Counts summed per device, domain, outcome and protocol       | **Not kept**                          |
| Source configuration          | That of the most recent period                               | As for the day                        |
| Score                         | The last produced within the day, with its components        | The last produced within the month    |

The quality of every consolidated value is the **least precise** among those of
the periods composing it.

---

## Device Activity Beyond Thirty Days

Activity per device and per domain is the part of the data closest to a
browsing history, even aggregated by the hour.

In daily aggregates it is kept for twelve months, because it allows questions
about a single device to be answered over an interval that is still recent.

In monthly aggregates it is **not kept**. After twelve months it remains known
which domains the network contacted and which devices were present, but no
longer which device contacted which domain. Keeping it for five years would
make it an archive of the browsing of every person in the household, for a use
that no planned feature requires.

This is an editorial choice, approved as such.

---

## Score In Consolidated Periods

A score is neither summed nor averaged: the average of two scores with
different coverage measures nothing.

A consolidated period therefore keeps **the last score produced within it**,
unchanged: value, status, trend, coverage, algorithm version, moment of
production, components.

The score judges the twenty-four hours preceding its production, as
Specification 07 establishes. The last score of a day therefore judges that
day. The last score of a month judges the last day of the month, **not the
month**, and is to be presented as such when the history is shown.

A period in which no score was produced has none. None is computed after the
fact.

---

## Recalculation

Consolidation reduces what can be recomputed. Beyond the hourly retention a
new algorithm can be applied to days, not to hours; beyond the daily
retention, to months, without the activity per device.

It is the declared consequence of keeping the minimum necessary.

---

## Effective Deletion

Detail removed by consolidation must not stay readable in the file.

SQLite, by default, leaves the content of deleted rows in free pages until they
are reused. The Storage enables **secure deletion** (`secure_delete`), which
overwrites that content. The cost is one additional write at the moment of
deletion, negligible at the volumes PIE handles.

The file does not shrink: the space freed is reused by later acquisitions.
Growth flattens; it does not become a reduction.

---

## Retention Configuration

| Parameter                         | Default | Minimum |
| --------------------------------- | ------- | ------- |
| `Storage:Retention:HourlyDays`    | 30      | 2       |
| `Storage:Retention:DailyMonths`   | 12      | 1       |
| `Storage:Retention:MonthlyYears`  | 5       | 1       |

The minimum for hourly retention protects the twenty-four hour window, which
is read from the hourly detail: consolidating a day still within the window
would take from the score and from the pages the data they rest on.

A finer retention **prevails** over a coarser one: a month is neither
consolidated nor deleted while it holds data a finer tier must still keep. An
hourly retention of four hundred days therefore keeps the hours for four
hundred days even with a daily retention of twelve months.

A value below the minimum **prevents startup**, with a message saying which
parameter and which minimum. A value silently corrected would change what gets
deleted without the person knowing.

Reducing a retention takes effect at the next consolidation and cannot be
undone. The documentation for the person says so before explaining how to do
it.

---

# Architectural Placement

Persistence is the responsibility of a Backend component named **Storage**.

```text
Adapter Manager

↓

Unified Data Model

↓

Core

↓

Storage
```

The following constraints hold.

**The Core does not know the Storage.** It produces results and does not know
where they end up, consistently with the constraint that it know neither
sources nor recipients.

**The Unified Data Model contains no element of persistence.** No mapping
attribute, no reference to storage technology, no dependency. Conversion
between the model and storage happens entirely inside the Storage.

**The Query Flow reads from the Storage**, never from the Data Sources.

---

# Technology

## Database

**SQLite**, in a single local file.

The choice is consistent with the Local First and Self Hosted principles: no
additional service to install, no port to expose, no separate process to look
after.

The file lives in a location determined by the configuration, together with
the other application data.

---

## List Files

The domains contained in the classification lists are kept **in files**, not in
the database.

Each list is a file in the `data/lists/` folder, beside the database.

The file keeps the **original format** of the list as downloaded. No
conversion, no normalisation in advance.

The reasons for the separation.

* A list may contain hundreds of thousands of domains, which would grow the
  database by orders of magnitude relative to the observations.
* A text file can be inspected with any editor, while a table requires an SQL
  tool. Specification 08 requires that the person be able to check why a
  domain was classified.
* An update replaces a file, an atomic operation, rather than rewriting
  hundreds of thousands of rows.
* The domains of a list are not observations and have no period: keeping them
  outside the database prevents retention from touching them.

The database keeps the **description** of the list. The file keeps the
**content**.

A description without its corresponding file indicates a list never
downloaded, and is declared as such.

---

## Data Access

Access happens through **explicit SQL**.

Schema and queries stay visible and inspectable, with no layers of implicit
behaviour.

The choice follows the Simplicity and Transparency principles and the
prohibition on introducing unnecessary abstractions.

---

## Dependency

| Item        | Value                                                |
| ----------- | ------------------------------------------------------ |
| Name        | `Microsoft.Data.Sqlite`                                |
| Purpose     | Access to the SQLite database                          |
| Licence     | MIT                                                    |
| Maintenance | Microsoft, part of the .NET ecosystem                  |

It is the first external dependency of the project.

The version is pinned in `Directory.Packages.props`, following the Central
Package Management already adopted.

---

# Schema Management

The database carries a **schema version**, kept inside it.

At startup the system compares the expected version with the one present.

| Condition            | Behaviour                                           |
| -------------------- | ----------------------------------------------------- |
| Versions match       | Normal start                                          |
| Schema older         | Migration, preceded by a backup copy                  |
| Schema newer         | Start refused with an explicit message                |

The last case indicates an attempt to use data produced by a later version of
the software. Carrying on would cause silent corruption.

Migrations are **explicit and ordered**. No migration is generated
automatically from the model.

---

# Backup

The Installation Specification provides for a backup copy before every update.

The copy comprises the database, the configuration and the lists folder.

As these are local files, the copy consists of duplicating them with the
service stopped.

The lists can be downloaded again in any case: their absence from a backup
does not mean any observation is lost.

---

# Privacy

The database contains data about the network activity of the person using PIE.

The following rules apply.

* The content never leaves the device.
* No telemetry is transmitted.
* Retention is configurable and declared to the person.
* The person can delete the data kept.
* Deletion is effective, not a logical marking.

The installation declares which data is kept, where it lives and for how long,
as the Installation Specification provides.

---

# Performance

On a modest device the following priorities hold.

* Writes grouped rather than made per entity.
* Read queries served by explicit indexes.
* Consolidation run as a scheduled operation, not during a request.
* No recomputation of the analysis during the Query Flow.

---

# Design Principles

Persistence follows these principles.

* idempotence by construction;
* immutability of elapsed periods;
* keeping the minimum necessary;
* transparency of the schema;
* independence of the data model from the storage technology.

---

# Constraints

The Storage:

* performs no analysis;
* does not modify the data it receives;
* is not reachable from the Frontend;
* is not known to the Core;
* does not keep the detail of an individual query.

---

# Related Specifications

* 03 - Privacy Intelligence Engine
* 04 - Technitium Integration
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 11 - Backend
* 12 - Installation
* 14 - License
