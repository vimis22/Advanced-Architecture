package org.advanced_architecture.domain;
/**
 * Enum representing the lifecycle states of a production order.
 *
 * State transitions:
 * PENDING → ORCHESTRATED → SCHEDULED → IN_PROGRESS → COMPLETED
 * Any state can transition to REJECTED if business rules are violated.
 */
public enum OrderState {
    PENDING,
    ORCHESTRATED,
    SCHEDULED,
    IN_PROGRESS,
    COMPLETED,
    REJECTED,
}
