# TivuStream Privacy Intelligence Engine (PIE)

Self-hosted platform that reads a DNS server, analyses what the network
contacts, and reports it honestly.

**Read `MASTER_PROMPT.md` first.** It holds the working method and the rules
that govern this repository. What follows is the short form.

---

## The point of this project

Most privacy tools show a number. This one shows a number **and what it rests
on**, and refuses to show it when it cannot support it.

If you are about to make the interface simpler by dropping a qualification,
stop. The qualification is the product.

---

## Non-negotiable

* The domains contacted by the network never leave the device. Classification
  is local. Logs carry counts, never network data.
* Credentials live only in `appsettings.Local.json`, which is gitignored.
* The system never claims more than it knows. Unmeasured is excluded, never
  scored as zero. Unclassified is never presented as safe.

---

## Where things are

```text
docs/           19 Specifications, the authoritative source
backend/        ASP.NET Core: Model, Core, Adapters, Storage, Api
frontend/       Vue 3, TypeScript, bilingual from the first line
scripts/        Helpers, including DNS traffic generation for testing
CHANGELOG.md    Every decision, with the reasoning
```

Start from `docs/00-Glossary.md` and the top of `CHANGELOG.md`. Read in full
only the Specifications your task touches.

---

## Commands

```powershell
dotnet build backend/TivuStream.Pie.sln
dotnet test  backend/TivuStream.Pie.sln          # 366 tests, all must pass
cd frontend && npm run test:unit -- --run        # 79 tests, all must pass

cd backend/src/TivuStream.Pie.Api && dotnet run   # serves on :5000; a fresh database
                                                  # prints a one-time setup code
cd backend/src/TivuStream.Pie.Api && dotnet run -- reset-password <name>
                                                  # restores access, from this machine
cd frontend && npm run dev                        # proxies /api to :5000
```

The Data Source is a Technitium DNS Server in Docker, named
`technitium-pie`, web interface on `:5380`, DNS on `:15353`.

Stop `dotnet run` before rebuilding: a running process locks the assemblies.

To generate DNS traffic for testing, since the host does not use this
resolver:

```powershell
docker run --rm --network container:technitium-pie `
  -v "<repo>/scripts/generate-dns-traffic.sh:/generate.sh" `
  busybox sh /generate.sh 10
```

---

## Process, in short

**Decisions** — architecture, data model, API contracts, scoring rules,
anything the person reads, new dependencies: Specification first, then
approval, then code.

**Everything else** — implement, then record in `CHANGELOG.md`.

Conventional choices with an obvious answer are decided, not asked about.

Delivering something incomplete is allowed when the gap is written down under
`Known Impact`. Debt that is not written down is a defect.

Every boundary crossed carries at least one test.

---

## Authentication

Specified in `docs/18-Authentication Specification.md`, milestones A1 to A7,
all done. Every endpoint requires a signed in account except `setup` and
`login`. A fresh instance prints a one-time setup code; `dotnet run --
reset-password <name>` restores access from this machine. A `Viewer` reads
aggregated data only. Passwords are refused over plain HTTP from another
machine, repeated failures are slowed down, requests that change data from
another `Origin` are refused, only the names in `AllowedHosts` are served
(loopback by default, `*` stops the start). No answer and no log line carries
a password, a session identifier, the setup code or a source address.

In the frontend `stores/session.ts` holds the five states. Backend tests
create accounts directly; requests in tests come from the loopback unless
they set `X-Test-Remote-Address`.

---

## Known debts

* No domain detail page in the frontend: per-device activity and
  `activityAccess` are not shown anywhere yet (F4 of Specification 18 waits
  for it).
* HTTP, not HTTPS. Until the Transport Security Specification exists and is
  implemented, a password can only be sent from this machine.
* No installation procedure: the `installer/` directory is empty.
* Tiered retention is designed in Specification 16 and not implemented; the
  database grows without limit.
* Device, Alert and Recommendation Engines do not exist. Device Health is
  therefore not measurable, and fifteen points of the score stay out.
* `/devices` and `/statistics` do not declare the period they refer to, a rule
  already applied to `/domains`.
* Documentation is in Italian and is to be translated before publication.
* No licence header in source files, no licence notice in the interface.

---

## Language

Speak Italian to the person working on this project.

Write code, identifiers and commit messages in English.
