# 12 - Installation

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Installation Specification

**Version:** 1.3.0

**Status:** Approved

**Last Updated:** 2026-09-26

---

# Purpose

Questa specifica definisce i requisiti e il processo di installazione del Privacy Intelligence Engine.

L'obiettivo è garantire una procedura di installazione semplice, ripetibile e indipendente dalla piattaforma.

---

# Objectives

L'installazione deve essere:

* semplice;
* guidata;
* ripetibile;
* sicura;
* facilmente aggiornabile.

---

# Supported Platforms

Le piattaforme supportate sono:

* Linux
* Windows
* Docker, prevista e non ancora fornita (vedi Not Yet Provided)

Ulteriori piattaforme potranno essere supportate nelle versioni successive.

---

# Installation Modes

Sono previsti i seguenti metodi di installazione.

## Standard Installation

Installazione completa del sistema.

Comprende:

* Backend
* Frontend
* Core
* REST API

---

## Docker Installation

Installazione tramite container.

Comprende tutti i componenti del progetto.

---

## Development Installation

Installazione destinata allo sviluppo.

Include strumenti aggiuntivi per debugging e test.

---

# System Requirements

## Minimum

* CPU Dual Core
* 4 GB RAM
* 2 GB spazio disponibile
* Connessione di rete

---

## Recommended

* CPU Quad Core
* 8 GB RAM
* SSD
* Connessione Gigabit

---

# Required Components

Per il funzionamento del sistema sono necessari.

* Privacy Intelligence Engine
* almeno una Data Source supportata
* Browser moderno
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

Prima dell'installazione vengono verificati.

* sistema operativo;
* spazio disponibile;
* permessi;
* rete;
* dipendenze.

---

# Initial Configuration

Durante la configurazione iniziale vengono definiti.

* lingua;
* Data Source;
* parametri di connessione;
* impostazioni di rete.

**Primo amministratore.** Una nuova installazione non ha alcun account, e non ne ha uno predefinito né una password di fabbrica. All'avvio il Backend mostra sull'output standard un **codice di configurazione**; inserirlo nel browser, con un nome utente e una password, crea il primo `Administrator`. La prova di possesso è l'accesso alla macchina.

Chi perde l'accesso lo recupera con un comando eseguito sulla macchina che ospita l'installazione, non da remoto e non tramite posta.

Il dettaglio è nell'Authentication Specification.

---

# Backend Registration

Ogni Data Source viene registrata attraverso il relativo Adapter.

Il sistema verifica automaticamente la connettività.

---

# Capability Detection

Al termine della registrazione il sistema rileva **quali capacità la Data Source è in grado di offrire** nella sua configurazione corrente.

Il risultato viene presentato all'utente prima del completamento dell'installazione.

Per ciascuna capacità mancante il sistema indica:

* quali analisi non saranno disponibili;
* quale intervento la renderebbe disponibile;
* quali conseguenze comporta tale intervento.

---

# Optional Capabilities

Alcune capacità richiedono componenti facoltativi della Data Source.

Quando tali componenti sono installabili in modo automatico, il sistema può proporne l'installazione durante la configurazione guidata.

La proposta rispetta tre regole.

**Scelta esplicita.** Nessun componente facoltativo viene installato senza una decisione dell'utente.

**Informazione simmetrica.** Vantaggi e costi sono presentati insieme. Se l'attivazione comporta un aumento del consumo di risorse, la registrazione di dati aggiuntivi o un impatto sulle prestazioni, tali aspetti vengono dichiarati prima della scelta.

**Reversibilità.** L'utente può rifiutare la proposta e completare comunque l'installazione, oppure attivare la capacità in un momento successivo.

---

# Retention Configuration

Quando una capacità dipende da un componente che registra dati con una politica di ritenzione propria, il sistema verifica la coerenza fra tale ritenzione e la frequenza di acquisizione configurata.

Una ritenzione inferiore all'intervallo di acquisizione comporta una **perdita di dati non segnalata dal backend**.

Il sistema propone valori coerenti e segnala la condizione quando si verifica.

Questa verifica viene ripetuta a ogni modifica della frequenza di acquisizione.

---

# Privacy Disclosure

Quando una capacità comporta la registrazione di dati aggiuntivi relativi all'attività degli utenti, l'installazione dichiara in modo esplicito:

* quali dati vengono registrati;
* dove risiedono;
* per quanto tempo vengono conservati;
* quali dati vengono conservati da PIE e quali restano nella Data Source.

Il progetto adotta il principio di aggregare il dato al momento dell'acquisizione e di non conservare il dettaglio puntuale all'interno di PIE.

Questa scelta va comunicata all'utente, poiché ne determina l'esposizione effettiva.

---

# Security

Durante l'installazione:

* vengono generate le configurazioni iniziali;
* vengono verificati i certificati;
* vengono protette le credenziali;
* non viene creato alcun account né alcuna password predefinita.

---

# Verification

Al termine dell'installazione il sistema esegue:

* verifica del Core;
* verifica delle REST API;
* verifica degli Adapter;
* verifica della Data Source.

---

# Update Process

L'aggiornamento del sistema mantiene:

* configurazioni;
* dati;
* Adapter installati.

L'aggiornamento non modifica la struttura del Unified Data Model.

---

# Backup

Prima di ogni aggiornamento il sistema può creare un backup della configurazione.

Il backup comprende.

* impostazioni;
* configurazione;
* dati applicativi.

---

# Uninstallation

La procedura di rimozione elimina:

* componenti applicativi;
* servizi;
* file temporanei.

Le configurazioni possono essere conservate su richiesta dell'utente.

---

# Logging

Ogni fase dell'installazione viene registrata.

I log facilitano la diagnosi di eventuali problemi.

---

# Error Handling

Ogni errore viene classificato e presentato con una descrizione comprensibile.

---

# First Installation

Questa sezione definisce la **prima procedura di installazione**, quella richiesta dal criterio di Beta: una persona estranea al progetto installa PIE su una macchina pulita seguendo le istruzioni.

Realizza una parte di ciò che il resto della specifica descrive. Ciò che non realizza è elencato in Not Yet Provided, e resta un obiettivo.

---

## Package

PIE si distribuisce come **pacchetto pronto**, uno per piattaforma.

| Piattaforma | Pacchetto                                   |
| ----------- | ------------------------------------------- |
| Windows     | `tivustream-pie-<versione>-win-x64.zip`      |
| Linux       | `tivustream-pie-<versione>-linux-x64.tar.gz` |

Il pacchetto contiene il programma, l'interfaccia già compilata, gli script di installazione e la guida. **Non richiede di installare .NET né Node.js**: il runtime è incluso nel programma.

Il pacchetto si produce con uno script del repository, in `installer/`, che compila l'interfaccia, pubblica il Backend per ciascuna piattaforma e crea gli archivi.

La **versione** del prodotto è unica, parte da `0.1.0` e compare nel nome del pacchetto, nel programma e nel registro all'avvio. È indipendente dalla Documentation Release.

Il programma si chiama `tivustream-pie` (`tivustream-pie.exe` su Windows).

---

## Where Things Live

Programma e dati stanno in **due cartelle separate**. Un aggiornamento sostituisce la prima e non tocca la seconda.

| Cosa                  | Windows                          | Linux                       |
| --------------------- | -------------------------------- | --------------------------- |
| Programma             | `C:\Program Files\TivuStream PIE` | `/opt/tivustream-pie`       |
| Dati e configurazione | `C:\ProgramData\TivuStream PIE`   | `/var/lib/tivustream-pie`   |

La **cartella dei dati** contiene il database, le liste, il certificato, le copie di sicurezza e `appsettings.Local.json`, che custodisce il token della Data Source.

È leggibile **solo dal servizio e dagli amministratori** della macchina. Su Linux appartiene a un utente di sistema dedicato, `tivustream-pie`, con permessi `0700`.

Il programma riceve la cartella dei dati all'avvio. I percorsi relativi della configurazione (`Storage:DatabasePath`, `Storage:ListDirectoryPath`, `Transport:CertificateDirectory`) si risolvono **rispetto alla cartella dei dati**, non rispetto alla cartella da cui il programma viene lanciato: un servizio di Windows viene lanciato da `C:\Windows\System32`.

Senza cartella dei dati indicata, come durante lo sviluppo, il comportamento resta quello attuale.

---

## Running As A Service

PIE funziona come **servizio di sistema**: parte all'accensione, anche senza nessuno collegato, e osserva la rete senza interruzioni.

| Piattaforma | Meccanismo | Identità |
| ----------- | ---------- | -------- |
| Windows | Servizio di Windows `TivuStreamPIE`, avvio automatico | Account virtuale `NT SERVICE\TivuStreamPIE`, senza privilegi di amministratore |
| Linux | Unità systemd `tivustream-pie.service` | Utente di sistema `tivustream-pie`, senza shell |

Su Linux l'unità limita ciò che il servizio può toccare: file di sistema in sola lettura, scrittura solo nella cartella dei dati, nessuna acquisizione di privilegi.

Servono due dipendenze ufficiali Microsoft, che permettono al programma di comportarsi da servizio:

| Voce | Valore |
| --- | --- |
| Nome | `Microsoft.Extensions.Hosting.WindowsServices`, `Microsoft.Extensions.Hosting.Systemd` |
| Scopo | Integrazione con il gestore dei servizi di Windows e con systemd |
| Licenza | MIT |
| Manutenzione | Microsoft, parte dell'ecosistema .NET |

Fuori da un servizio, entrambe non cambiano nulla.

---

## First Administrator Under A Service

Un servizio non ha una finestra: ciò che scrive sull'output standard finisce nel registro di sistema, oppure da nessuna parte. Il **codice di configurazione iniziale** della Authentication Specification non può quindi essere mostrato, e scriverlo nel registro di sistema lo metterebbe dove non deve stare.

Per questo, **quando PIE funziona come servizio, il codice di configurazione non viene generato né mostrato.** Il primo amministratore si crea durante l'installazione con il comando `reset-password`, che su un'installazione senza account crea un amministratore. Lo script di installazione lo esegue e chiede nome e password nel terminale.

La prova di possesso resta la stessa: l'accesso alla macchina, qui con i permessi di amministratore.

---

## Commands

Il programma offre tre comandi da terminale. Nessuno avvia il servizio.

| Comando | Cosa fa |
| --- | --- |
| `configure` | Chiede indirizzo e token della Data Source, prova la connessione, dice quali capacità sono disponibili e quali mancano, e scrive `appsettings.Local.json` nella cartella dei dati |
| `reset-password <nome>` | Esistente. Su un'installazione senza account crea il primo amministratore |
| `access` | Scrive gli indirizzi a cui PIE risponde e l'impronta del certificato |

**`configure`** non accetta una configurazione che non funziona. Se la connessione non riesce dice perché, con le parole della persona (indirizzo irraggiungibile, token rifiutato, permessi insufficienti), e non scrive nulla. Il token viene letto senza essere mostrato sullo schermo mentre si digita, e non compare mai in nessun messaggio.

Per ogni capacità mancante, `configure` dice che cosa non sarà disponibile e che cosa la renderebbe disponibile, come prevede Capability Detection. In particolare, per `DomainActivity` dichiara la conseguenza per la privacy prevista da Privacy Disclosure: l'attivazione dei Query Logs fa conservare a **Technitium** ogni singola interrogazione, secondo la ritenzione di Technitium, mentre PIE continua a conservarne solo l'aggregato.

**`access`** esiste perché, sotto un servizio, le righe scritte all'avvio non si vedono. Senza certificato ancora generato dice di avviare prima il servizio.

---

## Installation Script

Uno script per piattaforma, da eseguire come amministratore dalla cartella del pacchetto estratto: `install.ps1` su Windows, `install.sh` su Linux.

```text
Verifica      sistema operativo, permessi di amministratore, spazio, porte libere
↓
Copia         programma nella sua cartella
↓
Dati          cartella dei dati con i permessi ristretti
↓
Collegamento  configure
↓
Accesso       reset-password, se non esiste alcun account
↓
Servizio      registrazione e avvio
↓
Firewall      regola per la porta HTTPS, solo sulle reti private
↓
Verifica      il servizio risponde
↓
Pronto        access: indirizzi e impronta
```

Ogni passo dice che cosa sta facendo. Un passo che fallisce ferma lo script con una frase comprensibile e dice che cosa è già stato fatto.

**Il firewall.** Un servizio non fa comparire la richiesta del firewall di Windows: senza regola resterebbe irraggiungibile dagli altri dispositivi senza che nessuno lo dica. Lo script aggiunge una regola per la sola porta HTTPS, sulle sole reti private, e lo dichiara. Su Linux lo script non modifica il firewall: dice quale porta aprire se ne è attivo uno.

**Se PIE è già installato**, lo script lo aggiorna: ferma il servizio, sostituisce il programma, lo riavvia. Non ripete `configure` né `reset-password`, e non tocca la cartella dei dati. La copia di sicurezza prima di un cambio di schema la fa il programma stesso (Persistence Specification).

---

## Uninstallation Script

`uninstall.ps1` e `uninstall.sh` fermano e rimuovono il servizio, la regola del firewall e la cartella del programma.

**La cartella dei dati resta**, a meno che la persona non lo chieda esplicitamente con un'opzione (`-RemoveData`, `--remove-data`). Prima di cancellarla lo script dice che cosa contiene e chiede conferma. La cancellazione è effettiva.

---

## Installation Guide

Il pacchetto contiene una **guida**, `INSTALL.md`, scritta per una persona che non conosce il progetto, in linguaggio semplice. Comprende:

* che cosa serve prima: una Data Source Technitium già funzionante, e come crearvi un utente di sola lettura e il suo token;
* l'installazione, passo per passo, con ciò che lo script chiede;
* come aprire PIE da un altro dispositivo e confrontare l'impronta, che cosa fare se non coincide;
* il proxy di sistema e le VPN: PIE raggiunge Technitium direttamente, ignorandoli;
* un profilo del browser dedicato su un computer condiviso, perché la cronologia conserva gli indirizzi aperti;
* quanto a lungo PIE conserva i dati, e le copie di sicurezza da eliminare dopo un aggiornamento riuscito;
* come aggiornare, come disinstallare, come riavere l'accesso.

PIE **non installa Technitium** e non ne modifica la configurazione, come stabilito in Constraints.

---

## Field Tests

La procedura è verificata sul campo prima di essere dichiarata realizzata:

| Prova | Dove |
| --- | --- |
| Installazione, aggiornamento, disinstallazione | Windows, sul computer di sviluppo con il backend di sviluppo fermo |
| Installazione, aggiornamento, disinstallazione | Linux, in WSL o in una macchina virtuale |
| Accesso da un altro dispositivo | Un telefono o un portatile sulla rete di casa |
| Una persona estranea installa seguendo la guida | Macchina pulita; è il criterio di Beta, e resta aperto finché non avviene |

---

# Not Yet Provided

La prima procedura non realizza le parti seguenti della specifica. Restano obiettivi, e vanno dichiarati come tali.

| Parte | Stato |
| --- | --- |
| Docker Installation | Non fornita. Il pacchetto Linux copre lo stesso uso su un server di casa |
| Development Installation | È il repository stesso, con i comandi di `CLAUDE.md` |
| Scelta della lingua all'installazione | Non necessaria: l'interfaccia è bilingue e segue il browser |
| Proposta di installare componenti facoltativi della Data Source | Non fornita: `configure` dice che cosa manca e come ottenerlo, senza installarlo |
| Verifica della ritenzione della Data Source rispetto alla frequenza di acquisizione | Non fornita all'installazione |
| Registro delle fasi d'installazione su file | Lo script scrive sul terminale; nessun file |
| Pagina di configurazione nel browser | Non fornita: la configurazione si fa con `configure` |
| Altre architetture (ARM, per esempio Raspberry Pi) | Non fornite nella prima versione |

---
# Design Principles

L'installazione segue i seguenti principi.

* semplicità;
* ripetibilità;
* sicurezza;
* modularità;
* indipendenza dalla piattaforma.

---

# Constraints

La procedura di installazione:

* non modifica le Data Sources;
* non richiede modifiche ai backend supportati;
* utilizza esclusivamente componenti ufficiali del progetto.

---

# Related Specifications

* 04 - Technitium Integration
* 10 - Frontend
* 11 - Backend
* 13 - Roadmap
