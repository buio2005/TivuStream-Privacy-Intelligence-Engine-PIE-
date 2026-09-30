# Project Context

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Project Context

**Version:** 1.0.2

**Status:** Official

**Last Updated:** 2026-09-30

---

# Purpose

This document gives a complete overview of the project.

Its aim is to let developers and AI assistants understand the context, the
objectives and the philosophy of the Privacy Intelligence Engine quickly,
without having to read the whole of the technical documentation first.

---

# What PIE Is

TivuStream Privacy Intelligence Engine (PIE) is a modular framework designed
to analyse, correlate and interpret data about the privacy and the security of
networks.

PIE does not replace existing tools.

It integrates them.

The objective is to turn complex technical data into information that can be
understood.

---

# Project Vision

The project comes from the observation that most tools dedicated to security
and privacy expose large quantities of technical data without really helping
the user to understand what it means.

PIE introduces a layer of intelligence that collects information from
different sources, normalises it through a unified data model, and returns
analyses, indicators and recommendations.

---

# Current Scope

The first implementation of the project is dedicated to the analysis of DNS
networks.

The first backend supported is:

- Technitium DNS Server

The architecture is nevertheless designed to support further Data Sources in
future without modifying the Core.

---

# What PIE Is Not

PIE is not:

- a DNS Server;
- a Firewall;
- an IDS;
- an Antivirus;
- a DNS filtering system.

Those capabilities belong to the backends integrated.

PIE deals only with analysing and interpreting the information those systems
produce.

---

# First Application

The first application built on top of PIE is:

**Network Privacy**

Network Privacy is the user interface of the project.

It shows only information processed by the Privacy Intelligence Engine.

---

# Core Principles

The whole project follows a few fundamental principles.

- Documentation First
- Privacy First
- Local First
- Self Hosted
- Modular Architecture
- Backend Independence
- Simplicity
- Transparency

Every technical decision must respect these principles.

---

# High Level Architecture

The project is organised into independent layers.

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

Every layer has its own responsibilities.

---

# Documentation Structure

The official documentation lives in the folder:

```text
/docs
```

Every Specification describes a single aspect of the project.

The documentation is the official reference for development.

---

# Repository Structure

The repository is organised into the following main areas.

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

Every folder has a well-defined responsibility.

The Core lives inside `backend/` as an autonomous project, isolated from the
Adapters and from the host of the REST APIs.

---

# Technology Stack

The technology stack adopted by the project is the following.

## Backend

- ASP.NET Core
- C#
- REST API

## Frontend

- Vue 3
- TypeScript
- Vite
- Pinia

## Database

- SQLite

## Documentation

- Markdown

---

# Development Workflow

Development follows this order.

1. Documentation
2. Architecture
3. Implementation
4. Testing
5. Review
6. Final documentation

---

# AI Workflow

Every AI assistant is to read, in this order.

1. README.md

2. PROJECT_CONTEXT.md

3. AI_DEVELOPMENT_GUIDE.md

4. The Specifications concerned

Only afterwards may it begin to produce code.

---

# Long-Term Vision

PIE is designed to become a framework able to integrate several systems
dedicated to privacy and security.

The architecture is not limited to DNS.

Over time new Adapters and new applications may be developed while the Core
stays unchanged.

---

# Project Goal

The final objective of the project is to build a self-hosted platform able to
offer a simple, reliable and unified view of the state of privacy and security
of the network.

Every component of the ecosystem is to contribute to this objective while
keeping modularity, independence and simplicity.

---

# Recorded Decisions

Decisions taken by the owner of the project. The state of each is declared:
recording a decision as pending when it has been carried out, or the reverse,
would make this document lie about the project.

---

## Bilingual Interface — done

The Frontend offers the interface in **Italian and in English**.

The constraint extends to every public page of the project: landing page,
presentation, usage documentation. None of those pages exists yet.

Internationalisation was provided for **from the first line of the Frontend**.
Adding it once an interface is built means rewriting every text already
written.

---

## Documentation In English — in progress

The documentation published on GitHub, `README.md` included, is **in English**.

The reason is access for users who are not Italian, consistent with a public
platform.

The translation covers the twenty versioned Specifications and the documents
at the root, and is being carried out as a dedicated piece of work, not
incrementally: documents in two different languages in the same release would
leave it uncertain which is the authoritative source. Progress is tracked in
`docs/TRANSLATION-PROGRESS.md`.

The history of `CHANGELOG.md` below the language boundary stays in Italian and
is not translated. Every new text is written in English.

The right moment is **before the repository is published**, not before the end
of the Backend.

Communication with the owner of the project stays in Italian.

---

## Plain Language For Our Own Terms — pending

Most of the people who will use the tool have no technical background.

The explanations the product already gives are precise and, for that audience,
**opaque**: "lower bound", "coverage", "minimisation of the name queried",
"encrypted transport" are exact terms that do not explain themselves.

A further layer is therefore needed: not a page describing how the product
works, but a way to **explain our own explanations**.

---

### The Tension To Resolve

Honesty and simplicity pull in opposite directions here.

"They are not tracking you" is simple and false. "The value is a lower bound"
is true and does not arrive.

The solution is **not** to soften the sentences: the rules of the Network
Privacy Specification stay intact, and a simplified wording that lost the
qualification would breach the specification.

The solution is to make the meaning **reachable at the point where the term
appears**, without obliging the user to look for it elsewhere.

---

### Requirements

* Every technical term used in the interface has a short explanation, in plain
  language, available where the term appears.
* The explanation **is added to** the exact sentence, it does not replace it.
* The explanations live in the translation catalogue, together with the
  sentences they clarify, in both languages.
* This is not the Glossary in `docs/`, which is written for whoever develops.

Terms that need it, initial list: coverage, lower bound, not measurable,
measured in part, confidence, encrypted transport, DNSSEC, minimisation of the
name queried, client subnet, suspicious domain, freshness of the list,
unclassified domain.

---

### What This Is Not

A "how it works" section written to convince is the performance the project
refuses to give.

If an explanatory page is written one day, the rules of honesty apply to it in
full, and it must not rewrite by hand explanations the catalogue already
holds: two versions of the same statement drift apart over time with nobody
noticing.

---

## Licence Notices — pending

The licence has been chosen: **GPL-3.0**. Two things remain to be done.

**Notices in the source files.** The GPL recommends a copyright and licence
notice at the head of every file. It is to be added as a dedicated piece of
work before the repository is published.

**Notice in the interface.** The GPL provides that an interactive interface
show the copyright, the absence of warranty and how to consult the licence. It
is a requirement of the Frontend.

---

# References

Before starting any development work, consult:

- README.md
- AI_DEVELOPMENT_GUIDE.md
- Documentation Release 1.16.1 (/docs)
- CHANGELOG.md

These documents are the official reference of the project.
