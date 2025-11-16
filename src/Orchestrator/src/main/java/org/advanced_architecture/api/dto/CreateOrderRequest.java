package org.advanced_architecture.api.dto;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import org.advanced_architecture.domain.CoverType;
import org.advanced_architecture.domain.PageType;

// API request DTO for creating an order.
// CURRENT REQUEST CONTRACT (flat JSON) accepted by Orchestrator:
// {
//   "title": string,
//   "author": string,
//   "pages": number>=1,
//   "coverType": "HARDCOVER"|"SOFTCOVER",
//   "pageType": "GLOSSY"|"MATTE",
//   "quantity": number>=1
// }
// PLANNED FINAL CONTRACT (for Kafka and future API): a nested `books` object with snake_case keys.
public record CreateOrderRequest(
        @NotBlank(message = "title is required") String title,
        @NotBlank(message = "author is required") String author,
        @NotNull(message = "pages is required") @Min(value = 1, message = "pages must be >= 1") Integer pages,
        @NotNull(message = "coverType is required") CoverType coverType,
        @NotNull(message = "pageType is required") PageType pageType,
        @NotNull(message = "quantity is required") @Min(value = 1, message = "quantity must be >= 1") Integer quantity
) {}
