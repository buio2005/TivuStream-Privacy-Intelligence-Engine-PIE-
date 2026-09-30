# TivuStream Privacy Intelligence Engine (PIE)

**A self-hosted tool that reads your DNS server, works out what your network
is contacting, and tells you honestly — including what it does not know.**

> **Status: in development. Not ready for use.**
>
> PIE runs, it is tested, and it installs as a service on Windows and Linux.
> It is published at this stage because the one thing it needs most cannot be
> obtained privately: someone other than its author installing it. See
> [What it does not do yet](#what-it-does-not-do-yet) before you decide to try
> it.

---

## Why another one

Most privacy tools show you a number. A score, a percentage, a green tick.

The number is usually built on measurements that were incomplete, on lists
that were three weeks old, or on the absence of evidence. None of that is
visible, so the reassuring version and the meaningless version look identical.

PIE shows the number **and what it rests on** — and refuses to show it when it
cannot support it.

Three examples of what that means in practice, all of them implemented rather
than aspirational.

**The score can decline to exist.** Each of the six areas of the Network
Privacy & Security Score declares whether it was measured, measured in part,
or not measurable at all. What was not observed is excluded from the
calculation, never counted as zero. If less than sixty points out of a hundred
could be measured, no overall score is produced — and nothing takes its place.
No provisional figure, no empty bar, no placeholder suggesting a value is on
its way. The breakdown is still shown, with the reason each area was or was
not measured.

**A value declares how it is known.** A count that can only be a floor is
labelled a lower bound, not rounded up to something plausible. An instant that
is only known to the hour is presented as an hour, not as a moment. The
interface is forbidden, by specification, from presenting a qualified value as
an exact one.

**Unclassified never means safe.** Domain classification happens entirely on
your machine, against lists held locally. Lists assert that a domain tracks
you; they never assert that one does not. So a domain in no list is reported
as *not recognised*, with those words, and never as clean. Every
classification also declares which list produced it and how old that list was.

The cost of that last decision is real and stated: threats that appeared this
week are recognised late. The alternative was to ask a reputation service
about the domains your network contacted, which would mean telling that
service your browsing history in order to protect it.

---

## What it does today

* Reads a **Technitium DNS Server** through its public API, on a fixed hourly
  cycle. Per-query detail is aggregated inside the adapter and never reaches
  the rest of the system.
* Classifies the domains observed against local lists, roughly 3.6 million
  entries across seven categories, downloaded daily and inspectable as plain
  text files.
* Computes the **Network Privacy & Security Score** over six areas, with every
  factor that contributed to it.
* Keeps observations as non-overlapping hourly periods, consolidated into days
  after a month and into months after a year, then deleted.
* Serves an interface in **Italian and English** with a dashboard and a domain
  list with per-domain detail.
* Requires a signed-in account on every endpoint, over HTTPS when reached from
  another machine, with a certificate it generates and whose fingerprint it
  prints at startup.

Around 616 automated tests, 495 on the backend and 121 on the interface. Each
of them refers to a commitment stated in a specification, not to an
implementation detail.

---

## What it does not do yet

This list is the point of publishing at this stage.

* **Nobody outside the project has installed it.** The installation procedure
  exists, is documented and has been used once, by its author, on Windows.
  This is the gap that most needs closing.
* **Devices and statistics are measured but not shown.** The engine collects
  them and the API returns them; the interface has no screen for either.
* **History is written and never read.** Consolidated days and months
  accumulate correctly, and no view displays them. Copies taken before a
  database upgrade are never deleted automatically.
* **Three of the five Core engines do not exist.** Device, Alert and
  Recommendation. Device Health is therefore not measurable, and fifteen of
  the hundred points stay permanently outside the score.
* **No alerts, no recommendations, no reports.**
* **No licence header in the source files**, and the interface does not yet
  display the licence notice the GPL asks for.
* **`/devices` and `/statistics` are measured but not shown**, and neither
  declares the period it covers.
* **The history of the changelog is in Italian** below the language boundary.
  The reasoning it holds is in English in
  [`docs/DESIGN-RATIONALE.md`](docs/DESIGN-RATIONALE.md).

Known gaps are recorded as they are found. A gap that is not written down is
not a debt, it is a defect.

---

## Requirements

* A **Technitium DNS Server** your network actually uses, reachable over HTTP,
  with an API token that can read the Dashboard and Settings sections.
* Windows 10 or later, or a recent Linux distribution.
* To build from source: .NET 10 SDK and Node.js 22 or later.

PIE does not replace your DNS server, does not filter anything and does not
route traffic. It reads.

---

## Install

PIE installs as a service from a package that contains everything it needs.
The guide, written for someone who has never seen this project, is
[`installer/INSTALL.md`](installer/INSTALL.md) and is included in every
package.

Build the packages from the repository:

```text
powershell -ExecutionPolicy Bypass -File installer\build-package.ps1
```

They are written to `dist/`.

The first start prints a one-time setup code, which you use to create the
first account. If you lose access, `dotnet run -- reset-password <name>`
restores it from the machine PIE runs on.

---

## Reaching it from another device

PIE opens from a phone or another computer on the same network, over an
encrypted connection, with nothing to configure.

At startup it prints the addresses it answers on and a **fingerprint**:

```text
PIE is reachable at:
  https://YOUR-PC:5443
  https://192.168.1.5:5443
SHA-256 fingerprint: D7:00:13:3B:...
```

Your browser will warn that the connection "is not private". That is expected:
PIE created the certificate itself and no browser has heard of it. Open the
certificate details and check that the SHA-256 fingerprint matches the one PIE
printed. If it matches, continue. **If it does not match, do not type your
password.**

The warning returns once per device when PIE renews the certificate, about
once a year, or when your router gives the machine a different address.

If you have your own certificate, point `Transport:Certificate:Path` at it in
`appsettings.Local.json` and the warning disappears. If you would rather PIE
were not reachable from the network at all, set `Transport:HttpsPort` to `0`.

---

## What PIE keeps, and for how long

PIE keeps a summary of what the network did. It never keeps the list of
individual queries: aggregation happens in the adapter, before the data
reaches anything else.

| For how long | What remains |
| --- | --- |
| Last 30 days | Hour by hour: which domains, how many queries, from which device |
| Up to 12 months | Day by day, with the same information |
| Up to 5 years | Month by month: which domains and which devices, but no longer which device contacted which domain |
| Beyond | Nothing |

Every day and every month records how many hours were actually observed. A day
when PIE was switched off does not look like a quiet day.

What is deleted is deleted: PIE overwrites it in the file rather than merely
marking it as removed.

**Consolidation cannot be undone.** Shortening these periods makes PIE
summarise or delete the older detail at the next hourly cycle, and the detail
does not come back. The values are in `appsettings.Local.json`, under
`Storage:Retention`. A value that is too low is not silently corrected: PIE
refuses to start and says which one to change.

---

## How it is built

```text
Frontend  →  REST API  →  Privacy Intelligence Engine  →  Adapters  →  Data Sources
```

The engine analyses; the adapters translate a specific backend into a
**Unified Data Model**; the interface only ever displays results already
produced. Two flows are kept apart: acquisition runs on its own schedule and
is never triggered by a request from the interface.

A second data source would mean a second adapter, and no change to the engine.

**Backend:** ASP.NET Core, C#, SQLite with explicit SQL and hand-written
migrations.
**Frontend:** Vue 3, TypeScript, Pinia, Vite, vue-i18n.

The backend never sends text meant to be displayed. Every factor explaining a
score travels as a code with numeric values, and the interface renders it in
the language being read. That is why a share arrives as `0.071` and not as
`7.1%`: how a percentage is written belongs to the language, not to the
measurement.

---

## Documentation

The authoritative source is `docs/`, twenty specifications. The code
implements them; it never defines the architecture.

`CHANGELOG.md` records every decision together with the reasoning behind it,
including the ones that were later reversed and why. Its history is in
Italian, and is not translated; the fifteen decisions that shaped the project
are in English in [`docs/DESIGN-RATIONALE.md`](docs/DESIGN-RATIONALE.md), each
with the alternative that was not taken.

Start with `docs/00-Glossary.md`, then `docs/DESIGN-RATIONALE.md`.

---

## Contributing

The most useful contribution at this stage is **installing PIE and telling us
where the procedure fails**.

`CONTRIBUTING.md` describes the working method. In short: the documentation
comes before the code for anything costly to correct later — architecture,
data model, API contracts, scoring rules, and anything asserted to the person
using the tool. Everything else is implemented and then recorded.

Answers may be slow. This is a project built in the gaps of other work.

---

## Licence

**GNU General Public License, version 3.** Full text in `LICENSE.md`.

The copyleft follows from the promise this project makes: you must be able to
verify what the program does. A permissive licence would allow someone to ship
a closed version, with the same screens claiming that your domains never leave
your device, and no way for anyone to check.

Third-party components keep their own licences. Technitium DNS Server is
independent software: PIE uses only its public API and is not a fork of it.
