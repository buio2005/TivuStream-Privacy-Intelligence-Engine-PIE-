#!/bin/sh
# TivuStream Privacy Intelligence Engine (PIE)
# Installs PIE on Linux as a systemd service, or updates an installation.
#
# Installation Specification, Installation Script. Run from the folder of the
# extracted package:
#
#   sudo sh install.sh

set -u

SERVICE=tivustream-pie
SERVICE_USER=tivustream-pie
PROGRAM_DIR=/opt/tivustream-pie
DATA_DIR=/var/lib/tivustream-pie
PROGRAM="$PROGRAM_DIR/tivustream-pie"
COMPLETED="$DATA_DIR/installation-completed"
UNIT=/etc/systemd/system/tivustream-pie.service
HTTP_PORT=5000
HTTPS_PORT=5443
HERE=$(cd "$(dirname "$0")" && pwd)
PACKAGE="$HERE/app"
DONE=""

step() {
    printf '\n== %s\n' "$1"
}

done_() {
    DONE="$DONE
  - $1"
}

stop_because() {
    printf '\n%s\n' "$1" >&2
    if [ -n "$DONE" ]; then
        printf 'Already done:%s\n' "$DONE" >&2
    fi
    exit 1
}

pie() {
    "$PROGRAM" "$@" "--DataDirectory=$DATA_DIR"
}

# The files PIE writes while run by hand here belong to the service.
give_data_to_service() {
    chown -R "$SERVICE_USER:$SERVICE_USER" "$DATA_DIR"
}

wait_answer() {
    # 401 is the answer of a PIE that is up and asks who is calling.
    attempt=0
    while [ $attempt -lt 60 ]; do
        if command -v curl >/dev/null 2>&1; then
            code=$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:$HTTP_PORT/api/v1/health" || true)
            [ "$code" = "401" ] && return 0
        elif systemctl is-active --quiet "$SERVICE"; then
            sleep 5
            return 0
        fi
        attempt=$((attempt + 1))
        sleep 1
    done
    return 1
}

port_in_use() {
    ss -ltn 2>/dev/null | awk '{print $4}' | grep -Eq "[:.]$1\$"
}

JOURNAL_HINT="Look at what the service reported with: journalctl -u $SERVICE"

# ------------------------------------------------------------------
step 'Checking this computer'

[ "$(id -u)" -eq 0 ] || stop_because 'This script must be run as administrator: sudo sh install.sh'

[ "$(uname -m)" = "x86_64" ] || stop_because "PIE is prepared for 64-bit x86 computers; this one is $(uname -m)."

[ -d /run/systemd/system ] || stop_because 'PIE runs as a systemd service, and systemd is not running on this computer.'

# .NET reads names of languages and countries through ICU, which most
# distributions install and minimal ones do not.
if ! ldconfig -p 2>/dev/null | grep -q 'libicuuc'; then
    stop_because 'A library PIE needs is missing: ICU. Install the package of your distribution whose name starts with libicu (on Debian and Ubuntu: sudo apt install libicu-dev), then run this script again.'
fi

[ -f "$PACKAGE/tivustream-pie" ] || stop_because "The program was not found in $PACKAGE. Run the script from the folder of the extracted package."

free_kb=$(df -Pk /opt 2>/dev/null | awk 'NR==2 {print $4}')
if [ -n "$free_kb" ] && [ "$free_kb" -lt 1048576 ]; then
    stop_because 'There is less than 1 GB free under /opt. PIE needs room for the program and its history.'
fi

# An installation is updated only when it was completed: an interrupted one
# is taken up again from the start, and every step below can be repeated.
installed=false
if [ -f "$UNIT" ] && [ -f "$COMPLETED" ]; then
    installed=true
fi

if systemctl is-active --quiet "$SERVICE"; then
    systemctl stop "$SERVICE"
    done_ 'service stopped'
fi

for port in $HTTP_PORT $HTTPS_PORT; do
    if port_in_use "$port"; then
        stop_because "Port $port is already in use by another program. Stop it, then run this script again."
    fi
done

echo 'Everything needed is in place.'

# Copied beside the old program and swapped in at the end, so that an
# interruption never leaves half of each.
copy_program() {
    rm -rf "$PROGRAM_DIR.new" || return 1
    cp -R "$PACKAGE" "$PROGRAM_DIR.new" || return 1
    cp "$HERE/INSTALL.md" "$PROGRAM_DIR.new/" 2>/dev/null

    # Permissions do not survive an archive written on Windows.
    chmod 0755 "$PROGRAM_DIR.new/tivustream-pie" || return 1
    [ ! -f "$PROGRAM_DIR.new/createdump" ] || chmod 0755 "$PROGRAM_DIR.new/createdump"

    if [ -d "$PROGRAM_DIR" ]; then
        rm -rf "$PROGRAM_DIR.old"
        mv "$PROGRAM_DIR" "$PROGRAM_DIR.old" || return 1
    fi

    mv "$PROGRAM_DIR.new" "$PROGRAM_DIR" || return 1
    rm -rf "$PROGRAM_DIR.old"
}

# ------------------------------------------------------------------
if [ "$installed" = true ]; then
    step 'Updating PIE'

    echo 'Replacing the program. Your data and settings are not touched.'
    copy_program || stop_because 'The program could not be replaced. The service is stopped: run this script again.'
    done_ 'program replaced'

    systemctl start "$SERVICE"
    wait_answer || stop_because "The service did not answer after the update. $JOURNAL_HINT"

    step 'Updated'
    pie access
    exit 0
fi

# ------------------------------------------------------------------
step 'Copying the program'

copy_program || stop_because "The program could not be copied to $PROGRAM_DIR."
done_ "program copied to $PROGRAM_DIR"
echo "Program in $PROGRAM_DIR"

# ------------------------------------------------------------------
step 'Preparing the account and the data folder'

# An account of its own, that cannot sign in.
if ! id "$SERVICE_USER" >/dev/null 2>&1; then
    nologin=$(command -v nologin || echo /usr/sbin/nologin)
    useradd --system --no-create-home --home-dir "$DATA_DIR" --shell "$nologin" "$SERVICE_USER" ||
        stop_because 'The account of the service could not be created.'
fi
done_ "account $SERVICE_USER"

# Readable only by the service and by the administrator: it holds the
# history of the network and the token of the DNS server.
install -d -m 0700 -o "$SERVICE_USER" -g "$SERVICE_USER" "$DATA_DIR" ||
    stop_because "The data folder $DATA_DIR could not be created."
done_ "data folder $DATA_DIR"
echo "Data in $DATA_DIR, readable only by PIE and by the administrator."

# ------------------------------------------------------------------
step 'Connecting to your DNS server'

pie configure || stop_because 'PIE is not connected to the DNS server. Run this script again when you have the address and the token.'
give_data_to_service
done_ 'connected to the DNS server'

# ------------------------------------------------------------------
step 'Creating your administrator account'

echo 'Choose the name you will sign in with: 3 to 32 characters, lower case letters, digits, dot, hyphen, underscore.'

while true; do
    printf 'Name: '
    read -r name || stop_because 'No name was given.'
    if pie reset-password "$name"; then
        break
    fi
    echo 'Try again.'
done
give_data_to_service
done_ "administrator $name"

# ------------------------------------------------------------------
step 'Registering the service'

cp "$HERE/tivustream-pie.service" "$UNIT" && chmod 0644 "$UNIT" ||
    stop_because 'The service could not be registered.'
systemctl daemon-reload
systemctl enable "$SERVICE" >/dev/null 2>&1
done_ "service $SERVICE registered"

# ------------------------------------------------------------------
step 'Starting PIE'

systemctl start "$SERVICE"
wait_answer || stop_because "The service started but does not answer. $JOURNAL_HINT"

# The certificate is created by the service as it starts.
sleep 2

# ------------------------------------------------------------------
step 'Ready'

# Written last: from now on, running this script again updates PIE.
date -u +%Y-%m-%dT%H:%M:%SZ > "$COMPLETED"
give_data_to_service

pie access

echo
echo "Sign in as '$name' with the password you just chose."

# A firewall is not changed by this script: it says what to open instead.
if command -v ufw >/dev/null 2>&1 && ufw status 2>/dev/null | grep -q 'Status: active'; then
    echo
    echo "A firewall is active. To open PIE to your home network: sudo ufw allow $HTTPS_PORT/tcp"
elif command -v firewall-cmd >/dev/null 2>&1 && firewall-cmd --state >/dev/null 2>&1; then
    echo
    echo "A firewall is active. To open PIE to your home network: sudo firewall-cmd --permanent --add-port=$HTTPS_PORT/tcp && sudo firewall-cmd --reload"
fi
