#!/bin/bash
# Development Docker Compose helper script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if .env file exists
if [ ! -f .env ]; then
    print_warning ".env file not found. Creating from .env.example..."
    cp .env.example .env
    print_status "Please update .env file with your configuration"
fi

# Function to start development environment
start_dev() {
    print_status "Starting development environment..."
    docker-compose up -d --build
    
    print_status "Waiting for services to be healthy..."
    sleep 10
    
    print_status "Development environment is ready!"
    print_status "API: http://localhost:5000"
    print_status "Swagger: http://localhost:5000/swagger"
    print_status "Seq Logs: http://localhost:5341 (admin/admin123)"
    print_status "PostgreSQL: localhost:5433"
    print_status "Redis: localhost:6380"
}

# Function to stop development environment
stop_dev() {
    print_status "Stopping development environment..."
    docker-compose down
    print_status "Development environment stopped"
}

# Function to restart development environment
restart_dev() {
    print_status "Restarting development environment..."
    docker-compose down
    docker-compose up -d --build
    print_status "Development environment restarted"
}

# Function to view logs
logs_dev() {
    if [ -z "$1" ]; then
        docker-compose logs -f
    else
        docker-compose logs -f "$1"
    fi
}

# Function to clean up development environment
clean_dev() {
    print_warning "This will remove all containers, volumes, and images. Are you sure? (y/N)"
    read -r response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        print_status "Cleaning up development environment..."
        docker-compose down -v --rmi all
        docker system prune -f
        print_status "Development environment cleaned"
    else
        print_status "Cleanup cancelled"
    fi
}

# Function to run database migrations
migrate_dev() {
    print_status "Running database migrations..."
    docker-compose exec api dotnet ef database update --project /src/src/Infrastructure --startup-project /src/src/Api
    print_status "Database migrations completed"
}

# Function to show help
show_help() {
    echo "ModularMonolith Development Docker Helper"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  start     Start development environment"
    echo "  stop      Stop development environment"
    echo "  restart   Restart development environment"
    echo "  logs      View logs (optionally specify service name)"
    echo "  clean     Clean up all containers, volumes, and images"
    echo "  migrate   Run database migrations"
    echo "  help      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 start"
    echo "  $0 logs api"
    echo "  $0 clean"
}

# Main script logic
case "$1" in
    start)
        start_dev
        ;;
    stop)
        stop_dev
        ;;
    restart)
        restart_dev
        ;;
    logs)
        logs_dev "$2"
        ;;
    clean)
        clean_dev
        ;;
    migrate)
        migrate_dev
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Unknown command: $1"
        show_help
        exit 1
        ;;
esac