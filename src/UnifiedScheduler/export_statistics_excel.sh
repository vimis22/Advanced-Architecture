#!/bin/bash

# Script to export order statistics to Excel

OUTPUT_FILE="order_statistics.xlsx"

echo "Exporting order statistics to Excel..."

# Export data from PostgreSQL as CSV
CSV_DATA=$(docker exec timescaledb psql -U tsdbuser -d scheduler -c "
COPY (
    SELECT
        o.id as order_id,
        o.title,
        o.quantity,
        o.status,
        o.created_at,
        o.started_at,
        o.completed_at,
        EXTRACT(EPOCH FROM (o.started_at - o.created_at))::int as wait_time_seconds,
        EXTRACT(EPOCH FROM (o.completed_at - o.started_at))::int as completion_time_seconds,
        ROUND(EXTRACT(EPOCH FROM (o.completed_at - o.started_at))::numeric / 60, 2) as completion_time_minutes,
        COUNT(r.id) as total_requeues,
        ROUND(AVG(r.recovery_duration_ms)::numeric, 2) as avg_requeue_time_ms,
        MIN(r.recovery_duration_ms) as min_requeue_time_ms,
        MAX(r.recovery_duration_ms) as max_requeue_time_ms
    FROM orders o
    LEFT JOIN requeue_events r ON o.id = r.order_id
    WHERE o.status = 'completed' AND o.title = 'Batch Test'
    GROUP BY o.id, o.title, o.quantity, o.status, o.created_at, o.started_at, o.completed_at
    ORDER BY o.id
) TO STDOUT WITH CSV HEADER
")

if [ $? -ne 0 ]; then
    echo "Failed to export data from database"
    exit 1
fi

# Use Python container with pandas and openpyxl to convert to Excel
echo "$CSV_DATA" | docker run --rm -i \
    -v "$(pwd):/output" \
    python:3.12-slim \
    bash -c "pip install -q pandas openpyxl && python3 /output/export_to_excel.py"

if [ $? -eq 0 ]; then
    echo ""
    echo "Statistics exported to $OUTPUT_FILE"
    echo ""
    echo "The Excel file contains 3 sheets:"
    echo "  1. Order Details - Complete data for all orders"
    echo "  2. Summary - Key statistics and metrics"
    echo "  3. Requeue Analysis - Orders that experienced requeues"
    echo ""
    echo "File location: $(pwd)/$OUTPUT_FILE"
else
    echo "Failed to create Excel file"
    exit 1
fi
