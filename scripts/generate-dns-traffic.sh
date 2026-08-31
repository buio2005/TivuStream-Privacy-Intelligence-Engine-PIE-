#!/bin/sh
#
# Generates DNS traffic against a local DNS server, for testing purposes.
#
# Runs inside a container sharing the network namespace of the DNS server, so
# that the queries reach port 53 directly instead of crossing the port
# forwarding of the host, which is unreliable for UDP on some platforms.
#
# Usage, from the repository root:
#
#   docker run --rm --network container:technitium-pie \
#     -v "<absolute path>/scripts/generate-dns-traffic.sh:/generate.sh" \
#     busybox sh /generate.sh [passes]
#
# The domains are ordinary sites plus two well known tracking domains, so that
# the classification has something to recognise.

set -u

SERVER="127.0.0.1"
PASSES="${1:-10}"

DOMAINS="kernel.org debian.org wikipedia.org github.com gnu.org mozilla.org python.org rust-lang.org archlinux.org ubuntu.com google-analytics.com scorecardresearch.com"

count=0
pass=1

while [ "$pass" -le "$PASSES" ]; do
    for domain in $DOMAINS; do
        nslookup "$domain" "$SERVER" >/dev/null 2>&1
        count=$((count + 1))
    done

    pass=$((pass + 1))
done

echo "lookups: $count"
