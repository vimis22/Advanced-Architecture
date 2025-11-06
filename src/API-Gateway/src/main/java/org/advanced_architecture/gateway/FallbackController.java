package org.advanced_architecture.gateway;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import java.util.Map;

@RestController
@RequestMapping("/fallback")
public class FallbackController {
    @PostMapping("/orchestrator")
    @GetMapping("/orchestrator")
    public ResponseEntity<Map<String, String>> orchestratorFallback() {
        return new ResponseEntity<>(Map.of("message", "Orchestrator is down"), HttpStatus.SERVICE_UNAVAILABLE);
    }

    @PostMapping("/scheduler")
    @GetMapping("/scheduler")
    public ResponseEntity<Map<String, String>> schedulerFallback() {
        return new ResponseEntity<>(Map.of("message", "Scheduler is down"), HttpStatus.SERVICE_UNAVAILABLE);
    }
}
