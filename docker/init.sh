#!/bin/bash
# init.sh - Docker PostgreSQL initialization script
# This script runs the SQL init after removing PostgreSQL-specific commands

set -e

echo "=== Initializing STEM Store Database ==="

# Run the SQL file, skipping the first 21 lines (DROP/CREATE DATABASE and \c commands)
# PostgreSQL container already creates the database from POSTGRES_DB env var
tail -n +23 /docker-entrypoint-initdb.d/database_init_and_seed.sql | psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB"

echo "=== Database initialization completed! ==="
