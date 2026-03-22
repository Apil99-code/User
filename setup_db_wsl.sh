#!/bin/bash
# Run this in your WSL Ubuntu terminal to create the userdb and schema

# Connect as postgres user and create database
sudo -u postgres psql <<EOF

-- Create database if it doesn't exist
CREATE DATABASE IF NOT EXISTS userdb;

-- Connect to userdb
\c userdb

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    roles VARCHAR(255) DEFAULT 'User',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create refresh_tokens table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_revoked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);

-- Insert test users
INSERT INTO users (username, email, password_hash, roles, created_at)
VALUES ('admin', 'admin@example.com', '$2a$11$K2h22l7lJgV8/v.D8KK02OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUe', 'Admin,User', CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;

INSERT INTO users (username, email, password_hash, roles, created_at)
VALUES ('testuser', 'testuser@example.com', '$2a$11$q3ILxW7XdPlWR2pqaHJkVeMExIg6GKhxST6.FZ0KjJPg7Yn2VFKxK', 'User', CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;

-- Verify tables created
\dt

EOF

echo "Database setup complete!"
