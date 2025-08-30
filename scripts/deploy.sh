#!/bin/bash

# ByteBook Platform Deployment Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="bytebook"
IMAGE_TAG=${1:-latest}
REGISTRY=${REGISTRY:-"your-registry.com"}

echo -e "${BLUE}ByteBook Platform Deployment Script${NC}"
echo -e "${BLUE}====================================${NC}"

# Check if kubectl is available
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}kubectl is not installed or not in PATH${NC}"
    exit 1
fi

# Check if we're connected to a cluster
if ! kubectl cluster-info &> /dev/null; then
    echo -e "${RED}Not connected to a Kubernetes cluster${NC}"
    exit 1
fi

echo -e "${YELLOW}Deploying to cluster: $(kubectl config current-context)${NC}"
echo -e "${YELLOW}Image tag: ${IMAGE_TAG}${NC}"

# Create namespace if it doesn't exist
echo -e "${YELLOW}Creating namespace...${NC}"
kubectl apply -f k8s/namespace.yaml

# Apply ConfigMap
echo -e "${YELLOW}Applying ConfigMap...${NC}"
kubectl apply -f k8s/configmap.yaml

# Check if secrets exist
if ! kubectl get secret bytebook-secrets -n $NAMESPACE &> /dev/null; then
    echo -e "${RED}Secret 'bytebook-secrets' not found in namespace '$NAMESPACE'${NC}"
    echo -e "${YELLOW}Please create the secret from the template:${NC}"
    echo -e "${BLUE}cp k8s/secret.yaml.template k8s/secret.yaml${NC}"
    echo -e "${BLUE}# Edit k8s/secret.yaml with your values${NC}"
    echo -e "${BLUE}kubectl apply -f k8s/secret.yaml${NC}"
    exit 1
fi

# Deploy PostgreSQL
echo -e "${YELLOW}Deploying PostgreSQL...${NC}"
kubectl apply -f k8s/postgres.yaml

# Deploy Redis
echo -e "${YELLOW}Deploying Redis...${NC}"
kubectl apply -f k8s/redis.yaml

# Wait for database to be ready
echo -e "${YELLOW}Waiting for PostgreSQL to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app.kubernetes.io/component=database -n $NAMESPACE --timeout=300s

echo -e "${YELLOW}Waiting for Redis to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app.kubernetes.io/component=cache -n $NAMESPACE --timeout=300s

# Update API image tag if provided
if [ "$IMAGE_TAG" != "latest" ]; then
    echo -e "${YELLOW}Updating API image tag to ${IMAGE_TAG}...${NC}"
    sed -i.bak "s|image: bytebook/api:.*|image: ${REGISTRY}/bytebook/api:${IMAGE_TAG}|g" k8s/api.yaml
fi

# Deploy API
echo -e "${YELLOW}Deploying API...${NC}"
kubectl apply -f k8s/api.yaml

# Deploy HPA
echo -e "${YELLOW}Deploying Horizontal Pod Autoscaler...${NC}"
kubectl apply -f k8s/hpa.yaml

# Wait for API deployment to be ready
echo -e "${YELLOW}Waiting for API deployment to be ready...${NC}"
kubectl wait --for=condition=available deployment/bytebook-api -n $NAMESPACE --timeout=300s

# Show deployment status
echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${BLUE}Checking deployment status...${NC}"

kubectl get pods -n $NAMESPACE
echo ""
kubectl get services -n $NAMESPACE
echo ""
kubectl get ingress -n $NAMESPACE

# Get API URL
API_URL=$(kubectl get ingress bytebook-ingress -n $NAMESPACE -o jsonpath='{.spec.rules[0].host}' 2>/dev/null || echo "localhost")
echo -e "${GREEN}API should be available at: https://${API_URL}${NC}"

# Restore original file if we modified it
if [ -f k8s/api.yaml.bak ]; then
    mv k8s/api.yaml.bak k8s/api.yaml
fi

echo -e "${GREEN}Deployment script completed!${NC}"