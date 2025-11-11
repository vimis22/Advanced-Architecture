# Kafka Connect Image
resource "docker_image" "kafka_connect_image" {
  # Use the Confluent Connect image. Ensure the version matches your Kafka image (e.g., 7.6.0)
  name = "confluentinc/cp-kafka-connect:7.6.0" 
}

# Kafka Connect Container (Distributed Worker Mode)
resource "docker_container" "kafka_connect" {
  name  = "local-kafka-connect"
  image = docker_image.kafka_connect_image.name

  # Expose the Connect REST API port (8083) to your host machine
  ports {
    internal = 8083
    external = 8083 
  }

  env = [
    # --- REQUIRED CORE CONFIGURATION ---
    # Connect this worker to your Kafka broker
    "CONNECT_BOOTSTRAP_SERVERS=local-kafka:9092", 
    
    # The URL to access the Connect REST API from outside the container
    "CONNECT_REST_ADVERTISED_HOST_NAME=localhost",
    "CONNECT_REST_PORT=8083",
    
    # Internal topics used by the Connect worker to store metadata
    # The replication factor is set to 1 since you only have one Kafka broker.
    "CONNECT_GROUP_ID=compose-connect-group",
    "CONNECT_CONFIG_STORAGE_TOPIC=kafka_connect_configs",
    "CONNECT_OFFSET_STORAGE_TOPIC=kafka_connect_offsets",
    "CONNECT_STATUS_STORAGE_TOPIC=kafka_connect_statuses",
    "CONNECT_CONFIG_STORAGE_REPLICATION_FACTOR=1",
    "CONNECT_OFFSET_STORAGE_REPLICATION_FACTOR=1",
    "CONNECT_STATUS_STORAGE_REPLICATION_FACTOR=1",
    
    # --- DATA FORMAT CONFIGURATION ---
    # How Kafka Connect expects data to be formatted (JSON is common for local/dev)
    "CONNECT_KEY_CONVERTER=org.apache.kafka.connect.json.JsonConverter",
    "CONNECT_VALUE_CONVERTER=org.apache.kafka.connect.json.JsonConverter",
    "CONNECT_INTERNAL_KEY_CONVERTER=org.apache.kafka.connect.json.JsonConverter",
    "CONNECT_INTERNAL_VALUE_CONVERTER=org.apache.kafka.connect.json.JsonConverter",
    "CONNECT_INTERNAL_KEY_CONVERTER_SCHEMAS_ENABLE=false",
    "CONNECT_INTERNAL_VALUE_CONVERTER_SCHEMAS_ENABLE=false",
    
    # --- CONNECTOR PLUGIN PATH ---
    # Where Connect will look for connector JAR files.
    "CONNECT_PLUGIN_PATH=/usr/share/java,/usr/share/confluent-hub-components"
  ]

  # Ensure Connect starts only after the Kafka broker is running
  depends_on = [
    docker_container.kafka 
  ]
  
  restart = "unless-stopped"
}