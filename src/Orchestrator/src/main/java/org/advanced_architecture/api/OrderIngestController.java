package org.advanced_architecture.api;

import jakarta.validation.Valid;
import org.advanced_architecture.api.dto.CreateOrderRequest;
import org.advanced_architecture.api.dto.OrderResponse;
import org.advanced_architecture.application.OrderOrchestrationService;
import org.advanced_architecture.domain.ProductionOrder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/orchestrator")
public class OrderIngestController {

    private static final Logger logger = LoggerFactory.getLogger(OrderIngestController.class);

    private final OrderOrchestrationService orchestrationService;

    public OrderIngestController(OrderOrchestrationService orchestrationService) {
        this.orchestrationService = orchestrationService;
    }

    @PostMapping("/orders")
    public ResponseEntity<OrderResponse> ingestOrder(@Valid @RequestBody CreateOrderRequest request) {
        logger.info("Received order request for book: {}", request.title());

        ProductionOrder order = orchestrationService.orchestrateOrder(ApiOrderMapper.toDomain(request));

        OrderResponse response = ApiOrderMapper.toResponse(order);

        return ResponseEntity.ok(response);
    }

    @GetMapping("/orders/{orderId}")
    public ResponseEntity<OrderResponse> getOrder(@PathVariable Long orderId) {
        try {
            ProductionOrder order = orchestrationService.getOrder(orderId);

            OrderResponse response = ApiOrderMapper.toResponse(order);

            return ResponseEntity.ok(response);
        } catch (OrderOrchestrationService.OrderNotFoundException e) {
            return ResponseEntity.notFound().build();
        }
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<Map<String, String>> handleValidation(MethodArgumentNotValidException ex) {
        Map<String, String> errors = new HashMap<>();
        ex.getBindingResult().getFieldErrors().forEach(fe -> errors.put(fe.getField(), fe.getDefaultMessage()));
        return ResponseEntity.badRequest().body(errors);
    }
}
