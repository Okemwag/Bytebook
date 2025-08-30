-- ByteBook Database Initialization Script
-- This script runs when the PostgreSQL container starts for the first time

-- Create the main database if it doesn't exist
SELECT 'CREATE DATABASE "ByteBookDb"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ByteBookDb')\gexec

-- Create a test database for development
SELECT 'CREATE DATABASE "ByteBookDb_Test"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ByteBookDb_Test')\gexec

-- Connect to the main database
\c ByteBookDb;

-- Create extensions if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create indexes for better performance (these will be created by EF migrations too)
-- This is just a placeholder for any additional database setup

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE "ByteBookDb" TO postgres;
GRANT ALL PRIVILEGES ON DATABASE "ByteBookDb_Test" TO postgres;