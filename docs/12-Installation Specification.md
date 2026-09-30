# 12 - Installation

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Installation Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the requirements and the process for installing the
Privacy Intelligence Engine.

The objective is a procedure that is simple, repeatable and independent of the
platform.

---

# Objectives

The installation must be:

* simple;
* guided;
* repeatable;
* safe;
* easy to update.

---

# Supported Platforms

The platforms supported are:

* Linux
* Windows
* Docker, provided for and not yet supplied (see Not Yet Provided)

Further platforms may be supported in later versions.

---

# Installation Modes

The following methods of installation are provided for.

## Standard Installation

Complete installation of the system.

It comprises:

* Backend
* Frontend
* Core
* REST API

---

## Docker Installation

Installation by container.

It comprises every component of the project.

---

## Development Installation

Installation intended for development.

It includes additional tools for debugging and testing.

---

# System Requirements

## Minimum

* Dual-core CPU
* 4 GB RAM
* 2 GB of space available
* A network connection

---

## Recommended

* Quad-core CPU
* 8 GB RAM
* SSD
* Gigabit connection

---

# Required Components

The system needs, in order to work.

* the Privacy Intelligence Engine
* at least one supported Data Source
* a modern browser
* HTTPS

---

# Installation Flow

```text id="jpruor"
Environment Check

↓

Dependency Check

↓

Component Installation

↓

Configuration

↓

Connection Test

↓

System Validation

↓

Ready
```

---

# Environment Validation

Before the installation these are checked.

* operating system;
* space available;
* permissions;
* network;
* dependencies.

---

# Initial Configuration

During the initial configuration these are defined.

* language;
* Data Source;
* connection parameters;
* network settings.

**First administrator.** A new installation has no account at all, and has
neither a default one nor a factory password. At startup the Backend shows a
**setup code** on standard output; entering it in the browser, with a username
and a password, creates the first `Administrator`. The proof of possession is
access to the machine.

Whoever loses access recovers it with a command run on the machine hosting the
installation, not remotely and not by mail.

The detail is in the Authentication Specification.

---

# Backend Registration

Every Data Source is registered through its Adapter.

The system checks connectivity automatically.

---

# Capability Detection

At the end of the registration the system detects **which capabilities the
Data Source is able to offer** in its current configuration.

The result is presented to the user before the installation completes.

For each missing capability the system says:

* which analyses will not be available;
* which intervention would make it available;
* what consequences that intervention carries.

---

# Optional Capabilities

Some capabilities require optional components of the Data Source.

When those components can be installed automatically, the system may propose
installing them during the guided configuration.

The proposal respects three rules.

**Explicit choice.** No optional component is installed without a decision of
the user.

**Symmetric information.** Advantages and costs are presented together. If
enabling it carries an increase in the consumption of resources, the recording
of additional data or an impact on performance, those aspects are declared
before the choice.

**Reversibility.** The user can refuse the proposal and complete the
installation all the same, or enable the capability at a later moment.

---

# Retention Configuration

When a capability depends on a component that records data with a retention
policy of its own, the system checks the consistency between that retention
and the frequency of acquisition configured.

A retention shorter than the interval of acquisition carries a **loss of data
that the backend does not signal**.

The system proposes consistent values and signals the condition when it
occurs.

This check is repeated at every change of the frequency of acquisition.

---

# Privacy Disclosure

When a capability carries the recording of additional data relating to the
activity of users, the installation declares explicitly:

* which data is recorded;
* where it resides;
* how long it is kept;
* which data PIE keeps and which stays in the Data Source.

The project adopts the principle of aggregating the datum at the moment of
acquisition and of not keeping the individual detail inside PIE.

This choice is to be communicated to the user, since it determines their
actual exposure.

---

# Security

During the installation:

* the initial configurations are generated;
* the certificates are verified;
* the credentials are protected;
* no account and no default password is created.

---

# Verification

At the end of the installation the system performs:

* verification of the Core;
* verification of the REST APIs;
* verification of the Adapters;
* verification of the Data Source.

---

# Update Process

Updating the system preserves:

* configurations;
* data;
* the Adapters installed.

The update does not modify the structure of the Unified Data Model.

---

# Backup

Before every update the system can create a backup of the configuration.

The backup comprises.

* settings;
* configuration;
* application data.

---

# Uninstallation

The removal procedure deletes:

* application components;
* services;
* temporary files.

The configurations can be kept at the user's request.

---

# Logging

Every phase of the installation is recorded.

The logs make the diagnosis of any problem easier.

---

# Error Handling

Every error is classified and presented with an understandable description.

---

# First Installation

This section defines the **first installation procedure**, the one required by
the Beta criterion: a person who is a stranger to the project installs PIE on
a clean machine by following the instructions.

It realises part of what the rest of the specification describes. What it does
not realise is listed in Not Yet Provided, and remains an objective.

---

## Package

PIE is distributed as a **ready-made package**, one per platform.

| Platform | Package                                      |
| -------- | -------------------------------------------- |
| Windows  | `tivustream-pie-<version>-win-x64.zip`        |
| Linux    | `tivustream-pie-<version>-linux-x64.tar.gz`   |

The package contains the program, the interface already compiled, the
installation scripts and the guide. **It requires neither .NET nor Node.js to
be installed**: the runtime is included in the program.

The package is produced by a script of the repository, in `installer/`, which
compiles the interface, publishes the Backend for each platform and creates
the archives.

The **version** of the product is a single one, starts at `0.1.0` and appears
in the name of the package, in the program and in the log at startup. It is
independent of the Documentation Release.

The program is called `tivustream-pie` (`tivustream-pie.exe` on Windows).

---

## Where Things Live

Program and data live in **two separate folders**. An update replaces the
first and does not touch the second.

| What                     | Windows                           | Linux                       |
| ------------------------ | --------------------------------- | --------------------------- |
| Program                  | `C:\Program Files\TivuStream PIE` | `/opt/tivustream-pie`       |
| Data and configuration   | `C:\ProgramData\TivuStream PIE`   | `/var/lib/tivustream-pie`   |

The **data folder** contains the database, the lists, the certificate, the
backup copies and `appsettings.Local.json`, which holds the token of the Data
Source.

It is readable **only by the service and by the administrators** of the
machine. On Linux it belongs to a dedicated system user, `tivustream-pie`,
with `0700` permissions.

The program receives the data folder at startup. The relative paths of the
configuration (`Storage:DatabasePath`, `Storage:ListDirectoryPath`,
`Transport:CertificateDirectory`) are resolved **against the data folder**,
not against the folder the program is launched from: a Windows service is
launched from `C:\Windows\System32`.

With no data folder given, as during development, the behaviour stays as it
is.

---

## Running As A Service

PIE runs as a **system service**: it starts when the machine is turned on,
even with nobody signed in, and observes the network without interruption.

| Platform | Mechanism | Identity |
| --- | --- | --- |
| Windows | Windows service `TivuStreamPIE`, automatic start | Virtual account `NT SERVICE\TivuStreamPIE`, without administrator privileges |
| Linux | systemd unit `tivustream-pie.service` | System user `tivustream-pie`, without a shell |

On Linux the unit limits what the service can touch: system files read-only,
writing only in the data folder, no acquisition of privileges.

Two official Microsoft dependencies are needed, which let the program behave
as a service:

| Entry | Value |
| --- | --- |
| Name | `Microsoft.Extensions.Hosting.WindowsServices`, `Microsoft.Extensions.Hosting.Systemd` |
| Purpose | Integration with the Windows service manager and with systemd |
| Licence | MIT |
| Maintenance | Microsoft, part of the .NET ecosystem |

Outside a service, neither changes anything.

---

## First Administrator Under A Service

A service has no window: what it writes on standard output ends up in the
system log, or nowhere. The **setup code** of the Authentication Specification
cannot therefore be shown, and writing it to the system log would put it where
it must not be.

For this reason, **when PIE runs as a service, the setup code is neither
generated nor shown.** The first administrator is created during the
installation with the `reset-password` command, which on an installation with
no account creates an administrator. The installation script runs it and asks
for a name and a password at the terminal.

The proof of possession stays the same: access to the machine, here with
administrator permissions.

---

## Commands

The program offers three terminal commands. None starts the service.

| Command | What it does |
| --- | --- |
| `configure` | Asks for the address and the token of the Data Source, tries the connection, says which capabilities are available and which are missing, and writes `appsettings.Local.json` into the data folder |
| `reset-password <name>` | Existing. On an installation with no account it creates the first administrator |
| `access` | Writes the addresses PIE answers at and the fingerprint of the certificate |

**`configure`** does not accept a configuration that does not work. If the
connection does not succeed it says why, in the words of the person (address
unreachable, token refused, permissions insufficient), and writes nothing. The
token is read without being shown on screen while it is typed, and never
appears in any message.

For each missing capability, `configure` says what will not be available and
what would make it available, as Capability Detection provides. In particular,
for `DomainActivity` it declares the consequence for privacy that Privacy
Disclosure provides for: enabling Query Logs makes **Technitium** keep every
single query, under Technitium's retention, while PIE goes on keeping only the
aggregate of them.

**`access`** exists because, under a service, the lines written at startup are
not seen. With no certificate generated yet, it says to start the service
first.

---

## Installation Script

One script per platform, to be run as administrator from the folder of the
extracted package: `install.ps1` on Windows, `install.sh` on Linux.

```text
Check         operating system, administrator permissions, space, free ports
↓
Copy          program into its folder
↓
Data          data folder with restricted permissions
↓
Connection    configure
↓
Access        reset-password, if no account exists
↓
Service       registration and start
↓
Firewall      rule for the HTTPS port, on private networks only
↓
Check         the service answers
↓
Ready         access: addresses and fingerprint
```

Every step says what it is doing. A step that fails stops the script with an
understandable sentence and says what has already been done.

**The firewall.** A service does not make the Windows firewall prompt appear:
without a rule it would stay unreachable from the other devices with nobody
saying so. The script adds a rule for the HTTPS port alone, on private
networks alone, and declares it. On Linux the script does not modify the
firewall: it says which port to open if one is active.

**If PIE is already installed**, the script updates it: stops the service,
replaces the program, restarts it. It does not repeat `configure` nor
`reset-password`, and does not touch the data folder. The backup copy before a
change of schema is made by the program itself (Persistence Specification).

---

## Uninstallation Script

`uninstall.ps1` and `uninstall.sh` stop and remove the service, the firewall
rule and the folder of the program.

**The data folder stays**, unless the person asks explicitly with an option
(`-RemoveData`, `--remove-data`). Before deleting it the script says what it
contains and asks for confirmation. The deletion is real.

---

## Installation Guide

The package contains a **guide**, written for a person who does not know the
project, in plain language.

It exists in two languages, `INSTALL.md` in English and `INSTALL.it.md` in
Italian, and each refers to the other at the top. The reason is that the
interface is bilingual and the people who arrive at the project may come from
an Italian page: a person installing in their own language reaches the one
step that cannot be got wrong, comparing the fingerprint of the certificate,
without having to translate it. Both are in the package.

The guide comprises:

* what is needed beforehand: a Technitium Data Source already working, and how
  to create a read-only user on it and its token;
* the installation, step by step, with what the script asks;
* how to open PIE from another device and compare the fingerprint, and what to
  do if it does not match;
* the system proxy and VPNs: PIE reaches Technitium directly, ignoring them;
* a dedicated browser profile on a shared computer, because the history keeps
  the addresses opened;
* how long PIE keeps the data, and the backup copies to delete after a
  successful update;
* how to update, how to uninstall, how to get access back.

PIE **does not install Technitium** and does not modify its configuration, as
established in Constraints.

---

## Field Tests

The procedure is verified in the field before it is declared built:

| Test | Where |
| --- | --- |
| Installation, update, uninstallation | Windows, on the development computer with the development backend stopped |
| Installation, update, uninstallation | Linux, in WSL or in a virtual machine |
| Access from another device | A phone or a laptop on the home network |
| A stranger installs by following the guide | A clean machine; it is the Beta criterion, and stays open until it happens |

---

# Not Yet Provided

The first procedure does not realise the following parts of the specification.
They remain objectives, and are to be declared as such.

| Part | State |
| --- | --- |
| Docker Installation | Not supplied. The Linux package covers the same use on a home server |
| Development Installation | It is the repository itself, with the commands of `CLAUDE.md` |
| Choice of language at installation | Not necessary: the interface is bilingual and follows the browser |
| The texts the scripts and the commands write at the terminal | English only. The guide, in both languages, says what each question asks and what to answer |
| Proposal to install optional components of the Data Source | Not supplied: `configure` says what is missing and how to obtain it, without installing it |
| Check of the retention of the Data Source against the frequency of acquisition | Not supplied at installation |
| Log of the installation phases to a file | The script writes to the terminal; no file |
| Configuration page in the browser | Not supplied: the configuration is done with `configure` |
| Other architectures (ARM, a Raspberry Pi for instance) | Not supplied in the first version |

---

# Design Principles

The installation follows these principles.

* simplicity;
* repeatability;
* safety;
* modularity;
* independence from the platform.

---

# Constraints

The installation procedure:

* does not modify the Data Sources;
* requires no modification to the backends supported;
* uses components official to the project alone.

---

# Related Specifications

* 04 - Technitium Integration
* 10 - Frontend
* 11 - Backend
* 13 - Roadmap
