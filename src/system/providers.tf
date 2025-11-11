terraform {
    required_version = ">= 1.0.0"

    required_providers {
        docker = {
            source = "kreuzwerker/docker"
            version = "~> 3.0"
        }
        kubernetes = {
            source = "hashicorp/kubernetes"
            version = "~> 2.0"
        }
        helm = {
            source = "hashicorp/helm"
            version = "~> 2.0"
        }
    }
}

provider "docker" {
    # Uses local Docker daemon by default
}

provider "kubernetes" {
    config_path = "~/.kube/config" # Will become usable after kind cluster creation
}

provider "helm" {
    kubernetes = {
      config_path = "~/.kube/config"
    }
}
