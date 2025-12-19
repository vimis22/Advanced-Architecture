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
 * Configures rate limiting for the API Gateway using Redis.
 *
 * Responsibilities:
 * - Defines IP-based key resolver for identifying clients (delegates to {@link ClientIpExtractor})
 * - Configures Redis rate limiter with 10 requests/second and burst capacity of 20
 * - Provides test route for validating rate limiting behavior
 *
 * Usage:
 * Referenced in application.yml by RequestRateLimiter filter
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
