# 08 - Threat Intelligence

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Threat Intelligence Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the **Threat Intelligence** system of the Privacy
Intelligence Engine.

Threat Intelligence names the subject matter.

The module that implements it is the **Threat Engine**.

The Threat Engine is responsible for identifying, classifying and assessing
the threats detected while analysing data from the Data Sources.

---

# Objectives

The Threat Engine has the following objectives.

* to identify potentially dangerous domains;
* to classify threats;
* to attribute a level of severity;
* to support the computation of the NPSS;
* to generate Alerts;
* to generate Recommendations.

---

# Scope

The Threat Engine analyses only the information present in the Unified Data
Model.

It does not communicate directly with the Data Sources.

It does not handle the user interface.

---

# Threat Classification

Every domain observed is classified into a category.

The categories are the logical level used by the whole PIE ecosystem.

---

# Primary Categories

## Malware

Domains associated with the distribution of malicious software.

---

## Phishing

Domains designed to steal credentials or personal data.

---

## Tracking

Domains used to monitor the activity of users.

---

## Advertising

Domains used to deliver advertising content.

---

## Analytics

Domains used to collect statistics and usage data.

---

## Cryptomining

Domains associated with cryptocurrency mining.

---

## Suspicious

Domains with anomalous behaviour or uncertain reputation.

---

## Social

Domains belonging to social platforms.

---

## Streaming

Domains dedicated to the delivery of media content.

---

## Cloud

Cloud services and distributed infrastructure.

---

## AI Services

Services dedicated to artificial intelligence.

---

## Unknown

The category assigned when the domain cannot be classified.

---

# Threat Severity

Every Threat carries a level of severity.

Levels provided.

* Informational
* Low
* Medium
* High
* Critical

---

# Confidence Level

Every classification carries a level of reliability.

Values provided.

* Low
* Medium
* High

The confidence level makes it possible to distinguish a certain
classification from a probabilistic one.

---

# Threat Sources

Classifications are obtained from:

* local lists, downloaded periodically;
* internal rules;
* correlation algorithms.

Where a classification came from is always recorded.

---

## Local Classification Only

Matching happens **on the device alone**.

The domains contacted by the person's network are **never transmitted to a
third party**, for any purpose, including consulting a reputation service.

The reason is direct: a domain queried reveals what a device was doing.
Consulting an external service to establish whether a domain is dangerous
would mean telling that service the history of the very network being
protected.

A tool that analyses privacy cannot obtain its results by reducing it.

Consequences accepted.

* Threats that appeared recently are recognised with the delay of the list
  update.
* Accuracy depends on the quality of the lists adopted.
* The system works even with no connection to the outside.

The lists are downloaded periodically. The download concerns the lists, never
the domains observed: no information about the person's network leaves the
device on that occasion.

---

# Classification Lists

A classification list associates domains with a category.

---

## List Properties

Every list declares.

| Property        | Meaning                                           |
| --------------- | --------------------------------------------------- |
| `name`          | Name of the list                                     |
| `sourceUrl`     | Address it is downloaded from                        |
| `category`      | Category attributed to the domains it contains       |
| `licence`       | Licence of the list                                  |
| `updatedAt`     | Moment of the last successful update                 |
| `entryCount`    | Number of domains it contains                        |
| `enabled`       | Whether the list takes part in the classification    |

The licence is **mandatory**. A list with no declared licence is not
distributed with the project.

---

## Default Lists

The project adopts only lists whose licence allows the person to download
them.

The default lists come from the **Block List Project**, released into the
public domain under the Unlicense, in the one domain per line format.

| List             | Category       |
| ---------------- | -------------- |
| `ads`            | `Advertising`  |
| `tracking`       | `Tracking`     |
| `malware`        | `Malware`      |
| `phishing`       | `Phishing`     |
| `crypto`         | `Cryptomining` |
| `scam`           | `Suspicious`   |
| `abuse`          | `Suspicious`   |

The research that led to this choice, including the sources examined and
rejected, is documented separately.

### Single Source

The default lists come from **one source only**.

Its mistakes become ours, and its silences become `Unknown`.

The condition does not follow from a preference. Among the freely usable
sources, few are segmented by category, and the few that are feed one another:
adopting two would give the appearance of independent opinions without being
so, and an apparent agreement is worse than a declared dependency.

The person may add sources of their own at any time.

### Categories Without A Source

The categories `Analytics`, `Social`, `Streaming`, `Cloud` and `AI Services`
have no default list.

The domains that would belong to them stay `Unknown`.

No category is attributed to fill a gap.

---

## User Lists

The person may add, disable and remove lists.

This being self-hosted software, the choice of sources belongs to whoever uses
it.

---

# Matching

Matching happens **on the device alone**, comparing the domains observed with
the lists held locally.

---

## Matching Rule

The comparison proceeds from the full name upwards, dropping one label at a
time.

```text
tracker.ads.example.com
        ads.example.com
            example.com
```

The search stops at the first level that produces a match. If several lists
match at that level, the outcome is determined by the Competing
Classifications section.

A domain listed in a list is taken to include its own subdomains: it is the
convention the lists themselves adopt, and ignoring it would make the
classification ineffective.

---

## Confidence

The level of confidence depends on **how** the match was obtained.

| Match                                | Confidence |
| ------------------------------------ | ---------- |
| Full name present in a list           | `High`     |
| Match on a parent domain              | `Medium`   |

The second case is an inference: the list asserts something about the parent
domain, and the system extends the assertion to the subdomain observed.

The inference is reasonable and remains an inference. Declaring it with a
lower confidence lets the person tell it apart from a direct statement.

---

## Competing Classifications

A domain may appear in **several lists under different categories**. An
advertising domain that also tracks the person legitimately belongs to both.

The match is resolved in three steps, applied in this order.

### 1. The nearest name wins

A match on the full name prevails over a match obtained by walking upwards,
**whatever the category**.

Specificity is a stronger signal than severity: a list naming
`analytics.example.com` is saying something about that name, while a list
naming `example.com` is saying something about the parent domain. Letting the
second prevail would replace a direct statement with an inference.

### 2. At equal distance, the graver category wins

The order of severity is as follows.

| Order | Category       | Nature                      |
| ----- | -------------- | --------------------------- |
| 1     | `Malware`      | Security                     |
| 2     | `Phishing`     | Security                     |
| 3     | `Cryptomining` | Security                     |
| 4     | `Suspicious`   | Security, unconfirmed        |
| 5     | `Tracking`     | Privacy                      |
| 6     | `Analytics`    | Privacy                      |
| 7     | `Advertising`  | Privacy                      |
| 8     | `Social`       | Descriptive                  |
| 9     | `Streaming`    | Descriptive                  |
| 10    | `Cloud`        | Descriptive                  |
| 11    | `AI Services`  | Descriptive                  |
| 12    | `Unknown`      | Absence of classification    |

The order is a **declared editorial judgement**, like the weights of the
Network Privacy & Security Score. It does not derive from a measurement and
does not pretend to.

The reasons.

* Security comes before privacy. A domain that tracks and distributes malware
  is to be presented as a threat, not as a nuisance.
* `Suspicious` comes before the privacy categories because it signals a
  possible danger the person can act on, while `Tracking` signals a certain
  behaviour of lesser gravity.
* Among the privacy categories, `Tracking` comes before `Analytics`, which
  comes before `Advertising`: the first concerns the person, the last the
  content.
* The descriptive categories express no judgement and yield to any category
  that does.

### 3. At equal severity, the more recent list wins

When two lists of the same category claim the same name, the one updated more
recently prevails.

A list never updated yields to any list that was. Between two lists equivalent
on this too, the one whose name comes first alphabetically prevails, so that
the same set of lists always produces the same result.

### What Is Lost

The system shows **one category only**.

The other categories the domain appears in are not presented.

The loss is real and is declared. The classification shown is the gravest
among those found, not the only one found, and the interface must not suggest
otherwise.

Representing every category of a domain requires a change to the Unified Data
Model and remains an open possibility, not a decision taken.

---

## Unknown Domains

A domain that appears in no list receives the category `Unknown`.

`Unknown` means **not classified**, not harmless.

The interface never presents an unclassified domain as safe: that would be a
statement the system has not verified.

---

# Freshness

Every classification declares **the age of the list it comes from**.

Local classification entails a delay in recognising threats that appeared
recently. The delay is not hidden: it is measured and declared, like the
coverage of the score and the quality of the measurements.

A classification produced from a list updated six days earlier is a different
piece of information from one produced the same day, and the system tells them
apart.

---

# Update Policy

The lists are updated at a configurable interval, **twenty-four hours** by
default.

A list is downloaded when it has never been downloaded, or when the one held
is older than the interval. Downloading at every start would burden the source
without telling the person anything new.

---

## Failure Handling

A failed update **does not invalidate the existing list**.

The system carries on using the version held and declares its growing age.

A list that cannot be updated is less useful than a recent one and more useful
than none.

---

## Offline Operation

With no connectivity the system carries on classifying with the lists held.

No function of the analysis depends on the availability of the network: the
only consequence of having no connection is the ageing of the lists, declared
to the person.

---

## Storage

The lists are held locally in an inspectable form.

The content of each list lives in a **text file**, in the format it was
downloaded in. The description of the list lives in the database.

The person can therefore open a list with any editor, check which domains the
system considers to belong to a category, and understand the reason for a
classification rather than having to accept it.

Where the files live is defined by the Persistence Specification.

---

# Threat Lifecycle

Every Threat goes through a life cycle.

```text id="zq54ga"
Detected

↓

Classified

↓

Evaluated

↓

Alert Generated

↓

Recommendation Generated

↓

Archived
```

---

# Correlation

The Threat Engine may correlate events coming from different Data Sources.

Correlation makes it possible to improve the precision of the classification.

---

# Domain Reputation

For every domain the system maintains a reputation index.

The reputation contributes to the classification of the threat.

---

# Threat History

Every Threat keeps its own history.

Information recorded.

* first detection;
* latest detection;
* number of occurrences;
* current state.

---

# Alert Generation

The Threat Engine may generate Alerts when significant conditions are
detected.

The severity of the Alert depends on the gravity of the threat.

---

# Recommendation Generation

Every Threat may produce one or more Recommendations.

Each suggestion is linked to the threat that generated it.

---

# NPSS Integration

The Threat Engine contributes directly to the computation of the Network
Privacy & Security Score.

The presence of critical Threats lowers the overall score.

---

# Extensibility

New categories can be added while keeping compatibility with the existing
model.

Classifications already made are not modified.

---

# Design Principles

The Threat Engine follows these principles.

* uniformity;
* modularity;
* transparency;
* independence from the backend;
* extensibility;
* reproducibility.

---

# Constraints

The Threat Engine:

* does not modify the original data;
* does not communicate directly with the Frontend;
* uses only the Unified Data Model;
* does not depend on any specific Data Source.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 07 - Network Privacy & Security Score
* 09 - Network Privacy
