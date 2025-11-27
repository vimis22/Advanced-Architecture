package org.advanced_architecture.gateway.config;

import org.advanced_architecture.gateway.support.ClientIpExtractor;
import org.springframework.cloud.gateway.filter.ratelimit.KeyResolver;
import org.springframework.cloud.gateway.filter.ratelimit.RedisRateLimiter;
import org.springframework.cloud.gateway.route.RouteLocator;
import org.springframework.cloud.gateway.route.builder.RouteLocatorBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import reactor.core.publisher.Mono;

/**
 * The Spring COnfiguration for Rate Limiting Key Resolution.
 *
 * Responsibilites:
 * Shows the {@code ipKeyResolver} for Spring Cloud Gateway.
 * It delegates the IP-Extraction to {@link ClientIpExtractor}
 *
 * What is its Behavior?
 * Resolves client identity for Rate Limiting using the preferred client IP.
 * It keeps the bean seperated from logic and ensures Seperations of Concerns.
 *
 * Where is it used:
 * Referenced in Application.yml by RequestRateLimiter
 */
@Configuration
public class RateLimitConfig {

    private final ClientIpExtractor clientIpExtractor;

    public RateLimitConfig(ClientIpExtractor clientIpExtractor) {
        this.clientIpExtractor = clientIpExtractor;
    }

    // Keep bean name the same to preserve configuration semantics
    @Bean
    @Primary
    public KeyResolver ipKeyResolver() {
        return exchange -> Mono.just(clientIpExtractor.extract(exchange.getRequest()));
    }

    @Bean
    public RedisRateLimiter redisRateLimiter() {
        return new RedisRateLimiter(10, 20);
    }

    @Bean
    public RouteLocator customRouteLocator(RouteLocatorBuilder builder, RedisRateLimiter rateLimiter) {
        return builder.routes()
                .route("test-rate-limit-programmatic", r -> r
                        .path("/api/test/**")
                        .filters(f -> f
                                .requestRateLimiter(c -> c
                                        .setRateLimiter(rateLimiter)
                                        .setKeyResolver(ipKeyResolver()))
                                .rewritePath("/api/test/(?<segment>.*)", "/fallback/${segment}"))
                        .uri("http://localhost:8080"))
                .build();
    }
}
