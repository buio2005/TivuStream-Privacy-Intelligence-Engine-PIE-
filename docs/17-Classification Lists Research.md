# 17 - Classification Lists Research

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Classification Lists Research

**Version:** 1.1.0

**Status:** Analysis — Not Approved

**Last Updated:** 2026-08-04

---

# Purpose

Questo documento raccoglie la ricerca sulle fonti di classificazione dei domini.

La Specification 08 definisce il **meccanismo** e impone che ogni lista dichiari la propria licenza. Non nomina alcuna fonte, perché la scelta richiede una verifica che non può essere svolta scrivendo una specifica.

Il documento riporta ciò che è stato verificato il **4 agosto 2026** e propone una decisione. Non la assume.

---

# Method

Le licenze sono state lette **dai file di licenza delle fonti**, non da riassunti di terzi.

La distinzione non è formale. Una ricerca preliminare riportava il Block List Project come licenziato MIT; il file `LICENSE` del progetto dichiara **Unlicense**. Sono entrambe licenze permissive, ma una fonte che riferisce male un fatto verificabile non è una fonte su cui fondare una decisione legale.

Ogni riga della tabella seguente proviene da un documento pubblicato dal manutentore della lista.

---

# What We Distribute And What We Do Not

La distinzione governa l'intera analisi.

| Cosa | Chi lo fa | Cosa implica |
| --- | --- | --- |
| Contenuto della lista | L'utente lo scarica sulla propria macchina | Non lo distribuiamo |
| Nome, indirizzo, licenza dichiarata | Distribuito con il progetto | È configurazione, non contenuto |

PIE **non redistribuisce alcuna lista**. Il progetto contiene un indirizzo e una dichiarazione di licenza; il file arriva sulla macchina dell'utente, su sua istruzione, e non lascia mai quella macchina.

Questo riduce l'esposizione ma non la annulla. Indicare una fonte come predefinita è un'indicazione che diamo noi, e resta una responsabilità nostra.

---

# Findings

## Verified Sources

| Fonte | Licenza | Verificata su | Struttura | Formato |
| --- | --- | --- | --- | --- |
| **Block List Project** | Unlicense (pubblico dominio) | `LICENSE` nel repository | 18 liste **per categoria** | hosts, solo dominio, dnsmasq, adblock |
| **HaGeZi DNS Blocklists** | GPL-3.0 | `LICENSE` nel repository | Liste **combinate** | adblock, dnsmasq, altri |
| **oisd** | GPL-3.0 | FAQ ufficiale, sezione licenza | Liste **combinate** | adblock, dnsmasq, wildcard |
| **StevenBlack hosts** | MIT | `license.txt` nel repository | Lista **unificata** con varianti | hosts |
| **Disconnect** | **CC BY-NC-SA 4.0** | `LICENSE` nel repository | Categorie esplicite in JSON | JSON |
| **Peter Lowe (pgl.yoyo.org)** | **Non dichiarata** | Pagina policy: nessuna licenza | Lista unica | hosts, molti altri |
| **ShadowWhisperer** | Unlicense (pubblico dominio) | `LICENSE` nel repository | 23 liste **per categoria** | solo dominio |
| **lightswitch05 hosts** | Apache-2.0 | `LICENSE` nel repository | Liste **combinate** (`ads-and-tracking`) | hosts, altri |
| **Phishing Army** | **CC BY-NC 4.0** | Sito ufficiale | Lista unica di phishing | solo dominio |
| **DuckDuckGo Tracker Blocklists** | **CC BY-NC-SA 4.0** | Repository ufficiale | Categorie esplicite | JSON |

---

## Two Exclusions Follow From Our Own Rules

**Peter Lowe.** La pagina di policy descrive con precisione i criteri di inclusione e la procedura di validazione, e **non dichiara alcuna licenza**. La Specification 08 rende la licenza obbligatoria. La lista è esclusa dalla nostra regola, non da un giudizio sulla sua qualità, che è alta.

**Disconnect.** La clausola **NonCommercial** vincola l'uso. PIE non è un prodotto commerciale, ma è software che altri possono installare in qualsiasi contesto, compreso quello di un'azienda. Indicare come predefinita una fonte che vieta l'uso commerciale significherebbe imporre all'utente una condizione che l'utente non ha scelto e probabilmente non leggerà.

Resta una lista che l'utente **può aggiungere consapevolmente**. Non una che aggiungiamo noi al posto suo.

---

## A Pattern In The Ecosystem

Le fonti con la metodologia più solida sul tracciamento sono **tutte NonCommercial**.

| Fonte | Metodologia | Licenza |
| --- | --- | --- |
| Disconnect | Verifica umana, categorie esplicite, usata da Firefox | CC BY-NC-SA 4.0 |
| DuckDuckGo Tracker Blocklists | Misurazione automatica: crawl dei siti più visitati, osservazione di cookie e API di fingerprinting | CC BY-NC-SA 4.0 |
| Phishing Army | Aggregazione di segnalazioni di phishing | CC BY-NC 4.0 |

Non è una coincidenza. Costruire una fonte con una metodologia misurabile costa, e chi sostiene quel costo si riserva l'uso commerciale.

La conseguenza per noi è che **la qualità metodologica e la libertà di licenza non stanno dalla stessa parte**. Le fonti liberamente utilizzabili sono curate da volontari, per segnalazione e revisione manuale; quelle costruite per misurazione sistematica sono vincolate.

Registriamo il fatto anziché scegliere la fonte migliore e sperare che la licenza non conti.

---

## A Structural Constraint We Discovered

Il nostro modello associa **una categoria a una lista**. `ClassificationList` possiede una sola proprietà `Category`, e ogni dominio trovato in quella lista riceve quella categoria.

Questo esclude le liste combinate.

HaGeZi, oisd e StevenBlack raccolgono in un unico file domini pubblicitari, di tracciamento, di malware e di phishing. Dichiararne una come `Advertising` significherebbe attribuire quella categoria anche ai domini di malware che contiene: un'affermazione falsa prodotta dalla struttura, non da un errore.

Sono liste eccellenti per **bloccare**, che è il loro scopo. Sono inadatte a **classificare**, che è il nostro.

La conseguenza è netta: fra le fonti verificate, **soltanto il Block List Project e ShadowWhisperer sono compatibili con il modello**.

---

## The Second Source Is Upstream Of The First

Il Block List Project dichiara di sincronizzarsi quotidianamente con quattordici fonti a monte, e **ShadowWhisperer è fra queste**.

ShadowWhisperer dichiara l'opposto: *"I will not merge other lists"*. Le sue liste nascono da uno script personale e da aggiunte manuali.

L'ordine è quindi rovesciato rispetto all'apparenza: ShadowWhisperer è una fonte **primaria**, il Block List Project è in parte **derivato**.

Adottarle entrambe come predefinite darebbe l'aspetto di una pluralità di opinioni senza esserlo. Su un dominio contestato le due fonti tenderanno a concordare, perché una legge l'altra, e l'accordo verrebbe letto come conferma indipendente.

Questo non le rende inutili insieme, e rende necessario dichiarare la relazione anziché presentarle come due pareri.

---

## Where The Second Source Does Not Fit

Le categorie di ShadowWhisperer non coincidono con le nostre.

| Lista | Contenuto dichiarato | Compatibilità |
| --- | --- | --- |
| `Ads` | Pubblicità, banner, notifiche push | Corrisponde a `Advertising` |
| `Tracking` | Analytics, diagnostica, posizione, metriche | Corrisponde a `Tracking`, e copre anche l'analisi |
| `Scam` | Truffe su prodotti, spedizioni, assistenza | Corrisponde a `Suspicious` |
| `Malware` | Malware, **phishing**, PUP, redirector, truffatori remoti | **Incompatibile** |
| `Cryptocurrency` | Bitcoin, Ethereum, mining, esplicitamente *non* malware | **Incompatibile** |

`Malware` unisce malware e phishing, che nel nostro modello sono due categorie distinte. Dichiarare quella lista come `Malware` attribuirebbe la categoria sbagliata a ogni dominio di phishing che contiene.

`Cryptocurrency` comprende scambi e servizi legittimi, non solo cryptojacking. La nostra categoria `Cryptomining` significa un'altra cosa.

Restano utilizzabili tre liste su ventitré, proprio nelle categorie dove il Block List Project è già presente e dove il rischio di sbagliare è minore.

Nelle due categorie in cui un errore conta di più — malware e phishing — la seconda fonte **non aggiunge nulla di dichiarabile con verità**.

---

## A Constraint That Does Not Apply To Us

La FAQ di oisd spiega perché il progetto ha abbandonato i formati hosts e solo-dominio: quei formati non possono esprimere un carattere jolly, e quindi richiedono di elencare ogni sottodominio noto, senza poter coprire quelli ignoti o generati casualmente.

L'obiezione è corretta per chi consuma la lista riga per riga.

**Non si applica al nostro motore.** La regola di corrispondenza risale dal nome completo verso il dominio superiore: una riga `example.com` copre già `bad239ue9f59gw.example.com`, dichiarando l'esito come inferenza a confidenza `Medium`.

Il formato compatto è quindi sufficiente per noi, e la scelta di risalire — presa per ragioni di onestà, per distinguere un'affermazione da una deduzione — si rivela anche la scelta tecnicamente più efficiente.

---

# An Unresolved Rule In Specification 08

La ricerca ha portato alla luce una lacuna nella specifica già approvata.

Un dominio può comparire in **più liste con categorie diverse**. Un dominio pubblicitario che traccia anche l'utente appartiene legittimamente sia a `ads` sia a `tracking`, e ShadowWhisperer lo dichiara apertamente: *"Categories like Ads and Tracking may contain domains that do both"*.

La Specification 08 stabilisce che la ricerca si arresta alla prima corrispondenza. **Non stabilisce in quale ordine le liste vengano consultate.**

Oggi l'ordine è quello in cui il motore le riceve. Un dettaglio implementativo decide quindi quale categoria l'utente vede, e la stessa installazione potrebbe mostrare categorie diverse dopo una semplice riorganizzazione del codice.

La lacuna esiste già con le sole sette liste proposte. Aggiungere fonti la rende più frequente, non la introduce.

Va risolta prima di collegare il motore, perché riguarda ciò che viene affermato all'utente e non il modo in cui viene calcolato.

Tre soluzioni possibili, in ordine di costo crescente.

| Soluzione | Cosa comporta |
| --- | --- |
| Ordine dichiarato | Ogni lista possiede una priorità esplicita e configurabile. La scelta resta arbitraria ma diventa visibile e modificabile. |
| Categoria più specifica | Si definisce una gerarchia fra categorie. Richiede di stabilire che `Malware` prevale su `Advertising`, cioè un giudizio editoriale da dichiarare. |
| Classificazioni multiple | Un dominio porta tutte le categorie trovate. È la soluzione più veritiera e comporta una modifica al Data Model e all'interfaccia. |

Non propongo quale adottare in questo documento. È una decisione sulla verità di ciò che mostriamo, non sull'implementazione.

---

# Methodology Of The Proposed Source

Il Block List Project dichiara pubblicamente il proprio procedimento.

| Aspetto | Quanto dichiarato |
| --- | --- |
| Validazione | 151 test automatici a ogni modifica, verifica sintattica e del TLD |
| Domini morti | Scansione settimanale e rimozione |
| Falsi positivi | Segnalazione pubblica su GitHub, revisione umana |
| Protezione | Elenco di domini essenziali che non possono essere bloccati |
| Fonti a monte | 14 liste monitorate quotidianamente, fra cui HaGeZi e ShadowWhisperer |
| Aggiornamento | Quotidiano |

Il procedimento è ispezionabile: il codice di costruzione, i test e la cronologia delle decisioni sono pubblici.

È il criterio che conta più della licenza. Una lista con licenza perfetta e curatela opaca ci farebbe fare affermazioni di cui non conosciamo il fondamento.

---

# Risks To Declare

**Monocoltura.** Una sola fonte predefinita significa che i suoi errori diventano i nostri, e i suoi silenzi diventano `Unknown`. Il modello prevede più liste e l'aggiunta da parte dell'utente; la configurazione predefinita resta comunque una scelta nostra e va dichiarata come tale.

La ricerca di una seconda fonte non ha risolto il rischio. La sola fonte compatibile e liberamente licenziata è a monte della prima, e nelle due categorie dove sbagliare costa di più non è utilizzabile. Una pluralità apparente sarebbe stata peggiore di una monocoltura dichiarata.

**Licenza a monte.** Il Block List Project si dichiara di pubblico dominio e aggrega fonti fra cui HaGeZi, che è GPL-3.0. Se un elenco di domini sia opera protetta è questione controversa: negli Stati Uniti i fatti non sono coperti da copyright in assenza di selezione creativa, mentre nell'Unione Europea esiste un diritto sui generis sulle banche dati. Non siamo in grado di risolverla, e non è una consulenza legale. La registriamo perché esiste.

**Categorie senza fonte.** Le categorie `Analytics`, `Streaming`, `Cloud` e `AI Services` non hanno alcuna lista corrispondente. La lista `tracking` del progetto dichiara di coprire "tracking/analytics", quindi i domini di analisi ricevono `Tracking`.

Nessuna categoria viene inventata per riempire un vuoto: un dominio senza lista resta `Unknown`, che significa non classificato.

---

# Proposal

Adottare come predefinite **sette liste del Block List Project**, nel formato solo-dominio.

| Lista | Categoria PIE | Contenuto dichiarato |
| --- | --- | --- |
| `ads-nl.txt` | `Advertising` | Server pubblicitari |
| `tracking-nl.txt` | `Tracking` | Tracciamento e analisi |
| `malware-nl.txt` | `Malware` | Host di malware |
| `phishing-nl.txt` | `Phishing` | Siti di phishing |
| `crypto-nl.txt` | `Cryptomining` | Cryptojacking e truffe su criptovalute |
| `scam-nl.txt` | `Suspicious` | Siti truffaldini |
| `abuse-nl.txt` | `Suspicious` | Siti ingannevoli o abusivi |

Motivazioni.

* È l'unica fonte verificata **segmentata per categoria**, e quindi l'unica compatibile con il modello.
* La licenza Unlicense non impone alcuna condizione all'utente.
* Il formato solo-dominio è già compreso dal lettore, verificato da diciotto prove.
* Il procedimento di curatela è pubblico e verificabile.

Le liste `facebook`, `twitter`, `tiktok` e `whatsapp` corrisponderebbero alla categoria `Social`, e **non sono proposte**: elencano i domini di servizi che l'utente potrebbe usare deliberatamente, e classificarli come minaccia sarebbe un giudizio editoriale che non spetta a noi. Restano disponibili come liste aggiungibili.

Le liste `porn`, `gambling`, `drugs`, `piracy` e `torrent` riguardano il **contenuto**, non la privacy né la sicurezza. Sono fuori dallo scopo del progetto.

---

# What This Document Does Not Decide

* Se adottare la proposta. È una decisione da assumere esplicitamente.
* La licenza del progetto stesso, che resta da scegliere.
* L'intervallo di aggiornamento predefinito.
* Il lettore debba imparare il formato adblock, oggi non supportato.
* **Come risolvere il conflitto fra liste che rivendicano lo stesso dominio.** È la decisione più urgente: riguarda ciò che viene affermato all'utente, e la lacuna esiste già oggi.

---

# Sources

Tutte consultate il 2026-08-04.

* Block List Project — [repository](https://github.com/blocklistproject/Lists), [elenco delle liste](https://blocklistproject.github.io/Lists/), [licenza](https://raw.githubusercontent.com/blocklistproject/Lists/master/LICENSE)
* ShadowWhisperer — [repository](https://github.com/ShadowWhisperer/BlockLists), [categorie](https://raw.githubusercontent.com/ShadowWhisperer/BlockLists/master/README.md), [licenza](https://raw.githubusercontent.com/ShadowWhisperer/BlockLists/master/LICENSE)
* lightswitch05 hosts — [licenza](https://raw.githubusercontent.com/lightswitch05/hosts/master/LICENSE)
* Phishing Army — [sito ufficiale](https://phishing.army/)
* DuckDuckGo Tracker Blocklists — [repository](https://github.com/duckduckgo/tracker-blocklists)
* HaGeZi DNS Blocklists — [repository](https://github.com/hagezi/dns-blocklists), [licenza](https://raw.githubusercontent.com/hagezi/dns-blocklists/main/LICENSE)
* oisd — [FAQ, sezione licenza](https://oisd.nl/faq)
* StevenBlack hosts — [licenza](https://raw.githubusercontent.com/StevenBlack/hosts/master/license.txt)
* Disconnect — [licenza](https://raw.githubusercontent.com/disconnectme/disconnect-tracking-protection/master/LICENSE)
* Peter Lowe — [policy di inclusione](https://pgl.yoyo.org/adservers/policy.php)

---

# Related Specifications

* 05 - Data Model
* 08 - Threat Intelligence
* 16 - Persistence
