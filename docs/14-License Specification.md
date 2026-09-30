# 14 - License

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** License Specification

**Version:** 2.0.0

**Status:** Approved

**Last Updated:** 2026-09-30

---

# Purpose

This specification defines the licensing policy of the **TivuStream Privacy
Intelligence Engine (PIE)** and sets out the guidelines for the use of
third-party software, libraries and components.

This document is a design specification and is not the legal text of the
licence of the project.

---

# Objectives

The licensing policy has the following objectives.

* to guarantee transparency;
* to ensure that dependencies comply;
* to keep the project maintainable;
* to preserve compatibility with third-party components.

---

# Project Licence

The project is distributed under the **GNU General Public License, version
3**.

The full text is in `LICENSE.md`, at the root of the repository, reproduced
without modification.

---

## Why Copyleft

The central promise of PIE is not that it blocks trackers. It is that **the
person can verify what the program does**.

Every choice in the project serves that promise: classification that happens
only on the device, the declared age of the lists, coverage made explicit, the
score withheld when there is not enough to measure.

A permissive licence would allow someone to distribute a modified and closed
version, with the same screens claiming that domains never leave the device,
**with no way for anyone to check**. The promise would survive as text and
disappear as fact.

Copyleft requires whoever distributes a modified version to publish its source.
The licence thereby becomes the technical guarantee of what the Specifications
state in words.

The cost is accepted: some of this code will not be reused in proprietary
products. That is precisely what the choice intends.

---

## Why Version 3 And Not 2

Two reasons.

The `SQLitePCLRaw` dependency is distributed under Apache-2.0, which is
compatible with GPLv3 and **not** with GPLv2. Adopting version 2 would make the
project undistributable together with its own dependencies.

Version 3 also addresses patents and the technological measures that prevent
someone from running a modified version on their own device, a condition that
matters for a program meant to be self-installed.

---

## Why Not AGPL

The AGPL extends the obligation to publish to whoever offers the modified
program as a network service without distributing it.

PIE is designed to be installed, not offered as a service: the case the AGPL
closes contradicts the Local First principle of the project itself.

The AGPL also creates adoption friction in organisations whose internal policy
forbids it.

The choice is recorded as a deliberate one: were a hosted offering of PIE to
appear in future, the gap would exist.

---

# Third-Party Software

The Privacy Intelligence Engine may integrate open source software developed
by third parties.

Every component keeps its own original licence.

Integration does not alter the rights of the original authors.

---

# Technitium DNS Server

Technitium DNS Server is independent software.

PIE uses only its public APIs.

The project does not modify the source code of Technitium and is not a fork of
it.

Every reference to Technitium remains subject to its own licence.

---

# External Libraries

Every external library must satisfy at least one of the following
requirements.

* to be compatible with the licence of the project;
* to be actively maintained;
* to have adequate documentation.

---

# Source Code

The code developed for the Privacy Intelligence Engine stays separate from the
code of the Data Sources it integrates.

Changes made to the project must not alter the code of external software.

---

# Copyright

The copyright of the source files belongs to the authors of the project.

The GPL recommends that every source file carry a copyright and licence note
at its head. That note is **not yet present** in the existing files: adding it
touches the whole codebase and is to be carried out as dedicated work, before
the repository is published.

The absence of the note does not affect the validity of the licence, which is
declared by `LICENSE.md` and by this specification.

---

# Contributions

Contributions from outside the project must respect:

* the coding standards;
* the architecture of the project;
* the official documentation;
* the licence adopted.

---

# Dependencies

Every dependency introduced into the project must be documented.

The documentation covers at least.

* name;
* version;
* licence;
* purpose.

---

# License Compatibility

Before a new dependency is integrated, its compatibility with the licence of
the project is verified.

---

# Documentation

The technical documentation is distributed together with the project.

Every update to the documentation follows its own versioning.

---

# Distribution

How the project is distributed follows from the licence adopted.

---

# Design Principles

The licensing policy follows these principles.

* transparency;
* respect for third-party licences;
* separation between the project's own code and integrated software;
* regulatory compliance.

---

# Constraints

The project:

* does not modify the licence of the software it integrates;
* does not incorporate code incompatible with the licence chosen;
* keeps PIE and the supported Data Sources separate.

---

# Related Specifications

* 00 - Glossary
* 01 - Vision
* 02 - Architecture
* 04 - Technitium Integration
* 13 - Roadmap
