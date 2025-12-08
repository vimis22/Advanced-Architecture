# Testing the Unified Scheduler

This guide will help you test the complete unified scheduler system with the MQTT dashboard.

## Quick Start

### 1. Start the System

```bash
cd src/UnifiedScheduler
docker-compose up --build
```

This starts:
- **MQTT Broker** (Mosquitto) - Port 1883 (MQTT), Port 9001 (WebSockets)
- **Redis** - Port 6379
- **TimescaleDB** - Port 5432
- **Unified Scheduler** (C#)
- **4 Machine Simulators** (A, B, C, D)
- **MQTT Dashboard** - http://localhost:8080

### 2. Open the Dashboard

Navigate to: **http://localhost:8080**

You should see the "Unified Book Scheduler Dashboard"

### 3. Connect to MQTT

In the dashboard:

1. **Broker URL**: `ws://localhost:9001` (should be pre-filled)
2. Click **Connect**
3. You should see "connected" badge turn green
4. Console will show: `Subscribed to machines/+/heartbeat`

### 4. Wait for Machines

After a few seconds, you should see 4 machine cards appear:
- **Machine A** - Printing
- **Machine B** - Cover
- **Machine C** - Binding
- **Machine D** - Packaging

Each machine card shows:
- **Green dot** = Online (receiving heartbeats)
- **Status**: idle / running / off
- **Current Unit**: Which unit it's processing
- **Progress bar**: 0-100%
- **Heartbeats**: Count and time since last heartbeat

## Test Scenarios

### Test 1: Create a Simple Order

1. In the "Create Order" section, fill in:
   - **Title**: "Test Book 1"
   - **Author**: "Test Author"
   - **Pages**: 100
   - **Cover Type**: Hardcover
   - **Paper Type**: Glossy
   - **Quantity**: 5

2. Click **Create Order**

3. **Watch the Dashboard**:
   - Machines A and B should turn **green** (status: running)
   - Progress bars will show increasing progress
   - When they reach 100%, machines return to idle
   - Machine C should start (binding)
   - Then Machine D (packaging)

4. **Check the Console**:
   - You'll see green messages like: `✓ Job completed on A-machine-001: unit <unit-id>`
   - Watch the entire pipeline execute

### Test 2: Larger Order (Test Parallelization)

1. Create an order with **Quantity: 20**

2. **Observe**:
   - Machines A and B will process multiple units
   - As soon as units complete A and B, they queue to C
   - Machine C processes units one at a time
   - Machine D processes the final step

3. **What to look for**:
   - Multiple units being processed simultaneously on A and B
   - Dependencies working correctly (C waits for both A and B)
   - Sequential processing through C and D

### Test 3: Machine Failure (Fault Tolerance)

This tests the heartbeat monitoring system.

1. Create an order with **Quantity: 10**

2. While machines are processing, **kill a machine**:
   ```bash
   # In another terminal
   docker stop machine-a
   ```

3. **Watch the Dashboard**:
   - After 3 seconds (3 missed heartbeats), the dashboard will show:
     - Red console message: `🔴 MACHINE FAILURE: A-machine-001 (Type A) - No heartbeat for 3.0s`
     - Warning about unit being re-queued
   - The machine card's status dot turns **red** (offline)

4. **Check Scheduler Logs**:
   ```bash
   docker-compose logs scheduler
   ```

   You should see:
   ```
   [HeartbeatMonitor] ⚠ MACHINE FAILURE DETECTED: A-machine-001
   [HeartbeatMonitor] Re-queuing unit <unit-id> from failed machine
   ```

5. **Restart the Machine**:
   ```bash
   docker start machine-a
   ```

6. **Observe**:
   - Machine reconnects (green dot returns)
   - Status returns to idle
   - Scheduler assigns the re-queued job to it
   - Processing resumes

### Test 4: Multiple Machine Failures

1. Create an order with **Quantity: 15**

2. Kill multiple machines:
   ```bash
   docker stop machine-a machine-b
   ```

3. **Observe**:
   - Both failures detected
   - Units from both machines re-queued
   - Console shows failure alerts

4. **Restart one machine**:
   ```bash
   docker start machine-a
   ```

5. **Observe**:
   - Machine A picks up work from both queues
   - Machine B jobs remain queued

6. **Restart second machine**:
   ```bash
   docker start machine-b
   ```

7. **Observe**:
   - Both machines working again
   - Order completes successfully

### Test 5: Dependency Verification

This verifies that job C only starts after BOTH A and B complete.

1. Create an order with **Quantity: 3**

2. **Immediately kill Machine B**:
   ```bash
   docker stop machine-b
   ```

3. **Observe**:
   - Machine A completes job_a for all 3 units
   - Machine C does **NOT** start (because job_b isn't complete)
   - Units wait in pending state

4. **Restart Machine B**:
   ```bash
   docker start machine-b
   ```

5. **Observe**:
   - Machine B processes job_b for all 3 units
   - As soon as BOTH job_a and job_b complete for a unit:
     - Unit immediately queues to job_c
     - Machine C starts processing
   - Pipeline completes: C → D

## Monitoring

### Dashboard Console

The console shows real-time events:
- **Green**: Successful operations (job completions, connections)
- **Yellow**: Warnings (reconnections)
- **Red**: Errors (machine failures)

### Machine Status

Each machine card updates in real-time:
- **Status dot**: Green = online, Red = offline
- **Status**: idle, running, off
- **Current Unit**: Shows which unit is being processed
- **Progress**: Visual bar showing job completion %
- **Heartbeats**: Count and recency indicator

### Docker Logs

Monitor specific components:

```bash
# Scheduler logs
docker-compose logs -f scheduler

# Specific machine
docker-compose logs -f machine-a

# All machines
docker-compose logs -f machine-a machine-b machine-c machine-d

# MQTT broker
docker-compose logs -f mosquitto
```

### Database Queries

Connect to TimescaleDB:
```bash
docker exec -it timescaledb psql -U tsdbuser -d scheduler
```

Useful queries:
```sql
-- View all orders
SELECT * FROM orders ORDER BY created_at DESC;

-- View order progress
SELECT * FROM order_progress;

-- View units for an order
SELECT * FROM units WHERE order_id = '<order-id>';

-- View machine health
SELECT * FROM machine_health;

-- View recent machine events
SELECT * FROM machine_events
ORDER BY timestamp DESC
LIMIT 50;

-- Count completed units per order
SELECT order_id, COUNT(*) as completed_units
FROM units
WHERE job_d_status = 'completed'
GROUP BY order_id;
```

Check Redis:
```bash
docker exec -it redis redis-cli

# View all keys
KEYS *

# Check machine state
HGETALL machine:A-machine-001

# Check job queues
LLEN job_queue_a
LLEN job_queue_b
LLEN job_queue_c
LLEN job_queue_d

# View queue contents
LRANGE job_queue_a 0 -1
```

## Expected Behavior

### Normal Operation

1. **Order Created**:
   - Units created in TimescaleDB
   - Units initialized in Redis
   - All units queued to job_a AND job_b

2. **Job Assignment**:
   - JobAssigner finds idle machines
   - Assigns jobs from queues
   - Updates Redis and TimescaleDB
   - Publishes work to machines via MQTT

3. **Job Processing**:
   - Machine receives work assignment
   - Starts processing (status: running)
   - Sends heartbeats every 1 second with progress
   - Progress increases 0 → 100%

4. **Job Completion**:
   - HeartbeatObserver detects progress: 100
   - Updates database (completed)
   - Checks dependencies
   - Re-queues to next job if ready

5. **Order Completion**:
   - All units complete job_d
   - Order status updated to 'completed'
   - Redis cleaned up

### Machine Failure

1. **Detection**:
   - HeartbeatMonitor checks every 1 second
   - If no heartbeat for 3 cycles (3 seconds)
   - Machine marked as failed

2. **Recovery**:
   - Current unit retrieved
   - Unit status reset to 'pending'
   - Unit re-queued to appropriate job queue
   - Failure logged to TimescaleDB

3. **Resume**:
   - When machine restarts
   - Sends initial heartbeat
   - Marked as available (idle)
   - JobAssigner assigns new work

## Troubleshooting

### Machines Not Showing in Dashboard

- Check MQTT connection: Should see "connected" badge
- Check console: Should show "Subscribed to machines/+/heartbeat"
- Wait 5 seconds for initial heartbeats
- Check if machines are running: `docker ps`

### Jobs Not Being Assigned

- Check scheduler logs: `docker-compose logs scheduler`
- Verify machines are idle: Check dashboard
- Verify jobs in queues: Redis `LLEN job_queue_a`
- Check TimescaleDB connection: Scheduler needs DB access

### Dashboard Shows Machines Offline

- Check if heartbeat interval matches (default: 1 second)
- Check OFFLINE_AFTER_MS in dashboard (default: 3000ms)
- Verify MQTT websockets working: Port 9001
- Check browser console for errors

### Database Connection Errors

- TimescaleDB takes ~10 seconds to start
- Check if running: `docker ps | grep timescaledb`
- Check logs: `docker-compose logs timescaledb`
- Verify connection string in appsettings.json

## Clean Up

Stop all services:
```bash
docker-compose down
```

Remove volumes (clean slate):
```bash
docker-compose down -v
```

Remove images:
```bash
docker-compose down --rmi all
```

## Performance Metrics

Expected timings (approximate):
- **Heartbeat interval**: 1 second
- **Failure detection**: 3 seconds (3 missed heartbeats)
- **Job assignment**: 500ms polling interval
- **Job completion detection**: Immediate (on heartbeat)
- **Dependency check**: Immediate (after completion)

For a 10-unit order:
- **Job A duration**: ~3-8 seconds per unit (random tick)
- **Job B duration**: ~3-8 seconds per unit
- **Job C duration**: ~2-6 seconds per unit
- **Job D duration**: ~1-5 seconds per unit
- **Total time**: Depends on parallelization (A & B parallel)

## Success Criteria

✅ All 4 machines visible in dashboard with green dots
✅ Order creation triggers job assignment
✅ Progress bars show increasing values
✅ Job completions logged in console
✅ Dependencies respected (C waits for A & B)
✅ Machine failure detected within 3 seconds
✅ Failed jobs re-queued automatically
✅ Restarted machines resume work
✅ Orders complete successfully with all units processed

Happy Testing! 🚀📚
