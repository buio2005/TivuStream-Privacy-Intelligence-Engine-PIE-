# 18 - Authentication

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Authentication Specification

**Version:** 1.6.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines **who may question the Privacy Intelligence Engine
and what they may read**.

It closes the gravest declared debt of the project. Before it, the API
answered anyone who reached it, and was safe only because it listened on the
loopback address alone — a circumstance, not a measure.

The API Specification went no further than saying that "the system supports
centralised authentication" and that "the implementation is defined during the
development of the backend". It never was. This document defines it **before**
the code, because it is a decision whose correction, once users and data
exist, costs dearly.

---

# Why The Stakes Are Higher Than For A Dashboard

What PIE keeps and shows is, by construction, aggregated: there is no register
of individual queries. It remains sensitive all the same.

* **Activity per device** says which domains each device in the house has
  contacted, and how many times. It is the most delicate information the
  system exposes, and the Persistence Specification calls it, in its raw form,
  the browsing history of every device.
* **The state of the configuration** says what the network does not protect:
  DNSSEC off, no encrypted transport, no filter list. To whoever wants to
  attack it, that is a list.
* **The score** and its reasons describe the network in a way useful to anyone
  watching it.

An API open on a home network, where devices nobody has examined live side by
side, is not a theoretical risk.

---

# Threat Model

## Adversaries Considered

| Adversary | Example | What is to be prevented |
| --- | --- | --- |
| Another device on the network | A compromised appliance, a guest on the Wi-Fi | Reading data or configuration without being the user |
| Someone in the house without administration rights | A member of the family | Reading the activity of other people's devices |
| A web page open in the administrator's browser | A malicious site attempting requests towards the local address, including through DNS rebinding | Using the user's browser as the way in |
| Whoever guesses the credentials | Repeated attempts at the sign-in form | Getting in by trying |
| Whoever obtains the database file | A backup copy left lying about | Recovering the passwords of the accounts |

## Not Considered, And Declared

* **A compromised host.** Whoever has the privileges of the user running PIE
  already has the database, the configuration files and the memory.
* **Encryption of the database at rest.** It contains aggregated network data.
  Encrypting it requires a key management the project does not have, and the
  protection obtained without one would be apparent only.
* **Multi-factor authentication.** See Known Limits.

---

# Principles

Six rules. Every requirement below follows from them.

1. **Nothing answers without an identity.** Access is denied by default. A new
   endpoint is protected without anyone having to remember it.
2. **The absence of protection is not a silent state.** There is no setting
   that turns authentication off. A configuration that disabled it would be
   the first one somebody leaves on for convenience.
3. **A secret is never kept in the clear nor written to a log.** This holds
   for passwords, session identifiers and the setup code.
4. **A refusal teaches nothing to whoever receives it.** An account that does
   not exist and a wrong password produce the same answer.
5. **Local First.** No external identity service, no messages sent, no
   telemetry. Recovering a lost access goes through access to the machine.
6. **What is withheld by role is declared.** An answer that omits a datum
   because whoever asks has no right to it says so. An empty list for want of
   a right and an empty list because nothing happened are two different
   statements, and presenting them alike is false information: it is the
   Absent Versus Unmeasurable rule of the Network Privacy Specification,
   applied to permissions.

---

# Scope

**It covers.**

* local accounts with a role;
* credentials, sessions and recovery;
* the initial setup of the first installation;
* the check of permissions on every endpoint;
* the screens for sign in, initial setup and account management;
* basic countermeasures against repeated attempts and requests from other
  sites.

**It does not cover.**

* **The certificate and the HTTPS configuration.** They are the subject of a
  separate specification (Transport Security), required by the Beta criteria.
  This specification fixes only what depends on the transport: credentials do
  not travel in the clear outside the loopback.
* **Tokens for services and automations.** The Backend Specification mentions
  "authorised users and services", but no service questions the API: the only
  client is the Frontend. Providing for them now would be designing for a need
  that does not exist. See Decisions Taken, D5.
* **Authentication towards the Data Sources.** It stays as in the Technitium
  Integration Specification: a non-expiring API Token, in
  `appsettings.Local.json`. The authentication of users is independent of that
  of the sources.

---

# Accounts And Roles

An **account** is a person who uses PIE. It belongs to the Backend and not to
the Unified Data Model, which describes the network observed and not whoever
observes it.

| Property | Rule |
| --- | --- |
| `username` | From 3 to 32 characters among lowercase letters, digits, full stop, hyphen and underscore. The comparison ignores case |
| `role` | `Administrator` or `Viewer` |
| `enabled` | A disabled account cannot sign in, and its sessions fall |
| `passwordChangeRequired` | True at creation and after a reset by an administrator |

## Roles

| Role | May |
| --- | --- |
| `Administrator` | Everything: read every datum, manage the accounts |
| `Viewer` | Read the **aggregated** data: score, statistics, domains, system state. Does not read the data identifying a single device, nor its activity |

The distinction has a single reason, declared in the principles: activity per
device is the most sensitive datum, and in a house whoever looks at the
overall picture must not be able to see what each person did. The cost is
declared alongside the advantage, as the Installation Specification requires:
a `Viewer` in the family sees that a tracking domain was contacted, not from
which device.

## Constraint

There is always at least one active `Administrator`. The last one cannot be
disabled, removed or demoted. The recovery provided for (see Recovery) exists
for the cases that slip through, not to make this constraint negotiable.

---

# Credentials

## Password

* Length from **12 to 128** characters.
* **No composition rule.** Requiring capitals, digits and symbols produces
  passwords that are predictable and harder to remember, not safer. Length is
  what counts.
* It cannot coincide with the username.
* It is normalised into Unicode NFKC form before every use, so that the same
  password typed on different keyboards gives the same result.

## Keeping

Of the password only a **salted hash** is kept, never the password nor
anything from which it could be obtained directly.

| Parameter | Value |
| --- | --- |
| Algorithm | PBKDF2 with HMAC-SHA-512 |
| Iterations | 210,000 |
| Salt | 16 random bytes per password, from a cryptographic generator |
| Output | 64 bytes |
| Recorded format | Algorithm, iterations, salt and hash together, so that the parameters can grow |

At every successful sign in, if the recorded parameters are lower than those
in force, the password is recomputed with the new ones.

The comparison happens in constant time. For an account that does not exist a
computation is performed all the same, on a fictitious value, so that the
response time does not reveal whether the name exists.

The algorithm is the one the base library of .NET offers with no additional
dependency. Argon2id resists better whoever has graphics cards at their
disposal, but requires a third-party library. See D3.

---

# Sessions

After a successful sign in the Backend creates a **session** and communicates
it to the browser with a cookie.

| Aspect | Rule |
| --- | --- |
| Identifier | 32 random bytes from a cryptographic generator, never derived from data of the account |
| Cookie | `HttpOnly`, `SameSite=Strict`, path `/api`, no `Domain` attribute. `Secure` when the connection is encrypted |
| Keeping | Only the **SHA-256 hash** of the identifier is kept in the database. Whoever read the database could not use a session |
| Inactivity | Expires after 8 hours without requests |
| Maximum duration | Expires in any case after 14 days |
| Renewal | The identifier changes at every sign in |

The browser never handles the credential, nor a token in code the page can
execute: the `HttpOnly` cookie is not readable by script.

A session falls when: the user signs out, the account is disabled or removed,
the password changes (every other session of the account), it expires.

The role is not copied into the session: it is read from the account at every
request. A demotion takes effect immediately.

---

# First Run

A new installation has no accounts. Whoever reaches the address first must not
be able to become its administrator simply by arriving before the owner.

1. At startup, if no account exists, PIE enters the **`SetupRequired`** state
   and generates a random **setup code** of 12 characters, shown in groups of
   four.
2. The code is **written to standard output, by a direct write**, and does not
   pass through the logging system. It therefore reaches no log file, nor any
   collector that system feeds.
3. In the `SetupRequired` state every request, except the initial setup,
   receives `401` with code `SetupRequired`.
4. `POST /api/v1/setup` receives code, username and password, and creates the
   first `Administrator`. It succeeds once only: the operation is atomic, and
   a second simultaneous request fails.

   The code is checked first. A username or a password that does not conform
   is refused **without consuming the code**: whoever mistypes must not have
   to ask for another by restarting the service. The code is accepted in any
   combination of upper and lower case, with or without hyphens and spaces.
5. The code is never kept: it exists in memory in the form of a hash and is
   regenerated at every startup until it is used. Wrong attempts fall under
   the same limits as signing in.

The proof of possession is therefore **access to the machine**: whoever reads
the standard output of the process is, by definition, whoever administers the
host. In a container it is the output of `docker logs`, and this is a declared
limit.

**Under a system service the code does not exist.** The standard output of a
service ends up in the system log, or nowhere: showing the code there would
breach point 2, and not showing it would leave the installation with no way
in. When PIE runs as a service, it does not enter the `SetupRequired` state
with a code: the first `Administrator` is created with the recovery command,
which on an installation without accounts creates one (Installation
Specification, First Administrator Under A Service). Until one exists, every
request receives `401` with code `SetupRequired`, and `POST /api/v1/setup`
refuses any code at all.

---

# Recovery

There is no "I forgot my password" over the network and there is no mail
message: those are external services, and a recovery channel is a second way
in.

Recovery is **a command run on the machine hosting PIE**, from the same
executable, which:

* asks for the new password at the terminal;
* sets it on the account named and puts `passwordChangeRequired` back on it,
  except when it creates the first account of an installation that has none:
  that password was just chosen by whoever will use it, and changing it at
  once protects nothing;
* makes every session of that account fall;
* may, if no active `Administrator` exists, re-enable or create one.

The rules on the name given:

| Situation | Effect |
| --- | --- |
| An active `Administrator` exists and the name is that of an account | The password of that account is reset, whatever its role |
| An active `Administrator` exists and the name is **not** that of an account | Refused. A typing error must not create an account |
| No `Administrator` is active and the name is that of a disabled `Administrator` | It is re-enabled and its password reset |
| No `Administrator` is active and the name does not exist | An `Administrator` is created |
| No `Administrator` is active and the name is that of a `Viewer` | Refused. The command restores an administrator, it does not promote one |

The new password is subject to the usual rules.

Whoever can run it already has access to the database. Recovery grants nothing
that access to the machine does not grant already. The exact form of the
command is an implementation detail.

---

# Transport

Credentials are accepted **only over an encrypted connection, or when the
client is on the loopback**. Otherwise the answer is `403` with code
`TransportNotSecure`.

A credential is **any password**: `setup`, `login`, changing one's own
password, creating an account (`POST /accounts`) and resetting one (`PATCH
/accounts/{username}` with `password`). The initial password chosen by an
administrator is a password like the others (D9).

The check comes before every other: a request refused for the transport is not
evaluated and does not count as an attempt. An unknown remote address is not
the loopback.

The criterion is the remote address of the connection, not the `Host` header,
which whoever writes the request controls.

The session cookie carries `Secure` when the connection is encrypted. On the
loopback in the clear it does not: some browsers refuse it on an unencrypted
address, even a local one.

The `AllowedHosts` setting must list the names the installation is reachable
by. With `*`, a malicious site that remaps its own name onto the local address
(DNS rebinding) reaches the API with the administrator's browser.

The default value is `localhost;127.0.0.1;[::1]`, the names of the loopback
alone, and it holds when the setting is missing too: a line absent from a
configuration file must not open anything. Whoever reaches the installation by
another name adds it in `appsettings.Local.json`. With the HTTPS channel open,
the name and the addresses of the computer are added automatically (Transport
Security Specification, Names Accepted): a site remapping its own name onto
the local address carries none of these in the `Host` header. The value `*`
**prevents startup**, with a message saying why: it is exactly the setting
that turns a protection off, forbidden by the second principle. A request with
a `Host` not listed receives `400` from the filter of the framework, with no
body: it is not addressed to this service.

`Strict-Transport-Security` is sent only with a certificate supplied by the
operator (Transport Security Specification, Strict Transport Security). With
the certificate generated by PIE, a browser that had received it would no
longer let the warning be accepted, and at the first renewal PIE would become
unreachable from that device.

## Origin

A request that modifies data (every method except `GET`, `HEAD`, `OPTIONS` and
`TRACE`) with an `Origin` different from the scheme, name and port of the
request itself receives `403 OriginNotAllowed`. This holds for `setup` and
`login` too. `Origin: null` is different from any address.

A request **without** `Origin` is accepted. Browsers send it on every request
that modifies data; whoever does not send it is a client that is not a
browser, and carries nobody's session with it.

How the encrypted connection is obtained, and how a proxy terminating it in
front of PIE is treated, is defined by the Transport Security Specification.

---

# Attempts

Against whoever tries passwords repeatedly.

| Counter | Threshold | Effect |
| --- | --- | --- |
| Per source address | 5 failures | Refusal `429` with `TooManyAttempts` and `Retry-After` |
| Per account | 10 failures | The same refusal |

The delay starts at 30 seconds and **doubles** at every further failure, up to
a maximum of **15 minutes**. A successful sign in resets the counter of the
account. The counters live in memory and reset at restart.

What a failure is:

| Request | Counters |
| --- | --- |
| `login` refused with `AuthenticationFailed` | Address and account |
| `setup` refused with `SetupCodeRejected` | Address |
| A change of one's own password refused with `CurrentPasswordRejected` (D10) | Address and account |

A username or a password that does not conform is not a failure: it puts no
secret to the test. A request refused with `429` is not evaluated and does not
count. The right current password, in a password change, resets the counter of
the account as a successful sign in does.

Rules of the counters:

* **During the delay the credentials are not checked**, not even when they are
  right: otherwise the delay would slow nobody down.
* The counter of the account is indexed by the **name attempted**, whether the
  account exists or not. Slowing down only the names that exist would reveal
  which ones do.
* An IPv6 address counts by its **/64 prefix**, which is what a home network
  assigns to a single device: counting it by address would leave anyone
  billions of counters. An IPv4 address written as IPv6 counts as IPv4.
* **Forgetting (D11).** A counter with no failures for 15 minutes **after the
  end of its delay** resets. Counting from the last failure would make
  forgetting coincide with the end of the longest delay, and whoever persists
  would find the counter empty exactly when they come back to try. This way an
  occasional mistake is forgotten, a persistent adversary stays slowed down,
  and the memory does not grow without bound.

There is no permanent block: it would make it possible for anyone to lock the
owner out. The ceiling of 15 minutes is the measure of how much an adversary
can slow the legitimate administrator down, and it is declared as a
compromise.

The source addresses serve the counters and are recorded nowhere.

---

# API Contract

Every answer uses the common structure of the API Specification.

| Method and path | Who | Effect |
| --- | --- | --- |
| `POST /api/v1/setup` | Nobody, only in `SetupRequired` | Creates the first `Administrator` and opens the session |
| `POST /api/v1/auth/login` | Nobody | Opens the session |
| `POST /api/v1/auth/logout` | Session | Closes the session |
| `GET /api/v1/auth/session` | Session | Returns username, role, expiry and `passwordChangeRequired` |
| `POST /api/v1/auth/password` | Session | Changes one's own password |
| `GET /api/v1/accounts` | `Administrator` | Lists the accounts |
| `POST /api/v1/accounts` | `Administrator` | Creates an account with an initial password |
| `PATCH /api/v1/accounts/{username}` | `Administrator` | Changes role, enablement, or resets the password |
| `DELETE /api/v1/accounts/{username}` | `Administrator` | Removes the account |

`setup` and `login` are the **only** endpoints reachable without credentials,
because they are the way to obtain them. Neither returns any datum about the
network or the system.

## Errors

| Status | Code | When |
| --- | --- | --- |
| 401 | `SetupRequired` | No account exists yet |
| 401 | `AuthenticationRequired` | Session absent, unrecognised or expired |
| 401 | `AuthenticationFailed` | Sign in refused. Identical for a name that does not exist, a wrong password and a disabled account |
| 403 | `Forbidden` | The role is not enough |
| 403 | `PasswordChangeRequired` | The password must be changed before anything else |
| 403 | `TransportNotSecure` | Credentials over a connection in the clear that is not local |
| 403 | `OriginNotAllowed` | A request modifying data with an `Origin` different from the address of the service |
| 409 | `LastAdministrator` | The operation would leave the installation without an administrator |
| 409 | `AccountExists` | Name already used |
| 401 | `SetupCodeRejected` | The setup code is absent or wrong. Identical in the two cases |
| 409 | `SetupAlreadyCompleted` | An account exists already: the initial setup is no longer available |
| 422 | `UsernameRejected` | Name does not conform |
| 422 | `PasswordRejected` | Password does not conform, with the reason: `TooShort`, `TooLong`, `EqualsUsername`, `Unchanged` |
| 422 | `RoleRejected` | A role other than `Administrator` and `Viewer`, or absent in the creation of an account |
| 403 | `CurrentPasswordRejected` | In a change of one's own password, the current password is not the right one |
| 404 | `AccountNotFound` | No account has the name given in the path |
| 429 | `TooManyAttempts` | See Attempts |

The reason of `PasswordRejected` travels in the `reason` field of the error
answer and not in the text: the interface translates it into the language of
whoever reads.

Refusals never carry a `WWW-Authenticate` of type `Basic`: it would make the
credentials window of the browser appear in place of the screen of the
application.

`AuthenticationRequired` does not distinguish an expired session from one
never opened: it is not information the Backend has to supply. The Frontend
knows by itself whether the user had signed in.

While `passwordChangeRequired` is true, every endpoint except `auth/session`,
`auth/password` and `auth/logout` answers `PasswordChangeRequired`. This holds
where the role would not be enough either: the password to change comes before
anything else, and saying `Forbidden` to someone who can do nothing yet would
be a pointless indication.

## Requests And Answers

| Endpoint | Body | Successful answer |
| --- | --- | --- |
| `POST /auth/password` | `currentPassword`, `newPassword` | `200`, the session as `auth/session` describes it |
| `GET /accounts` | — | `200`, the list of the accounts |
| `POST /accounts` | `username`, `role`, `password` | `201`, the account created |
| `PATCH /accounts/{username}` | `role`, `enabled`, `password`, each optional | `200`, the account as it is after the change |
| `DELETE /accounts/{username}` | — | `200` |

An account, in the answers, is `username`, `role`, `enabled`,
`passwordChangeRequired` and `createdAt`. The hash of the password never
leaves the Backend.

## Changing One's Own Password

* It requires the **current password**. A session left open on a browser must
  not be enough to take over the account.
* A wrong current password answers `403 CurrentPasswordRejected`, **not**
  `401`: the Frontend reads a `401` as a session ended, and the session is
  instead valid.
* The new password follows the usual rules and moreover **cannot coincide with
  the current one** (`Unchanged`). Without this rule the obligation to change
  it after a reset would be got round by putting the same one back, and the
  administrator would go on knowing it.
* On a successful change `passwordChangeRequired` becomes false, the session
  it was done from stays open, **every other** session of the account falls.

## Account Management

* An account created by an administrator is born with
  `passwordChangeRequired` true.
* The name follows the rules of Accounts And Roles, and the role is written by
  name: `Administrator` or `Viewer`.
* In `PATCH` the fields present apply **all or none**. A body with no fields
  changes nothing and returns the account.
* `password` in `PATCH` is a reset: `passwordChangeRequired` goes back to true
  and **every** session of the account falls, including that of whoever
  performs it if the account is their own.
* `enabled: false` makes every session of the account fall.
* An administrator acts on their own account as on another's. The only limit
  is the constraint of the last administrator.
* The constraint is checked **in the same transaction** as the change. Two
  administrators disabling each other at the same instant must not be able to
  leave the installation without either of them.

## Headers

* Every authenticated answer carries `Cache-Control: no-store`: it contains
  network data and must not stay in the cache of the browser nor of an
  intermediary.
* Requests that modify data check `Origin`, besides `SameSite=Strict`. See
  Transport, Origin.
* No page served by PIE can be put into a frame of another site: every answer
  carries `Content-Security-Policy: frame-ancestors 'none'`.
* A `429` refusal carries `Retry-After` in whole seconds, rounded up.

---

# Authorization

Every endpoint **declares the minimum role** it requires. An endpoint that
does not declare one requires `Administrator`: forgetting a declaration
produces a refusal, never an opening.

| Endpoint | Minimum role |
| --- | --- |
| `/health` | `Viewer` |
| `/statistics` | `Viewer` |
| `/npss` | `Viewer` |
| `/domains` | `Viewer` |
| `/domains/{domain}` | `Viewer`, with the activity withheld (see below) |
| `/devices` | `Administrator` |
| Account endpoints | `Administrator` |

## What Is Withheld Is Declared

`/domains/{domain}` returns `activityAvailable`, a boolean, and the list
`activities`. For a `Viewer` the list would be empty, and empty for want of a
right would be indistinguishable from empty because no device contacted the
domain.

The field becomes **`activityAccess`**, with three values:

| Value | Meaning |
| --- | --- |
| `Available` | The source offers the activity and whoever asks may read it. `activities` is the list |
| `Unavailable` | The source does not offer the activity. `activities` is empty and means nothing |
| `Withheld` | The source offers it, but the role of whoever asks does not include it. `activities` is empty and means nothing |

The Frontend shows the third case as such: **not** as an absence of activity
and **not** as an error.

---

# Frontend

The Frontend is born bilingual: every text below exists in Italian and in
English in the catalogue.

## States

The application is always in one of these states, distinct from one another:

| State | When |
| --- | --- |
| `Checking` | At opening, while it asks whether a session exists |
| `SetupRequired` | The Backend answers `SetupRequired` |
| `Unauthenticated` | No session |
| `Authenticated` | Valid session |
| `PasswordChangeRequired` | Valid session, password to be changed |

A `401` received while the user was `Authenticated` leads to `Unauthenticated`
with a message saying the session has ended. It is different from the failure
of a sign in just attempted.

## Screens

Initial setup, sign in, password change, account management (`Administrator`
only).

## Rules

* **No network datum survives signing out.** When the session closes the
  stores empty every datum read. Another user of the same browser must not
  find what the previous one was seeing.
* The password never appears in an address, is never put into `localStorage`
  nor into any other persistent memory. There is no "remember me" beyond the
  duration of the session.
* The three situations **are not confused**: wrong credentials, engine
  unreachable, connection not secure. Saying "credentials incorrect" when the
  engine did not answer is a false statement.

## Messages

| Code | Italian | English |
| --- | --- | --- |
| `auth.failed` | Nome utente o password non corretti. | Username or password incorrect. |
| `auth.engineUnreachable` | Il motore non ha risposto. Le credenziali non sono state verificate. | The engine did not answer. Your credentials were not checked. |
| `auth.sessionEnded` | La sessione è terminata. Accedi di nuovo. | Your session has ended. Sign in again. |
| `auth.tooManyAttempts` | Troppi tentativi. Riprova fra {seconds} secondi. | Too many attempts. Try again in {seconds} seconds. |
| `auth.notSecure` | Le credenziali non possono essere inviate su una connessione non cifrata. Apri l'indirizzo in HTTPS. | Credentials cannot be sent over an unencrypted connection. Open the address over HTTPS. |
| `domains.activityWithheld` | L'attività per dispositivo è visibile solo agli amministratori. Questo non dice se qualche dispositivo abbia contattato il dominio. | Activity per device is visible to administrators only. This says nothing about whether any device contacted the domain. |

`auth.failed` says neither whether the name exists, nor whether the account is
disabled. `auth.engineUnreachable` exists because an engine that does not
answer and a wrong password are two different facts. The last message takes up
the rule of the principles: a refusal must not be able to pass for an absence.

The reasons of `PasswordRejected` and the other errors each have an entry in
the catalogue, with the same parity between the two languages the catalogue
already verifies.

---

# Persistence

It adds two logical entities to the Persistence Specification, internal to the
Backend:

* **account**: username, role, enabled, hash of the password with its
  parameters, `passwordChangeRequired`, instant of creation;
* **session**: hash of the identifier, account, creation, last use, expiry.

Expired sessions are deleted. Migrations follow the rules already in force.

Added to What Is Never Persisted: **passwords in the clear, session
identifiers in the clear, the setup code**.

---

# Logging

The logs carry counts and outcomes, never network data and never secrets.

| Event | Content |
| --- | --- |
| Initial setup completed | The event and the name of the administrator created |
| Successful sign in | The name of the account |
| Failed sign in | The event and a **count**: the refusals since the service started. Not the name attempted: it could be a password typed into the wrong field |
| Operation on accounts | Who performed it, on which account, which operation. It includes a change of one's own password; a change carrying more than one records them one by one |
| Limit reached | The event, the kind of counter (address or name) and the delay. Not the origin nor the name |

These never appear: passwords, session identifiers, the setup code, source
addresses.

The rule holds for **every** log of the process, including those of the
framework at any level of detail, and for every answer, including those to an
unforeseen fault: the diagnostic page of the framework, which reports the
headers of the request and therefore the session cookie, is never shown. See
API Specification, Error Handling.

---

# Verification

Every commitment is **verifiable**, and each corresponds to at least one test,
as the MASTER_PROMPT requires.

## Backend

| # | Commitment | Test |
| --- | --- | --- |
| V1 | Nothing answers without an identity | **Every** endpoint of the host is enumerated. Without a session each answers `401`, except `setup` and `login`, which answer only according to this specification |
| V2 | Refusal is the default | An endpoint is registered with no role declaration: it requires `Administrator` |
| V3 | A refusal teaches nothing | A name that does not exist, a wrong password and a disabled account produce the same answer |
| V4 | Secrets are not kept in the clear | Neither the password nor the session identifier appears in the database |
| V5 | A session falls when it must | After signing out, disabling, a password change and expiry, the old identifier is refused |
| V6 | What is withheld is declared | A `Viewer` does not read `/devices` and obtains `Withheld`, not an empty list, on `/domains/{domain}` |
| V7 | An administrator always exists | The last one cannot be disabled, removed or demoted |
| V8 | Repeated attempts are slowed down | After the threshold `429` is obtained; a successful sign in resets; the delay has the declared ceiling |
| V9 | Credentials only over a suitable channel | Over a connection in the clear that is not local, every request carrying a password answers `TransportNotSecure` |
| V10 | The initial setup happens once | Without a code or with a wrong one it fails; with the right code it succeeds; the second request fails |
| V11 | No secret leaves | No answer and no log line contains a password, a session identifier or the code |
| V12 | No cache | Authenticated answers carry `Cache-Control: no-store` |
| V13 | Requests from other sites | A request modifying data with a foreign `Origin` is refused |
| V14 | Allowed hosts | A request with a `Host` not listed is refused |
| V15 | Updated parameters | A successful sign in with old hash parameters recomputes them |

The existing test "no answer contains the credential of the data source"
remains and extends to the new secrets.

## Frontend

| # | Commitment | Test |
| --- | --- | --- |
| F1 | Distinct states | Wrong credentials, engine unreachable, session ended and connection not secure produce four different messages |
| F2 | Nothing survives signing out | After signing out the stores contain no network data |
| F3 | The refusal message does not distinguish | The same text for every reason of `AuthenticationFailed` |
| F4 | What is withheld is not an absence | `Withheld` is presented neither as an empty list nor as an error |
| F5 | Parity of the catalogue | Every new entry exists in both languages with the same placeholders |

---

# Implementation Milestones

A feature of this scope is not built in a single step. Each milestone can be
verified on its own. All completed on 2026-09-26.

| # | Content | Tests |
| --- | --- | --- |
| A1 | Accounts, hash, persistence, migration | V4, V15 |
| A2 | Sessions, sign in and out, refusal by default, role per endpoint | V1, V2, V3, V5, V12 |
| A3 | Initial setup and recovery | V10 |
| A4 | Roles, account management, what is withheld | V6, V7 |
| A5 | Attempts, transport, `Origin`, `Host` | V8, V9, V13, V14 |
| A6 | Frontend: states, screens, catalogue | F1–F3, F5. F4 is verified with the domain detail page, which the Frontend does not have yet |
| A7 | Logging without secrets; answers to faults in the common structure | V11 |

The existing API tests had to obtain a session: their test factory creates
one.

---

# Consequences For Other Documents

Applied in Documentation Release 1.6.0.

| Document | Change |
| --- | --- |
| 06 - API | The Authentication section refers to this specification; endpoints, error codes; `activityAvailable` becomes `activityAccess` |
| 11 - Backend | The Authentication and Authorization sections refer to this specification |
| 12 - Installation | The initial setup replaces the entry "authentication"; recovery is described |
| 10 - Frontend | Screens and states; the point about security |
| 16 - Persistence | The two entities; What Is Never Persisted |
| 00 - Glossary | Account, Role, Session, Setup Code |
| 13 - Roadmap | See below |
| README, CLAUDE.md | The debt about authentication leaves the list when the last milestone is closed |

## Inconsistency Corrected In The Roadmap

The Beta criterion says "**No endpoint** answers without valid credentials".
Taken literally it is incompatible with the existence of a sign-in form, which
by definition answers whoever has no credentials yet.

Reformulation adopted: *no endpoint returning data about the network or the
system answers without valid credentials; the only ones reachable without are
those that establish them, `setup` and `login`, and they return nothing about
the network.*

---

# Decisions Taken

Product choices, approved on 2026-09-19 together with the messages of the
Frontend. For each, the alternative not chosen and what it cost are recorded,
so that whoever reopens it one day knows what it was decided not to do.

| # | Decision | Adopted | Alternative not chosen and its cost |
| --- | --- | --- | --- |
| **D1** | Access model | **Local accounts with password and session** | *A single shared secret*, in a configuration file: simpler to build, but no distinction between people, no way to revoke access for one alone, and the secret sits in the clear in a file |
| **D2** | Roles and visibility per device | **Two roles**; the `Viewer` does not see the data per device | *A single role*: fewer states and fewer screens, but anyone who signs in sees the activity of each device |
| **D3** | Hash algorithm | **PBKDF2-SHA-512, with no dependency** | *Argon2id*: more resistant to whoever uses graphics cards, but introduces a third-party library and requires your approval as a new dependency |
| **D4** | Recovery | **From the machine only**, with a local command | *A channel by mail or over the network*: convenient, but requires an external service and opens a second way in |
| **D5** | Tokens for services | **Deferred** until a service exists that uses them | *Providing for them now*: adds a surface to protect for a need that does not exist |
| **D6** | Values: session 8 hours of inactivity and 14 days at most, delay from 30 seconds to 15 minutes, password from 12 characters, 210,000 iterations | **Those written here** | They are thresholds: they can change without touching the structure. A longer session is more convenient and leaves more time to a browser left open |
| **D7** | Reformulation of the Beta criterion in the Roadmap | **Yes**, as proposed | Leaving it as it is keeps a criterion no implementation can satisfy literally |
| **D8** | A new password equal to the current one, approved on 2026-09-25 | **Refused**, with `Unchanged` | *Accepting it*: no further rule, but the obligation to change becomes an invitation and the administrator goes on knowing the password they assigned |
| **D9** | Transport of passwords, approved on 2026-09-26 | **Every** request carrying a password, including the creation and the reset of an account | *Only `setup`, `login` and password change*: the initial password chosen by the administrator would travel in the clear over the local network |
| **D10** | A wrong current password in a password change, approved on 2026-09-26 | **Counts** as a failure, per address and per account | *Not counting it*: whoever finds a session open would try passwords without limit |
| **D11** | Forgetting of the counters, approved on 2026-09-26 | **15 minutes** without failures after the end of the delay | *24 hours*: harsher on whoever errs by mistake. *Only at restart*: whoever errs five times over months stays slowed down for ever, and the memory grows with every name attempted |

---

# Known Limits

Declared, not hidden.

* **No multi-factor authentication.** A second factor requires a channel the
  project does not have, or a scheme of temporary codes that is another
  specification. The minimum length of the password and the slowing of
  attempts are the protection present.
* **The database is not encrypted.** Whoever copies it reads the aggregated
  network data, not the passwords.
* **A compromised host defeats everything.**
* **The attempt counters reset at restart.**
* **The setup code is readable by whoever reads the output of the process.**
  In a container, by whoever runs `docker logs`.
* **The administrator knows the initial password** they assign to an account,
  until the person changes it. `passwordChangeRequired` makes the change
  compulsory, it does not remove the window.
* **Two roles only.** There is no permission per section nor per device.
* **No persistent register of sign-ins.** The events exist in the application
  log, not as a queryable history.
* **PBKDF2 resists less than Argon2id** an adversary with dedicated hardware.
  See D3.
* **Encrypted transport is not defined here**, but in the Transport Security
  Specification, built on 2026-09-26. From the other devices access is over
  HTTPS; `login` in the clear outside the loopback goes on answering
  `TransportNotSecure`.

---

# Related Specifications

* 02 - Architecture
* 06 - API
* 09 - Network Privacy
* 10 - Frontend
* 11 - Backend
* 12 - Installation
* 13 - Roadmap
* 16 - Persistence
