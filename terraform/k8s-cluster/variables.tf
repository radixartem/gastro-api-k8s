variable "hcloud_token" {
  description = "Hetzner Cloud API token"
  type        = string
  sensitive   = true
}

variable "cluster_name" {
  description = "Logical Kubernetes cluster name"
  type        = string
  default     = "gastro-prod"
}

variable "environment" {
  description = "Deployment environment"
  type        = string
  default     = "production"
}

variable "location" {
  description = "Hetzner Cloud location"
  type        = string
  default     = "fsn1"
}

variable "network_zone" {
  description = "Hetzner network zone"
  type        = string
  default     = "eu-central"
}

variable "network_ip_range" {
  description = "CIDR range of the Hetzner private network"
  type        = string
  default     = "10.42.0.0/16"
}

variable "network_subnet_ip_range" {
  description = "CIDR range of the Hetzner private subnet"
  type        = string
  default     = "10.42.1.0/24"
}

variable "admin_ip" {
  description = "Administrator public IPv4 CIDR allowed to access SSH and Kubernetes API"
  type        = string
}

variable "server_image" {
  description = "Operating system image for K3s nodes"
  type        = string
  default     = "ubuntu-24.04"
}

variable "k3s_server_count" {
  description = "Number of K3s control-plane nodes"
  type        = number
  default     = 1

  validation {
    condition     = var.k3s_server_count >= 1
    error_message = "k3s_server_count must be at least 1."
  }
}

variable "k3s_server_type" {
  description = "Hetzner server type for K3s control-plane nodes"
  type        = string
  default     = "cx23"
}

variable "k3s_agent_count" {
  description = "Number of K3s worker nodes"
  type        = number
  default     = 2

  validation {
    condition     = var.k3s_agent_count >= 1
    error_message = "k3s_agent_count must be at least 1."
  }
}

variable "k3s_agent_type" {
  description = "Hetzner server type for K3s worker nodes"
  type        = string
  default     = "cx23"
}

variable "load_balancer_type" {
  description = "Hetzner Cloud Load Balancer type"
  type        = string
  default     = "lb11"
}