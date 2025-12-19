package org.advanced_architecture.gateway.support;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatusCode;
import org.springframework.stereotype.Component;

/**
 * Handles fallback responses when circuit breaker opens or downstream services are unavailable.
 *
 * Responsibilities:
 * - Returns structured JSON error responses when services are down
 * - Provides endpoints under "/fallback" consumed by CircuitBreaker fallbackUri
 * - Ensures a consistent error response structure for clients
 *
 * Behavior:
 * - Produces 503 Service Unavailable responses with metadata
 * - Includes timestamp, path, and helpful suggestions in error response
 */
@Component
public class DefaultResponseLogger implements ResponseLogger {
    private static final Logger log = LoggerFactory.getLogger(DefaultResponseLogger.class);

    @Override
    public void logRequest(String method, String path) {
        log.info("Gateway request: {} {}", safe(method), safe(path));
    }

    @Override
    public void logResponse(String method, String path, HttpStatusCode status) {
        String statusCode = (status != null) ? String.valueOf(status.value()) : "UNKNOWN";
        log.info("Gateway response: {} {} - Status: {}", safe(method), safe(path), statusCode);
    }

    @Override
    public void logError(String method, String path, Throwable error) {
        String message = (error != null && error.getMessage() != null) ? error.getMessage() : "";
        log.error("Gateway error: {} {} - {}", safe(method), safe(path), message);
    }

    private String safe(String s) {
        return s == null ? "" : s;
    }
}
