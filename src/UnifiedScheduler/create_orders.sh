#!/bin/bash

# Script to create 200 orders with random quantities between 100-300

echo "Creating 200 orders with random quantities (100-300)..."

for i in {1..100}; do
    # Generate random quantity between 100 and 300
    QUANTITY=$((100 + RANDOM % 201))

    # Create order via MQTT
    docker exec mqtt-broker mosquitto_pub -t "scheduler/orders/create" -m "{\"title\":\"Order Batch Test\",\"author\":\"Automated\",\"pages\":200,\"cover_type\":\"hardcover\",\"paper_type\":\"glossy\",\"quantity\":$QUANTITY}"

    echo "Created order $i with quantity $QUANTITY"

    # Small delay to avoid overwhelming the system
    sleep 0.1
done

echo "All 200 orders created successfully!"
echo "Waiting for orders to complete..."
