#!/usr/bin/env bash
set -euo pipefail

# Container that runs machine.py and logs "Starting up Again"
FLAP_CONTAINER="${FLAP_CONTAINER:-edge-mqtt-simulator-c-1}"

# Log message to react to
PATTERN="${PATTERN:-Starting again}"

# How long to wait before starting it again (in seconds)
RESTART_DELAY="${RESTART_DELAY:-30}"

echo "Watching logs of container: $FLAP_CONTAINER"
echo "Will stop & restart it ${RESTART_DELAY}s after seeing: \"$PATTERN\""
echo "Press Ctrl+C to stop this script."
echo

while true; do
  echo "Attaching to logs..."
  docker logs -f "$FLAP_CONTAINER" --since=0s 2>&1 | \
  while IFS= read -r line; do
    # Uncomment if you want to see all logs:
    # echo "$line"

    if [[ "$line" == *"$PATTERN"* ]]; then
      ts="$(date -Iseconds)"
      echo
      echo "[$ts] Detected \"$PATTERN\" in logs:"
      echo "  $line"
      echo "Stopping container $FLAP_CONTAINER ..."
      docker stop "$FLAP_CONTAINER" >/dev/null || echo "Container already stopped?"

      echo "Sleeping ${RESTART_DELAY}s before restart..."
      sleep "$RESTART_DELAY"

      ts2="$(date -Iseconds)"
      echo "[$ts2] Starting container $FLAP_CONTAINER ..."
      docker start "$FLAP_CONTAINER" >/dev/null

      echo "Flap complete. Re-attaching to logs after restart..."
      echo
      # Break inner loop so we reattach docker logs -f for the restarted container
      break
    fi
  done

  # Small pause so we don't spin if container is down
  sleep 1
done
