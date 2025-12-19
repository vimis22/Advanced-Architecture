# UPPAAL Timed Automata Model Specification

**Generated**: 2025-11-30
**Source Codebase**: Advanced-Architecture (API-Gateway, Orchestrator, External-Service, BookScheduler_MQTT, Scheduler)

---

## 1. FUNCTION BLOCK LIST

### External-Service (Frontend)
- External-Service React SPA
- External-Service Nginx Proxy

### API-Gateway
- API-Gateway Router
- API-Gateway RateLimiter
- API-Gateway CircuitBreaker
- API-Gateway FallbackController
- API-Gateway LoggingFilter

### Orchestrator
- Orchestrator OrderIngestController
- Orchestrator OrderOrchestrationService
- Orchestrator ProductionOrder (Domain Entity)
- Orchestrator KafkaEventPublisher
- Orchestrator JpaOrderRepository

### BookScheduler_MQTT
- BookScheduler MachineManager
- BookScheduler Printer
- BookScheduler Cover
- BookScheduler Binder
- BookScheduler Packager
- BookScheduler MqttClientService
- BookScheduler DbHelper

### Infrastructure Components
- Kafka Broker
- PostgreSQL Database (Orchestrator)
- PostgreSQL Database (BookScheduler)
- Redis (RateLimiter State)
- MQTT Broker (Mosquitto)

---

## 2. STATE TRANSITIONS

### 2.1 ProductionOrder (Orchestrator Domain Entity)

**File**: `src/Orchestrator/src/main/java/org/advanced_architecture/domain/ProductionOrder.java`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| PENDING | Order created | PENDING | - | Set createdAt = LocalDateTime.now() | Instant |
| PENDING | markAsOrchestrated() | ORCHESTRATED | state == PENDING | Set state=ORCHESTRATED, orchestratedAt=LocalDateTime.now() | Instant |
| PENDING | markAsOrchestrated() | ERROR | state != PENDING | Throw IllegalStateException | Instant |
| ANY_STATE | reject(reason) | REJECTED | - | Set state=REJECTED, rejectionReason=reason | Instant |

**Code Locations**:
- State initialization: ProductionOrder.java:36-50
- PENDING → ORCHESTRATED: ProductionOrder.java:56-64
- ANY_STATE → REJECTED: ProductionOrder.java:66-69

---

### 2.2 API-Gateway CircuitBreaker State Machine

**File**: `src/API-Gateway/src/main/resources/application.yml`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| CLOSED | Failure rate > threshold | OPEN | failureRateThreshold=50%, minimumNumberOfCalls=10, slidingWindowSize=50 | Reject requests, redirect to fallback | Instant |
| OPEN | Wait duration expires | HALF_OPEN | - | Allow limited test requests | waitDurationInOpenState: 10s |
| HALF_OPEN | Success rate meets threshold | CLOSED | - | Resume normal operation | Instant |
| HALF_OPEN | Continued failures | OPEN | - | Return to open state | waitDurationInOpenState: 10s |

**Code Locations**:
- Circuit breaker config: application.yml:65-73

---

### 2.3 API-Gateway RateLimiter State Machine

**File**: `src/API-Gateway/src/main/resources/application.yml`, `src/API-Gateway/src/main/java/org/advanced_architecture/gateway/config/RateLimitConfig.java`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| ALLOWED | Request received | ALLOWED | Token count < burstCapacity (20) | Forward request | Instant |
| ALLOWED | Request received | THROTTLED | Token count >= burstCapacity (20) | Return HTTP 429 | Instant |
| THROTTLED | Token replenishment | ALLOWED | - | Allow request | Tokens replenish at 10/second |

**Code Locations**:
- Rate limit config: application.yml:34-38, 50-54
- RedisRateLimiter: RateLimitConfig.java:45

---

### 2.4 BookScheduler Machine State Machine

**File**: `src/BookScheduler_MQTT/Machines/Machine.cs`, `src/BookScheduler_MQTT/Services/DbHelper.cs`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| off | Heartbeat received | idle | isUp=true, status='off' | Update last_seen, status → idle | Instant |
| idle | Command received (job) | running | Valid job payload | SetBusyAsync(true), PublishStatusAsync("running") | Instant |
| running | Job completes | idle | Progress = 100% | SetBusyAsync(false), PublishStatusAsync("idle"), PublishDoneAsync | Instant |
| idle | Heartbeat tick | idle | Every 10 seconds | PublishStatusAsync("idle"), SetMachineHeartbeatAsync | Interval: 10s |

**Code Locations**:
- Machine state transitions: Machine.cs:38-68
- Heartbeat loop: Machine.cs:112-130
- Database state update: DbHelper.cs:42-65

---

### 2.5 BookScheduler BookStage State Machine

**File**: `src/BookScheduler_MQTT/Machines/MachineManager.cs`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| (not exists) | Stage created | queued | - | EnsureStageExistsAsync | Instant |
| queued | Machine assigned | running | Available machine of correct type exists | AssignStageMachineAsync, SetMachineBusyAsync, Publish command | Instant |
| queued | Machine check | queued | No available machine | Log "no machine", remain queued | Instant |
| running | Progress update | running | Progress < 100% | UpdateStageProgressAsync(status="running") | Continuous |
| running | Job completes | done | Progress >= 100% | UpdateStageProgressAsync(status="done"), PublishDoneAsync | Instant |

**Code Locations**:
- Stage creation: MachineManager.cs:131-134
- Stage assignment: MachineManager.cs:155-178
- Progress updates: MachineManager.cs:34-68

---

### 2.6 BookScheduler Production Pipeline State Machine

**File**: `src/BookScheduler_MQTT/Machines/MachineManager.cs`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| BOOK_ADDED | KickOffPendingJobsAsync | PRINTING_AND_COVER | - | Assign printing stage, Assign cover stage | Instant |
| PRINTING_AND_COVER | Printing done | PRINTING_AND_COVER | Cover != "done" | Wait for cover | Depends on cover machine |
| PRINTING_AND_COVER | Cover done | PRINTING_AND_COVER | Printing != "done" | Wait for printing | Depends on printer |
| PRINTING_AND_COVER | Both done | BINDING | Printing="done" AND Cover="done" | TryAdvancePipelineAsync, Assign binding stage | Instant |
| BINDING | Binding done | PACKAGING | Binding="done" | TryAdvancePipelineAsync, Assign packaging stage | Instant |
| PACKAGING | Packaging done | COMPLETE | Packaging="done" | Log "Book production complete" | Instant |

**Code Locations**:
- Pipeline orchestration: MachineManager.cs:131-205
- Stage dependency checks: MachineManager.cs:181-205

---

### 2.7 External-Service UI State Machine

**File**: `src/External-Service/src/components/functions/FunctionMethods.tsx`

| STATE NAME | EVENT/TRIGGER | NEXT STATE | GUARDS | ACTIONS | TIMING |
|------------|---------------|------------|--------|---------|--------|
| IDLE | Form mount | IDLE | - | Initialize empty form | Instant |
| IDLE | handleSubmit() | LOADING | All fields valid | Set loading=true, POST to API Gateway | Instant |
| IDLE | handleSubmit() | ERROR | Validation fails | Set error message | Instant |
| LOADING | HTTP 200 response | SUCCESS | - | Set result with OrderResponse, loading=false | Response timeout: browser default |
| LOADING | HTTP error | ERROR | - | Set error message, loading=false | Response timeout: browser default |

**Code Locations**:
- State management: FunctionMethods.tsx:8-66
- Form submission: FunctionMethods.tsx:25-66

---

## 3. INTER-COMPONENT INTERACTIONS

### 3.1 REST Calls

```
External-Service → API-Gateway : POST /api/v1/orchestrator/orders
API-Gateway → Orchestrator : POST /api/v1/orchestrator/orders (proxied)
External-Service → API-Gateway : GET /api/v1/orchestrator/orders/{orderId}
API-Gateway → Orchestrator : GET /api/v1/orchestrator/orders/{orderId} (proxied)
API-Gateway → FallbackController : forward:/fallback/orchestrator (when circuit open)
```

**Timing**:
- API-Gateway connect timeout: 5000ms (application.yml:21)
- API-Gateway response timeout: 10s (application.yml:22)
- External-Service: no explicit timeout (browser default ~30-120s)

---

### 3.2 Kafka Events

```
Orchestrator.KafkaEventPublisher → Kafka : Topic="orders.created"
```

**Message Format** (OrderEventPayloadMapper.java:40-58):
```json
{
  "order_id": "string",
  "timestamp": "ISO-8601",
  "status": "pending|orchestrated",
  "books": {
    "book_id": null,
    "title": "string",
    "author": "string",
    "pages": number,
    "quantity": number,
    "covertype": "HARDCOVER|SOFTCOVER",
    "pagetype": "GLOSSY|MATTE"
  },
  "ack_required": true
}
```

**Timing**:
- Kafka retries: 3 (KafkaConfiguration.java:30)
- Kafka acks: "all" (wait for all replicas) (KafkaConfiguration.java:29)
- Graceful failure: continues if Kafka unavailable (OrderOrchestrationService.java:45-47)

---

### 3.3 MQTT Messages

#### 3.3.1 Job Assignment
```
MachineManager → MQTT Broker : Topic="machines/{machineId}/commands"
MQTT Broker → BaseMachine : Topic="machines/{machineId}/commands"
```

**Payload**: `{"job": {"id": "guid", "title": "string", "pages": int, "copies": int}}`
**QoS**: AtLeastOnce (QoS 1) (MqttClientService.cs:114)

#### 3.3.2 Progress Updates
```
BaseMachine → MQTT Broker : Topic="jobs/{bookId}/stages/{stage}/progress"
MQTT Broker → MachineManager : Topic="jobs/+/stages/+/progress"
```

**Payload**: `{"bookId": "guid", "stage": "string", "progress": 0-100}`

#### 3.3.3 Stage Completion
```
BaseMachine → MQTT Broker : Topic="jobs/{bookId}/stages/{stage}/done"
MQTT Broker → MachineManager : Topic="jobs/+/stages/+/done"
```

**Payload**: `{"bookId": "guid", "stage": "string", "timestamp": "ISO-8601"}`

#### 3.3.4 Machine Heartbeat
```
BaseMachine → MQTT Broker : Topic="machines/{machineId}/status"
MQTT Broker → MachineManager : Topic="machines/+/status"
```

**Payload**: `{"machineId": "guid", "name": "string", "type": "string", "status": "idle|running", "timestamp": "ISO-8601"}`
**Interval**: Every 10 seconds (Machine.cs:127)

---

### 3.4 Database Access

#### Orchestrator → PostgreSQL (Orchestrator DB)
```
OrderOrchestrationService → JpaOrderRepository : save(ProductionOrder)
OrderOrchestrationService → JpaOrderRepository : findById(Long)
OrderIngestController → OrderOrchestrationService : createOrder()
OrderIngestController → OrderOrchestrationService : getOrder()
```

**Timing**: Uses Spring transaction defaults (no explicit timeouts)

#### BookScheduler → PostgreSQL (BookScheduler DB)
```
MachineManager → DbHelper : GetAvailableMachinesByTypeAsync()
MachineManager → DbHelper : EnsureStageExistsAsync()
MachineManager → DbHelper : AssignStageMachineAsync()
MachineManager → DbHelper : UpdateStageProgressAsync()
BaseMachine → DbHelper : SetMachineHeartbeatAsync()
BaseMachine → DbHelper : SetMachineBusyAsync()
```

**Timing**: Uses Npgsql defaults (no explicit timeouts)

---

### 3.5 Direct Function Calls

#### External-Service (Internal React)
```
HomeScreen.navigate() → OrderScreen
OrderScreen → OrderForm
OrderForm → useOrderForm.handleSubmit()
```

#### API-Gateway (Internal Spring)
```
APIGateway → LoggingFilter.filter() → [all requests]
APIGateway → CorsConfig.corsWebFilter() → [preflight]
APIGateway → RateLimitConfig.ipKeyResolver() → [rate limiting]
APIGateway → CircuitBreaker → [orchestrator route]
APIGateway → FallbackController.orchestratorFallback() → [when circuit open]
```

#### Orchestrator (Internal Spring)
```
OrderIngestController.ingestOrder() → OrderOrchestrationService.createOrder()
OrderOrchestrationService.createOrder() → JpaOrderRepository.save()
OrderOrchestrationService.createOrder() → KafkaEventPublisher.publish()
OrderOrchestrationService.createOrder() → ProductionOrder.markAsOrchestrated()
```

#### BookScheduler (Internal C#)
```
Program.Main() → MachineManager.KickOffPendingJobsAsync()
MachineManager → DbHelper.[various methods]
MachineManager → MqttClientService.PublishAsync()
BaseMachine → MqttClientService.SubscribeAsync()
```

---

### 3.6 Asynchronous Behavior

| Component | Async Pattern | Description |
|-----------|---------------|-------------|
| External-Service | JavaScript Promises | fetch() returns Promise, async/await in handleSubmit |
| API-Gateway | Spring WebFlux | Reactive Mono/Flux patterns (implicit) |
| Orchestrator | Spring @Transactional | Synchronous JPA transactions, Kafka fire-and-forget |
| BookScheduler | C# async/await | All I/O operations use async/await pattern |
| MQTT | Pub/Sub | All messages are asynchronous fire-and-forget (QoS 1) |
| Kafka | Fire-and-forget | Orchestrator publishes events without waiting for consumers |

---

## 4. TIMING CONSTRAINTS

### 4.1 HTTP Timeouts

| Component | Parameter | Value | Location |
|-----------|-----------|-------|----------|
| API-Gateway | Connect Timeout | 5000ms (5s) | application.yml:21 |
| API-Gateway | Response Timeout | 10s | application.yml:22 |
| API-Gateway | Connection Acquire Timeout | 500ms | application.yml:26 |
| External-Service | HTTP Request Timeout | Browser default (~30-120s) | FunctionMethods.tsx:52 |

---

### 4.2 Rate Limiting

| Route | Parameter | Value | Location |
|-------|-----------|-------|----------|
| /test/** | Replenish Rate | 10 requests/second | application.yml:37 |
| /test/** | Burst Capacity | 20 requests | application.yml:38 |
| /api/v1/orchestrator/** | Replenish Rate | 10 requests/second | application.yml:53 |
| /api/v1/orchestrator/** | Burst Capacity | 20 requests | application.yml:54 |
| Programmatic route | Replenish Rate | 10 requests/second | RateLimitConfig.java:45 |
| Programmatic route | Burst Capacity | 20 requests | RateLimitConfig.java:45 |

---

### 4.3 Circuit Breaker

| Parameter | Value | Location |
|-----------|-------|----------|
| Wait Duration in Open State | 10s | application.yml:73 |
| Sliding Window Size | 50 calls | application.yml:70 |
| Minimum Number of Calls | 10 calls | application.yml:71 |
| Failure Rate Threshold | 50% | application.yml:72 |

---

### 4.4 MQTT Timing

| Parameter | Value | Location |
|-----------|-------|----------|
| Reconnection Delay | 2 seconds | MqttClientService.cs:72 |
| QoS Level | 1 (AtLeastOnce) | MqttClientService.cs:114 |
| Keepalive | 30 seconds | (MQTT default) |

---

### 4.5 Periodic Events

| Component | Event | Interval | Location |
|-----------|-------|----------|----------|
| BaseMachine | Heartbeat publish | 10 seconds | Machine.cs:127 |
| Scheduler (legacy) | Scheduling cycle | 5 seconds | Scheduler.cs:30 |
| MQTT Client | Reconnection attempt | 2 seconds | MqttClientService.cs:72 |

---

### 4.6 Machine Simulation Timing

| Machine Type | Tick Delay | Progress Steps | Total Duration | Location |
|--------------|------------|----------------|----------------|----------|
| Printer | 1000ms (1s) | Variable (pages/33 per tick) | Depends on pages | Printer.cs:58 |
| Cover | 800ms | 10 steps (10% each) | ~8 seconds | Cover.cs:35 |
| Binder | 900ms | 4 steps (25% each) | ~3.6 seconds | Binder.cs:40 |
| Packager | 700ms | 5 steps (20% each) | ~3.5 seconds | Packager.cs:37 |

---

### 4.7 Kafka Retry Logic

| Parameter | Value | Location |
|-----------|-------|----------|
| Retries | 3 attempts | KafkaConfiguration.java:30 |
| Acks | "all" (wait for all replicas) | KafkaConfiguration.java:29 |
| Idempotence | Enabled | KafkaConfiguration.java:31 |

---

### 4.8 Database Connection Pool

| Parameter | Value | Location |
|-----------|-------|----------|
| Max Connections | 200 | application.yml:25 |
| Pool Type | elastic | application.yml:24 |

---

### 4.9 CORS Configuration

| Parameter | Value | Location |
|-----------|-------|----------|
| Max Age (preflight cache) | 3600 seconds (1 hour) | CorsConfig.java:21 |

---

### 4.10 Static Asset Caching

| Asset Type | Cache Duration | Location |
|------------|----------------|----------|
| HTML (index.html) | no-store (no caching) | nginx.conf:34 |
| JS/CSS/Images | 31536000s (1 year) | nginx.conf:40 |

---

### 4.11 Docker Health Checks

| Service | Interval | Timeout | Retries | Start Period | Location |
|---------|----------|---------|---------|--------------|----------|
| PostgreSQL | 10s | 5s | 5 | - | docker-compose.yml:22-25 |
| Redis | 10s | 3s | 5 | - | docker-compose.yml:37-40 |
| Kafka | 30s | 10s | 5 | - | docker-compose.yml:76-79 |
| Orchestrator | 30s | 10s | 3 | 60s | docker-compose.yml:121-125 |
| API-Gateway | 30s | 10s | 3 | 40s | docker-compose.yml:150-154 |

---

## 5. PROPERTIES FOR VERIFICATION

### 5.1 Safety Properties

#### S1: Order State Consistency
**Property**: An order must transition from PENDING → ORCHESTRATED in strict sequence
**Formula**: `A[] (order.state == ORCHESTRATED imply order.state_history[0] == PENDING)`
**Code Location**: ProductionOrder.java:56-64
**Guard**: `this.state != OrderState.PENDING` throws IllegalStateException

#### S2: Rate Limit Enforcement
**Property**: No client can exceed burst capacity before token replenishment
**Formula**: `A[] (requests_in_window <= 20)`
**Code Location**: application.yml:38, 54
**Timing**: Tokens replenish at 10/second

#### S3: Circuit Breaker Safety
**Property**: When circuit is OPEN, all requests must route to fallback
**Formula**: `A[] (circuit.state == OPEN imply response.source == FALLBACK)`
**Code Location**: application.yml:56-58
**Timing**: Wait 10s before transitioning to HALF_OPEN

#### S4: Pipeline Stage Dependencies
**Property**: Binding stage cannot start until both printing AND cover are done
**Formula**: `A[] (binding.state == running imply (printing.state == done and cover.state == done))`
**Code Location**: MachineManager.cs:184-194

#### S5: Machine Mutual Exclusion
**Property**: A machine cannot process two jobs simultaneously
**Formula**: `A[] forall m:Machine (m.status == running imply count(m.assigned_jobs) <= 1)`
**Code Location**: DbHelper.cs:59-65 (SetMachineBusyAsync)

#### S6: Kafka Event Ordering
**Property**: Orders must be persisted before Kafka event is published
**Formula**: `A[] (kafka.event_published imply database.order_exists)`
**Code Location**: OrderOrchestrationService.java:36-47

#### S7: Stage Uniqueness
**Property**: Each book can have only one instance of each stage type
**Formula**: `A[] forall b:Book, s:StageType (count(b.stages[s]) <= 1)`
**Code Location**: Database constraint in 001_init.sql:29 (UNIQUE(book_id, stage))

---

### 5.2 Liveness Properties

#### L1: Order Eventually Orchestrated
**Property**: Every created order must eventually reach ORCHESTRATED state (unless rejected)
**Formula**: `A<> (order.state == PENDING imply order.state == ORCHESTRATED or order.state == REJECTED)`
**Code Location**: OrderOrchestrationService.java:31-54
**Note**: Graceful Kafka failure allows orchestration even if event publish fails

#### L2: Stage Eventually Assigned
**Property**: Every queued stage must eventually be assigned to a machine if machines exist
**Formula**: `A<> (stage.status == queued and exists_available_machine imply stage.status == running)`
**Code Location**: MachineManager.cs:155-178

#### L3: Job Eventually Completes
**Property**: Every running machine job must eventually complete
**Formula**: `A<> (machine.status == running imply machine.status == idle)`
**Code Location**: Printer.cs:65-69, Cover.cs:43-47, Binder.cs:48-52, Packager.cs:45-49

#### L4: Pipeline Progress
**Property**: If all machines are operational, book must eventually complete all stages
**Formula**: `A<> (book.created and all_machines_available imply book.packaging.status == done)`
**Code Location**: MachineManager.cs:131-205

#### L5: Circuit Breaker Recovery
**Property**: Circuit breaker must eventually transition from OPEN to HALF_OPEN
**Formula**: `A<> (circuit.state == OPEN imply circuit.state == HALF_OPEN)`
**Timing**: Maximum wait time = 10 seconds (application.yml:73)

#### L6: Rate Limiter Token Replenishment
**Property**: Throttled clients must eventually be allowed again
**Formula**: `A<> (client.throttled imply client.allowed)`
**Timing**: Maximum wait = 2 seconds (burstCapacity 20 / replenishRate 10 = 2s)

#### L7: MQTT Reconnection
**Property**: Disconnected MQTT client must eventually reconnect
**Formula**: `A<> (mqtt.disconnected imply mqtt.connected)`
**Code Location**: MqttClientService.cs:68-86
**Timing**: Retry interval = 2 seconds

---

### 5.3 Timing Properties

#### T1: Machine Heartbeat Timeliness
**Property**: Machines must send heartbeat within 10 seconds
**Formula**: `A[] (machine.online imply (now - machine.last_heartbeat) <= 10s)`
**Code Location**: Machine.cs:127
**Timing**: Heartbeat interval = 10 seconds

#### T2: Circuit Breaker Timeout
**Property**: Circuit breaker must stay in OPEN state for at least 10 seconds
**Formula**: `A[] (circuit.enter_open_time imply (circuit.exit_open_time - circuit.enter_open_time) >= 10s)`
**Code Location**: application.yml:73

#### T3: HTTP Response Deadline
**Property**: API Gateway must respond or timeout within 10 seconds
**Formula**: `A[] (request.sent imply (response.received_time - request.sent_time) <= 10s or timeout)`
**Code Location**: application.yml:22

#### T4: Printer Completion Bound
**Property**: Printer must complete job within calculated duration
**Formula**: `A[] (printer.job_assigned imply printer.job_done within (pages / pages_per_tick) * 1s)`
**Code Location**: Printer.cs:54-63
**Calculation**: pages_per_tick = max(1, pages_per_min / 6)

#### T5: Cover Completion Bound
**Property**: Cover machine must complete within ~8 seconds
**Formula**: `A[] (cover.job_assigned imply cover.job_done within 8s)`
**Code Location**: Cover.cs:35 (800ms × 10 steps)

#### T6: Binder Completion Bound
**Property**: Binder machine must complete within ~3.6 seconds
**Formula**: `A[] (binder.job_assigned imply binder.job_done within 3.6s)`
**Code Location**: Binder.cs:40 (900ms × 4 steps)

#### T7: Packager Completion Bound
**Property**: Packager machine must complete within ~3.5 seconds
**Formula**: `A[] (packager.job_assigned imply packager.job_done within 3.5s)`
**Code Location**: Packager.cs:37 (700ms × 5 steps)

#### T8: Database Transaction Duration
**Property**: JPA transactions must complete within reasonable time (no explicit timeout)
**Note**: Uses Spring Boot defaults (no hard guarantee in code)

#### T9: MQTT Message Delivery (QoS 1)
**Property**: MQTT messages with QoS 1 must be delivered at least once
**Formula**: `A[] (mqtt.publish(qos=1) imply eventually mqtt.delivered)`
**Code Location**: MqttClientService.cs:114

---

### 5.4 Deadlock Freedom

#### D1: No Message Flow Deadlock
**Property**: System must not deadlock in message flow
**Potential Deadlock Scenario**: MachineManager waits for stage completion, but machine never publishes done message
**Mitigation**: Machine heartbeat (10s) allows detection of failed machines
**Code Location**: Machine.cs:112-130

#### D2: No Database Lock Deadlock
**Property**: No circular wait on database locks
**Mitigation**: Optimistic locking with @Version field
**Code Location**: ProductionOrder.java:27-28

#### D3: No MQTT Subscription Deadlock
**Property**: MQTT message handlers must not block indefinitely
**Observation**: All handlers are async and non-blocking
**Code Locations**: MachineManager.cs:34-117

---

### 5.5 Bounded Liveness

#### BL1: Order Creation Response Time
**Property**: Order creation must respond within 10s (API Gateway timeout) + database transaction time
**Upper Bound**: ~10-15 seconds
**Code Location**: application.yml:22, OrderOrchestrationService.java:31-54

#### BL2: End-to-End Book Production Time
**Property**: Book production from creation to packaging must complete within bounded time
**Calculation**:
- Printing: (pages / 33) × 1s (example: 120 pages = ~4s)
- Cover: 8s (parallel with printing)
- Binding: 3.6s
- Packaging: 3.5s
- **Total**: max(printing_time, 8s) + 3.6s + 3.5s ≈ 15.1s for 120-page book
**Assumption**: Machines available immediately (no queuing delay)

#### BL3: Machine Failure Detection Time
**Property**: Failed machine must be detected within 10 seconds (heartbeat interval)
**Code Location**: Machine.cs:127, DbHelper.cs:42-49

#### BL4: Circuit Breaker Recovery Attempt Time
**Property**: Failed service recovery must be tested within 10s of circuit opening
**Code Location**: application.yml:73

---

### 5.6 Consistency Properties

#### C1: Database-MQTT Consistency
**Property**: Stage status in database must match last published MQTT message
**Observation**: Eventual consistency - MQTT message triggers database update
**Code Location**: MachineManager.cs:34-68

#### C2: Order State-Kafka Event Consistency
**Property**: Kafka event status must reflect order state at publication time
**Note**: Order is marked ORCHESTRATED even if Kafka publish fails (graceful degradation)
**Code Location**: OrderOrchestrationService.java:45-49

#### C3: Machine Status Consistency
**Property**: Machine status in database must match last MQTT status message within 10s
**Code Location**: MachineManager.cs:97-117

---

### 5.7 Fairness Properties

#### F1: Machine Assignment Fairness
**Property**: Stages should be assigned to available machines in fair order
**Observation**: First available machine is selected (DbHelper.cs:32-40)
**Note**: No explicit fairness guarantee (could lead to starvation if one machine is always faster)

#### F2: Rate Limiter Token Bucket Fairness
**Property**: All clients receive equal opportunity to consume tokens
**Observation**: IP-based key resolution, no prioritization
**Code Location**: RateLimitConfig.java:29-40

---

## 6. SUMMARY OF KEY VERIFICATION TARGETS

### Critical Properties (Must Verify)
1. **S4**: Pipeline stage dependencies (binding waits for printing AND cover)
2. **T1**: Machine heartbeat timeliness (10s interval)
3. **T3**: HTTP response deadline (10s timeout)
4. **L4**: Pipeline progress (books eventually complete)
5. **D1**: No message flow deadlock

### Important Properties (Should Verify)
1. **S1**: Order state consistency (PENDING → ORCHESTRATED)
2. **S2**: Rate limit enforcement (≤20 requests burst)
3. **S3**: Circuit breaker safety (OPEN → fallback)
4. **L2**: Stage eventually assigned (queued → running)
5. **BL2**: End-to-end production time bounded

### Nice-to-Have Properties (Optional)
1. **F1**: Machine assignment fairness
2. **C1**: Database-MQTT consistency
3. **T8**: Database transaction duration

---

## 7. UPPAAL MODELING NOTES

### Suggested Clock Variables
- `machineHeartbeat` - Reset every 10s (machine heartbeat)
- `circuitBreakerWait` - 10s wait in OPEN state
- `rateLimiterReplenish` - Token replenishment at 10/s
- `httpResponseTimeout` - 10s response deadline
- `printerWorkTick` - 1s per work unit
- `coverWorkTick` - 800ms per progress step
- `binderWorkTick` - 900ms per progress step
- `packagerWorkTick` - 700ms per progress step
- `mqttReconnectDelay` - 2s reconnection interval

### Suggested Channels (Synchronization)
- `orderCreate_Request`
- `orderCreate_Response`
- `kafkaPublish_OrderCreated`
- `mqttPublish_Command`
- `mqttPublish_Progress`
- `mqttPublish_Done`
- `mqttPublish_Heartbeat`
- `dbUpdate_Order`
- `dbUpdate_Stage`
- `dbUpdate_Machine`

### Suggested Integer Variables
- `requestCount` - Rate limiter token bucket
- `failureCount` - Circuit breaker failure tracking
- `orderState` - ProductionOrder state enum
- `machineStatus` - Machine state enum
- `stageStatus` - BookStage state enum
- `printingProgress` - 0-100%
- `coverProgress` - 0-100%
- `bindingProgress` - 0-100%
- `packagingProgress` - 0-100%

### Suggested Boolean Variables
- `circuitBreakerOpen`
- `machineAvailable`
- `kafkaConnected`
- `mqttConnected`
- `printingDone`
- `coverDone`
- `bindingDone`
- `packagingDone`

---

## END OF SPECIFICATION

This specification provides all necessary information to build UPPAAL Timed Automata models of the system. Each timing constraint has exact values and code locations for verification.