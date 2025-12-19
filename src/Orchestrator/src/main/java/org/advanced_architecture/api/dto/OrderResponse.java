package org.advanced_architecture.api.dto;

/**
 * API response DTO returned after order creation or retrieval.
 * 
 * Contains:
 * - orderId: Unique identifier of the production order
 * - state: Current order state (PENDING, ORCHESTRATED, etc.)
 * - createdAt: ISO-8601 timestamp of order creation
 */
public record OrderResponse(
        Long orderId,
        String state,
        String createdAt
) {}
