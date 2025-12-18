#!/bin/bash

LAST_COMPLETED=0

while true; do
    echo "=== Check $(date +%H:%M:%S) ==="

    # Check progress
    docker compose exec -T timescaledb psql -U tsdbuser -d scheduler -t -c \
        "SELECT '  Order ' || o.id || ': ' || COUNT(u.id) FILTER (WHERE u.job_d_status = 'completed') || '/' || o.quantity || ' units - ' || o.status
         FROM orders o
         LEFT JOIN units u ON o.id = u.order_id
         GROUP BY o.id, o.quantity, o.status
         ORDER BY o.id;"

    # Check for newly completed orders
    COMPLETED=$(docker compose exec -T timescaledb psql -U tsdbuser -d scheduler -t -c \
        "SELECT COUNT(*) FROM orders WHERE status = 'completed';" | tr -d ' ')

    if [ "$COMPLETED" -gt "$LAST_COMPLETED" ]; then
        echo ""
        echo "╔════════════════════════════════════════════════════════════════╗"
        echo "║          ORDER COMPLETED - SHOWING STATISTICS                  ║"
        echo "╚════════════════════════════════════════════════════════════════╝"
        echo ""

        docker compose exec -T timescaledb psql -U tsdbuser -d scheduler -c \
            "SELECT
                id as \"Order\",
                title as \"Title\",
                quantity as \"Units\",
                ROUND(EXTRACT(EPOCH FROM (started_at - created_at))::numeric, 1) || 's' as \"Wait Time\",
                ROUND(EXTRACT(EPOCH FROM (completed_at - started_at))::numeric, 1) || 's' as \"Processing\"
             FROM orders
             WHERE status = 'completed'
             ORDER BY completed_at DESC
             LIMIT 5;"

        echo ""
        LAST_COMPLETED=$COMPLETED
    fi

    echo ""
    sleep 30
done
