# Changelog

Tutte le modifiche rilevanti al progetto **TivuStream Privacy Intelligence Engine (PIE)** vengono registrate in questo documento.

La documentazione segue un versionamento indipendente da quello del codice.

Il progetto utilizza il versionamento semantico nel formato `MAJOR.MINOR.PATCH`.

---

## Milestone A6 — Frontend: stati, schermate, catalogo — 2026-09-26

Sesta delle sette milestone della Specification 18. L'interfaccia torna utilizzabile: una nuova installazione si configura dal browser, si accede, si cambia la password, e un amministratore gestisce gli account.

### Added

**Stati**

* `stores/session.ts` tiene i cinque stati della specifica (`Checking`, `SetupRequired`, `Unauthenticated`, `Authenticated`, `PasswordChangeRequired`), sempre uno solo. `App.vue` mostra una schermata per stato, e **le schermate dei dati non vengono montate** in nessuno stato tranne `Authenticated`: prima di entrare, e con la password da cambiare, il punteggio non viene neppure chiesto.
* Un motore che non risponde durante la verifica della sessione **non** porta a `Unauthenticated`: si resta in `Checking`, con il messaggio `auth.engineUnreachable` e un pulsante per riprovare. Non si sa ancora se una sessione esista.
* Un `401 AuthenticationRequired`, da qualunque schermata, porta a `Unauthenticated` con «La sessione è terminata», ma solo se si era dentro. Un `403 PasswordChangeRequired` a metà sessione porta al cambio password. Le due reazioni stanno in un solo punto: il client dell'API le notifica, lo store decide.

**Client dell'API**

* `call()` distingue una risposta di rifiuto da un motore che non ha risposto (`EngineUnreachable`), comprese le risposte che non sono del motore, come la pagina d'errore di un proxy. `read()`, usata dagli store esistenti, ne è costruita sopra.
* `api/messages.ts` traduce un rifiuto in una voce del catalogo **partendo dal solo codice e dal motivo**, mai dal testo del motore. Un motore che non ha risposto ha due frasi diverse: nell'accesso «le credenziali non sono state verificate», in una modifica «non si può sapere se la modifica sia stata applicata». Un codice sconosciuto si mostra con il proprio nome.

**Schermate**

* Configurazione iniziale, accesso, cambio password (obbligato o volontario, dal nome dell'utente nell'intestazione), gestione degli account (solo `Administrator`, voce di menu e rotta).
* La gestione degli account rilegge l'elenco dopo ogni operazione invece di correggerlo localmente: si mostra ciò che è, non ciò che si sperava. La rimozione si conferma sulla pagina, non con una finestra del browser.
* Ogni modulo che porta una password è `method="post"`: se lo script mancasse, un invio in `GET` metterebbe la password nell'indirizzo. Le password si svuotano dal modulo dopo ogni invio, riuscito o no, e non toccano alcuna memoria persistente. I campi dichiarano `autocomplete` corretti, così un gestore di password distingue la password attuale da una nuova.
* Due password nuove diverse fra loro non vengono inviate: è l'unica regola che l'interfaccia controlla da sé, perché i due campi sono una sua idea. Tutte le regole sulla password restano del motore.

**Uscita (F2)**

* All'uscita, e quando la sessione finisce da sola, gli store del punteggio, dei domini e degli account vengono svuotati (`reset()` in ciascuno). Anche se il motore non risponde all'uscita, il browser dimentica comunque.

**Catalogo**

* Le frasi di A6, approvate il 2026-09-26, in italiano e in inglese; le sei della Specification 18 riportate identiche. Il ruolo `Viewer` è «Lettore» in italiano.

**Prove — 41 nuove, da 35 a 76 sul frontend**

| Impegno | Cosa si verifica |
| --- | --- |
| Stati | Installazione senza account: solo la configurazione. Nessuna sessione: accesso senza «sessione terminata». Motore muto: nessun modulo, pulsante per riprovare. Password da cambiare: nessuna sezione e **nessuna richiesta del punteggio**. Voce Account solo per un amministratore |
| F1 | Credenziali sbagliate, motore muto, sessione terminata e connessione non sicura danno **quattro messaggi diversi, in entrambe le lingue**; «credenziali errate» solo quando il motore lo dice, non per la pagina d'errore di un proxy; i secondi di `Retry-After` compaiono nel messaggio |
| F2 | All'uscita, alla fine spontanea della sessione e con un motore che non risponde all'uscita: nessuno store conserva dati, e i domini non compaiono più sulla pagina |
| F3 | Tre rifiuti con tre testi diversi del motore producono la stessa frase |
| F5 | Parità del catalogo, e cinque nuove qualificazioni che devono sopravvivere alla traduzione: il motore muto non ha verificato nulla, non si sa se la modifica sia applicata, il trattenuto non dice nulla, il `Viewer` non vede i dispositivi, chi crea un account ne conosce la password |
| Account | Elenco con ruolo, stato e cambio password in attesa; creazione con il corpo giusto e password svuotata; rifiuto con il suo motivo; motore muto senza verdetto; codice sconosciuto mostrato; rimozione solo dopo conferma; reimpostazione con il corpo che il motore legge come tale; un `Viewer` che chiede la rotta viene riportato alla dashboard |
| Password | Viaggia nel corpo di un `POST` e non resta nel modulo né in `localStorage`; dal cambio obbligato si arriva ai dati; due password diverse non vengono inviate; il motivo di un rifiuto è detto nella lingua di chi legge |

Il finto motore delle prove risponde per metodo e percorso, può non rispondere affatto, e fa fallire la prova per ogni richiesta non prevista invece di inventare una risposta.

### Verified

Diciassette difetti introdotti di proposito, tutti intercettati, e una modifica innocua come controllo che non ha fatto fallire nulla: motore muto trattato come credenziali errate; secondi fissi nel messaggio dei tentativi; domini o account non svuotati all'uscita; «sessione terminata» mostrato anche senza sessione; obbligo di cambio ignorato a metà sessione; dati mostrati con la password da cambiare; password lasciata nel modulo; controllo delle due password tolto; uscita che dimentica solo se il motore risponde; elenco degli account non riletto; guardia della rotta tolta; voce Account per tutti; modulo senza `POST`; motore muto alla verifica trattato come uscita; stessa frase per il motore muto in accesso e in modifica; `AuthenticationFailed` non tradotto.

Attraverso il proxy di Vite, contro il backend vero su un database temporaneo: configurazione iniziale, creazione di un account e rifiuto di un `Origin` estraneo si comportano come previsto. `Host` e `Origin` che il proxy inoltra coincidono, quindi i controlli di A5 non ostacolano lo sviluppo.

Controllo dei tipi pulito, compilazione di produzione riuscita, 76 prove sul frontend.

### Known Impact

**Nessuna verifica visiva in un browser.** L'estensione del browser non era collegata. Le schermate sono provate montate con il catalogo vero e contro il backend attraverso il proxy, ma nessuno le ha ancora guardate.

**F4 non è verificato.** Il Frontend non ha una pagina di dettaglio del dominio, quindi l'attività per dispositivo, e con essa `Withheld`, non compare da nessuna parte. Il messaggio `domains.activityWithheld` è nel catalogo. La pagina di dettaglio è un lavoro a sé: richiede frasi nuove per l'attività disponibile e per quella non offerta dalla sorgente.

**La sessione scade senza preavviso.** L'interfaccia non conta il tempo: se ne accorge alla prima richiesta rifiutata, e allora dice che la sessione è terminata.

**La gestione degli account non ha ricerca né paginazione.** Adatta a una casa, non a un'organizzazione.

**Le schermate non sono state provate con la tastiera né con un lettore di schermo.** Hanno etichette, ruoli `alert` e `status` e un contorno di focus visibile, ma nessuno le ha usate così.

---

## Documentation Release 1.7.1 — Dove si verifica F4 — 2026-09-26

### Changed

**Specification 18 alla 1.3.1:** la milestone A6 verifica F1–F3 e F5. F4 si verifica con la pagina di dettaglio del dominio, che il Frontend non ha ancora. Rimandarla è stato deciso il 2026-09-26.

**Roadmap 1.3.4, README e PROJECT_CONTEXT** allineati alla Documentation Release 1.7.1.

---

## Milestone A5 — Tentativi, trasporto, `Origin`, `Host` — 2026-09-26

Quinta delle sette milestone della Specification 18. Le protezioni che non dipendono da chi chiede: chi prova password a ripetizione viene rallentato, una password non viaggia in chiaro fuori dalla macchina, e una pagina di un altro sito non può usare il browser dell'amministratore.

### Added

**Tentativi (V8)**

* `AttemptLimiter`: 5 fallimenti per indirizzo, 10 per nome. Il ritardo è di 30 secondi e raddoppia a ogni fallimento successivo, fino a 15 minuti. Durante il ritardo si risponde `429 TooManyAttempts` con `Retry-After`, **senza verificare le credenziali**, nemmeno quelle giuste.
* Contano come fallimenti: un `login` rifiutato, un codice di `setup` sbagliato (solo per indirizzo) e una password attuale sbagliata nel cambio password (D10). Un nome o una password non conformi non contano, perché non mettono alla prova alcun segreto.
* Il contatore del nome vale per il nome **tentato**, che esista o no: rallentare solo i nomi esistenti rivelerebbe quali esistono. Un accesso riuscito azzera il contatore del nome, non quello dell'indirizzo.
* Un indirizzo IPv6 si conta per il suo prefisso /64, e un IPv4 scritto come IPv6 si conta come IPv4. Gli indirizzi sconosciuti condividono un solo contatore.
* **Oblio (D11):** un contatore si dimentica dopo 15 minuti senza fallimenti **dalla fine del suo ritardo**.

**Trasporto (V9)**

* Ogni richiesta che porta una password (D9) risponde `403 TransportNotSecure` se non arriva su connessione cifrata o dal loopback. Il criterio è l'indirizzo remoto della connessione. Tutto 127.0.0.0/8 è loopback, anche scritto come IPv6; un indirizzo sconosciuto non lo è. Il controllo precede ogni altro, e un rifiuto per il trasporto non conta come tentativo.
* Su connessione cifrata le risposte portano `Strict-Transport-Security: max-age=31536000`.

**Origin (V13)**

* Una richiesta che modifica dati con un `Origin` diverso da schema, nome e porta del servizio riceve `403 OriginNotAllowed`, anche su `login` e `setup`. Una richiesta senza `Origin` passa: la mandano solo i client che non sono browser.

**Host (V14)**

* `AllowedHosts` ha nel codice il valore predefinito `localhost;127.0.0.1;[::1]`, che vale anche quando l'impostazione manca. **`*` impedisce l'avvio**, con un messaggio che spiega perché. `appsettings.Local.json.example` mostra come aggiungere un nome.

**Intestazioni**

* Ogni risposta porta `Content-Security-Policy: frame-ancestors 'none'`.

**Prove — 48 nuove, da 314 a 362 sul backend**

| Impegno | Cosa si verifica |
| --- | --- |
| V8, unità | Soglie, raddoppio fino al tetto, azzeramento dopo un accesso riuscito solo per il nome, oblio a 15 minuti dalla fine del ritardo e non prima, ritardo più lungo che non finisce con un contatore vuoto, chiavi IPv6 e IPv4 |
| V8, API | Cinque errori bloccano anche la password giusta per 30 secondi, con `Retry-After: 30` e senza cookie, mentre un altro dispositivo non è bloccato. Dieci errori su un nome lo bloccano da ovunque, **con la stessa risposta per un nome inesistente**. Un codice di `setup` giusto funziona dopo il ritardo. Una password non conforme non conta. Una password attuale sbagliata conta |
| V9 | Tutte e cinque le richieste che portano una password sono rifiutate da un altro dispositivo in chiaro, senza cambiare nulla; un `PATCH` senza password passa; il loopback è accettato in ogni forma, un indirizzo sconosciuto no; su HTTPS si entra da ovunque, con `Secure` e `Strict-Transport-Security` |
| V13 | Un `Origin` estraneo, con un'altra porta, con un altro schema o `null` è rifiutato e non cambia nulla; lo stesso vale per `login`; la lettura non è toccata; l'`Origin` del servizio e l'assenza di `Origin` passano |
| V14 | Un nome non elencato riceve `400`; i nomi del loopback passano senza configurazione; un nome aggiunto passa; `*` impedisce l'avvio |

L'host di prova dà a ogni richiesta l'indirizzo del loopback, o quello che la prova dichiara: il server in memoria lo lascerebbe sconosciuto, e ora conta.

### Verified

Diciannove difetti introdotti di proposito, tutti intercettati, e un controllo con una modifica innocua che non ha fatto fallire nulla.

Uno di questi, la conversione degli indirizzi IPv4 scritti come IPv6, inizialmente **non era intercettato**. Il framework riconosce `::ffff:127.0.0.1` come loopback ma non il resto di 127.0.0.0/8 scritto allo stesso modo: la prova usava proprio l'unico indirizzo che non serviva a nulla. È stata aggiunta `::ffff:127.5.6.7`, e il codice spiega perché la conversione esiste.

Verifica sull'eseguibile vero, avviato con la configurazione di produzione: `Host` estraneo `400`, `Origin` estraneo `403 OriginNotAllowed`, e le intestazioni presenti.

Compilazione senza avvisi. 362 prove sul backend e 35 sul frontend, tre esecuzioni complete consecutive senza fallimenti.

### Known Impact

**Un proxy sulla stessa macchina rende ogni client locale.** Se un proxy davanti a PIE accetta connessioni in chiaro e le inoltra dal loopback, il controllo del trasporto lo considera idoneo, e tutti i client condividono il contatore dell'indirizzo del proxy. Un proxy che termina HTTPS, al contrario, fa vedere a PIE `http` mentre il browser dichiara `https` nell'`Origin`: le richieste che modificano dati sarebbero rifiutate. Le intestazioni inoltrate non sono lette. Tutto questo appartiene alla Transport Security Specification, che ancora non esiste.

**Chi raggiunge l'installazione con un nome diverso dal loopback** deve aggiungerlo ad `AllowedHosts` in `appsettings.Local.json`. Senza, riceve un `400` senza spiegazione. La procedura di installazione dovrà dirlo.

**Il `400` per un `Host` non elencato non ha la struttura comune delle risposte.** Lo produce il filtro del framework prima che l'applicazione veda la richiesta, ed è destinato a chi non si sta rivolgendo a questo servizio.

**Il limite raggiunto non viene registrato.** Arriva con A7.

**Il Frontend non mostra ancora `auth.tooManyAttempts` né `auth.notSecure`.** Arrivano con A6; i secondi da mostrare sono in `Retry-After`.

---

## Documentation Release 1.7.0 — Tentativi, trasporto, Origin e Host — 2026-09-26

### Changed

**Specification 18 alla 1.3.0**

* **D9, approvata:** il canale idoneo vale per ogni password, comprese la creazione e la reimpostazione di un account. La specifica elencava solo `setup`, `login` e cambio password.
* **D10, approvata:** una password attuale sbagliata nel cambio password conta come fallimento.
* **D11, approvata:** un contatore si dimentica dopo 15 minuti senza fallimenti dalla fine del ritardo. La specifica li azzerava solo al riavvio: chi sbagliava cinque volte in mesi restava rallentato per sempre, e la memoria cresceva con ogni nome tentato.
* Che cosa è un fallimento; le credenziali non si verificano durante il ritardo; il contatore del nome vale per il nome tentato; conteggio IPv6 per /64.
* `Origin`: che cosa è una richiesta che modifica dati, il confronto con schema, nome e porta, l'`Origin` assente accettato.
* `AllowedHosts`: il valore predefinito, e `*` che impedisce l'avvio, in applicazione del secondo principio.
* Valori di `Strict-Transport-Security`, `Content-Security-Policy` e `Retry-After`.
* V9 esteso a ogni richiesta che porta una password.

**Roadmap 1.3.3, README e PROJECT_CONTEXT** allineati alla Documentation Release 1.7.0.

---

## Milestone A4 — Ruoli, gestione degli account, ciò che viene trattenuto — 2026-09-26

Quarta delle sette milestone della Specification 18. Da qui i due ruoli leggono cose diverse, un amministratore gestisce gli account dall'API, e una password scelta da qualcun altro va cambiata prima di fare qualunque altra cosa.

### Added

**Ruoli**

* Un `Viewer` legge `/health`, `/statistics`, `/npss`, `/domains` e `/domains/{domain}`, e riceve `403 Forbidden` su `/devices` e sugli endpoint degli account. Il ruolo si legge dall'account a ogni richiesta, quindi una retrocessione vale dalla richiesta successiva, sulla stessa sessione.
* **`activityAccess`** sostituisce `activityAvailable` su `/domains/{domain}`, con tre valori: `Available`, `Unavailable`, `Withheld`. Per un `Viewer` l'attività **non viene neppure letta** dal database: quello che si trattiene non passa per la memoria del processo che risponde.
* `Unavailable` precede `Withheld`. Dire «trattenuta» quando la sorgente non la offre significherebbe affermare che la offre.

**Gestione degli account**

* `GET /api/v1/accounts`, `POST /api/v1/accounts`, `PATCH /api/v1/accounts/{username}` e `DELETE /api/v1/accounts/{username}`, solo per `Administrator`. Un account nelle risposte è `username`, `role`, `enabled`, `passwordChangeRequired` e `createdAt`; l'hash non esce mai.
* Un account creato da un amministratore nasce con `passwordChangeRequired`. Una reimpostazione via `PATCH` lo rimette e fa cadere **tutte** le sessioni dell'account, anche quella di chi la esegue, se l'account è il suo. Anche la disattivazione fa cadere tutte le sessioni, e le sessioni di un account rimosso se ne vanno con lui.
* **Il vincolo dell'ultimo amministratore** (`409 LastAdministrator`) è verificato nella stessa transazione della modifica. Un amministratore disattivato non conta. Finché non esiste un amministratore attivo (è la situazione a cui serve il recupero), gli altri account restano modificabili: il vincolo vieta di peggiorare lo stato, non di toccarlo.
* In `PATCH` i campi si applicano tutti o nessuno. Un ruolo valido accompagnato da una password troppo corta non passa da solo, e la password che accompagna una retrocessione rifiutata non viene scritta.

**Cambio della propria password**

* `POST /api/v1/auth/password`, con `currentPassword` e `newPassword`. Se la password attuale è sbagliata risponde `403 CurrentPasswordRejected`, non `401`, perché la sessione è valida.
* La nuova password non può coincidere con l'attuale (`Unchanged`, decisione D8). Il confronto passa dalla normalizzazione NFKC: la stessa password scritta con una legatura resta la stessa.
* A cambio riuscito l'obbligo cade, la sessione da cui è stato fatto resta aperta e tutte le altre dell'account cadono. Questo chiude V5 anche per il cambio di password.

**Obbligo di cambio password**

* Finché `passwordChangeRequired` è vero, ogni endpoint tranne `auth/session`, `auth/password` e `auth/logout` risponde `403 PasswordChangeRequired`, anche dove il ruolo non basterebbe.
* L'obbligo è un **requisito di autorizzazione** presente in ogni policy, **compresa quella predefinita**. Un endpoint aggiunto senza dichiarazione non diventa quindi il modo di aggirarlo. Le tre eccezioni usano una policy a parte, `SignedIn`.

**Storage**

* `AccountRepository`: `List`, `Change` e `Delete`, con esito `Changed`, `NotFound` o `LastAdministrator`.

**Prove — 62 nuove, da 252 a 314 sul backend**

| Impegno | Cosa si verifica |
| --- | --- |
| V6 | Un `Viewer` legge i dati aggregati, riceve `Forbidden` su `/devices` e sugli account prima che qualcosa venga fatto, ottiene `Withheld` con un elenco vuoto e **nessuna traccia del dispositivo nel testo della risposta**; con una sorgente che non offre l'attività ottiene `Unavailable` |
| V7 | L'ultimo amministratore non si disattiva, non si retrocede e non si rimuove, né dall'API né dal repository; un amministratore disattivato non conta; con un secondo amministratore attivo si può |
| V7, concorrenza | Venti volte di seguito, due amministratori che si disattivano a vicenda nello stesso istante: ne resta sempre uno, e uno solo dei due riceve il rifiuto |
| V5 | Cambio della propria password: la sessione corrente resta, l'altra cade, la vecchia password non apre più. Reimpostazione, disattivazione e rimozione fanno cadere le sessioni; una riattivazione non le riporta |
| Obbligo di cambio | Si enumerano **tutti** gli endpoint dell'host: con la password da cambiare ciascuno risponde `PasswordChangeRequired`, tranne i tre previsti; la policy predefinita rifiuta un amministratore con la password da cambiare |
| Contratto | Nome, ruolo e password non conformi sono `422` con il codice giusto e non creano nulla; il ruolo `"0"` o `"viewer"` è rifiutato; un nome già usato è `409` in qualunque maiuscola; un account inesistente è `404`; un `PATCH` vuoto non cambia nulla |

### Fixed

**La creazione di un account poteva fallire se un'altra scrittura arrivava nello stesso istante.** È un difetto di A3. `INSERT … SELECT … RETURNING` in autocommit comincia come lettura; quando trova un'altra connessione che scrive, SQLite rifiuta subito il passaggio a scrittura invece di metterlo in attesa. Il risultato era un `500` al posto di un `409`, per esempio con due configurazioni iniziali contemporanee. Il difetto si è manifestato a intermittenza con la suite intera, quando le nuove classi di prove hanno aumentato il carico. Adesso la creazione prende il lock di scrittura **prima** di leggere, in una transazione `IMMEDIATE`, come fanno `Change` e `Delete`. Una prova nuova crea 160 account in parallelo: senza la correzione fallisce tre volte su tre.

### Verified

Tredici difetti introdotti di proposito, tutti intercettati, poi annullati: obbligo di cambio password tolto dalla policy predefinita; `Withheld` che precede `Unavailable`; attività letta anche per un `Viewer`; disattivazione che non fa cadere le sessioni; cambio password che fa cadere anche la sessione corrente; password attuale non verificata; `PasswordChangeRequired` restituito come `Forbidden`; account creato senza obbligo di cambio; reimpostazione senza obbligo di cambio; obbligo non riportato nell'identità della sessione; password uguale all'attuale accettata; rimozione dell'ultimo amministratore consentita; ruolo numerico accettato. Una modifica innocua, come controllo, non ha fatto fallire nulla.

La prima tornata ha rivelato una **trappola del metodo**. Un file ripristinato con la sua data di modifica originale è più vecchio dei binari compilati con il difetto, e la compilazione incrementale non lo ricompila: i difetti successivi giravano sopra quello precedente. Dopo ogni ripristino il file va toccato. Le verifiche inquinate sono state ripetute.

Compilazione senza avvisi. 314 prove sul backend e 35 sul frontend, tre esecuzioni complete consecutive senza fallimenti.

### Decided

**Il ruolo si scrive per nome, esattamente.** Il parser del framework accetterebbe anche un numero, e `"0"` vorrebbe dire in silenzio `Administrator`.

**La forma canonica del nome** (spazi tolti, minuscole) è una funzione sola, usata da configurazione iniziale, recupero e gestione degli account. Prima era ripetuta.

**Gli strumenti per enumerare gli endpoint** passano dalle prove di autenticazione all'host di prova, perché ora li usano due classi.

### Known Impact

**Il Frontend non ha ancora nulla di tutto questo.** Mancano la gestione degli account, il cambio password e il messaggio `domains.activityWithheld`: arrivano con A6. Oggi il Frontend non legge `/domains/{domain}`, quindi il cambio di campo non rompe nulla.

**Una password attuale sbagliata nel cambio password non conta come tentativo.** I contatori arrivano con A5, e la specifica li definisce soltanto per l'accesso. Che contino anche qui è da decidere in A5.

**Le operazioni sugli account non vengono registrate.** La specifica chiede chi ha fatto che cosa e su quale account. Arriva con A7, insieme al resto della registrazione.

**Un corpo con un tipo sbagliato** (per esempio `"enabled": "sì"`) riceve il `400` del framework e non la struttura comune delle risposte. Vale per tutti gli endpoint con un corpo, non solo per quelli nuovi.

---

## Documentation Release 1.6.2 — Contratto della gestione degli account — 2026-09-25

La Specification 18 elencava gli endpoint degli account e il cambio password, ma non ne diceva i corpi, le risposte, alcuni codici di errore né il comportamento nei casi di confine.

### Changed

**Specification 18 alla 1.2.0**

* Corpi e risposte di `auth/password` e degli endpoint `/accounts`.
* Codici `RoleRejected` (422), `CurrentPasswordRejected` (403) e `AccountNotFound` (404); motivo `Unchanged` per `PasswordRejected`.
* `CurrentPasswordRejected` è un `403` e non un `401`, perché il Frontend legge un `401` come sessione terminata.
* **D8, approvata:** la nuova password non può coincidere con l'attuale. Altrimenti l'obbligo di cambio si aggira e l'amministratore continua a conoscere la password.
* `PasswordChangeRequired` precede `Forbidden`.
* `PATCH` applica tutto o niente. Una reimpostazione fa cadere tutte le sessioni dell'account, anche quella di chi la esegue; una disattivazione le fa cadere tutte.
* Un amministratore agisce sul proprio account come su quello di un altro, nel limite del vincolo, che si verifica nella stessa transazione della modifica.

**Roadmap 1.3.2, README e PROJECT_CONTEXT** allineati alla Documentation Release 1.6.2.

---

## Milestone A3 — Configurazione iniziale e recupero — 2026-09-19

Terza delle sette milestone della Specification 18. Restituisce all'installazione la possibilità di avere un primo account, e a chi perde l'accesso quella di riaverlo. L'istanza torna utilizzabile da un client dell'API; per il Frontend serve A6.

### Added

**Configurazione iniziale**

* `SetupService`: quando l'installazione non ha alcun account, all'avvio genera un **codice di 12 caratteri** (`ABCD-EFGH-JKLM`) e lo scrive sull'output standard del processo. Il codice esiste in memoria come hash, si rifà a ogni avvio finché non viene usato, e non compare nel sistema di registrazione: è scritto con una scrittura diretta, perché quel sistema alimenta file e raccoglitori che non devono ricevere un segreto.
* `POST /api/v1/setup`: riceve codice, nome e password, crea il primo `Administrator` e lo fa entrare (`201`). Il codice si controlla per primo; un nome o una password non conformi vengono rifiutati **senza consumarlo**; si accetta in qualunque combinazione di maiuscole e con o senza trattini e spazi.
* Finché non esiste alcun account, ogni endpoint e lo stesso `login` rispondono `401 SetupRequired` invece di `AuthenticationRequired` o `AuthenticationFailed`: dire «autenticati» sarebbe un'istruzione che nessuno può seguire, e dire «credenziali errate» sarebbe falso.
* La creazione del primo account e il controllo che nessun altro esista sono **un'unica istruzione SQL**. In due passi, otto richieste contemporanee con il codice giusto avrebbero trovato l'installazione vuota e creato otto amministratori.

**Recupero**

* `RecoveryCommand`, dallo stesso eseguibile: `reset-password <nome>`. Chiede la nuova password due volte sul terminale, senza mostrarla; con l'input rediretto (script, container) legge le righe così come arrivano.
* Regole, dalla Specification 18: con un amministratore che può entrare reimposta la password di qualunque account esistente e **rifiuta un nome sconosciuto**, perché un errore di battitura non deve creare un account; senza alcun amministratore che possa entrare riattiva un amministratore disattivato o ne crea uno, e **rifiuta di promuovere** un `Viewer`.
* La password nuova è soggetta alle regole di sempre, vi si rimette `passwordChangeRequired` e cadono tutte le sessioni dell'account.
* Il comando non avvia il servizio: viene smistato dalla composition root subito dopo la migrazione.

**Storage**

* `AccountRepository`: `CreateFirst` (atomico), `HasAny`, `HasEnabledAdministrator`, `SetEnabled`.

**Api**

* `ApiError.Reason`: causa precisa leggibile da un programma, per esempio `TooShort`. **Assente e non `null`** quando non c'è: un campo sempre presente non dice nulla.

**Prove — 41 nuove, da 211 a 252 sul backend**

| Impegno (Specification 18) | Cosa si verifica |
| --- | --- |
| V10, la configurazione si fa una volta | Senza codice o con codice errato: `401 SetupCodeRejected`, con le stesse parole, senza cookie e senza creare nulla. Con il codice giusto: `201`, amministratore, già dentro. Una seconda richiesta: `409 SetupAlreadyCompleted` |
| Un errore di battitura non brucia il codice | Nome non conforme e password troppo corta (con `reason: TooShort`) sono `422`, e il codice serve ancora |
| Otto richieste insieme | Una sola `201`, sette `409`, un solo account |
| Il codice non è un segreto perduto | Formato in gruppi di quattro, senza caratteri ambigui; **non compare in nessuna riga di registro**, con un controllo positivo che l'host abbia registrato qualcosa; non è nel file del database |
| Vita del codice | Nessuna installazione con account lo mostra; un riavvio ne fa uno nuovo e il vecchio smette di valere; prima che ne sia mostrato uno nessun tentativo può riuscire |
| Recupero | Ogni regola della tabella sopra, e che nulla cambia quando la password è rifiutata, le due non coincidono o nessuno risponde; che il terminale non riceve mai la password, **compresa quella rifiutata** |
| **L'eseguibile vero** | Un processo lanciato dal prodotto compilato, con la sua base di dati, crea l'amministratore da terminale e **si ferma**; senza nome stampa l'uso ed esce con codice 2. È l'unica prova dello smistamento nella composition root |
| Storage | Primo account solo se nessuno esiste, sedici richieste parallele ne creano uno, `HasEnabledAdministrator` conta soltanto un amministratore attivo |

### Verified

Tredici difetti introdotti di proposito, tutti intercettati, poi annullati: qualunque codice accettato, un nome non conforme che consuma il codice, codice non normalizzato, creazione del primo account non condizionata all'assenza di altri, rifiuto che non dice mai `SetupRequired`, accesso su installazione vuota che dice «credenziali errate», `setup` che non fa entrare il nuovo amministratore, `reason` sempre presente, recupero che lascia aperte le sessioni, che crea un account per un nome sconosciuto, che promuove un `Viewer`, che stampa la password, eseguibile che avvia il servizio invece di uscire.

Due prove erano più deboli di come apparivano e sono state corrette:

* la prova «nessuna password sul terminale» usava una password diversa da quella rifiutata, che è quella che si è tentati di stampare per spiegare il rifiuto;
* la prova sull'atomicità a livello di host non è deterministica, perché la finestra della gara è stretta. L'atomicità è provata **a livello di repository**, con sedici richieste parallele, dove la mutazione viene vista.

La prova sull'eseguibile termina il processo figlio se il comando non si ferma. Senza, un difetto lascerebbe un servizio in ascolto ben dopo la fine della prova.

Compilazione senza avvisi.

### Decided

**Il codice ha 12 caratteri da un alfabeto di 32 simboli**: 60 bit, senza `I`, `O`, `0`, `1` che si scambiano fra loro.

**Un solo punto decide se l'installazione ha un account.** Una volta visto un account il risultato si ricorda e la base di dati non viene più interrogata: un'installazione con account non torna mai a non averne.

**`SetEnabled` esiste in anticipo di una milestone.** Serve al recupero. A4 lo riuserà per disattivare gli account.

### Known Impact

**Il Frontend non sa ancora creare il primo account.** Non esiste schermata di configurazione iniziale né di accesso fino ad A6. Per usare l'istanza oggi: avviare il servizio, leggere il codice sull'output e chiamare `POST /api/v1/setup`; oppure `dotnet run -- reset-password <nome>` nella cartella dell'Api, che con un'installazione vuota crea un amministratore. Con `dotnet run` gli argomenti del programma vanno dopo `--`.

**`passwordChangeRequired` è messo a vero dal recupero e non ancora applicato.** Un account la cui password è stata reimpostata non è ancora costretto a cambiarla: l'obbligo arriva con A4.

**Nessun limite ai tentativi sul codice** (A5). Sessanta bit e l'ascolto sul solo loopback sono ciò che oggi protegge.

**Il codice è leggibile da chi legge l'output del processo**, in un container da chi esegue `docker logs`. È il limite dichiarato nella specifica: la prova di possesso è l'accesso alla macchina.

**I messaggi sul terminale sono in inglese.** Sono per chi amministra l'host, non per l'interfaccia bilingue.

---

## Documentation Release 1.6.1 — Codici della configurazione iniziale — 2026-09-19

Scrivendo A3 sono emerse tre cose che la Specification 18 non diceva.

### Changed

**Specification 18 alla 1.1.0**

* Tre codici di errore: `SetupCodeRejected` (401, codice assente o errato, identico nei due casi), `SetupAlreadyCompleted` (409) e `UsernameRejected` (422). La specifica elencava il motivo dei rifiuti di password e non diceva che cosa succede a un nome non conforme né dopo che la configurazione è stata fatta.
* L'ordine dei controlli sulla configurazione iniziale e il fatto che un nome o una password non conformi **non consumano il codice**. Senza, chi sbaglia a digitare dovrebbe riavviare il servizio.
* Le regole del recupero, in tabella. «Riattivarne o crearne uno» lasciava indeciso che cosa succede con un `Viewer` o con un nome sbagliato: sarebbe stata una scelta di prodotto presa in silenzio dal codice.
* Il motivo di `PasswordRejected` viaggia in `reason` e non nel testo, perché l'interfaccia lo traduce.

**Specification 06 alla 1.3.1**: il campo `reason` degli errori.

**Roadmap 1.3.1, README e PROJECT_CONTEXT** allineati alla Documentation Release 1.6.1.

---

## Milestone A2 — Sessioni, accesso e rifiuto per impostazione predefinita — 2026-09-19

Seconda delle sette milestone della Specification 18. Da qui ogni endpoint richiede un account. È il momento in cui il codice smette di essere aperto, ed è anche il momento in cui il sistema in esecuzione diventa inutilizzabile finché A3 non permette di creare il primo account: vedi Known Impact.

### Added

**Storage**

* Migrazione `0011`: la tabella `session`. Del valore che il browser tiene si conserva soltanto l'hash, e una sessione non sopravvive al proprio account.
* `SessionRepository` e `StoredSession`. La sessione si legge insieme all'account, così ruolo e stato sono quelli **di adesso** e non del giorno dell'accesso.

**Api**

* `SessionService`: identificativo di 32 byte casuali, hash SHA-256 nel database, fine dopo otto ore senza richieste e in ogni caso dopo quattordici giorni. Un accesso nuovo dove una sessione è già aperta la sostituisce.
* `CredentialVerifier`: lo stesso calcolo in ogni caso di rifiuto. Per un nome inesistente la password è confrontata con un hash fatto apposta, per un account disattivato con il suo hash prima di rifiutarlo: la risposta non rivela il motivo, nemmeno dal tempo che impiega. Rifà l'hash quando i parametri sono più leggeri di quelli in vigore.
* Autenticazione e autorizzazione con i meccanismi standard di ASP.NET Core, senza dipendenze nuove: un gestore che riconosce il cookie, due criteri (`Viewer`, `Administrator`) e un criterio di riserva `Administrator` per ciò che non dichiara nulla.
* Rifiuti nella struttura standard: `401 AuthenticationRequired`, `403 Forbidden`, `401 AuthenticationFailed`. Mai una finestra di credenziali del browser.
* Endpoint: `POST /auth/login` (l'unico raggiungibile senza credenziali), `POST /auth/logout`, `GET /auth/session`.
* `Cache-Control: no-store` su ogni risposta di `/api`.

**Ruolo richiesto da ciascun endpoint**

| Endpoint | Ruolo |
| --- | --- |
| `/health`, `/statistics`, `/npss`, `/domains`, `/domains/{domain}` | `Viewer` |
| `/devices` | `Administrator` |

**Prove — 32 nuove, da 179 a 211 sul backend**

| Impegno (Specification 18) | Cosa si verifica |
| --- | --- |
| V1, rifiuto predefinito | Si enumerano **tutti** gli endpoint dell'host: senza sessione ciascuno risponde `401`. L'unico aperto è `login` |
| V2, dimenticare non apre | Ogni endpoint dichiara il proprio ruolo, e il criterio di riserva rifiuta un anonimo e un `Viewer` e ammette un `Administrator` |
| V3, il rifiuto non insegna nulla | Cinque motivi diversi (nome inesistente, password errata, account disattivato, nome vuoto, corpo vuoto) producono la stessa risposta, senza cookie |
| V5, una sessione finisce quando deve | Uscita, nuovo accesso, otto ore di inattività (con rinnovo dall'attività), quattordici giorni comunque attiva, account disattivato, fine delle sessioni di un account tranne quella in uso |
| V12, niente cache | Risposte con dati e rifiuti portano `no-store` |
| V15 dal vivo | L'accesso rifà l'hash fatto con parametri più leggeri, e il nuovo funziona |
| Segreto non in chiaro | L'identificativo non è nel file del database; l'hash SHA-256, **calcolato dalla prova e non chiedendolo al codice**, sì |
| Ruoli | Un `Viewer` legge statistiche e domini e non `/devices`, e la risposta non contiene l'indirizzo del dispositivo |
| Corpi illeggibili | JSON rotto, password con caratteri Unicode non validi e un corpo di forma sbagliata sono errori del client e non guasti |
| Storage | Sessione con il proprio account, unicità, rinnovo, rimozione, rimozione per account, scadute e vive, cascata alla rimozione dell'account |

### Decided

**Il cookie non ha scadenza propria.** Finisce con il browser. Le otto ore e i quattordici giorni sono tetti lato server, non una promessa di restare dentro: un «ricordami» implicito sarebbe una scelta che nessuno ha approvato, e la Frontend Specification dice che non esiste oltre la durata della sessione.

**`no-store` su ogni risposta di `/api`, errori compresi.** La specifica lo chiede per le risposte autenticate. Una regola che vale ovunque è più difficile da violare per dimenticanza.

**La prova sulla dichiarazione del ruolo è più severa della specifica.** La specifica dice che un endpoint senza dichiarazione richiede `Administrator`, e questo è garantito dal criterio di riserva. In più una prova **fallisce** se un endpoint non dichiara nulla: chi ne aggiunge uno deve scegliere il ruolo, non ereditarlo per caso.

**L'host di test usa un hash a mille iterazioni.** Le prove verificano che cosa fanno gli endpoint con una password, non quanto dura il calcolo, e duecento millisecondi per ogni accesso avrebbero fatto aspettare l'insieme per nulla. Le prove sull'hash, con i parametri veri, sono in `PasswordHasherTests`.

**Le sessioni scadute vengono eliminate quando se ne apre una nuova** e quando una viene presentata, non da un'attività a parte.

### Verified

Quattordici difetti introdotti di proposito, tutti intercettati, poi annullati: endpoint `/devices` aperto a un `Viewer`, risposte cacheabili, criterio di riserva che ammette chiunque sia entrato, limite di inattività ignorato, limite dei quattordici giorni ignorato, sessione di un account disattivato che continua a valere, cookie leggibile dalla pagina, cookie inviato da altri siti, account disattivato che può entrare, nuovo accesso che lascia aperta la sessione precedente, attività che non rinnova, fine delle sessioni che non risparmia quella in uso, un endpoint che dimentica di dichiarare il ruolo, identificativo conservato in forma reversibile.

L'ultimo ha mostrato un limite della prova: la prima versione chiedeva l'hash al codice stesso, e non avrebbe visto un hash reversibile. Ora lo calcola per conto proprio.

Compilazione senza avvisi.

### Known Impact

**Il sistema in esecuzione è chiuso a tutti.** Nessun account può essere creato finché non arriva A3, quindi ogni endpoint risponde `401`. `dotnet run` e `npm run dev` funzionano e non mostrano nulla. Le prove creano gli account direttamente. È la conseguenza voluta di rifiutare per impostazione predefinita, e la soluzione è A3.

**Il Frontend dice una cosa falsa.** Un `401` arriva alla schermata come «Nessun punteggio disponibile: il motore non ha ancora prodotto una valutazione». Non è vero: il motore ha il punteggio e lo nega a chi non è entrato. È la stessa classe di difetto corretta per Domini nella 1.5.1, e si chiude con gli stati dell'applicazione in A6.

**`SetupRequired` non viene ancora emesso.** Senza account, la risposta è `AuthenticationRequired`. Arriva con A3.

**`passwordChangeRequired` è esposto e non ancora applicato.** Nessun account può averlo vero finché A4 non crea account gestiti da un amministratore.

**Nessun limite ai tentativi.** Ogni accesso costa un PBKDF2 a 210.000 iterazioni, e chi raggiunge l'API può consumare CPU. Mitigato dal fatto che l'API ascolta soltanto sul loopback. Arriva con A5.

**Credenziali accettate su connessione in chiaro non locale, `Origin` e `Host` non verificati.** Anche questi in A5; per ora valgono `SameSite=Strict` e il solo ascolto sul loopback.

**`EndAllFor` esiste, con prove, e nessun endpoint lo chiama.** V5 è chiuso per uscita, nuovo accesso, scadenza e disattivazione. Per il cambio di password chiude in A4, dove esiste l'endpoint.

**Le soglie sono costanti.** La Specification 18 le dice modificabili senza toccare la struttura, e oggi modificarle richiede ricompilare.

---

## Milestone A1 — Account, hash e persistenza — 2026-09-19

Prima delle sette milestone della Specification 18. Costruisce ciò su cui l'autenticazione poggia e che non è ancora raggiungibile da nessun endpoint.

### Added

**Storage**

* `AccountRole` (`Administrator`, `Viewer`), `StoredAccount` e `AccountRepository`, con la migrazione `0010`: la tabella `account`.
* La migrazione impone in schema ciò che il codice promette: nome unico e ruolo limitato ai due noti. Un ruolo che il software non conosce non deve poter essere scritto, perché verrebbe riletto come qualcosa che nessuno ha deciso.
* Il nome è conservato in minuscolo: due persone che differiscono per una maiuscola sarebbero, per chi digita il nome, una persona sola.

**Api**

* `PasswordHasher`: PBKDF2 con HMAC-SHA-512, 210.000 iterazioni, sale casuale di 16 byte per password. Algoritmo, iterazioni, sale e hash stanno insieme in un unico valore, così i parametri possono crescere. Un accesso riuscito con parametri più leggeri dei correnti lo segnala (`ValidButOutdated`), perché la password serve a ricalcolare l'hash soltanto in quel momento.
* `AccountPolicy`: password da 12 a 128 **caratteri** (non byte), nessuna regola di composizione, diversa dal nome; nome da 3 a 32 caratteri fra minuscole, cifre, punto, trattino e trattino basso. La password è normalizzata in forma Unicode NFKC prima della misura della lunghezza e prima dell'hash: le due cose partono dalla stessa forma, e non possono discordare su che cosa sia la password.
* Registrazione dei servizi nella composition root.

**Prove — 51 nuove, da 128 a 179 sul backend**

| Impegno (Specification 18) | Cosa si verifica |
| --- | --- |
| V4, nessun segreto in chiaro | La password non compare **in nessun punto del file del database**, con un controllo positivo che la ricerca guardi il file giusto |
| V15, parametri aggiornati | Un hash fatto con meno iterazioni è riconosciuto e chiede di essere rifatto |
| Sale per password | La stessa password non produce mai due volte lo stesso valore |
| La stessa password su tastiere diverse | Forma composta e scomposta della stessa lettera sono la stessa password |
| Un record danneggiato non fa entrare nessuno | Nove forme di valore illeggibile falliscono, e un hash corretto con l'etichetta dell'algoritmo cambiata è rifiutato |
| Lunghezza in caratteri | 11 no, 12 sì, 128 sì, 129 no; nessuna regola di composizione |
| Nomi | Doppioni rifiutati qualunque sia la maiuscola, senza sovrascrivere l'esistente |
| Cambio password | Su un account inesistente è un fallimento e non una modifica che non modifica nulla |

### Verified

Mutazioni introdotte di proposito, poi annullate: nome non più portato in minuscolo, cambio password su account inesistente reso silenzioso, confronto sulle iterazioni disattivato, etichetta dell'algoritmo ignorata, sale costante, normalizzazione indebolita, lunghezza minima ridotta. Nove prove fallite in totale.

La prima versione della prova sull'etichetta dell'algoritmo non distingueva: usava un hash sbagliato, che falliva comunque. Rafforzata con un hash corretto e l'etichetta cambiata.

### Decided

**`Create` restituisce `null` quando il nome è già preso** invece di lanciare un'eccezione. Un nome occupato è una risposta attesa, e verrà tradotta in `AccountExists`. Ogni altro guasto resta un'eccezione.

**Tetto alle iterazioni accettate: dieci milioni.** Un record alterato può farlo solo chi ha già il database, ma un valore manomesso non deve trasformare un accesso in un calcolo che non finisce.

**`PasswordHasher` è interno e il costruttore con iterazioni serve alle prove**, che devono produrre un hash «di una volta» senza aspettarlo. Il codice di produzione usa `PasswordHasher.Standard`.

**Nell'Api e non in Storage o Core.** La Backend Specification assegna la persistenza a Storage e l'host a Api, e l'hash non è né una cosa né l'altra. Il Core analizza la rete e non ha ragione di conoscere le password.

### Known Impact

**Nulla è ancora raggiungibile.** Non esiste alcun endpoint di accesso: l'API risponde a chiunque, come prima. Il debito dell'autenticazione resta aperto fino ad A7.

**Una password con caratteri Unicode non validi** (un surrogato spaiato) fa lanciare un'eccezione a `Normalize`. Oggi non può arrivare: il deserializzatore JSON la rifiuta prima. A2 non deve contarci in silenzio, e dovrà trattare il caso al confine.

**La tabella delle sessioni non esiste** e arriverà con A2.

**Costo di calcolo.** Ogni accesso richiede un PBKDF2 a 210.000 iterazioni. È voluto, ed è ciò che rallenta chi prova password; ma il rallentamento dei tentativi ripetuti (A5) non è ancora presente, e fino ad allora un flusso di richieste di accesso costa CPU.

---

## Documentation Release 1.6.0 — Authentication Specification — 2026-09-19

La prima Specification scritta per chiudere una decisione che era rimasta aperta per scelta. La API Specification dichiarava che «il sistema supporta autenticazione centralizzata» e che «le modalità implementative vengono definite durante lo sviluppo del backend». Non lo sono mai state.

### Added

**Specification 18 — Authentication, alla 1.0.0, Approved**

Comprende: modello delle minacce, sei principi, account con due ruoli, credenziali, sessioni, configurazione iniziale, recupero, trasporto, tentativi ripetuti, contratto API, autorizzazione, Frontend, persistenza, registrazione, quindici impegni verificabili sul backend e cinque sul Frontend, sette milestone di realizzazione (A1–A7).

### Decided

Sette decisioni di prodotto, approvate il 2026-09-19. Ciascuna registra l'alternativa non scelta e ciò che costava.

| # | Scelta | Ragione |
| --- | --- | --- |
| D1 | Account locali con password e sessione | Un segreto condiviso non distingue le persone e non si revoca a una sola |
| D2 | Due ruoli; il `Viewer` non vede i dati per dispositivo | L'attività per dispositivo è il dato più sensibile: in una casa chi guarda il quadro d'insieme non deve poter vedere che cosa ha fatto ciascuno |
| D3 | PBKDF2-SHA-512, senza dipendenze | Argon2id resiste meglio, ma richiede una libreria di terzi |
| D4 | Recupero solo dalla macchina | Un canale via posta o rete è un secondo modo di entrare, e un servizio esterno |
| D5 | Nessun token per servizi, per ora | Oggi nessun servizio interroga l'API: sarebbe progettare per un bisogno che non c'è |
| D6 | Soglie della specifica | Sono valori, modificabili senza cambiare la struttura |
| D7 | Criterio di Beta della Roadmap riformulato | Vedi sotto |

Due scelte non ovvie che discendono dai principi:

* **Ciò che il ruolo non comprende è dichiarato «trattenuto»**, non omesso. Un elenco vuoto per mancanza di diritto e uno vuoto perché nessun dispositivo ha contattato il dominio sono affermazioni diverse. È la regola Absent Versus Unmeasurable applicata ai permessi.
* **Il codice di configurazione iniziale** impedisce che chi arriva per primo all'indirizzo diventi amministratore. La prova di possesso è l'accesso alla macchina, e il codice non passa dal sistema di registrazione.

### Changed

| Documento | Versione | Modifica |
| --- | --- | --- |
| 06 - API | 1.3.0 | Authentication e Authorization rimandano alla 18; eccezione di `setup` e `login`; `activityAccess` |
| 09 - Network Privacy | 1.7.0 | L'accesso richiede un account; ciò che il ruolo non comprende è trattenuto |
| 10 - Frontend | 1.2.0 | Sezione Authentication: cinque stati, schermate, regole; sicurezza |
| 11 - Backend | 1.4.0 | Authentication e Authorization rimandano alla 18. I «servizi autorizzati» non sono previsti finché non esiste un servizio che li usi |
| 12 - Installation | 1.2.0 | Primo amministratore e recupero; nessun account né password predefiniti |
| 13 - Roadmap | 1.3.0 | Criterio di Beta riformulato; situazione della Phase 2 |
| 16 - Persistence | 1.2.0 | Account e sessioni; nulla di segreto in chiaro |
| 00 - Glossary | 1.6.0 | Account, Role, Session, Setup Code, Withheld |

**Incoerenza corretta nella Roadmap.** Il criterio di Beta diceva «nessun endpoint risponde senza credenziali valide». Preso alla lettera è incompatibile con l'esistenza di un modulo di accesso, che risponde per definizione a chi non ha ancora credenziali. Ora: nessun endpoint che restituisca dati sulla rete o sul sistema risponde senza credenziali; fanno eccezione `setup` e `login`, che non restituiscono nulla sulla rete.

### Known Impact

**Manca la specifica Transport Security.** I criteri di Beta richiedono HTTPS, e nessun documento dice come si ottiene: certificato, rinnovo, proxy davanti a PIE. La Specification 18 fissa soltanto ciò che dipende dal trasporto: le credenziali non viaggiano in chiaro fuori dal loopback. Finché non esiste, l'accesso da altri dispositivi resta rifiutato.

**La documentazione descrive `activityAccess` e il codice ha ancora `activityAvailable`.** La divergenza è temporanea e dichiarata: si chiude nella milestone A4.

**Il README non elenca i debiti** che il criterio di pubblicazione del repository richiede di elencare, l'assenza di autenticazione compresa. Invariato da prima di questa release.

---

## Documentation Release 1.5.1 — Stato del progetto e identità dell'attività — 2026-09-19

Nessun cambiamento di comportamento del prodotto: la documentazione smette di dire cose non più vere e dichiara una regola che il codice non rispettava.

### Changed

**Roadmap Specification alla 1.2.0**

* **Stato.** Dichiarava «Documentation Completed» e «Development Status: Not Started», con ogni milestone da M2 in poi «Planned», mentre il CHANGELOG registra lavoro fino alla sezione Domini. Ora: `In Development`, milestone M2, M3, M4, M5 e M7 `In Progress`, e sotto ogni fase una riga **Situazione** che dice cosa esiste e cosa manca.
* **Adapter Manager.** Esiste l'interfaccia `IAdapterManager` e nessuna implementazione. La Phase 3 non può dirsi completata finché non c'è.
* **Numerazione.** Il CHANGELOG usa `M5.x` per la persistenza e `M6.x` per il Frontend, che nella Roadmap sono rispettivamente parte di M2 e M5. Scelta la tabella di corrispondenza anziché la rinumerazione: la cronologia non si riscrive, e le milestone di rilascio M8 e M9 citate nel README non cambiano.
* **Nome di M4.** «Core Modules», come nel Glossary, nella Specification 03 e nel README. Solo la Roadmap diceva «Intelligence Modules».
* **Intestazione.** Riportava 1.1.0 e 2026-08-02 pur contenendo i criteri di rilascio aggiunti il 2026-09-18, che non registravano la versione della Specification che li introduceva. La 1.2.0 comprende entrambe le cose.

**Specification 04 alla 1.6.0 — l'identità vale anche per l'attività**

`DomainActivity.deviceId` è derivato con la stessa regola di `Device.deviceId`. La regola non era scritta, e il codice la violava: vedi la voce «Prove ai confini».

Un indirizzo che ha avuto un'assegnazione durante il periodo ma non ne ha più una al momento dell'acquisizione conserva la base più debole. È dichiarato come limite.

**README e PROJECT_CONTEXT** allineati: stato del progetto, tabella delle milestone, Documentation Release 1.5.1.

---

## Prove ai confini — 2026-09-19

Prima applicazione della regola introdotta con MASTER_PROMPT 2.0.0: ogni confine attraversato porta almeno una prova. Le sessantaquattro esistenti erano quarantasei sul Core e diciotto su un solo componente della persistenza, il lettore delle liste. Adapter, repository dei periodi e dei punteggi, API e Frontend non ne avevano nessuna.

Ora sono 163: 128 sul backend e 35 sul Frontend.

### Added

**Adapter Technitium — 28 prove, progetto `TivuStream.Pie.Adapters.Technitium.Tests`**

Eseguite contro risposte preparate del server, modellate sulla Technitium API Reconnaissance, senza istanza reale.

| Impegno (Specification 04) | Cosa si verifica |
| --- | --- |
| Aggregazione dei log | Cinque interrogazioni su due pagine escono come tre fatti; prima e ultima osservazione coprono le pagine in qualunque ordine arrivino; bloccato e risolto restano separati |
| Il registro non oltrepassa l'Adapter | Assenza del componente di log: rifiuto esplicito, non elenco vuoto |
| Token come credenziale `Bearer` | Presente su ogni richiesta, assente da ogni messaggio d'errore |
| Esito nel corpo, non nel codice HTTP | Un errore con HTTP 200 resta un errore; stack trace ed errore interno del server non compaiono |
| Valori qualificati | `UniqueDomains` dichiarato limite inferiore; NXDOMAIN escluso dai fallimenti; solo Tls, Https, Quic contano come cifrati |
| L'Adapter non classifica | Categoria `Unknown`, nessuna reputazione, istanti dichiarati `PeriodBounded` |
| Identità del dispositivo | Base hardware con lease, indirizzo senza; stessa identità dopo un cambio di indirizzo; assenza del DHCP non è un errore; **l'attività porta l'identificativo del proprio dispositivo** |
| Capability | Dichiarate solo se l'account può leggere la sezione e il componente è installato |

**Persistenza — 19 prove, `TivuStream.Pie.Storage.Tests`**

Eseguite su un file SQLite vero, migrato allo schema corrente e distrutto a fine prova.

* Una misura qualificata resta qualificata dopo il giro attraverso il database.
* Rileggere un periodo lo sostituisce senza residui; un'osservazione è registrata per intero o per nulla.
* Le regole di aggregazione su finestra della API Specification 1.2.0: somma delle occorrenze, prima e ultima osservazione, qualità pari alla meno precisa, classificazione del periodo più recente **indipendentemente dall'ordine di scrittura**.
* I valori dei fattori tornano come numeri; un punteggio negato torna negato e non zero; un'area non misurabile conserva il proprio stato; i decimali non dipendono dalla lingua della macchina.
* I fattori scritti come testo da una versione precedente tornano vuoti e non interpretati.

**API — 17 prove, `TivuStream.Pie.Api.Tests`**

L'host vero gira in memoria con un database proprio. Sono tolti di proposito i due servizi in background, che raggiungerebbero la sorgente dati e scaricherebbero liste da Internet, e il file `appsettings.Local.json` dello sviluppatore, che contiene un token reale.

Verificato con una controprova che l'esecuzione non tocchi nulla sotto `Api/data`.

* Prima di qualunque acquisizione statistiche e punteggio sono un **rifiuto** (503, `AcquisitionPending`, `ScorePending`), non un insieme di zeri.
* `/domains` dichiara periodo, ore osservate e ore richieste; senza osservazioni il periodo è assente e l'elenco vuoto non si presenta come una rete silenziosa; i periodi più vecchi della finestra non contano.
* Un punteggio negato viaggia come `null`; un fattore viaggia come codice e numeri; le enumerazioni viaggiano come nomi.
* L'attività di un dominio è dichiarata non disponibile, non mostrata vuota, quando la sorgente non la offre.
* Nessuna risposta contiene la credenziale della sorgente dati.

**Frontend — 35 prove, Vitest, in `__tests__`**

* Dashboard: caricamento, rifiuto di giudicare e assenza di punteggio sono tre stati distinti; con il punteggio negato non compare alcun numero; un codice sconosciuto al catalogo compare con il proprio identificativo; la quota arriva come frazione e si scrive `7.1%` in inglese e `7,1%` in italiano; le note su «limite inferiore» e «non misurabile» compaiono solo quando qualcosa in pagina vi si appoggia.
* Domini: ore osservate dichiarate contro ore richieste, al singolare e al plurale; elenco vuoto sempre accompagnato dal proprio periodo; «mai osservato» distinto da «periodo vuoto»; un dominio non riconosciuto resta non classificato e non riceve né fonte né confidenza; una lettura fallita non si presenta come elenco vuoto.
* Catalogo: le due lingue hanno le stesse voci e gli stessi segnaposto, nessuna voce è vuota, e sei frasi che portano una qualificazione (limite inferiore, non confermato, non significa che non vi sia tracciamento, non significa che sia sicuro) la conservano in entrambe le lingue. Una voce mancante non produce un errore: l'interfaccia ricade sull'inglese, e chi legge in italiano incontra una frase in un'altra lingua senza che nulla lo dica.

**Dipendenza approvata: `Microsoft.AspNetCore.Mvc.Testing` 10.0.8**

| | |
| --- | --- |
| Scopo | Eseguire l'host reale in memoria per provare gli endpoint attraverso HTTP |
| Licenza | MIT |
| Riferita da | Solo `TivuStream.Pie.Api.Tests`, mai dal prodotto |
| Versione | 10.0.8, quella del runtime ASP.NET installato: una più recente avrebbe portato nell'output di test assembly del framework diversi da quelli su cui il prodotto gira |
| Discendenti | `Microsoft.AspNetCore.TestHost` e la famiglia `Microsoft.Extensions.*`, alla stessa versione, MIT |
| Vulnerabilità | Nessuna: il controllo di NuGet è attivo e la build tratta gli avvisi come errori |

Per raggiungere `Program` dal progetto di test, `TivuStream.Pie.Api.csproj` dichiara `InternalsVisibleTo`. Gli endpoint restano funzioni anonime in `Program.cs`, senza modifiche.

### Fixed

**L'attività non portava l'identificativo del proprio dispositivo.**

`GetDevicesAsync` derivava l'identificativo dall'indirizzo hardware quando esisteva un lease DHCP. `GetDomainActivitiesAsync` lo derivava sempre dall'indirizzo di rete. Per un dispositivo con lease, `Device.DeviceId` e `DomainActivity.DeviceId` erano due valori diversi per lo stesso dispositivo, e nessuna correlazione fra i due era possibile.

Non compariva perché nessun componente li univa: il Device Engine non esiste e lo schema non ha un vincolo fra le due tabelle. Sarebbe comparso con il primo indicatore che attribuisse un comportamento a un dispositivo.

Trovato scrivendo la prova, che è stata scritta prima della correzione e ha fallito. La scelta della base sta ora in un solo punto, `DeviceIdentity.Resolve`, usato da entrambe le letture. L'acquisizione dei log legge dunque anche i lease, una chiamata in più per acquisizione. Regola registrata nella Specification 04 alla 1.6.0.

**La vista Domini diceva «Nessun punteggio disponibile» quando la lettura falliva.**

Era la frase del Dashboard, riutilizzata. Su una pagina di domini parlava di un punteggio che non c'entra. Ora: «Impossibile leggere i domini. Il motore non ha risposto. Questo non dice nulla su ciò che la rete ha contattato.» L'ultima frase è il punto: un fallimento non deve poter passare per una rete silenziosa. Voci nuove del catalogo, in entrambe le lingue.

### Verified

Ogni gruppo è stato messo alla prova introducendo di proposito il difetto che dovrebbe intercettare, poi annullato:

* Adapter: bloccato fuso con risposto, NXDOMAIN contato come fallimento. Quattro prove fallite.
* Persistenza: qualità aggregata calcolata sulla più precisa invece che sulla meno precisa, classificazione presa dal periodo più vecchio, decimali scritti secondo la lingua corrente. Quattro prove fallite.
* API: ore osservate uguali a quelle richieste, attività letta anche quando la sorgente non la offre, enumerazioni non più scritte come nomi. Otto prove fallite.
* Frontend: fattore sconosciuto omesso, nota «non misurabile» mostrata sempre, elenco vuoto senza periodo. Tre prove fallite.

Compilazione senza avvisi, `vue-tsc` pulito, build del Frontend riuscita.

### Known Impact

**Le attività già registrate mantengono gli identificativi vecchi.** Quelle dei dispositivi con lease sono nel database con l'identificativo fondato sull'indirizzo di rete. Non vengono migrate: il loro solo uso oggi è la lettura dell'ultimo periodo, che si rinnova a ogni acquisizione, e il consolidamento a livelli non esiste ancora. Chi userà lo storico per attribuire comportamenti dovrà tenerne conto.

**`/health` dichiara `Core: NotImplemented`.** Il Core esiste da M4.1: il campo è rimasto com'era quando non c'era. È un'affermazione falsa in una risposta che descrive lo stato del sistema. Non corretto: il valore va scelto (per esempio `Online`, o derivato da uno stato reale del Core) ed è testo dichiarato a chi legge. Le prove dell'API non lo fissano di proposito.

**`/devices` e `/statistics` non dichiarano il periodo** (già annotato in M6.2). Le prove dell'API non lo verificano perché la regola non è ancora implementata per quei due endpoint.

**Nessuna prova sull'autenticazione, che non esiste.** È il blocco della Beta, e resta il prossimo passo che richiede una Specification prima del codice.

**Le prove sull'Adapter non toccano un'istanza reale.** Verificano la lettura di ciò che la Reconnaissance ha registrato, non che la versione installata risponda ancora così. È il criterio «seconda versione della sorgente» della Stabile.

**Un indirizzo con lease durante il periodo e senza al momento dell'acquisizione** produce attività sulla base più debole, mentre il suo dispositivo, se ha cambiato indirizzo, compare con un altro. Limite dichiarato nella Specification 04.

---

## Revisione del metodo — MASTER_PROMPT 2.0.0 — 2026-09-18

Le regole di lavoro vengono riviste dopo sei settimane di applicazione, sulla base di ciò che ha prodotto risultati e di ciò che è costato senza produrne.

Le regole su privacy, sicurezza e onestà della presentazione **non cambiano**, e vengono anzi rese esplicite in una sezione dedicata invece di restare implicite nei principi.

### Rationale

**Cosa ha pagato, e resta invariato.**

Documentation First ha intercettato la contraddizione fra Acquisition e Query Flow, il problema delle finestre sovrapposte, gli indicatori privi di definizione e l'autenticazione mai decisa. La scelta dei periodi fissi, nata da quella disciplina, è ciò che ha reso possibile l'aggregazione sulle ventiquattro ore: una rinuncia di agosto ha pagato a settembre.

Riportare le incoerenze invece di aggirarle ha generato l'intera linea che distingue lo strumento: misure qualificate, copertura, punteggio negato.

Il vincolo sulle dipendenze ha intercettato un template rotto e tenuto l'elenco minimo e pulito sul piano delle licenze.

**Cosa è costato senza produrre valore.**

L'obbligo di leggere tutte le Specification prima di ogni compito veniva ignorato oppure sprecava tempo: diciotto documenti non si rileggono a ogni sessione.

Documentation First applicato con lo stesso peso a ogni cosa produceva cerimonia sui dettagli interni. Il valore della regola è concentrato su ciò che è costoso correggere dopo.

L'attesa di conferma «quando opportuno» è diventata attesa quasi a ogni sotto-passo. Con un interlocutore che lavora a finestre interrotte, ogni attesa costa una sessione.

Il divieto di inventare requisiti, applicato anche alle scelte convenzionali, trasformava in domande decisioni che avevano una risposta ovvia.

**Cosa mancava.**

Nessuna nozione di «abbastanza finito»: il risultato è due schermate rifinite e sette inesistenti. Nessuna regola su dove collocare i test: sessantaquattro sul Core, zero su Adapter, API e Frontend. Nessun criterio di rilascio verificabile.

### Changed

| Regola precedente | Regola attuale |
| --- | --- |
| Leggere tutte le Specification | Indice, CHANGELOG recente, e le Specification toccate |
| Documentation First su tutto | Distinzione fra **Decisioni** e **Implementazione** |
| Attendere conferma quando opportuno | Approvazione su un elenco dichiarato; sul resto si procede e si riferisce |
| Non inventare requisiti | Vale per le decisioni di prodotto; le scelte convenzionali si prendono e si dichiarano |

### Added

**Sezione Non-Negotiable.** Privacy, sicurezza e onestà della presentazione erano implicite nei principi. Ora sono scritte come vincoli espliciti, insieme alla motivazione, perché nessuna considerazione di velocità possa eroderle per gradi.

**Modo «debito dichiarato».** Consegnare qualcosa di incompleto è ammesso se la lacuna è registrata sotto `Known Impact`. Serve a costruire in larghezza oltre che in profondità.

> Il debito che non viene scritto non è debito. È un difetto.

**Prove ai confini.** Ogni confine attraversato — Adapter, API, persistenza — richiede almeno una prova. Erano finite tutte nel Core perché era il posto più facile e più interessante dove scriverle.

**Criteri di rilascio verificabili — Specification 13.**

Tre soglie distinte, ciascuna con criteri che si possono dichiarare soddisfatti senza interpretazione: pubblicazione del repository, Beta, Stabile.

La prima separa esplicitamente il **rendere pubblico il codice** dal **rilasciare il prodotto**. Sono due cose che venivano trattate come una sola, e la confusione fra le due rendeva la pubblicazione molto più lontana di quanto sia.

**`CLAUDE.md`.**

Forma breve delle regole, dei comandi e dei debiti noti, nella radice del repository.

Serve a un assistente che lavori direttamente nella cartella del progetto, dove il ciclo compila-correggi-riprova si chiude senza passare dall'utente. Il ciclo attuale consuma minuti di attesa per ogni errore del compilatore.

---

## Milestone M6.2 — Domini su una giornata mobile — 2026-08-31

La sezione Domini smette di mostrare l'ora corrente e mostra le ultime ventiquattro ore, dichiarando quante ne esistono davvero.

### Changed

**Specification 06 alla 1.2.0**

Le risposte che descrivono ciò che è stato osservato dichiarano l'intervallo a cui si riferiscono.

Un elenco vuoto senza il proprio periodo è ambiguo: chi legge non distingue «la rete non ha contattato nulla» da «l'ora in corso è appena cominciata». La prima è un'affermazione sulla rete, la seconda sul momento in cui si guarda.

**L'ora corrente non era tempo reale**

Un periodo orario fisso si azzera allo scoccare dell'ora: alle 21:03 la pagina era quasi vuota, alle 21:58 piena, a parità di rete. Sessanta volte al giorno.

La finestra mobile risponde alla domanda che l'utente si pone davvero, e non si svuota mai di colpo.

**Aggregazione**

I periodi sono fissi e non si sovrappongono, quindi le occorrenze si sommano senza contare due volte lo stesso traffico.

È la scelta compiuta nella Persistence Specification a rendere lecita questa somma: con finestre mobili di acquisizione sarebbe stata impossibile. Una rinuncia di allora paga adesso.

| Proprietà | Regola |
| --- | --- |
| `occurrences` | Somma dei periodi inclusi |
| `firstSeen` / `lastSeen` | Il più antico e il più recente |
| `observationQuality` | La meno precisa fra quelle aggregate |
| Classificazione | Quella del periodo più recente in cui il dominio compare |

L'ultima riga è una scelta dichiarata. Ogni periodo conserva ciò che era possibile affermare allora; la più recente è ciò che si sa adesso, e l'età della lista dice quanto recente sia. Una media delle classificazioni ricevute produrrebbe un'affermazione che nessun periodo ha mai fatto.

**Ore osservate contro ore richieste**

`periodsObserved` e `periodsRequested` sono entrambi dichiarati.

Un'installazione accesa da cinque ore che scrivesse «ultime 24 ore» direbbe il falso.

### Fixed

**«Osservato fra le 10:00 e le 21:00» suggeriva una presenza continua**

Con la finestra di un'ora la formulazione era quasi vera, perché i due istanti distavano sessanta minuti. Allargata la finestra ha cominciato a promettere qualcosa che non sappiamo: il dominio può essere stato contattato in due di quelle ore e in nessuna delle altre.

Sostituita con due fatti distinti: prima osservazione e ultima.

Il difetto non è nuovo. È l'aggregazione ad averlo reso visibile, e nessun test lo avrebbe preso.

### Added

**Note in lingua corrente per i nostri termini**

`TermNote` presenta un termine tecnico con la sua spiegazione breve, dove il termine compare.

La spiegazione **si aggiunge** alla formulazione precisa, non la sostituisce: una frase semplificata che perdesse la qualificazione romperebbe la promessa che quella frase esiste per mantenere.

Cinque termini scritti: copertura, limite inferiore, non misurabile, non classificato, osservazione per periodo.

Le note compaiono soltanto quando qualcosa in pagina vi si appoggia davvero. La stessa spiegazione ripetuta su ogni riga diventa arredamento che nessuno legge, come si era visto con la nota sui domini non classificati stampata undici volte.

### Known Impact

`/devices` e `/statistics` presentano la stessa ambiguità corretta su `/domains`: restituiscono ciò che è stato osservato senza dichiarare quando. Erano già così, e nessuno ci era ancora inciampato.

---

## Milestone M6.1 — Prima schermata — 2026-08-31

Il Frontend esiste, ed è bilingue dalla prima riga.

### Added

**Progetto Vue**

Generato con `create-vue`, lo strumento ufficiale, e ripulito dei componenti dimostrativi.

Il pacchetto di linting proposto dal template è stato escluso: `oxlint` e `eslint-plugin-oxlint` arrivavano con un conflitto di dipendenze fra pari, quindi il progetto generato non si sarebbe installato.

Escluso anche `vite-plugin-vue-devtools`, un pannello di ispezione utile a chi scrive i componenti e inutile al prodotto.

Dipendenze approvate e verificate: `vue`, `vue-router`, `pinia`, `vue-i18n`, `vite`, `typescript`, `vitest`, `@vue/test-utils`, più le nove che discendono tecnicamente da quella scelta. Tutte MIT tranne TypeScript, Apache-2.0, tutte compatibili con GPL-3.0. Nessuna vulnerabilità.

**Dashboard**

Punteggio, copertura, versione dell'algoritmo, le sei aree con stato, punti ottenuti, punti ottenibili e peso nominale.

Il rifiuto di giudicare è uno **stato distinto** nel componente, separato dall'errore e dal caricamento. Quando il punteggio non esiste l'interfaccia non mostra zero e non mostra un guasto: dichiara che la copertura è sotto il minimo e riporta comunque le aree misurate.

**Catalogo dei fattori**

Trentaquattro codici in italiano e inglese, resi sotto l'area che li ha prodotti.

Il catalogo è parte del prodotto e non decorazione: `ClassificationUnavailable` non dice «nessun dominio classificato» ma dichiara che senza liste ogni dominio risulta non classificato, **il che non significa che non vi sia tracciamento**. Il limite inferiore sopravvive alla traduzione in entrambe le lingue.

Un codice che il catalogo non conosce viene mostrato con il proprio identificativo, mai omesso.

**Proxy verso il Backend**

`vite.config.ts` inoltra `/api` alla porta 5000. L'interfaccia parla al motore sulla stessa origine, quindi non è stato necessario introdurre regole CORS nel Backend: un debito evitato anziché aggiunto.

### Fixed

**Numeri formattati secondo la lingua**

La prima versione mostrava `1.88` anche in italiano, usando l'interpolazione grezza di Vue.

È esattamente il difetto che l'intera revisione dei fattori serviva a evitare, ricomparso un piano più su. Corretto con `$n()`, e le quote passano da `7,1%` a `7.1%` cambiando lingua, senza che il Backend sappia nulla della lingua.

### Recorded

**Glossario pubblico**

Registrata la decisione di una pagina pubblica che spieghi i concetti dell'analisi, con tre vincoli: non è il Glossary per sviluppatori, non riscrive a mano spiegazioni che il catalogo contiene già, e le regole di onestà vi si applicano integralmente.

---

## Documentation Release 1.5.0 e Milestone M4.6 — Fattori localizzabili — 2026-08-31

Il Core smette di produrre frasi e produce fatti. È la condizione perché il Frontend possa nascere bilingue senza testi da riscrivere.

### Changed

**Il Core non trasmette più testo**

Prima, dentro il motore:

```csharp
factors.Add("Nessuna lista di filtro configurata: il filtraggio non ha effetto.");
```

Adesso:

```csharp
factors.Add(ScoreFactor.Of(FactorCodes.FilterListsAbsent));
```

Una frase già scritta appartiene a una lingua sola. Presentare lo stesso risultato in inglese avrebbe richiesto di modificare il Core, che è esattamente ciò che l'architettura vieta.

**I numeri viaggiano come numeri**

`{ "share": 0.071 }`, non `"7,1%"`.

La convenzione con cui si scrive una percentuale appartiene alla lingua: in inglese quel valore si scrive `7.1%`. Formattare nel Core avrebbe reso l'inglese impossibile senza toccarlo.

**Specification 05 alla 2.2.0, 07 alla 3.1.0, 09 alla 1.6.0, 10 alla 1.1.0**

L'entità `ScoreFactor` è codice più valori, mai testo. Il catalogo completo dei codici è nella NPSS Specification, area per area.

Un codice pubblicato **non cambia significato**: alterarne il senso cambierebbe le frasi mostrate dalle interfacce già scritte, senza che nulla lo segnali. Un fattore che cambia significato riceve un codice nuovo.

**Migrazione 0009**

I punteggi con i fattori in forma di testo vengono rimossi.

Il lettore li degraderebbe a elenco vuoto senza errori, ma un punteggio le cui ragioni sono andate perse è un numero che il sistema non sa più giustificare. Conservarlo lascerebbe nello storico proprio la cifra inspiegata contro cui il progetto esiste.

### Rationale

Il vincolo emerge da una conseguenza non ovvia della richiesta di interfaccia bilingue.

I fattori sono la parte più importante del prodotto: sono il motivo per cui un punteggio non è un numero opaco. Erano anche l'unica parte scritta in una lingua sola, dentro il codice.

Spostandoli fuori dal Core, gli impegni di onestà si spostano con loro: le regole della Network Privacy Specification valgono ora sul **catalogo delle traduzioni**, che è parte del prodotto. Una traduzione che scrivesse «rete pulita» invece di «nessun tracciamento noto» violerebbe la specifica quanto lo farebbe il motore.

La Frontend Specification aggiunge una regola conseguente: un codice sconosciuto al catalogo viene mostrato con il proprio identificativo, mai omesso. Un fattore che sparisce toglierebbe all'utente una ragione del punteggio senza dichiararlo.

### Changed

**Due prove riscritte**

`The_reason_for_not_judging_is_stated_in_full` verifica ora i valori invece delle parole: codice `ObservationInsufficient`, `queries` 99, `minimum` 100. Il testo lo scriverà l'interfaccia; i numeri devono esserci perché possa scriverlo.

`Full_marks_on_exposure_are_reported_as_nothing_known` verifica il codice `TrackingExposureNone`. Il motore non può più sorvegliare le parole, e garantisce che la distinzione arrivi intatta fino all'interfaccia.

Sessantaquattro prove, tutte riuscite.

### Verified

Forma restituita dall'API, dopo il giro completo attraverso il database.

```json
{ "code": "ObservationContinuity", "values": { "observed": 6, "expected": 24 } }
```

I valori tornano dalla persistenza come numeri, non come testo.

### Known Impact

**Terza occorrenza di CA1859.**

L'analizzatore ha di nuovo segnalato un metodo privato che dichiara un'interfaccia invece del tipo concreto. Era stato annotato come da applicare in anticipo dopo la seconda volta, e non è stato fatto.

---

## Milestone M4.5 — Il primo punteggio complessivo — 2026-08-31

Per la prima volta il sistema si considera autorizzato a esprimere un giudizio d'insieme.

### Added

**Quattro indicatori nel motore — M4.5a**

Privacy Protection e Threat Protection sono calcolate secondo la Specification 07 alla 3.0.0.

`NpssEvaluationInput` riceve i domini classificati e le attività, e due dichiarazioni esplicite.

| Proprietà | Perché non è dedotta da un risultato vuoto |
| --- | --- |
| `ClassificationAvailable` | Senza liste ogni dominio è `Unknown`. Leggerlo come assenza di tracciamento trasformerebbe la mancanza di uno strumento in un buon risultato. |
| `DomainActivityAvailable` | Una sorgente che non sa dire cosa ha bloccato è una lacuna nell'osservazione, non una rete senza blocchi. |

**Migrazione 0008 — M4.5b**

I punteggi calcolati con l'algoritmo 2.0.0 vengono rimossi anziché conservati. Due aree passano da non misurabili a misurabili, quindi i valori precedenti non sono confrontabili con i successivi.

Nessuna misura va perduta: i punteggi derivano dalle acquisizioni, che restano.

**Dodici prove — M4.5c**

Sessantaquattro prove complessive.

Fra le proprietà verificate.

* **Non avere le liste non vale quanto non aver trovato nulla.** Due reti con traffico identico: quella con le liste ottiene dieci punti, quella senza ne ottiene zero **ottenibili**.
* **Un dominio contattato quattrocento volte pesa più di dieci contattati una volta.** Contando i domini la rete peggiore sembrerebbe la migliore.
* **La soglia per le minacce è più alta.** Al novantanove per cento bloccato il tracciamento ha già tutto, le minacce no.
* **Un dominio sospetto abbassa il punteggio senza azzerarlo**, e resta distinto da una minaccia confermata.
* **Il testo mostrato all'utente contiene «limite inferiore».** È l'unica prova che verifica una formulazione anziché un numero, e sorveglia l'unica affermazione che il prodotto non potrebbe sostenere.

### Verified

Prima valutazione completa su installazione reale.

```
overallScore 57, status Warning, coverage 67, algoritmo 3.0.0

ThreatProtection    12 / 12   PartiallyMeasured
DnsSecurity      13,93 / 20   Measured
PrivacyProtection    4 / 20   Measured
DeviceHealth         0 / 0    NotMeasurable
Configuration      7,5 / 10   Measured
NetworkIntegrity  1,04 / 5    PartiallyMeasured
```

Il giudizio su Privacy Protection è severo e corretto: *interrogazioni verso domini di tracciamento bloccate, 0% su 60*. La rete osservata contatta tracciatori e non ne blocca alcuno.

Threat Protection ottiene il punteggio pieno su ciò che è stato misurato e resta `PartiallyMeasured`: tredici punti di peso escludono l'indicatore di blocco, perché *il filtro non è stato messo alla prova*. La copertura scende invece di regalare punti.

### Added

**Script di prova**

`scripts/generate-dns-traffic.sh` genera traffico DNS verso il server locale, da eseguire in un container che ne condivide la rete.

Serve perché su Docker Desktop per Windows l'inoltro UDP verso il container non è affidabile, e le prove manuali non raggiungono la soglia minima di osservazione.

---

## Documentation Release 1.4.0 — Indicatori di privacy e minaccia — 2026-08-31

Le due aree che valgono quarantacinque punti su cento smettono di essere elenchi di titoli e diventano misure calcolabili.

### Changed

**Specification 07 alla 3.0.0**

Privacy Protection e Threat Protection elencavano i propri indicatori come soli titoli, mentre la stessa specifica stabilisce che un titolo non è utilizzabile. La contraddizione non emergeva finché quelle aree erano non misurabili.

Ogni area ha ora **due indicatori**: quanto la rete è esposta, e quanto le viene impedito.

Rispondono a due domande che l'utente si pone entrambe. Il solo blocco premierebbe un filtro efficace su una rete assediata; la sola esposizione ignorerebbe il lavoro del filtro.

**L'esposizione al tracciamento si conta per interrogazione, non per dominio.**

Dieci domini contattati una volta ciascuno e un solo dominio contattato quattrocento volte descrivono reti diverse. Contare i domini le farebbe apparire uguali.

**L'esposizione alle minacce si conta per dominio, non per quota.**

Un dominio di malware contattato una sola volta è un fatto rilevante, e diluirlo sul totale delle interrogazioni lo farebbe sparire. Per il tracciamento la proporzione informa, per una minaccia il numero assoluto informa di più.

Un dominio soltanto sospetto riduce il punteggio senza azzerarlo: la segnalazione non è confermata, e trattarla come accertata attribuirebbe alla rete un problema non dimostrato.

**Soglia minima di osservazione: cento interrogazioni.**

Una rete che non ha contattato alcun tracciatore in tre interrogazioni non è protetta: non è stata osservata.

Senza la soglia, la rete meno usata otterrebbe il punteggio migliore, e il punteggio misurerebbe il silenzio.

**Indicatori rimossi.**

`Tracker Blocking`, `Analytics Detection`, `Advertising Domains` e `Telemetry Detection` descrivevano la stessa misura suddivisa per categoria, senza pesi distinti motivati. Confluiscono nei due nuovi indicatori.

`Privacy Configuration` è rimosso perché ogni impostazione che avrebbe potuto misurare è già valutata da Resolver Configuration e da Filtering Configuration. Contarla di nuovo avrebbe premiato due volte lo stesso fatto.

`Threat Intelligence` è rimosso perché non è un indicatore: è il nome del sottosistema che produce la classificazione da cui gli altri derivano.

**Specification 09 alla 1.5.0**

Due vincoli di presentazione.

*Known, not absent.* Le liste affermano solo in positivo: dicono che un dominio traccia, non che non traccia. Il punteggio pieno significa **nessun tracciamento noto**, mai nessun tracciamento. Dire «rete pulita» sarebbe l'unica affermazione dell'intero prodotto che il prodotto non può sostenere.

*Device visibility.* L'elenco dei dispositivi comprende soltanto quelli che usano questo servizio DNS. Un apparecchio con un resolver proprio non risulta privo di attività: risulta inesistente.

La condizione va dichiarata perché riguarda proprio gli apparecchi che l'utente non percepisce come dispositivi connessi — televisori, console, oggetti domestici — che sono anche quelli che più spesso portano un resolver cablato dal produttore.

### Rationale

Un'asimmetria governa entrambe le aree.

| Osservazione | Affidabilità |
| --- | --- |
| Esposizione elevata | Attendibile: quei domini sono noti per tracciare |
| Esposizione nulla | Non attendibile come assoluzione |

Il calcolo è simmetrico, la formulazione no. È il motivo per cui la Specification 09 vincola le parole e non solo i numeri.

### Known Impact

**L'algoritmo passa alla versione 3.0.0.**

I punteggi prodotti finora non sono confrontabili con i successivi: due aree che erano non misurabili diventano misurabili. Sarà necessaria una migrazione che li rimuove anziché conservarli, come già fatto alla versione 2.0.0.

**Nessuna lista fornisce la categoria `Analytics`.**

I domini di analisi ricevono in pratica `Tracking`, secondo quanto dichiara la fonte adottata. La categoria resta nel modello e non viene attribuita per convenienza.

**La copertura non raggiungerà 100.**

Device Health resta priva di definizioni calcolabili, e Tracking Blocking è escluso quando non c'è nulla da bloccare. Una rete senza tracciamento noto avrà copertura inferiore a una rete tracciata, ed è corretto: il suo filtro non è stato messo alla prova.

---

## Milestone M4.4 — La classificazione entra in funzione — 2026-08-31

Per la prima volta un dominio osservato riceve una categoria, e la categoria dichiara da dove viene e quanto è recente.

### Added

**Licenza del progetto**

**GPL-3.0**. Testo integrale in `LICENSE.md`, Specification 14 alla 2.0.0.

La promessa del progetto non è bloccare i tracciatori: è che l'utente possa verificare che cosa il programma fa. Una licenza permissiva consentirebbe di distribuire una versione chiusa con le stesse schermate che dichiarano che i domini non lasciano il dispositivo, senza che nessuno possa verificarlo.

La versione 3 e non la 2 anche per una ragione concreta: `SQLitePCLRaw` è Apache-2.0, compatibile con GPLv3 e non con GPLv2.

**Liste predefinite in essere — M4.4a**

Sette liste del Block List Project inserite al primo avvio tramite `AddIfAbsent`, mai `Save`.

Una lista disattivata dall'utente, o con l'indirizzo modificato, è una sua decisione: riavviare il programma non è un'occasione per disfarla.

Nascono senza età e senza voci. Sono descrizioni, e una descrizione senza file è una lista che non esiste ancora.

**Classificazione durante l'acquisizione — M4.4b**

I domini vengono classificati **prima** di essere scritti.

Classificare in lettura produrrebbe uno storico in cui ogni dominio appare come se fosse sempre stato ciò che le liste dicono oggi. Classificando in scrittura, ogni periodo conserva il giudizio che era possibile dare in quel momento.

La conseguenza è dichiarata: i periodi osservati prima che le liste esistessero restano `Unknown` per sempre.

**Scaricamento periodico — M4.4c**

Intervallo predefinito di ventiquattro ore. Una lista viene scaricata quando non è mai stata scaricata o quando quella conservata è più vecchia dell'intervallo.

`UpdatedAt` viene registrato soltanto dopo che il file è arrivato ed è stato riletto: scriverlo prima dichiarerebbe una freschezza che la lista non ha.

Un aggiornamento fallito non invalida nulla. Il log distingue i due casi, perché sono situazioni diverse: `the copy from {UpdatedAt} stays in use and keeps ageing` quando una copia esiste, `it classifies nothing` quando non esiste.

Limite di 64 MB per lista: gli indirizzi li sceglie l'utente, e un indirizzo sbagliato non deve poter riempire il disco.

### Verified

Verifica sull'installazione reale.

```
"name": "google-analytics.com"
"category": "Tracking"
"categoryConfidence": "High"
"categorySource": "Block List Project - Tracking"
"categorySourceUpdatedAt": "2026-08-31T09:15:55Z"
```

Categoria, confidenza, fonte ed età: la catena dal DNS al giudizio è completa e ogni affermazione dichiara su cosa si fonda.

### Known Impact

**La copertura resta 27.**

Il NPSS non riceve i domini: `NpssEvaluationInput` non li contiene. Classificare non incide sul punteggio.

Le aree Privacy Protection e Threat Protection elencano i propri indicatori come **soli titoli**, mentre la stessa Specification 07 stabilisce che un indicatore descritto soltanto da un titolo non è utilizzabile. La contraddizione non emergeva finché quelle aree erano non misurabili.

Definirli in forma calcolabile è il milestone successivo, e va svolto in documentazione prima che in codice.

**Osservazione sulla sorgente, da confermare.**

Technitium non elenca fra i domini quelli le cui interrogazioni sono fallite. Osservato su tredici query, di cui cinque fallite, con quattro domini registrati.

Se si conferma, va registrato nella Specification 04.

---

## Documentation Release 1.3.0 — Fonti e conflitti — 2026-08-04

Le liste predefinite sono scelte, e la regola che decide fra liste in disaccordo è dichiarata.

### Added

**Ricerca sulle fonti — documento 17, stato Analysis**

Licenze lette dai file di licenza delle fonti, non da riassunti. Una prima ricerca riportava il Block List Project come licenziato MIT; il file `LICENSE` dichiara Unlicense.

Fonti esaminate: Block List Project, HaGeZi, oisd, StevenBlack, Disconnect, Peter Lowe, ShadowWhisperer, lightswitch05, Phishing Army, DuckDuckGo Tracker Blocklists.

Due esclusioni derivano dalle nostre regole. Peter Lowe non dichiara licenza, e la Specification 08 la rende obbligatoria. Disconnect, Phishing Army e DuckDuckGo sono NonCommercial: indicarle come predefinite imporrebbe all'utente una condizione che non ha scelto.

**Liste predefinite — Specification 08 alla 1.3.0**

Sette liste del Block List Project: `ads`, `tracking`, `malware`, `phishing`, `crypto`, `scam`, `abuse`.

Escluse deliberatamente le liste `facebook`, `twitter`, `tiktok` e `whatsapp`: elencano domini di servizi che l'utente può usare di proposito, e classificarli come minaccia è un giudizio editoriale che non ci spetta. Escluse anche le liste di contenuto, fuori dallo scopo del progetto.

**Regola sui conflitti fra liste**

Un dominio può comparire in più liste con categorie diverse. La specifica stabiliva l'arresto alla prima corrispondenza senza stabilire in quale ordine le liste venissero consultate: un dettaglio implementativo decideva quale categoria l'utente vede.

La risoluzione avviene ora in tre passaggi.

1. Vince il nome più vicino, qualunque sia la categoria. La specificità è un segnale più forte della gravità: lasciare che una categoria grave trovata sul padre prevalga su una corrispondenza diretta significherebbe sostituire un'affermazione con un'inferenza.
2. A parità di distanza vince la categoria più grave, secondo un ordine dichiarato che pone la sicurezza prima della privacy e le categorie descrittive per ultime.
3. A parità di gravità vince la lista aggiornata più di recente, poi il nome in ordine alfabetico, affinché lo stesso insieme di liste produca sempre lo stesso risultato.

L'ordine di gravità è un giudizio editoriale dichiarato, come i pesi del punteggio. Non deriva da una misura e non pretende di derivarne.

### Changed

**Classification Engine allineato**

Le liste vengono ordinate una sola volta, alla costruzione del motore, per gravità e poi per freschezza.

Cinque nuove prove, fra cui quella che verifica che due ordinamenti della stessa installazione affermino la stessa cosa.

### Known Impact

**Il sistema mostra una sola categoria.**

Le altre categorie nelle quali il dominio compare non vengono presentate. La perdita è dichiarata nella specifica: la classificazione mostrata è la più grave fra quelle trovate, non l'unica trovata.

Rappresentarle tutte richiede una modifica del Unified Data Model e resta una possibilità aperta.

**Una sola fonte predefinita.**

I suoi errori diventano i nostri e i suoi silenzi diventano `Unknown`.

La ricerca di una seconda fonte non ha risolto il rischio: l'unica compatibile e liberamente licenziata è a monte della prima, che la ingerisce quotidianamente. Adottarle entrambe avrebbe dato l'aspetto di pareri indipendenti senza esserlo, e un accordo apparente è peggio di una dipendenza dichiarata.

**Categorie senza fonte.**

`Analytics`, `Social`, `Streaming`, `Cloud` e `AI Services` non hanno alcuna lista predefinita. I domini che vi apparterrebbero restano `Unknown`.

---

## Milestone M4.3 — Classificazione dei domini — 2026-08-04

Il sistema sa attribuire una categoria a un dominio, dichiarando da quale lista proviene e quanto è recente.

### Added

**Modello**

`ClassificationList`, e su `Domain` le proprietà `CategoryConfidence`, `CategorySource`, `CategorySourceUpdatedAt`.

Sono assenti quando la categoria è `Unknown`: non esiste una lista a cui attribuire l'affermazione, e inventarne una sarebbe un registro falso.

**Persistenza**

Migrazione `0007`, `ClassificationListRepository` per la descrizione delle liste, `ClassificationListStore` per i file.

La tabella delle liste non è legata ad alcun Observation Period. Se lo fosse finirebbe sotto ritenzione, e applicare la ritenzione rimuoverebbe lo strumento con cui si classifica anziché un'osservazione invecchiata.

**Classification Engine**

Corrispondenza risalente dal nome completo verso il dominio superiore, arresto alla prima corrispondenza, confidenza dipendente da come la corrispondenza è stata ottenuta.

Il motore riceve le liste già caricate e non apre alcun file: il Core continua a non sapere dove i dati risiedono.

**Progetto di test della persistenza**

`TivuStream.Pie.Storage.Tests`, riferito al lettore dei file.

Il lettore è il punto in cui un file di formato non controllato diventa qualcosa su cui il Core agisce. Un errore lì non interrompe nulla: produce una lista vuota, e lo strumento riferisce una rete in cui non è stato trovato niente perché niente è stato guardato.

È un guasto che assomiglia a una buona notizia, ed è la ragione per cui questo progetto esiste.

Quarantasette prove complessive, tutte riuscite.

### Changed

**Specification 11 alla 1.3.0**

Il documento non elencava `TivuStream.Pie.Storage`, assente dall'albero dei progetti e dalla tabella delle responsabilità fin dal milestone M5.1.

Incoerenza segnalata e corretta, insieme all'aggiunta dei due progetti di test e dei riferimenti fra progetti.

**Specification 16 alla 1.1.0, Specification 08 alla 1.2.1**

I domini contenuti nelle liste risiedono su file, in `data/lists/`, nel formato originale. Il database conserva la descrizione della lista.

Una lista può contenere centinaia di migliaia di nomi, e un file di testo si apre con un editor qualsiasi mentre una tabella richiede uno strumento SQL. La Specification 08 chiede che l'utente possa verificare perché un dominio è classificato, e una verifica che richiede competenze tecniche non è disponibile a chi dovrebbe usarla.

### Known Impact

**Il motore non è ancora collegato al flusso di acquisizione.**

I domini acquisiti continuano ad arrivare con categoria `Unknown`. Il codice che classifica esiste, è verificato, e nessuno lo chiama.

Il collegamento è il milestone successivo, e fino ad allora la copertura del NPSS resta sotto la soglia minima.

**Il lettore comprende due formati.**

Un dominio per riga, e il formato hosts. Il formato adblock non è supportato.

Le righe non comprese vengono contate, mai indovinate. Una lista compresa a metà non viene riferita come compresa per intero.

**Nessuna lista predefinita è ancora definita.**

La scelta delle fonti richiede la verifica delle licenze e resta una decisione da assumere esplicitamente.

---

## Documentation Release 1.2.0 — 2026-08-04

Definisce il meccanismo di classificazione dei domini, che resta interamente locale e dichiara la propria età.

È la revisione che precede il Threat Engine: nessuna riga di codice viene scritta prima che la regola sia scritta.

### Added

**Liste di classificazione — Specification 08 alla 1.2.0**

Struttura di una lista: nome, indirizzo di origine, categoria, **licenza**, ultimo aggiornamento riuscito, numero di voci, stato di attivazione.

La licenza è obbligatoria. Una lista priva di licenza dichiarata non viene distribuita con il progetto.

**L'insieme delle liste predefinite non è definito da questa specifica.** La scelta richiede la verifica della licenza, della manutenzione e della qualità di ciascuna fonte, ed è una decisione da assumere esplicitamente prima del rilascio pubblico. Definire il meccanismo non autorizza a inventare le fonti.

**Regola di corrispondenza**

Il confronto risale dal nome completo verso il dominio superiore, una etichetta alla volta, e si arresta alla prima corrispondenza.

La confidenza dipende da come la corrispondenza è stata ottenuta.

| Corrispondenza                        | Confidenza |
| ------------------------------------- | ---------- |
| Nome completo presente in lista        | `High`     |
| Corrispondenza su un dominio superiore | `Medium`   |

Il secondo caso è un'inferenza. Resta ragionevole e resta un'inferenza, e viene dichiarata come tale.

**Freschezza dichiarata**

Ogni classificazione dichiara l'età della lista dalla quale proviene.

La classificazione locale comporta un ritardo nel riconoscimento delle minacce recenti. Il ritardo non viene nascosto: viene misurato e dichiarato, come la copertura del punteggio e la qualità delle misure.

**Comportamento in caso di aggiornamento fallito e in assenza di rete**

Un aggiornamento non riuscito non invalida la lista esistente: il sistema continua a usarla e ne dichiara l'età crescente.

Nessuna funzione di analisi dipende dalla disponibilità della rete.

**Entità ClassificationList — Specification 05 alla 2.1.0**

Il modello acquisisce l'entità che descrive una lista, e `Domain` acquisisce `categoryConfidence`, `categorySource` e `categorySourceUpdatedAt`.

Queste proprietà sono assenti quando la categoria è `Unknown`.

**Presentazione — Specification 09 alla 1.4.0**

L'interfaccia dichiara per ogni classificazione la lista di provenienza, la sua età e la confidenza.

Un dominio non classificato viene presentato come **non classificato**, mai come sicuro: è la differenza fra dire che non si sa e dire che non c'è nulla.

**Glossario alla 1.5.0**

Voci `Classification List` e `Classification Freshness`.

### Rationale

La classificazione locale è stata confermata dopo aver considerato l'alternativa.

Consultare un servizio di reputazione esterno migliorerebbe l'accuratezza sulle minacce recenti, e comunicherebbe a quel servizio i domini contattati dalla rete dell'utente. Anche le tecniche che trasmettono soltanto un prefisso del dominio richiedono la rete al momento della classificazione, rivelano quando e quanto una rete è attiva, e restringono l'insieme dei candidati con il ripetersi delle interrogazioni.

Il costo non è tecnico: trasformerebbe una promessa verificabile in una promessa da valutare.

La rigidità viene quindi mantenuta dove protegge qualcosa di concreto, la cronologia di navigazione, e il ritardo che ne deriva viene dichiarato anziché subito in silenzio.

### Known Impact

Le euristiche per i domini non classificati non fanno parte di questa release.

Restano un milestone successivo, e fino ad allora un dominio assente da ogni lista resta `Unknown`.

---

## Milestone M4.2 — Allineamento alla 1.1.0 e primi test — 2026-08-04

Il codice recepisce la revisione documentale, e per la prima volta le regole del progetto sono verificate automaticamente.

### Added

**Misure qualificate**

`MeasurementQuality` e `DeviceIdentityBasis` nel modello.

I domini univoci sono dichiarati **limite inferiore**. Gli istanti di osservazione derivati da statistiche su finestra sono dichiarati **vincolati al periodo** anziché presentati come osservazioni.

**Identità dei dispositivi da indirizzo hardware**

L'Adapter legge le assegnazioni DHCP quando la sorgente le fornisce, e ne ricava un'identità stabile fra cambi di indirizzo.

L'acquisizione non fallisce mai: se la sorgente non svolge quel ruolo, o se l'account non può leggere quella sezione, l'identità resta fondata sull'indirizzo di rete e i dispositivi lo dichiarano.

**Progetto di test**

Prima verifica automatica del progetto. Quattordici prove sul motore NPSS, tutte riferite a impegni presi nelle Specification e non a dettagli implementativi.

Fra le proprietà verificate.

* Ciò che non è osservato ha zero punti **ottenibili**, non zero punti.
* Un'area non misurabile conserva il **peso nominale**, altrimenti sparirebbe la traccia di ciò che manca.
* **Togliere dati non può alzare il risultato**: è la proprietà che rende il punteggio non manipolabile per omissione.
* Un'impostazione disattivata abbassa il punteggio ma non la misurabilità: è un fatto sulla rete, non una lacuna nell'osservazione.
* Sotto la soglia non viene prodotto alcun riassunto, ma il breakdown esiste comunque.

Dipendenze approvate, referenziate soltanto dai progetti di test e mai dal prodotto distribuito.

| Nome | Scopo | Licenza |
| --- | --- | --- |
| `xunit` | Verifica automatica | Apache-2.0 |
| `xunit.runner.visualstudio` | Esecuzione delle prove | Apache-2.0 |
| `Microsoft.NET.Test.Sdk` | Piattaforma di test | MIT |

### Changed

**Soglia di copertura applicata**

`Npss.OverallScore` e `Npss.Status` sono ora assenti quando la copertura è inferiore a 60.

Anche il registro operativo lo dichiara: nessun punteggio prodotto, breakdown registrato.

**Algoritmo alla versione 2.0.0**

La migrazione 0006 rimuove i punteggi calcolati con la versione precedente anziché conservarli. Non sono confrontabili con i successivi, e mantenerli produrrebbe uno storico apparentemente continuo. Nessuna misura va perduta: i punteggi derivano dalle acquisizioni, che restano.

### Found

**Un ramo del motore non è raggiungibile**

Scrivendo le prove sul trend è emerso che quel calcolo **non viene mai eseguito**.

Il trend richiede un punteggio complessivo, che richiede copertura 60. Ma le aree dipendenti dalla classificazione dei domini e dal Device Engine valgono sessanta punti su cento, e nessuno dei due esiste: nessun input possibile raggiunge la soglia.

Abbassare la soglia nelle prove avrebbe verificato una regola che il prodotto non applica. La condizione è annotata nel file di test e resterà tale finché il Threat Engine non sarà disponibile.

I test non hanno trovato un difetto: hanno trovato una parte di sistema che nessuno stava esercitando.

---

## Documentation Release 1.1.0 — 2026-08-04

Revisione nata da una valutazione critica del progetto, non da un requisito nuovo.

Tre difetti erano stati rilevati: il punteggio complessivo presentava un giudizio come se fosse una misura, i valori approssimati non venivano dichiarati tali nonostante la specifica lo imponesse, e due proprietà temporali erano valorizzate con dati plausibili anziché osservati.

Nessun codice è stato prodotto in questa release.

### Root Cause

I tre difetti avevano un'unica origine.

Il punteggio trattava già la conoscenza come **graduata**: area misurata, parzialmente misurata, non misurabile.

Il singolo valore era invece trattato in modo **binario**: noto oppure vuoto.

Quella asimmetria costringeva a scegliere fra due errori ogni volta che un dato era noto con precisione ridotta: inventare un valore plausibile, oppure scartare un'informazione realmente posseduta. In due casi era stata scelta la prima strada.

### Added

**Measurement Quality**

Ogni valore può dichiarare come è conosciuto: `Exact`, `LowerBound`, `PeriodBounded`, `Estimated`.

> Un valore conosciuto con minore precisione viene qualificato, non cancellato e non arrotondato al plausibile.

Applicazioni immediate.

* Il numero di domini univoci diventa un **limite inferiore dichiarato**, che è un'affermazione più precisa di "approssimato" e più onesta di un conteggio.
* Gli istanti di prima e ultima osservazione derivati da statistiche su finestra sono dichiarati **vincolati al periodo**. Quando la stessa proprietà arriva dai log delle interrogazioni diventa esatta, e la differenza è visibile all'utente.

Non è una regola aggiuntiva: è la regola del punteggio, estesa al livello che ne era rimasto escluso.

**Identity Basis**

`Device` dichiara se la propria identità si fonda sull'indirizzo hardware, stabile, oppure sull'indirizzo di rete, che cambia.

Attribuire un comportamento a un dispositivo è l'affermazione più forte che il sistema produce, e la sua solidità va resa visibile.

**Acquisizione delle assegnazioni DHCP**

Quando la Data Source svolge anche il ruolo di server DHCP, l'Adapter ne ricava la corrispondenza fra indirizzo hardware e indirizzo di rete.

È un guadagno di accuratezza reale, non una qualifica: l'identità del dispositivo sopravvive ai cambi di indirizzo.

### Changed

**Il punteggio complessivo è dichiarato come giudizio**

Il breakdown è misura: chiunque disponga degli stessi dati ottiene gli stessi valori.

Il numero complessivo richiede di stabilire quanto ciascuna area conti, e quella scelta non discende dai dati.

I pesi diventano una **posizione editoriale dichiarata del progetto**, motivata voce per voce, versionata insieme all'algoritmo e rivedibile. Non derivano da uno standard di settore, perché non ne esiste uno.

Un giudizio presentato come misura è una misura falsa.

**Soglia minima di copertura**

Sotto una copertura di **60** il punteggio complessivo **non viene prodotto**.

`Npss.overallScore` e `Npss.status` diventano opzionali.

Il sistema presenta il solo breakdown e dichiara di non disporre di elementi sufficienti per un giudizio complessivo, pur avendo misure valide da mostrare.

La Specification 09 vieta di sostituirlo con un numero provvisorio, una barra vuota o un segnaposto che suggerisca un valore in arrivo.

È la scelta che distingue maggiormente il progetto: uno strumento che rifiuta di dare un voto quando non ha elementi sufficienti.

**09 - Network Privacy**

L'interfaccia non presenta mai come esatto un valore qualificato diversamente, dichiara la base dell'identità dei dispositivi e comunica il punteggio trattenuto come scelta del sistema, non come guasto.

### Consequence

Con la copertura attuale di 35, il sistema **non produrrà più un punteggio complessivo** finché il Threat Engine non sarà disponibile.

È il comportamento voluto. Il 69 mostrato ieri poggiava su un terzo del sistema di valutazione.

---

## Milestone M4.1 — Configurazione e primo motore del Core — 2026-08-03

Il progetto Core, vuoto dall'inizio, contiene il primo motore. PIE produce un punteggio.

### Added

**Acquisizione della configurazione**

Entità `SourceConfiguration`, interfaccia di capacità corrispondente, lettura delle impostazioni nel Technitium Adapter, migrazione 0004 e persistenza.

Le proprietà sono espresse in termini indipendenti dal backend: nessun nome proprio di Technitium compare nel modello.

La configurazione è conservata **per periodo di osservazione**, non come stato corrente. Se una impostazione cambia e il punteggio ne risente, lo storico saprà dire quando è accaduto.

**La capability si deduce dai permessi del token**

L'Adapter dichiara `SourceConfiguration` soltanto se l'account può leggere le impostazioni del server.

Senza il permesso, il sistema non fallisce: dichiara semplicemente meno, e la copertura del punteggio lo riflette.

**`NpssEngine`**

Primo motore del Core. Calcola DNS Security, Configuration e Network Integrity secondo le definizioni della Specification 07, e dichiara non misurabili le tre aree che dipendono da motori non ancora scritti.

Per ogni area conserva i **fattori** che hanno determinato il punteggio, requisito minimo perché ogni variazione sia spiegabile.

Il motore riceve i dati e non ne cerca: non sa da dove provengano né dove finirà il risultato.

**Migrazione 0005, `ScoreRepository` e `GET /api/v1/npss`**

Il punteggio e il suo dettaglio sono conservati per periodo. Il Query Flow legge, non ricalcola.

### Changed

**`Npss.Trend` diventa opzionale**

Il primo punteggio non ha un predecessore, e punteggi con copertura diversa non sono confrontabili.

Con l'enumerazione precedente sarebbe stato necessario dichiarare `Stable`, che significa "nessuna variazione significativa": un'affermazione che il sistema non può sostenere.

Vale la stessa regola già applicata alla reputazione dei domini: ciò che non si conosce resta vuoto.

**`NpssEngine` riceve il fornitore del tempo**

L'istante non viene letto dall'orologio di sistema. Un motore che legge l'orologio non è verificabile: non sarebbe possibile riprodurne una valutazione a un momento scelto.

### Fixed

**La continuità penalizzava le installazioni recenti**

I periodi attesi erano sempre ventiquattro. Un'istanza avviata da cinque ore risultava con diciannove periodi mancanti e un punteggio ridotto.

Quei periodi non mancavano per un problema della rete: mancavano perché il sistema non stava ancora osservando.

I periodi attesi decorrono ora dalla prima osservazione registrata. Chi osserva da cinque ore senza interruzioni ottiene il punteggio pieno.

Il difetto è emerso alla prima esecuzione reale del motore.

### First Evaluation

Prima valutazione prodotta su dati reali.

| Grandezza      | Valore |
| -------------- | ------ |
| Punteggio      | 69     |
| Stato          | Fair   |
| Copertura      | 35     |
| Trend          | assente, primo punteggio |

Il sistema ha inoltre rilevato la prima condizione reale della configurazione dell'utente: filtraggio dei domini attivo ma **nessuna lista configurata**, quindi privo di effetto.

---

## Documentation Release 1.0.6 — 2026-08-03

Definizione di ciò che il punteggio misura, preliminare alla scrittura del Core.

Nessun codice è stato prodotto in questa release.

### Context

La Specification 07 elencava gli indicatori come **titoli**, non come definizioni. Nessuno indicava come si traduce in punti.

Portarli in codice così com'erano avrebbe significato deciderne il significato dentro un metodo, rendendo il punteggio dipendente da un'interpretazione non verificabile. È l'opposto del principio di Transparency, che richiede che ogni valutazione sia spiegabile.

### Changed

**07 - NPSS**

Ogni indicatore è ora definito in modo calcolabile, con il dato da cui deriva dichiarato.

* **DNS Security** passa da cinque a quattro indicatori da 5 punti: validazione DNSSEC, cifratura del trasporto, configurazione del resolver, errori DNS.
* **Configuration** passa da quattro indicatori sovrapposti a due: raggiungibilità della sorgente e configurazione del filtraggio.
* **Network Integrity** viene ridefinita attorno alla continuità dell'osservazione. Il precedente indicatore sugli errori duplicava DNS Errors.

Rimosso l'indicatore **Query Validation**, privo di significato distinto da DNSSEC Validation. È stato eliminato anziché reinterpretato: un indicatore senza definizione propria avrebbe prodotto punti arbitrari.

L'indicatore sulla cifratura distingue la disponibilità dei trasporti dal loro utilizzo effettivo. Misurare solo la prima premierebbe un'intenzione, misurare solo il secondo ignorerebbe una configurazione corretta.

Dichiarato un limite: la cifratura misurata è quella fra dispositivi e server locale, non fra server e resolver esterni.

**05 - Data Model**

Introdotta l'entità `SourceConfiguration` e la capability corrispondente.

Il modello non aveva alcun posto per la configurazione della sorgente, benché la Specification 04 la elencasse fra i dati da acquisire.

**04 - Technitium Integration**

Il token richiede ora la sola lettura su **Dashboard** e **Settings**.

Il secondo permesso è necessario per acquisire la configurazione, dalla quale dipendono due aree del punteggio. Nessun permesso di modifica è richiesto: PIE non altera mai la configurazione della Data Source.

**08 - Threat Intelligence**

Stabilito che la classificazione avviene **esclusivamente in locale**.

I domini contattati dalla rete non vengono mai trasmessi a terzi, nemmeno per stabilire se siano pericolosi.

Un dominio interrogato rivela cosa un dispositivo stava facendo: consultare un servizio esterno significherebbe comunicargli la cronologia della rete che si sta proteggendo. Uno strumento che analizza la privacy non può ottenere i propri risultati riducendola.

Conseguenze accettate e dichiarate: le minacce recenti vengono riconosciute con il ritardo di aggiornamento delle liste, e l'accuratezza dipende dalla qualità delle liste adottate. In compenso il sistema funziona anche senza connessione.

### Expected Coverage

Con queste definizioni, la copertura del punteggio cresce per gradi.

| Milestone | Aree misurabili                                        | Copertura |
| --------- | ------------------------------------------------------ | --------- |
| M4.1      | DNS Security, Configuration, Network Integrity parziale | circa 35  |
| M4.2      | più Privacy Protection e Threat Protection              | circa 80  |
| M4.3      | più Device Health                                       | circa 100 |

La copertura viene dichiarata all'utente in ogni caso, come previsto dalla specifica.

---

## Milestone M3.6 — Ispezionabilità — 2026-08-03

Correzione di un'incompletezza e introduzione degli endpoint che rendono verificabile ciò che viene conservato.

### Fixed

**I domini bloccati non venivano acquisiti**

`GetDomainsAsync` interrogava soltanto l'elenco dei domini risolti. Technitium tiene quelli bloccati in un elenco separato.

Un dominio bloccato risultava quindi fra le interazioni ma assente dall'elenco dei domini, benché fosse stato osservato. La Specification 05 definisce `Domain` come dominio osservato durante l'analisi, senza distinguere per esito.

Il difetto è emerso da un'osservazione dell'utente sui conteggi riportati nei log: cinque domini a fronte di sei interazioni.

Sono proprio i domini bloccati quelli più significativi per uno strumento di privacy, trattandosi dei tracker e dei domini malevoli che il filtro ha fermato.

### Added

**Endpoint previsti dalla Specification 06**

* `GET /api/v1/devices` — dispositivi dell'ultimo periodo conservato.
* `GET /api/v1/domains` — domini dell'ultimo periodo conservato.
* `GET /api/v1/domains/{domain}` — dettaglio del dominio con i dispositivi coinvolti.

Fino a questo momento i dati venivano scritti nel database senza alcun modo di rileggerli. Per uno strumento che fonda la propria credibilità sulla trasparenza, l'impossibilità di ispezionare ciò che conserva era una lacuna sostanziale.

**Letture nel repository**

Dispositivi, domini e interazioni relative a un dominio dell'ultimo periodo.

### Design Decisions

**Il dettaglio del dominio dichiara se la correlazione è disponibile.**

Il campo `activityAvailable` distingue l'assenza di interazioni dall'assenza della capacità di osservarle.

Senza questa distinzione un elenco vuoto affermerebbe che nessun dispositivo ha raggiunto il dominio, mentre il sistema potrebbe soltanto non essere in grado di saperlo.

---

## Milestone M3.5 — Domain Activity — 2026-08-03

L'Adapter acquisisce la correlazione fra dispositivi e domini, e PIE la conserva.

È la capacità che distingue un motore di analisi da una dashboard: senza di essa una minaccia può essere rilevata ma non attribuita a un dispositivo.

### Added

**`TechnitiumAdapter` implementa `IDomainActivitySource`**

Rileva il componente di registrazione delle query interrogando le applicazioni installate, legge i log in modo paginato e li aggrega.

**Capability condizionata al rilevamento**

`DomainActivity` viene dichiarata soltanto quando il componente risulta installato sull'istanza.

L'implementazione dell'interfaccia esprime ciò che l'Adapter sa fare; la capability esprime ciò che quella istanza offre in quel momento. La distinzione introdotta in M3.2 trova qui il suo primo caso reale.

**Acquisizione condizionata alla capability**

L'`AcquisitionService` richiede la correlazione solo se la Data Source dichiara di poterla fornire. La capability smette di essere descrittiva e diventa portante.

**Migrazione 0003**

Tabella `domain_activity`, agganciata al periodo di osservazione come le altre.

### Design Decisions

**Esito e protocollo non vengono accorpati.**

Una `DomainActivity` rappresenta la combinazione di dispositivo, dominio, esito e protocollo.

Un dispositivo che ha raggiunto lo stesso dominio sia normalmente sia venendo bloccato ha prodotto due fatti distinti. Unirli avrebbe costretto a scegliere un esito prevalente, affermando qualcosa che non è avvenuto.

La chiave primaria della tabella ripete il criterio, così che lo schema non consenta di violarlo.

**Il registro puntuale non viene mai trattenuto per intero.**

L'aggregazione avviene mentre le pagine arrivano. Le singole interrogazioni esistono per il tempo di una pagina e non oltrepassano l'Adapter.

Non è un accorgimento sulla memoria: è il modo concreto di rispettare quanto la Specification 04 dichiara riguardo alla cronologia di navigazione dei dispositivi.

**Limite di lettura.**

L'acquisizione legge al massimo centomila voci per periodo. Il componente di registrazione ne conserva molte meno per impostazione predefinita, quindi il limite non interviene nell'uso normale: esiste perché una ritenzione configurata male non possa trasformare una singola acquisizione in una lettura illimitata.

### Changed

**Registrazione dell'acquisizione**

Il messaggio riporta ora il periodo osservato e il numero di dispositivi, domini e interazioni.

Riporta conteggi e mai i dati stessi: nessun indirizzo, nessun dominio, nulla che appartenga alla rete dell'utente.

**05 - Data Model** — documentato il criterio di aggregazione di `DomainActivity`.

**04 - Technitium Integration** — documentati il rilevamento del componente e il momento in cui avviene l'aggregazione.

---

## Milestone M5.2 — Persistenza delle acquisizioni — 2026-08-03

Le acquisizioni vengono conservate. Il riavvio non azzera più nulla.

### Added

**`ObservationPeriod`**

Intervallo fisso, allineato all'ora solare, che un'acquisizione osserva.

Colloca un istante nel proprio periodo e sa dire se un periodo è già trascorso, quindi immutabile.

**Migrazione 0002**

Tabelle `statistics`, `device` e `domain`, tutte agganciate a un periodo di osservazione ed eliminate insieme a esso.

Applicare la ritenzione consisterà quindi nell'eliminare periodi, e nient'altro.

**`AcquisitionRepository`**

Registra un'osservazione in un'unica transazione: o viene conservata per intero, o non viene conservata affatto.

L'osservazione di un periodo già osservato **sostituisce** la precedente. Il periodo viene rimosso e riscritto, così che nulla della prima osservazione sopravviva accanto alla seconda.

### Changed

**Acquisizione allineata ai periodi**

Chiude il debito registrato come Known Impact nella Documentation Release 1.0.5.

L'`AcquisitionService` non chiede più una finestra mobile degli ultimi sessanta minuti ma il periodo in cui cade l'istante corrente.

I log della sessione precedente mostravano il problema dal vivo: tre acquisizioni consecutive che osservavano intervalli sovrapposti per cinquantanove minuti su sessanta. Scritte così com'erano, avrebbero prodotto decine di righe al giorno che raccontano la stessa ora.

`WindowMinutes` è stato rimosso dalla configurazione: il periodo è fisso e non più negoziabile. `IntervalMinutes` resta e governa soltanto la freschezza del periodo corrente.

**L'acquisizione raccoglie anche dispositivi e domini**

In precedenza venivano richieste le sole statistiche.

Dispositivi e domini vengono conservati ma non ancora riletti: la lettura arriverà con gli endpoint che li presentano.

**Gli endpoint leggono dal database**

`statistics` restituisce l'ultima osservazione conservata anziché lo stato in memoria.

`health` riporta il numero di periodi conservati.

Lo stato in memoria e il database rispondono a domande diverse e restano separati: il primo dice cosa è appena successo, compresi i guasti, il secondo cosa si sa. Un guasto non cancella lo storico, e lo storico non nasconde un guasto.

### Verified

La persistenza è stata verificata con una prova che esclude ogni altra spiegazione.

1. Traffico DNS generato verso l'istanza, acquisito e reso disponibile dagli endpoint.
2. Numero di periodi conservati rimasto a **1** dopo più acquisizioni della stessa ora: ciascuna ha sostituito la precedente anziché accodarsi.
3. Applicazione arrestata, **Data Source spenta**, applicazione riavviata.
4. `health` ha riportato `Failing`, mentre `statistics` ha continuato a restituire gli stessi valori.

Con la sorgente spenta l'acquisizione è impossibile, e il riavvio azzera la memoria. I dati serviti provenivano quindi necessariamente dal database.

La prova ha confermato anche la separazione fra stato e storico: il sistema ha dichiarato di non riuscire a leggere e nello stesso momento ha continuato a servire l'ultimo dato valido.

---

## Milestone M5.1 — Storage, schema e migrazioni — 2026-08-03

Fondamenta della persistenza. Nessun dato applicativo viene ancora scritto.

### Added

**Progetto `TivuStream.Pie.Storage`**

Referenzia il solo Unified Data Model. Il Core non lo conosce.

**`SqliteConnectionFactory`**

Apre le connessioni e crea la cartella del database se assente.

Abilita i vincoli di integrità referenziale su ogni connessione: SQLite li lascia disattivati per impostazione predefinita, e un riferimento non verificato è un riferimento che prima o poi risulterà errato.

**`SchemaMigrator` e `IMigration`**

Migrazioni scritte a mano, ordinate, applicate ciascuna in una propria transazione.

Nessuna migrazione viene generata a partire dal modello: uno schema prodotto automaticamente cambia ogni volta che cambia il modello, che è esattamente ciò che non deve accadere a dati già presenti sulla macchina di una persona.

Una migrazione già applicata non viene mai modificata; un errore si corregge con una migrazione successiva.

**Rifiuto dello schema più recente**

Se il database dichiara una versione superiore a quella attesa, l'avvio viene interrotto con un messaggio esplicito.

Proseguire significherebbe scrivere record che questa versione non comprende, con danni che emergerebbero molto dopo la causa.

**Migrazione 0001**

Crea le tabelle `data_source` e `observation_period`.

Il vincolo di unicità su sorgente e inizio del periodo **rende impossibile violare la regola dei periodi di osservazione**: una regola architetturale diventa un vincolo del database anziché una raccomandazione.

**`MigrationOutcome`**

Riporta versione iniziale, finale e migrazioni applicate. Una modifica alla forma dei dati conservati non viene mai eseguita in silenzio.

### Dependencies

**Prima dipendenza esterna, e primo blocco dell'audit**

`Microsoft.Data.Sqlite` 10.0.0 ha portato con sé `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, segnalata dall'advisory GHSA-2m69-gcr7-jv3q.

Si tratta di CVE-2025-6965: nelle versioni di SQLite precedenti alla 3.50.2 il numero di termini di aggregazione può eccedere le colonne disponibili, con possibile corruzione di memoria. Gravità 7,2.

L'audit delle dipendenze, configurato in M2.1 quando il progetto non aveva ancora alcuna dipendenza, ha rifiutato la compilazione. Il primo pacchetto esterno del progetto è stato anche il primo caso in cui quel controllo è servito.

**Percorso seguito**

1. Aggiornamento a `Microsoft.Data.Sqlite` 10.0.10, l'ultima disponibile. Insufficiente: risolve ancora a 2.1.11.
2. Pinning transitivo centralizzato dell'intera famiglia SQLitePCLRaw a 2.1.12. Le versioni risultano registrate ma NuGet non le applica.
3. **Riferimento diretto** a `SQLitePCLRaw.bundle_e_sqlite3` nel progetto Storage. Un riferimento diretto prevale sempre sulla risoluzione transitiva.

L'audit accetta la 2.1.12. Nessuna deroga è stata necessaria.

| Nome                              | Scopo                       | Licenza    |
| --------------------------------- | --------------------------- | ---------- |
| `Microsoft.Data.Sqlite` 10.0.10   | Accesso al database SQLite  | MIT        |
| `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 | Libreria SQLite nativa | Apache-2.0 |

Il riferimento diretto alla libreria nativa è una forzatura consapevole, annotata nel file di progetto con la propria condizione di uscita: va rimosso quando `Microsoft.Data.Sqlite` aggiornerà la dipendenza per conto proprio.

### Changed

**Host applicativo**

Lo schema viene portato alla versione attesa prima che qualunque richiesta venga servita, e l'esito viene registrato.

L'endpoint di stato riporta ora lo stato dello Storage e la versione dello schema.

Il percorso del database è deliberatamente escluso dalla risposta: l'endpoint non verifica ancora i permessi, e la posizione dei dati di una persona non è un'informazione da fornire a chiunque la chieda.

---

## Documentation Release 1.0.5 — 2026-08-03

Definizione della persistenza, ultima lacuna documentale rilevata durante la revisione iniziale.

Nessun codice è stato prodotto in questa release.

### Added

**16 - Persistence Specification**

Nuovo documento. Definisce cosa viene conservato, per quanto tempo, dove e con quali garanzie.

Elementi principali.

* Si conservano le acquisizioni convertite nel Unified Data Model e i risultati del Core. Le prime consentono di ricalcolare le analisi su dati storici quando un algoritmo cambia, cosa altrimenti impossibile data la ritenzione dei backend.
* **Non viene mai conservato il dettaglio della singola interrogazione DNS.** L'aggregazione avviene nell'Adapter. È la principale misura di protezione dell'utente prevista dal progetto.
* Ritenzione a livelli: dettaglio orario per 30 giorni, aggregati giornalieri per 12 mesi, aggregati mensili per 5 anni. Valori predefiniti e configurabili.
* Schema versionato, con migrazioni esplicite e ordinate. L'avvio viene rifiutato se lo schema è più recente del software, per evitare corruzione silenziosa.
* Il database non lascia mai il dispositivo. Nessuna telemetria. L'eliminazione dei dati è effettiva e non una marcatura logica.

**Periodi di osservazione**

Concetto centrale introdotto da questa specifica.

Un'acquisizione **non è un insieme di eventi ma l'osservazione di un intervallo**. Due osservazioni di intervalli sovrapposti descrivono in parte lo stesso traffico e non sono sommabili.

Con la configurazione attuale, che acquisisce ogni cinque minuti una finestra di sessanta, si sarebbero prodotte dodici osservazioni all'ora largamente ridondanti. Sommarle avrebbe generato valori privi di senso.

Le acquisizioni sono quindi allineate a periodi fissi, per impostazione predefinita l'ora solare. Una nuova osservazione dello stesso periodo **sostituisce** la precedente; un periodo concluso è immutabile; lo storico è la sequenza dei periodi conclusi, che non si sovrappongono e sono aggregabili.

L'idempotenza è garantita per costruzione. La frequenza di acquisizione diventa un parametro di freschezza, non di correttezza.

**Componente Storage**

Aggiunto ai componenti ufficiali del Backend. Nessuno degli otto esistenti riguardava la conservazione dei dati.

Il Core non lo conosce, il Frontend non lo raggiunge, il Unified Data Model resta privo di qualunque elemento di persistenza.

### Dependencies

Approvata la prima dipendenza esterna del progetto.

| Nome                   | Scopo                    | Licenza |
| ---------------------- | ------------------------ | ------- |
| `Microsoft.Data.Sqlite`| Accesso al database SQLite | MIT   |

L'accesso ai dati avviene tramite SQL esplicito. Schema e interrogazioni restano ispezionabili, senza livelli di comportamento implicito.

### Changed

**11 - Backend Specification** — aggiunto il componente Storage con responsabilità e vincoli, e il riferimento ai periodi di osservazione nell'Adapter Manager.

**00 - Glossary** — aggiunti i termini `Storage` e `Observation Period`.

### Known Impact

L'attuale `AcquisitionService` richiede una finestra mobile degli ultimi sessanta minuti. Andrà allineato ai periodi di osservazione fissi al momento dell'implementazione dello Storage.

---

## Milestone M3.4 — Prima fetta verticale — 2026-08-02

Primo percorso completo dal Data Source al browser: Technitium, Adapter, Unified Data Model, REST API.

Nessuna dipendenza esterna introdotta.

### Added

**`AcquisitionService`**

Servizio in background che acquisisce all'avvio e poi a intervallo regolare. È lo Scheduler della Specification 11 in forma minima.

È l'unico componente che raggiunge una Data Source, e non viene mai innescato da una richiesta del Frontend.

**`AcquisitionState`**

Conserva in memoria l'esito dell'ultima acquisizione. Il Query Flow legge da qui e non raggiunge mai la sorgente: è ciò che tiene separati i due flussi.

Lo stato è volatile. La persistenza è un milestone successivo, quindi il riavvio dell'host azzera l'ultima acquisizione.

**`AcquisitionOptions`**

Intervallo fra due acquisizioni e ampiezza della finestra richiesta, tenuti distinti: sovrapporre la finestra evita di perdere dati ai bordi.

**Involucro di risposta**

`ApiResponse` e `ApiError` secondo la forma definita dalla Specification 06.

**Endpoint**

* `GET /api/v1/health` — stato di API, Core, Adapter e Backend, con descrizione della Data Source e momento dell'ultima acquisizione.
* `GET /api/v1/statistics` — statistiche aggregate acquisite.

Il report di stato dichiara `Core: NotImplemented` anziché omettere la sezione: una sezione assente si leggerebbe come una sezione senza nulla da segnalare, che è un'affermazione diversa.

**Configurazione**

`appsettings.Local.json`, escluso da Git, ospita il token. Il file `appsettings.Local.json.example` documenta come ottenerlo.

### Design Decisions

**Le enumerazioni viaggiano come nomi.** Un numero sarebbe privo di significato per chi legge la risposta.

**L'Adapter viene risolto all'interno del blocco protetto.** Un Adapter privo di configurazione fallisce alla creazione, e un host che si arresta perché manca un token sarebbe un modo scadente di segnalarlo. Con questa scelta il sistema resta in piedi e riporta il motivo tramite l'endpoint di stato.

### Fixed

**Due forme di risposta nelle API di Technitium**

La prima esecuzione end-to-end ha rivelato che le API non usano una sola forma di risposta.

La maggior parte delle chiamate annida il contenuto in una proprietà `response`. Le chiamate relative alla sessione restituiscono invece i propri campi alla radice, accanto a `status`.

L'Adapter cercava sempre la proprietà annidata e riceveva un contenuto vuoto da `session/get`.

L'informazione era già presente nei dati raccolti in M3.1, ma era passata inosservata: la ricognizione aveva confrontato i contenuti e non le forme.

Il client supporta ora entrambe le forme. La distinzione è documentata nella Specification 04, con l'avvertenza che non è deducibile dal nome della chiamata.

### Verified

La fetta verticale è stata verificata contro un'istanza reale di Technitium 15.4.

Il traffico generato — cinque domini, tre ripetizioni, due tipi di record — ha prodotto trenta interrogazioni. Le statistiche acquisite tramite REST API corrispondono a quelle mostrate dalla console del server, con l'unica differenza attribuibile a due interrogazioni successive all'acquisizione.

Elementi confermati.

* Il percorso completo da Data Source a REST API attraversa tutti i livelli previsti.
* `session/get` è la fonte corretta per versione e stato DNSSEC.
* Le capability sono dedotte dai permessi del token e non dichiarate a mano.
* `DomainActivity` non viene dichiarata, coerentemente con il fatto che l'Adapter non ne implementa l'interfaccia.
* **L'intervallo personalizzato conserva il dettaglio per dominio.** L'acquisizione incrementale resta quindi praticabile.

Il comportamento in caso di guasto è stato osservato prima della correzione: l'errore è stato catturato, tradotto, memorizzato e riportato dagli endpoint senza arrestare l'host.

### Declared Debt

Due deroghe consapevoli alla Specification 06, ammesse per l'uso locale e da sanare prima di qualunque esposizione in rete.

* Il servizio ascolta in **HTTP** anziché HTTPS.
* **Nessun endpoint verifica i permessi.**

Sono registrate qui come debito dichiarato, non come dimenticanze.

---

## Milestone M3.3 — Technitium Adapter, livello base — 2026-08-02

Prima implementazione che comunica con una Data Source reale.

Copre le tre capacità disponibili su qualunque installazione Technitium: `Statistics`, `Device` e `Domain`.

Nessuna dipendenza esterna introdotta: `HttpClient` e `System.Text.Json` appartengono al framework. `Directory.Packages.props` resta vuoto.

### Added

**`TechnitiumOptions`** — identificativo della Data Source, indirizzo e API Token. Il token resta confinato nell'Adapter.

**`TechnitiumClient`** — trasporto verso le API pubbliche. Autentica la richiesta, legge l'esito **dal corpo della risposta e non dal codice HTTP**, e converte i guasti in `AdapterException`. Traccia di stack e messaggio interno prodotti dal backend non oltrepassano l'Adapter.

**DTO di deserializzazione** — tipi interni che rispecchiano le risposte dell'API, tenuti separati dal Unified Data Model.

**`DeviceIdentity`** — derivazione deterministica dell'identificatore del dispositivo dal suo indirizzo di rete.

**`TechnitiumAdapter`** — implementa `IStatisticsSource`, `IDeviceSource` e `IDomainSource`. Non implementa `IDomainActivitySource`, coerentemente con il fatto che la correlazione richiede un componente facoltativo.

### Design Decisions

**Descrizione della Data Source da `session/get`.** Una sola chiamata fornisce versione, nome, stato DNSSEC e permessi del token. Le capability vengono dichiarate solo quando il token è realmente in grado di leggere il dato corrispondente.

**`FailedQueries` esclude NXDOMAIN.** Un dominio inesistente è una risposta corretta, non un guasto. Includerlo avrebbe prodotto tassi di errore elevati in assenza di problemi, abbassando indebitamente l'area Network Integrity.

**`UniqueDomains` è approssimato.** La sorgente espone solo i domini più frequenti.

**`FirstSeen` e `LastSeen` sono i limiti della finestra acquisita.** La sorgente riporta attività su intervallo, non istanti di prima e ultima osservazione.

**Finestra tradotta in intervallo personalizzato.** Il comportamento di questa modalità rispetto al dettaglio per dominio non è ancora stato verificato: il primo test end-to-end lo mostrerà.

### Changed

**Modello dati**

* `Domain.Reputation` diventa opzionale. È prodotta dal Threat Engine e un Adapter non la assegna mai.
* `DataSource.Status` e `Device.Status` passano da `string` a enumerazioni dedicate.

L'implementazione ha rivelato che il modello obbligava l'Adapter a valorizzare proprietà di competenza del Core. Per compilare sarebbe stato necessario inventare valori.

**05 - Data Model**

Introdotta la sezione Ownership of Properties con la regola corrispondente, e i vocabolari dei due stati.

**04 - Technitium Integration**

Documentati `session/get` come fonte della descrizione, la composizione di `FailedQueries` e la derivazione dell'identità dei dispositivi con i relativi limiti.

Corretta l'indicazione precedente che attribuiva lo stato DNSSEC alle impostazioni del server, che richiedono permessi più ampi del necessario.

---

## Milestone M3.2 — Contratto Adapter — 2026-08-02

Definizione del contratto che ogni Adapter deve rispettare.

Nessuna implementazione: il progetto `TivuStream.Pie.Adapters` contiene esclusivamente interfacce, un tipo per la finestra temporale e un'eccezione dedicata.

### Added

**Interfaccia di base**

`IAdapter` porta l'identità dell'Adapter e la descrizione della Data Source, comprensiva di versione, stato operativo e capability effettivamente disponibili.

La proprietà `Provider` è disponibile senza contattare la Data Source, così che un Adapter resti identificabile anche quando è irraggiungibile.

**Interfacce segregate per capacità**

`IStatisticsSource`, `IDeviceSource`, `IDomainSource`, `IDomainActivitySource`.

Un Adapter implementa soltanto ciò che è in grado di fornire. Non deve implementare metodi non supportati né sollevare eccezioni di non supporto.

**`AcquisitionWindow`**

Intervallo temporale esplicito per ogni acquisizione.

La ricognizione M3.1 ha dimostrato che finestre differenti non contengono necessariamente le stesse informazioni: l'intervallo non può quindi essere assunto dall'Adapter. Consente inoltre l'acquisizione incrementale.

**`AdapterException`**

Gli errori della Data Source vengono convertiti prima di lasciare l'Adapter. Il resto del sistema non gestisce mai errori espressi nel vocabolario di uno specifico backend, e i dettagli diagnostici non oltrepassano l'Adapter.

**`IAdapterManager`**

Registrazione ed elenco degli Adapter.

Il metodo che esegue il ciclo di acquisizione è deliberatamente assente: la sua forma dipende dal punto di ingresso del Core, non ancora definito, e dichiararlo ora significherebbe indovinarlo.

### Design Rationale

Le interfacce implementate esprimono ciò che un Adapter **può** fornire; le capability dichiarate esprimono ciò che la Data Source fornisce **effettivamente** nella configurazione corrente.

Le due informazioni non coincidono: Technitium implementa la correlazione fra dispositivi e domini soltanto dopo l'attivazione di un componente facoltativo.

Vale quindi la regola che un Adapter non può dichiarare una capability della quale non implementa l'interfaccia. La verifica spetta all'Adapter Manager alla registrazione.

### Changed

**11 - Backend Specification**

Documentati il contratto Adapter, la distinzione fra capacità potenziali ed effettive, la finestra di acquisizione e la gestione degli errori.

---

## Milestone M2.2.1 — Allineamento del modello — 2026-08-02

Allineamento del Unified Data Model implementato alla Documentation Release 1.0.4.

### Added

* `ScoreComponentState`, enumerazione con i valori `NotMeasurable`, `PartiallyMeasured` e `Measured`.
* `ScoreComponent.State`.
* `Npss.Coverage`.

### Changed

* `ScoreComponent.Score` e `ScoreComponent.MaxScore` passano da `int` a `decimal`.

La misurazione parziale produce valori frazionari: un'area di peso 15 con un indicatore valutato su quattro ha un punteggio ottenibile di 3,75. Un tipo intero avrebbe imposto un arrotondamento, e arrotondare la porzione osservata avrebbe alterato la copertura dichiarata.

* `ScoreComponent.Weight` resta `int`: è il peso nominale definito dall'algoritmo, sempre intero.
* Precisata nei commenti la semantica di `MaxScore` come punteggio effettivamente ottenibile.

La corrispondenza con la Specification 05 è verificata proprietà per proprietà.

---

## Documentation Release 1.0.4 — 2026-08-02

Introduzione dello stato di misurazione parziale nel Network Privacy & Security Score.

Nessun codice è stato prodotto in questa release.

### Context

La release precedente prevedeva due soli stati per un'area di valutazione: misurata o non misurabile.

La revisione ha evidenziato un caso reale non rappresentabile. Senza Domain Activity l'area Device Health perde il comportamento dei dispositivi ma conserva il volume di traffico, che resta un dato esatto.

Dichiarare l'intera area non misurabile avrebbe significato scartare un'informazione realmente posseduta.

### Changed

**05 - Data Model**

* `ScoreComponent.state` acquisisce il valore `PartiallyMeasured`.
* Definita la relazione fra `weight` e `maxScore`: il primo è il peso nominale dell'area, il secondo il punteggio effettivamente ottenibile in base agli indicatori valutati.
* La porzione di peso non misurata non concorre né al punteggio ottenuto né a quello ottenibile.
* `NPSS.coverage` è ora la somma dei `maxScore` di tutte le aree.

Nessun campo è stato aggiunto al modello: i campi esistenti hanno ricevuto una semantica precisa.

**07 - NPSS**

* Introdotti i tre stati di misurazione.
* Definita la regola del calcolo parziale: `maxScore` proporzionale alla quota di indicatori valutati, con indicatori di pari peso all'interno dell'area salvo indicazione contraria.
* Aggiornato l'esempio con il calcolo completo e verificabile.
* Le aree parzialmente misurate producono una Recommendation come quelle non misurabili.

**09 - Network Privacy**

* L'interfaccia distingue tre condizioni anziché due.
* Una condizione parziale non viene mai presentata come completa.

**00 - Glossary**

* Aggiornata la definizione di `Coverage`.

### Design Rationale

Il criterio adottato è che l'assenza di dati non possa in alcun caso migliorare il punteggio.

La verifica sull'esempio documentato lo conferma: riconoscere la misurazione parziale ha portato la copertura da 85 a 88,75 e il punteggio da 91 a 90.

La conoscenza aumenta, il risultato no. È il comportamento atteso da uno strumento che non deve trarre vantaggio da ciò che non osserva.

---

## Documentation Release 1.0.3 — 2026-08-02

Recepimento dei risultati della ricognizione M3.1 nelle Specification ufficiali.

Il tema comune delle modifiche è la **distinzione fra ciò che il sistema misura e ciò che non è in grado di misurare**, e l'obbligo di comunicare la differenza all'utente.

Nessun codice è stato prodotto in questa release.

### Changed

**05 - Data Model**

* Definita la semantica di `DataSource.Capabilities`: dichiara quali entità del Unified Data Model la sorgente è in grado di fornire. Il vocabolario coincide con i nomi delle entità. Le funzionalità del prodotto esterno non sono capability ma dati da analizzare.
* `DomainActivity` diventa a disponibilità condizionata, dichiarata tramite capability. In sua assenza il Core non produce l'entità, dichiara non misurabili le analisi che ne dipendono e genera una Recommendation. È vietato sostituire il dato mancante con stime o valori predefiniti.
* `ScoreComponent` acquisisce il campo `state`, con valori `Measured` e `NotMeasurable`.
* `NPSS` acquisisce il campo `coverage`.
* Introdotta la sezione Data Availability, che distingue dato presente, dato assente e dato non misurabile.

**07 - NPSS**

* Un'area non misurabile non riceve punteggio zero: assegnare zero equivarrebbe ad affermare una condizione critica inesistente.
* Il punteggio complessivo è calcolato sulle sole aree misurate e normalizzato sui rispettivi pesi.
* Introdotta la copertura, da mostrare sempre quando inferiore a 100.
* Punteggi con copertura differente non sono confrontabili; una variazione di copertura interrompe la serie storica del trend.
* Ogni area non misurabile produce una Recommendation che dichiara anche le conseguenze sfavorevoli dell'intervento proposto.

**04 - Technitium Integration**

* Documentati i due livelli di disponibilità dei dati, base ed esteso, con le capability corrispondenti.
* Autenticazione tramite API Token non scadente, con utente dedicato a permessi minimi.
* Documentato che l'esito delle chiamate non è espresso dal codice di stato HTTP.
* Introdotti i vincoli di acquisizione: scelta esplicita della finestra temporale, frequenza inferiore al tempo di rotazione del log, dichiarazione dei valori approssimati derivanti da elenchi tronchi.
* Stabilito che l'aggregazione dei log avviene nell'Adapter: il Core non accede mai alla singola interrogazione.

**09 - Network Privacy**

* Introdotta la sezione Honesty of Presentation.
* L'interfaccia distingue sempre il dato assente dal dato non misurabile: uno zero o una sezione vuota affermano che la rete è in ordine, e usarli per rappresentare l'indisponibilità è un'informazione falsa.
* La copertura del punteggio è mostrata quando inferiore a 100.
* Le Recommendation di configurazione sono presentate come azioni proposte, con vantaggi e costi esposti in modo simmetrico prima della scelta.

**12 - Installation**

* Rilevamento delle capacità della Data Source al termine della registrazione.
* Proposta di installazione dei componenti facoltativi, con scelta esplicita, informazione simmetrica e reversibilità.
* Verifica di coerenza fra ritenzione del componente e frequenza di acquisizione, ripetuta a ogni modifica della frequenza.
* Dichiarazione esplicita di quali dati vengono registrati, dove risiedono e per quanto tempo.

**00 - Glossary**

* Aggiunti i termini `Capability` e `Coverage`.

### Fixed

**15 - Technitium API Reconnaissance**

Corretta l'ipotesi iniziale secondo cui `DataSource.Capabilities` andasse valorizzato con i flag di configurazione di Technitium. L'ipotesi confondeva le funzionalità del prodotto esterno, che sono dati da analizzare, con le capacità strutturali della sorgente.

---

## Milestone M3.1 — Technitium API Reconnaissance — 2026-08-02

Ricognizione delle API pubbliche di Technitium DNS Server, propedeutica alla progettazione del contratto Adapter.

Nessun codice prodotto. Nessuna Specification modificata.

### Added

**15 - Technitium API Reconnaissance**

Documento di analisi, non una Specification. Stato `Analysis — Not Approved`.

Contiene autenticazione, endpoint confermati, mapping verso il Unified Data Model proprietà per proprietà, punti aperti e sezioni delle API non ancora verificate.

### Findings

**DomainActivity non è costruibile dalle API del dashboard**

`topClients` e `topDomains` sono aggregati indipendenti: indicano quante query ha prodotto ogni client e quante volte è stato richiesto ogni dominio, ma non quale client abbia contattato quale dominio.

La correlazione richiede i Query Logs, che in Technitium dipendono da una DNS App opzionale.

**Statistiche per finestra temporale, non eventi**

Il modello prevede `firstSeen`, `lastSeen` e `occurrences`, che presuppongono osservazione continua. La sorgente espone aggregati su finestre predefinite. La semantica va documentata.

**Liste troncate**

`getTop` restituisce al massimo `limit` elementi. `Statistics.UniqueDomains` non è calcolabile con esattezza.

**`FailedQueries` non ha una definizione univoca**

Technitium distingue server failure, NXDOMAIN, refused e dropped. NXDOMAIN è una risposta legittima: includerlo fra gli errori altererebbe la valutazione di Network Integrity nel NPSS.

**Verifica su istanza reale**

La ricognizione è stata completata interrogando un'istanza reale di Technitium 15.4 in container. Diciotto endpoint interrogati, diciassette con esito positivo.

Risultati principali.

* `Statistics.DnssecEnabled` risolto: `settings/get` → `dnssecValidation`.
* `DataSource.Capabilities` risolto: i flag `enableDnsOver*`, `dnssecValidation`, `qnameMinimization` ed `eDnsClientSubnet` forniscono un vocabolario derivato dalla sorgente.
* `apps/list` vuoto su installazione nuova e `logQueries` disattivato: conferma definitiva che i Query Logs, e quindi Domain Activity, non sono disponibili di serie.
* `blocked/list` e `cache/list` sono navigatori gerarchici, non elenchi piatti: l'enumerazione richiede di percorrere un albero.
* Le risoluzioni tramite DNS Client API non vengono conteggiate nelle statistiche del dashboard.
* Le statistiche sono persistite su file con ritenzione predefinita di 365 giorni.

**Osservazioni su traffico reale**

La ricognizione è stata completata generando traffico DNS effettivo contro l'istanza.

* `DomainActivity.Protocol` risolto: `protocolTypeChartData.labels` espone etichette in PascalCase, valore osservato `Udp`, insieme completo `Udp`, `Tcp`, `Tls`, `Https`, `Quic`.
* Struttura di `topClients` confermata: `name`, `domain` da risoluzione inversa, `hits`, `rateLimited`.
* `apps/listStoreApps` elenca ventisette app, fra cui quattro varianti di Query Logs e Log Exporter. La descrizione ufficiale avverte dell'impatto sul throughput: l'accesso a Domain Activity comporta un costo prestazionale esplicito.

**La finestra temporale non è indifferente**

A parità di stato del server e nello stesso istante, `LastHour` ha restituito il dettaglio per dominio mentre `LastDay` lo ha restituito vuoto, pur riportando più query e mantenendo le statistiche per client.

L'Acquisition Flow non può assumere che una finestra più ampia contenga tutto ciò che contiene una più stretta. Comportamento da confermare con osservazioni prolungate.

**Verifica dell'app Query Logs**

L'app `Query Logs (Sqlite)` 9.1.1 è stata installata sull'istanza di prova e la sua API interrogata con otto combinazioni di parametri. Tutte hanno risposto correttamente.

* Ogni voce contiene contemporaneamente `clientIpAddress` e `qname`: è la correlazione assente dalle API del dashboard.
* Sono supportati i filtri per client, dominio, tipo di record, protocollo e intervallo temporale, oltre alla paginazione con conteggio totale.
* L'estrazione incrementale è quindi possibile: l'Adapter può richiedere le sole voci successive all'ultima acquisizione.
* Tutte le proprietà di `DomainActivity` risultano ottenibili.

**Ritenzione predefinita insufficiente**

La configurazione dell'app prevede `maxLogRecords: 10000`, che su una rete reale corrisponde a meno di un'ora di traffico. Il limite di sette giorni è quindi teorico.

La frequenza di acquisizione deve essere inferiore al tempo di rotazione del log, altrimenti si perdono dati in modo silenzioso.

La scrittura è bufferizzata in memoria con `maxQueueSize: 200000`, quindi asincrona rispetto alla risoluzione: l'impatto sul throughput è minore di quanto l'avvertenza dell'app lasci supporre.

La configurazione è leggibile e modificabile via API, quindi l'installazione guidata può proporre valori adeguati e l'Adapter può segnalare configurazioni incoerenti.

**Campi aperti**

Chiusi `DataSource.Capabilities` e `DomainActivity.Protocol`. Restano `DataSource.Status` e `Device.Status`, che la sorgente non espone e che vanno definiti da PIE.

---

## Milestone M2.2 — Unified Data Model — 2026-08-02

Implementazione delle entità di dominio definite dalla Data Model Specification.

Il modello è indipendente dalla persistenza: nessun ORM, nessun attributo di mapping o di serializzazione, nessuna logica di business, nessuna validazione.

### Added

**Entità del Unified Data Model**

Undici `sealed record` con proprietà `init`, in `TivuStream.Pie.Model/Entities/`.

`DataSource`, `NetworkSnapshot`, `Statistics`, `Device`, `Domain`, `DomainActivity`, `Threat`, `Alert`, `Recommendation`, `Npss`, `ScoreComponent`.

La corrispondenza con l'elenco delle proprietà della Specification 05 è verificata una a una.

**Enumerazioni**

Sei `enum` in `TivuStream.Pie.Model/Enums/`, limitati ai set di valori esplicitamente elencati nella documentazione.

`ThreatCategory` e `ConfidenceLevel` dalla Threat Intelligence Specification, `SeverityLevel` dalla stessa, `ScoreStatus`, `ScoreTrend` e `ScoreComponentType` dalla NPSS Specification.

I campi il cui insieme di valori non è documentato restano di tipo `string`.

**`Directory.Packages.props`**

Central Package Management abilitato prima dell'introduzione del primo pacchetto NuGet, con pinning delle dipendenze transitive.

Nessun pacchetto è referenziato.

**`global.json`**

Vincola la build a un .NET SDK 10.x.

Senza questo file la build utilizza sempre l'SDK più recente installato sulla macchina. Poiché il progetto tratta i warning come errori e ogni SDK introduce nuove regole degli analizzatori, un aggiornamento dell'ambiente potrebbe far fallire la compilazione di codice non modificato.

`rollForward` è impostato su `latestMinor`: gli aggiornamenti di patch e feature band sono accettati, il passaggio a una major version successiva resta una decisione esplicita.

Il prerequisito è documentato in `CONTRIBUTING.md`.

### Fixed

**Configurazione di build**

La prima compilazione ha rivelato che `IDE0005`, la regola sugli `using` non necessari, non può essere applicata in build se il progetto non genera il file di documentazione XML.

La regola era stata attivata in `.editorconfig` senza il prerequisito, e con i warning trattati come errori la build falliva.

* `GenerateDocumentationFile` impostato su `true` in `Directory.Build.props`.
* `CS1591` disattivato: la generazione del file XML non deve trasformarsi nell'obbligo di commentare ogni membro pubblico, requisito che contraddirebbe il principio di commentare solo dove serve.

### Design Decisions

Scelte imposte dal linguaggio e non coperte dalla Specification 05, approvate prima dell'implementazione.

* Identificatori di sistema come `Guid`, per garantire per costruzione l'indipendenza dal backend richiesta dalla specifica.
* Entità come `sealed record` con proprietà `init` e collezioni `IReadOnlyList`: un NetworkSnapshot rappresenta un istante già trascorso e non deve poter essere modificato dopo la produzione.
* Date come `DateTimeOffset`, per identificare un istante senza ambiguità di fuso orario.
* `ThreatCategory.Unknown` con valore 0, poiché la specifica la designa come categoria di fallback.

---

## Milestone M2.1 — Backend Bootstrap — 2026-08-02

Creazione della struttura permanente della solution.

Nessuna logica applicativa è stata implementata: REST API, Unified Data Model, database, Adapter, servizi, Engine e autenticazione restano da realizzare.

### Added

**Struttura della solution**

Cinque progetti .NET 10 sotto `backend/src/`, radice dei namespace `TivuStream.Pie`.

| Progetto                             | Riferimenti                          |
| ------------------------------------ | ------------------------------------ |
| `TivuStream.Pie.Model`               | nessuno                              |
| `TivuStream.Pie.Core`                | Model                                |
| `TivuStream.Pie.Adapters`            | Model                                |
| `TivuStream.Pie.Adapters.Technitium` | Adapters                             |
| `TivuStream.Pie.Api`                 | Core, Adapters, Adapters.Technitium  |

L'assenza di un riferimento da `Core` verso `Adapters` e `Api` rende l'isolamento del Core verificabile in fase di compilazione.

**`Directory.Build.props`**

Configurazione di build condivisa: `net10.0`, nullable abilitato, warning trattati come errori, analizzatori .NET al livello `Recommended`, applicazione delle regole di stile in build, audit delle dipendenze.

Metadati di copyright omessi intenzionalmente: la License Specification rinvia la decisione alla prima Beta pubblica.

**File `.gitkeep`**

Aggiunti in `frontend/`, `installer/`, `examples/`, `resources/`, `scripts/`, `tools/` e `backend/tests/`.

Git non versiona le directory vuote: senza questi file la struttura ufficiale non sarebbe presente dopo un clone.

**CONTRIBUTING.md**

Il file era presente ma vuoto.

Il contenuto è derivato da `AI_DEVELOPMENT_GUIDE.md`, dal Glossary e dalla License Specification.

Non introduce requisiti nuovi.

### Changed

**11 - Backend Specification**

Documentate la struttura della solution, le responsabilità dei singoli progetti e le regole di riferimento tra progetti.

---

## Documentation Release 1.0.2 — 2026-08-02

Preparazione del repository e allineamento del README alla documentazione ufficiale.

Nessun codice applicativo è stato prodotto in questa release.

### Added

**`.gitattributes`**

Il repository era privo di regole sulle terminazioni di riga: i file erano committati con LF e presenti nel working tree con CRLF, per cui ogni documento risultava interamente riscritto a ogni commit.

In un progetto Documentation First questo rendeva illeggibili proprio i diff delle Specification.

* Normalizzazione a LF per tutti i file di testo.
* Eccezioni CRLF per `*.sln`, `*.ps1`, `*.bat` e `*.cmd`.
* Classificazione dei file binari, inclusi i database SQLite.
* Driver di diff `csharp` per i file `*.cs`.

**`.editorconfig`**

Il file era presente ma vuoto.

* Convenzioni di indentazione per C#, TypeScript, Vue, JSON, YAML, XML e Markdown.
* Regole di stile C# orientate alla leggibilità, come richiesto dai Coding Principles.
* Convenzioni di denominazione: PascalCase per tipi e membri, camelCase per parametri e variabili locali, prefisso `_` per i campi privati, prefisso `I` per le interfacce.

**`.gitignore`**

Il file era presente ma vuoto.

* Esclusione degli artefatti di build .NET e Node.
* Esclusione dei database SQLite locali, che contengono dati di rete dell'utente e la cui esclusione discende dal principio Privacy First.
* Esclusione di credenziali, file `.env` e configurazioni locali.
* Esclusione di file di ambiente di sviluppo e di sistema operativo.

### Changed

**README.md**

Il documento descriveva un'architettura difforme da quella ufficiale.

* Diagramma architetturale corretto con i livelli Adapter, Unified Data Model e REST API, in precedenza assenti.
* Aggiunto il riferimento ad Acquisition Flow e Query Flow.
* Rimosso il termine *Privacy Score*, vietato dal Glossary.
* Rimosso il componente *Data Normalizer*, inesistente in ogni Specification.
* Nomenclatura dei moduli allineata al suffisso Engine.
* Stack tecnologico allineato a quello ufficiale: ASP.NET Core, C#, SQLite, Vue 3, TypeScript, Pinia, Vite.
* Roadmap allineata alle nove milestone della Roadmap Specification, in precedenza descritte come sette fasi differenti.
* Stato del progetto e sezione Licenza allineati alle rispettive Specification.

**Versionamento della documentazione**

Il puntatore alla Documentation Release era rimasto a 1.0.0 nonostante l'aggiornamento delle singole Specification.

* Chiarita in Roadmap Specification la distinzione tra Documentation Release e versione del singolo documento.
* Aggiornati i riferimenti in `PROJECT_CONTEXT.md`, README e Roadmap Specification.

Documenti aggiornati: README.md, PROJECT_CONTEXT.md, 13.

---

## Documentation Release 1.0.1 — 2026-08-02

Risoluzione delle inconsistenze architetturali rilevate durante la revisione precedente all'inizio dello sviluppo.

Nessun codice è stato prodotto in questa release.

### Changed

**Acquisition Flow e Query Flow**

Il flusso dei dati era descritto in modo contraddittorio: le Specification 02 e 03 indicavano `Adapter → Unified Data Model → Core`, mentre le Specification 06, 09 e 11 indicavano `Core → Adapter → Data Source`.

I due percorsi sono stati riconosciuti come flussi distinti e documentati separatamente.

* L'**Acquisition Flow** è innescato dallo Scheduler e orchestrato dall'Adapter Manager.
* Il **Query Flow** serve le richieste del Frontend e non raggiunge mai le Data Sources.
* Il Core è dichiarato componente passivo: non orchestra l'acquisizione e non conosce l'origine dei dati.
* Non sono stati introdotti componenti nuovi.

Specification aggiornate: 00, 02, 03, 06, 09, 11.

---

**Network Privacy & Security Score**

La Specification 07 definiva sei aree di valutazione ma ne mostrava cinque nell'esempio di breakdown, lasciando i pesi indefiniti.

La Specification 05 modellava il punteggio con tre scalari non riconducibili alle aree documentate.

* Introdotta la tabella **Score Weights** con sei aree e pesi la cui somma è 100.
* Corretto l'esempio di breakdown, ora comprensivo di tutte le sei aree.
* Sostituite in 05 le proprietà `securityScore`, `privacyScore` e `protectionScore` con `overallScore`, `status`, `trend`, `algorithmVersion` e `breakdown`.
* Introdotta l'entità **ScoreComponent**, che rende ogni variazione del punteggio spiegabile come richiesto dal principio di Transparency.
* Rimossa la proprietà `privacyScore`, in violazione del Glossary.

Specification aggiornate: 05, 07.

---

**Nomenclatura dei moduli del Core**

Gli stessi moduli comparivano con nomi differenti in Specification diverse, in violazione della regola che vieta i sinonimi.

* Adottato il suffisso **Engine** per tutti i moduli del Core: Threat Engine, Device Engine, NPSS Engine, Alert Engine, Recommendation Engine.
* Aggiunta al Glossary la voce **NPSS Engine**, in precedenza assente.
* *Threat Intelligence* e *Device Intelligence* spostati in **Terms to Avoid** come nomi di modulo.
* *Threat Intelligence* resta valido come nome della materia trattata; il titolo della Specification 08 rimane invariato.
* Allineati i riferimenti ai moduli nel corpo della Specification 08 e nella Phase 4 della Roadmap.

Specification aggiornate: 00, 02, 08, 13.

---

### Removed

**Cartella `engine/`**

Residuo di un'analisi architetturale precedente, mai utilizzata e rimasta in root per dimenticanza.

Il Core risiede in `backend/` come progetto autonomo, isolato dagli Adapter e dall'host delle REST API in modo che il vincolo sia verificabile in fase di compilazione.

Documenti aggiornati: PROJECT_CONTEXT.md, 11.

---

## Documentation Release 1.0.0 — 2026-08-02

Baseline ufficiale del progetto.

### Added

* Documentazione completa del progetto in `docs/`, Specification da 00 a 14.
* Definizione dell'architettura, del Unified Data Model e delle REST API.
* Milestone M1 completata. Sviluppo non iniziato.
