# Contributing

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Contribution Guidelines

**Version:** 1.0.0

**Status:** Official

**Last Updated:** 2026-09-30

---

# Purpose

This document describes how to contribute to the project.

It introduces no new requirement.

It gathers, in operational form, the rules already defined in
`AI_DEVELOPMENT_GUIDE.md` and in the Specifications in `docs/`.

In case of divergence, the official documentation prevails over this document.

---

# Development Model

The project follows a **Documentation First** model.

The documentation is the authoritative source.

The code implements what the Specifications define.

The code does not define the architecture.

---

# Before Contributing

Before writing code it is necessary to read, in this order:

1. `README.md`
2. `PROJECT_CONTEXT.md`
3. `AI_DEVELOPMENT_GUIDE.md`
4. the Specifications concerned, in `docs/`

A contribution that cannot be traced back to an official Specification cannot
be accepted.

---

# Documentation Priority

In case of conflict the following priorities hold.

1. `AI_DEVELOPMENT_GUIDE.md`
2. The Specifications in `docs/`
3. `README.md`
4. The existing code

---

# Architecture

The architecture of the project is fixed.

```text
Frontend

↓

REST API

↓

Privacy Intelligence Engine

↓

Adapter

↓

Data Source
```

The system uses two distinct flows.

The **Acquisition Flow** is triggered by the Scheduler and orchestrated by the
Adapter Manager.

The **Query Flow** serves the requests of the Frontend and never reaches the
Data Sources.

---

# Architectural Rules

The following rules admit no exception.

* The Core does not communicate directly with the Data Sources.
* The Core does not orchestrate the acquisition of data.
* Every Data Source has a dedicated Adapter.
* The Adapters convert the data and perform no analysis.
* The Frontend contains no business logic.
* The Core contains no presentation logic.
* The Unified Data Model is the only internal data format.

The project implementing the Core references neither the Adapters nor the host
of the REST APIs.

This isolation is verified at compile time: a reference added for convenience
makes the build fail.

---

# What Is Not Allowed

A contribution must not:

* modify the architecture on its own;
* rename official components;
* introduce dependencies that have not been approved;
* create new folders without authorisation;
* modify the Specifications without an explicit request;
* create data models parallel to the Unified Data Model.

---

# Missing Requirements

If a Specification does not describe a behaviour that is needed:

* do not invent a solution;
* do not introduce new architectures;
* ask for clarification;
* or propose a solution clearly identified as a proposal.

Proposals stay separate from the implementation asked for.

---

# Terminology

Use only the terminology defined in `docs/00-Glossary.md`.

Do not introduce synonyms.

The modules of the Core all use the **Engine** suffix.

Some examples of terms to avoid.

| Avoid | Use |
| --- | --- |
| Privacy Score | Network Privacy & Security Score (NPSS) |
| Connector | Adapter |
| Threat Intelligence (as the name of a module) | Threat Engine |

The complete list is in the Glossary.

---

# Language

**The project is in English**: issues, pull requests, source code and
documentation.

Identifiers, namespaces, classes, methods, file names and commit messages are
in English.

The official terminology of the project is not translated.

The interface is bilingual, Italian and English, and every text there lives in
the translation catalogue; a new text is added to both languages at once.

---

# Coding Principles

Every implementation respects the following principles.

* Single Responsibility
* Separation of Concerns
* Modularity
* Readability
* Simplicity
* Maintainability

The code favours readability over compact solutions.

Unnecessary abstractions are avoided.

Components stay small.

Logic is not duplicated.

---

# Repository Structure

```text
backend/
frontend/
docs/
installer/
examples/
resources/
scripts/
tools/
```

The Backend is organised as follows.

```text
backend/
├── TivuStream.Pie.sln
├── Directory.Build.props
├── src/
│   ├── TivuStream.Pie.Model/
│   ├── TivuStream.Pie.Core/
│   ├── TivuStream.Pie.Adapters/
│   ├── TivuStream.Pie.Adapters.Technitium/
│   ├── TivuStream.Pie.Storage/
│   └── TivuStream.Pie.Api/
└── tests/
    ├── TivuStream.Pie.Core.Tests/
    ├── TivuStream.Pie.Storage.Tests/
    ├── TivuStream.Pie.Adapters.Technitium.Tests/
    └── TivuStream.Pie.Api.Tests/
```

---

# Technology Stack

## Backend

* ASP.NET Core
* C#
* .NET 10
* SQLite

## Frontend

* Vue 3
* TypeScript
* Pinia
* Vite

---

# Code Style

The formatting conventions are defined in `.editorconfig` and are applied
automatically by compatible editors.

Line endings are normalised by `.gitattributes`.

Neither of these settings is to be worked around by hand.

The main C# conventions.

* file-scoped namespaces;
* `using` directives outside the namespace;
* PascalCase for types, methods, properties and constants;
* camelCase for parameters and local variables;
* the `_` prefix for private fields;
* the `I` prefix for interfaces.

---

# Prerequisites

Compiling the Backend requires a **.NET SDK 10.x**.

The version is constrained by `global.json` in the root of the repository.

An SDK of a different major version produces an explicit error rather than a
silently different build.

To check the SDK installed.

```bash
dotnet --list-sdks
```

Moving to a later major version is an explicit decision and involves changing
`global.json` and `Directory.Build.props`.

---

# Build

The build treats warnings as errors.

A contribution that produces warnings is not considered complete.

Every version of the SDK introduces new analyser rules: `global.json`
guarantees that an update of the environment does not make the build of
unchanged code fail.

Main commands.

```bash
dotnet restore backend/TivuStream.Pie.sln
dotnet build   backend/TivuStream.Pie.sln
dotnet test    backend/TivuStream.Pie.sln
```

---

# Dependencies

Before introducing an external library, check:

* that it is really needed;
* its quality;
* that it is actively maintained;
* the compatibility of its licence.

Always prefer the libraries already present in the framework.

Every dependency introduced is documented with its name, version, licence and
purpose.

Introducing a new dependency requires explicit approval.

---

# Error Handling and Logging

Every error must be handled, recorded and understandable.

Every significant operation must be capable of being recorded.

The logs must contain no sensitive information.

---

# Security

Every implementation takes into account:

* input validation;
* authentication;
* authorisation;
* protection of credentials;
* HTTPS.

The credentials of the Data Sources stay confined inside their Adapter.

No credential, local database or environment file is ever committed.

---

# Git Workflow

Every change concerns the current task alone.

Unrelated changes are to be avoided.

Commit messages are written in English.

---

# Documentation Changes

When the implementation reveals an inconsistency in the documentation:

* do not silently modify the implementation to work around it;
* report the inconsistency;
* propose an update of the documentation;
* wait for approval.

Every change to the documentation is recorded in `CHANGELOG.md`.

---

# Before Submitting

Before considering a contribution complete, check:

* the project compiles;
* no warnings;
* the architecture is respected;
* the documentation is respected;
* naming is consistent with the Glossary;
* no duplicated logic;
* no dead code;
* no unnecessary dependency.

---

# License

The project is released under **GPL-3.0**.

Contributions are accepted under that licence.

Open source components integrated keep their own original licences.

The complete policy is described in `docs/14-License Specification.md`.

---

# References

* `README.md`
* `PROJECT_CONTEXT.md`
* `AI_DEVELOPMENT_GUIDE.md`
* `CHANGELOG.md`
* The Specifications in `docs/`
