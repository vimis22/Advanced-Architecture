# Description of the Architecture of API-Gateway & Orchestrator
I have made an description of the architecture that I have tried to make.

## What is Spring and Why am I using it?
- Inversion Control and Dependency Injection: The Spring Framework controls creation and the lifecycle of objects (beans).
- Modularity: We can use the Spring Framework to create modular applications.
- Configuration over Konvention: We can use Annotation and Configuration-Classes instead of XMR-Filer.
- Testability: Dependency Injection makes it easy to mock and test our application.

## Core Ideas:
- Inversion of Control: The Container creates and controls the objects.
- Dependency Injection: The Container injects dependencies, instead of being manually created inside each Class.

## Spring Annotations:
- @SpringBootApplication: Man kan se det som Main, der kører den samlet operation.
- @Configuration: This is used to define a Configuration-Class.
- @Bean: This method returns a bean, such containers register and can be injected other places.
- @Component: This marks a class, like a component or bean, where it is auto-detected through component scanning.
- @Service: This specialises a component for a service-layer, but functions the same as a component.
- @Repository: Specialization of @Component for the persistent layer and translates persistence-related exceptions into Spring's DataAccessException hiearchy.
- @RestController: @Controller + @ResponseBody by default.
- @Controller: Web Controller: Typically returns views or models.
- @RequestMapping/@GetMapping/@PostMapping: Binds HTTP Method and path to a controller method.

## Spring Cloud Gateway:
- @EnableKafka: Enables Kafka support.
- @KafkaListener: Used to listen to a topic.
- @KafkaHandler: Used to handle a message.
- @KafkaTemplate: Used to send messages to a topic, or produces an API.

## Spring for Kafka:
- @KafkaListener: Used to listen to a topic.
- @KafkaHandler: Used to handle a message.
