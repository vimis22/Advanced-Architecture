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
