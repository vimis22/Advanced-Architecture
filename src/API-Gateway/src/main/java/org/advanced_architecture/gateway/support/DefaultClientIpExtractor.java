package org.advanced_architecture.gateway.support;

import org.springframework.http.server.reactive.ServerHttpRequest;
import org.springframework.stereotype.Component;

/**
 * Default implementation of {@link ResponseLogger}.
 *
 * Behavior:
 * - Writes info logs for all requests and responses
 * - Writes error logs for exceptions during filter chain execution
 * - Uses safe() method to prevent null pointer exceptions in log messages.
 *
 * Notes:
 * - Designed for use behind proxies/load balancers.
 * - Keep logic minimal to ease testing.
 */
@Component
public class DefaultClientIpExtractor implements ClientIpExtractor {
    @Override
    public String extract(ServerHttpRequest request) {
        String xff = request.getHeaders().getFirst("X-Forwarded-For");
        if (xff != null && !xff.isBlank()) {
            return xff.split(",")[0].trim();
        }
        var remoteAddress = request.getRemoteAddress();
        if (remoteAddress != null && remoteAddress.getAddress() != null) {
            return remoteAddress.getAddress().getHostAddress();
        }
        return "unknown";
    }
}
