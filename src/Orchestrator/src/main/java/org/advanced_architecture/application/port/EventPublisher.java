package org.advanced_architecture.application.port;

public interface EventPublisher {
    void publish(String topic, String key, Object event);
}
