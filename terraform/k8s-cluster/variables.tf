variable "hcloud_token" {
  description = "Hetzner Cloud API Token"
  sensitive   = true
}

variable "location" {
  description = "Hetzner location"
  default     = "hel1"  # Helsinki
}

variable "admin_ip" {
  description = "IP address for admin access (SSH)"
  default     = "0.0.0.0/0"
}