# Installing TivuStream PIE

*Versione italiana: [`INSTALL.it.md`](INSTALL.it.md)*

PIE reads your DNS server and tells you what your home network contacts.
Everything stays on your computer: no data about your network is sent
anywhere.

This guide takes you through it step by step. You do not need to know how to
program.

---

## Before you start

You need three things.

1. **A computer that stays on**, with 64-bit Windows 10 or 11, or 64-bit Linux
   with systemd (Ubuntu or Debian, for instance). PIE observes the network
   only while the computer is on.
2. **Technitium DNS Server already working** on your network. PIE does not
   install it and does not change its settings: it only reads it.
3. **A Technitium token**, that is, a key that lets PIE read the data. How to
   create one is below.

**If Technitium runs in Docker**, check two things before you start:

* that it restarts by itself after the computer reboots
  (`--restart unless-stopped`); otherwise PIE, which does start by itself,
  finds nothing to read;
* that its data lives in a volume (`-v technitium-data:/etc/dns`). Without
  one, updating Technitium — that is, recreating the container — deletes
  users, tokens and apps, and PIE stops being able to connect. Before an
  update, in any case, make a backup from **Settings → Backup**.

### Creating the token in Technitium

The token belongs to a Technitium user created specially for PIE, which can
only **look**, never change anything.

1. Open the Technitium interface (usually
   `http://address-of-the-server:5380`) and sign in as administrator.
2. Go to **Administration → Users** and create a user, `pie` for instance. In
   the user's page, clear the **Everyone** group: otherwise the user also
   receives the permissions of that group, more than are needed.
3. Go to **Administration → Permissions**. For each of the two sections below,
   add the user `pie` with the **View** box alone, and save:
   * **Dashboard**, required: without it PIE sees nothing;
   * **Settings**, recommended: it lets PIE work out whether the protections
     of the DNS are on.

   Nothing else is needed, not even to know which device contacted which
   domain (see below).
4. Go to **Administration → Sessions**, choose **Create Token**, select the
   user `pie` and give the token a name, `pie` for instance. Copy the token:
   you will need it shortly, and Technitium will not show it to you again.

You do not need to sign out of Technitium, nor to sign in as `pie`: the token
works on its own.

### Which device contacted which domain

As standard, Technitium tells PIE which domains were contacted and which
devices were there, but not who contacted what.

To know that, the **Query Logs (Sqlite)** app must be installed in Technitium
(menu **Apps → App Store**).

**Think about it before you install it.** That app makes Technitium keep every
single request from every device, for as long as its settings provide: in
practice the browsing history of whoever uses the network. PIE, for its part,
keeps only the totals hour by hour. If you decide to install it, check in the
app's settings how long it keeps the data.

You can add it later: PIE notices by itself.

---

## Installing on Windows

1. Extract the file `tivustream-pie-…-win-x64.zip` into any folder, the
   Desktop for instance.
2. Open the Start menu, look for **PowerShell**, right-click it and choose
   **Run as administrator**.
3. Type `cd` followed by a space, drag the extracted folder into the window
   and press Enter.
4. Type this and press Enter:

   ```text
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```

The script does everything by itself and stops only to ask you three things:

| What it asks | What it means | What to answer |
| --- | --- | --- |
| `Address of Technitium` | Where Technitium is | The address you use to open it, `http://192.168.1.10:5380` for instance. If Technitium is on this same computer, just press Enter |
| `API token` | The token you created earlier | Paste it with the **right** mouse button (`Ctrl+V` may not work in that window) and press Enter. You will not see it appear: that is normal. Straight afterwards PIE says how many characters it received: for a Technitium token that is 64 |
| `Name`, then `New password` and `Repeat the password` | Your account for signing in to PIE | A name in lower case, `maria` for instance, and a password of at least 12 characters, twice |

After the token, PIE tells you what it can read and what is missing, and how
to obtain it. If the token does not work it tells you so in plain words and
asks whether you want to try again (`Try again? [Y/n]`: press Enter to try
again).

At the end the script writes the addresses to open PIE at, and a
**fingerprint**, a long sequence of letters and numbers. Keep it to hand for
the first sign in from another device.

PIE now starts by itself every time you turn the computer on, even if nobody
signs in to Windows.

---

## Installing on Linux

1. Copy the file `tivustream-pie-…-linux-x64.tar.gz` onto the computer and
   extract it:

   ```text
   tar -xzf tivustream-pie-*-linux-x64.tar.gz
   cd tivustream-pie-*-linux-x64
   ```

2. Start the installation:

   ```text
   sudo sh install.sh
   ```

The questions are the same as on Windows, in the table above.

If the computer has a firewall on, at the end the script tells you the command
to open PIE to your network: it does not change it by itself.

On a minimal distribution a library may be missing (ICU): the script tells you
so, and tells you which package to install.

---

## Opening PIE

**On the computer where you installed it:** open `http://localhost:5000`. The
address starts with `http` and the browser shows no padlock: that is right,
because that connection never leaves the computer and there is nothing to
protect along the way.

**From your phone or another computer in the house:**

1. Open one of the addresses written at the end of the installation. The one
   that starts like your router's (often `https://192.168.…:5443`) is usually
   the right one.
2. The browser warns you that the connection "is not private". That is normal:
   the certificate was created by PIE, and no browser knows it yet.
3. Open the details of the certificate and compare the **SHA-256** fingerprint
   with the one PIE wrote.
   * **If it matches**, carry on: you are talking to your own PIE.
   * **If it does not match, do not type your password.** Somebody else is
     getting in between.

The warning comes back, once per device, when PIE renews the certificate
(about once a year) or when the router changes the address of the computer.

**If you have lost the fingerprint or the addresses**, write them out again
like this:

* Windows, in PowerShell as administrator:
  `& "C:\Program Files\TivuStream PIE\tivustream-pie.exe" access "--DataDirectory=C:\ProgramData\TivuStream PIE"`
* Linux:
  `sudo /opt/tivustream-pie/tivustream-pie access --DataDirectory=/var/lib/tivustream-pie`

---

## Things worth knowing

**Home network, public or private.** Windows often marks a home network as
"public", and on a public network PIE does not open from the other devices:
the phone waits and then says the time ran out. The installation script
notices and asks whether to mark it as private. You can do it later too, in
**Settings → Network & Internet**, in the properties of the connection, under
**Network profile**. PIE never opens on public networks, such as the Wi-Fi of
a café.

**VPNs and proxies.** PIE connects to Technitium directly, without going
through a VPN or a proxy set on the computer. A VPN on your phone, on the
other hand, usually sends everything into its tunnel, and the address of PIE,
which exists only in the house, cannot be reached. Look in the VPN app for the
option that leaves the local network out ("Allow LAN access", or split
tunnelling): with that on, PIE opens even with the VPN running. Otherwise turn
the VPN off while you use PIE.

**A computer used by several people.** The browser history keeps the addresses
of the pages opened, and some pages of PIE have the name of a domain in the
address. If the computer is shared, use a dedicated browser profile for PIE.

**How long PIE keeps the data.** Hour by hour for 30 days, then day by day up
to 12 months, then month by month up to 5 years, without the link between
device and domain any more. Beyond that, nothing. What is deleted is really
deleted.

**Where your data lives.**

| | Windows | Linux |
| --- | --- | --- |
| Program | `C:\Program Files\TivuStream PIE` | `/opt/tivustream-pie` |
| Data, settings, token | `C:\ProgramData\TivuStream PIE` | `/var/lib/tivustream-pie` |

The data folder can be opened only by PIE and by the administrators of the
computer.

**Backup copies.** When an update changes the structure of the database, PIE
makes a copy of it first, in the data folder (`pie.db.schema-…bak`). The
copies do not delete themselves and contain all the detail of that moment:
once the update looks right to you, you can delete them.

---

## Updating

Extract the package of the new version and run the installation script exactly
as you did the first time. The script notices that PIE is already installed,
replaces the program and restarts it. Your data, settings and accounts stay.

---

## If you can no longer get in

On the computer where PIE is installed:

* Windows, in PowerShell as administrator:
  `& "C:\Program Files\TivuStream PIE\tivustream-pie.exe" reset-password maria "--DataDirectory=C:\ProgramData\TivuStream PIE"`
* Linux:
  `sudo /opt/tivustream-pie/tivustream-pie reset-password maria --DataDirectory=/var/lib/tivustream-pie`

In place of `maria` write your own name. PIE asks you for the new password; at
the first sign in you will have to choose another one.

---

## Uninstalling

From the folder of the extracted package:

* Windows, in PowerShell as administrator:
  `powershell -ExecutionPolicy Bypass -File .\uninstall.ps1`
* Linux: `sudo sh uninstall.sh`

The program is removed, **your data stays**: if you install again, PIE finds
it. To delete that too, add `-RemoveData` on Windows or `--remove-data` on
Linux. You will be asked to confirm by typing `YES`.

---

## If something goes wrong

The script stops, says what did not work and what it has already done. Usually
it is enough to fix the cause and run it again.

**If the installation stopped before the end**, run it again: it starts over
with no harm done.

To see what PIE reported:

* Windows: **Event Viewer → Windows Logs → Application**, source
  **TivuStreamPIE**;
* Linux: `journalctl -u tivustream-pie`.
