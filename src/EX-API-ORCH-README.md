# Readme for External-Service, API-Gateway & Orchestrator Part Together
The purpose with this readme is to guide the user on how to run the services of External-Service, API-Gateway and Orchestrator, where are able to create an order from the browser as a customer and thereafter send it through the API-Gateway to the Orchestrator and then get a response back with an orderId.

# Prerequisites
In order to execute these specific services, we require you to have prehandedly installed the following:
- Docker Desktop
- Git Installed

## Step 1: Clone the Project if not done!
Please go to our Github Repository and clone the HTTP-URL:
```` bash
https://github.com/vimis22/Advanced-Architecture.git
````
## Step 2: Start all the services
```bash
# Start all services at once through this command.
# Please note that for External-Service, API-Gateway and Orchestrator. This command needs to be runned under src/API-Gateway.
docker-compose up -d
```

### Alternative: Start step-by-step
If you have any issues with the docker-compose up command, please execute the following:
```bash
# Trin 2a: Start infrastruktur services først
docker-compose up -d postgres redis zookeeper kafka mosquitto

# Trin 2b: Vent 30 sekunder og start derefter applikations-services
docker-compose up -d orchestrator api-gateway external-service
```
## Step 3: Verify that the Services work
```bash
# Tjek status på alle containers
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```
This command helps in identifying if the following services are working and healthy:

- `external-service` - Port 5173:80
- `api-gateway` - Port 8080:8080 (healthy)
- `orchestrator` - Port 8082:8082 (healthy)
- `postgres`, `redis`, `kafka`, `zookeeper`, `mosquitto`

## Step 4:
When the previous commands are done, and the docker containerization is up running. Please open the UI in the browser:
```
http://localhost:5173
```
You need to do the following steps:
- Create an order
- Send the order to the orchestrator, by clicking the "Submit" button.
- Get the response from the orchestrator
```json
{
  "orderId": 1,
  "state": "ORCHESTRATED",
  "createdAt": "2025-12-12T10:43:13.123747449"
}
```

It is also possible to send an order with the curl-command:
```bash
curl -X POST http://localhost:8080/api/v1/orchestrator/orders \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Test Book\",\"author\":\"Test Author\",\"pages\":100,\"quantity\":10,\"coverType\":\"HARDCOVER\",\"pageType\":\"GLOSSY\"}"
```

## Step 5: Stop all services, when done working!
```bash
# Stop all services
docker-compose down

# Stop and delete all volumes and restart from scratch
docker-compose down -v
```

## TroubleShooting:
### In case you recieve an 503 error:
```bash
# Check the logs and find the error
docker logs api-gateway --tail 50
docker logs orchestrator --tail 50
docker logs external-service --tail 20

# Restart all services
docker-compose restart orchestrator api-gateway external-service
```

### If images are missing:
```bash
# Please pull the prebuilt images from Docker Hub
docker pull vimis222/api-gateway:latest
docker pull vimis222/orchestrator:latest
docker pull vimis222/external-service:latest

# Then start all the services again.
docker-compose up -d
```

### What if docker compose is taking long time?
Sometimes it can take a bit of time for the Orchestrator to be ready. Please wait 1-2 minutes after the `docker-compose up -d` has been executed, before you test the User Interface.

### Service Endpoints
- **UI (External-Service):** http://localhost:5173
- **API Gateway:** http://localhost:8080
- **Orchestrator:** http://localhost:8082
- **PostgreSQL:** localhost:5432
- **Redis:** localhost:6379
- **Kafka:** localhost:9092

### Architectural Overview:
- External-Service: This is the service, where the submits an Production Order HTTP Request to the API-Gateway.
- API-Gateway: This forwards the Request to the Orchestrator Service and logs the request/response.
- Orchestrator: Validates the Request, Orchestrates the Domain logic to create an Production Order, through the Repository Port and publishes a domain event to Kafka (via the publisher post).
```
┌─────────────────────┐
│  External-Service   │
│   (React UI)        │
│   Port: 5173        │
└──────────┬──────────┘
           │
           │ HTTP POST /api/v1/orchestrator/orders
           ▼
┌─────────────────────┐
│    API Gateway      │
│ (Rate Limiting +    │
│  Circuit Breaker)   │
│   Port: 8080        │
└──────────┬──────────┘
           │
           │ Forwards to /orders
           ▼
┌─────────────────────┐
│   Orchestrator      │
│ (Business Logic +   │
│  Kafka Publisher)   │
│   Port: 8082        │
└──────────┬──────────┘
           │
           │ Stores in PostgreSQL
           │ Publishes to Kafka
           ▼
┌─────────────────────┐
│    PostgreSQL       │
│   Port: 5432        │
└─────────────────────┘
```

## The fastest method in order to get started:
```bash
cd Advanced-Architecture
docker-compose up -d
```

Please wait 1-2 minutes after the `docker-compose up -d` has been executed.
You should get this response in the console: **http://localhost:5173**, which leads to the External-Service Browser.

