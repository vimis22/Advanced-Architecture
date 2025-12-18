#!/usr/bin/env python3
"""
Script to wait for all orders to complete and generate CSV statistics
"""

import subprocess
import time
import sys

def get_order_stats():
    """Query database for order completion status"""
    cmd = """docker exec unifiedscheduler-timescaledb-1 psql -U tsdbuser -d scheduler -t -c "
        SELECT
            COUNT(*) as total,
            COUNT(CASE WHEN status='completed' THEN 1 END) as completed,
            COUNT(CASE WHEN status='processing' THEN 1 END) as processing
        FROM orders
        WHERE title='Batch Test';
    " """

    try:
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=10)
        if result.returncode == 0:
            parts = result.stdout.strip().split('|')
            if len(parts) >= 3:
                total = int(parts[0].strip())
                completed = int(parts[1].strip())
                processing = int(parts[2].strip())
                return total, completed, processing
    except Exception as e:
        print(f"Error querying database: {e}")

    return 0, 0, 0

def export_to_csv():
    """Export order statistics to CSV"""
    print("\nExporting statistics to CSV...")

    cmd = """docker exec unifiedscheduler-timescaledb-1 psql -U tsdbuser -d scheduler -c "
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
" > order_statistics.csv"""

    result = subprocess.run(cmd, shell=True, cwd='/home/akris19/Advanced-Architecture/src/UnifiedScheduler')

    if result.returncode == 0:
        print("✓ CSV file created: /home/akris19/Advanced-Architecture/src/UnifiedScheduler/order_statistics.csv")
        return True
    else:
        print("✗ Failed to create CSV")
        return False

def main():
    print("Waiting for all 200 orders to complete...")
    print("This may take a while depending on system load and machine availability.\n")

    start_time = time.time()
    last_completed = 0

    while True:
        total, completed, processing = get_order_stats()

        if total == 0:
            print("No 'Batch Test' orders found. Waiting...")
            time.sleep(5)
            continue

        if completed != last_completed:
            elapsed = time.time() - start_time
            print(f"[{elapsed/60:.1f}m] Progress: {completed}/{total} completed, {processing} processing")
            last_completed = completed

        if completed >= 200 and processing == 0:
            print(f"\n✓ All 200 orders completed in {(time.time() - start_time)/60:.1f} minutes!")
            break

        time.sleep(10)  # Check every 10 seconds

    # Export to CSV
    if export_to_csv():
        print("\nDone! You can find the statistics in:")
        print("  /home/akris19/Advanced-Architecture/src/UnifiedScheduler/order_statistics.csv")
    else:
        print("\nOrders completed but CSV export failed.")
        sys.exit(1)

if __name__ == "__main__":
    main()
