# Modular Monolith - Enterprise User Management API
# Makefile for development automation

# Variables
PROJECT_NAME = ModularMonolith
API_PROJECT = src/Api/ModularMonolith.Api.csproj
INFRASTRUCTURE_PROJECT = src/Infrastructure/ModularMonolith.Infrastructure.csproj
SOLUTION_FILE = ModularMonolith.sln
DOCKER_COMPOSE_FILE = docker-compose.yml
DOCKER_COMPOSE_PROD = docker-compose.prod.yml
TEST_PROJECTS = tests/**/*.csproj

# Colors for output
GREEN = \033[0;32m
YELLOW = \033[1;33m
RED = \033[0;31m
NC = \033[0m # No Color

# Default target
.DEFAULT_GOAL := help

## Help
help: ## Show this help message
	@echo "$(GREEN)Modular Monolith - Enterprise User Management API$(NC)"
	@echo "$(YELLOW)Available commands:$(NC)"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  $(GREEN)%-20s$(NC) %s\n", $$1, $$2}' $(MAKEFILE_LIST)

## Development
install: ## Install .NET dependencies and tools
	@echo "$(YELLOW)Installing .NET dependencies...$(NC)"
	dotnet restore
	dotnet tool restore
	@echo "$(GREEN)Dependencies installed successfully!$(NC)"

build: ## Build the solution
	@echo "$(YELLOW)Building solution...$(NC)"
	dotnet build $(SOLUTION_FILE) --configuration Release --no-restore
	@echo "$(GREEN)Build completed successfully!$(NC)"

clean: ## Clean build artifacts
	@echo "$(YELLOW)Cleaning build artifacts...$(NC)"
	dotnet clean $(SOLUTION_FILE)
	rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj
	@echo "$(GREEN)Clean completed!$(NC)"

rebuild: clean install build ## Clean, restore, and build

run: ## Run the API locally
	@echo "$(YELLOW)Starting API server...$(NC)"
	dotnet run --project $(API_PROJECT)

run-watch: ## Run the API with hot reload
	@echo "$(YELLOW)Starting API server with hot reload...$(NC)"
	dotnet watch --project $(API_PROJECT)

## Testing
test: ## Run all tests
	@echo "$(YELLOW)Running tests...$(NC)"
	dotnet test --configuration Release --no-build --verbosity normal

test-watch: ## Run tests in watch mode
	@echo "$(YELLOW)Running tests in watch mode...$(NC)"
	dotnet watch test

test-coverage: ## Run tests with coverage report
	@echo "$(YELLOW)Running tests with coverage...$(NC)"
	dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
	@echo "$(GREEN)Coverage report generated in ./coverage$(NC)"

## Database
db-update: ## Update database with latest migrations
	@echo "$(YELLOW)Updating database...$(NC)"
	dotnet ef database update --project $(INFRASTRUCTURE_PROJECT) --startup-project $(API_PROJECT)
	@echo "$(GREEN)Database updated successfully!$(NC)"

db-migration: ## Create a new migration (usage: make db-migration name=MigrationName)
	@echo "$(YELLOW)Creating migration: $(name)...$(NC)"
	dotnet ef migrations add $(name) --project $(INFRASTRUCTURE_PROJECT) --startup-project $(API_PROJECT)
	@echo "$(GREEN)Migration '$(name)' created successfully!$(NC)"

db-drop: ## Drop the database
	@echo "$(RED)Dropping database...$(NC)"
	dotnet ef database drop --project $(INFRASTRUCTURE_PROJECT) --startup-project $(API_PROJECT) --force
	@echo "$(GREEN)Database dropped!$(NC)"

db-reset: db-drop db-update ## Drop and recreate database

## Docker
docker-build: ## Build Docker images
	@echo "$(YELLOW)Building Docker images...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) build
	@echo "$(GREEN)Docker images built successfully!$(NC)"

docker-up: ## Start development environment with Docker
	@echo "$(YELLOW)Starting development environment...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) up -d
	@echo "$(GREEN)Development environment started!$(NC)"
	@echo "$(YELLOW)API: http://localhost:8080$(NC)"
	@echo "$(YELLOW)Swagger: http://localhost:8080/swagger$(NC)"

docker-down: ## Stop development environment
	@echo "$(YELLOW)Stopping development environment...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) down
	@echo "$(GREEN)Development environment stopped!$(NC)"

docker-logs: ## Show Docker logs
	docker-compose -f $(DOCKER_COMPOSE_FILE) logs -f

docker-restart: docker-down docker-up ## Restart development environment

## Production Docker
docker-prod-build: ## Build production Docker images
	@echo "$(YELLOW)Building production Docker images...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) -f $(DOCKER_COMPOSE_PROD) build
	@echo "$(GREEN)Production images built successfully!$(NC)"

docker-prod-up: ## Start production environment
	@echo "$(YELLOW)Starting production environment...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) -f $(DOCKER_COMPOSE_PROD) up -d
	@echo "$(GREEN)Production environment started!$(NC)"

docker-prod-down: ## Stop production environment
	@echo "$(YELLOW)Stopping production environment...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) -f $(DOCKER_COMPOSE_PROD) down
	@echo "$(GREEN)Production environment stopped!$(NC)"

## Code Quality
format: ## Format code using dotnet format
	@echo "$(YELLOW)Formatting code...$(NC)"
	dotnet format $(SOLUTION_FILE)
	@echo "$(GREEN)Code formatted successfully!$(NC)"

lint: ## Run code analysis
	@echo "$(YELLOW)Running code analysis...$(NC)"
	dotnet format $(SOLUTION_FILE) --verify-no-changes --verbosity diagnostic
	@echo "$(GREEN)Code analysis completed!$(NC)"

## Utilities
check-health: ## Check API health endpoints
	@echo "$(YELLOW)Checking API health...$(NC)"
	@curl -s http://localhost:8080/health | jq . || echo "$(RED)Health check failed or jq not installed$(NC)"

check-swagger: ## Open Swagger UI
	@echo "$(YELLOW)Opening Swagger UI...$(NC)"
	@echo "$(GREEN)Swagger UI: http://localhost:8080/swagger$(NC)"

logs: ## Show application logs (requires Docker)
	docker-compose -f $(DOCKER_COMPOSE_FILE) logs -f api

## Localization
test-localization: ## Test localization endpoints
	@echo "$(YELLOW)Testing localization...$(NC)"
	@echo "$(GREEN)Testing Indonesian localization:$(NC)"
	@curl -s -H "Accept-Language: id-ID" http://localhost:8080/api/modular-localization-test/all-modules | jq . || echo "API not running"
	@echo "$(GREEN)Testing Spanish localization:$(NC)"
	@curl -s -H "Accept-Language: es-ES" http://localhost:8080/api/modular-localization-test/all-modules | jq . || echo "API not running"

## Development Workflow
dev-setup: install db-update ## Complete development setup
	@echo "$(GREEN)Development environment setup completed!$(NC)"
	@echo "$(YELLOW)Next steps:$(NC)"
	@echo "  1. Run 'make run' to start the API"
	@echo "  2. Visit http://localhost:7000/swagger for API documentation"
	@echo "  3. Run 'make test' to execute tests"

dev-reset: clean db-reset install build ## Reset development environment

quick-start: docker-up ## Quick start with Docker
	@echo "$(GREEN)Quick start completed!$(NC)"
	@echo "$(YELLOW)Services available:$(NC)"
	@echo "  - API: http://localhost:8080"
	@echo "  - Swagger: http://localhost:8080/swagger"
	@echo "  - Health: http://localhost:8080/health"

## CI/CD
ci-build: install build test ## CI build pipeline
	@echo "$(GREEN)CI build completed successfully!$(NC)"

ci-test: test test-coverage ## CI test pipeline
	@echo "$(GREEN)CI test pipeline completed!$(NC)"

## Cleanup
clean-docker: ## Clean Docker resources
	@echo "$(YELLOW)Cleaning Docker resources...$(NC)"
	docker-compose -f $(DOCKER_COMPOSE_FILE) down -v --remove-orphans
	docker system prune -f
	@echo "$(GREEN)Docker cleanup completed!$(NC)"

clean-all: clean clean-docker ## Clean everything
	@echo "$(GREEN)Complete cleanup finished!$(NC)"

## Information
info: ## Show project information
	@echo "$(GREEN)Project Information:$(NC)"
	@echo "  Name: $(PROJECT_NAME)"
	@echo "  API Project: $(API_PROJECT)"
	@echo "  Infrastructure: $(INFRASTRUCTURE_PROJECT)"
	@echo "  Solution: $(SOLUTION_FILE)"
	@echo ""
	@echo "$(YELLOW)Key Features:$(NC)"
	@echo "  ✅ Modular Monolith Architecture"
	@echo "  ✅ CQRS with MediatR"
	@echo "  ✅ JWT Authentication"
	@echo "  ✅ Role-Based Authorization"
	@echo "  ✅ Multi-Language Support (9 languages)"
	@echo "  ✅ Entity Framework Core"
	@echo "  ✅ Redis Caching"
	@echo "  ✅ Docker Support"
	@echo "  ✅ Comprehensive Testing"

status: ## Show development environment status
	@echo "$(GREEN)Development Environment Status:$(NC)"
	@echo -n "  .NET SDK: "
	@dotnet --version 2>/dev/null || echo "$(RED)Not installed$(NC)"
	@echo -n "  Docker: "
	@docker --version 2>/dev/null || echo "$(RED)Not installed$(NC)"
	@echo -n "  Docker Compose: "
	@docker-compose --version 2>/dev/null || echo "$(RED)Not installed$(NC)"
	@echo ""
	@echo "$(YELLOW)Docker Services:$(NC)"
	@docker-compose -f $(DOCKER_COMPOSE_FILE) ps 2>/dev/null || echo "  $(RED)Docker Compose not running$(NC)"

.PHONY: help install build clean rebuild run run-watch test test-watch test-coverage \
        db-update db-migration db-drop db-reset \
        docker-build docker-up docker-down docker-logs docker-restart \
        docker-prod-build docker-prod-up docker-prod-down \
        format lint check-health check-swagger logs test-localization \
        dev-setup dev-reset quick-start ci-build ci-test \
        clean-docker clean-all info status