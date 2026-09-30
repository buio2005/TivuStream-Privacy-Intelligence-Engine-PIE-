# 17 - Classification Lists Research

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Classification Lists Research

**Version:** 1.1.0

**Status:** Analysis — Not Approved

**Last Updated:** 2026-08-04

---

> **Note added on translation, 2026-09-30.** This is a dated research record
> and is not updated as the project moves on. Its findings stand; some of the
> questions it leaves open have since been settled. The project licence is
> GPL-3.0 (Specification 14). The conflict between lists claiming the same
> domain is resolved in Specification 08, by declared severity. The update
> interval has a default of twenty-four hours. The adblock format is still not
> supported.

---

# Purpose

This document gathers the research on sources for classifying domains.

Specification 08 defines the **mechanism** and requires every list to declare
its licence. It names no source, because the choice requires a verification
that cannot be carried out by writing a specification.

The document reports what was verified on **4 August 2026** and proposes a
decision. It does not take it.

---

# Method

The licences were read **from the licence files of the sources**, not from
third-party summaries.

The distinction is not formal. A preliminary search reported the Block List
Project as MIT licensed; the `LICENSE` file of the project declares
**Unlicense**. Both are permissive, but a source that misreports a verifiable
fact is not a source on which to found a legal decision.

Every row of the table below comes from a document published by the
maintainer of the list.

---

# What We Distribute And What We Do Not

The distinction governs the whole analysis.

| What | Who does it | What it implies |
| --- | --- | --- |
| The content of the list | The person downloads it onto their own machine | We do not distribute it |
| Name, address, declared licence | Distributed with the project | It is configuration, not content |

PIE **redistributes no list**. The project holds an address and a declaration
of licence; the file arrives on the person's machine, at their instruction,
and never leaves that machine.

This reduces the exposure without removing it. Naming a source as a default is
a pointer we give, and remains our responsibility.

---

# Findings

## Verified Sources

| Source | Licence | Verified in | Structure | Format |
| --- | --- | --- | --- | --- |
| **Block List Project** | Unlicense (public domain) | `LICENSE` in the repository | 18 lists **by category** | hosts, domain only, dnsmasq, adblock |
| **HaGeZi DNS Blocklists** | GPL-3.0 | `LICENSE` in the repository | **Combined** lists | adblock, dnsmasq, others |
| **oisd** | GPL-3.0 | Official FAQ, licence section | **Combined** lists | adblock, dnsmasq, wildcard |
| **StevenBlack hosts** | MIT | `license.txt` in the repository | **Unified** list with variants | hosts |
| **Disconnect** | **CC BY-NC-SA 4.0** | `LICENSE` in the repository | Explicit categories in JSON | JSON |
| **Peter Lowe (pgl.yoyo.org)** | **Not declared** | Policy page: no licence | Single list | hosts, many others |
| **ShadowWhisperer** | Unlicense (public domain) | `LICENSE` in the repository | 23 lists **by category** | domain only |
| **lightswitch05 hosts** | Apache-2.0 | `LICENSE` in the repository | **Combined** lists (`ads-and-tracking`) | hosts, others |
| **Phishing Army** | **CC BY-NC 4.0** | Official site | Single phishing list | domain only |
| **DuckDuckGo Tracker Blocklists** | **CC BY-NC-SA 4.0** | Official repository | Explicit categories | JSON |

---

## Two Exclusions Follow From Our Own Rules

**Peter Lowe.** The policy page describes the inclusion criteria and the
validation procedure precisely, and **declares no licence**. Specification 08
makes the licence mandatory. The list is excluded by our own rule, not by any
judgement on its quality, which is high.

**Disconnect.** The **NonCommercial** clause constrains use. PIE is not a
commercial product, but it is software others may install in any context,
including a company. Naming as a default a source that forbids commercial use
would impose on the person a condition they did not choose and will probably
not read.

It remains a list the person **may add knowingly**. Not one we add on their
behalf.

---

## A Pattern In The Ecosystem

The sources with the most solid methodology on tracking are **all
NonCommercial**.

| Source | Methodology | Licence |
| --- | --- | --- |
| Disconnect | Human verification, explicit categories, used by Firefox | CC BY-NC-SA 4.0 |
| DuckDuckGo Tracker Blocklists | Automated measurement: crawling the most visited sites, observing cookies and fingerprinting APIs | CC BY-NC-SA 4.0 |
| Phishing Army | Aggregation of phishing reports | CC BY-NC 4.0 |

This is not a coincidence. Building a source with a measurable methodology
costs money, and whoever bears that cost reserves commercial use.

The consequence for us is that **methodological quality and freedom of licence
are not on the same side**. The freely usable sources are curated by
volunteers, through reports and manual review; the ones built on systematic
measurement are constrained.

We record the fact rather than pick the best source and hope the licence does
not matter.

---

## A Structural Constraint We Discovered

Our model associates **one category with one list**. `ClassificationList` has a
single `Category` property, and every domain found in that list receives that
category.

This excludes combined lists.

HaGeZi, oisd and StevenBlack gather advertising, tracking, malware and
phishing domains into a single file. Declaring one of them as `Advertising`
would attribute that category to the malware domains it contains too: a false
statement produced by the structure, not by a mistake.

They are excellent lists for **blocking**, which is their purpose. They are
unsuited to **classifying**, which is ours.

The consequence is plain: among the verified sources, **only the Block List
Project and ShadowWhisperer are compatible with the model**.

---

## The Second Source Is Upstream Of The First

The Block List Project declares that it synchronises daily with fourteen
upstream sources, and **ShadowWhisperer is one of them**.

ShadowWhisperer declares the opposite: *"I will not merge other lists"*. Its
lists come from a personal script and manual additions.

The order is therefore the reverse of the appearance: ShadowWhisperer is a
**primary** source, the Block List Project is in part **derived**.

Adopting both as defaults would give the appearance of a plurality of opinions
without being one. On a contested domain the two would tend to agree, because
one reads the other, and the agreement would be read as independent
confirmation.

This does not make them useless together, and it does make it necessary to
declare the relation rather than present them as two opinions.

---

## Where The Second Source Does Not Fit

The categories of ShadowWhisperer do not coincide with ours.

| List | Declared content | Compatibility |
| --- | --- | --- |
| `Ads` | Advertising, banners, push notifications | Matches `Advertising` |
| `Tracking` | Analytics, diagnostics, location, metrics | Matches `Tracking`, and covers analytics too |
| `Scam` | Fraud around products, shipping, support | Matches `Suspicious` |
| `Malware` | Malware, **phishing**, PUPs, redirectors, remote scammers | **Incompatible** |
| `Cryptocurrency` | Bitcoin, Ethereum, mining, explicitly *not* malware | **Incompatible** |

`Malware` merges malware and phishing, which in our model are two distinct
categories. Declaring that list as `Malware` would attribute the wrong
category to every phishing domain it contains.

`Cryptocurrency` includes exchanges and legitimate services, not only
cryptojacking. Our `Cryptomining` category means something else.

Three lists out of twenty-three remain usable, precisely in the categories
where the Block List Project is already present and where the risk of being
wrong is lower.

In the two categories where a mistake costs most — malware and phishing — the
second source **adds nothing that can be declared truthfully**.

---

## A Constraint That Does Not Apply To Us

The oisd FAQ explains why the project abandoned the hosts and domain-only
formats: those formats cannot express a wildcard, and therefore require every
known subdomain to be listed, with no way to cover the unknown or randomly
generated ones.

The objection is correct for anyone consuming the list line by line.

**It does not apply to our engine.** The matching rule walks from the full
name up towards the parent domain: a line `example.com` already covers
`bad239ue9f59gw.example.com`, declaring the outcome as an inference with
`Medium` confidence.

The compact format is therefore sufficient for us, and the choice to walk
upwards — taken for reasons of honesty, to distinguish a statement from an
inference — turns out to be the technically more efficient one as well.

---

# An Unresolved Rule In Specification 08

The research brought to light a gap in a specification already approved.

A domain may appear in **several lists under different categories**. An
advertising domain that also tracks belongs legitimately to both `ads` and
`tracking`, and ShadowWhisperer says so openly: *"Categories like Ads and
Tracking may contain domains that do both"*.

Specification 08 states that the search stops at the first match. **It does not
state in what order the lists are consulted.**

Today the order is the one in which the engine receives them. An
implementation detail therefore decides which category the person sees, and
the same installation could show different categories after a simple
reorganisation of the code.

The gap exists already with only the seven lists proposed. Adding sources
makes it more frequent, it does not introduce it.

It must be resolved before the engine is connected, because it concerns what
is asserted to the person and not how it is computed.

Three possible solutions, in increasing order of cost.

| Solution | What it entails |
| --- | --- |
| Declared order | Every list carries an explicit, configurable priority. The choice stays arbitrary but becomes visible and changeable. |
| Most specific category | A hierarchy between categories is defined. It requires establishing that `Malware` prevails over `Advertising`, that is, an editorial judgement to be declared. |
| Multiple classifications | A domain carries every category found. It is the most truthful solution and entails a change to the Data Model and to the interface. |

This document does not propose which to adopt. It is a decision about the
truth of what we show, not about the implementation.

---

# Methodology Of The Proposed Source

The Block List Project declares its procedure publicly.

| Aspect | What is declared |
| --- | --- |
| Validation | 151 automated tests on every change, syntax and TLD checking |
| Dead domains | Weekly scan and removal |
| False positives | Public reporting on GitHub, human review |
| Protection | A list of essential domains that cannot be blocked |
| Upstream sources | 14 lists monitored daily, among them HaGeZi and ShadowWhisperer |
| Update | Daily |

The procedure is inspectable: the build code, the tests and the history of the
decisions are public.

That criterion counts for more than the licence. A list with a perfect licence
and opaque curation would have us make statements whose grounds we do not
know.

---

# Risks To Declare

**Monoculture.** A single default source means its mistakes become ours, and
its silences become `Unknown`. The model provides for several lists and for
the person adding their own; the default configuration is still our choice and
is to be declared as such.

The search for a second source did not resolve the risk. The only compatible
and freely licensed source is upstream of the first, and in the two categories
where being wrong costs most it is unusable. An apparent plurality would have
been worse than a declared monoculture.

**Upstream licence.** The Block List Project declares itself public domain and
aggregates sources including HaGeZi, which is GPL-3.0. Whether a list of
domains is a protected work is contested: in the United States facts are not
covered by copyright absent creative selection, while in the European Union a
sui generis database right exists. We are not in a position to resolve it, and
this is not legal advice. We record it because it exists.

**Categories without a source.** The categories `Analytics`, `Streaming`,
`Cloud` and `AI Services` have no corresponding list. The project's `tracking`
list declares that it covers "tracking/analytics", so analytics domains
receive `Tracking`.

No category is invented to fill a gap: a domain with no list stays `Unknown`,
which means not classified.

---

# Proposal

Adopt as defaults **seven lists of the Block List Project**, in the
domain-only format.

| List | PIE category | Declared content |
| --- | --- | --- |
| `ads-nl.txt` | `Advertising` | Advertising servers |
| `tracking-nl.txt` | `Tracking` | Tracking and analytics |
| `malware-nl.txt` | `Malware` | Malware hosts |
| `phishing-nl.txt` | `Phishing` | Phishing sites |
| `crypto-nl.txt` | `Cryptomining` | Cryptojacking and cryptocurrency scams |
| `scam-nl.txt` | `Suspicious` | Scam sites |
| `abuse-nl.txt` | `Suspicious` | Deceptive or abusive sites |

Reasons.

* It is the only verified source **segmented by category**, and therefore the
  only one compatible with the model.
* The Unlicense imposes no condition on the person.
* The domain-only format is already understood by the reader, verified by
  eighteen tests.
* The curation procedure is public and verifiable.

The lists `facebook`, `twitter`, `tiktok` and `whatsapp` would correspond to
the `Social` category and are **not proposed**: they list the domains of
services the person may use deliberately, and classifying them as a threat
would be an editorial judgement that is not ours to make. They remain
available as lists the person can add.

The lists `porn`, `gambling`, `drugs`, `piracy` and `torrent` concern
**content**, not privacy or security. They are outside the scope of the
project.

---

# What This Document Does Not Decide

* Whether to adopt the proposal. It is a decision to be taken explicitly.
* The licence of the project itself, which remains to be chosen.
* The default update interval.
* Whether the reader should learn the adblock format, today unsupported.
* **How to resolve the conflict between lists claiming the same domain.** It
  is the most urgent decision: it concerns what is asserted to the person, and
  the gap exists already today.

---

# Sources

All consulted on 2026-08-04.

* Block List Project — [repository](https://github.com/blocklistproject/Lists), [list index](https://blocklistproject.github.io/Lists/), [licence](https://raw.githubusercontent.com/blocklistproject/Lists/master/LICENSE)
* ShadowWhisperer — [repository](https://github.com/ShadowWhisperer/BlockLists), [categories](https://raw.githubusercontent.com/ShadowWhisperer/BlockLists/master/README.md), [licence](https://raw.githubusercontent.com/ShadowWhisperer/BlockLists/master/LICENSE)
* lightswitch05 hosts — [licence](https://raw.githubusercontent.com/lightswitch05/hosts/master/LICENSE)
* Phishing Army — [official site](https://phishing.army/)
* DuckDuckGo Tracker Blocklists — [repository](https://github.com/duckduckgo/tracker-blocklists)
* HaGeZi DNS Blocklists — [repository](https://github.com/hagezi/dns-blocklists), [licence](https://raw.githubusercontent.com/hagezi/dns-blocklists/main/LICENSE)
* oisd — [FAQ, licence section](https://oisd.nl/faq)
* StevenBlack hosts — [licence](https://raw.githubusercontent.com/StevenBlack/hosts/master/license.txt)
* Disconnect — [licence](https://raw.githubusercontent.com/disconnectme/disconnect-tracking-protection/master/LICENSE)
* Peter Lowe — [inclusion policy](https://pgl.yoyo.org/adservers/policy.php)

---

# Related Specifications

* 05 - Data Model
* 08 - Threat Intelligence
* 16 - Persistence
