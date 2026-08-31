# 14 - License

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** License Specification

**Version:** 2.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce la politica di licenza del progetto **TivuStream Privacy Intelligence Engine (PIE)** e stabilisce le linee guida per l'utilizzo di software, librerie e componenti di terze parti.

Questo documento rappresenta una specifica progettuale e non costituisce il testo legale della licenza finale del progetto.

---

# Objectives

La politica di licenza ha i seguenti obiettivi.

* garantire trasparenza;
* assicurare la conformità delle dipendenze;
* favorire la manutenzione del progetto;
* preservare la compatibilità con componenti di terze parti.

---

# Project Licence

Il progetto è distribuito sotto **GNU General Public License, versione 3**.

Il testo integrale risiede in `LICENSE.md`, nella radice del repository, riprodotto senza modifiche.

---

## Why Copyleft

La promessa centrale di PIE non è bloccare i tracciatori. È che **l'utente possa verificare che cosa il programma fa**.

Ogni scelta del progetto serve quella promessa: la classificazione che avviene solo sul dispositivo, l'età dichiarata delle liste, la copertura resa esplicita, il punteggio negato quando non c'è abbastanza da misurare.

Una licenza permissiva consentirebbe di distribuire una versione modificata e chiusa, con le stesse schermate che dichiarano che i domini non lasciano il dispositivo, **senza che nessuno possa verificarlo**. La promessa sopravvivrebbe come testo e sparirebbe come fatto.

Il copyleft impone a chi distribuisce una versione modificata di pubblicarne il codice. La licenza diventa così la garanzia tecnica di ciò che le Specification dichiarano a parole.

Il costo è accettato: parte del codice non verrà riusata in prodotti proprietari. È esattamente ciò che la scelta intende ottenere.

---

## Why Version 3 And Not 2

Due ragioni.

La dipendenza `SQLitePCLRaw` è distribuita sotto Apache-2.0, compatibile con GPLv3 e **non** con GPLv2. Adottare la versione 2 renderebbe il progetto non distribuibile con le proprie dipendenze.

La versione 3 disciplina inoltre i brevetti e le misure tecnologiche che impediscono all'utente di eseguire una versione modificata sul proprio dispositivo, condizione rilevante per un programma destinato all'auto-installazione.

---

## Why Not AGPL

La AGPL estende l'obbligo di pubblicazione a chi offre il programma modificato come servizio in rete, senza distribuirlo.

PIE è progettato per essere installato, non offerto come servizio: la fattispecie che la AGPL chiude contraddice il principio Local First del progetto stesso.

La AGPL comporta inoltre un attrito di adozione presso organizzazioni che ne vietano l'uso per policy interna.

La scelta è registrata come consapevole: se in futuro emergesse un'offerta ospitata di PIE, la lacuna esisterebbe.

---

# Third-Party Software

Il Privacy Intelligence Engine può integrare software open source sviluppato da terze parti.

Ogni componente mantiene la propria licenza originale.

L'integrazione non modifica i diritti degli autori originali.

---

# Technitium DNS Server

Technitium DNS Server rappresenta un software indipendente.

PIE utilizza esclusivamente le sue API pubbliche.

Il progetto non modifica il codice sorgente di Technitium e non ne costituisce un fork.

Ogni riferimento a Technitium rimane soggetto alla relativa licenza.

---

# External Libraries

Ogni libreria esterna deve soddisfare almeno uno dei seguenti requisiti.

* essere compatibile con la licenza del progetto;
* essere mantenuta attivamente;
* avere una documentazione adeguata.

---

# Source Code

Il codice sviluppato per il Privacy Intelligence Engine rimane separato da quello delle Data Sources integrate.

Le modifiche apportate al progetto non devono alterare il codice dei software esterni.

---

# Copyright

Il copyright dei file sorgente appartiene agli autori del progetto.

La GPL raccomanda che ogni file sorgente porti in testa una nota di copyright e di licenza. La nota **non è ancora presente** nei file esistenti: la sua aggiunta costituisce un intervento su tutto il codice e va svolta come attività dedicata, prima della pubblicazione del repository.

L'assenza della nota non incide sulla validità della licenza, che è dichiarata dal file `LICENSE.md` e da questa specifica.

---

# Contributions

Eventuali contributi esterni dovranno rispettare:

* gli standard di codifica;
* l'architettura del progetto;
* la documentazione ufficiale;
* la licenza adottata.

---

# Dependencies

Ogni dipendenza introdotta nel progetto dovrà essere documentata.

La documentazione comprenderà almeno.

* nome;
* versione;
* licenza;
* scopo.

---

# License Compatibility

Prima dell'integrazione di una nuova dipendenza verrà verificata la compatibilità con la licenza del progetto.

---

# Documentation

La documentazione tecnica viene distribuita insieme al progetto.

Ogni aggiornamento della documentazione segue il proprio versionamento.

---

# Distribution

Le modalità di distribuzione del progetto saranno definite contestualmente alla scelta della licenza definitiva.

---

# Future License

La scelta della licenza dovrà garantire:

* protezione del progetto;
* chiarezza per gli utilizzatori;
* compatibilità con le dipendenze;
* possibilità di evoluzione futura.

---

# Design Principles

La politica di licenza segue i seguenti principi.

* trasparenza;
* rispetto delle licenze di terze parti;
* separazione tra codice proprietario e software integrato;
* conformità normativa.

---

# Constraints

Il progetto:

* non modifica la licenza dei software integrati;
* non incorpora codice incompatibile con la licenza scelta;
* mantiene la separazione tra PIE e le Data Sources supportate.

---

# Related Specifications

* 00 - Glossary
* 01 - Vision
* 02 - Architecture
* 04 - Technitium Integration
* 13 - Roadmap
