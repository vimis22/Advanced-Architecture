#!/bin/bash

# Script to export order statistics to CSV

OUTPUT_FILE="order_statistics.csv"

echo "Exporting order statistics to $OUTPUT_FILE..."

# Export to CSV using PostgreSQL COPY command
docker exec timescaledb psql -U tsdbuser -d scheduler -c "
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
    WHERE o.status = 'completed'
    GROUP BY o.id, o.title, o.quantity, o.status, o.created_at, o.started_at, o.completed_at
    ORDER BY o.id
) TO STDOUT WITH CSV HEADER
" > $OUTPUT_FILE

echo "Statistics exported to $OUTPUT_FILE"
echo ""
echo "Summary:"
wc -l $OUTPUT_FILE
head -5 $OUTPUT_FILE
echo "..."
echo ""
echo "CSV file location: $(pwd)/$OUTPUT_FILE"
