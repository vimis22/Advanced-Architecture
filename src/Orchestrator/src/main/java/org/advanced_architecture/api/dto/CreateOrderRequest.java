package org.advanced_architecture.api.dto;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

// API request DTO for creating an order. Mirrors the previous inline record fields and validation.
public record CreateOrderRequest(
        @NotBlank(message = "title is required") String title,
        @NotBlank(message = "author is required") String author,
        @NotNull(message = "pages is required") @Min(value = 1, message = "pages must be >= 1") Integer pages,
        @NotBlank(message = "coverType is required") String coverType,
        @NotNull(message = "quantity is required") @Min(value = 1, message = "quantity must be >= 1") Integer quantity
) {}
