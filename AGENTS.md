# Netflix Clone - Agent Guidelines

## Project Goal

Netflix-like streaming web application built as a portfolio project
for learning production-oriented ASP.NET Core backend development.

Primary goal:
- Build a working product.
- Preserve understanding of architecture and business logic.
- Prefer simple, explicit implementations over unnecessary abstractions.

## Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Database First
- Clean Architecture
- BCrypt for password hashing

## Architecture

Projects:

- NetflixClone.Domain
- NetflixClone.Application
- NetflixClone.Infrastructure
- NetflixClone.Api

Dependency rules:

- Domain depends on nothing.
- Application depends only on Domain.
- Infrastructure depends on Application and Domain.
- API depends on Application and Infrastructure.
- Application must not reference Infrastructure, EF Core, ASP.NET Core,
  or dependency injection packages.

## Database First Rules

The SQL schema is the source of truth.

When schema changes:
1. Update database scripts.
2. Update DBML/ERD.
3. Apply the SQL change to SQL Server.
4. Re-scaffold EF Core intentionally.

Do not manually edit scaffold-generated entity or DbContext files.
Use partial classes for custom behavior.

## Application Rules

- Use cases live in Application.
- Application depends on abstractions, not Infrastructure implementations.
- Use Result<T> for expected business failures.
- Do not use exceptions for normal business outcomes.
- Do not introduce MediatR or CQRS libraries unless explicitly requested.

## Persistence Rules

- Repositories are use-case driven.
- Do not create generic repositories.
- Repositories must not call SaveChangesAsync.
- Use IUnitOfWork to commit changes once per use case where appropriate.

## Security Rules

- Never store plaintext passwords.
- Passwords use BCrypt.
- Email confirmation/reset tokens are cryptographically random.
- Only token hashes are stored in the database.
- Avoid account enumeration in public authentication endpoints.
- Do not log real authentication tokens or secrets outside local development.

## Current Authentication Status

Completed:
- Register Account
- Email Confirmation
- Resend Email Confirmation

Current feature:
- Login

Login must consider:
- invalid credentials
- EmailConfirmed
- manual IsLocked
- FailedLoginCount
- LockoutEnd

JWT and refresh-token issuance will be implemented in later steps.

## Working Style

Before changing code:
1. Inspect existing patterns.
2. Explain the intended change.
3. List files expected to change.
4. Do not implement unrelated refactors.

After changing code:
1. Run dotnet build.
2. Report changed files.
3. Explain important decisions.
4. Call out assumptions and unresolved issues.