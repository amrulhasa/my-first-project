# Development Guide

## Recommended workflow

1. Pull the latest `main` branch.
2. Make a focused feature change.
3. Run `dotnet clean`.
4. Run `dotnet build`.
5. Apply EF Core migrations only when the database model changed.
6. Test the affected workflow locally.
7. Commit with a clear message.
8. Push to `main` only after the build succeeds.

## Database changes

When a model or EF configuration changes:

```powershell
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

Commit the generated migration files.

## Build

```powershell
dotnet clean
dotnet build
```

## Run

```powershell
dotnet run
```

## Security

Never commit real:

- Stripe secret keys
- SMTP passwords
- database passwords
- API tokens
- private certificates

Use local development configuration, environment variables, or .NET user-secrets for sensitive values.
