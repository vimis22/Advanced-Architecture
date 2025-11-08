package org.advanced_architecture.gateway.support;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatusCode;
import org.springframework.stereotype.Component;

/**
 * Default Implementation of {@link ResponseLogger}
 * Behavior:
 * - This class writes the precise info logs for requests and responses.
 * - This calsswrites errors logs in exceptions during filter chain execution.
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
