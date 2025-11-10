# Orchestrator Module — Layered Structure (Refactor Notes)

This module has been refactored to clarify responsibilities and improve separation of concerns without changing any external behavior.

No public behavior has changed:
- Same REST endpoints, URLs, request/response JSON, and HTTP status codes
- Same log messages and levels at the same points in the flow
- Same Kafka topics/keys/payload structure
- Same persistence schema and behavior
- Same configuration keys and environment variables

## Layer boundaries

- api
  - Controllers and DTOs used for the HTTP API
  - Small, pure mapper to convert API DTOs ↔ domain models
  - Location:
    - `org.advanced_architecture.api.OrderIngestController`
    - `org.advanced_architecture.api.dto.CreateOrderRequest`
    - `org.advanced_architecture.api.dto.OrderResponse`
    - `org.advanced_architecture.api.ApiOrderMapper`

- application
  - Application services (use cases) and ports (interfaces)
  - Event payload mapper lives here; builds Maps used for publishing without changing structure
  - No framework-specific types in services/ports
  - Location:
    - `org.advanced_architecture.application.OrderOrchestrationService`
    - `org.advanced_architecture.application.port.OrderRepository`
    - `org.advanced_architecture.application.port.EventPublisher`
    - `org.advanced_architecture.application.mapper.OrderEventPayloadMapper`

- domain
  - Entities/value objects, enums, business rules
  - Focused on invariants and state transitions
  - Location:
    - `org.advanced_architecture.domain.*`

- infrastructure
  - Adapters for persistence and Kafka, plus configuration
  - Persistence adapter implements `OrderRepository`
  - Kafka adapter implements `EventPublisher`
  - Spring configuration beans for Kafka and ObjectMapper
  - Location:
    - `org.advanced_architecture.infrastructure.persistence.JpaOrderRepository`
    - `org.advanced_architecture.infrastructure.kafka.KafkaEventPublisher`
    - `org.advanced_architecture.infrastructure.kafka.KafkaConfiguration`

## Mappers

- API mapper (`ApiOrderMapper`)
  - Maps `CreateOrderRequest` → `BookDetails`
  - Maps `ProductionOrder` → `OrderResponse`
  - Pure transformations; no side effects

- Application event payload mapper (`OrderEventPayloadMapper`)
  - Builds the event payload Map for the `orders.created` topic
  - Keys and values are unchanged from the pre-refactor implementation

## Wiring

- Constructor injection is used throughout
- Configuration classes remain under `infrastructure.kafka`
- Component scanning continues to work because package roots are unchanged (`org.advanced_architecture`)

## Notes

- No tests were added or modified as part of this refactor per the requirements.
- If you add new use cases, follow the same layering: define a port in `application.port`, implement it in `infrastructure`, expose via `api` controller using DTOs, and keep domain pure.


---

## Docker Compose (local stack)

A single compose file is provided at `src/Orchestrator/docker-compose.yml`. The previous `docker-compose.yaml` has been removed to avoid confusion.

### Services included
- postgres (15-alpine)
  - Port: host `5432` → container `5432`
  - Volume: `postgres_data:/var/lib/postgresql/data`
- zookeeper (Confluent 7.5.0)
  - Port: `2181` exposed
- kafka (Confluent 7.5.0)
  - Port: host `9092` → container `9092`
  - Advertised listener: `PLAINTEXT://kafka:9092` (usable from other containers)
- orchestrator (this module)
  - Depends on: postgres, kafka
  - Exposes container port `8082` mapped to host `8081`
  - Env: DB and Kafka bootstrap servers preconfigured for the compose network
- api-gateway
  - Depends on: orchestrator
  - Port: `8080` exposed

### How to run
From the project root:

```
# Validate config
docker compose -f src/Orchestrator/docker-compose.yml config

# Start in background
docker compose -f src/Orchestrator/docker-compose.yml up -d

# View logs (optional)
docker compose -f src/Orchestrator/docker-compose.yml logs -f --tail=200

# Stop and remove containers
docker compose -f src/Orchestrator/docker-compose.yml down
```

Alternatively, run the same commands from the `src/Orchestrator` directory and omit the `-f` flag (Compose will pick up `docker-compose.yml` automatically).

### Expected ports
- API Gateway: http://localhost:8080/
- Orchestrator (direct): http://localhost:8081/
- Kafka broker: localhost:9092 (from host)
- Zookeeper: localhost:2181
- Postgres: localhost:5432 (db `bookproduction`, user `postgres`, password `postgres`)

