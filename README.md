# BDTechMarket

> A full-stack ASP.NET Core MVC tech marketplace built with .NET 8, Entity Framework Core, SQL Server, ASP.NET Core Identity, and Stripe integration.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4)](https://learn.microsoft.com/aspnet/core/mvc/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.x-512BD4)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-Private-lightgrey)](#)

## Overview

BDTechMarket is an ecommerce-style web application for browsing and managing technology products. The project combines product catalog management, authentication, shopping cart and order workflows, product specifications, brand management, and advanced catalog filtering.

The application is being developed as a practical, real-world ASP.NET Core MVC project rather than a static demo.

## Core Features

### Catalog
- Product management with CRUD operations
- Category management
- Brand management
- Product SKU generation
- Product image upload and replacement
- Active/inactive product handling
- Product details page
- Related products
- Product specifications

### Search & Discovery
- Product name / description search
- Brand and SKU search
- Category filtering
- Brand filtering
- Minimum and maximum price filtering
- In-stock, low-stock, and out-of-stock filtering
- Multiple sorting modes
- Server-side pagination

### Authentication
- ASP.NET Core Identity
- User registration and login
- Role-based authorization
- Admin area
- Account lockout configuration
- Password reset email flow

### Cart & Orders
- User-specific cart
- Add/update/remove cart items
- Checkout flow
- Order creation
- Order details
- Order status management
- Customer order history

### Payments
- Stripe integration
- Bangladesh payment assets/configuration for the ecommerce UI

## Technology Stack

| Area | Technology |
|---|---|
| Framework | ASP.NET Core MVC |
| Runtime | .NET 8 |
| Language | C# |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Authentication | ASP.NET Core Identity |
| Frontend | Razor Views, HTML, CSS, Bootstrap, JavaScript |
| Payments | Stripe.net |
| Version Control | Git / GitHub |

## Development Structure

The intended application organization is:

```text
BDTechMarket/
├── Controllers/
├── Data/
├── Models/
│   └── ViewModels/
├── Services/
│   └── Email/
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Cart/
│   ├── Category/
│   ├── Home/
│   ├── Order/
│   ├── Product/
│   ├── ProductSpecification/
│   └── Shared/
├── Migrations/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── images/
├── docs/
├── appsettings.json
├── Program.cs
└── BDTechMarket.csproj
```

See [docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) for the development conventions used for the project.

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server / LocalDB
- Visual Studio 2022 or another .NET 8 compatible IDE
- Git

### Run locally

Clone the repository:

```bash
git clone https://github.com/amrulhasa/my-first-project.git
cd my-first-project
```

Restore and build:

```bash
dotnet restore
dotnet build
```

Update the connection string in `appsettings.json` for your local SQL Server environment, then apply EF Core migrations:

```bash
dotnet ef database update
```

Run the application:

```bash
dotnet run
```

## Development Notes

- Development-only settings should not contain real production secrets.
- Do not commit API keys, payment secrets, SMTP passwords, connection-string passwords, or private certificates.
- Keep generated build output such as `bin/` and `obj/` out of source control.
- Database schema changes should be committed together with their EF Core migration files.

## Current Development Roadmap

- [x] Product catalog foundation
- [x] Brand management
- [x] Product specifications
- [x] Advanced catalog search and filtering
- [x] Pagination and sorting
- [ ] Inventory management and stock history
- [ ] Wishlist / favorites
- [ ] Product reviews and ratings
- [ ] Coupons and promotions
- [ ] Shipping management
- [ ] Admin analytics and audit logging
- [ ] Production deployment hardening

## Author

**Md. Amrul Hasan Sakib**

GitHub: [@amrulhasa](https://github.com/amrulhasa)

---

### Project Status

BDTechMarket is an actively developed project. Features and internal architecture continue to evolve toward a production-ready ecommerce workflow.
