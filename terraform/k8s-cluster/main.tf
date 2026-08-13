locals {
  cluster_name = var.cluster_name

  common_labels = {
    project     = "gastro-api"
    environment = var.environment
    cluster     = var.cluster_name
    managed_by  = "opentofu"
  }

  k3s_server_labels = merge(
    local.common_labels,
    {
      role = "k3s-server"
    }
  )

  k3s_agent_labels = merge(
    local.common_labels,
    {
      role = "k3s-agent"
    }
  )
}

# ------------------------------------------------------------
# Private network
# ------------------------------------------------------------

resource "hcloud_network" "cluster" {
  name     = "${var.cluster_name}-network"
  ip_range = var.network_ip_range

  labels = local.common_labels
}

resource "hcloud_network_subnet" "cluster" {
  network_id   = hcloud_network.cluster.id
  type         = "cloud"
  network_zone = var.network_zone
  ip_range     = var.network_subnet_ip_range
}

# ------------------------------------------------------------
# Firewall
# ------------------------------------------------------------

resource "hcloud_firewall" "cluster" {
  name = "${var.cluster_name}-firewall"

  labels = local.common_labels

  # SSH only from administrator IP
  rule {
    direction   = "in"
    protocol    = "tcp"
    port        = "22"
    source_ips  = [var.admin_ip]
    description = "SSH administration"
  }

  # HTTP
  rule {
    direction   = "in"
    protocol    = "tcp"
    port        = "80"
    source_ips  = ["0.0.0.0/0", "::/0"]
    description = "HTTP"
  }

  # HTTPS
  rule {
    direction   = "in"
    protocol    = "tcp"
    port        = "443"
    source_ips  = ["0.0.0.0/0", "::/0"]
    description = "HTTPS"
  }

  # Kubernetes API:
  # - administrator access from the public Internet
  # - access from the private network for K3s nodes and Hetzner Load Balancer
  rule {
    direction = "in"
    protocol  = "tcp"
    port      = "6443"
    source_ips = [
      var.admin_ip,
      var.network_ip_range
    ]
    description = "Kubernetes API from administrator and private network"
  }

  # K3s embedded etcd between control-plane nodes.
  rule {
    direction   = "in"
    protocol    = "tcp"
    port        = "2379-2380"
    source_ips  = [var.network_ip_range]
    description = "K3s embedded etcd"
  }

  # Kubelet metrics/API between cluster nodes.
  rule {
    direction   = "in"
    protocol    = "tcp"
    port        = "10250"
    source_ips  = [var.network_ip_range]
    description = "Kubelet"
  }

  # Flannel VXLAN.
  rule {
    direction   = "in"
    protocol    = "udp"
    port        = "8472"
    source_ips  = [var.network_ip_range]
    description = "Flannel VXLAN"
  }

  # Allow ICMP for diagnostics.
  rule {
    direction   = "in"
    protocol    = "icmp"
    source_ips  = [var.network_ip_range]
    description = "Private network ICMP"
  }
}

# ------------------------------------------------------------
# K3s control-plane nodes
# ------------------------------------------------------------

resource "hcloud_server" "k3s_server" {
  count = var.k3s_server_count

  name        = format("%s-server-%02d", var.cluster_name, count.index + 1)
  server_type = var.k3s_server_type
  image       = var.server_image
  location    = var.location

  labels = local.k3s_server_labels

  firewall_ids = [
    hcloud_firewall.cluster.id
  ]

  public_net {
    ipv4_enabled = true
    ipv6_enabled = true
  }

  network {
    network_id = hcloud_network.cluster.id
    ip         = cidrhost(var.network_subnet_ip_range, 10 + count.index)
  }

  depends_on = [
    hcloud_network_subnet.cluster
  ]
}

# ------------------------------------------------------------
# K3s worker nodes
# ------------------------------------------------------------

resource "hcloud_server" "k3s_agent" {
  count = var.k3s_agent_count

  name        = format("%s-agent-%02d", var.cluster_name, count.index + 1)
  server_type = var.k3s_agent_type
  image       = var.server_image
  location    = var.location

  labels = local.k3s_agent_labels

  firewall_ids = [
    hcloud_firewall.cluster.id
  ]

  public_net {
    ipv4_enabled = true
    ipv6_enabled = true
  }

  network {
    network_id = hcloud_network.cluster.id
    ip         = cidrhost(var.network_subnet_ip_range, 30 + count.index)
  }

  depends_on = [
    hcloud_network_subnet.cluster
  ]
}

# ------------------------------------------------------------
# Hetzner Load Balancer
# ------------------------------------------------------------

resource "hcloud_load_balancer" "cluster" {
  name               = "${var.cluster_name}-lb"
  load_balancer_type = var.load_balancer_type
  location           = var.location

  labels = local.common_labels
}

resource "hcloud_load_balancer_network" "cluster" {
  load_balancer_id = hcloud_load_balancer.cluster.id
  network_id       = hcloud_network.cluster.id
}

# ------------------------------------------------------------
# Kubernetes API service
#
# Used by K3s control-plane nodes.
# ------------------------------------------------------------

resource "hcloud_load_balancer_service" "kubernetes_api" {
  load_balancer_id = hcloud_load_balancer.cluster.id

  protocol         = "tcp"
  listen_port      = 6443
  destination_port = 6443

  health_check {
    protocol = "tcp"
    port     = 6443
    interval = 10
    timeout  = 5
    retries  = 3
  }
}

resource "hcloud_load_balancer_target" "k3s_servers" {
  for_each = {
    for server in hcloud_server.k3s_server :
    server.name => server.id
  }

  type             = "server"
  load_balancer_id = hcloud_load_balancer.cluster.id
  server_id        = each.value

  use_private_ip = true

  depends_on = [
    hcloud_load_balancer_network.cluster
  ]
}

# ------------------------------------------------------------
# HTTP ingress service
#
# Targets are all worker nodes.
# ------------------------------------------------------------

resource "hcloud_load_balancer_service" "http" {
  load_balancer_id = hcloud_load_balancer.cluster.id

  protocol         = "tcp"
  listen_port      = 80
  destination_port = 80

  health_check {
    protocol = "tcp"
    port     = 80
    interval = 15
    timeout  = 5
    retries  = 3
  }
}

# ------------------------------------------------------------
# HTTPS ingress service
# ------------------------------------------------------------

resource "hcloud_load_balancer_service" "https" {
  load_balancer_id = hcloud_load_balancer.cluster.id

  protocol         = "tcp"
  listen_port      = 443
  destination_port = 443

  health_check {
    protocol = "tcp"
    port     = 443
    interval = 15
    timeout  = 5
    retries  = 3
  }
}

resource "hcloud_load_balancer_target" "k3s_agents" {
  for_each = {
    for server in hcloud_server.k3s_agent :
    server.name => server.id
  }

  type             = "server"
  load_balancer_id = hcloud_load_balancer.cluster.id
  server_id        = each.value

  use_private_ip = true

  depends_on = [
    hcloud_load_balancer_network.cluster
  ]
}