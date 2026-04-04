# 🥭 Mango — Full-Stack E-Commerce Microservices

A full-stack e-commerce application built with **ASP.NET Core Microservices** on the backend and **Angular** on the frontend. The system is designed around a microservices architecture with an API Gateway, JWT-based authentication, Stripe payments, and asynchronous messaging via RabbitMQ.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                   Angular Frontend                   │
└────────────────────────┬────────────────────────────┘
                         │ HTTP
┌────────────────────────▼────────────────────────────┐
│              API Gateway  (Ocelot)                   │
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

| Service | Description | Port |
|---|---|---|
| `Mango.GatewaySolution` | API Gateway using **Ocelot** — single entry point for all requests | 7058 |
| `Mango.Services.AuthAPI` | User registration & login with **ASP.NET Identity** + JWT | 7200 |
| `ProductAPI` | Product CRUD with image upload support | — |
| `Mango.Services.CouponAPI` | Coupon management with validation | — |
| `Mango.Services.ShoppingCartAPI` | Cart operations, talks to Product & Coupon APIs | — |
| `Mango.Services.OrderAPI` | Order processing with **Stripe** payment integration | — |
| `Mango.Services.EmailAPI` | Listens to RabbitMQ and logs email notifications | — |
| `MessageBus` | Shared library for RabbitMQ publishing | — |

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

### Frontend
- **Angular 21** — Standalone Components architecture
- **TypeScript 5.9**
- **Bootstrap 5** + Bootstrap Icons
- **ngx-toastr** — Notifications
- **jwt-decode** — JWT token parsing
- **RxJS** — Reactive programming

---

## 📁 Project Structure

```
Mango-Microservices/
│
├── Mango-Backend/
│   ├── Mango.GatewaySolution/        # Ocelot API Gateway
│   ├── Mango.Services.AuthAPI/       # Authentication service
│   ├── Mango.Services.CouponAPI/     # Coupon service
│   ├── Mango.Services.ShoppingCartAPI/  # Cart service
│   ├── Mango.Services.OrderAPI/      # Order & payment service
│   ├── Mango.Services.EmailAPI/      # Email notification service
│   ├── ProductAPI/                   # Product service
│   ├── MessageBus/                   # Shared messaging library
│   └── Mango-Backend.sln
│
└── Mango-Frontend/
    └── src/
        └── app/
            ├── core/                 # Guards, interceptors, services
            ├── features/             # Auth, Home, Products, Cart, Orders, Coupons
            ├── shared/               # Navbar, Spinner, Alert components
            └── models/               # TypeScript interfaces
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

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) & npm 11+
- [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)
- SQL Server (local or cloud)
- [RabbitMQ](https://www.rabbitmq.com/download.html) (local or cloud)
- Stripe account (test keys)

---

### Backend Setup

**1. Clone the repository**
```bash
git clone https://github.com/Yahya-Elebrashy/Mango-Microservices.git
cd Mango-Microservices/Mango-Backend
```

**2. Configure each service**

In every service's `appsettings.json`, update the following:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SQL_SERVER_CONNECTION_STRING"
  },
  "ApiSettings": {
    "Secret": "YOUR_JWT_SECRET_KEY"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest"
  },
  "StripeSettings": {
    "SecretKey": "YOUR_STRIPE_SECRET_KEY",
    "PublishableKey": "YOUR_STRIPE_PUBLISHABLE_KEY"
  }
}
```

**3. Apply EF Core Migrations**

Run this for each service that has a database:
```bash
cd Mango.Services.AuthAPI
dotnet ef database update

cd ../Mango.Services.CouponAPI
dotnet ef database update

# Repeat for ShoppingCartAPI, OrderAPI, EmailAPI, ProductAPI
```

**4. Run all services**

Open the solution in Visual Studio and configure multiple startup projects, or run each individually:
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

# Install dependencies
npm install

# Start the development server
npm start
```

The app will be available at `http://localhost:4200`

**Configure API URLs** in `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  authApiBase:   'https://localhost:7200',
  couponApiBase: 'https://localhost:7058',
  productApiBase: 'https://localhost:7058',
  cartApiBase:   'https://localhost:7058',
  orderApiBase:  'https://localhost:7058',
};
```

---

## 🔒 Authentication Flow

1. User registers or logs in via **AuthAPI**
2. AuthAPI returns a signed **JWT token**
3. Angular stores the token and the **JWT Interceptor** automatically attaches it to every request
4. The **API Gateway** validates the token before forwarding requests to services
5. Services use **role claims** to enforce Admin-only endpoints

---

## 📬 Async Messaging Flow

```
OrderAPI  ──publishes──►  RabbitMQ  ──consumed by──►  EmailAPI
                                                       (logs email to DB)
```

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

---

## 📄 License

This project is for educational purposes. Built as part of a Udemy course on ASP.NET Core Microservices.