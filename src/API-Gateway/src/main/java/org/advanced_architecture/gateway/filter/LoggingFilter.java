package org.advanced_architecture.gateway.filter;

import org.advanced_architecture.gateway.support.ResponseLogger;
import org.springframework.cloud.gateway.filter.GatewayFilterChain;
import org.springframework.cloud.gateway.filter.GlobalFilter;
import org.springframework.core.Ordered;
import org.springframework.http.HttpStatusCode;
import org.springframework.stereotype.Component;
import org.springframework.web.server.ServerWebExchange;
import reactor.core.publisher.Mono;

/**
 * Global filter that logs all incoming requests and outgoing responses.
 *
 * Responsibilities:
 * - Logs HTTP method and path for every incoming request
 * - Logs response status code after request processing
 * - Logs errors without modifying the response
 * - Executes early in the filter chain (order: -1) to capture all traffic
 *
 * Uses {@link ResponseLogger} to handle the actual logging logic while
 * keeping this filter focused on interception only.
 */
@Component
public class LoggingFilter implements GlobalFilter, Ordered {

    private final ResponseLogger responseLogger;

    public LoggingFilter(ResponseLogger responseLogger) {
        this.responseLogger = responseLogger;
    }

    @Override
    public Mono<Void> filter(ServerWebExchange exchange, GatewayFilterChain chain) {
        String path = exchange.getRequest().getPath().toString();
        exchange.getRequest().getMethod();
        String method = exchange.getRequest().getMethod().toString();

        // Log before proceeding in the chain
        responseLogger.logRequest(method, path);

        return chain
                .filter(exchange)
                .doOnError(error -> responseLogger.logError(method, path, error))
                .then(Mono.fromRunnable(() -> {
                    HttpStatusCode status = exchange.getResponse().getStatusCode();
                    responseLogger.logResponse(method, path, status);
                }));
    }

    @Override
    public int getOrder() {
        return -1;
    }
}
