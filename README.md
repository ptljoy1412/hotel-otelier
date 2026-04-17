# Otelier Backend API

A secure ASP.NET Core Web API for hotel bookings, backed by PostgreSQL with EF Core, local JWT bearer authentication, Serilog structured logging, Swagger/OpenAPI, and Docker support.

## Tech Stack

- .NET 10 Web API
- Entity Framework Core + Npgsql (PostgreSQL)
- Local JWT bearer authentication (see Auth0 note below)
- Serilog structured logging
- Docker + Docker Compose
- Swagger / OpenAPI

## Auth0 Assumption

The assignment specifies Auth0 for JWT authentication. This implementation uses **local JWT generation** instead, for the following reasons:

- Auth0 requires a paid/free tenant, external DNS, and callback URLs that are environment-specific.
- A local JWT setup is fully self-contained, reproducible, and testable without any third-party account.
- The JWT validation logic (issuer, audience, signature, expiry, role claims) is **identical** to what Auth0 would require — only the token *issuer* changes.

To switch to Auth0, replace the `AddJwtBearer` configuration in `Program.cs` with:

```csharp
options.Authority = "https://<your-auth0-domain>/";
options.Audience  = "<your-auth0-api-identifier>";
```

All role-based authorization (`staff`, `reception`) and claim extraction (`sub`) remain unchanged.

---

## Local Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Option A – Docker Compose (recommended)

```bash
docker-compose up --build
```

The API will be available at `http://localhost:8080/swagger`.

### Option B – Run locally

1. Start PostgreSQL:

```bash
docker-compose up -d db
```

2. Apply EF Core migrations:

```bash
dotnet ef database update
```

3. Run the API:

```bash
dotnet run
```

4. Open Swagger: `https://localhost:7171/swagger`

The application seeds demo users and hotels automatically on first run.

---

## Demo Users

| Username | Password | Role |
|---|---|---|
| `guest.user` | `Guest@123` | guest |
| `staff.user` | `Staff@123` | staff |
| `reception.user` | `Reception@123` | reception |

---

## Authentication Flow

1. `POST /api/auth/login` with `{ "userName": "staff.user", "password": "Staff@123" }`
2. Copy the `accessToken` from the response.
3. In Swagger, click **Authorize** and enter `Bearer <token>`.
4. All subsequent requests will include the token automatically.

---

## API Endpoints

### POST /api/auth/login

Authenticate and receive a JWT.

**Request body:**
```json
{
  "userName": "staff.user",
  "password": "Staff@123"
}
```

**Response `200 OK`:**
```json
{
  "accessToken": "<jwt>",
  "expiresIn": 3600
}
```

---

### GET /api/hotels/{hotelId}/bookings

Returns bookings for a hotel. Requires any valid JWT.

**Query parameters (optional):**

| Parameter | Type | Description |
|---|---|---|
| `startDate` | `DateTime` | Filter bookings with check-in on or after this date |
| `endDate` | `DateTime` | Filter bookings with check-out on or before this date |

**Response `200 OK`:**
```json
[
  {
    "bookingId": 1,
    "hotelId": 1,
    "guestName": "John Doe",
    "checkInDate": "2026-05-01T00:00:00Z",
    "checkOutDate": "2026-05-05T00:00:00Z",
    "createdBy": "staff.user"
  }
]
```

**Errors:** `401 Unauthorized`, `404 Not Found`

---

### POST /api/hotels/{hotelId}/bookings

Creates a new booking. Requires role `staff` or `reception`.

**Request body:**
```json
{
  "guestName": "Jane Smith",
  "checkInDate": "2026-06-10T00:00:00Z",
  "checkOutDate": "2026-06-15T00:00:00Z"
}
```

**Response `201 Created`:**
```json
{
  "bookingId": 2,
  "hotelId": 1,
  "guestName": "Jane Smith",
  "checkInDate": "2026-06-10T00:00:00Z",
  "checkOutDate": "2026-06-15T00:00:00Z",
  "createdBy": "staff.user"
}
```

**Errors:** `400 Bad Request` (invalid dates), `401 Unauthorized`, `403 Forbidden` (wrong role), `404 Not Found` (hotel), `409 Conflict` (overlapping booking)

On success, a notification is logged (simulating an email/Slack alert to the support team).

---

## Configuration

`appsettings.json` contains default local development values:

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Issuer` | JWT issuer claim |
| `Jwt:Audience` | JWT audience claim |
| `Jwt:Secret` | HMAC-SHA256 signing key (min 32 chars) |
| `Jwt:ExpiryMinutes` | Token lifetime in minutes |

Docker Compose overrides the connection string via environment variables for the containerized API.

---

## Database Schema

See [`Scripts/db_schema.sql`](Scripts/db_schema.sql) for the full PostgreSQL schema.

EF Core migrations are the source of truth — run `dotnet ef database update` to apply them, or use the SQL script for manual setup.

---

## Deployment

The service is containerized and can be deployed to any platform that supports Docker:

- **Render** – connect the repo, set `ASPNETCORE_ENVIRONMENT=Production` and the connection string env var.
- **Railway** – add a PostgreSQL plugin, set env vars, deploy from GitHub.
- **Fly.io** – `fly launch` then `fly deploy`.
- **Azure App Service** – use the Docker container deployment option.

> Deployment URL: *(add your live URL here after deploying)*

---

## Bonus Features Implemented

- Serilog structured logging (console + request logging middleware)
- Swagger / OpenAPI with Bearer auth support
- Dockerfile (multi-stage build)
- Docker Compose (API + PostgreSQL)
