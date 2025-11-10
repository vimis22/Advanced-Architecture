package org.advanced_architecture.application;

import org.advanced_architecture.application.mapper.OrderEventPayloadMapper;
import org.advanced_architecture.application.port.EventPublisher;
import org.advanced_architecture.application.port.OrderRepository;
import org.advanced_architecture.domain.BookDetails;
import org.advanced_architecture.domain.ProductionOrder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Map;

@Service
public class OrderOrchestrationService {
    private static final Logger logger = LoggerFactory.getLogger(OrderOrchestrationService.class);

    private static final String ORDER_CREATED_TOPIC = "orders.created";

    private final OrderRepository orderRepository;
    private final EventPublisher eventPublisher;

    public OrderOrchestrationService(OrderRepository orderRepository,
                                     EventPublisher eventPublisher) {
        this.orderRepository = orderRepository;
        this.eventPublisher = eventPublisher;
    }

    @Transactional
    public ProductionOrder orchestrateOrder(BookDetails bookDetails) {
        logger.info("Starting orchestration for book: {}", bookDetails.getTitle());

        ProductionOrder order = ProductionOrder.createOrder(bookDetails);

        ProductionOrder savedOrder = orderRepository.save(order);
        logger.info("Order persisted with ID: {}", savedOrder.getId());

        Map<String, Object> orderCreatedEvent = OrderEventPayloadMapper.buildOrderCreatedEvent(savedOrder);

        String eventKey = String.valueOf(savedOrder.getId());
        try {
            eventPublisher.publish(ORDER_CREATED_TOPIC, eventKey, orderCreatedEvent);
            logger.info("OrderCreated event published for order ID: {}", savedOrder.getId());
        } catch (Exception ex) {
            logger.warn("Kafka not available or publish failed. Continuing without event publication. Cause: {}", ex.toString());
        }

        savedOrder.markAsOrchestrated();
        orderRepository.save(savedOrder);
        logger.info("Order {} marked as ORCHESTRATED", savedOrder.getId());

        return savedOrder;
    }

    @Transactional(readOnly = true)
    public ProductionOrder getOrder(Long orderId) {
        return orderRepository.findById(orderId)
                .orElseThrow(() -> new OrderNotFoundException("Order ikke fundet: " + orderId));
    }

    public static class OrderNotFoundException extends RuntimeException {
        public OrderNotFoundException(String message) {
            super(message);
        }
    }
}
