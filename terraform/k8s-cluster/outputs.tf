output "cluster_name" {
  description = "Kubernetes cluster logical name"
  value       = var.cluster_name
}

output "load_balancer_ipv4" {
  description = "Public IPv4 address of the Hetzner Load Balancer"
  value       = hcloud_load_balancer.cluster.ipv4
}

output "load_balancer_ipv6" {
  description = "Public IPv6 address of the Hetzner Load Balancer"
  value       = hcloud_load_balancer.cluster.ipv6
}

output "kubernetes_api_endpoint" {
  description = "Kubernetes API endpoint through the Load Balancer"
  value       = "https://${hcloud_load_balancer.cluster.ipv4}:6443"
}

output "k3s_server_public_ips" {
  description = "Public IPv4 addresses of K3s control-plane nodes"

  value = {
    for server in hcloud_server.k3s_server :
    server.name => server.ipv4_address
  }
}

output "k3s_server_private_ips" {
  description = "Private IPv4 addresses of K3s control-plane nodes"

  value = {
    for server in hcloud_server.k3s_server :
    server.name => one(server.network).ip
  }
}

output "k3s_agent_public_ips" {
  description = "Public IPv4 addresses of K3s worker nodes"

  value = {
    for server in hcloud_server.k3s_agent :
    server.name => server.ipv4_address
  }
}

output "k3s_agent_private_ips" {
  description = "Private IPv4 addresses of K3s worker nodes"

  value = {
    for server in hcloud_server.k3s_agent :
    server.name => one(server.network).ip
  }
}

output "network_id" {
  description = "Hetzner private network ID"
  value       = hcloud_network.cluster.id
}

output "firewall_id" {
  description = "Hetzner firewall ID"
  value       = hcloud_firewall.cluster.id
}