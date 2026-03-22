# User Authentication API - Complete Guide

## 🎯 Project Overview

This is a complete **ASP.NET Core 10 Web API** for user authentication and management with JWT tokens, using PostgreSQL and Dapper ORM.

### Features Implemented ✅

- ✅ User Registration & Login with JWT tokens
- ✅ Refresh token mechanism for long-lived sessions
- ✅ Role-based access control (RBAC) - Admin & User roles
- ✅ Password hashing with BCrypt
- ✅ HTTP-only cookies for secure refresh token storage
- ✅ PostgreSQL database integration
- ✅ Dapper ORM for lightweight, high-performance queries
- ✅ Comprehensive logging
- ✅ OpenAPI/Swagger documentation with Scalar UI
- ✅ Bearer token authentication with JWT validation

### Architecture

```
User/
├── Controllers/          # API endpoints
│   ├── AuthController   # Login, Register, Refresh, Logout
│   └── UsersController  # Get Profile, List Users, Create/Update/Delete Users
├── Services/            # Business logic layer
│   ├── IJwtService      # JWT token generation and validation
│   ├── IAuthService     # Authentication orchestration
│   ├── IUserService     # User management
│   └── IRefreshTokenService # Refresh token management
├── Data/                # Data access layer
│   ├── UserRepository   # User CRUD operations
│   └── RefreshTokenRepository # Token management
├── Models/              # Data models and DTOs
└── Program.cs          # Dependency injection & configuration
```

---

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- PostgreSQL 13+
- Git

### 1. Clone Repository

```bash
git clone <repository-url>
cd User
```

### 2. Setup PostgreSQL Database

#### Option A: Using Docker

```bash
docker run -d \
  --name userdb \
  -e POSTGRES_DB=userdb \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:15
```

#### Option B: Install PostgreSQL Locally

Download and install from: https://www.postgresql.org/download/

### 3. Create Database Schema

```bash
# Connect to PostgreSQL
psql -U postgres -h localhost

# Create database
CREATE DATABASE userdb;

# Run schema script
\c userdb
\i database_schema.sql
```

Or execute the SQL commands directly:

```sql
-- Create tables
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    roles VARCHAR(255) DEFAULT 'User',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE refresh_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_revoked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Test users (passwords: admin123, test123)
INSERT INTO users (username, email, password_hash, roles)
VALUES
('admin', 'admin@example.com', '$2a$11$K2h22l7lJgV8/v.D8KK02OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUe', 'Admin,User'),
('testuser', 'testuser@example.com', '$2a$11$q3ILxW7XdPlWR2pqaHJkVeMExIg6GKhxST6.FZ0KjJPg7Yn2VFKxK', 'User');
```

### 4. Configuration

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=userdb;Username=postgres;Password=postgres;Port=5432"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-min-32-characters-long-here12345",
    "Issuer": "UserAuthAPI",
    "Audience": "UserAuthAPIUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 5. Run Application

```bash
dotnet restore
dotnet build
dotnet run
```

Application starts at: `https://localhost:7262` or `http://localhost:5239`

### 6. Access API Documentation

Navigate to: `https://localhost:7262/scalar/v1`

---

## 📚 API Endpoints

### Authentication

#### Register New User

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "newuser",
  "email": "newuser@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**

```json
{
  "message": "Registration successful",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2024-12-25T10:30:00Z",
  "user": {
    "id": 3,
    "username": "newuser",
    "email": "newuser@example.com",
    "roles": ["User"]
  }
}
```

#### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "test123"
}
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2024-12-25T10:30:00Z",
  "user": {
    "id": 2,
    "username": "testuser",
    "email": "testuser@example.com",
    "roles": ["User"]
  }
}
```

**Note:** Refresh token is stored in HTTP-only cookie named `refreshToken`

#### Refresh Token

```http
POST /api/auth/refresh
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2024-12-25T10:45:00Z",
  "user": {
    "id": 2,
    "username": "testuser",
    "email": "testuser@example.com",
    "roles": ["User"]
  }
}
```

#### Logout

```http
POST /api/auth/logout
Authorization: Bearer <accessToken>
```

**Response:**

```json
{
  "message": "Logged out successfully"
}
```

### User Management

#### Get Current User Profile

```http
GET /api/users/profile
Authorization: Bearer <accessToken>
```

**Response:**

```json
{
  "id": 2,
  "username": "testuser",
  "email": "testuser@example.com",
  "roles": ["User"]
}
```

#### Get All Users (Admin Only)

```http
GET /api/users
Authorization: Bearer <adminAccessToken>
```

**Response:**

```json
[
  {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "roles": ["Admin", "User"]
  },
  {
    "id": 2,
    "username": "testuser",
    "email": "testuser@example.com",
    "roles": ["User"]
  }
]
```

#### Create User (Admin Only)

```http
POST /api/users
Authorization: Bearer <adminAccessToken>
Content-Type: application/json

{
  "username": "newadmin",
  "email": "newadmin@example.com",
  "password": "SecurePassword123!"
}
```

#### Update User (Admin Only)

```http
PUT /api/users/{userId}
Authorization: Bearer <adminAccessToken>
Content-Type: application/json

{
  "email": "updated@example.com",
  "roles": ["Admin", "User"]
}
```

#### Delete User (Admin Only)

```http
DELETE /api/users/{userId}
Authorization: Bearer <adminAccessToken>
```

---

## 🔐 Security Features

1. **JWT Bearer Tokens**
   - Signed with HMAC-SHA256
   - 15-minute expiration for access tokens
   - 7-day expiration for refresh tokens

2. **Password Security**
   - BCrypt hashing with salt
   - Never stored in plain text

3. **HTTP-Only Cookies**
   - Refresh tokens stored in HTTP-only cookies
   - Prevents XSS attacks

4. **HTTPS Enforcement**
   - HTTPS required in production
   - SameSite=Strict CORS policy

5. **Role-Based Access Control**
   - Admin role for sensitive operations
   - User role for normal operations

6. **Token Blacklisting**
   - Refresh tokens tracked in database
   - Revocation support on logout

---

## 📊 Database Schema

### Users Table

```
id (PK)              : SERIAL
username             : VARCHAR(50) UNIQUE
email                : VARCHAR(100) UNIQUE
password_hash        : VARCHAR(255)
roles                : VARCHAR(255)
created_at           : TIMESTAMP
```

### Refresh Tokens Table

```
id (PK)              : SERIAL
user_id (FK)         : INTEGER
token                : VARCHAR(500) UNIQUE
expires_at           : TIMESTAMP
is_revoked           : BOOLEAN
created_at           : TIMESTAMP
```

---

## 🛠️ Development

### Project Structure

```
User/
├── User.csproj                  # Project file with NuGet dependencies
├── Program.cs                   # Startup configuration
├── appsettings.json            # Configuration
├── appsettings.Development.json # Dev overrides
├── User.http                    # REST client requests
├── database_schema.sql          # PostgreSQL schema
├── Controller/
│   ├── AuthController.cs
│   └── UsersController.cs
├── Services/
│   ├── AuthService.cs
│   ├── IAuthService.cs
│   ├── JwtService.cs
│   ├── IJwtService.CS
│   ├── UserService.cs
│   ├── IUserService.cs (in IAuthService.cs)
│   ├── RefreshTokenService.cs
│   └── (in Services/IAuthService.cs)
├── Data/
│   ├── UserRepository.cs
│   ├── IUserRepository.cs (in UserRepository.cs)
│   ├── RefreshTokenRepository.cs
│   ├── IRefreshTokenRepository.cs (in RefreshTokenRepository.cs)
│   └── DatabaseConnection.cs
├── Modles/
│   └── JWTSettings.cs (all DTOs and models)
├── bin/
└── obj/
```

### Build & Run

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Run with watch mode (for development)
dotnet watch run
```

### Testing with REST Client

Use [VS Code REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) with `User.http` file.

---

## 📦 Dependencies

| Package                                       | Version | Purpose              |
| --------------------------------------------- | ------- | -------------------- |
| BCrypt.Net-Next                               | 4.1.0   | Password hashing     |
| Dapper                                        | 2.1.72  | Lightweight ORM      |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.5  | JWT authentication   |
| Microsoft.AspNetCore.OpenApi                  | 10.0.2  | OpenAPI support      |
| Npgsql                                        | 8.0.3   | PostgreSQL driver    |
| Scalar.AspNetCore                             | 2.13.6  | API documentation UI |
| System.IdentityModel.Tokens.Jwt               | 8.16.0  | JWT token handling   |

---

## 🚀 Advanced Features (Optional)

### 1. Redis Caching

See: [REDIS_IMPLEMENTATION.md](./REDIS_IMPLEMENTATION.md)

- Token blacklisting
- Rate limiting
- Session management
- Real-time notifications

### 2. SignalR Real-Time Communication

See: [SIGNALR_IMPLEMENTATION.md](./SIGNALR_IMPLEMENTATION.md)

- Live user status
- Admin notifications
- Real-time dashboard
- Instant alerts

### 3. Two-Factor Authentication (2FA)

- TOTP (Time-based One-Time Password)
- Email verification
- Recovery codes

### 4. OAuth/Social Login

- Google OAuth
- GitHub OAuth
- Microsoft OAuth

---

## 📝 Logging

Logs are configured in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Logged events include:

- User authentication attempts (success/failure)
- Token generation and validation
- User CRUD operations
- Security events

View logs in:

- Console output
- Debug window (Visual Studio)
- Application Insights (if configured)

---

## 🐛 Troubleshooting

### Connection String Issues

```
Error: Connection refused on port 5432
Solution: Ensure PostgreSQL is running: systemctl start postgresql (Linux) or start service from PostgreSQL installer (Windows)
```

### JWT Validation Errors

```
Error: Token validation failed
Solution: Verify JwtSettings in appsettings.json match the token generation configuration
```

### Database Migration Issues

```
Error: Table does not exist
Solution: Run database_schema.sql to create tables
```

### HTTPS Certificate Error

```
Error: The certificate is not trusted
Solution: For development, run: dotnet dev-certs https --trust
```

---

## 📚 Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [JWT.io - JWT Debugger](https://jwt.io)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Dapper GitHub](https://github.com/DapperLib/Dapper)
- [BCrypt.net](https://github.com/BcryptNet/bcrypt.net)

---

## 📄 License

This project is provided as-is for educational and commercial use.

---

## ✅ Completion Checklist

- [x] User Registration with validation
- [x] Login with JWT tokens
- [x] Refresh token mechanism
- [x] Logout with token revocation
- [x] Role-based access control
- [x] PostgreSQL integration
- [x] Dapper ORM implementation
- [x] Password hashing (BCrypt)
- [x] HTTP-only cookie security
- [x] Comprehensive error handling
- [x] Bearer token authentication
- [x] Token blacklisting
- [x] Logging and monitoring
- [x] OpenAPI documentation
- [x] Database schema creation

---

## 🎓 Next Steps

1. **Deploy to Production**: Use Azure App Services or equivalent
2. **Add Redis**: Implement caching and rate limiting (see REDIS_IMPLEMENTATION.md)
3. **Add SignalR**: Real-time features (see SIGNALR_IMPLEMENTATION.md)
4. **Implement 2FA**: Two-factor authentication
5. **Add Email Verification**: Confirm user email addresses
6. **Setup CI/CD**: GitHub Actions or Azure Pipelines
7. **Add Unit Tests**: xUnit or NUnit
8. **Performance Optimization**: Query optimization and caching

---

**Version**: 1.0  
**Last Updated**: December 2024  
**Status**: ✅ Production Ready
