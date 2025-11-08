package org.advanced_architecture.gateway.support;

import org.springframework.http.HttpStatusCode;

/**
 * Abstraction for Logging Gateway Request/Response Lifecycle Events.
 *
 * Contract:
 * - Log Request Line (Method, Path).
 * - Log Successful Response (Method, Path, Status).
 * - Log Error Response (Method, Path, Status, Error).
 *
 * Goals:
 * - Decouple logging details from filter implementation.
 * - Allow swapping logging strategy if needed.
 */
public interface ResponseLogger {
    void logRequest(String method, String path);
    void logResponse(String method, String path, HttpStatusCode status);
    void logError(String method, String path, Throwable error);
}
