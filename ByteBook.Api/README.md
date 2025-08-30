# ByteBook API

## Overview
The ByteBook API provides RESTful endpoints for the digital publishing platform, enabling user authentication, book management, payments, and content access.

## Authentication
The API uses JWT (JSON Web Tokens) for authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

## Endpoints

### Authentication (`/api/auth`)
- `POST /register` - Register new user
- `POST /login` - User login
- `POST /refresh` - Refresh access token
- `POST /verify-email` - Verify email address
- `POST /forgot-password` - Request password reset
- `POST /reset-password` - Reset password
- `POST /change-password` - Change password (authenticated)
- `POST /logout` - Logout user (authenticated)
- `GET /profile` - Get user profile (authenticated)
- `GET /health` - API health check

### Health (`/api/health`)
- `GET /` - API health status
- `GET /database` - Database connectivity check

## Configuration
Key configuration sections in `appsettings.json`:
- `JwtSettings` - JWT token configuration
- `ConnectionStrings` - Database connections
- `Redis` - Cache configuration
- `Elasticsearch` - Search configuration

## Development
Run the API locally:
```bash
dotnet run --project ByteBook.Api
```

Access Swagger UI at: `https://localhost:5001/swagger`