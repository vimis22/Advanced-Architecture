terraform {
  required_providers {
    kind = {
      source  = "tehcyx/kind"
      version = "~> 0.2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.8"
    }
    local = {
      source  = "hashicorp/local"
      version = "~> 2.4"
    }
  }
}

# -------------------------------
# Kind cluster
# -------------------------------
resource "kind_cluster" "local" {
  name           = var.cluster_name
  wait_for_ready = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"
    }
    node { role = "worker" }
    node { role = "worker" }
  }
}

# -------------------------------
# Save kubeconfig locally
# -------------------------------
resource "local_file" "kubeconfig" {
  filename = "${path.module}/kube-config.yaml"
  content  = kind_cluster.local.kubeconfig
}

# -------------------------------
# Helm provider using kubeconfig
# -------------------------------
provider "helm" {
  kubernetes {
    config_path = local_file.kubeconfig.filename
  }
}

# -------------------------------
# Strimzi Kafka Operator via Helm
# -------------------------------
resource "helm_release" "strimzi" {
  name             = "strimzi"
  repository       = "https://strimzi.io/charts/"
  chart            = "strimzi-kafka-operator"
  namespace        = "kafka"
  create_namespace = true

  depends_on = [kind_cluster.local]
}

# -------------------------------
# Apply Kafka CRD YAML using local-exec
# -------------------------------
resource "null_resource" "apply_kafka_yaml" {
  depends_on = [helm_release.strimzi]

  provisioner "local-exec" {
    command = "kubectl apply -f kafka-cluster.yaml --kubeconfig ${local_file.kubeconfig.filename}"
  }
}

# -------------------------------
# Apply Kafka Connect CRD YAML
# -------------------------------
resource "null_resource" "apply_connect_yaml" {
  depends_on = [null_resource.apply_kafka_yaml]

  provisioner "local-exec" {
    command = "kubectl apply -f kafka-connect.yaml --kubeconfig ${local_file.kubeconfig.filename}"
  }
}

# -------------------------------
# InfluxDB Helm chart
# -------------------------------
resource "helm_release" "influxdb" {
  name             = "influxdb"
  repository       = "https://helm.influxdata.com/"
  chart            = "influxdb2"
  namespace        = "metrics"
  create_namespace = true
  values           = [file("${path.module}/influxdb-config.yaml")]
}

# -------------------------------
# Outputs
# -------------------------------
output "kubeconfig_file" {
  value = local_file.kubeconfig.filename
}

output "kafka_bootstrap_servers" {
  value = "my-cluster-kafka:9092"
}

output "kafka_connect_url" {
  value = "http://my-connect.kafka.svc.cluster.local:8083"
}

output "influxdb_url" {
  value = "http://influxdb.metrics.svc.cluster.local:8086"
}

output "influxdb_admin_user" {
  value = var.influx_admin_user
}

output "influxdb_admin_token" {
  value = var.influx_token
}

output "influxdb_bucket" {
  value = var.influx_bucket
}

output "influxdb_org" {
  value = var.influx_org
}
