#!/bin/bash
# init.sh - Docker PostgreSQL initialization script
# This script runs the SQL init after filtering out incompatible commands

set -e

echo "=== Initializing STEM Store Database ==="

# Filter out DROP DATABASE, CREATE DATABASE, and \c commands, then execute
# These commands don't work inside Docker container (database is already created)
grep -v "^DROP DATABASE" /docker-entrypoint-initdb.d/database_init_and_seed.sql | \
grep -v "^CREATE DATABASE" | \
grep -v "^\\\\c" | \
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB"

echo "=== Database initialization completed! ==="
