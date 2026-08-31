# 07 - Network Privacy & Security Score

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Network Privacy & Security Score (NPSS) Specification

**Version:** 3.0.0

**Status:** Approved

**Last Updated:** 2026-08-02

---

# Purpose

Questa specifica definisce il **Network Privacy & Security Score (NPSS)**, il principale indicatore prodotto dal Privacy Intelligence Engine.

Il NPSS sintetizza lo stato complessivo della rete attraverso un unico valore numerico ottenuto dall'analisi dei dati elaborati dal Core.

---

# Objectives

Il NPSS deve essere:

* semplice da comprendere;
* coerente;
* riproducibile;
* trasparente;
* indipendente dal backend;
* aggiornato automaticamente.

---

# Score Range

Il punteggio utilizza una scala da **0** a **100**.

| Score    | Status    |
| -------- | --------- |
| 90 – 100 | Excellent |
| 75 – 89  | Good      |
| 60 – 74  | Fair      |
| 40 – 59  | Warning   |
| 0 – 39   | Critical  |

---

# Score Components

Il punteggio è composto da differenti aree di valutazione.

```text id="fvc3pk"
Network Privacy & Security Score

├── DNS Security
├── Privacy Protection
├── Threat Protection
├── Device Health
├── Configuration
└── Network Integrity
```

---

# Measurement and Judgement

Il NPSS è composto da due elementi di natura differente, e la distinzione non è formale.

**Il breakdown è misura.** Ogni area riporta ciò che è stato osservato, con i fattori che lo hanno determinato. È verificabile: chiunque disponga degli stessi dati ottiene gli stessi valori.

**Il punteggio complessivo è giudizio.** Aggregare aree diverse in un unico numero richiede di stabilire quanto ciascuna conti, e quella scelta non discende dai dati.

I pesi definiti in questa specifica costituiscono una **posizione editoriale dichiarata del progetto**, non una verità misurata.

Sono motivati, versionati insieme all'algoritmo e rivedibili. Non derivano da uno standard di settore, perché non ne esiste uno per questo tipo di valutazione.

Dichiararlo è parte del principio di Transparency: un giudizio presentato come misura è una misura falsa.

---

# Score Weights

Ogni area contribuisce al punteggio complessivo con un peso definito.

La somma dei pesi è sempre pari a **100**.

| Component          | Weight |
| ------------------ | ------ |
| Threat Protection  | 25     |
| DNS Security       | 20     |
| Privacy Protection | 20     |
| Device Health      | 15     |
| Configuration      | 10     |
| Network Integrity  | 10     |
| **Total**          | **100**|

Motivazione di ciascun peso.

| Area                | Motivazione della posizione adottata                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------- |
| Threat Protection   | Una minaccia attiva produce un danno immediato e concreto, superiore a qualunque difetto di configurazione |
| DNS Security        | Determina la qualità di tutto ciò che transita, indipendentemente da quali minacce siano presenti      |
| Privacy Protection  | Oggetto dichiarato del progetto, ma di impatto graduale anziché immediato                               |
| Device Health       | Attribuisce i problemi ai dispositivi, informazione preziosa ma successiva alla loro rilevazione        |
| Configuration       | Presupposto delle altre aree più che valore autonomo                                                    |
| Network Integrity   | Riguarda l'affidabilità dell'osservazione, non lo stato della rete osservata                            |

I pesi appartengono all'algoritmo NPSS e seguono il versionamento dell'algoritmo.

Una loro modifica costituisce un cambiamento sostanziale e incrementa la Major Version dell'algoritmo, poiché rende i punteggi precedenti non confrontabili.

---

# Indicator Definitions

Gli indicatori di ciascuna area sono definiti in modo **calcolabile e verificabile**.

Un indicatore descritto soltanto da un titolo non è utilizzabile: renderebbe il punteggio dipendente dall'interpretazione di chi scrive il codice, in contrasto con il principio di Transparency, che richiede che ogni valutazione sia spiegabile.

Ogni indicatore dichiara il dato da cui deriva. Un indicatore il cui dato non è disponibile è **non misurabile**, e la sua quota di peso viene esclusa dal calcolo.

---

# DNS Security

Valuta la configurazione del servizio DNS.

Peso complessivo **20**, distribuito su quattro indicatori da **5** punti.

---

## DNSSEC Validation

La validazione DNSSEC protegge dalla manomissione delle risposte.

| Condizione             | Punti |
| ---------------------- | ----- |
| Validazione attiva     | 5     |
| Validazione non attiva | 0     |

Dato: configurazione della Data Source.

---

## Transport Encryption

Valuta sia la disponibilità di trasporti cifrati sia il loro utilizzo effettivo.

| Componente                                            | Punti |
| ----------------------------------------------------- | ----- |
| Almeno un trasporto cifrato abilitato                  | 2     |
| Quota di interrogazioni ricevute su trasporto cifrato  | 3     |

La seconda componente è proporzionale alla quota osservata.

La distinzione è voluta: un servizio può offrire trasporti cifrati senza che alcun dispositivo li utilizzi. Misurare solo la disponibilità premierebbe un'intenzione, misurare solo l'utilizzo ignorerebbe una configurazione corretta.

Dato: configurazione della Data Source e statistiche per protocollo.

**Limite dichiarato.** L'indicatore misura il trasporto fra i dispositivi e il server DNS locale, non fra il server e i resolver esterni.

---

## Resolver Configuration

Valuta le impostazioni del resolver che incidono sulla privacy.

| Componente                                     | Punti |
| ---------------------------------------------- | ----- |
| Minimizzazione del nome interrogato attiva      | 2,5   |
| Inoltro della sottorete del client disattivato  | 2,5   |

La minimizzazione riduce le informazioni trasmesse ai server autoritativi.

L'inoltro della sottorete del client comunica ai server esterni la porzione di rete da cui proviene l'interrogazione: è una funzione di ottimizzazione che riduce la privacy, quindi il punteggio premia la sua **assenza**.

Dato: configurazione della Data Source.

---

## DNS Errors

Valuta la quota di interrogazioni che il servizio non è riuscito a soddisfare.

```text
punti = 5 × ( 1 − interrogazioni fallite / interrogazioni totali )
```

Le risposte di dominio inesistente non concorrono al conteggio delle interrogazioni fallite, trattandosi di risposte corrette.

In assenza di traffico l'indicatore è **non misurabile**: non esiste nulla su cui esprimere un giudizio.

Dato: statistiche.

---

## Removed Indicator

La versione precedente elencava un indicatore denominato **Query Validation**, privo di una definizione distinta da DNSSEC Validation.

È stato rimosso anziché reinterpretato. Un indicatore senza significato proprio avrebbe prodotto punti arbitrari.

---

# Classification-Based Areas

Privacy Protection e Threat Protection derivano entrambe dalla classificazione dei domini.

Ne condividono due condizioni.

---

## Minimum Observation

Sotto **cento interrogazioni** nel periodo, gli indicatori di queste due aree sono **non misurabili**.

Una rete che non ha contattato alcun dominio di tracciamento in tre interrogazioni non è una rete protetta: è una rete che non è stata osservata abbastanza.

Senza questa condizione la rete meno usata otterrebbe il punteggio migliore, e il punteggio misurerebbe il silenzio anziché la protezione.

---

## What Classification Can And Cannot Assert

Le liste affermano soltanto in positivo: dicono che un dominio traccia, non che non traccia.

Un dominio assente da ogni lista è `Unknown`, e `Unknown` comprende sia i domini innocui sia i tracciatori che nessuna lista conosce.

Ne discende un'asimmetria che governa la formulazione di questi indicatori.

| Osservazione | Affidabilità |
| ------------ | ------------- |
| Esposizione elevata | **Attendibile**: quei domini sono noti per tracciare |
| Esposizione nulla   | **Non attendibile come assoluzione**: può significare rete pulita oppure tracciatori sconosciuti |

Il calcolo è simmetrico, la formulazione no. Il punteggio pieno significa **nessun tracciamento noto**, mai *nessun tracciamento*, e la Network Privacy Specification vincola l'interfaccia a dirlo così.

Il valore dell'esposizione è sempre dichiarato come **limite inferiore**.

---

# Privacy Protection

Peso complessivo **20**, distribuito su due indicatori da **10** punti.

Le categorie considerate sono `Tracking`, `Analytics` e `Advertising`.

I due indicatori rispondono a domande diverse che l'utente si pone entrambe: *quanto mi tracciano* e *quanto me lo impediscono*. Il secondo da solo premierebbe un filtro efficace su una rete assediata; il primo da solo ignorerebbe il lavoro del filtro.

---

## Known Tracking Exposure

Quota di interrogazioni dirette a domini classificati in una categoria di privacy, sul totale delle interrogazioni.

Il conteggio è **per interrogazione, non per dominio**. Dieci domini di tracciamento contattati una volta ciascuno e un solo dominio contattato quattrocento volte descrivono reti diverse, e contare i domini le farebbe apparire uguali.

| Quota osservata | Punti |
| --------------- | ----- |
| Nessuna          | 10    |
| Fino al 2%       | 8     |
| Fino al 5%       | 6     |
| Fino al 10%      | 4     |
| Fino al 20%      | 2     |
| Oltre il 20%     | 0     |

Qualità della misura: **limite inferiore**.

Non misurabile quando le interrogazioni sono meno di cento, o quando nessuna lista è disponibile.

Dato: categoria e occorrenze dei domini, statistiche.

---

## Tracking Blocking

Quota di interrogazioni dirette a quei domini che sono state bloccate.

| Quota bloccata | Punti |
| -------------- | ----- |
| Almeno il 99%   | 10    |
| Almeno il 90%   | 8     |
| Almeno il 75%   | 6     |
| Almeno il 50%   | 4     |
| Almeno il 25%   | 2     |
| Inferiore       | 0     |

Non misurabile quando nessuna interrogazione è diretta a quei domini, e quando la Data Source non fornisce l'attività per dominio.

**L'assenza di tracciamento non produce un doppio giudizio.** Se non c'è nulla da bloccare, l'indicatore è escluso e il punteggio pieno arriva dall'esposizione. La copertura risulta inferiore a 100, ed è corretto: quel filtro non è stato messo alla prova.

Dato: attività per dominio, categoria dei domini.

---

## Removed Indicators

La versione precedente elencava **Tracker Blocking**, **Analytics Detection**, **Advertising Domains**, **Telemetry Detection** e **Privacy Configuration** come soli titoli.

I primi quattro descrivevano la stessa misura suddivisa per categoria, senza che le categorie avessero pesi distinti motivati. Sono confluiti nei due indicatori sopra.

**Privacy Configuration** è stato rimosso perché ogni impostazione che avrebbe potuto misurare è già valutata da Resolver Configuration e da Filtering Configuration. Contarla di nuovo avrebbe gonfiato il punteggio due volte per lo stesso fatto.

**Nota sulla categoria `Analytics`.** Nessuna lista predefinita la fornisce: i domini di analisi ricevono in pratica la categoria `Tracking`, secondo quanto dichiarato dalla fonte adottata. La categoria resta nel modello e non viene attribuita per convenienza.

---

# Threat Protection

Peso complessivo **25**, distribuito su due indicatori.

Le categorie considerate sono `Malware`, `Phishing` e `Cryptomining`, che costituiscono minacce **confermate** da una lista, e `Suspicious`, che costituisce una segnalazione **non confermata**.

La struttura ricalca quella di Privacy Protection, con due differenze motivate.

**L'esposizione si conta per dominio, non per quota.** Un dominio di malware contattato una sola volta è un fatto rilevante; diluirlo sul totale delle interrogazioni lo farebbe sparire. Per il tracciamento la proporzione è informativa, per una minaccia il numero assoluto lo è di più.

**La soglia del blocco è più alta.** Un tracciatore che passa costa privacy, un dominio di malware che passa può costare la macchina.

---

## Known Threat Exposure

Peso **12**.

| Condizione osservata                                          | Punti |
| ------------------------------------------------------------- | ----- |
| Nessun dominio di minaccia, nessun dominio sospetto             | 12    |
| Nessuna minaccia confermata, almeno un dominio sospetto         | 9     |
| Un dominio di minaccia confermata                               | 6     |
| Da due a cinque domini di minaccia confermata                   | 3     |
| Più di cinque                                                   | 0     |

Un dominio sospetto riduce il punteggio senza azzerarlo: la segnalazione non è confermata, e trattarla come una minaccia accertata attribuirebbe alla rete un problema che non è stato dimostrato.

Qualità della misura: **limite inferiore**.

Non misurabile quando le interrogazioni sono meno di cento, o quando nessuna lista è disponibile.

Dato: categoria dei domini, statistiche.

---

## Threat Blocking

Peso **13**.

Quota di interrogazioni dirette a domini di minaccia, confermata o sospetta, che sono state bloccate.

| Quota bloccata | Punti |
| -------------- | ----- |
| Totalità        | 13    |
| Almeno il 95%   | 10    |
| Almeno l'80%    | 6     |
| Almeno il 50%   | 3     |
| Inferiore       | 0     |

Non misurabile quando nessuna interrogazione è diretta a quei domini, e quando la Data Source non fornisce l'attività per dominio.

Dato: attività per dominio, categoria dei domini.

---

## Removed Indicator

La versione precedente elencava **Threat Intelligence** fra gli indicatori.

Non è un indicatore: è il nome del sottosistema che produce la classificazione da cui tutti gli altri derivano.

È stato rimosso anziché reinterpretato.

---

# Device Health

Analizza il comportamento dei dispositivi.

Indicatori.

* attività anomala;
* numero di Alert;
* traffico DNS;
* comportamento generale.

---

# Configuration

Valuta la qualità della configurazione complessiva.

Peso complessivo **10**, distribuito su due indicatori da **5** punti.

---

## Source Availability

| Condizione                              | Punti |
| --------------------------------------- | ----- |
| La Data Source ha risposto correttamente | 5     |
| La Data Source non è raggiungibile       | 0     |

Dato: stato della Data Source.

---

## Filtering Configuration

| Componente                          | Punti |
| ----------------------------------- | ----- |
| Filtraggio attivo                    | 2,5   |
| Almeno una lista di filtro configurata | 2,5  |

Il filtraggio attivo senza alcuna lista configurata non produce alcun effetto: le due componenti sono distinte perché descrivono condizioni diverse.

Dato: configurazione della Data Source.

---

## Redefined Indicators

La versione precedente elencava quattro indicatori: configurazione valida, servizi disponibili, sincronizzazione, stato operativo.

Erano sovrapposti fra loro e privi di criterio. Sono stati sostituiti da due indicatori definiti in modo verificabile.

---

# Network Integrity

Valuta la continuità e l'affidabilità dell'osservazione della rete.

Peso complessivo **10**, distribuito su due indicatori da **5** punti.

---

## Observation Continuity

Valuta quanti dei periodi di osservazione attesi sono stati effettivamente osservati.

```text
punti = 5 × ( periodi osservati / periodi attesi )
```

L'intervallo di riferimento predefinito è di ventiquattro ore.

Un periodo mancante indica che il sistema non ha potuto osservare la rete in quel lasso di tempo, e quindi che l'analisi presenta una lacuna.

**Nulla viene atteso prima della prima osservazione.** I periodi attesi decorrono dalla prima osservazione registrata, mai da prima.

Un'installazione recente verrebbe altrimenti penalizzata per non avere osservato la rete prima di esistere, il che non dice nulla sulla rete e attribuirebbe all'utente una lacuna che non gli appartiene.

Dato: periodi conservati.

---

## Acquisition Reliability

Valuta la quota di tentativi di acquisizione andati a buon fine.

Richiede la registrazione dei tentativi, compresi quelli falliti.

Finché tale registrazione non esiste, l'indicatore è **non misurabile**.

---

## Redefined Indicators

La versione precedente elencava errori, disponibilità, consistenza e stabilità.

Il primo duplicava l'indicatore DNS Errors, gli altri erano privi di definizione. Sono stati sostituiti da due indicatori riferiti alla continuità dell'osservazione, che è ciò che questa area può realmente misurare.

---

# Score Breakdown

Il sistema conserva il dettaglio del punteggio.

Ogni componente contribuisce al risultato finale in base al proprio peso.

Il breakdown comprende tutte e sei le aree di valutazione.

Esempio.

```text id="c8qqdc"
Overall Score

92 /100

Threat Protection

25 /25

DNS Security

19 /20

Privacy Protection

18 /20

Device Health

12 /15

Configuration

9 /10

Network Integrity

9 /10
```

Per ogni area il sistema conserva inoltre l'elenco dei fattori che hanno determinato il punteggio.

Questo dettaglio costituisce il requisito minimo per soddisfare il principio di Transparency.

---

# Measurement States

Non tutte le Data Sources forniscono i dati necessari a valutare ogni indicatore.

Ogni area di valutazione si trova quindi in uno di tre stati.

| Stato               | Condizione                                        |
| ------------------- | -------------------------------------------------- |
| `Measured`          | Tutti gli indicatori dell'area sono valutabili      |
| `PartiallyMeasured` | Solo una parte degli indicatori è valutabile        |
| `NotMeasurable`     | Nessun indicatore dell'area è valutabile            |

---

## Principle

Ciò che non è stato osservato viene **escluso** dal calcolo.

Non riceve punteggio zero, perché zero è un'affermazione sullo stato della rete: dichiarerebbe una condizione critica che il sistema non ha verificato.

Non riceve nemmeno un valore stimato o presunto favorevole, perché sarebbe un'affermazione altrettanto infondata.

La porzione non osservata semplicemente non entra nel conteggio, né al numeratore né al denominatore.

Ne consegue che **l'assenza di dati non può in alcun caso migliorare il punteggio**.

---

## Partial Measurement

Quando un'area è parzialmente misurabile, il punteggio ottenibile è proporzionale alla quota di indicatori valutati.

```text
maxScore = weight × ( indicatori valutati / indicatori totali dell'area )
```

Gli indicatori di una stessa area hanno peso uguale fra loro, salvo diversa indicazione esplicita nella presente specifica.

Questa convenzione rende il calcolo deterministico e verificabile, ed è estendibile assegnando in futuro pesi specifici ai singoli indicatori.

---

## Calculation

```text
overallScore = ( somma dei punteggi ottenuti
                 / somma dei maxScore ) × 100
```

Il valore risultante mantiene la scala da 0 a 100 e la corrispondenza con la tabella Score Range.

---

## Minimum Coverage

Il punteggio complessivo **non viene prodotto** quando la copertura è inferiore a **60**.

Al di sotto di quel livello il giudizio sintetico poggerebbe su meno di tre quinti del sistema di valutazione, e un numero unico comunicherebbe una completezza che non esiste.

In tale condizione il sistema presenta il **solo breakdown**, con le aree misurate, quelle parziali e quelle non misurabili con il relativo motivo.

Non si tratta di un malfunzionamento e non va presentato come tale: il sistema sta dichiarando di non disporre di elementi sufficienti per un giudizio complessivo, pur avendo misure valide da mostrare.

La soglia appartiene all'algoritmo e ne segue il versionamento.

---

## Coverage

La copertura è la somma dei `maxScore` di tutte le aree, su un massimo di 100.

Rappresenta la porzione del sistema di valutazione effettivamente osservata.

Esempio, riferito a una Data Source che non fornisce Domain Activity.

L'area Device Health prevede quattro indicatori. Uno soltanto, il volume di traffico DNS, è valutabile senza Domain Activity.

```text
maxScore di Device Health = 15 × ( 1 / 4 ) = 3,75
```

```text
Overall Score

90 /100

Copertura

88,75 /100

Aree misurate

DNS Security          19    /20
Privacy Protection    18    /20
Threat Protection     22    /25
Configuration          9    /10
Network Integrity      9    /10

Area parzialmente misurata

Device Health          3    /3,75
   Valutato      : volume di traffico DNS
   Non valutato  : attività anomala, numero di Alert, comportamento generale
   Motivo        : la Data Source non fornisce Domain Activity
```

Il punteggio deriva da 80 punti ottenuti su 88,75 punti osservabili.

Rispetto a una valutazione che avesse dichiarato l'intera area non misurabile, la copertura sale da 85 a 88,75: il sistema riconosce il dato che possiede realmente, senza attribuirsi quello che non ha.

Si noti che riconoscere la misurazione parziale **non ha migliorato il punteggio**, sceso da 91 a 90. Il dato aggiuntivo ha aumentato la porzione osservata e vi ha contribuito con il proprio valore effettivo, che era inferiore alla media delle altre aree.

Questo è il comportamento atteso: la misurazione parziale aumenta la conoscenza, non il risultato.

---

## Comparability

Due punteggi con copertura differente **non sono confrontabili**.

Il sistema deve sempre presentare la copertura accanto al punteggio quando questa è inferiore a 100.

Il confronto storico del trend è ammesso soltanto fra rilevazioni con la stessa copertura. Un cambiamento di copertura interrompe la serie e deve essere segnalato.

---

## Recommendation

Ogni area non misurabile o parzialmente misurata produce una Recommendation.

La Recommendation indica:

* quale area non è valutabile;
* quale dato manca;
* quale intervento renderebbe il dato disponibile;
* quali conseguenze comporta tale intervento.

L'ultimo punto è vincolante: se rendere disponibile un dato comporta un costo, quel costo va dichiarato insieme al beneficio.

Il sistema non propone all'utente configurazioni presentandone soltanto i vantaggi.

---

# Score History

Ogni aggiornamento del NPSS viene memorizzato.

Lo storico consente:

* confronti temporali;
* analisi dei trend;
* report periodici.

---

# Score Trend

Il sistema calcola automaticamente la variazione del punteggio.

Stati previsti.

* Improving
* Stable
* Decreasing

---

# Positive Factors

Il punteggio aumenta quando vengono rilevate configurazioni corrette.

Esempi.

* DNSSEC attivo;
* DNS cifrato;
* assenza di malware;
* blocklist aggiornate;
* configurazione valida.

---

# Negative Factors

Il punteggio diminuisce quando vengono rilevate condizioni critiche.

Esempi.

* malware;
* phishing;
* DNSSEC disattivato;
* configurazioni errate;
* Alert critici.

---

# Transparency

Ogni variazione del punteggio deve essere spiegabile.

Il sistema conserva il dettaglio degli indicatori che hanno contribuito al risultato.

---

# Backend Independence

L'algoritmo NPSS utilizza esclusivamente il Unified Data Model.

Non contiene dipendenze dirette dai backend.

---

# Versioning

L'algoritmo possiede una propria versione indipendente.

Le modifiche sostanziali incrementano la Major Version.

---

# Design Principles

Il NPSS segue i seguenti principi.

* semplicità;
* trasparenza;
* uniformità;
* riproducibilità;
* indipendenza;
* aggiornamento continuo.

---

# Constraints

Il NPSS:

* non rappresenta una certificazione di sicurezza;
* non misura esclusivamente la privacy;
* non dipende da una specifica tecnologia;
* non assegna punteggio zero a ciò che non è stato misurato;
* non è confrontabile fra rilevazioni con copertura differente;
* rappresenta un indicatore sintetico prodotto dal Core.

---

# Related Specifications

* 00 - Glossary
* 02 - Architecture
* 03 - Privacy Intelligence Engine
* 05 - Data Model
* 06 - API
* 08 - Threat Intelligence
* 09 - Network Privacy
