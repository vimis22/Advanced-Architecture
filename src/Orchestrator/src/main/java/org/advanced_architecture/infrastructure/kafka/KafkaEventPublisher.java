package org.advanced_architecture.infrastructure.kafka;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.advanced_architecture.application.port.EventPublisher;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Component;
/**
 * Kafka implementation of {@link EventPublisher}.
 *
 * Responsibilities:
 * - Serializes event objects to JSON using Jackson ObjectMapper
 * - Publishes events to Kafka topics via KafkaTemplate
 * - Logs successful publishes and serialization errors
 *
 * Error handling:
 * - JSON serialization failures throw RuntimeException
 * - Kafka send failures are not caught (rely on Spring Kafka retries)
 */
@Component
public class KafkaEventPublisher implements EventPublisher {

    private static final Logger logger = LoggerFactory.getLogger(KafkaEventPublisher.class);

    private final KafkaTemplate<String, String> kafkaTemplate;
    private final ObjectMapper objectMapper;

    public KafkaEventPublisher(KafkaTemplate<String, String> kafkaTemplate, ObjectMapper objectMapper) {
        this.kafkaTemplate = kafkaTemplate;
        this.objectMapper = objectMapper;
    }

    /**
     * Publishes an event to the specified topic.
     *
     * @param topic the target topic/channel
     * @param key the event key (e.g., order ID) for partitioning
     * @param event the event payload (will be serialized by implementation)
     */
    @Override
    public void publish(String topic, String key, Object event) {
        try {
            String eventJson = objectMapper.writeValueAsString(event);
            kafkaTemplate.send(topic, key, eventJson);
            logger.info("Published event to topic: {} with key: {}", topic, key);
        } catch (JsonProcessingException e) {
            logger.error("Failed to serialize event to JSON", e);
            throw new RuntimeException("Event publishing failed", e);
        }
    }
}
