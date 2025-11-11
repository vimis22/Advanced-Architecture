# Grafana
resource "docker_image" "grafana_image" { name = "grafana/grafana:10.0.0" }

resource "docker_container" "grafana" {
  name  = "local-grafana"
  image = docker_image.grafana_image.name
  env = ["GF_SECURITY_ADMIN_USER=admin", "GF_SECURITY_ADMIN_PASSWORD=secret"]
  ports { 
    internal = 3000 
    external = 3000 
    }
  depends_on = [docker_container.loki, docker_container.postgres, docker_container.influx]
  restart = "unless-stopped"
}

# Prometheus
resource "docker_image" "prom_image" { name = "prom/prometheus:v2.46.0" }

resource "docker_container" "prometheus" {
  name  = "local-prometheus"
  image = docker_image.prom_image.name
  ports { 
    internal = 9090 
    external = 9090 
    }
  restart = "unless-stopped"
}
