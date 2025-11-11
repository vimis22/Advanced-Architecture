# Kind cluster creation
resource "null_resource" "kind_cluster" {
  provisioner "local-exec" {
    command = <<EOT
if ! kind get clusters | grep -q "^local-kind-cluster$"; then
  kind create cluster --name local-kind-cluster --config kind-cluster-config.yaml
else
  echo "kind cluster 'local-kind-cluster' already exists"
fi
EOT
    interpreter = ["bash", "-c"]
  }
  triggers = { cluster = timestamp() }
}

# Wait for kube ready
resource "null_resource" "wait_for_kube" {
  depends_on = [null_resource.kind_cluster]
  provisioner "local-exec" {
    command = <<EOT
for i in $(seq 1 30); do
  kubectl get nodes && exit 0 || sleep 2
done
echo "Timed out waiting for Kubernetes"
exit 1
EOT
    interpreter = ["bash", "-c"]
  }
}
