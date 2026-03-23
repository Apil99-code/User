-- PostgreSQL Database Schema for User Authentication System
-- Execute this script to create the required tables

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

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);

-- Optional: Create an admin user for testing (password: admin123)
-- Username: admin, Password: admin123 (hashed with BCrypt)
INSERT INTO users (username, email, password_hash, roles, created_at)
VALUES ('admin', 'admin@example.com', '$2a$11$K2h22l7lJgV8/v.D8KK02OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUe', 'Admin,User', CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;

-- Optional: Create a test user
-- Username: testuser, Password: test123 (hashed with BCrypt)
INSERT INTO users (username, email, password_hash, roles, created_at)
VALUES ('testuser', 'testuser@example.com', '$2a$11$q3ILxW7XdPlWR2pqaHJkVeMExIg6GKhxST6.FZ0KjJPg7Yn2VFKxK', 'User', CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;



order api 

-- 1. Create Products Table
CREATE TABLE IF NOT EXISTS Products (
    Id UUID PRIMARY KEY,
    ProductDescription TEXT NOT NULL,
    Rate NUMERIC(18, 2) NOT NULL DEFAULT 0,
    Stock INTEGER NOT NULL DEFAULT 0
);

-- 2. Create Orders Table
CREATE TABLE IF NOT EXISTS Orders (
    Id UUID PRIMARY KEY,
    ProductId UUID NOT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0,
    TotalPrice NUMERIC(18, 2) NOT NULL DEFAULT 0,
    OrderDate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Optional: Add a Foreign Key constraint to ensure data integrity
    CONSTRAINT fk_product
      FOREIGN KEY(ProductId) 
      REFERENCES Products(Id)
      ON DELETE CASCADE
);