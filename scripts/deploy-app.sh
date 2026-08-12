#!/bin/bash
set -e

echo "🚀 Deploying application..."

# Build and push Docker image
echo "📦 Building Docker image..."
IMAGE_TAG="${GITHUB_SHA:-latest}"
IMAGE_NAME="ghcr.io/radixartem/gastro-api:${IMAGE_TAG}"

# Build with commit SHA tag
docker build -t ${IMAGE_NAME} -t ghcr.io/radixartem/gastro-api:latest .
docker push ${IMAGE_NAME}
docker push ghcr.io/radixartem/gastro-api:latest

# Update deployment with new image tag
echo "🔄 Updating Kubernetes manifests..."
sed -i "s|ghcr.io/radixartem/gastro-api:latest|${IMAGE_NAME}|g" k8s/base/deployment.yaml

# Apply manifests
echo "📤 Applying to cluster..."
kubectl apply -f k8s/base/namespace.yaml
kubectl apply -f k8s/base/configmap.yaml
kubectl apply -f k8s/base/secrets.yaml
kubectl apply -f k8s/base/deployment.yaml
kubectl apply -f k8s/base/service.yaml
kubectl apply -f k8s/base/ingress.yaml

# Wait for rollout
echo "⏳ Waiting for rollout..."
kubectl rollout status deployment/gastro-api -n gastro-api

# Get service info
echo "📍 Service information:"
kubectl get svc -n ingress-nginx
kubectl get ingress -n gastro-api

echo "✅ Deployment complete!"
echo "🌐 Application should be available at: https://$(kubectl get ingress -n gastro-api -o jsonpath='{.items[0].spec.rules[0].host}')"