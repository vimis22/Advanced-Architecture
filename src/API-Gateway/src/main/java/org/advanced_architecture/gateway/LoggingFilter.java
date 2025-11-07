package org.advanced_architecture.gateway;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.cloud.gateway.filter.GatewayFilterChain;
import org.springframework.cloud.gateway.filter.GlobalFilter;
import org.springframework.core.Ordered;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.stereotype.Component;
import org.springframework.web.server.ServerWebExchange;
import reactor.core.publisher.Mono;

@Component
public class LoggingFilter implements GlobalFilter, Ordered {
    private static final Logger logger = LoggerFactory.getLogger(LoggingFilter.class);

    @Override
    public Mono<Void> filter(ServerWebExchange exchange, GatewayFilterChain chain) {
        String path = exchange.getRequest().getPath().toString();
        exchange.getRequest().getMethod();
        String method = exchange.getRequest().getMethod().toString();

        logger.info("Request received request: {} {} ", method, path);

        return chain.filter(exchange).then(Mono.fromRunnable(() -> {
            HttpStatusCode status = exchange.getResponse().getStatusCode();
            String statusCode = (status != null) ? String.valueOf(status.value()) : "UNKNOWN";
            logger.info("Gateway response: {} {} - Status: {}", method, path, statusCode);
        }));
    }

    @Override
    public int getOrder() {
        return -1;
    }
}
