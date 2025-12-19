# -------------------------------
# Cluster variables
# -------------------------------
variable "cluster_name" {
  description = "Name of the Kind cluster"
  type        = string
  default     = "local-dev-cluster"
}

# -------------------------------
# Kafka variables
# -------------------------------
variable "kafka_replicas" {
  description = "Number of Kafka brokers"
  type        = number
  default     = 1
}

variable "kafka_connect_replicas" {
  description = "Number of Kafka Connect workers"
  type        = number
  default     = 1
}

# -------------------------------
# InfluxDB variables
# -------------------------------
variable "influx_admin_user" {
  description = "InfluxDB admin user"
  type        = string
  default     = "admin"
}

variable "influx_admin_password" {
  description = "InfluxDB admin password"
  type        = string
  default     = "admin123"
}

variable "influx_token" {
  description = "InfluxDB token"
  type        = string
  default     = "mytoken"
}

variable "influx_bucket" {
  description = "InfluxDB bucket name"
  type        = string
  default     = "mybucket"
}

variable "influx_org" {
  description = "InfluxDB organization"
  type        = string
  default     = "myorg"
}
