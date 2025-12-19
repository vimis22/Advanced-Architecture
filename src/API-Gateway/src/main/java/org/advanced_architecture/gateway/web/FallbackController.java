package org.advanced_architecture.gateway.web;

import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.http.server.reactive.ServerHttpRequest;

import java.time.Instant;
import java.util.Map;

/**
 * The purpose of this class is to handle fallback routes when circuit breaker opens, and thereby give fallback responses.
 *
 * Responsibilities:
 * - Returns simple JSON, when downstream services are unavailable.
 * - Provides endpoints under "/fallback" consumed by CircuitBreaker from fallBackUri
 *
 * Behavior:
 * - Produces 503 Service-Unavailable responses with Metadata.
 * - Ensures to keep structure consistent for clients.
 *
 */
@RestController
@RequestMapping(path = "/fallback", produces = MediaType.APPLICATION_JSON_VALUE)
public class FallbackController {

    @RequestMapping(path = "/orchestrator", method = {RequestMethod.GET, RequestMethod.POST, RequestMethod.PUT, RequestMethod.DELETE})
    public ResponseEntity<Map<String, Object>> orchestratorFallback(ServerHttpRequest request) {
        Map<String, Object> body = Map.of(
                "service", "orchestrator",
                "status", "unavailable",
                "error", "Service Unavailable",
                "message", "The Orchestrator service is temporarily unavailable. Please try again later.",
                "path", request.getPath().value(),
                "timestamp", Instant.now().toString(),
                "suggestion", "Check service health or contact system administrator if the problem persists"
        );
        return new ResponseEntity<>(body, HttpStatus.SERVICE_UNAVAILABLE);
    }

    @RequestMapping(path = "/scheduler", method = {RequestMethod.GET, RequestMethod.POST, RequestMethod.PUT, RequestMethod.DELETE})
    public ResponseEntity<Map<String, Object>> schedulerFallback(ServerHttpRequest request) {
        Map<String, Object> body = Map.of(
                "service", "scheduler",
                "status", "unavailable",
                "error", "Service Unavailable",
                "message", "The Scheduler service is temporarily unavailable. Please try again later.",
                "path", request.getPath().value(),
                "timestamp", Instant.now().toString(),
                "suggestion", "Check service health or contact system administrator if the problem persists"
        );
        return new ResponseEntity<>(body, HttpStatus.SERVICE_UNAVAILABLE);
    }

    @RequestMapping(path = "/default", method = {RequestMethod.GET, RequestMethod.POST, RequestMethod.PUT, RequestMethod.DELETE})
    public ResponseEntity<Map<String, Object>> defaultFallback(ServerHttpRequest request) {
        Map<String, Object> body = Map.of(
                "service", "unknown",
                "status", "unavailable",
                "error", "Service Unavailable",
                "message", "The requested service is temporarily unavailable. Please try again later.",
                "path", request.getPath().value(),
                "timestamp", Instant.now().toString(),
                "suggestion", "Contact system administrator if the problem persists"
        );
        return new ResponseEntity<>(body, HttpStatus.SERVICE_UNAVAILABLE);
    }

    @RequestMapping(path = "/test", method = RequestMethod.GET)
    public ResponseEntity<Map<String, Object>> testEndpoint(ServerHttpRequest request) {
        Map<String, Object> body = Map.of(
                "status", "ok",
                "message", "Rate limiting test endpoint",
                "path", request.getPath().value(),
                "timestamp", Instant.now().toString()
        );
        return ResponseEntity.ok(body);
    }
}
