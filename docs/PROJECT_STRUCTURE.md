# BDTechMarket Project Structure

This document defines the intended organization of the BDTechMarket ASP.NET Core MVC application.

## Top-level folders

| Folder | Responsibility |
|---|---|
| `Controllers/` | HTTP request handling, authorization, and application flow |
| `Data/` | Entity Framework Core DbContext and data access configuration |
| `Models/` | Entity models and domain objects |
| `Models/ViewModels/` | Request/response models used by Razor views |
| `Services/` | Reusable application services |
| `Services/Email/` | Email delivery implementations |
| `Views/` | Razor UI grouped by controller |
| `Migrations/` | EF Core database migrations |
| `wwwroot/` | Static CSS, JavaScript, and images |
| `docs/` | Development and architecture documentation |

## View organization

Razor views should follow the controller convention:

```text
Views/
├── Account/
├── Admin/
├── Cart/
├── Category/
├── Home/
├── Order/
├── Product/
├── ProductSpecification/
└── Shared/
```

A view belongs in the folder matching the controller that serves it. Shared layout and reusable Razor resources belong under `Views/Shared/`.

## Product module

The product module currently covers:

```text
Product
├── Catalog listing
├── Search
├── Category filtering
├── Brand filtering
├── Price filtering
├── Stock filtering
├── Sorting
├── Pagination
├── Details
├── Related products
└── Specifications
```

## Database conventions

- EF Core migrations belong under `Migrations/`.
- Schema changes should be committed together with their migration files.
- Product deactivation is preferred over destructive deletion where historical order references may exist.
- Navigation properties should be configured explicitly when delete behavior matters.

## Source-control conventions

Do not commit:

- `bin/`
- `obj/`
- IDE caches
- local environment secrets
- production credentials
- generated uploaded product images

