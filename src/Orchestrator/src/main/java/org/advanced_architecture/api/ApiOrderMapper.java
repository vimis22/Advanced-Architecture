package org.advanced_architecture.api;

import org.advanced_architecture.api.dto.CreateOrderRequest;
import org.advanced_architecture.api.dto.OrderResponse;
import org.advanced_architecture.domain.BookDetails;
import org.advanced_architecture.domain.ProductionOrder;

/**
 * Mapper for API layer conversions between DTOs and domain models.
 * Pure mapping: no business logic, no framework dependencies beyond DTO/domain types.
 */
public final class ApiOrderMapper {

    private ApiOrderMapper() {
        // utility class
    }

    public static BookDetails toDomain(CreateOrderRequest request) {
        return new BookDetails(
                request.title(),
                request.author(),
                request.pages(),
                request.coverType(),
                request.pageType(),
                request.quantity()
        );
    }

    public static OrderResponse toResponse(ProductionOrder order) {
        return new OrderResponse(
                order.getId(),
                order.getState().toString(),
                order.getCreatedAt().toString()
        );
    }
}
