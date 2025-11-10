package org.advanced_architecture.api.dto;

// API response DTO mirroring previous inline record fields.
public record OrderResponse(
        Long orderId,
        String state,
        String createdAt
) {}
