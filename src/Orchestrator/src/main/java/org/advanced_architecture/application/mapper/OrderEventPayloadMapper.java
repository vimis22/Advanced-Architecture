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
     * TARGET CONTRACT (Kafka):
     * {
     *   "order_id": string,
     *   "timestamp": string,
     *   "status": string,
     *   "books": {
     *     "book_id": string | null,
     *     "title": string,
     *     "author": string,
     *     "pages": number,
     *     "quantity": number,
     *     "covertype": "HARDCOVER"|"SOFTCOVER",
     *     "pagetype": "GLOSSY"|"MATTE"
     *   },
     *   "ack_required": true
     * }
     * Notes:
     * - book_id is currently not part of our domain; we publish null for now and will populate when available.
     * - status is the lowercase of domain state (e.g., PENDING -> "pending").
     */
    public static Map<String, Object> buildOrderCreatedEvent(ProductionOrder order) {
        Map<String, Object> event = new HashMap<>();
        event.put("order_id", String.valueOf(order.getId()));
        event.put("timestamp", order.getCreatedAt().toString());
        event.put("status", order.getState().toString().toLowerCase());

        Map<String, Object> books = new HashMap<>();
        books.put("book_id", null); // TODO: fill from catalog when available
        books.put("title", order.getBookDetails().getTitle());
        books.put("author", order.getBookDetails().getAuthor());
        books.put("pages", order.getBookDetails().getPages());
        books.put("quantity", order.getBookDetails().getQuantity());
        books.put("covertype", order.getBookDetails().getCoverType().name());
        books.put("pagetype", order.getBookDetails().getPageType().name());

        event.put("books", books);
        event.put("ack_required", true);
        return event;
    }
}
