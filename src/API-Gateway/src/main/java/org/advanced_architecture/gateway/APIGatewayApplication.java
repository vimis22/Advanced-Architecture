package org.advanced_architecture.gateway;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * The API-Gateway Spring Boot Application Entry Point.
 * The purpose of this class is to bootstrap the Spring Application Context.
 * This starts the Spring Cloud Gateway Runtime.
 */
@SpringBootApplication
public class APIGatewayApplication {
    public static void main(String[] args) {
        SpringApplication.run(APIGatewayApplication.class, args);
    }
}
