# 15 - Technitium API Reconnaissance

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Technitium API Reconnaissance

**Version:** 1.3.0

**Status:** Analysis — Not Approved

**Last Updated:** 2026-08-02

---

# Nature of This Document

This document **is not a Specification**.

It is the result of the reconnaissance carried out in milestone M3.1 on the
public APIs of Technitium DNS Server.

It defines no architecture, modifies no part of the Unified Data Model and
introduces no requirement.

Its purpose is to provide the factual basis needed to design the Adapter
contract and to update, where necessary, the Technitium Integration
Specification.

**It is a record of what was observed on 2026-08-02, and it is kept as it was
written.** The Open Points it raises were decided afterwards, in the
Technitium Integration Specification and in the Data Model Specification;
those documents, not this one, say what the project does today. Only the
language of this document has been changed.

---

# Verification Status

The information is classified as follows.

| Level | Meaning |
| --- | --- |
| **Confirmed** | Verified against the official documentation of the APIs |
| **To be verified** | Not retrieved, or inferred; requires confirmation on a real instance |

Two sources were used.

1. The official documentation `APIDOCS.md` of the repository of the project.
   Its retrieval stopped before the Settings, Blocked Zones, Cache and Logs
   sections.
2. **A real instance** of Technitium DNS Server **version 15.4**, running in a
   container and questioned directly. This verification covered exactly the
   areas missing from the documentation.

Eighteen endpoints were questioned: seventeen answered, one failed through a
network dependency.

The information derived from the real instance is the most reliable, because
it describes the actual behaviour of the product and not its description.

---

# Authentication

**Confirmed.**

From version 15.0 the APIs require a bearer token.

```text
Authorization: Bearer <token>
```

Passing the token as a `token` parameter in the query string or in form data
remains supported for backward compatibility.

---

## Session Token

```text
GET /api/user/login?user=<user>&pass=<pass>&includeInfo=true
```

Returns a session token that **expires** after the inactivity timeout of the
user, by default 30 minutes.

With `includeInfo=true` the answer also includes the version of the server,
the domain and the permissions of the user.

---

## API Token

```text
GET /api/user/createToken?user=<user>&pass=<pass>&tokenName=<name>
```

Returns a **non-expiring** token, intended exactly for automation.

Its characteristics are relevant to PIE.

* It requires no periodic renewal, so the Acquisition Flow does not have to
  manage the life cycle of the session.
* It inherits the permissions of the user who created it.
* It does not allow the password or the profile of the user to be modified.

The official documentation recommends creating a dedicated user with limited
permissions.

For PIE the **Dashboard: View** permission is enough.

This recommendation directly satisfies the constraint of Specification 04 that
credentials stay confined to the Adapter, and reduces the impact of any
compromise.

---

## Response Format

**Confirmed.**

Every answer is JSON and contains the `status` property.

| Value | Meaning |
| --- | --- |
| `ok` | Call succeeded |
| `error` | Error, with `errorMessage` and debug detail |
| `invalid-token` | Session expired or token not valid |
| `2fa-required` | Two-factor authentication required |

A note relevant to the Adapter: **the outcome is not expressed by the HTTP
status code** but by the body of the answer. An application error can arrive
with HTTP 200.

Error answers include `stackTrace` and `innerErrorMessage`, which must not be
propagated beyond the Adapter nor end up in the logs, consistently with the
rules on recording sensitive information.

---

# Server Information

**Confirmed.**

The version of the server is obtained from the answer of `login` with
`includeInfo=true`, in the `info.version` field.

```text
GET /api/dashboard/metrics/json
```

Returns `uptimestamp`, `uptimeSeconds` and the lifetime counters of the
server.

The official documentation marks this call as **experimental and subject to
change**. It is not to be used as a primary dependency.

---

# Statistics

**Confirmed.**

```text
GET /api/dashboard/stats/get?type=LastHour&utc=true
```

Main parameters.

* `type`: `LastHour`, `LastDay`, `LastWeek`, `LastMonth`, `LastYear`, `Custom`
* `start` and `end`: ISO 8601 dates, only with `Custom`
* `utc`: returns the time labels in UTC

Fields returned in the `stats` object.

```text
totalQueries, totalNoError, totalServerFailure, totalNxDomain,
totalRefused, totalAuthoritative, totalRecursive, totalCached,
totalBlocked, totalDropped, totalClients,
zones, cachedEntries, allowedZones, blockedZones,
allowListZones, blockListZones
```

The answer also contains `protocolTypeChartData`, `queryTypeChartData` and
reduced versions of `topClients`, `topDomains` and `topBlockedDomains`.

---

# Top Statistics

**Confirmed.**

```text
GET /api/dashboard/stats/getTop?type=LastHour&statsType=TopClients&limit=1000
```

* `statsType`: `TopClients`, `TopDomains`, `TopBlockedDomains`
* `limit`: default 1000
* `noReverseLookup`: turns off the reverse resolution of the clients

Structure of a `topClients` element.

```json
{
  "name": "192.168.10.5",
  "domain": "server1.home",
  "hits": 463,
  "rateLimited": false
}
```

The `domain` field is the result of a reverse resolution and **may be
absent**.

Structure of a `topDomains` and `topBlockedDomains` element.

```json
{
  "name": "edge.microsoft.com",
  "hits": 52
}
```

---

# Query Logs Application

The **Query Logs (Sqlite)** app version 9.1.1 was installed on the test
instance and verified.

Its `classPath` is `QueryLogsSqlite.App`.

This verification was necessary because the possibility of building
`DomainActivity` depends on it.

---

## Installation

**Confirmed.**

```text
GET /api/apps/downloadAndInstall?name=<name>&url=<url>
```

The installation is entirely automatable through the API. The URL of the
package comes from `apps/listStoreApps`.

The download is about 16 MB and requires connectivity towards the outside.

---

## Query API

**Confirmed.**

```text
GET /api/logs/query?name=<app name>&classPath=QueryLogsSqlite.App&...
```

The answer has the following structure.

```json
{
  "pageNumber": 1,
  "totalPages": 4,
  "totalEntries": 90,
  "entries": [ ... ]
}
```

---

## Entry Structure

**Confirmed.**

```json
{
  "rowNumber": 90,
  "timestamp": "2026-08-02T16:39:44.7068085Z",
  "clientIpAddress": "127.0.0.1",
  "protocol": "Udp",
  "responseType": "Blocked",
  "rcode": "NxDomain",
  "qname": "blocked-test.example",
  "qtype": "AAAA",
  "qclass": "IN",
  "answer": null
}
```

Every entry contains the client and the domain at the same time: it is the
correlation the dashboard APIs do not supply.

---

## Supported Filters

**Every one verified and working.**

| Parameter | Outcome | Verification |
| --- | --- | --- |
| `pageNumber` | ✔ | pagination with `totalPages` and `totalEntries` |
| `entriesPerPage` | ✔ | up to 1000 entries in a single answer |
| `descendingOrder` | ✔ | ordering |
| `clientIpAddress` | ✔ | filter by device |
| `qname` | ✔ | filter by domain, 6 entries out of 90 for one domain |
| `qtype` | ✔ | filter by record type, 45 entries out of 90 for `A` |
| `protocol` | ✔ | filter by transport protocol |
| `start` and `end` | ✔ | filter by time interval in ISO 8601 UTC |

The presence of the time filter together with the total count makes
**incremental extraction** possible: the Adapter can ask only for the entries
following the last acquisition.

---

## Observed Value Sets

Values observed on the test traffic.

| Field | Values observed | Notes |
| --- | --- | --- |
| `protocol` | `Udp` | same vocabulary as `protocolTypeChartData` |
| `responseType` | `Blocked`, `Cached`, `Recursive` | coincides with `queryResponseChartData.labels`, which also comprises `Authoritative` and `Dropped` |
| `rcode` | `NoError`, `NxDomain` | DNS response codes |
| `qtype` | `A`, `AAAA` | DNS record types |
| `qclass` | `IN` | |

The consistency between `responseType` and the labels of the dashboard
indicates a single vocabulary throughout the product.

`responseType: Blocked` accompanied by `rcode: NxDomain` reflects the
`blockingType: NxDomain` setting.

---

## Application Configuration

**Confirmed.** Default configuration, readable and modifiable through the API.

```json
{
  "enableLogging": true,
  "maxQueueSize": 200000,
  "maxLogDays": 7,
  "maxLogRecords": 10000,
  "enableVacuum": false,
  "useInMemoryDb": false,
  "sqliteDbPath": "querylogs.db"
}
```

Three elements have direct consequences for the design of the Adapter.

**`maxLogRecords: 10000`** — the default retention is ten thousand records
altogether, not seven days of traffic. On a real home network that corresponds
to less than an hour of activity. The time limit of seven days is therefore
theoretical: the limit on the number of records almost always prevails.

**`maxQueueSize: 200000`** — writing is buffered in memory and asynchronous
with respect to resolution. The impact on throughput is therefore smaller than
the app's own warning would suggest.

**`enableVacuum: false`** — the SQLite file is not compacted automatically and
may not shrink after records are deleted.

Consequences.

* The frequency of acquisition must be **shorter than the rotation time of the
  log**, otherwise data is lost silently.
* The Adapter should read the configuration of the app and signal when the
  retention is insufficient with respect to the frequency of acquisition.
* The configuration is modifiable through the API, so the guided installation
  can propose adequate values.

---

# Mapping to the Unified Data Model

Key: **✔** available, **~** derivable, **✘** not obtainable.

## DataSource

| Property | | Origin |
| --- | - | --- |
| `Version` | ✔ | `login` → `info.version` |
| `Provider` | ✔ | constant of the Adapter |
| `Name` | ~ | `login` → `info.dnsServerDomain` |
| `LastUpdate` | ~ | clock of the Adapter at the end of the acquisition |
| `Id` | ~ | generated by PIE, does not exist on the Technitium side |
| `Status` | ✘ | no explicit state exposed by the API |
| `Capabilities` | — | see the note below |

## Statistics

| Property | | Origin |
| --- | - | --- |
| `TotalQueries` | ✔ | `stats.totalQueries` |
| `BlockedQueries` | ✔ | `stats.totalBlocked` |
| `CachedQueries` | ✔ | `stats.totalCached` |
| `ActiveDevices` | ✔ | `stats.totalClients` |
| `FailedQueries` | ~ | ambiguous aggregation, see Open Points |
| `EncryptedQueries` | ~ | sum of the encrypted protocols in `protocolTypeChartData` |
| `DnssecEnabled` | ✔ | `settings/get` → `dnssecValidation` |
| `UniqueDomains` | ✘ | the API exposes only the first N domains, not the total |

## Device

| Property | | Origin |
| --- | - | --- |
| `IpAddress` | ✔ | `topClients[].name` |
| `Hostname` | ~ | `topClients[].domain`, often absent |
| `DeviceId` | ~ | generated by PIE |
| `Status` | ~ | derivable from presence in the window |
| `FirstSeen` | ✘ | not exposed |
| `LastSeen` | ✘ | not exposed |
| `MacAddress` | ✘ | not in the dashboard, perhaps in the DHCP APIs |
| `Vendor` | ✘ | not exposed |
| `OperatingSystem` | ✘ | not exposed |

## Domain

| Property | | Origin |
| --- | - | --- |
| `Name` | ✔ | `topDomains[].name` |
| `Occurrences` | ✔ | `topDomains[].hits` |
| `Category` | ✘ | the business of the Threat Engine, not of the source |
| `Reputation` | ✘ | the business of the Threat Engine, not of the source |
| `FirstSeen` | ✘ | not exposed |
| `LastSeen` | ✘ | not exposed |

## DomainActivity

No property is obtainable from the dashboard APIs.

**Every** one is obtainable from the Query Logs app, when installed.

| Property | | Origin with Query Logs |
| --- | - | --- |
| `DeviceId` | ~ | resolved from `entries[].clientIpAddress` |
| `Domain` | ✔ | `entries[].qname` |
| `QueryCount` | ~ | count of the entries per client and domain pair |
| `Blocked` | ✔ | `entries[].responseType` equal to `Blocked` |
| `Protocol` | ✔ | `entries[].protocol` |
| `FirstSeen` | ~ | smallest `timestamp` of the group |
| `LastSeen` | ~ | largest `timestamp` of the group |

The aggregation happens in the Adapter: the Core receives `DomainActivity`
already consolidated and never sees an individual query.

---

# Open Points

Elements requiring a decision or an update to the documentation.

---

## 1. DomainActivity Cannot Be Built

This is the most significant finding of the reconnaissance.

`DomainActivity` represents the relation between a Device and a Domain. The
dashboard APIs, however, expose `topClients` and `topDomains` as **separate
and independent aggregates**: they say how many queries each client made and
how many times each domain was asked for, but not which client contacted which
domain.

The correlation requires the query logs.

The logs in Technitium are not part of the server: they require the
installation of an optional DNS App, typically **Query Logs (Sqlite)**.
Without that app, the datum does not exist.

With the app installed the problem is solved: the verification showed that
every property of `DomainActivity` is obtainable and that incremental
extraction is supported.

Consequences to be evaluated.

* Specification 04 lists the Logs among the data acquired with the phrase
  "when available": the reconnaissance confirms that caution was well founded,
  but the condition is to be made explicit.
* The Glossary defines Domain Activity as a first-class entity. Since the
  primary source does not supply it as standard, it must be established
  whether it is an optional datum or whether support for Query Logs becomes a
  declared prerequisite.
* Other Data Sources provided for, among them Pi-hole and AdGuard Home, expose
  the logs per client natively. The limitation is therefore specific to
  Technitium and not to the application domain: making it a general
  prerequisite would contradict the principle of Backend Independence.

---

## 2. Time Aggregates, Not Events

Technitium exposes statistics **per time window** — last hour, day, week,
month, year — not a stream of events.

The Unified Data Model instead uses `firstSeen`, `lastSeen` and `occurrences`,
which presuppose continuous observation.

How the Adapter is to interpret the window must be documented: whether
`firstSeen` and `lastSeen` correspond to the bounds of the window questioned,
or whether it is PIE that keeps the history by accumulating successive
snapshots.

The second hypothesis appears more consistent with the concept of
NetworkSnapshot, but it is an architectural decision that is not the Adapter's
to take.

---

## 3. Truncated Lists

`getTop` returns at most `limit` elements, by default 1000.

There is no call returning the complete list of the domains observed.

It follows that `Statistics.UniqueDomains` cannot be computed exactly and that
the set of Domains is by construction partial.

It is to be decided whether the value should be omitted, marked as
approximate, or computed on a different basis.

---

## 4. Definition of FailedQueries

Technitium distinguishes `totalServerFailure`, `totalNxDomain`,
`totalRefused` and `totalDropped`.

The Unified Data Model provides for a single `FailedQueries`.

The aggregation is not obvious: an NXDOMAIN is a legitimate answer, not an
error of the service. Adding it to the errors would alter the evaluation of
Network Integrity in the computation of the NPSS.

Which counters contribute to `FailedQueries` is to be defined.

---

## 5. Undefined Fields, Outcome Of The Reconnaissance

State of the four "external" fields left open after M2.2.

| Field | Outcome |
| --- | --- |
| `DataSource.Capabilities` | **Resolved, with a correction.** See the note that follows. |
| `DomainActivity.Protocol` | **Resolved.** `protocolTypeChartData.labels` returns labels in PascalCase, value observed `Udp`. The complete set deducible from the configuration flags is `Udp`, `Tcp`, `Tls`, `Https`, `Quic`. |
| `DataSource.Status` | **Open.** No state exposed by the API. It must be produced by the Adapter from the outcome of the communication. |
| `Device.Status` | **Open.** No state exposed. Derivable from the presence of the client in the window observed. |

### Correction On The Nature Of Capabilities

A first reading of this reconnaissance had supposed that
`DataSource.Capabilities` would be filled with the configuration flags of
Technitium, such as `enableDnsOverTls` or `dnssecValidation`.

The supposition was **wrong** and confused two distinct concepts.

* The features of the external product, such as support for DNSSEC or for
  encrypted protocols, are **data to analyse**. They contribute to the
  computation of the NPSS and find their place in `Statistics` and in the
  configuration acquired.
* A capability is instead **structural** information: it declares which
  entities of the Unified Data Model that source is able to supply, and serves
  the Core in knowing which analyses it can perform.

The correct vocabulary is therefore made of the names of the entities of the
model, as defined in the Data Model Specification.

For Technitium the capabilities are `Statistics`, `Device` and `Domain` at the
base level, with the addition of `DomainActivity` when the component recording
queries is active.

---

The reconnaissance closed two fields out of four.

The two remaining ones moved from "undefined" to "not obtainable from the
source", which is different and more useful information: it indicates that the
vocabulary must be defined by PIE, not sought elsewhere.

---

## 6. Choice Of The Time Window

Emerged from the observations on real traffic.

`LastDay` returned `topDomains` empty in the presence of traffic, while
`LastHour` contained the correct detail.

The Acquisition Flow must therefore define which window to question for each
kind of datum, and cannot assume that a wider window contains everything a
narrower one contains.

This is to be confirmed with prolonged observations before becoming a
documented rule.

---

# Live Instance Findings

Results obtained by questioning a real instance of Technitium 15.4.

---

## Settings

**Confirmed.**

```text
GET /api/settings/get
```

Returns more than a hundred and twenty configuration properties. Those
relevant to PIE are the following.

| Property | Default value observed | Use in PIE |
| --- | --- | --- |
| `version` | `15.4` | `DataSource.Version` |
| `dnsServerDomain` | host name of the server | `DataSource.Name` |
| `dnssecValidation` | `true` | `Statistics.DnssecEnabled` |
| `enableDnsOverTls` | `false` | capability |
| `enableDnsOverHttps` | `false` | capability |
| `enableDnsOverQuic` | `false` | capability |
| `enableDnsOverHttp` | `false` | capability |
| `enableDnsOverHttp3` | `false` | capability |
| `qnameMinimization` | `true` | capability, privacy indicator |
| `eDnsClientSubnet` | `false` | capability, privacy indicator |
| `enableBlocking` | `true` | state of the filtering |
| `blockingType` | `NxDomain` | mode of blocking |
| `blockListUrls` | **empty** | state of the blocklists |
| `blockListUpdateIntervalHours` | `24` | frequency of update |
| `logQueries` | `false` | availability of the query logs |
| `enableInMemoryStats` | `false` | the statistics are persisted |
| `maxStatFileDays` | `365` | retention of the statistics |
| `recursion` | `AllowOnlyForPrivateNetworks` | configuration of the resolver |

---

## DNS Apps

**Confirmed.**

```text
GET /api/apps/list
```

On a freshly created installation it returns an **empty** array.

No DNS App is installed as standard.

Together with `logQueries` set to `false`, this confirms definitively that the
Query Logs are not available on a standard installation.

```text
GET /api/apps/listStoreApps
```

Returns **twenty-seven** apps available.

The call **fails if the server has no connectivity towards the outside**:
during the first collection it returned a resolution error for
`go.technitium.com`, and worked only after a forwarder was configured.

The Acquisition Flow must therefore not depend on this call.

The apps relevant to the project are the following.

| App | Relevance |
| --- | --- |
| `Query Logs (Sqlite)` | Enables the query logs, prerequisite of Domain Activity |
| `Query Logs (MySQL)` | Variant with a MySQL or MariaDB database |
| `Query Logs (PostgreSQL)` | Variant with a PostgreSQL database |
| `Query Logs (SQL Server)` | Variant with Microsoft SQL Server |
| `Log Exporter` | Exports the logs towards a file, an HTTP endpoint or Syslog |
| `Advanced Blocking` | Blocking rules for groups of clients |
| `DNS Block List (DNSBL)` | Blocking based on DNSBL lists |

The official description of `Query Logs (Sqlite)` warns that logging queries
has an impact on throughput.

This confirms definitively that reaching Domain Activity requires the explicit
installation of an app and carries a cost in performance the user must accept
knowingly.

---

## Logs

**Confirmed.**

```text
GET /api/logs/list
```

Returns `logFiles`, with `fileName` and `size`.

These are the **diagnostic logs of the server**, not the queries. The settings
indicate `loggingType: File` and `maxLogFileDays: 365`.

---

## Hierarchical Browsing

**Confirmed.**

```text
GET /api/blocked/list?domain=&direction=down
GET /api/cache/list?domain=&direction=down
```

Both return `{ domain, zones, records }`.

They are not flat lists but **hierarchical browsers**: one descends a level at
a time starting from the root.

Enumerating the blocked domains or the whole cache therefore requires walking
a tree with successive calls.

This has a direct impact on the design of the Adapter and on the cost of
acquisition.

---

## Fixed Value Sets

**Confirmed.**

`queryResponseChartData.labels` exposes a fixed set.

```text
Authoritative, Recursive, Cached, Blocked, Dropped
```

`mainChartData.datasets` exposes the time series.

```text
Total, No Error, Server Failure, NX Domain, Refused,
Authoritative, Recursive, Cached, Blocked, Dropped, Clients
```

---

## Response Envelope

**Confirmed.**

Every answer contains, besides `status`, a `server` field with the name of the
server that answered.

---

## DHCP

**Confirmed.**

```text
GET /api/dhcp/leases/list
```

Answers correctly and returns `leases`, empty in the absence of a configured
DHCP scope.

It remains a possible origin of `Device.MacAddress`, but only when Technitium
is also used as a DHCP server. It is therefore not a source the Adapter can
count on.

---

## DNS Client

**Confirmed.**

```text
GET /api/dnsClient/resolve?server=this-server&domain=<domain>&type=A&protocol=Udp
```

Returns `{ result, rawResponses }`.

An important finding: the resolutions performed through this API **are not
counted in the statistics of the dashboard**. The cache fills, but
`totalQueries` stays at zero.

It follows that this call cannot be used to generate observable traffic, nor
is it to be considered part of the network activity measured.

---

## Traffic Observations

Observations collected by generating real DNS traffic against the instance.

---

### Protocol Labels

**Confirmed.**

`protocolTypeChartData.labels` returns the labels of the transport protocols
in PascalCase.

Value observed with UDP traffic.

```json
{ "labels": ["Udp"], "datasets": [{ "data": [2] }] }
```

The labels appear only for the protocols actually used. The complete set of
possible values is deduced from the configuration flags: `Udp`, `Tcp`, `Tls`,
`Https`, `Quic`.

This closes the format of `DomainActivity.Protocol`.

---

### Query Types

**Confirmed.**

`queryTypeChartData.labels` returns the types of DNS record queried.

```json
{ "labels": ["A", "AAAA"], "datasets": [{ "data": [1, 1] }] }
```

The set is not fixed: it depends on the traffic observed.

---

### Top Clients

**Confirmed.** Structure populated.

```json
{
  "name": "127.0.0.1",
  "domain": "localhost",
  "hits": 7,
  "rateLimited": false
}
```

The `domain` field comes from a reverse resolution and is absent when not
available.

---

### Time Window Behaviour

**A significant finding, to be looked into further.**

With the same state of the server and at the same instant, two time windows
returned different data.

| Window | `totalQueries` | `topClients` | `topDomains` |
| --- | --- | --- | --- |
| `LastHour` | 2 | 1 element | `example.com`, 2 hits |
| `LastDay` | 7 | 1 element | **empty** |

The wider window reports more queries and keeps the statistics per client, but
**loses the detail per domain entirely**.

The most plausible hypothesis is that the statistics aggregated over long
periods are built from files consolidated periodically, and that the detail
per domain of the current hour has not yet flowed into them.

The consequences for the Adapter are relevant.

* The choice of the time window **is not indifferent**: it determines which
  data really exists.
* Questioning `LastDay` may return zero domains even in the presence of
  traffic.
* The acquisition of the domains seems reliable only on `LastHour`.

This behaviour is to be confirmed with observations over a longer period
before being assumed as a rule.

---

### Custom Interval

**Verified in M3.4.**

Questioning with a custom interval **preserves the detail per domain**.

The comparison between the statistics acquired through a custom interval and
those shown by the console of the server found exact correspondence.

| Quantity | Acquired | Console | Note |
| --- | --- | --- | --- |
| Total queries | 30 | 32 | difference caused by two later queries |
| Queries from the cache | 20 | 22 | the same two queries |
| Distinct domains | 5 | 5 | correspondence |
| Active clients | 1 | 1 | correspondence |

The loss of the detail per domain therefore concerns the predefined window
relating to the last day, not the custom interval.

The strategy of incremental acquisition remains practicable.

---

# Impact on Existing Specifications

No Specification was modified by this document.

The changes the reconnaissance suggests are the following, all to be approved.

| Specification | Change suggested |
| --- | --- |
| 04 - Technitium Integration | Declare the dependency of the Logs on an optional DNS App |
| 04 - Technitium Integration | Document authentication through an API Token and the Dashboard View permission |
| 05 - Data Model | Clarify the semantics of `firstSeen`, `lastSeen` and `occurrences` over windows |
| 05 - Data Model | Define which counters contribute to `FailedQueries` |
| 09 - Network Privacy | Evaluate the conditional availability of Domain Activity |

---

# Related Specifications

* 00 - Glossary
* 04 - Technitium Integration
* 05 - Data Model
* 11 - Backend

---

# Sources

* Technitium DNS Server API Documentation, official repository
  `TechnitiumSoftware/DnsServer`, file `APIDOCS.md`
* Technitium DNS Server, official site and Help section
