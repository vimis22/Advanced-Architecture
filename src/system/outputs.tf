output "postgres" { value = "Postgres -> localhost:5432 user=admin password=secret db=appdb" }
output "influx"   { value = "InfluxDB -> http://localhost:8086" }
output "loki"     { value = "Loki -> http://localhost:3100" }
output "grafana"  { value = "Grafana -> http://localhost:3000 (admin/secret)" }
output "prometheus" { value = "Prometheus -> http://localhost:9090" }
output "kafka"    { value = "Kafka -> PLAINTEXT://localhost:9092" }
output "kind_cluster" { value = "Kind cluster: local-kind-cluster (kubeconfig at ~/.kube/config)" }
