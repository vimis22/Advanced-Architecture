# Description of External -> API -> Orchestration
- Canonical startup (single stack)

## How to run the full stack (canonical)
The entire system (Postgres, Redis, Zookeeper, Kafka, Orchestrator, API-Gateway, External-Service) is now started from a single Compose file:

```
cd src/API-Gateway
docker compose up --build
```

Notes:
- Do not run `docker compose` in `src/External-Service` or `src/Orchestrator`. Their compose files are intentionally empty to avoid accidental duplicate infra.
- Ports in use: Postgres 5432, Redis 6379, Zookeeper 2181, Kafka 9092, API-Gateway 8080, External-Service 5173 (mapped to container port 80), Orchestrator exposed on 8082 inside the network.
- The purpose with this readme is to explain how the Production Order works from External Service to Orchestrator via the API-Gateway.

## Overall Overview:
- External-Service: This is the service, where the submits an Production Order HTTP Request to the API-Gateway.
- API-Gateway: This forwards the Request to the Orchestrator Service and logs the request/response.
- Orchestrator: Validates the Request, Orchestrates the Domain logic to create an Production Order, through the Repository Port and publishes a domain event to Kafka (via the publisher post).

### Deep Dive with Code Structure & Examples
#### Starting Steps
We start out at the External Service, where we see that the User submits a POST Request to the API-Gateway.
In this context, we see that we are using the JSON-Body by using title, author, pages, coverType and quantity.
``
// TypeScript
async function submitOrder(payload) {
  const res = await fetch("http://localhost:8080/api/v1/orchestrator/orders", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}
``

PS: Note that to run the code for External Service, you need to run: npm run dev .
#### API-Gateway
The API-Gateway is the middleman between the External Service and the Orchestrator Service.
We see that the API-Gateway runs the Spring Boot Application, that helps in forwarding towards the Orchestrator.
The way the API-Gateway forwards towards the production order is through route-mapping inside the application.yml.

#### Orchestrator
The Orchestrator is the core of the Production Order.
We see that the Orchestrator is a Spring Boot Application, that is responsible for orchestrating the Production Order.
We see that the Orchestrator is responsible for validating the request, and orchestrating the domain logic.
We see that the Orchestrator is responsible for publishing a domain event to Kafka.

The following example shows that the OrderIngestController, which is the entry point for getting the Production order from API-Gateway.
´´
// Java
@RestController
@RequestMapping("/orders")
public class OrderIngestController {

private final OrderOrchestrationService orchestrationService;

public OrderIngestController(OrderOrchestrationService orchestrationService) {
this.orchestrationService = orchestrationService;
}

@PostMapping
public ResponseEntity<OrderResponse> create(@RequestBody CreateOrderRequest request) {
// 1) Map API DTO -> domain input
BookDetails details = ApiOrderMapper.toDomain(request);

    // 2) Call use case
    ProductionOrder order = orchestrationService.createOrder(details, request.getQuantity());

    // 3) Map domain -> API DTO
    OrderResponse response = ApiOrderMapper.toResponse(order);

    return ResponseEntity.status(HttpStatus.CREATED).body(response);
}

@GetMapping("/{orderId}")
public ResponseEntity<OrderResponse> get(@PathVariable Long orderId) {
try {
ProductionOrder order = orchestrationService.getOrder(orderId);
return ResponseEntity.ok(ApiOrderMapper.toResponse(order));
} catch (OrderOrchestrationService.OrderNotFoundException e) {
return ResponseEntity.notFound().build();
}
}
}
´´

More Information will be written as follows.
