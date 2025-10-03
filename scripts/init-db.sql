-- Database initialization script for ModularMonolith
-- This script runs when the PostgreSQL container starts for the first time

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Create additional databases for different environments
CREATE DATABASE "ModularMonolith_Test";
CREATE DATABASE "ModularMonolith_Integration";

-- Create application user with limited privileges (for production)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'mmuser') THEN
        CREATE ROLE mmuser WITH LOGIN PASSWORD 'changeme';
    END IF;
END
$$;

-- Grant necessary permissions to application user
GRANT CONNECT ON DATABASE "ModularMonolith" TO mmuser;
GRANT CONNECT ON DATABASE "ModularMonolith_Test" TO mmuser;
GRANT CONNECT ON DATABASE "ModularMonolith_Integration" TO mmuser;

-- Switch to application database and set up schema permissions
\c ModularMonolith;

-- Grant schema permissions
GRANT USAGE ON SCHEMA public TO mmuser;
GRANT CREATE ON SCHEMA public TO mmuser;

-- Grant table permissions (for future tables)
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO mmuser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO mmuser;

-- Create indexes for better performance (these will be created by EF migrations, but good to have as backup)
-- Note: Actual table creation will be handled by Entity Framework migrations

-- Set up database configuration for better performance
ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements';
ALTER SYSTEM SET track_activity_query_size = 2048;
ALTER SYSTEM SET pg_stat_statements.track = 'all';
ALTER SYSTEM SET log_min_duration_statement = 1000;

-- Reload configuration
SELECT pg_reload_conf();

-- Create a function to generate UUID v7 (if not available natively)
-- Note: .NET 9.0 Guid.CreateVersion7() will handle this, but keeping as backup
CREATE OR REPLACE FUNCTION uuid_generate_v7()
RETURNS uuid
AS $$
DECLARE
    unix_ts_ms bigint;
    uuid_bytes bytea;
BEGIN
    unix_ts_ms := (extract(epoch from clock_timestamp()) * 1000)::bigint;
    uuid_bytes := substring(int8send(unix_ts_ms), 3, 6) || gen_random_bytes(10);
    return encode(uuid_bytes, 'hex')::uuid;
END;
$$ LANGUAGE plpgsql;