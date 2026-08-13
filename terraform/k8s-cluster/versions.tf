terraform {
  required_version = ">= 1.12.0, < 1.13.0"

  required_providers {
    hcloud = {
      source  = "hetznercloud/hcloud"
      version = "1.66.1"
    }
  }
}