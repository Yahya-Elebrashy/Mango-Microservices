# 🥭 Mango — Full-Stack E-Commerce Microservices

A full-stack e-commerce application built with **ASP.NET Core Microservices** on the backend and **Angular** on the frontend. The system is designed around a microservices architecture with an API Gateway, JWT-based authentication, Stripe payments, and asynchronous messaging via RabbitMQ.

> ⚡ **Recently refactored** with Clean Architecture principles: Repository Pattern, Unit of Work, Service Layer, Global Exception Handling, Result Pattern, Serilog Logging, and Extension Methods.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                   Angular Frontend                   │
└────────────────────────┬────────────────────────────┘
                         │ HTTP
┌────────────────────────▼────────────────────────────┐
│              API Gateway  (Ocelot)                   │
│         Serilog Request Logging + JWT Validation     │
└──┬──────────┬──────────┬──────────┬─────────────────┘
   │          │          │          │
   ▼          ▼          ▼          ▼
 Auth      Product    Coupon     Cart ──► Order
  API        API        API       API      API
                                            │
                                        RabbitMQ
                                            │
                                        Email API
```

---

## 🧩 Services

| Service | Description | Pattern |
|---|---|---|
| `Mango.GatewaySolution` | API Gateway using **Ocelot** — single entry point for all requests | Reverse Proxy |
| `Mango.Services.AuthAPI` | User registration & login with **ASP.NET Identity** + JWT | Service Layer |
| `ProductAPI` | Product CRUD with image upload support | Repository + UoW + Service Layer |
| `Mango.Services.CouponAPI` | Coupon management with Stripe sync | Repository + UoW + Service Layer |
| `Mango.Services.ShoppingCartAPI` | Cart operations, talks to Product & Coupon APIs | Repository + UoW + Service Layer |
| `Mango.Services.OrderAPI` | Order processing with **Stripe** payment integration | Repository + UoW + Service Layer |
| `Mango.Services.EmailAPI` | Listens to RabbitMQ and logs email notifications | Service Layer |
| `MessageBus` | Shared library for RabbitMQ publishing | Shared Library |

---

## 💻 Tech Stack

### Backend
- **ASP.NET Core 8** — Web API
- **Entity Framework Core** — ORM with SQL Server
- **Ocelot** — API Gateway
- **ASP.NET Identity** — User management
- **JWT Bearer** — Authentication & Authorization
- **AutoMapper** — Object mapping
- **Stripe** — Payment processing
- **RabbitMQ** — Async messaging between services
- **Serilog** — Structured logging (Console + File sinks)
- **FluentValidation** — Input validation

### Frontend
- **Angular 21** — Standalone Components architecture
- **TypeScript 5.9**
- **Bootstrap 5** + Bootstrap Icons
- **ngx-toastr** — Notifications
- **jwt-decode** — JWT token parsing
- **RxJS** — Reactive programming

---

## 🏛️ Clean Architecture & Design Patterns

Every service follows a consistent layered architecture:

```
Controller  →  Service Layer  →  Repository  →  Database
    ↓               ↓
IActionResult   Business Logic
Guard Clauses   Exception Throwing
```

### Patterns Applied

| Pattern | Where | Benefit |
|---|---|---|
| **Repository Pattern** | All data services | Abstracts DB access |
| **Unit of Work** | All data services | Atomic transactions |
| **Service Layer** | All services | Business logic separation |
| **Generic Repository** | `IRepository<T>` | DRY — reusable across all entities |
| **Global Exception Handler** | Shared Middleware | Single place for all errors |
| **Result Pattern** | Service Layer | Explicit success/failure flow |
| **Guard Clauses** | All controllers | Clean, readable validation |
| **Constants (SD)** | All services | No magic strings |

---

## 📋 Code Quality Improvements

### Before vs After

**Controller — Before:**
```csharp
private ResponseDto _response = new();
private readonly AppDbContext _db;

public ResponseDto Get()
{
    try {
        _response.Result = _db.Products.ToList();
    }
    catch (Exception ex) {
        _response.IsSuccess = false;
        _response.Message = ex.Message;
    }
    return _response;
}
```

**Controller — After:**
```csharp
private readonly IProductService _productService;

public async Task<IActionResult> Get()
{
    var products = await _productService.GetAllProductsAsync();
    return Ok(products);
}
```

---

## 📊 Logging Strategy

```
Gateway  →  UseSerilogRequestLogging  (all HTTP requests)
              ↓
         [10:15:32 INF] GET /api/product → 200 in 45ms

Services →  ILogger in Service Classes  (business events)
              ↓
         [10:15:33 INF] Order 123 created for user abc
         [10:15:40 WRN] Product 999 not found
         [10:15:41 ERR] Payment failed for Order 456
```

All logs written to:
- **Console** — visible via `docker logs`
- **File** — daily rolling files in `/app/logs`

---

## 🐳 Docker — Full Containerization

The entire application is fully containerized. Every service, database, and message broker runs inside Docker. One command starts everything.

---

### 📦 What's Containerized

| Container | Image | Role |
|---|---|---|
| `gateway` | Custom .NET image | Ocelot API Gateway |
| `auth-api` | Custom .NET image | Authentication service |
| `product-api` | Custom .NET image | Product service |
| `coupon-api` | Custom .NET image | Coupon service |
| `cart-api` | Custom .NET image | Shopping cart service |
| `order-api` | Custom .NET image | Order & payment service |
| `email-api` | Custom .NET image | Email notification service |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | Database |
| `rabbitmq` | `rabbitmq:3-management` | Message broker |

---

### 🏗️ Dockerfile — Multi-stage Build

Every service uses a **Multi-stage Dockerfile** to keep production images small:

```dockerfile
# Stage 1: Build — uses full SDK (700MB)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run — uses only runtime (200MB)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.ProductAPI.dll"]
```

Result: **~200MB** images instead of ~700MB — 3.5x smaller.

Services that depend on the shared `MessageBus` library (OrderAPI, CartAPI, EmailAPI) copy it explicitly:

```dockerfile
COPY Mango.Services.OrderAPI/*.csproj ./Mango.Services.OrderAPI/
COPY MessageBus/*.csproj ./MessageBus/
RUN dotnet restore ./Mango.Services.OrderAPI/Mango.Services.OrderAPI.csproj
```

---

### 🌐 Docker Networking

All containers communicate through a dedicated **bridge network** (`mango-network`).

Docker provides an internal DNS — containers talk to each other by name, not IP:

```
cart-api  →  "http://product-api:8080"   ✅ internal DNS
cart-api  →  "http://sqlserver:1433"     ✅ internal DNS
```

**Security:** Only the Gateway and infrastructure ports are exposed externally. All other services are internal-only:

```
From outside (browser):
  localhost:7000  →  gateway     ✅ exposed
  localhost:7001  →  product-api ❌ internal only (in production)

From inside Docker (container to container):
  http://product-api:8080  ✅
  http://sqlserver:1433    ✅
  http://rabbitmq:5672     ✅
```

This is why `ocelot.json` uses container names instead of localhost:
```json
"DownstreamHostAndPorts": [{ "Host": "product-api", "Port": 8080 }]
```

---

### ❤️ Health Checks

Services wait until their dependencies are **truly ready**, not just started:

```yaml
sqlserver:
  healthcheck:
    test: ["CMD", "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost",
           "-U", "sa", "-P", "${SA_PASSWORD}", "-Q", "SELECT 1", "-C"]
    interval: 10s
    timeout: 5s
    retries: 5

rabbitmq:
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5

product-api:
  depends_on:
    sqlserver:
      condition: service_healthy   # waits for healthy, not just started
```

Health checks run **continuously** every 10 seconds — if SQL Server crashes, status changes to `unhealthy` immediately.

---

### 💾 Volumes — Data Persistence

SQL Server data is stored in a **Named Volume** — survives container restarts and `docker compose down`:

```yaml
sqlserver:
  volumes:
    - sqlserver-data:/var/opt/mssql

volumes:
  sqlserver-data:    # managed by Docker
```

```bash
docker compose down       # containers removed — data safe ✅
docker compose down -v    # containers + volumes removed — data gone ❌
```

---

### 🔄 Restart Policies

| Policy | Development | Production |
|---|---|---|
| `on-failure:3` | ✅ Services | — |
| `unless-stopped` | — | ✅ All services |

`on-failure` — restarts only on crash (solves the RabbitMQ startup race condition).
`unless-stopped` — restarts after machine reboot, unless manually stopped.

---

### 🌍 Multi-Environment Setup

Three compose files — base + environment-specific overrides:

```
docker-compose.yml          ← shared base (services, networks, volumes)
docker-compose.dev.yml      ← development additions
docker-compose.prod.yml     ← production settings
```

**Development** adds:
- Swagger enabled (`ASPNETCORE_ENVIRONMENT=Development`)
- All ports exposed for direct Swagger access
- `build` section to rebuild from Dockerfile on `--build`

**Production** uses:
- `ASPNETCORE_ENVIRONMENT=Production` (Swagger disabled)
- Only Gateway port exposed
- `unless-stopped` restart policy

```bash
# Development
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build

# Production
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

### 🔐 Secrets with `.env` File

All sensitive values live in `.env` — never committed to GitHub:

```env
DOCKER_USERNAME=yourusername
SA_PASSWORD=YourStrong@Password
DB_CONNECTION=Server=sqlserver;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True
RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
ASPNET_ENVIRONMENT=Development
```

Referenced in `docker-compose.yml` as `${SA_PASSWORD}` — Docker substitutes automatically.

---

### 🚀 Useful Docker Commands

```bash
# Start everything (development)
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build

# Check status + health
docker compose ps

# View logs — all services
docker compose logs -f

# View logs — specific service
docker compose logs -f order-api

# Rebuild a single service after code change
docker compose build product-api
docker compose up -d product-api

# Stop everything (keep data)
docker compose down

# Stop everything + wipe database
docker compose down -v

# Inspect the network — see all containers and IPs
docker network inspect mango-network

# Enter a running container
docker exec -it product-api bash
```

---

### 📊 Services & Ports

| Service | External Port | Internal Port |
|---|---|---|
| API Gateway | 7000 | 8080 |
| ProductAPI (dev only) | 7001 | 8080 |
| AuthAPI (dev only) | 7002 | 8080 |
| CouponAPI (dev only) | 7003 | 8080 |
| CartAPI (dev only) | 7004 | 8080 |
| OrderAPI (dev only) | 7005 | 8080 |
| EmailAPI (dev only) | 7006 | 8080 |
| SQL Server | 1433 | 1433 |
| RabbitMQ AMQP | 5672 | 5672 |
| RabbitMQ Dashboard | 15672 | 15672 |

---

## 📁 Project Structure

```
Mango-Microservices/
│
├── Mango-Backend/
│   ├── Mango.GatewaySolution/
│   │   └── ocelot.json
│   ├── Mango.Services.AuthAPI/
│   │   ├── Controllers/
│   │   ├── Service/           # IAuthService, AuthService
│   │   └── Validators/
│   ├── Mango.Services.CouponAPI/
│   │   ├── Controllers/
│   │   ├── Repositories/      # IRepository, ICouponRepository
│   │   ├── UnitOfWork/
│   │   ├── Services/          # ICouponService, CouponService
│   │   └── Constants/         # SD.cs
│   ├── Mango.Services.ShoppingCartAPI/
│   ├── Mango.Services.OrderAPI/
│   ├── Mango.Services.EmailAPI/
│   ├── ProductAPI/
│   ├── MessageBus/
│   ├── docker-compose.yml
│   ├── docker-compose.dev.yml
│   ├── docker-compose.prod.yml
│   ├── .env                   # Not committed
│   └── Mango-Backend.sln
│
└── Mango-Frontend/
    └── src/app/
        ├── core/
        ├── features/
        ├── shared/
        └── models/
```

---

## ✨ Features

- 🔐 User authentication (register / login) with JWT
- 🛍️ Browse and view product details
- 🛒 Shopping cart with coupon code support
- 💳 Checkout with Stripe payment integration
- 📦 Order history and order detail tracking
- 🏷️ Admin coupon management (create / delete)
- 📷 Admin product management (create / edit / delete) with image upload
- 📧 Email notification system via RabbitMQ
- 🔑 Role-based access control (Admin / Customer)
- 🔄 JWT interceptor for automatic token attachment
- 📊 Structured logging with Serilog across all services
- 🛡️ Global exception handling with proper HTTP status codes
- ✅ Input validation with guard clauses

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js 20+](https://nodejs.org/) & npm 11+
- [Angular CLI](https://angular.io/cli)
- Stripe account (test keys)

---

### 🐳 Run with Docker (Recommended)

**1. Clone the repository**
```bash
git clone https://github.com/Yahya-Elebrashy/Mango-Microservices.git
cd Mango-Microservices/Mango-Backend
```

**2. Create `.env` file**
```env
DOCKER_USERNAME=yourusername
SA_PASSWORD=Pass@1234
DB_CONNECTION=Server=sqlserver;User Id=sa;Password=Pass@1234;TrustServerCertificate=True
RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
ASPNET_ENVIRONMENT=Development
```

**3. Start everything**
```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

**4. Check services are healthy**
```bash
docker compose ps
```

---

### 🛠️ Run Locally (Without Docker)

**1. Configure each service's `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SQL_SERVER_CONNECTION_STRING"
  },
  "JwtSettings": {
    "SecretKey": "YOUR_JWT_SECRET_KEY"
  },
  "RabbitMQ": {
    "Hostname": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Stripe": {
    "SecretKey": "YOUR_STRIPE_SECRET_KEY"
  }
}
```

**2. Apply EF Core Migrations**
```bash
dotnet ef database update --project Mango.Services.AuthAPI
dotnet ef database update --project Mango.Services.CouponAPI
dotnet ef database update --project Mango.Services.ShoppingCartAPI
dotnet ef database update --project Mango.Services.OrderAPI
dotnet ef database update --project Mango.Services.EmailAPI
dotnet ef database update --project ProductAPI
```

**3. Run all services**
```bash
dotnet run --project Mango.GatewaySolution
dotnet run --project Mango.Services.AuthAPI
dotnet run --project ProductAPI
# ... and so on
```

---

### Frontend Setup

```bash
cd Mango-Microservices/Mango-Frontend
npm install
npm start
```

App available at `http://localhost:4200`

**Configure API URLs** in `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiGateway: 'http://localhost:7000',
};
```

---

## 🔒 Authentication Flow

1. User registers or logs in via **AuthAPI**
2. AuthAPI returns a signed **JWT token**
3. Angular stores the token and the **JWT Interceptor** attaches it to every request
4. The **API Gateway** validates the token before forwarding requests
5. Services enforce **role-based** access (Admin / Customer)

---

## 📬 Async Messaging Flow

```
OrderAPI  ──publishes──►  RabbitMQ  ──consumed by──►  EmailAPI
CartAPI   ──publishes──►  RabbitMQ                    (logs to DB)
```

---

## 🔍 Viewing Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f order-api

# Log files (if volumes mounted)
cat logs/OrderAPI-2026-04-25.txt
```

---
