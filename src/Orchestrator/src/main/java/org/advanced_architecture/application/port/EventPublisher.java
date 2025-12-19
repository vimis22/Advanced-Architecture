package org.advanced_architecture.application.port;

/**
 * Port interface for publishing domain events to external message brokers.
 *
 * Implementations handle serialization and transport of events.
 * Used by the application layer to decouple from infrastructure concerns.
 */
public interface EventPublisher {

    /**
     * Publishes an event to the specified topic.
     *
     * @param topic the target topic/channel
     * @param key the event key (e.g., order ID) for partitioning
     * @param event the event payload (will be serialized by implementation)
     */
    void publish(String topic, String key, Object event);
}
