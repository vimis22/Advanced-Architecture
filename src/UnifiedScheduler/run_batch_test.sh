#!/bin/bash

set -e

echo "====================================="
echo "  Book Scheduler Batch Test (200 Orders)"
echo "====================================="
echo ""

# Change to UnifiedScheduler directory
cd /home/akris19/Advanced-Architecture/src/UnifiedScheduler

# Step 1: Clean up database
echo "Step 1: Cleaning up old test data..."
docker exec timescaledb psql -U tsdbuser -d scheduler -c "DELETE FROM requeue_events WHERE order_id IN (SELECT id FROM orders WHERE title='Batch Test');" 2>/dev/null || echo "No old requeue events to clean"
docker exec timescaledb psql -U tsdbuser -d scheduler -c "DELETE FROM orders WHERE title='Batch Test';" 2>/dev/null || echo "No old data to clean"
echo "✓ Database cleaned"
echo ""

# Step 2: Verify services are running
echo "Step 2: Verifying services..."
if ! docker ps | grep -q timescaledb; then
    echo "ERROR: TimescaleDB is not running. Please start services with: docker-compose up -d"
    exit 1
fi
if ! docker ps | grep -q unified-scheduler; then
    echo "ERROR: Unified Scheduler is not running. Please start services with: docker-compose up -d"
    exit 1
fi
MACHINE_COUNT=$(docker ps | grep -c "machine-[abcd]" || true)
echo "✓ Services running: TimescaleDB, Redis, Scheduler, $MACHINE_COUNT machines"
echo ""

# Step 3: Create 200 orders
echo "Step 3: Creating 200 orders with random quantities (100-300)..."
for i in {1..200}; do
    QUANTITY=$((100 + RANDOM % 201))

    # Publish via MQTT (using host mosquitto on localhost)
    mosquitto_pub -h localhost -t "scheduler/orders/create" -m "{\"title\":\"Batch Test\",\"author\":\"Automated\",\"pages\":200,\"cover_type\":\"hardcover\",\"paper_type\":\"glossy\",\"quantity\":$QUANTITY}" 2>/dev/null

    if [ $((i % 20)) -eq 0 ]; then
        echo "  Created $i orders..."
    fi

    sleep 0.05
done
echo "✓ All 200 orders created"
echo ""

# Step 4: Monitor and generate Excel
echo "Step 4: Monitoring order completion and will generate Excel when done..."
echo "This will take a while. Press Ctrl+C to stop monitoring."
echo ""

python3 /home/akris19/Advanced-Architecture/src/UnifiedScheduler/generate_stats_excel.py

echo ""
echo "====================================="
echo "  Test Complete!"
echo "====================================="
