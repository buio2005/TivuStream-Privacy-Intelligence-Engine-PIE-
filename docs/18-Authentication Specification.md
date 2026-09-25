# 18 - Authentication

**Project:** TivuStream Privacy Intelligence Engine (PIE)

**Document:** Authentication Specification

**Version:** 1.2.0

**Status:** Approved

**Last Updated:** 2026-09-25

---

# Purpose

Questa specifica definisce **chi può interrogare il Privacy Intelligence Engine e che cosa può leggere**.

Chiude il debito dichiarato più grave del progetto: oggi l'API risponde a chiunque la raggiunga. È sicura soltanto perché ascolta sul solo indirizzo di loopback, e questa è una circostanza, non una misura.

La API Specification si limitava a dire che «il sistema supporta autenticazione centralizzata» e che «le modalità implementative vengono definite durante lo sviluppo del backend». Le modalità non sono mai state definite. Questo documento le definisce **prima** del codice, perché è una decisione la cui correzione, una volta che esistono utenti e dati, costa cara.

---

# Why The Stakes Are Higher Than For A Dashboard

Ciò che PIE conserva e mostra è, per costruzione, aggregato: non esiste il registro delle singole interrogazioni. Ma resta sensibile.

* **L'attività per dispositivo** dice quali domini ciascun dispositivo della casa ha contattato e quante volte. È l'informazione più delicata che il sistema espone, e la Persistence Specification la chiama «la cronologia di navigazione di ogni dispositivo» nella sua forma grezza.
* **Lo stato della configurazione** dice che cosa la rete non protegge: DNSSEC spento, nessun trasporto cifrato, nessuna lista di filtro. Per chi volesse attaccarla è un elenco.
* **Il punteggio** e le sue ragioni descrivono la rete in modo utile a chiunque la osservi.

Un'API aperta su una rete domestica, dove convivono dispositivi che nessuno ha esaminato, non è un rischio teorico.

---

# Threat Model

## Avversari considerati

| Avversario | Esempio | Che cosa si vuole impedire |
| --- | --- | --- |
| Un altro dispositivo della rete | Un apparecchio compromesso, un ospite collegato al Wi-Fi | Leggere dati o configurazione senza essere l'utente |
| Una persona di casa senza diritti di amministrazione | Un familiare | Leggere l'attività di dispositivi altrui |
| Una pagina web aperta dal browser dell'amministratore | Un sito malevolo che tenta richieste verso l'indirizzo locale, anche tramite DNS rebinding | Usare il browser dell'utente come tramite |
| Chi indovina le credenziali | Tentativi ripetuti sul modulo di accesso | Entrare per tentativi |
| Chi ottiene il file del database | Una copia di riserva lasciata in giro | Recuperare le password degli account |

## Non considerati, e dichiarati

* **Un host compromesso.** Chi ha i privilegi dell'utente che esegue PIE ha già il database, i file di configurazione e la memoria.
* **La cifratura a riposo del database.** Contiene dati di rete aggregati. Cifrarla richiede una gestione delle chiavi che il progetto non ha, e la protezione che si otterrebbe senza di essa sarebbe apparente.
* **L'autenticazione a più fattori.** Vedi Known Limits.

---

# Principles

Sei regole. Ogni requisito seguente ne discende.

1. **Nulla risponde senza un'identità.** L'accesso è negato per impostazione predefinita. Un endpoint nuovo è protetto senza che nessuno debba ricordarlo.
2. **L'assenza di protezione non è uno stato silenzioso.** Non esiste un'impostazione che spegne l'autenticazione. Una configurazione che la disattivasse sarebbe la prima che qualcuno lascia attiva per comodità.
3. **Un segreto non viene mai conservato in chiaro né scritto in un registro.** Vale per password, identificativi di sessione e codice di configurazione iniziale.
4. **Un rifiuto non insegna nulla a chi lo riceve.** Un account inesistente e una password sbagliata producono la stessa risposta.
5. **Local First.** Nessun servizio di identità esterno, nessun invio di messaggi, nessuna telemetria. Il recupero di un accesso perso passa dall'accesso alla macchina.
6. **Ciò che viene trattenuto per ruolo viene dichiarato.** Una risposta che omette un dato perché chi chiede non ne ha diritto lo dice. Un elenco vuoto per mancanza di diritto e un elenco vuoto perché nulla è accaduto sono due affermazioni diverse, e presentarle allo stesso modo è un'informazione falsa: è la regola Absent Versus Unmeasurable della Network Privacy Specification, applicata ai permessi.

---

# Scope

**Comprende.**

* account locali con ruolo;
* credenziali, sessioni e recupero;
* configurazione iniziale della prima installazione;
* verifica dei permessi su ogni endpoint;
* schermate di accesso, configurazione iniziale e gestione degli account;
* contromisure di base contro tentativi ripetuti e richieste da altri siti.

**Non comprende.**

* **Il certificato e la configurazione HTTPS.** Sono oggetto di una specifica separata (Transport Security), richiesta dai criteri della Beta. Questa specifica fissa soltanto ciò che dipende dal trasporto: le credenziali non viaggiano in chiaro fuori dal loopback.
* **Token per servizi e automazioni.** La Backend Specification cita «utenti e servizi autorizzati», ma oggi non esiste alcun servizio che interroghi l'API: il solo cliente è il Frontend. Prevederli ora sarebbe progettare per un bisogno che non c'è. Vedi Decisions Taken, D5.
* **Autenticazione verso le Data Source.** Resta come nella Technitium Integration Specification: un API Token non scadente, in `appsettings.Local.json`. L'autenticazione degli utenti è indipendente da quella delle sorgenti.

---

# Accounts And Roles

Un **account** è una persona che usa PIE. Appartiene al Backend e non al Unified Data Model, che descrive la rete osservata e non chi la osserva.

| Proprietà | Regola |
| --- | --- |
| `username` | Da 3 a 32 caratteri fra lettere minuscole, cifre, punto, trattino e trattino basso. Il confronto non distingue maiuscole e minuscole |
| `role` | `Administrator` oppure `Viewer` |
| `enabled` | Un account disattivato non può accedere e le sue sessioni cadono |
| `passwordChangeRequired` | Vero alla creazione e dopo una reimpostazione da parte di un amministratore |

## Ruoli

| Ruolo | Può |
| --- | --- |
| `Administrator` | Tutto: leggere ogni dato, gestire gli account |
| `Viewer` | Leggere i dati **aggregati**: punteggio, statistiche, domini, stato di sistema. Non legge i dati che identificano un singolo dispositivo né la sua attività |

La distinzione ha una sola ragione, dichiarata nei principi: l'attività per dispositivo è il dato più sensibile, e in una casa chi guarda il quadro d'insieme non deve poter vedere che cosa ha fatto ciascuno. Il costo è dichiarato insieme al vantaggio, come richiede l'Installation Specification: un familiare `Viewer` vede che un dominio di tracciamento è stato contattato, non da quale dispositivo.

## Vincolo

Esiste sempre almeno un `Administrator` attivo. L'ultimo non può essere disattivato, rimosso o retrocesso. Il recupero previsto (vedi Recovery) esiste per i casi che sfuggono, non per rendere questo vincolo negoziabile.

---

# Credentials

## Password

* Lunghezza da **12 a 128** caratteri.
* **Nessuna regola di composizione.** Imporre maiuscole, cifre e simboli produce password prevedibili e più difficili da ricordare, non più sicure. Conta la lunghezza.
* Non può coincidere con il nome utente.
* Viene normalizzata in forma Unicode NFKC prima di ogni uso, così che la stessa password scritta su tastiere diverse dia lo stesso risultato.

## Conservazione

Della password viene conservato soltanto un **hash con sale**, mai la password né qualcosa da cui si possa ricavarla in modo diretto.

| Parametro | Valore |
| --- | --- |
| Algoritmo | PBKDF2 con HMAC-SHA-512 |
| Iterazioni | 210.000 |
| Sale | 16 byte casuali per password, da un generatore crittografico |
| Uscita | 64 byte |
| Formato registrato | Algoritmo, iterazioni, sale e hash insieme, così che i parametri possano crescere |

Ad ogni accesso riuscito, se i parametri registrati sono inferiori a quelli in vigore, la password viene ricalcolata con i nuovi.

Il confronto avviene a tempo costante. Per un account inesistente viene eseguito comunque un calcolo su un valore fittizio, così che il tempo di risposta non riveli se il nome esiste.

L'algoritmo è quello che la libreria di base di .NET offre senza dipendenze aggiuntive. Argon2id resiste meglio a chi dispone di schede grafiche, ma richiede una libreria di terzi. Vedi D3.

---

# Sessions

Dopo un accesso riuscito il Backend crea una **sessione** e la comunica al browser con un cookie.

| Aspetto | Regola |
| --- | --- |
| Identificativo | 32 byte casuali da un generatore crittografico, mai derivati da dati dell'account |
| Cookie | `HttpOnly`, `SameSite=Strict`, percorso `/api`, senza attributo `Domain`. `Secure` quando la connessione è cifrata |
| Conservazione | Nel database si conserva soltanto l'**hash SHA-256** dell'identificativo. Chi leggesse il database non potrebbe usare una sessione |
| Inattività | Scade dopo 8 ore senza richieste |
| Durata massima | Scade in ogni caso dopo 14 giorni |
| Rinnovo | L'identificativo cambia ad ogni accesso |

Il browser non maneggia mai la credenziale né un token in codice eseguibile dalla pagina: il cookie `HttpOnly` non è leggibile dallo script.

Una sessione cade quando: l'utente esce, l'account viene disattivato o rimosso, la password cambia (tutte le altre sessioni dell'account), scade.

Il ruolo non è copiato nella sessione: viene letto dall'account ad ogni richiesta. Una retrocessione ha effetto immediato.

---

# First Run

Una nuova installazione non ha account. Chi raggiunge per primo l'indirizzo non deve poterne diventare l'amministratore semplicemente arrivando prima del proprietario.

1. All'avvio, se non esiste alcun account, PIE entra nello stato **`SetupRequired`** e genera un **codice di configurazione** casuale di 12 caratteri, mostrato in gruppi di quattro.
2. Il codice viene **scritto sull'output standard, con una scrittura diretta**, e non passa dal sistema di registrazione. Non raggiunge quindi file di log né raccoglitori che quel sistema alimenti.
3. Nello stato `SetupRequired` ogni richiesta, tranne la configurazione iniziale, riceve `401` con codice `SetupRequired`.
4. `POST /api/v1/setup` riceve codice, nome utente e password, e crea il primo `Administrator`. Ha successo una volta sola: l'operazione è atomica, e una seconda richiesta contemporanea fallisce.

   Il codice si controlla per primo. Un nome o una password non conformi vengono rifiutati **senza consumare il codice**: chi sbaglia a digitare non deve doverne chiedere un altro riavviando il servizio. Il codice si accetta in qualunque combinazione di maiuscole e minuscole, con o senza trattini e spazi.
5. Il codice non viene mai conservato: esiste in memoria in forma di hash e si rigenera ad ogni avvio finché non viene usato. I tentativi sbagliati sottostanno agli stessi limiti dell'accesso.

La prova di possesso è dunque l'**accesso alla macchina**: chi legge l'output standard del processo è, per definizione, chi amministra l'host. In un container è l'output di `docker logs`, e questo è un limite dichiarato.

---

# Recovery

Non esiste un «ho dimenticato la password» via rete e non esiste un messaggio di posta: sono servizi esterni, e un canale di recupero è un secondo modo di entrare.

Il recupero è **un comando eseguito sulla macchina che ospita PIE**, dallo stesso eseguibile, che:

* chiede la nuova password sul terminale;
* la imposta sull'account indicato e vi rimette `passwordChangeRequired`;
* fa cadere tutte le sessioni di quell'account;
* può, se non esiste alcun `Administrator` attivo, riattivarne o crearne uno.

Le regole sul nome indicato:

| Situazione | Effetto |
| --- | --- |
| Esiste un `Administrator` attivo e il nome è di un account | La password di quell'account viene reimpostata, qualunque ne sia il ruolo |
| Esiste un `Administrator` attivo e il nome **non** è di un account | Rifiutato. Un errore di battitura non deve creare un account |
| Nessun `Administrator` è attivo e il nome è di un `Administrator` disattivato | Viene riattivato e la sua password reimpostata |
| Nessun `Administrator` è attivo e il nome non esiste | Viene creato un `Administrator` |
| Nessun `Administrator` è attivo e il nome è di un `Viewer` | Rifiutato. Il comando ripristina un amministratore, non ne promuove uno |

La password nuova è soggetta alle regole di sempre.

Chi può eseguirlo ha già accesso al database. Il recupero non concede nulla che l'accesso alla macchina non conceda già. La forma esatta del comando è un dettaglio d'implementazione.

---

# Transport

Le credenziali (`setup`, `login`, cambio password) sono accettate **soltanto su una connessione cifrata oppure quando il client è sul loopback**. Altrimenti la risposta è `403` con codice `TransportNotSecure`.

Il criterio è l'indirizzo remoto della connessione, non l'intestazione `Host`, che chi scrive la richiesta controlla.

Il cookie di sessione porta `Secure` quando la connessione è cifrata. Sul loopback in chiaro non lo porta: alcuni browser lo rifiutano su un indirizzo non cifrato anche locale.

L'impostazione `AllowedHosts`, oggi `*`, deve elencare i nomi con cui l'installazione è raggiungibile. Con `*` un sito malevolo che rimappi il proprio nome sull'indirizzo locale (DNS rebinding) raggiunge l'API con il browser dell'amministratore.

Su una connessione cifrata le risposte portano `Strict-Transport-Security`.

Come si ottiene la connessione cifrata, e come si tratta un proxy che la termina davanti a PIE, appartiene alla specifica Transport Security.

---

# Attempts

Contro chi prova password a ripetizione.

| Contatore | Soglia | Effetto |
| --- | --- | --- |
| Per indirizzo di origine | 5 fallimenti | Rifiuto `429` con `TooManyAttempts` e `Retry-After` |
| Per account | 10 fallimenti | Lo stesso rifiuto |

Il ritardo parte da 30 secondi e **raddoppia** ad ogni ulteriore fallimento, fino a un massimo di **15 minuti**. Un accesso riuscito azzera il contatore dell'account. I contatori vivono in memoria e si azzerano al riavvio.

Non esiste un blocco permanente: renderebbe possibile a chiunque chiudere fuori il proprietario. Il tetto di 15 minuti è la misura di quanto un avversario può rallentare l'amministratore legittimo, ed è dichiarato come compromesso.

Gli indirizzi di origine servono ai contatori e non vengono registrati in alcun luogo.

---

# API Contract

Tutte le risposte usano la struttura comune della API Specification.

| Metodo e percorso | Chi | Effetto |
| --- | --- | --- |
| `POST /api/v1/setup` | Nessuno, solo in `SetupRequired` | Crea il primo `Administrator` e apre la sessione |
| `POST /api/v1/auth/login` | Nessuno | Apre la sessione |
| `POST /api/v1/auth/logout` | Sessione | Chiude la sessione |
| `GET /api/v1/auth/session` | Sessione | Restituisce nome utente, ruolo, scadenza e `passwordChangeRequired` |
| `POST /api/v1/auth/password` | Sessione | Cambia la propria password |
| `GET /api/v1/accounts` | `Administrator` | Elenca gli account |
| `POST /api/v1/accounts` | `Administrator` | Crea un account con password iniziale |
| `PATCH /api/v1/accounts/{username}` | `Administrator` | Cambia ruolo, attivazione, o reimposta la password |
| `DELETE /api/v1/accounts/{username}` | `Administrator` | Rimuove l'account |

`setup` e `login` sono gli **unici** endpoint raggiungibili senza credenziali, perché sono il modo di ottenerle. Nessuno dei due restituisce alcun dato sulla rete o sul sistema.

## Errori

| Stato | Codice | Quando |
| --- | --- | --- |
| 401 | `SetupRequired` | Nessun account esiste ancora |
| 401 | `AuthenticationRequired` | Sessione assente, non riconosciuta o scaduta |
| 401 | `AuthenticationFailed` | Accesso rifiutato. Identico per nome inesistente, password errata e account disattivato |
| 403 | `Forbidden` | Il ruolo non basta |
| 403 | `PasswordChangeRequired` | La password deve essere cambiata prima di ogni altra cosa |
| 403 | `TransportNotSecure` | Credenziali su connessione in chiaro non locale |
| 403 | `OriginNotAllowed` | Richiesta che modifica dati con `Origin` diverso dall'indirizzo del servizio |
| 409 | `LastAdministrator` | L'operazione lascerebbe l'installazione senza amministratore |
| 409 | `AccountExists` | Nome già usato |
| 401 | `SetupCodeRejected` | Il codice di configurazione iniziale è assente o errato. Identico nei due casi |
| 409 | `SetupAlreadyCompleted` | Esiste già un account: la configurazione iniziale non è più disponibile |
| 422 | `UsernameRejected` | Nome non conforme |
| 422 | `PasswordRejected` | Password non conforme, con il motivo: `TooShort`, `TooLong`, `EqualsUsername`, `Unchanged` |
| 422 | `RoleRejected` | Ruolo diverso da `Administrator` e `Viewer`, o assente nella creazione di un account |
| 403 | `CurrentPasswordRejected` | Nel cambio della propria password, la password attuale non è quella giusta |
| 404 | `AccountNotFound` | Nessun account ha il nome indicato nel percorso |
| 429 | `TooManyAttempts` | Vedi Attempts |

Il motivo di `PasswordRejected` viaggia nel campo `reason` della risposta di errore e non nel testo: l'interfaccia lo traduce nella lingua di chi legge.

Le risposte di rifiuto non riportano mai una `WWW-Authenticate` di tipo `Basic`: farebbe comparire la finestra di credenziali del browser al posto della schermata dell'applicazione.

`AuthenticationRequired` non distingue una sessione scaduta da una mai aperta: non è un'informazione che il Backend debba fornire. Il Frontend sa da sé se l'utente era entrato.

Mentre `passwordChangeRequired` è vero, ogni endpoint tranne `auth/session`, `auth/password` e `auth/logout` risponde `PasswordChangeRequired`. Vale anche dove il ruolo non basterebbe: la password da cambiare viene prima di ogni altra cosa, e dire `Forbidden` a chi non può ancora fare nulla sarebbe un'indicazione inutile.

## Richieste e risposte

| Endpoint | Corpo | Risposta riuscita |
| --- | --- | --- |
| `POST /auth/password` | `currentPassword`, `newPassword` | `200`, la sessione come la descrive `auth/session` |
| `GET /accounts` | — | `200`, l'elenco degli account |
| `POST /accounts` | `username`, `role`, `password` | `201`, l'account creato |
| `PATCH /accounts/{username}` | `role`, `enabled`, `password`, ciascuno facoltativo | `200`, l'account come è dopo la modifica |
| `DELETE /accounts/{username}` | — | `200` |

Un account, nelle risposte, è `username`, `role`, `enabled`, `passwordChangeRequired` e `createdAt`. L'hash della password non esce mai dal Backend.

## Cambio della propria password

* Richiede la **password attuale**. Una sessione lasciata aperta su un browser non deve bastare a impadronirsi dell'account.
* Una password attuale sbagliata risponde `403 CurrentPasswordRejected`, **non** `401`: il Frontend legge un `401` come sessione terminata, e la sessione è invece valida.
* La nuova password segue le regole di sempre e in più **non può coincidere con quella attuale** (`Unchanged`). Senza questa regola l'obbligo di cambiarla dopo una reimpostazione si aggirerebbe rimettendo la stessa, e l'amministratore continuerebbe a conoscerla.
* A cambio riuscito `passwordChangeRequired` diventa falso, la sessione da cui è stato fatto resta aperta, **tutte le altre** dell'account cadono.

## Gestione degli account

* Un account creato da un amministratore nasce con `passwordChangeRequired` vero.
* Il nome segue le regole di Accounts And Roles, e il ruolo è scritto per nome: `Administrator` o `Viewer`.
* In `PATCH` i campi presenti si applicano **tutti o nessuno**. Un corpo senza campi non cambia nulla e restituisce l'account.
* `password` in `PATCH` è una reimpostazione: `passwordChangeRequired` torna vero e **tutte** le sessioni dell'account cadono, anche quella di chi la esegue se l'account è il proprio.
* `enabled: false` fa cadere tutte le sessioni dell'account.
* Un amministratore agisce sul proprio account come su quello di un altro. Il solo limite è il vincolo dell'ultimo amministratore.
* Il vincolo è verificato **nella stessa transazione** della modifica. Due amministratori che si disattivano a vicenda nello stesso istante non devono poter lasciare l'installazione senza nessuno dei due.

## Intestazioni

* Ogni risposta autenticata porta `Cache-Control: no-store`: contiene dati di rete e non deve restare nella cache del browser né di un intermediario.
* Le richieste che modificano dati verificano `Origin`, oltre a `SameSite=Strict`.
* Nessuna pagina servita da PIE può essere inserita in un frame di un altro sito (`frame-ancestors 'none'`).

---

# Authorization

Ogni endpoint **dichiara il ruolo minimo** che richiede. Un endpoint che non lo dichiara richiede `Administrator`: dimenticare una dichiarazione produce un rifiuto, mai un'apertura.

| Endpoint | Ruolo minimo |
| --- | --- |
| `/health` | `Viewer` |
| `/statistics` | `Viewer` |
| `/npss` | `Viewer` |
| `/domains` | `Viewer` |
| `/domains/{domain}` | `Viewer`, con l'attività trattenuta (vedi sotto) |
| `/devices` | `Administrator` |
| Endpoint degli account | `Administrator` |

## Ciò che viene trattenuto è dichiarato

`/domains/{domain}` restituisce oggi `activityAvailable`, un booleano, e l'elenco `activities`. Per un `Viewer` l'elenco sarebbe vuoto, e vuoto per mancanza di diritto sarebbe indistinguibile da vuoto perché nessun dispositivo ha contattato il dominio.

Il campo diventa **`activityAccess`**, con tre valori:

| Valore | Significato |
| --- | --- |
| `Available` | La sorgente offre l'attività e chi chiede può leggerla. `activities` è l'elenco |
| `Unavailable` | La sorgente non offre l'attività. `activities` è vuoto e non significa nulla |
| `Withheld` | La sorgente la offre, ma il ruolo di chi chiede non la comprende. `activities` è vuoto e non significa nulla |

Il Frontend mostra il terzo caso come tale: **non** come assenza di attività e **non** come errore.

---

# Frontend

Il Frontend nasce bilingue: ogni testo che segue esiste in italiano e in inglese nel catalogo.

## Stati

L'applicazione è sempre in uno di questi stati, distinti fra loro:

| Stato | Quando |
| --- | --- |
| `Checking` | All'apertura, mentre chiede se una sessione esiste |
| `SetupRequired` | Il Backend risponde `SetupRequired` |
| `Unauthenticated` | Nessuna sessione |
| `Authenticated` | Sessione valida |
| `PasswordChangeRequired` | Sessione valida, password da cambiare |

Un `401` ricevuto mentre l'utente era `Authenticated` porta a `Unauthenticated` con un messaggio che dice che la sessione è terminata. È diverso dal fallimento di un accesso appena tentato.

## Schermate

Configurazione iniziale, accesso, cambio password, gestione degli account (solo `Administrator`).

## Regole

* **Nessun dato di rete sopravvive all'uscita.** Alla chiusura della sessione gli store svuotano ogni dato letto. Un altro utente dello stesso browser non deve trovare ciò che il precedente vedeva.
* La password non compare mai in un indirizzo, non viene mai messa in `localStorage` né in un'altra memoria persistente. Non esiste «ricordami» oltre la durata della sessione.
* Le tre situazioni **non si confondono**: credenziali sbagliate, motore non raggiungibile, connessione non sicura. Dire «credenziali errate» quando il motore non ha risposto è un'affermazione falsa.

## Messaggi

| Codice | Italiano | Inglese |
| --- | --- | --- |
| `auth.failed` | Nome utente o password non corretti. | Username or password incorrect. |
| `auth.engineUnreachable` | Il motore non ha risposto. Le credenziali non sono state verificate. | The engine did not answer. Your credentials were not checked. |
| `auth.sessionEnded` | La sessione è terminata. Accedi di nuovo. | Your session has ended. Sign in again. |
| `auth.tooManyAttempts` | Troppi tentativi. Riprova fra {seconds} secondi. | Too many attempts. Try again in {seconds} seconds. |
| `auth.notSecure` | Le credenziali non possono essere inviate su una connessione non cifrata. Apri l'indirizzo in HTTPS. | Credentials cannot be sent over an unencrypted connection. Open the address over HTTPS. |
| `domains.activityWithheld` | L'attività per dispositivo è visibile solo agli amministratori. Questo non dice se qualche dispositivo abbia contattato il dominio. | Activity per device is visible to administrators only. This says nothing about whether any device contacted the domain. |

`auth.failed` non dice se il nome esiste, né se l'account è disattivato. `auth.engineUnreachable` esiste perché il motore che non risponde e la password sbagliata sono due fatti diversi. L'ultimo messaggio riprende la regola dei principi: il rifiuto non deve poter passare per un'assenza.

I motivi di `PasswordRejected` e gli altri errori hanno una voce ciascuno nel catalogo, con la stessa parità fra le due lingue che il catalogo già verifica.

---

# Persistence

Aggiunge alla Persistence Specification due entità logiche, interne al Backend:

* **account**: nome utente, ruolo, attivo, hash della password con i parametri, `passwordChangeRequired`, istante di creazione;
* **sessione**: hash dell'identificativo, account, creazione, ultimo uso, scadenza.

Le sessioni scadute vengono eliminate. Le migrazioni seguono le regole già in vigore.

Si aggiungono a What Is Never Persisted: **password in chiaro, identificativi di sessione in chiaro, codice di configurazione iniziale**.

---

# Logging

I registri portano conteggi e a esiti, mai dati di rete e mai segreti.

| Evento | Contenuto |
| --- | --- |
| Configurazione iniziale completata | L'evento e il nome dell'amministratore creato |
| Accesso riuscito | Il nome dell'account |
| Accesso fallito | L'evento e un **conteggio**. Non il nome tentato: potrebbe essere una password digitata nel campo sbagliato |
| Operazione sugli account | Chi l'ha eseguita, su quale account, quale operazione |
| Limite raggiunto | L'evento, non l'origine |

Non compaiono mai: password, identificativi di sessione, codice di configurazione, indirizzi di origine.

---

# Verification

Ogni impegno è **verificabile**, e ciascuno corrisponde ad almeno una prova, come richiede il MASTER_PROMPT.

## Backend

| # | Impegno | Prova |
| --- | --- | --- |
| V1 | Nulla risponde senza identità | Si enumerano **tutti** gli endpoint dell'host. Senza sessione ciascuno risponde `401`, tranne `setup` e `login`, che rispondono soltanto secondo questa specifica |
| V2 | Il rifiuto è la predefinita | Si registra un endpoint senza dichiarazione di ruolo: richiede `Administrator` |
| V3 | Il rifiuto non insegna nulla | Nome inesistente, password errata e account disattivato producono la stessa risposta |
| V4 | Segreti non conservati in chiaro | Nel database non compaiono né la password né l'identificativo di sessione |
| V5 | Una sessione cade quando deve | Dopo uscita, disattivazione, cambio password e scadenza, l'identificativo vecchio è rifiutato |
| V6 | Ciò che è trattenuto è dichiarato | Un `Viewer` non legge `/devices` e ottiene `Withheld`, non un elenco vuoto, su `/domains/{domain}` |
| V7 | Esiste sempre un amministratore | L'ultimo non si può disattivare, rimuovere né retrocedere |
| V8 | I tentativi ripetuti rallentano | Dopo la soglia si ottiene `429`; un accesso riuscito azzera; il ritardo ha il tetto dichiarato |
| V9 | Credenziali solo su canale idoneo | Su connessione in chiaro non locale `setup`, `login` e cambio password rispondono `TransportNotSecure` |
| V10 | La configurazione iniziale si fa una volta | Senza codice o con codice errato fallisce; con il codice giusto ha successo; la seconda richiesta fallisce |
| V11 | Nessun segreto in uscita | Nessuna risposta e nessuna riga di registro contiene una password, un identificativo di sessione o il codice |
| V12 | Nessuna cache | Le risposte autenticate portano `Cache-Control: no-store` |
| V13 | Richieste da altri siti | Una richiesta che modifica dati con `Origin` estraneo è rifiutata |
| V14 | Host consentiti | Una richiesta con `Host` non elencato è rifiutata |
| V15 | Parametri aggiornati | Un accesso riuscito con parametri di hash vecchi li ricalcola |

La prova esistente «nessuna risposta contiene la credenziale della sorgente dati» resta e si estende ai nuovi segreti.

## Frontend

| # | Impegno | Prova |
| --- | --- | --- |
| F1 | Stati distinti | Credenziali sbagliate, motore non raggiungibile, sessione terminata e connessione non sicura producono quattro messaggi diversi |
| F2 | Nulla sopravvive all'uscita | Dopo l'uscita gli store non contengono dati di rete |
| F3 | Il messaggio di rifiuto non distingue | Lo stesso testo per ogni motivo di `AuthenticationFailed` |
| F4 | Il trattenuto non è un'assenza | `Withheld` non si presenta né come elenco vuoto né come errore |
| F5 | Parità del catalogo | Tutte le voci nuove esistono in entrambe le lingue con gli stessi segnaposto |

---

# Implementation Milestones

Una funzionalità di questa portata non si realizza in un solo passo. Ogni milestone si può verificare da sola.

| # | Contenuto | Prove |
| --- | --- | --- |
| A1 | Account, hash, persistenza, migrazione | V4, V15 |
| A2 | Sessioni, accesso e uscita, rifiuto predefinito, ruolo per endpoint | V1, V2, V3, V5, V12 |
| A3 | Configurazione iniziale e recupero | V10 |
| A4 | Ruoli, gestione degli account, ciò che viene trattenuto | V6, V7 |
| A5 | Tentativi, trasporto, `Origin`, `Host` | V8, V9, V13, V14 |
| A6 | Frontend: stati, schermate, catalogo | F1–F5 |
| A7 | Registrazione senza segreti | V11 |

Le prove dell'API esistenti dovranno ottenere una sessione: la loro fabbrica di test dovrà crearne una.

---

# Consequences For Other Documents

Applicate nella Documentation Release 1.6.0.

| Documento | Modifica |
| --- | --- |
| 06 - API | La sezione Authentication rimanda a questa specifica; endpoint, codici di errore; `activityAvailable` diventa `activityAccess` |
| 11 - Backend | Le sezioni Authentication e Authorization rimandano a questa specifica |
| 12 - Installation | La configurazione iniziale sostituisce la voce «autenticazione»; il recupero è descritto |
| 10 - Frontend | Schermate e stati; il punto sulla sicurezza |
| 16 - Persistence | Le due entità; What Is Never Persisted |
| 00 - Glossary | Account, Role, Session, Setup Code |
| 13 - Roadmap | Vedi sotto |
| README, CLAUDE.md | Il debito sull'autenticazione esce dall'elenco quando l'ultima milestone è chiusa |

## Incoerenza corretta nella Roadmap

Il criterio di Beta dice «**Nessun endpoint** risponde senza credenziali valide». Preso alla lettera è incompatibile con l'esistenza di un modulo di accesso, che per definizione risponde a chi credenziali non ne ha ancora.

Riformulazione adottata: *nessun endpoint che restituisce dati sulla rete o sul sistema risponde senza credenziali valide; gli unici raggiungibili senza sono quelli che le stabiliscono, `setup` e `login`, e non restituiscono nulla sulla rete.*

---

# Decisions Taken

Scelte di prodotto, approvate il 2026-09-19 insieme ai messaggi del Frontend. Per ciascuna sono registrate l'alternativa non scelta e ciò che costava, perché chi la riapre un giorno sappia che cosa si è deciso di non fare.

| # | Decisione | Scelta | Alternativa non scelta e suo costo |
| --- | --- | --- | --- |
| **D1** | Modello di accesso | **Account locali con password e sessione** | *Un solo segreto condiviso*, in un file di configurazione: più semplice da realizzare, ma nessuna distinzione fra persone, nessun modo di revocare l'accesso a uno soltanto, e il segreto sta in chiaro in un file |
| **D2** | Ruoli e visibilità per dispositivo | **Due ruoli**; il `Viewer` non vede i dati per dispositivo | *Un solo ruolo*: meno stati e meno schermate, ma chiunque acceda vede l'attività di ciascun dispositivo |
| **D3** | Algoritmo dell'hash | **PBKDF2-SHA-512, senza dipendenze** | *Argon2id*: più resistente a chi usa schede grafiche, ma introduce una libreria di terzi e richiede la tua approvazione come nuova dipendenza |
| **D4** | Recupero | **Solo dalla macchina**, con un comando locale | *Un canale via posta o via rete*: comodo, ma richiede un servizio esterno e apre un secondo modo di entrare |
| **D5** | Token per servizi | **Rimandati** finché non esiste un servizio che li usi | *Prevederli ora*: aggiunge una superficie da proteggere per un bisogno che non c'è |
| **D6** | Valori: sessione 8 ore di inattività e 14 giorni al massimo, ritardo da 30 secondi a 15 minuti, password da 12 caratteri, 210.000 iterazioni | **Quelli scritti qui** | Sono soglie: si possono cambiare senza toccare la struttura. Una sessione più lunga è più comoda e lascia più tempo a un browser dimenticato aperto |
| **D7** | Riformulazione del criterio di Beta nella Roadmap | **Sì**, come proposto | Lasciarlo com'è mantiene un criterio che nessuna implementazione può soddisfare alla lettera |
| **D8** | Nuova password uguale all'attuale, approvata il 2026-09-25 | **Rifiutata**, con `Unchanged` | *Accettarla*: nessuna regola in più, ma l'obbligo di cambio diventa un invito e l'amministratore resta a conoscenza della password che ha assegnato |

---

# Known Limits

Dichiarati, non nascosti.

* **Nessuna autenticazione a più fattori.** Un secondo fattore richiede un canale che il progetto non ha, o uno schema di codici temporanei che è un'altra specifica. La lunghezza minima della password e il rallentamento dei tentativi sono la protezione presente.
* **Il database non è cifrato.** Chi lo copia legge i dati di rete aggregati, non le password.
* **Un host compromesso vanifica tutto.**
* **I contatori dei tentativi si azzerano al riavvio.**
* **Il codice di configurazione iniziale è leggibile da chi legge l'output del processo.** In un container, da chi esegue `docker logs`.
* **L'amministratore conosce la password iniziale** che assegna a un account, fino a quando la persona non la cambia. `passwordChangeRequired` rende il cambio obbligatorio, non elimina la finestra.
* **Solo due ruoli.** Non c'è un permesso per singola sezione né per singolo dispositivo.
* **Nessun registro persistente degli accessi.** Esistono gli eventi nel registro applicativo, non uno storico interrogabile.
* **PBKDF2 resiste meno di Argon2id** a un avversario con hardware dedicato. Vedi D3.
* **Il trasporto cifrato non è definito qui.** Finché la Transport Security Specification non esiste e non è realizzata, l'accesso da altri dispositivi è possibile soltanto dietro un proxy che l'operatore configura a proprio rischio: `login` fuori dal loopback risponde `TransportNotSecure`.

---

# Related Specifications

* 02 - Architecture
* 06 - API
* 09 - Network Privacy
* 10 - Frontend
* 11 - Backend
* 12 - Installation
* 13 - Roadmap
* 16 - Persistence
