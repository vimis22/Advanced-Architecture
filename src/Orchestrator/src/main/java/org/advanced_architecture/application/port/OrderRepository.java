package org.advanced_architecture.application.port;

import org.advanced_architecture.domain.ProductionOrder;

import java.util.Optional;

public interface OrderRepository {
    ProductionOrder save(ProductionOrder order);

    Optional<ProductionOrder> findById(Long id);

    void deleteById(Long id);
}
