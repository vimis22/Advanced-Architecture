package org.advanced_architecture.gateway.support;

import org.springframework.http.server.reactive.ServerHttpRequest;

/**
 * Strategy Abstraction for Extracting Client IP from an HTTP Request.
 *
 * Contract:
 * - It returns the preferred Client IP, otherwise the remote address, otherwise "unknown".
 *
 * Goals:
 * - Keeps the IP Extraction Logic, Small, Testable, and replaceable.
 * - Avoid duplicating parsing across filters/configuration.
 */
public interface ClientIpExtractor {
    /**
     * Returns preferred client IP: first X-Forwarded-For value, else remote address, else "unknown".
     */
    String extract(ServerHttpRequest request);
}
