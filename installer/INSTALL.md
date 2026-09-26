# Installare TivuStream PIE

PIE legge il tuo server DNS e ti dice che cosa contatta la tua rete di casa. Tutto resta sul tuo computer: nessun dato della tua rete viene mandato altrove.

Questa guida ti accompagna passo per passo. Non serve saper programmare.

**Le domande che PIE ti fa durante l'installazione sono in inglese.** Accanto a ogni passo trovi qui che cosa chiede e che cosa rispondere.

---

## Prima di cominciare

Ti servono tre cose.

1. **Un computer che resta acceso**, con Windows 10 o 11 a 64 bit, oppure Linux a 64 bit con systemd (per esempio Ubuntu o Debian). PIE osserva la rete solo mentre il computer è acceso.
2. **Technitium DNS Server già funzionante** sulla tua rete. PIE non lo installa e non ne cambia le impostazioni: lo legge soltanto.
3. **Un token di Technitium**, cioè una chiave che permette a PIE di leggere i dati. Qui sotto trovi come crearlo.

### Creare il token in Technitium

Il token appartiene a un utente di Technitium creato apposta per PIE, che può solo **guardare**, mai modificare.

1. Apri l'interfaccia di Technitium (di solito `http://indirizzo-del-server:5380`) ed entra come amministratore.
2. Vai in **Administration → Users** e crea un utente, per esempio `pie`.
3. Vai in **Administration → Permissions** e dai a quell'utente il permesso **View** su:
   * **Dashboard**, obbligatorio: senza, PIE non vede nulla;
   * **Settings**, consigliato: permette a PIE di capire se le protezioni del DNS sono attive;
   * **Apps** e **Logs**, solo se vuoi sapere quale dispositivo ha contattato quale dominio (vedi sotto).
4. Esci, rientra come `pie`, apri il menu dell'utente in alto a destra e scegli **Create API Token**. Copia il token: ti servirà tra poco, e Technitium non te lo mostrerà di nuovo.

### Quale dispositivo ha contattato quale dominio

Di serie, Technitium dice a PIE quali domini sono stati contattati e quali dispositivi c'erano, ma non chi ha contattato cosa.

Per saperlo va installata in Technitium l'app **Query Logs (Sqlite)** (menu **Apps → App Store**).

**Pensaci prima di installarla.** Quell'app fa conservare a Technitium ogni singola richiesta di ogni dispositivo, per tutto il tempo previsto dalle sue impostazioni: di fatto la cronologia di navigazione di chi usa la rete. PIE, da parte sua, ne tiene solo i totali ora per ora. Se decidi di installarla, controlla nelle impostazioni dell'app per quanto tempo conserva i dati.

Puoi aggiungerla anche dopo: PIE se ne accorge da solo.

---

## Installare su Windows

1. Estrai il file `tivustream-pie-…-win-x64.zip` in una cartella qualsiasi, per esempio sul Desktop.
2. Apri il menu Start, cerca **PowerShell**, fai clic con il tasto destro e scegli **Esegui come amministratore**.
3. Scrivi `cd` seguito da uno spazio, trascina nella finestra la cartella estratta e premi Invio.
4. Scrivi questo e premi Invio:

   ```text
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```

Lo script fa tutto da solo e si ferma solo per chiederti tre cose:

| Cosa chiede | Che cosa vuol dire | Che cosa rispondere |
| --- | --- | --- |
| `Address of Technitium` | Dove si trova Technitium | L'indirizzo che usi per aprirlo, per esempio `http://192.168.1.10:5380`. Se Technitium è su questo stesso computer, premi solo Invio |
| `API token` | Il token creato prima | Incollalo con il tasto destro e premi Invio. Non lo vedrai comparire: è normale |
| `Name`, poi `New password` e `Repeat the password` | Il tuo account per entrare in PIE | Un nome in minuscolo, per esempio `maria`, e una password di almeno 12 caratteri, due volte |

Dopo il token, PIE ti dice che cosa riesce a leggere e che cosa manca, e come ottenerlo. Se il token non funziona te lo dice con parole semplici e ti chiede se vuoi riprovare (`Try again? [Y/n]`: premi Invio per riprovare).

Alla fine lo script scrive gli indirizzi a cui aprire PIE e un'**impronta**, una lunga sequenza di lettere e numeri. Tienila a portata di mano per il primo accesso da un altro dispositivo.

PIE ora parte da solo ogni volta che accendi il computer, anche se nessuno accede a Windows.

---

## Installare su Linux

1. Copia sul computer il file `tivustream-pie-…-linux-x64.tar.gz` ed estrailo:

   ```text
   tar -xzf tivustream-pie-*-linux-x64.tar.gz
   cd tivustream-pie-*-linux-x64
   ```

2. Avvia l'installazione:

   ```text
   sudo sh install.sh
   ```

Le domande sono le stesse di Windows, nella tabella qui sopra.

Se il computer ha un firewall attivo, alla fine lo script ti dice il comando per aprire PIE alla tua rete: non lo modifica da solo.

Su una distribuzione minima può mancare una libreria (ICU): lo script te lo dice e ti dice quale pacchetto installare.

---

## Aprire PIE

**Sul computer dove l'hai installato:** apri `http://localhost:5000`.

**Dal telefono o da un altro computer di casa:**

1. Apri uno degli indirizzi scritti alla fine dell'installazione. Quello che comincia come quello del tuo router (spesso `https://192.168.…:5443`) è di solito quello giusto.
2. Il browser avvisa che la connessione «non è privata». È normale: il certificato lo ha creato PIE, e nessun browser lo conosce ancora.
3. Apri i dettagli del certificato e confronta l'impronta **SHA-256** con quella scritta da PIE.
   * **Se coincide**, prosegui: stai parlando con il tuo PIE.
   * **Se non coincide, non inserire la password.** Qualcun altro si sta mettendo in mezzo.

L'avviso ricompare, una volta per dispositivo, quando PIE rinnova il certificato (circa una volta l'anno) o quando il router cambia l'indirizzo del computer.

**Se hai perso l'impronta o gli indirizzi**, riscrivili così:

* Windows, in PowerShell come amministratore: `& "C:\Program Files\TivuStream PIE\tivustream-pie.exe" access "--DataDirectory=C:\ProgramData\TivuStream PIE"`
* Linux: `sudo /opt/tivustream-pie/tivustream-pie access --DataDirectory=/var/lib/tivustream-pie`

---

## Cose da sapere

**VPN e proxy.** PIE si collega a Technitium direttamente, senza passare da una VPN o da un proxy impostati sul computer. Se un altro dispositivo non riesce ad aprire PIE mentre usa una VPN, spegnila e riprova.

**Computer usato da più persone.** La cronologia del browser conserva gli indirizzi delle pagine aperte, e alcune pagine di PIE hanno il nome di un dominio nell'indirizzo. Se il computer è condiviso, usa per PIE un profilo del browser dedicato.

**Quanto a lungo PIE conserva i dati.** Ora per ora per 30 giorni, poi giorno per giorno fino a 12 mesi, poi mese per mese fino a 5 anni, senza più il legame fra dispositivo e dominio. Oltre, niente. Ciò che viene cancellato è cancellato davvero.

**Dove stanno i tuoi dati.**

| | Windows | Linux |
| --- | --- | --- |
| Programma | `C:\Program Files\TivuStream PIE` | `/opt/tivustream-pie` |
| Dati, impostazioni, token | `C:\ProgramData\TivuStream PIE` | `/var/lib/tivustream-pie` |

La cartella dei dati può essere aperta solo da PIE e dagli amministratori del computer.

**Copie di sicurezza.** Quando un aggiornamento cambia la struttura del database, PIE ne fa prima una copia nella cartella dei dati (`pie.db.schema-…bak`). Le copie non si cancellano da sole e contengono tutto il dettaglio di quel momento: quando l'aggiornamento ti sembra riuscito, puoi eliminarle.

---

## Aggiornare

Estrai il pacchetto della nuova versione e lancia lo script di installazione esattamente come la prima volta. Lo script si accorge che PIE è già installato, sostituisce il programma e lo riavvia. I tuoi dati, le impostazioni e gli account restano.

---

## Se non riesci più a entrare

Sul computer dove è installato PIE:

* Windows, in PowerShell come amministratore: `& "C:\Program Files\TivuStream PIE\tivustream-pie.exe" reset-password maria "--DataDirectory=C:\ProgramData\TivuStream PIE"`
* Linux: `sudo /opt/tivustream-pie/tivustream-pie reset-password maria --DataDirectory=/var/lib/tivustream-pie`

Al posto di `maria` scrivi il tuo nome. PIE ti chiede la nuova password; al primo accesso dovrai sceglierne un'altra.

---

## Disinstallare

Dalla cartella del pacchetto estratto:

* Windows, in PowerShell come amministratore: `powershell -ExecutionPolicy Bypass -File .\uninstall.ps1`
* Linux: `sudo sh uninstall.sh`

Il programma viene rimosso, **i tuoi dati restano**: se reinstalli, PIE li ritrova. Per cancellare anche quelli aggiungi `-RemoveData` su Windows o `--remove-data` su Linux. Ti verrà chiesto di confermare scrivendo `YES`.

---

## Se qualcosa va storto

Lo script si ferma, dice che cosa non ha funzionato e che cosa ha già fatto. Di solito basta correggere la causa e lanciarlo di nuovo.

**Se l'installazione si è interrotta prima della fine**, lanciala di nuovo: riprende dall'inizio senza danni.

Per vedere che cosa ha segnalato PIE:

* Windows: **Visualizzatore eventi → Registri di Windows → Applicazione**, origine **TivuStreamPIE**;
* Linux: `journalctl -u tivustream-pie`.
