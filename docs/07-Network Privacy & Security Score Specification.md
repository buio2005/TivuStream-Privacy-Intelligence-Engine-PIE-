# 07 - Network Privacy & Security Score

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Network Privacy & Security Score (NPSS) Specification

**Version:** 4.0.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the **Network Privacy & Security Score (NPSS)**,
the principal indicator produced by the Privacy Intelligence Engine.

The NPSS summarises the overall state of the network in a single number
obtained from the analysis of the data processed by the Core.

---

# Objectives

The NPSS must be:

* simple to understand;
* coherent;
* reproducible;
* transparent;
* independent of the backend;
* updated automatically.

---

# Score Range

The score uses a scale from **0** to **100**.

| Score    | Status    |
| -------- | --------- |
| 90 – 100 | Excellent |
| 75 – 89  | Good      |
| 60 – 74  | Fair      |
| 40 – 59  | Warning   |
| 0 – 39   | Critical  |

---

# Score Components

The score is made up of different areas of evaluation.

```text id="fvc3pk"
Network Privacy & Security Score

├── DNS Security
├── Privacy Protection
├── Threat Protection
├── Device Health
├── Configuration
└── Network Integrity
```

---

# Measurement and Judgement

The NPSS is made of two elements of different nature, and the distinction is
not formal.

**The breakdown is measurement.** Every area reports what was observed, with
the factors that determined it. It is verifiable: anyone holding the same data
obtains the same values.

**The overall score is judgement.** Aggregating different areas into a single
number requires establishing how much each counts, and that choice does not
follow from the data.

The weights defined in this specification are a **declared editorial position
of the project**, not a measured truth.

They are motivated, versioned along with the algorithm, and open to revision.
They do not derive from an industry standard, because none exists for this
kind of evaluation.

Declaring it is part of the Transparency principle: a judgement presented as a
measurement is a false measurement.

---

# Evaluation Window

The score judges the network over the **last twenty-four hours**, the same
window as domains, devices and statistics (API Specification, Observed
Period).

Up to version 3 it judged the current hour alone. The result emptied at every
turn of the clock: at five past three in the morning, with the network idle,
privacy and threats became not measurable, coverage fell below the minimum and
the score disappeared until morning. One page said "here are the domains of
the last twenty-four hours", the other "I do not have enough data".

| Data | Where it comes from |
| --- | --- |
| Traffic: total, failed and encrypted queries | Sum of the periods in the window |
| Domains and their classification | The domains of the window, aggregated as in `/domains`: occurrences summed, classification from the most recent period |
| Activity per device, for the blocking of risky domains | The activity of the window, summed per device, domain, outcome and transport |
| Configuration of the service | The most recent acquisition: it is the present state, not traffic |
| Continuity of observation | Unchanged: periods observed against those expected in the twenty-four hours |

The Minimum Observation threshold, one hundred queries, now applies to the
window and not to the hour. A household that uses the network a few times a
day reaches the threshold; a network observed for a few minutes still does
not.

The score is recomputed at every acquisition, as before. Every value kept in
the history describes the twenty-four hours ending with the period in which it
was produced.

**Trend.** It is compared only with a score produced by the same version of
the algorithm, as well as with the same coverage. A score from version 3,
computed over an hour, is not comparable with one from version 4, computed
over a day.

---

# Score Weights

Every area contributes to the overall score with a defined weight.

The weights always sum to **100**.

| Component          | Weight |
| ------------------ | ------ |
| Threat Protection  | 25     |
| DNS Security       | 20     |
| Privacy Protection | 20     |
| Device Health      | 15     |
| Configuration      | 10     |
| Network Integrity  | 10     |
| **Total**          | **100**|

The reason for each weight.

| Area                | Reason for the position adopted                                                                       |
| ------------------- | ------------------------------------------------------------------------------------------------------ |
| Threat Protection   | An active threat does immediate and concrete harm, greater than any defect of configuration             |
| DNS Security        | It determines the quality of everything that passes through, whatever threats are present               |
| Privacy Protection  | The declared subject of the project, but of gradual rather than immediate impact                        |
| Device Health       | It attributes problems to devices, valuable information but subsequent to their detection               |
| Configuration       | A precondition of the other areas more than a value of its own                                          |
| Network Integrity   | It concerns the reliability of the observation, not the state of the network observed                   |

The weights belong to the NPSS algorithm and follow its versioning.

Changing them is a substantial change and increments the Major Version of the
algorithm, because it makes earlier scores non-comparable.

---

# Factor Codes

Every area reports the factors that determined its score.

A factor is a **code with its values**, not a sentence.

The Core produces results, not prose. The wording belongs to the interface,
which renders it in the language the person chose, under the constraints of
the Network Privacy Specification.

---

## DNS Security

| Code                            | Values                | Meaning                                                  |
| ------------------------------- | --------------------- | -------------------------------------------------------- |
| `ConfigurationUnavailable`      | —                     | The source does not provide its configuration             |
| `DnssecValidationEnabled`       | —                     | DNSSEC validation is on                                   |
| `DnssecValidationDisabled`      | —                     | DNSSEC validation is off                                  |
| `EncryptedTransportsAvailable`  | `transports`          | Encrypted transports enabled                              |
| `EncryptedTransportsAbsent`     | —                     | No encrypted transport enabled                            |
| `QueryMinimisationEnabled`      | —                     | Query name minimisation is on                             |
| `QueryMinimisationDisabled`     | —                     | Minimisation is off                                       |
| `ClientSubnetForwardingEnabled` | —                     | Client subnet forwarding is on                            |
| `ClientSubnetForwardingDisabled`| —                     | Client subnet forwarding is off                           |
| `EncryptedQueryShare`           | `share`               | Share of queries received over an encrypted transport     |
| `FailedQueryShare`              | `share`               | Share of queries not satisfied                            |
| `NoTrafficObserved`             | —                     | No traffic in the period                                  |

---

## Privacy Protection

| Code                              | Values               | Meaning                                             |
| --------------------------------- | -------------------- | ---------------------------------------------------- |
| `ClassificationUnavailable`       | —                    | No list available                                    |
| `ObservationInsufficient`         | `queries`, `minimum` | Queries below the minimum threshold                  |
| `TrackingExposureNone`            | —                    | No **known** tracking observed                       |
| `TrackingExposureMeasured`        | `share`              | Share of queries towards privacy domains             |
| `TrackingBlockingMeasured`        | `share`, `queries`   | Blocked share of those queries                       |
| `TrackingBlockingUntested`        | —                    | No query to block                                    |
| `DomainActivityUnavailable`       | —                    | The source does not report activity per domain       |

---

## Threat Protection

| Code                           | Values                    | Meaning                                       |
| ------------------------------ | ------------------------- | ---------------------------------------------- |
| `ClassificationUnavailable`    | —                         | No list available                              |
| `ObservationInsufficient`      | `queries`, `minimum`      | Queries below the minimum threshold            |
| `ThreatExposureNone`           | —                         | No **known** threat and nothing suspicious     |
| `ThreatExposureSuspiciousOnly` | `suspicious`              | Only suspicious domains, nothing confirmed     |
| `ThreatExposureMeasured`       | `confirmed`, `suspicious` | Confirmed and suspicious threats observed      |
| `ThreatBlockingMeasured`       | `share`, `queries`        | Blocked share of the queries at risk           |
| `ThreatBlockingUntested`       | —                         | No query to block                              |
| `DomainActivityUnavailable`    | —                         | The source does not report activity per domain |

---

## Device Health

| Code                    | Values | Meaning                                       |
| ----------------------- | ------ | --------------------------------------------- |
| `EnginesNotImplemented` | —      | The Device Engine and Alert Engine do not exist |

---

## Configuration

| Code                         | Values  | Meaning                                             |
| ---------------------------- | ------- | ---------------------------------------------------- |
| `SourceReachable`            | —       | The source answered                                  |
| `SourceUnreachable`          | —       | The source cannot be reached                         |
| `ConfigurationUnavailable`   | —       | The source does not provide its configuration        |
| `FilteringEnabled`           | —       | Domain filtering is on                               |
| `FilteringDisabled`          | —       | Domain filtering is off                              |
| `FilterListsConfigured`      | `count` | Filter lists configured                              |
| `FilterListsAbsent`          | —       | No list: filtering has no effect                     |

---

## Network Integrity

| Code                            | Values                  | Meaning                                             |
| ------------------------------- | ----------------------- | ---------------------------------------------------- |
| `ObservationContinuity`         | `observed`, `expected`  | Periods observed against those expected              |
| `ObservationContinuityUnknown`  | —                       | No period was expected, so nothing can be judged     |
| `AcquisitionReliabilityUnknown` | —                       | Acquisition attempts are not recorded                |

---

## Stability

A code, once published, **does not change meaning**.

Altering the sense of an existing code would change the sentences shown by
interfaces already written, with nothing to signal it.

A factor whose meaning changes receives a new code.

---

# Indicator Definitions

The indicators of each area are defined in a way that is **computable and
verifiable**.

An indicator described by a title alone is unusable: it would make the score
depend on the interpretation of whoever writes the code, contrary to the
Transparency principle, which requires every judgement to be explainable.

Every indicator declares the data it derives from. An indicator whose data is
not available is **not measurable**, and its share of the weight is excluded
from the calculation.

---

# DNS Security

Assesses the configuration of the DNS service.

Total weight **20**, distributed over four indicators of **5** points each.

---

## DNSSEC Validation

DNSSEC validation protects against tampering with the answers.

| Condition              | Points |
| ---------------------- | ------ |
| Validation on          | 5      |
| Validation off         | 0      |

Data: configuration of the Data Source.

---

## Transport Encryption

Assesses both the availability of encrypted transports and their actual use.

| Component                                              | Points |
| ------------------------------------------------------ | ------ |
| At least one encrypted transport enabled                | 2      |
| Share of queries received over an encrypted transport   | 3      |

The second component is proportional to the share observed.

The distinction is deliberate: a service may offer encrypted transports
without any device using them. Measuring availability alone would reward an
intention; measuring use alone would ignore a correct configuration.

Data: configuration of the Data Source and statistics per protocol.

**Declared limit.** The indicator measures the transport between the devices
and the local DNS server, not between the server and external resolvers.

---

## Resolver Configuration

Assesses the resolver settings that bear on privacy.

| Component                                      | Points |
| ---------------------------------------------- | ------ |
| Query name minimisation on                      | 2.5    |
| Client subnet forwarding off                    | 2.5    |

Minimisation reduces the information sent to authoritative servers.

Client subnet forwarding tells external servers which part of the network a
query came from: it is an optimisation feature that reduces privacy, so the
score rewards its **absence**.

Data: configuration of the Data Source.

---

## DNS Errors

Assesses the share of queries the service could not satisfy.

```text
points = 5 × ( 1 − failed queries / total queries )
```

Answers of non-existent domain do not count towards failed queries, being
correct answers.

With no traffic the indicator is **not measurable**: there is nothing to pass
judgement on.

Data: statistics.

---

## Removed Indicator

The previous version listed an indicator named **Query Validation**, with no
definition distinct from DNSSEC Validation.

It was removed rather than reinterpreted. An indicator without a meaning of
its own would have produced arbitrary points.

---

# Classification-Based Areas

Privacy Protection and Threat Protection both derive from the classification
of domains.

They share two conditions.

---

## Minimum Observation

Below **one hundred queries** in the evaluation window (see Evaluation
Window), the indicators of these two areas are **not measurable**.

A network that contacted no tracking domain in three queries is not a
protected network: it is a network that was not observed enough.

Without this condition the least used network would obtain the best score, and
the score would be measuring silence rather than protection.

---

## What Classification Can And Cannot Assert

Lists assert only in the positive: they say that a domain tracks, not that it
does not.

A domain absent from every list is `Unknown`, and `Unknown` covers both
harmless domains and trackers no list knows about.

An asymmetry follows, and it governs the wording of these indicators.

| Observation | Reliability |
| ----------- | ------------ |
| High exposure | **Reliable**: those domains are known to track |
| No exposure   | **Not reliable as an acquittal**: it may mean a clean network or unknown trackers |

The calculation is symmetric, the wording is not. Full marks mean **no known
tracking**, never *no tracking*, and the Network Privacy Specification binds
the interface to say it that way.

The value of the exposure is always declared as a **lower bound**.

---

# Privacy Protection

Total weight **20**, distributed over two indicators of **10** points each.

The categories considered are `Tracking`, `Analytics` and `Advertising`.

The two indicators answer two different questions the person asks equally:
*how much am I tracked* and *how much is it prevented*. The second alone would
reward an effective filter on a besieged network; the first alone would ignore
the work the filter does.

---

## Known Tracking Exposure

The share of queries directed at domains classified in a privacy category, out
of the total number of queries.

The count is **per query, not per domain**. Ten tracking domains contacted once
each and a single domain contacted four hundred times describe different
networks, and counting domains would make them look alike.

| Share observed  | Points |
| --------------- | ------ |
| None            | 10     |
| Up to 2%        | 8      |
| Up to 5%        | 6      |
| Up to 10%       | 4      |
| Up to 20%       | 2      |
| Above 20%       | 0      |

Quality of the measurement: **lower bound**.

Not measurable when there are fewer than one hundred queries, or when no list
is available.

Data: category and occurrences of the domains, statistics.

---

## Tracking Blocking

The share of queries directed at those domains that were blocked.

| Share blocked   | Points |
| --------------- | ------ |
| At least 99%    | 10     |
| At least 90%    | 8      |
| At least 75%    | 6      |
| At least 50%    | 4      |
| At least 25%    | 2      |
| Below           | 0      |

Not measurable when no query is directed at those domains, and when the Data
Source does not provide activity per domain.

**The absence of tracking does not produce a double judgement.** If there is
nothing to block, the indicator is excluded and the full marks come from the
exposure. Coverage falls below 100, and rightly: that filter was not put to
the test.

Data: activity per domain, category of the domains.

---

## Removed Indicators

The previous version listed **Tracker Blocking**, **Analytics Detection**,
**Advertising Domains**, **Telemetry Detection** and **Privacy Configuration**
as titles alone.

The first four described the same measurement split by category, without the
categories carrying distinct, motivated weights. They have merged into the two
indicators above.

**Privacy Configuration** was removed because every setting it might have
measured is already assessed by Resolver Configuration and by Filtering
Configuration. Counting it again would have inflated the score twice for the
same fact.

**A note on the `Analytics` category.** No default list provides it: analytics
domains receive in practice the category `Tracking`, according to what the
adopted source declares. The category stays in the model and is not attributed
for convenience.

---

# Threat Protection

Total weight **25**, distributed over two indicators.

The categories considered are `Malware`, `Phishing` and `Cryptomining`, which
are threats **confirmed** by a list, and `Suspicious`, which is an
**unconfirmed** report.

The structure mirrors that of Privacy Protection, with two motivated
differences.

**Exposure is counted per domain, not as a share.** A malware domain contacted
once is a relevant fact; diluting it over the total number of queries would
make it disappear. For tracking the proportion is informative, for a threat
the absolute number is more so.

**The bar for blocking is higher.** A tracker that gets through costs privacy;
a malware domain that gets through can cost the machine.

---

## Known Threat Exposure

Weight **12**.

| Condition observed                                        | Points |
| --------------------------------------------------------- | ------ |
| No threat domain, no suspicious domain                     | 12     |
| No confirmed threat, at least one suspicious domain        | 9      |
| One confirmed threat domain                                | 6      |
| Two to five confirmed threat domains                       | 3      |
| More than five                                             | 0      |

A suspicious domain lowers the score without emptying it: the report is not
confirmed, and treating it as an established threat would attribute to the
network a problem that has not been shown.

Quality of the measurement: **lower bound**.

Not measurable when there are fewer than one hundred queries, or when no list
is available.

Data: category of the domains, statistics.

---

## Threat Blocking

Weight **13**.

The share of queries directed at threat domains, confirmed or suspicious, that
were blocked.

| Share blocked   | Points |
| --------------- | ------ |
| All of them     | 13     |
| At least 95%    | 10     |
| At least 80%    | 6      |
| At least 50%    | 3      |
| Below           | 0      |

Not measurable when no query is directed at those domains, and when the Data
Source does not provide activity per domain.

Data: activity per domain, category of the domains.

---

## Removed Indicator

The previous version listed **Threat Intelligence** among the indicators.

It is not an indicator: it is the name of the subsystem that produces the
classification every other indicator derives from.

It was removed rather than reinterpreted.

---

# Device Health

Analyses the behaviour of devices.

Indicators.

* anomalous activity;
* number of Alerts;
* DNS traffic;
* general behaviour.

---

# Configuration

Assesses the quality of the overall configuration.

Total weight **10**, distributed over two indicators of **5** points each.

---

## Source Availability

| Condition                               | Points |
| --------------------------------------- | ------ |
| The Data Source answered correctly       | 5      |
| The Data Source cannot be reached        | 0      |

Data: state of the Data Source.

---

## Filtering Configuration

| Component                             | Points |
| ------------------------------------- | ------ |
| Filtering on                           | 2.5    |
| At least one filter list configured    | 2.5    |

Filtering on with no list configured has no effect: the two components are
kept distinct because they describe different conditions.

Data: configuration of the Data Source.

---

## Redefined Indicators

The previous version listed four indicators: valid configuration, services
available, synchronisation, operational state.

They overlapped with one another and had no criterion. They were replaced by
two indicators defined in a verifiable way.

---

# Network Integrity

Assesses the continuity and the reliability of the observation of the network.

Total weight **10**, distributed over two indicators of **5** points each.

---

## Observation Continuity

Assesses how many of the expected observation periods were actually observed.

```text
points = 5 × ( periods observed / periods expected )
```

The default reference interval is twenty-four hours.

A missing period indicates that the system could not observe the network in
that span of time, and therefore that the analysis has a gap.

**Nothing is expected before the first observation.** Expected periods run
from the first observation recorded, never from before it.

A recent installation would otherwise be penalised for not having observed the
network before it existed, which says nothing about the network and would
attribute to the person a gap that is not theirs.

Data: periods kept.

---

## Acquisition Reliability

Assesses the share of acquisition attempts that succeeded.

It requires attempts to be recorded, including the failed ones.

Until that recording exists, the indicator is **not measurable**.

---

## Redefined Indicators

The previous version listed errors, availability, consistency and stability.

The first duplicated the DNS Errors indicator; the others had no definition.
They were replaced by two indicators referring to the continuity of the
observation, which is what this area can actually measure.

---

# Score Breakdown

The system keeps the detail of the score.

Every component contributes to the final result according to its weight.

The breakdown covers all six areas of evaluation.

Example.

```text id="c8qqdc"
Overall Score

92 /100

Threat Protection

25 /25

DNS Security

19 /20

Privacy Protection

18 /20

Device Health

12 /15

Configuration

9 /10

Network Integrity

9 /10
```

For every area the system also keeps the list of the factors that determined
the score.

This detail is the minimum requirement for satisfying the Transparency
principle.

---

# Measurement States

Not every Data Source provides the data needed to assess every indicator.

Every area of evaluation is therefore in one of three states.

| State               | Condition                                          |
| ------------------- | ---------------------------------------------------- |
| `Measured`          | Every indicator of the area can be assessed           |
| `PartiallyMeasured` | Only some of the indicators can be assessed           |
| `NotMeasurable`     | No indicator of the area can be assessed              |

---

## Principle

What was not observed is **excluded** from the calculation.

It does not receive a score of zero, because zero is a statement about the
state of the network: it would declare a critical condition the system has not
verified.

Neither does it receive an estimated or presumed favourable value, because
that would be an equally unfounded statement.

The unobserved portion simply does not enter the count, neither in the
numerator nor in the denominator.

It follows that **the absence of data can in no case improve the score**.

---

## Partial Measurement

When an area is partly measurable, the obtainable score is proportional to the
share of indicators assessed.

```text
maxScore = weight × ( indicators assessed / total indicators of the area )
```

The indicators of one area carry equal weight among themselves, unless this
specification states otherwise explicitly.

This convention makes the calculation deterministic and verifiable, and can be
extended by assigning specific weights to individual indicators in future.

---

## Calculation

```text
overallScore = ( sum of the points obtained
                 / sum of the maxScore ) × 100
```

The resulting value keeps the scale from 0 to 100 and its correspondence with
the Score Range table.

---

## Minimum Coverage

The overall score **is not produced** when coverage is below **60**.

Below that level the summary judgement would rest on less than three fifths of
the evaluation system, and a single number would communicate a completeness
that does not exist.

In that condition the system presents the **breakdown alone**, with the areas
measured, those partial and those not measurable, each with its reason.

It is not a malfunction and is not to be presented as one: the system is
declaring that it does not hold enough to support an overall judgement, while
still having valid measurements to show.

The threshold belongs to the algorithm and follows its versioning.

---

## Coverage

Coverage is the sum of the `maxScore` of every area, out of a maximum of 100.

It represents the portion of the evaluation system actually observed.

An example, for a Data Source that does not provide Domain Activity.

The Device Health area has four indicators. Only one, the volume of DNS
traffic, can be assessed without Domain Activity.

```text
maxScore of Device Health = 15 × ( 1 / 4 ) = 3.75
```

```text
Overall Score

90 /100

Coverage

88.75 /100

Areas measured

DNS Security          19    /20
Privacy Protection    18    /20
Threat Protection     22    /25
Configuration          9    /10
Network Integrity      9    /10

Area partly measured

Device Health          3    /3.75
   Assessed     : volume of DNS traffic
   Not assessed : anomalous activity, number of Alerts, general behaviour
   Reason       : the Data Source does not provide Domain Activity
```

The score derives from 80 points obtained out of 88.75 observable points.

Compared with an evaluation that had declared the whole area not measurable,
coverage rises from 85 to 88.75: the system acknowledges the data it actually
holds, without claiming what it does not.

Note that acknowledging the partial measurement **did not improve the score**,
which fell from 91 to 90. The additional data increased the portion observed
and contributed its own actual value, which was below the average of the other
areas.

This is the expected behaviour: partial measurement increases knowledge, not
the result.

---

## Comparability

Two scores with different coverage **are not comparable**.

The system must always present the coverage beside the score whenever it is
below 100.

Historical comparison of the trend is admitted only between readings with the
same coverage. A change of coverage breaks the series and must be signalled.

---

## Recommendation

Every area that is not measurable, or measured only in part, produces a
Recommendation.

The Recommendation states:

* which area cannot be assessed;
* which data is missing;
* what would make the data available;
* what consequences that would entail.

The last point is binding: if making a piece of data available carries a cost,
that cost is declared alongside the benefit.

The system does not propose configurations to the person by listing only their
advantages.

---

# Score History

Every update of the NPSS is stored.

The history allows:

* comparisons over time;
* trend analysis;
* periodic reports.

---

# Score Trend

The system computes the change in the score automatically.

States provided.

* Improving
* Stable
* Decreasing

---

# Positive Factors

The score rises when correct configurations are detected.

Examples.

* DNSSEC on;
* encrypted DNS;
* absence of malware;
* blocklists up to date;
* valid configuration.

---

# Negative Factors

The score falls when critical conditions are detected.

Examples.

* malware;
* phishing;
* DNSSEC off;
* incorrect configurations;
* critical Alerts.

---

# Transparency

Every change in the score must be explainable.

The system keeps the detail of the indicators that contributed to the result.

---

# Backend Independence

The NPSS algorithm uses only the Unified Data Model.

It holds no direct dependency on any backend.

---

# Versioning

The algorithm carries a version of its own, independent of others.

Substantial changes increment the Major Version.

---

# Design Principles

The NPSS follows these principles.

* simplicity;
* transparency;
* uniformity;
* reproducibility;
* independence;
* continuous updating.

---

# Constraints

The NPSS:

* is not a security certification;
* does not measure privacy alone;
* does not depend on any specific technology;
* does not assign a score of zero to what was not measured;
* is not comparable between readings with different coverage;
* is a summary indicator produced by the Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
* 08 - Threat Intelligence
* 09 - Network Privacy
