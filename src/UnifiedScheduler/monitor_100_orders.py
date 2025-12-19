#!/usr/bin/env python3
import subprocess, time, sys

def get_stats():
    cmd = """docker exec timescaledb psql -U tsdbuser -d scheduler -t -c "SELECT COUNT(*), COUNT(CASE WHEN status='completed' THEN 1 END), COUNT(CASE WHEN status='processing' THEN 1 END) FROM orders WHERE title='Batch Test';" """
    try:
        r = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=10)
        if r.returncode == 0:
            p = r.stdout.strip().split('|')
            return int(p[0].strip()), int(p[1].strip()), int(p[2].strip())
    except: pass
    return 0, 0, 0

print("Monitoring 100 orders...\n")
start, last = time.time(), 0
while True:
    total, done, proc = get_stats()
    if done != last:
        print(f"[{(time.time()-start)/60:.1f}m] {done}/{total} completed, {proc} processing")
        last = done
    if done >= 100 and proc == 0:
        print(f"\n✓ Done in {(time.time()-start)/60:.1f} min! Generating Excel...")
        subprocess.run('bash export_statistics_excel.sh', shell=True, cwd='/home/akris19/Advanced-Architecture/src/UnifiedScheduler')
        print("✓ Excel: order_statistics.xlsx")
        break
    time.sleep(10)
