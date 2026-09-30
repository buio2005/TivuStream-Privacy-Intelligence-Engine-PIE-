# AI Development Guide

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** AI Development Guide

**Version:** 1.0.0

**Status:** Official

**Last Updated:** 2026-09-30

---

# Purpose

This document defines the operating rules every AI assistant is to follow
while developing the project.

The objective is to ensure that all the code produced is consistent with the
architecture, the documentation and the standards of the Privacy Intelligence
Engine.

The instructions in this document take priority over the default preferences
of the assistant.

**`MASTER_PROMPT.md` governs.** It holds the working method of the repository
and decides, among other things, which changes need a Specification and an
approval before code and which are recorded in `CHANGELOG.md` afterwards. What
follows here is consistent with it and more detailed on the architecture;
where the two differ, the MASTER_PROMPT wins.

---

# Development Philosophy

The project is developed following a **Documentation First** approach.

The documentation is the official source of the project.

The code implements what the Specifications define.

The code does not define the architecture.

---

# Development Workflow

Every development session follows this order.

1. Read README.md

2. Read AI_DEVELOPMENT_GUIDE.md

3. Read the Specifications concerned

4. Analyse the task asked for

5. Implement only what is documented

6. Update the documentation if necessary

---

# Documentation Priority

In case of conflict the following priorities hold.

1. AI_DEVELOPMENT_GUIDE.md

2. The Specifications in the docs folder

3. README.md

4. The existing code

The code does not modify the documentation.

It is the documentation that guides the code.

---

# AI Responsibilities

The AI is responsible for:

- writing clean code;
- respecting the architecture;
- keeping modularity;
- avoiding duplication;
- proposing improvements with reasons.

---

# AI Restrictions

The AI must not:

- modify the architecture on its own;
- rename official components;
- introduce dependencies that were not asked for;
- create new folders without authorisation;
- modify the Specifications without an explicit request.

---

# Coding Principles

Every implementation must respect the following principles.

- Single Responsibility
- Separation of Concerns
- Modularity
- Readability
- Simplicity
- Maintainability

---

# Architecture Rules

The project uses a layered architecture.

Every layer has its own responsibilities.

Frontend

↓

REST API

↓

Core

↓

Adapter

↓

Data Source

Communication must always respect this flow.

---

# Backend Rules

The Backend:

- implements the business logic;
- manages the Adapters;
- exposes the REST APIs;
- uses the Unified Data Model.

---

# Frontend Rules

The Frontend:

- shows information;
- contains no business logic;
- uses the REST APIs alone.

---

# Core Rules

The Core:

- analyses;
- classifies;
- correlates;
- evaluates;
- produces results.

The Core does not know the Data Sources.

---

# Adapter Rules

Every Adapter:

- communicates with a single Data Source;
- converts the data;
- performs no analysis.

---

# Data Model

Every component uses the Unified Data Model alone.

Creating parallel data models is forbidden.

---

# API Rules

The APIs are the public contract of the system.

Every change to the APIs must preserve backward compatibility.

---

# Naming Convention

Use only the terminology defined in the Glossary.

Do not introduce synonyms.

---

# Dependencies

Before introducing a new library, check.

- that it is really needed;
- its quality;
- its maintenance;
- the compatibility of its licence.

Always prefer the libraries already present in the framework.

---

# Error Handling

Every error must be.

- handled;
- recorded;
- understandable.

---

# Logging

Every significant operation must be capable of being recorded.

The logs must contain no sensitive information.

---

# Security

Every implementation must take into account.

- input validation;
- authentication;
- authorisation;
- protection of credentials;
- HTTPS.

---

# Performance

Optimise the code only when necessary.

Always favour.

- clarity;
- simplicity;
- maintainability.

---

# Code Quality

The code produced must be.

- readable;
- commented only where necessary;
- easy to test;
- consistent with the rest of the project.

---

# Git Workflow

Every change should concern the current task alone.

Avoid unrelated changes.

---

# When Requirements Are Missing

If a Specification does not describe a behaviour that is needed:

- do not invent a solution;
- do not introduce new architectures;
- ask for clarification;
- or propose a solution clearly identified as a proposal.

---

# Suggestions

The AI may propose improvements.

Every proposal must be kept separate from the implementation asked for.

Proposals must not modify the project automatically.

---

# Final Check

Before considering a task complete, check.

- conformity with the Specifications;
- respect for the architecture;
- that the code compiles;
- the absence of duplication;
- consistency of the naming.

---

# Goal

The objective of the AI is not merely to produce working code.

The objective is to contribute to a project that is consistent, modular,
documented and easy to maintain over time.

Every implementation must be traceable back to the official Specifications of
the project.
