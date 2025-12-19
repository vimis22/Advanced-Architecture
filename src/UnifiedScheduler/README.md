# Unified Book Scheduler

A production-ready book manufacturing scheduler with heartbeat monitoring, fault tolerance, and dependency-based job distribution.

## Architecture

### Components

1. **UnifiedScheduler (C#)** - Main scheduler with:
   - HeartbeatObserver: Tracks machine state via MQTT heartbeats
   - HeartbeatMonitor: Detects failed machines (3 missed heartbeats)
   - JobQueueManager: Manages 4 job type queues (A, B, C, D)
   - JobAssigner: Assigns jobs to available machines
   - OrderManager: Creates and tracks orders

2. **Machine Simulators (Python)** - 4 machine types:
   - Machine A: Printing pages
   - Machine B: Creating covers
   - Machine C: Binding (requires A & B complete)
   - Machine D: Packaging (requires C complete)

3. **Redis** - Real-time state (Source of Truth):
   - Machine heartbeat tracking with status
   - Priority-based job queues (3 levels)
   - Unit state with job status and timestamps
   - Order completion tracking

4. **TimescaleDB** - Analytics and historical data:
   - Orders (metadata only)
   - Requeue events (machine failures and recovery)

5. **MQTT** - Communication:
   - `machines/{id}/heartbeat` - Machine publishes state every 1s
   - `machines/{id}/work` - Scheduler publishes job assignments

### Data Flow

```
Order Created
  ↓
Units Created (quantity)
  ↓
Queued to job_a (printing) AND job_b (cover) [PARALLEL]
  ↓
Machine A processes job_a  +  Machine B processes job_b
  ↓
Both complete (detected via heartbeat progress: 100)
  ↓
Unit queued to job_c (binding)
  ↓
Machine C processes job_c
  ↓
Completes (detected via heartbeat)
  ↓
Unit queued to job_d (packaging)
  ↓
Machine D processes job_d
  ↓
Unit complete → Order complete when all units done
```

## Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8.0 SDK (for local development)

### Run the System

1. **Start all services:**
```bash
cd src/UnifiedScheduler
docker-compose up -d
```

This starts:
- MQTT Broker (port 1883)
- Redis (port 6379)
- TimescaleDB (port 5432)
- Unified Scheduler with Web Dashboard (port 8080)
- 4 Machine Simulators (A, B, C, D)

2. **Access the Web Dashboard:**

Open your browser and navigate to:
```
http://localhost:8080
```

From the web dashboard you can:
- **Create new orders** - Specify title, author, pages, cover type, paper type, and quantity
- **Monitor the scheduler in real-time** - Watch jobs being assigned to machines
- **Track order progress** - See which units are in progress and completed
- **View completion status** - Know when your order is complete
- **Analyze statistics** - View processing times and requeue events for each order

The dashboard provides a live view of:
- Active machines and their current status
- Job queue lengths for each stage (A, B, C, D)
- Order progress with unit-by-unit tracking
- Historical statistics including total processing time and requeue counts

### Run Locally (Development)

1. **Start infrastructure:**
```bash
docker-compose up mosquitto redis timescaledb
```

2. **Run scheduler:**
```bash
cd src/UnifiedScheduler
dotnet run
```

3. **Run machines (separate terminals):**
```bash
cd src/edge-mqtt/simulator

# Machine A
MACHINE_TYPE=A DEVICE_ID=A-local-001 BROKER_HOST=localhost python machine.py

# Machine B
MACHINE_TYPE=B DEVICE_ID=B-local-001 BROKER_HOST=localhost python machine.py

# Machine C
MACHINE_TYPE=C DEVICE_ID=C-local-001 BROKER_HOST=localhost python machine.py

# Machine D
MACHINE_TYPE=D DEVICE_ID=D-local-001 BROKER_HOST=localhost python machine.py
```

## Configuration

Edit `appsettings.json`:

```json
{
  "MQTT": {
    "Broker": "localhost",
    "Port": 1883
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "TimescaleDB": {
    "ConnectionString": "Host=localhost;Port=5432;Database=scheduler;Username=tsdbuser;Password=tsdbpass"
  },
  "Scheduler": {
    "HeartbeatIntervalSeconds": 1,
    "HeartbeatTimeoutCycles": 3,  // Fail after 3 missed heartbeats
    "JobAssignmentIntervalMs": 500
  }
}
```

## Testing Fault Tolerance

1. **Kill a machine during processing:**
```bash
docker stop machine-a
```

The scheduler will detect the failure after 3 seconds (3 missed heartbeats) and re-queue the job.

2. **Restart the machine:**
```bash
docker start machine-a
```

The machine will reconnect and start receiving new jobs.

## Database Queries and Analytics

### TimescaleDB Statistics

Connect to the database:
```bash
docker exec -it timescaledb psql -U tsdbuser -d scheduler
```

### View all orders with completion status:
```sql
SELECT
    id,
    title,
    quantity,
    status,
    created_at,
    completed_at,
    EXTRACT(EPOCH FROM (completed_at - started_at))::int as processing_seconds
FROM orders
ORDER BY id DESC;
```

### View all requeue events (machine failures):
```sql
SELECT * FROM requeue_events
ORDER BY timestamp DESC
LIMIT 50;
```

### Requeue summary by order:
```sql
SELECT
    order_id,
    COUNT(*) as total_requeues,
    AVG(recovery_duration_ms)::int as avg_recovery_ms,
    MIN(recovery_duration_ms) as min_recovery_ms,
    MAX(recovery_duration_ms) as max_recovery_ms
FROM requeue_events
GROUP BY order_id
ORDER BY order_id;
```

### Requeue summary by job type:
```sql
SELECT
    job_type,
    COUNT(*) as requeue_count,
    AVG(recovery_duration_ms)::int as avg_recovery_ms
FROM requeue_events
GROUP BY job_type
ORDER BY requeue_count DESC;
```

### Requeue summary by machine:
```sql
SELECT
    machine_id,
    machine_type,
    COUNT(*) as failure_count,
    AVG(recovery_duration_ms)::int as avg_recovery_ms
FROM requeue_events
GROUP BY machine_id, machine_type
ORDER BY failure_count DESC;
```

### Orders with their requeue counts:
```sql
SELECT
    o.id,
    o.title,
    o.quantity,
    o.status,
    COUNT(r.id) as requeue_count,
    EXTRACT(EPOCH FROM (o.completed_at - o.started_at))::int as processing_seconds
FROM orders o
LEFT JOIN requeue_events r ON o.id = r.order_id
GROUP BY o.id, o.title, o.quantity, o.status, o.completed_at, o.started_at
ORDER BY o.id DESC;
```

### Use the built-in analytics view:
```sql
-- Pre-built view for requeue statistics
SELECT * FROM requeue_statistics;
```

### Order duration statistics (completed orders only):
```sql
SELECT * FROM order_duration_statistics
ORDER BY created_at DESC;
```

### Redis Real-time Queries

View current queue lengths:
```bash
docker exec -it redis redis-cli
> ZCARD job_queue_job_a
> ZCARD job_queue_job_b
> ZCARD job_queue_job_c
> ZCARD job_queue_job_d
```

View machine state:
```bash
> KEYS machine:*
> HGETALL machine:A-machine-001
```

View unit state (example):
```bash
> KEYS unit:*
> HGETALL unit:1:1
```

Check order completion counter:
```bash
> GET order:1:completed_units
```

## API (Future Enhancement)

The scheduler can be extended with a REST API:

```
POST /api/orders - Create order
GET /api/orders/{id} - Get order status
GET /api/machines - List machines
GET /api/queues - Get queue status
```

## Monitoring

View logs:
```bash
docker-compose logs -f scheduler
docker-compose logs -f machine-a
```

View Redis state:
```bash
docker exec -it redis redis-cli
> KEYS *
> HGETALL machine:A-machine-001
> LLEN job_queue_a
```

View TimescaleDB:
```bash
docker exec -it timescaledb psql -U tsdbuser -d scheduler
```

## Troubleshooting

**Machines not connecting:**
- Check MQTT broker is running: `docker-compose logs mosquitto`
- Check network: `docker network inspect unifiedscheduler_scheduler-network`

**Jobs not being assigned:**
- Check queues: Type `queues` in scheduler console
- Check machines: Type `machines` in scheduler console
- Check Redis: `docker exec -it redis redis-cli KEYS *`

**Database connection errors:**
- Wait for TimescaleDB to fully start (takes ~10s)
- Check connection string in appsettings.json

## Architecture Decisions

### Data Storage Strategy

**Redis = Real-time Source of Truth**
- All unit state stored as Redis hashes (`unit:{orderId}:{unitNumber}`)
- Machine state tracked in Redis (`machine:{machineId}`)
- Job queues use sorted sets with priority scoring
- Fully cleaned up after order completion

**TimescaleDB = Analytics Only**
- Stores order metadata (title, author, quantity, etc.)
- Logs requeue events for failure analysis
- Does NOT store real-time unit or machine state
- Optimized for time-series analytics and reporting

### Key Design Principles

1. **Heartbeat as ACK**: No explicit ACK messages. Heartbeat with progress serves as implicit acknowledgment.

2. **Separation of Concerns**: Redis for real-time operations (low latency), TimescaleDB for analytics (historical queries).

3. **Queue-based Distribution**: Decouples scheduler from machines, enables fault tolerance.

4. **Dependency Management**: Observer pattern detects job completions and automatically queues dependent jobs.

5. **Priority-based Requeuing**: Failed jobs get highest priority (3) to ensure fast recovery.

## License

MIT
