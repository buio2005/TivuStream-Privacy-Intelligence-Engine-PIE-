#!/bin/sh
# TivuStream Privacy Intelligence Engine (PIE)
# Removes PIE from Linux.
#
# Installation Specification, Uninstallation Script. The data folder stays,
# unless --remove-data is given and confirmed:
#
#   sudo sh uninstall.sh
#   sudo sh uninstall.sh --remove-data

set -u

SERVICE=tivustream-pie
SERVICE_USER=tivustream-pie
PROGRAM_DIR=/opt/tivustream-pie
DATA_DIR=/var/lib/tivustream-pie
UNIT=/etc/systemd/system/tivustream-pie.service

if [ "$(id -u)" -ne 0 ]; then
    echo 'This script must be run as administrator: sudo sh uninstall.sh' >&2
    exit 1
fi

if [ -f "$UNIT" ]; then
    systemctl disable --now "$SERVICE" >/dev/null 2>&1
    rm -f "$UNIT"
    systemctl daemon-reload
    echo 'Service removed.'
fi

if [ -d "$PROGRAM_DIR" ]; then
    rm -rf "$PROGRAM_DIR"
    echo "Program removed from $PROGRAM_DIR."
fi

if [ ! -d "$DATA_DIR" ]; then
    exit 0
fi

if [ "${1:-}" != "--remove-data" ]; then
    echo
    echo "Your data is still in $DATA_DIR: the history of your network, your settings and the token of your DNS server."
    echo 'Installing PIE again will find it. To delete it too, run: sudo sh uninstall.sh --remove-data'
    exit 0
fi

echo
echo "$DATA_DIR holds:"
echo '  - the history of what your network contacted'
echo '  - your accounts and settings, with the token of your DNS server'
echo '  - the certificate of PIE and any backup copies of the database'
echo
printf 'Delete all of it? This cannot be undone. Type YES to confirm: '
read -r answer

if [ "$answer" != "YES" ]; then
    echo 'Nothing was deleted.'
    exit 0
fi

rm -rf "$DATA_DIR"
userdel "$SERVICE_USER" >/dev/null 2>&1
echo 'Data deleted.'
