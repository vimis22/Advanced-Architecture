package org.advanced_architecture.gateway;

import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.http.server.reactive.ServerHttpRequest;

import java.time.Instant;
import java.util.Map;

@RestController
@RequestMapping(path = "/fallback", produces = MediaType.APPLICATION_JSON_VALUE)
public class FallbackController {

    @RequestMapping(path = "/orchestrator", method = {RequestMethod.GET, RequestMethod.POST})
    public ResponseEntity<Map<String, Object>> orchestratorFallback(ServerHttpRequest request) {
        Map<String, Object> body = Map.of(
                "service", "orchestrator",
                "message", "Orchestrator is temporarily unavailable",
                "path", request.getPath().value(),
                "timestamp", Instant.now().toString()
        );
        return new ResponseEntity<>(body, HttpStatus.SERVICE_UNAVAILABLE);
    }

    @RequestMapping(path = "/scheduler", method = {RequestMethod.GET, RequestMethod.POST})
    public ResponseEntity<Map<String, Object>> schedulerFallback(ServerHttpRequest request) {
        Map<String, Object> body = Map.of(
                "service", "scheduler",
                "message", "Scheduler is temporarily unavailable",
                "path", request.getPath().value(),
                "timestamp", Instant.now().toString()
        );
        return new ResponseEntity<>(body, HttpStatus.SERVICE_UNAVAILABLE);
    }
}
