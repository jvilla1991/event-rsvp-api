# Event RSVP API

A .NET 8 Web API following Clean Architecture principles for managing event RSVPs.

## Architecture

The solution follows Clean Architecture with four main layers:

- **Domain**: Core business entities and interfaces
- **Application**: Use cases, DTOs, and business logic handlers
- **Infrastructure**: Data access, EF Core, and external service implementations
- **API**: Controllers, middleware, and API configuration

## Prerequisites

- .NET 8 SDK
- PostgreSQL 12+ (or Docker with PostgreSQL)

## Local Setup

### 1. Database Setup

Ensure PostgreSQL is running locally. Update the connection string in `EventRsvp.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=EventRsvpDb;Username=postgres;Password=yourpassword"
  }
}
```

### 2. Create Database

Create the PostgreSQL database:

```sql
CREATE DATABASE EventRsvpDb;
```

### 3. Run Migrations

The database will be automatically created when you run the application in Development mode (using `EnsureCreatedAsync`). Alternatively, you can apply migrations manually:

```bash
dotnet ef database update --project EventRsvp.Infrastructure/EventRsvp.Infrastructure.csproj --startup-project EventRsvp.Api/EventRsvp.Api.csproj
```

### 4. Run the API

```bash
cd EventRsvp.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## API Endpoints

### POST /api/events/{eventId}/rsvps
Create (or update) an RSVP for an event. Only `name` is required; `email`,
`guestCount` (1-4), `mealChoice`, and `note` are optional extras for events
that collect them (e.g. weddings).

**Request Body:**
```json
{
  "name": "John Doe",
  "status": "Yes",
  "email": "john.doe@example.com",
  "guestCount": 2,
  "mealChoice": "Vegetarian",
  "note": "Allergic to peanuts"
}
```

**Response:** 201 Created
```json
{
  "id": 1,
  "name": "John Doe",
  "status": "Yes",
  "proposedTime": null,
  "email": "john.doe@example.com",
  "guestCount": 2,
  "mealChoice": "Vegetarian",
  "note": "Allergic to peanuts",
  "createdAt": "2024-12-16T12:00:00Z"
}
```

### GET /api/events/{eventId}/rsvps
Get all RSVPs for an event (ordered by CreatedAt descending)

**Response:** 200 OK
```json
[
  {
    "id": 1,
    "name": "John Doe",
    "status": "Yes",
    "proposedTime": null,
    "email": "john.doe@example.com",
    "guestCount": 2,
    "mealChoice": "Vegetarian",
    "note": "Allergic to peanuts",
    "createdAt": "2024-12-16T12:00:00Z"
  }
]
```

### Health Check Endpoints

- `GET /health` - Overall health status
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

## Swagger Documentation

When running in Development mode, Swagger UI is available at:
- `https://localhost:5001/swagger`

## CORS Configuration

CORS is configured to allow requests from:
- `http://localhost:5173` (Vite default)
- `http://localhost:3000` (React default)

Update `appsettings.json` to add additional origins if needed.

## Project Structure

```
event-rsvp-api/
├── EventRsvp.Domain/          # Domain entities and interfaces
├── EventRsvp.Application/     # Application logic and DTOs
├── EventRsvp.Infrastructure/  # Data access and EF Core
└── EventRsvp.Api/             # API controllers and configuration
```

## Development

### Building the Solution

```bash
dotnet build
```

### Running Tests

(Test projects will be added in future iterations)

## Next Steps

- [ ] Add NUnit test projects
- [ ] Add integration tests
- [ ] Create Dockerfile
- [ ] Create Helm charts for EKS deployment
- [ ] Set up CI/CD pipeline

