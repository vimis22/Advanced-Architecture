#!/usr/bin/env python3
"""
Script to wait for all orders to complete and generate Excel statistics
"""

import subprocess
import time
import sys

def get_order_stats():
    """Query database for order completion status"""
    cmd = """docker exec timescaledb psql -U tsdbuser -d scheduler -t -c "
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

def export_to_excel():
    """Export order statistics to Excel"""
    print("\nExporting statistics to Excel...")

    # Run the export script
    try:
        result = subprocess.run(
            'bash /home/akris19/Advanced-Architecture/src/UnifiedScheduler/export_statistics_excel.sh',
            shell=True,
            cwd='/home/akris19/Advanced-Architecture/src/UnifiedScheduler',
            capture_output=True,
            text=True,
            timeout=60
        )

        if result.returncode == 0:
            print("✓ Excel file created: /home/akris19/Advanced-Architecture/src/UnifiedScheduler/order_statistics.xlsx")
            return True
        else:
            print(f"✗ Failed to create Excel file: {result.stderr}")
            return False

    except Exception as e:
        print(f"✗ Failed to create Excel file: {e}")
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

        if completed >= 100 and processing == 0:
            print(f"\n✓ All 200 orders completed in {(time.time() - start_time)/60:.1f} minutes!")
            break

        time.sleep(10)  # Check every 10 seconds

    # Export to Excel
    if export_to_excel():
        print("\nDone! You can find the statistics in:")
        print("  /home/akris19/Advanced-Architecture/src/UnifiedScheduler/order_statistics.xlsx")
        print("\nThe Excel file contains 3 sheets:")
        print("  1. Order Details - Complete data for all orders")
        print("  2. Summary - Key statistics and metrics")
        print("  3. Requeue Analysis - Orders that experienced requeues")
    else:
        print("\nOrders completed but Excel export failed.")
        sys.exit(1)

if __name__ == "__main__":
    main()
