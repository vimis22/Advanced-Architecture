package org.advanced_architecture.domain;

import jakarta.persistence.*;
import java.time.LocalDateTime;
/**
 * Domain entity representing a book production order.
 *
 * Lifecycle states:
 * - PENDING: Initial state when order is created
 * - ORCHESTRATED: Order has been processed and event published
 * - SCHEDULED: Order has been scheduled for production
 * - IN_PROGRESS: Production is ongoing
 * - COMPLETED: Production finished successfully
 * - REJECTED: Order rejected due to validation or business rule failure
 *
 * Business rules:
 * - Only PENDING orders can be marked as ORCHESTRATED
 * - createdAt timestamp is set automatically on creation
 * - Uses optimistic locking (version field) for concurrent updates
 */

@Entity
@Table(name = "production_orders")
public class ProductionOrder {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Embedded
    private BookDetails bookDetails;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private OrderState state;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @Column(name = "orchestrated_at")
    private LocalDateTime orchestratedAt;

    @Version
    private Long version;

    private String rejectionReason;

    protected ProductionOrder() {
        // JPA constructor
    }

    public ProductionOrder(BookDetails bookDetails) {
        this.bookDetails = bookDetails;
        this.state = OrderState.PENDING;
        this.createdAt = LocalDateTime.now();
    }

    @PrePersist
    protected void onCreate() {
        if (this.createdAt == null) {
            this.createdAt = LocalDateTime.now();
        }
        if (this.state == null) {
            this.state = OrderState.PENDING;
        }
    }

    public static ProductionOrder createOrder(BookDetails bookDetails) {
        return new ProductionOrder(bookDetails);
    }

    public void markAsOrchestrated() {
        if (this.state != OrderState.PENDING) {
            throw new IllegalStateException(
                    "Kan kun orkestrere orders i PENDING state. Nuværende state: " + this.state
            );
        }
        this.state = OrderState.ORCHESTRATED;
        this.orchestratedAt = LocalDateTime.now();
    }

    public void reject(String reason) {
        this.state = OrderState.REJECTED;
        this.rejectionReason = reason;
    }

    // Getters
    public Long getId() { return id; }
    public BookDetails getBookDetails() { return bookDetails; }
    public OrderState getState() { return state; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public LocalDateTime getOrchestratedAt() { return orchestratedAt; }
    public String getRejectionReason() { return rejectionReason; }
}
