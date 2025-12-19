package org.advanced_architecture.api;

import jakarta.validation.Valid;
import org.advanced_architecture.api.dto.CreateOrderRequest;
import org.advanced_architecture.api.dto.OrderResponse;
import org.advanced_architecture.application.OrderOrchestrationService;
import org.advanced_architecture.domain.BookDetails;
import org.advanced_architecture.domain.ProductionOrder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.*;

import java.time.Instant;
import java.util.HashMap;
import java.util.Map;
/**
 * REST controller for order ingestion.
 *
 * Responsibilities:
 * - Accepts order creation requests via POST /api/v1/orchestrator/orders
 * - Retrieves orders by ID via GET /api/v1/orchestrator/orders/{orderId}
 * - Validates incoming requests and handles exceptions
 * - Returns structured JSON responses with appropriate HTTP status codes
 *
 * Exception handling:
 * - Validation errors return 400 Bad Request
 * - Order not found returns 404 Not Found
 * - Server errors return 500 Internal Server Error
 */
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
        logger.info("Received order request for book: {} with quantity: {}", request.title(), request.quantity());

        try {
            BookDetails bookDetails = ApiOrderMapper.toDomain(request);
            ProductionOrder order = orchestrationService.createOrder(bookDetails);

            OrderResponse response = ApiOrderMapper.toResponse(order);
            logger.info("Order created successfully with ID: {}", order.getId());

            return ResponseEntity.status(HttpStatus.CREATED).body(response);
        } catch (IllegalArgumentException e) {
            logger.error("Invalid order request: {}", e.getMessage());
            throw e;
        } catch (Exception e) {
            logger.error("Failed to create order: {}", e.getMessage(), e);
            throw new OrderCreationException("Failed to create order", e);
        }
    }

    @GetMapping("/orders/{orderId}")
    public ResponseEntity<OrderResponse> getOrder(@PathVariable Long orderId) {
        logger.info("Fetching order with ID: {}", orderId);

        try {
            ProductionOrder order = orchestrationService.getOrder(orderId);
            OrderResponse response = ApiOrderMapper.toResponse(order);
            return ResponseEntity.ok(response);
        } catch (OrderOrchestrationService.OrderNotFoundException e) {
            logger.warn("Order not found: {}", orderId);
            return ResponseEntity.notFound().build();
        }
    }

    //Here we have some Exception Handlers.

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<Map<String, Object>> handleValidation(MethodArgumentNotValidException ex) {
        logger.warn("Validation error: {}", ex.getMessage());

        Map<String, Object> response = new HashMap<>();
        response.put("error", "Validation Failed");
        response.put("timestamp", Instant.now().toString());

        Map<String, String> fieldErrors = new HashMap<>();
        ex.getBindingResult().getFieldErrors().forEach(fe ->
                fieldErrors.put(fe.getField(), fe.getDefaultMessage())
        );
        response.put("errors", fieldErrors);

        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(response);
    }

    @ExceptionHandler(IllegalArgumentException.class)
    public ResponseEntity<Map<String, Object>> handleIllegalArgument(IllegalArgumentException ex) {
        logger.error("Invalid argument: {}", ex.getMessage());

        Map<String, Object> response = Map.of(
                "error", "Invalid Request",
                "message", ex.getMessage(),
                "timestamp", Instant.now().toString()
        );

        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(response);
    }

    @ExceptionHandler(OrderCreationException.class)
    public ResponseEntity<Map<String, Object>> handleOrderCreation(OrderCreationException ex) {
        logger.error("Order creation failed: {}", ex.getMessage());

        Map<String, Object> response = Map.of(
                "error", "Order Creation Failed",
                "message", ex.getMessage(),
                "timestamp", Instant.now().toString()
        );

        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(response);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<Map<String, Object>> handleGenericException(Exception ex) {
        logger.error("Unexpected error: {}", ex.getMessage(), ex);

        Map<String, Object> response = Map.of(
                "error", "Internal Server Error",
                "message", "An unexpected error occurred. Please try again later.",
                "timestamp", Instant.now().toString()
        );

        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(response);
    }

    //This is just a custom exception.

    public static class OrderCreationException extends RuntimeException {
        public OrderCreationException(String message, Throwable cause) {
            super(message, cause);
        }
    }
}
