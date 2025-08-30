# ByteBook Platform Makefile

# Variables
DOCKER_COMPOSE = docker-compose
DOTNET = dotnet
PROJECT_NAME = bytebook
API_PROJECT = ByteBook.Api
INFRASTRUCTURE_PROJECT = ByteBook.Infrastructure

# Colors for output
RED = \033[0;31m
GREEN = \033[0;32m
YELLOW = \033[1;33m
BLUE = \033[0;34m
NC = \033[0m # No Color

.PHONY: help build test clean run stop logs migrate seed docker-build docker-up docker-down

# Default target
help: ## Show this help message
	@echo "$(BLUE)ByteBook Platform Development Commands$(NC)"
	@echo ""
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "$(GREEN)%-20s$(NC) %s\n", $$1, $$2}' $(MAKEFILE_LIST)

# Development Commands
build: ## Build the solution
	@echo "$(YELLOW)Building solution...$(NC)"
	$(DOTNET) build

test: ## Run all tests
	@echo "$(YELLOW)Running tests...$(NC)"
	$(DOTNET) test --verbosity normal

test-watch: ## Run tests in watch mode
	@echo "$(YELLOW)Running tests in watch mode...$(NC)"
	$(DOTNET) test --watch

clean: ## Clean build artifacts
	@echo "$(YELLOW)Cleaning solution...$(NC)"
	$(DOTNET) clean
	@echo "$(GREEN)Clean completed$(NC)"

restore: ## Restore NuGet packages
	@echo "$(YELLOW)Restoring packages...$(NC)"
	$(DOTNET) restore

run: ## Run the API locally
	@echo "$(YELLOW)Starting API...$(NC)"
	$(DOTNET) run --project $(API_PROJECT)

run-watch: ## Run the API with hot reload
	@echo "$(YELLOW)Starting API with hot reload...$(NC)"
	$(DOTNET) watch --project $(API_PROJECT)

# Database Commands
migrate: ## Run database migrations
	@echo "$(YELLOW)Running database migrations...$(NC)"
	$(DOTNET) ef database update --project $(INFRASTRUCTURE_PROJECT) --startup-project $(API_PROJECT)

migrate-add: ## Add a new migration (usage: make migrate-add NAME=MigrationName)
	@echo "$(YELLOW)Adding migration: $(NAME)$(NC)"
	$(DOTNET) ef migrations add $(NAME) --project $(INFRASTRUCTURE_PROJECT) --startup-project $(API_PROJECT)

migrate-remove: ## Remove the last migration
	@echo "$(YELLOW)Removing last migration...$(NC)"
	$(DOTNET) ef migrations remove --project $(INFRASTRUCTURE_PROJECT) --startup-project $(API_PROJECT)

seed: ## Seed the database with sample data
	@echo "$(YELLOW)Seeding database...$(NC)"
	$(DOTNET) run --project $(INFRASTRUCTURE_PROJECT)/Scripts/TestDatabaseSetup.cs

# Docker Commands
docker-build: ## Build Docker images
	@echo "$(YELLOW)Building Docker images...$(NC)"
	$(DOCKER_COMPOSE) build

docker-up: ## Start all services with Docker Compose
	@echo "$(YELLOW)Starting services...$(NC)"
	$(DOCKER_COMPOSE) up -d
	@echo "$(GREEN)Services started. API available at http://localhost:8080$(NC)"
	@echo "$(GREEN)PgAdmin available at http://localhost:8082 (admin@bytebook.com / admin123)$(NC)"

docker-down: ## Stop all services
	@echo "$(YELLOW)Stopping services...$(NC)"
	$(DOCKER_COMPOSE) down

docker-logs: ## Show logs from all services
	$(DOCKER_COMPOSE) logs -f

docker-logs-api: ## Show API logs
	$(DOCKER_COMPOSE) logs -f api

docker-restart: ## Restart all services
	@echo "$(YELLOW)Restarting services...$(NC)"
	$(DOCKER_COMPOSE) restart

docker-clean: ## Clean up Docker resources
	@echo "$(YELLOW)Cleaning Docker resources...$(NC)"
	$(DOCKER_COMPOSE) down -v --remove-orphans
	docker system prune -f

# Development Environment
dev-setup: ## Set up development environment
	@echo "$(YELLOW)Setting up development environment...$(NC)"
	$(DOTNET) restore
	$(DOTNET) build
	$(DOCKER_COMPOSE) up -d postgres redis
	@echo "$(GREEN)Development environment ready!$(NC)"

dev-reset: ## Reset development environment
	@echo "$(YELLOW)Resetting development environment...$(NC)"
	$(DOCKER_COMPOSE) down -v
	$(DOTNET) clean
	$(DOTNET) restore
	$(DOTNET) build
	$(DOCKER_COMPOSE) up -d
	@echo "$(GREEN)Development environment reset!$(NC)"

# Production Commands
prod-build: ## Build for production
	@echo "$(YELLOW)Building for production...$(NC)"
	$(DOTNET) publish $(API_PROJECT) -c Release -o ./publish

# Health Checks
health: ## Check service health
	@echo "$(YELLOW)Checking service health...$(NC)"
	@curl -f http://localhost:8080/health || echo "$(RED)API is not healthy$(NC)"

# Utility Commands
format: ## Format code
	@echo "$(YELLOW)Formatting code...$(NC)"
	$(DOTNET) format

lint: ## Run code analysis
	@echo "$(YELLOW)Running code analysis...$(NC)"
	$(DOTNET) build --verbosity normal

install-tools: ## Install required .NET tools
	@echo "$(YELLOW)Installing .NET tools...$(NC)"
	$(DOTNET) tool install --global dotnet-ef
	$(DOTNET) tool install --global dotnet-format
	@echo "$(GREEN)Tools installed$(NC)"

# Quick start
quick-start: build docker-up migrate ## Quick start for new developers
	@echo "$(GREEN)ByteBook Platform is ready!$(NC)"
	@echo "$(BLUE)API: http://localhost:8080$(NC)"
	@echo "$(BLUE)PgAdmin: http://localhost:8082$(NC)"
	@echo "$(BLUE)Run 'make logs' to see service logs$(NC)"