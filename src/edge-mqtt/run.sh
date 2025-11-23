#!/usr/bin/env bash
set -euo pipefail

echo "Starting docker-compose..."
docker compose up -d --scale simulator-a=10 --scale simulator-b=10

echo "Waiting for DB to be ready..."
until docker exec timescaledb pg_isready -U tsdbuser -d grid >/dev/null 2>&1; do
  echo "  DB not ready yet, retrying..."
  sleep 1
done

echo "Running init.sql inside timescaledb..."
docker exec -i timescaledb \
  psql -U tsdbuser -d grid < db/init.sql

echo "Done."
