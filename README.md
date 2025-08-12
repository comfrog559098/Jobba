# Jobba

[![CI](https://github.com/comfrog559098/jobba/actions/workflows/ci.yml/badge.svg)](https://github.com/comfrog559098/jobba/actions/workflows/ci.yml)
![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

Jobba is a lightweight job application tracking API built with ASP.NET Core 9, EF Core, and SQLite. It lets you create, update, filter, and manage job applications with an interactive Swagger UI.

---

## Features

- Minimal API architecture for clarity and speed
- CRUD for job applications
- SQLite + EF Core migrations
- Seeded demo data in Development
- Swagger/OpenAPI docs with “Try it out”
- Simple filtering/sorting on list endpoint
- Structured logging with Serilog
- GitHub Actions CI (build + tests)

---

## Tech Stack

- **ASP.NET Core 9** (Web API)
- **Minimal API** (endpoint-first style)
- **Entity Framework Core** (ORM)
- **SQLite** (file-backed database)
- **Serilog** (structured logging)
- **Swagger / OpenAPI** (docs + UI via Swashbuckle)
- **xUnit** (tests)
- **GitHub Actions** (CI)

---

## Getting Started

### Prerequisites
- .NET SDK 9.x  
- Visual Studio 2022 (ASP.NET workload) or Rider  
- Optional: SQLite CLI/tools if you want to inspect `jobba.db`

### Clone and run

```
git clone https://github.com/comfrog559098/jobba.git
cd jobba/src/Jobba
dotnet run
```

The server will print the listening URLs (for example `http://localhost:5241`).  
Open Swagger UI at:

```
http://localhost:<port>/swagger
```

---

## API Overview

### Endpoints
- `GET /health` → basic health
- `GET /applications` → list applications  
  - Optional query: `status`, `sortBy` (company|role|date)
- `GET /applications/{id}` → get one
- `POST /applications` → create
- `PUT /applications/{id}` → update
- `DELETE /applications/{id}` → delete

### Example JSON (JobApplication)

```
{
  "company": "Acme",
  "role": "C# Developer",
  "source": "Referral",
  "status": 1,
  "location": "Remote",
  "salaryRange": "80–100k CAD",
  "appliedAt": "2025-08-12T00:00:00Z",
  "nextAction": "Follow up Friday",
  "notes": "Tailor resume to posting"
}
```

### Example requests (PowerShell)

**Create**

```
$body = @{
  company  = "Acme"
  role     = "C# Developer"
  status   = 1
  location = "Remote"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5241/applications" `
  -Method POST `
  -Body $body `
  -ContentType 'application/json'
```

**List**

```
Invoke-RestMethod -Uri "http://localhost:5241/applications"
```

---

## Database & Migrations

- Default connection string (Development) uses SQLite file: `Data Source=jobba.db`
- First run applies migrations and seeds sample data in Development.

**Common EF CLI commands**

```
# Install EF tools once (global)
dotnet tool install --global dotnet-ef

# From repo root (specify project) or run inside src/Jobba
dotnet ef migrations add InitialCreate --project src/Jobba
dotnet ef database update --project src/Jobba
```

---

## Tests

From the repo root:

```
dotnet test
```

CI runs on every push/PR: see the badge at the top.

---

## Project Structure

```
jobba/
  ├─ src/
  │   └─ Jobba/            # API project
  ├─ tests/
  │   └─ Jobba.Tests/      # xUnit tests
  └─ docs/
      └─ DECISIONS.md      # Tech choices and rationale
```

---

## Configuration

- App settings live in `src/Jobba/appsettings*.json`
- Change the connection string under `ConnectionStrings:Jobba`
- Logging configured via Serilog section in `appsettings.json`

---

## Roadmap (short)

- Pagination + richer filters
- Activities per application (timeline)
- Simple UI (Razor Pages or static frontend calling API)
- Dockerfile and containerized run

---

## License

MIT License. See `LICENSE` for details.
