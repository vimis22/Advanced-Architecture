package org.advanced_architecture.api;

import jakarta.validation.Valid;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import org.advanced_architecture.application.OrderOrchestrationService;
import org.advanced_architecture.domain.BookDetails;
import org.advanced_architecture.domain.ProductionOrder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
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

        BookDetails bookDetails = new BookDetails(
                request.title(),
                request.author(),
                request.pages(),
                request.coverType(),
                request.quantity()
        );

        ProductionOrder order = orchestrationService.orchestrateOrder(bookDetails);

        OrderResponse response = new OrderResponse(
                order.getId(),
                order.getState().toString(),
                order.getCreatedAt().toString()
        );

        return ResponseEntity.status(HttpStatus.ACCEPTED).body(response);
    }

    @GetMapping("/orders/{orderId}")
    public ResponseEntity<OrderResponse> getOrder(@PathVariable Long orderId) {
        try {
            ProductionOrder order = orchestrationService.getOrder(orderId);

            OrderResponse response = new OrderResponse(
                    order.getId(),
                    order.getState().toString(),
                    order.getCreatedAt().toString()
            );

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

    public static record CreateOrderRequest(
            @NotBlank(message = "title is required") String title,
            @NotBlank(message = "author is required") String author,
            @NotNull(message = "pages is required") @Min(value = 1, message = "pages must be >= 1") Integer pages,
            @NotBlank(message = "coverType is required") String coverType,
            @NotNull(message = "quantity is required") @Min(value = 1, message = "quantity must be >= 1") Integer quantity
    ) {}

    public static record OrderResponse(
            Long orderId,
            String state,
            String createdAt
    ) {}
}
