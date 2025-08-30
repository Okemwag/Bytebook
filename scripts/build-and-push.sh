#!/bin/bash

# Build and Push Docker Images Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
REGISTRY=${REGISTRY:-"your-registry.com"}
IMAGE_NAME="bytebook/api"
VERSION=${1:-$(git rev-parse --short HEAD 2>/dev/null || echo "latest")}
DOCKERFILE="Dockerfile"

echo -e "${BLUE}ByteBook Docker Build and Push Script${NC}"
echo -e "${BLUE}====================================${NC}"

# Check if Docker is available
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker is not installed or not in PATH${NC}"
    exit 1
fi

# Check if we're in the right directory
if [ ! -f "$DOCKERFILE" ]; then
    echo -e "${RED}Dockerfile not found in current directory${NC}"
    exit 1
fi

echo -e "${YELLOW}Registry: ${REGISTRY}${NC}"
echo -e "${YELLOW}Image: ${IMAGE_NAME}${NC}"
echo -e "${YELLOW}Version: ${VERSION}${NC}"

# Build the image
echo -e "${YELLOW}Building Docker image...${NC}"
docker build -t ${IMAGE_NAME}:${VERSION} -f ${DOCKERFILE} .
docker tag ${IMAGE_NAME}:${VERSION} ${IMAGE_NAME}:latest

# Tag for registry
if [ "$REGISTRY" != "your-registry.com" ]; then
    echo -e "${YELLOW}Tagging for registry...${NC}"
    docker tag ${IMAGE_NAME}:${VERSION} ${REGISTRY}/${IMAGE_NAME}:${VERSION}
    docker tag ${IMAGE_NAME}:${VERSION} ${REGISTRY}/${IMAGE_NAME}:latest
    
    # Push to registry
    echo -e "${YELLOW}Pushing to registry...${NC}"
    docker push ${REGISTRY}/${IMAGE_NAME}:${VERSION}
    docker push ${REGISTRY}/${IMAGE_NAME}:latest
    
    echo -e "${GREEN}Images pushed successfully!${NC}"
    echo -e "${BLUE}${REGISTRY}/${IMAGE_NAME}:${VERSION}${NC}"
    echo -e "${BLUE}${REGISTRY}/${IMAGE_NAME}:latest${NC}"
else
    echo -e "${YELLOW}Registry not configured. Images built locally only.${NC}"
    echo -e "${BLUE}${IMAGE_NAME}:${VERSION}${NC}"
    echo -e "${BLUE}${IMAGE_NAME}:latest${NC}"
fi

echo -e "${GREEN}Build script completed!${NC}"