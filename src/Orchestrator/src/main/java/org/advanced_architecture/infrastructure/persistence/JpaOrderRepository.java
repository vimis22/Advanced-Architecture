package org.advanced_architecture.infrastructure.persistence;

import org.advanced_architecture.application.port.OrderRepository;
import org.advanced_architecture.domain.ProductionOrder;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import java.util.Optional;

@Repository
@Transactional
public class JpaOrderRepository implements OrderRepository {

    @PersistenceContext
    private EntityManager entityManager;

    @Override
    public ProductionOrder save(ProductionOrder order) {
        if (order.getId() == null) {
            entityManager.persist(order);
            return order;
        } else {
            return entityManager.merge(order);
        }
    }

    @Override
    public Optional<ProductionOrder> findById(Long id) {
        ProductionOrder order = entityManager.find(ProductionOrder.class, id);
        return Optional.ofNullable(order);
    }

    @Override
    public void deleteById(Long id) {
        ProductionOrder order = entityManager.find(ProductionOrder.class, id);
        if (order != null) {
            entityManager.remove(order);
        }
    }
}
