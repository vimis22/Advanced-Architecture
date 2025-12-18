#!/usr/bin/env python3
"""
Script to export order statistics to Excel format
Runs inside a Docker container with required packages
"""

import sys
import pandas as pd
from io import StringIO

def main():
    # Read CSV data from stdin
    csv_data = sys.stdin.read()

    if not csv_data.strip():
        print("Error: No data received", file=sys.stderr)
        sys.exit(1)

    # Parse CSV into DataFrame
    df = pd.read_csv(StringIO(csv_data))

    # Create Excel file with multiple sheets
    output_file = '/output/order_statistics.xlsx'

    with pd.ExcelWriter(output_file, engine='openpyxl') as writer:
        # Main data sheet
        df.to_excel(writer, sheet_name='Order Details', index=False)

        # Summary statistics sheet
        summary_data = {
            'Metric': [
                'Total Orders',
                'Average Completion Time (minutes)',
                'Min Completion Time (minutes)',
                'Max Completion Time (minutes)',
                'Average Wait Time (seconds)',
                'Total Requeues',
                'Average Requeue Time (ms)',
                'Orders with Requeues',
                'Average Quantity per Order'
            ],
            'Value': [
                len(df),
                round(df['completion_time_minutes'].mean(), 2) if 'completion_time_minutes' in df else 0,
                round(df['completion_time_minutes'].min(), 2) if 'completion_time_minutes' in df else 0,
                round(df['completion_time_minutes'].max(), 2) if 'completion_time_minutes' in df else 0,
                round(df['wait_time_seconds'].mean(), 2) if 'wait_time_seconds' in df else 0,
                int(df['total_requeues'].sum()) if 'total_requeues' in df else 0,
                round(df[df['avg_requeue_time_ms'].notna()]['avg_requeue_time_ms'].mean(), 2) if 'avg_requeue_time_ms' in df and df['avg_requeue_time_ms'].notna().any() else 0,
                len(df[df['total_requeues'] > 0]) if 'total_requeues' in df else 0,
                round(df['quantity'].mean(), 2) if 'quantity' in df else 0
            ]
        }
        summary_df = pd.DataFrame(summary_data)
        summary_df.to_excel(writer, sheet_name='Summary', index=False)

        # Requeue analysis sheet
        if 'total_requeues' in df:
            requeue_df = df[df['total_requeues'] > 0][['order_id', 'quantity', 'total_requeues',
                                                         'avg_requeue_time_ms', 'min_requeue_time_ms',
                                                         'max_requeue_time_ms']]
            if not requeue_df.empty:
                requeue_df.to_excel(writer, sheet_name='Requeue Analysis', index=False)

    print(f"Excel file created successfully: {output_file}")

if __name__ == "__main__":
    main()
