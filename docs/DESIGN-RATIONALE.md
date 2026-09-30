# Why PIE Is The Way It Is

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Design Rationale

**Status:** Official

**Last Updated:** 2026-09-30

---

# What This Document Is

The Specifications in this folder say **what** the system does. This document
says **why**, for the fifteen decisions that shaped everything else.

It exists because the reasoning was recorded in `CHANGELOG.md`, entry by
entry, in Italian, across forty entries and three and a half thousand lines.
The history stays there and is not translated: which migration, which test
count, which afternoon a defect appeared — that is the project's own memory
and it interests nobody outside it. What follows is the part that a person
arriving at the repository needs in order to understand why the code looks
like this and not like something simpler.

Each decision is given with the alternative that was not taken and what it
would have cost. A decision recorded without its alternative is indistinguish-
able from an accident.

---

# 1. The Number Is Not The Product

Most privacy tools show a score. This one shows a score **and what it rests
on**, and refuses to show it when it cannot support it.

The temptation, every single time, is to make the interface calmer by dropping
a qualification: to write "no tracking" instead of "no *known* tracking", "0
trackers" instead of "the filter was not put to the test". Each of those
edits makes the product nicer and makes it lie.

The rule adopted is that **the qualification is the product**. Everything
below follows from it, and most of the complexity in this codebase exists to
keep it true.

---

# 2. Unmeasured Is Excluded, Never Scored As Zero

An area of the score that could not be measured does not contribute zero
points. It is removed from the calculation, and the weight it would have
carried is declared as missing.

*The alternative*: treat what was not measured as zero, which is what a naive
implementation does for free. It produces a number that always exists and is
always wrong in the same direction: a network nobody could observe scores
badly, and looks like a network that was observed and found wanting.

The visible cost is that the score is often lower than 100 for reasons that
are not the network's fault, and that fifteen points are permanently absent
today because the Device Engine does not exist.

---

# 3. Below 60% Coverage, No Score At All

Coverage is the share of the score's weight that was actually measurable.
Below **60**, PIE produces no overall number and **puts nothing in its
place** — no estimate, no partial score, no greyed-out figure.

*The alternative*: show the partial score with a warning. Warnings are not
read. A number on a screen is read, remembered, and repeated without its
warning; the warning does not travel with it.

This is the rule that costs the most in daily use, and it is the one that has
been defended most often.

---

# 4. One Hundred Queries, Or The Classification Areas Say Nothing

Areas that depend on classifying domains require at least **100 queries** in
the window being evaluated. Below that they are `NotMeasurable`.

*The alternative*: score whatever traffic there is. Three queries, none of
them to a known tracker, is not evidence of a clean network; it is evidence of
a quiet afternoon. Without this threshold the score rewards silence.

---

# 5. Observation Periods Are Fixed Hours, Not A Stream

Acquisition is aligned to fixed hourly buckets. A period is non-overlapping
and, once elapsed, immutable; observing the same period again **replaces** the
previous observation rather than adding to it.

*The alternative*: accumulate events continuously. It is the obvious design
and it double-counts: two acquisitions over overlapping intervals describe
part of the same traffic, and there is no way afterwards to tell what was
counted twice.

This decision looked like plumbing at the time. It is what later made 24-hour
aggregation and tiered consolidation possible at all: both are lawful only
because the pieces being summed are known not to overlap.

---

# 6. The Score Reads Twenty-Four Hours, Not The Current Hour

The score was originally computed on the hour in progress. On the afternoon of
2026-09-26 it went from 56 to "not measurable" in front of the person working
on the project, because a new hour had begun and had no queries in it yet.

The score now evaluates the **last twenty-four hours**, like the domains,
devices and statistics pages, and the answer declares which window it used and
how many of the hours requested were actually observed.

The lesson recorded with it is more general than the fix: **a page that does
not declare its period is lying by omission**. The same defect had already
appeared as domains that seemed to "disappear" — an hour boundary, invisible
because nothing on the page said what an hour was.

---

# 7. The Core Emits Codes, Never Sentences

A `ScoreFactor` is a code plus numeric values. The Core never produces text.

*The alternative*: have the Core produce the explanation, which is faster and
reads better in a debugger. It also welds the engine to one language. The
interface is bilingual from its first line, and a Core that speaks Italian
cannot be made to speak English without rewriting the engine.

The defect this was meant to prevent appeared anyway, once, in the other half
of the system: a number rendered with raw Vue interpolation instead of `$n()`,
so an English interface printed `1,88`. Moving the text out of the Core does
not help if the frontend then formats the numbers by hand.

---

# 8. One Declared Source Of Lists, Not A Pile Of Them

Domain classification uses the **Block List Project** alone, declared by name,
with its licence checked at the primary source.

*The alternative*: aggregate many lists, which sounds richer and was seriously
considered. Aggregation multiplies the sources whose licence, freshness and
editorial judgement have to be verified, and the result cannot be explained to
the person: "this domain is a tracker according to something" is not an
answer PIE is willing to give.

A **declared monoculture** is weaker at finding things and stronger at saying
what it knows. When two lists do claim the same domain, the conflict is
resolved in a fixed order — nearest name wins, then declared severity, then
freshness, then alphabetically — and the severity ranking is declared as an
editorial judgement, because that is what it is.

---

# 9. Unclassified Is Not Safe

A domain that no list recognises is `Unclassified`. It is never shown as
clean, and the interface never counts it as evidence of anything.

*The alternative*: treat unknown as harmless, which is what every dashboard
that displays "0 threats" is quietly doing.

---

# 10. The Network's Domains Never Leave The Device

Classification happens locally, against lists downloaded in advance. No domain
contacted by the network is ever sent anywhere, for any purpose, including
diagnostics. Logs carry counts, never network data. There is no telemetry.

*The alternative*: a classification service, which would be more accurate,
cheaper to build and continuously updated. It would also mean the tool that
tells you who is watching your network is itself a thing watching your
network.

This is one of the two rules that cannot be revised. Aggregation happens in
the Adapter, before data reaches the Core, so the register of individual
queries does not exist anywhere inside PIE even transiently.

---

# 11. The Adapter Aggregates, The Core Never Sees A Query

When query logs are available, the Adapter turns them into consolidated
`DomainActivity` before handing them on. The Core receives totals and never
reaches an individual query.

Three reasons, in order of weight: the raw register **is** the browsing
history of every device in the house; the aggregate is orders of magnitude
smaller; and the Core works on the Unified Data Model, not on the format of
one backend.

---

# 12. Everything Depends On Technitium, And Nothing Does

Technitium is the first supported Data Source. The Core contains no reference
to it; replacing it requires writing a new Adapter and nothing else, and the
isolation is enforced at compile time by the project references, so a shortcut
taken for convenience breaks the build rather than the architecture.

This mattered immediately. Technitium exposes devices and domains as
**independent aggregates** — how many queries each device made, how many times
each domain was asked for, but not which device asked for which. The
correlation exists only in the query logs, which are an optional component.

*The alternative*: declare query logs a prerequisite of the project. Other
sources the project provides for — Pi-hole, AdGuard Home — expose that datum
natively. The limitation belongs to Technitium, not to the problem, and making
it a general prerequisite would have contradicted the whole point of having
Adapters.

So there are two declared levels of availability, and the extended one is
optional and its cost is disclosed before it is enabled.

---

# 13. Device Visibility Has A Hole, And It Is Declared

A device that uses its own resolver does not appear in PIE **at all** — not as
a device with no traffic, but as nothing.

There is no way to detect from a DNS server the existence of a client that
never speaks to it. The limit is stated rather than left for the person to
discover, because a list of devices that silently omits the interesting one is
worse than no list.

---

# 14. Authentication Was The Gravest Debt, And It Was Documented First

For months the API answered anyone who reached it, and was safe only because
it listened on the loopback — a circumstance, not a measure.

It was closed with a Specification written **before** any code, because it is
the kind of decision whose correction, once users and data exist, costs
dearly. Two rules from it are worth repeating here: **there is no setting that
turns authentication off**, because a configuration that disabled it would be
the first one somebody leaves on for convenience; and **what is withheld by
role is declared**, so a `Viewer` asking about a domain gets `Withheld`, not
an empty list that would be indistinguishable from "no device contacted it".

The second is the rule of this whole project — absent is not the same as
unmeasurable — applied to permissions.

---

# 15. Debt That Is Not Written Down Is A Defect

Shipping something incomplete is allowed. Shipping something incomplete
**quietly** is not. Every gap is recorded under `Known Impact` in the
changelog entry that created it, and the ones that outlive their entry are
listed in `CLAUDE.md` and in the README.

This is why the README says what the project does **not** do before it says
how to install it, and why the Roadmap carried a publication criterion about
one of its own sentences: "the repository is not public" was true while it was
written and false the moment it was published. The sentence was moved on the
day of publication, which is what the criterion existed to force.

The same rule is why this repository went public with a licence criterion only
partly met — the source files carry no licence notice — stated in the Roadmap
rather than quietly deferred.

---

# A Failure Mode Worth Naming

Four times, during the translation of this documentation, a Specification was
found opening with a sentence in the present tense describing a state that had
ended weeks earlier — the transport document declaring that no encrypted
connection existed, the authentication document declaring that the API
answered anyone.

The cause is that the motivation for a change gets written into the very
document that introduces the change, in the present tense, and then outlives
it. Nobody rereads an opening paragraph.

The rule now lives in `MASTER_PROMPT.md`: **the motivation of a Specification
is written in the past tense**. The present tense belongs to the rules
themselves, which is what the document is for.

---

# Related

* `MASTER_PROMPT.md` — the working method these decisions are applied under
* `CHANGELOG.md` — the full history, in Italian below the language boundary
* `docs/00-Glossary.md` — the vocabulary
* `docs/13-Roadmap Specification.md` — what is still missing, and the criteria
  for Beta and Stable
