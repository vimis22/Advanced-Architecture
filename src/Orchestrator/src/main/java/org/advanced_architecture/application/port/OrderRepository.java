package org.advanced_architecture.application.port;

import org.advanced_architecture.domain.ProductionOrder;

import java.util.Optional;
/**
 * Port interface for production order persistence.
 *
 * Defines repository contract for the domain layer without depending on
 * specific persistence technology (JPA, NoSQL, etc.).
 */
public interface OrderRepository {
    /**
     * Saves a production order (create or update).
     *
     * @param order the order to save
     * @return the saved order with generated ID (if new)
     */
    ProductionOrder save(ProductionOrder order);
    /**
     * Finds an order by its ID.
     *
     * @param id the order ID
     * @return Optional containing the order if found, empty otherwise
     */
    Optional<ProductionOrder> findById(Long id);
    /**
     * Deletes an order by its ID.
     *
     * @param id the order ID
     */
    void deleteById(Long id);
}
