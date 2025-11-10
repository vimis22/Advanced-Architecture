package org.advanced_architecture.application.mapper;

import org.advanced_architecture.domain.ProductionOrder;

import java.util.HashMap;
import java.util.Map;

/**
 * Builds event payloads for the application layer.
 * Pure mapping of domain objects to simple payload structures (e.g., Map) used by adapters.
 */
public final class OrderEventPayloadMapper {

    private OrderEventPayloadMapper() {
        // utility class
    }

    /**
     * Builds the payload for the "OrderCreated" event.
     * The structure and keys are intentionally preserved to avoid any behavior change.
     */
    public static Map<String, Object> buildOrderCreatedEvent(ProductionOrder order) {
        Map<String, Object> event = new HashMap<>();
        event.put("orderId", order.getId());
        event.put("title", order.getBookDetails().getTitle());
        event.put("author", order.getBookDetails().getAuthor());
        event.put("pages", order.getBookDetails().getPages());
        event.put("coverType", order.getBookDetails().getCoverType());
        event.put("quantity", order.getBookDetails().getQuantity());
        event.put("estimatedCost", order.getBookDetails().getEstimatedCost());
        event.put("createdAt", order.getCreatedAt().toString());
        return event;
    }
}
