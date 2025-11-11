resource "docker_image" "kafka_image" {
  name = "apache/kafka:4.1.0" 
}

resource "docker_container" "kafka" {
  name  = "local-kafka"
  image = docker_image.kafka_image.name

  env = [
    # KRaft mode configuration
    "KAFKA_NODE_ID=1",                          
    "KAFKA_PROCESS_ROLES=controller,broker",     
    "KAFKA_LISTENERS=PLAINTEXT://:9092,CONTROLLER://:9093",
    "KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092",
    "KAFKA_CONTROLLER_QUORUM_VOTERS=1@local-kafka:9093",
    "KAFKA_LOG_DIRS=/var/lib/kafka/data", 
    
    # Required Internal Settings
    "KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER",
    "KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT",
    "KAFKA_INTER_BROKER_LISTENER_NAME=PLAINTEXT",
    "KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1", 
    "KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1",
    "KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1"    
  ]

  ports {
    internal = 9092
    external = 9092
  }

  restart = "unless-stopped"
}