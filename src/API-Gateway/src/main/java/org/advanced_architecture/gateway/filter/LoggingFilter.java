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
 * This is a Global Gateway Filter, which logs requests and responses.
 *
 * Responsibilities:
 * It logs HTTP Method and Path before delegating to the filter chain.
 * It logs response status after processing the book production order.
 * It logs errors without modifying responses.
 *
 * What is its Behavior?
 * This class uses the {@code ResponseLogger} to log requests and responses.
 * This helps in captuing the most traffic details.
 * But this class does not change the request/response payloads or routing.
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
