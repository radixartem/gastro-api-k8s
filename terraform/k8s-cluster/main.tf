terraform {
  required_version = ">= 1.0"
  
  backend "s3" {
    bucket  = "gastro-terraform-state"
    key     = "k8s-cluster/terraform.tfstate"
    region  = "hel1"
    endpoint = "https://hel1.your-objectstorage.com"  # Hetzner Object Storage
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_requesting_account_id  = true
    skip_metadata_api_check     = true

  }
}

provider "hcloud" {
  token = var.hcloud_token
}

# Kubernetes cluster using Hetzner KaaS (Managed Kubernetes)
resource "hcloud_kubernetes_cluster" "gastro_cluster" {
  name       = "gastro-prod-cluster"
  location   = var.location
  version    = "1.28"
  
  # Node pool for workloads
  node_pool {
    name         = "worker-pool"
    server_type  = "cx23"
    location     = var.location
    count        = 3  # 3 nodes for HA
    
    # Auto-scaling
    autoscaling {
      min_node_count = 2
      max_node_count = 5
    }
  }
  
  # Node pool for system components
  node_pool {
    name         = "system-pool"
    server_type  = "cx23"
    location     = var.location
    count        = 1
    labels = {
      node-role.kubernetes.io/system = "true"
    }
    taints = [
      {
        key    = "node-role.kubernetes.io/system"
        value  = "true"
        effect = "NoSchedule"
      }
    ]
  }
  
  # Network configuration
  network_id = hcloud_network.gastro_network.id
}

# Network for cluster
resource "hcloud_network" "gastro_network" {
  name     = "gastro-network"
  ip_range = "10.0.0.0/16"
}

# Subnet for the cluster
resource "hcloud_network_subnet" "gastro_subnet" {
  network_id   = hcloud_network.gastro_network.id
  type         = "cloud"
  network_zone = "eu-central"
  ip_range     = "10.0.1.0/24"
}

# Firewall rules for security
resource "hcloud_firewall" "gastro_firewall" {
  name = "gastro-cluster-firewall"
  
  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "80"
    source_ips = ["0.0.0.0/0"]
  }
  
  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "443"
    source_ips = ["0.0.0.0/0"]
  }
  
  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "22"
    source_ips = [var.admin_ip]  # Только ваш IP для SSH
  }
  
  rule {
    direction  = "in"
    protocol   = "icmp"
    source_ips = ["0.0.0.0/0"]
  }
}

resource "hcloud_firewall_attachment" "cluster_attachment" {
  firewall_id = hcloud_firewall.gastro_firewall.id
  
  label_selector {
    selector = "hcloud-k3s-node=true"
  }
}

# Outputs
output "kubeconfig" {
  value     = hcloud_kubernetes_cluster.gastro_cluster.kubeconfig
  sensitive = true
}

output "cluster_endpoint" {
  value = hcloud_kubernetes_cluster.gastro_cluster.endpoint
}

output "cluster_id" {
  value = hcloud_kubernetes_cluster.gastro_cluster.id
}