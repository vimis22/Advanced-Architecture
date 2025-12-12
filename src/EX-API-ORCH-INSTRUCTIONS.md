# 🚀 Hurtig Start Guide - External-Service, API-Gateway og Orchestrator

Denne guide hjælper dig med at køre External-Service, API-Gateway og Orchestrator, så du kan se UI'en og få orderId tilbage.

## Forudsætninger

- Docker Desktop installeret og kørende
- Git installeret

## Trin 1: Klon projektet (hvis ikke allerede gjort)

```bash
git clone <repository-url>
cd Advanced-Architecture
```

## Trin 2: Start alle services

```bash
# Start ALLE services med Docker Compose
docker-compose up -d
```

### Alternativ: Start trin-for-trin

Hvis der er problemer med at starte alt på én gang, så kør følgende:

```bash
# Trin 2a: Start infrastruktur services først
docker-compose up -d postgres redis zookeeper kafka mosquitto

# Trin 2b: Vent 30 sekunder og start derefter applikations-services
docker-compose up -d orchestrator api-gateway external-service
```

## Trin 3: Verificer at services kører

```bash
# Tjek status på alle containers
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

Du skal se følgende services som **Up** og **healthy**:

- `external-service` - Port 5173:80
- `api-gateway` - Port 8080:8080 (healthy)
- `orchestrator` - Port 8082:8082 (healthy)
- `postgres`, `redis`, `kafka`, `zookeeper`, `mosquitto`

## Trin 4: Test systemet

### Åbn UI'en i browseren

```
http://localhost:5173
```

### Udfyld formularen

1. Indtast bogdetaljer (titel, forfatter, antal sider, antal, cover type, side type)
2. Klik på **Submit**-knappen
3. Du skulle nu modtage et orderId tilbage!

**Forventet output:**

```json
{
  "orderId": 1,
  "state": "ORCHESTRATED",
  "createdAt": "2025-12-12T10:43:13.123747449"
}
```

### Test med curl (valgfrit)

```bash
curl -X POST http://localhost:8080/api/v1/orchestrator/orders \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Test Book\",\"author\":\"Test Author\",\"pages\":100,\"quantity\":10,\"coverType\":\"HARDCOVER\",\"pageType\":\"GLOSSY\"}"
```

## Trin 5: Stop alle services (når du er færdig)

```bash
# Stop alle services
docker-compose down

# Stop og slet volumes (genstart fra scratch)
docker-compose down -v
```

## Troubleshooting

### Hvis du får 503 fejl

```bash
# Tjek logs for at finde fejl
docker logs api-gateway --tail 50
docker logs orchestrator --tail 50
docker logs external-service --tail 20

# Genstart services
docker-compose restart orchestrator api-gateway external-service
```

### Hvis images mangler

```bash
# Træk præ-byggede images fra Docker Hub
docker pull vimis222/api-gateway:latest
docker pull vimis222/orchestrator:latest
docker pull vimis222/external-service:latest

# Start derefter services igen
docker-compose up -d
```

### Hvis Docker Compose tager for lang tid

Nogle gange kan det tage lidt tid for Orchestrator at blive klar. Vent 1-2 minutter efter `docker-compose up -d` før du tester UI'en.

## Service Endpoints

- **UI (External-Service):** http://localhost:5173
- **API Gateway:** http://localhost:8080
- **Orchestrator:** http://localhost:8082
- **PostgreSQL:** localhost:5432
- **Redis:** localhost:6379
- **Kafka:** localhost:9092

## Arkitektur Oversigt

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

## ⚡ TL;DR - Hurtigste metode

```bash
cd Advanced-Architecture
docker-compose up -d
```

Vent 1-2 minutter, åbn derefter **http://localhost:5173** i browseren! 🎉

---

**Lavet af:** Vivek
**Dato:** 12. December 2025
