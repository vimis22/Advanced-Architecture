# ------- Postgres -------
resource "docker_image" "Postgres_image" {
    name = "postgres:15"
}

resource "docker_container" "postgres" {
    name = "local-postgres"
    image = docker_image.Postgres_image.name

    env = [
        "POSTGRES_USER=admin",
        "POSTGRES_PASSWORD=secret",
        "POSTGRES_DB=appdb"
    ]

    ports {
        internal = 5432
        external = 5432
    }

    restart = "unless-stopped"
}

# ------- InfluxDB -------
resource "docker_image" "influx_image" {
  name = "influxdb:2.7"
}

resource "docker_container" "influx" {
  name  = "local-influx"
  image = docker_image.influx_image.name

  ports {
    internal = 8086
    external = 8086
  }

  restart = "unless-stopped"
}

# ------- Loki -------
resource "docker_image" "loki_image" {
  name = "grafana/loki:2.8.2"
}

resource "docker_container" "loki" {
  name  = "local-loki"
  image = docker_image.loki_image.name

  ports {
    internal = 3100
    external = 3100
  }

  restart = "unless-stopped"
}



