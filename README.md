# TivuStream Privacy Intelligence Engine (PIE)

> **The intelligence layer for privacy-first network analysis.**

---

# TivuStream Privacy Intelligence Engine

TivuStream Privacy Intelligence Engine (PIE) è il progetto alla base di una nuova generazione di strumenti TivuStream dedicati alla privacy, alla sicurezza e all'analisi della rete.

L'obiettivo non è sviluppare un nuovo DNS Server o sostituire software già esistenti, ma costruire un motore capace di raccogliere informazioni provenienti da diverse sorgenti, analizzarle e trasformarle in dati comprensibili, utili e immediatamente fruibili dall'utente.

Il primo modulo ufficiale sviluppato sopra il Privacy Intelligence Engine sarà **Network Privacy**.

---

# Obiettivo del progetto

Realizzare una piattaforma self-hosted che permetta di monitorare e comprendere il comportamento della rete locale attraverso un'interfaccia semplice, moderna e orientata alla privacy.

Il progetto è pensato per utenti che desiderano conoscere lo stato della propria rete senza dover interpretare dati tecnici complessi.

---

# Filosofia

Il progetto segue alcuni principi fondamentali:

* Privacy First
* Local First
* Self Hosted
* Architettura modulare
* Nessuna telemetria
* Massima semplicità per l'utente finale

Ogni funzionalità dovrà contribuire a rendere la privacy più comprensibile e accessibile.

---

# Architettura

Il Privacy Intelligence Engine rappresenta il livello di analisi del sistema.

Le Data Sources raccolgono i dati.

Gli Adapter li convertono nel Unified Data Model.

Il Core li interpreta.

Le applicazioni TivuStream li presentano all'utente.

```text
                Data Sources
                     │
                     ▼
                  Adapters
                     │
                     ▼
            Unified Data Model
                     │
                     ▼
     TivuStream Privacy Intelligence Engine
                     │
      ┌──────────────┼──────────────┐
      ▼              ▼              ▼
 Threat Engine  Device Engine   NPSS Engine
      │              │              │
      └──────────────┼──────────────┘
                     ▼
             Recommendation Engine
                     │
                     ▼
                  REST API
                     │
                     ▼
          TivuStream Applications
```

Il sistema utilizza due flussi distinti.

L'**Acquisition Flow** acquisisce periodicamente i dati dalle Data Sources.

Il **Query Flow** serve le richieste del Frontend restituendo esclusivamente risultati già elaborati.

La descrizione completa è contenuta nella Architecture Specification.

---

# Primo modulo

## Network Privacy

Network Privacy rappresenta la prima applicazione sviluppata utilizzando il Privacy Intelligence Engine.

Il suo compito è analizzare il traffico DNS della rete locale e fornire informazioni semplici riguardo a:

* sicurezza DNS
* privacy della rete
* tracker
* malware
* dispositivi
* attività DNS
* configurazione
* suggerimenti

Per la gestione DNS il progetto utilizza **Technitium DNS Server** come backend.

Technitium rimane il motore DNS.

TivuStream fornisce l'intelligenza, l'analisi e l'interfaccia utente.

---

# Componenti previsti

Il progetto sarà composto da moduli indipendenti.

## Core

I moduli del Core utilizzano tutti il suffisso **Engine**.

* Threat Engine
* Device Engine
* NPSS Engine
* Alert Engine
* Recommendation Engine

## Integration

* Adapter Manager
* Technitium Adapter
* Unified Data Model

## Interface

* REST API
* Network Privacy

---

# Obiettivi principali

* Rendere comprensibili dati complessi.
* Aiutare gli utenti a migliorare la privacy della rete.
* Fornire analisi chiare e immediate.
* Costruire una piattaforma estensibile nel tempo.
* Integrare più sorgenti di dati mantenendo un'unica esperienza utente.

---

# Tecnologie

## Backend

* ASP.NET Core
* C#
* REST API
* SQLite

## Frontend

* Vue 3
* TypeScript
* Pinia
* Vite

## Data Source

* Technitium DNS Server (prima integrazione supportata)

## Piattaforme

* Linux
* Windows
* Docker

Ogni dipendenza introdotta nel progetto viene documentata con nome, versione, licenza e scopo.

---

# Roadmap

| Milestone | Descrizione           | Stato     |
| --------- | --------------------- | --------- |
| M1        | Documentation Release | Completed |
| M2        | Backend Core          | In Progress |
| M3        | Technitium Adapter    | In Progress |
| M4        | Core Modules          | In Progress |
| M5        | Frontend              | In Progress |
| M6        | Reports               | Planned   |
| M7        | Testing               | In Progress |
| M8        | Beta Release          | Planned   |
| M9        | Stable Release        | Planned   |

Il dettaglio delle fasi è contenuto nella Roadmap Specification.

---

# Stato del progetto

**Documentation Release:** 1.15.0

**Project Status:** In Development

**Development Status:** In Progress. Il progetto non è pronto all'uso: vedi la Roadmap Specification per ciò che manca.

---

# Aprire PIE da un altro dispositivo

PIE si apre dal telefono o da un altro computer di casa, con una connessione cifrata. Non serve configurare nulla.

1. Sul computer dove gira PIE, prepara l'interfaccia una volta: nella cartella `frontend`, `npm run build`.
2. Avvia PIE: nella cartella `backend/src/TivuStream.Pie.Api`, `dotnet run`.
3. All'avvio PIE scrive gli indirizzi a cui risponde e un'**impronta**, una lunga sequenza di lettere e numeri:

   ```text
   PIE is reachable at:
     https://NOME-DEL-PC:5443
     https://192.168.1.5:5443
   SHA-256 fingerprint: D7:00:13:3B:...
   ```

4. La prima volta Windows chiede se consentire a PIE l'accesso alla rete. Rispondi sì, almeno per le reti private: senza permesso, dagli altri dispositivi la pagina non si apre.
5. Sull'altro dispositivo apri uno degli indirizzi. Quello che comincia come quello del tuo router (spesso `192.168.`) è di solito quello giusto.
6. Il browser avvisa che la connessione «non è privata». È normale: il certificato lo ha creato PIE e nessun browser lo conosce ancora. Apri i dettagli del certificato e controlla che l'impronta SHA-256 sia la stessa scritta da PIE. Se coincide, prosegui. Se non coincide, non inserire la password.

L'avviso ricompare, una volta per dispositivo, quando PIE rinnova il certificato (circa una volta l'anno) o quando il router cambia l'indirizzo del computer.

**Chi ha un certificato proprio** lo indica in `appsettings.Local.json` (`Transport:Certificate:Path`) e l'avviso scompare. **Chi non vuole PIE raggiungibile dalla rete** imposta `Transport:HttpsPort` a `0`. I dettagli sono nella Transport Security Specification.

PIE legge il server DNS direttamente, senza passare da VPN o proxy del sistema. Su un computer usato da più persone conviene un profilo del browser dedicato a PIE: la cronologia conserva gli indirizzi delle pagine aperte.

---

# Quanto a lungo PIE conserva i dati

PIE tiene sul tuo computer un riassunto di ciò che la rete ha fatto, mai l'elenco delle singole richieste. Con il tempo lo riassume ancora di più, e alla fine lo cancella.

| Per quanto tempo | Che cosa resta |
| --- | --- |
| Ultimi 30 giorni | Ora per ora: quali domini, quante richieste, da quale dispositivo |
| Fino a 12 mesi | Giorno per giorno, con le stesse informazioni |
| Fino a 5 anni | Mese per mese: quali domini e quali dispositivi, ma non più quale dispositivo ha contattato quale dominio |
| Oltre | Niente |

Ogni giorno e ogni mese ricorda quante ore sono state davvero osservate. Un giorno in cui PIE è rimasto spento non sembra un giorno tranquillo.

Ciò che viene cancellato è cancellato davvero: PIE lo sovrascrive nel file, non lo segna soltanto come eliminato.

**Riassumere non si può annullare.** Se riduci questi tempi, alla prossima ora PIE riassume o cancella ciò che è più vecchio, e il dettaglio non torna indietro. Se vuoi comunque farlo, i valori stanno in `appsettings.Local.json`, sotto `Storage:Retention`: `HourlyDays`, `DailyMonths`, `MonthlyYears`. Un valore troppo basso non viene corretto in silenzio: PIE non si avvia e dice quale valore cambiare.

Prima di aggiornare la struttura del database, PIE ne fa una copia accanto al file (`pie.db.schema-…bak`). Le copie non vengono cancellate da sole: contengono gli stessi dati, e puoi eliminarle tu quando l'aggiornamento ti sembra riuscito.

---

# Documentazione

La documentazione tecnica completa è disponibile nella cartella `docs/`.

Ogni documento descrive uno specifico componente dell'architettura e costituisce il riferimento ufficiale per lo sviluppo del progetto.

Il progetto segue un modello **Documentation First**: la documentazione rappresenta la fonte autorevole, il codice la implementa.

Prima di contribuire consultare nell'ordine:

1. `README.md`
2. `PROJECT_CONTEXT.md`
3. `AI_DEVELOPMENT_GUIDE.md`
4. le Specification in `docs/`

Le modifiche alla documentazione sono registrate in `CHANGELOG.md`.

---

# Licenza

Il progetto è distribuito sotto **GNU General Public License, versione 3**. Il testo integrale risiede in `LICENSE.md`.

La scelta del copyleft discende dalla promessa del progetto: l'utente deve poter verificare che cosa il programma fa. Una licenza permissiva consentirebbe di distribuire una versione chiusa, con le stesse schermate che dichiarano che i domini non lasciano il dispositivo, senza che nessuno possa verificarlo.

Le componenti open source integrate mantengono le rispettive licenze originali.

Technitium DNS Server rappresenta un software indipendente: PIE ne utilizza esclusivamente le API pubbliche e non ne costituisce un fork.

La politica completa è descritta nella License Specification.
