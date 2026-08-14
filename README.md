# SMS-Hazard

**An Enterprise Web-Based Safety Management System for Hazard Reporting, Risk Assessment, and Corrective Action Management.**

Capstone project for **CSCD602 — Advanced Software Engineering**.
Student: **Prince Ofoe Duodu** (ID **22427657**, Cohort B).

Digitises the ICAO-style safety loop — **Hazard Identification → Risk Assessment → Corrective Action → Assurance (verify & close)** — into a single auditable web application.

---

## Stack
- **C# / ASP.NET Core MVC / .NET 10**
- **Clean Architecture** — 4 projects (Domain ← Application ← Infrastructure, Web composition root)
- **PostgreSQL** via EF Core (Npgsql) · **ASP.NET Core Identity** (roles: Reporter, SafetyOfficer, Manager, Admin)
- **MailKit/SMTP** email · **Hangfire** durable reminders
- Deployed behind **Apache** on an Ubuntu VPS with Let's Encrypt TLS (no containers)

## Solution layout
```
SMSHazard.sln
src/
  SMSHazard.Domain/          entities, enums, RiskScore value object, hazard state machine (no external deps)
  SMSHazard.Application/     interfaces, DTOs, settings, DI (use-case services added per phase)
  SMSHazard.Infrastructure/  EF Core DbContext + configs, Identity, MailKit email, DI
  SMSHazard.Web/             MVC controllers, views, Program.cs (composition root), /health
tests/
  SMSHazard.Tests/           xUnit unit tests (RiskScore, state machine, overdue detection)
```

## Prerequisites
- .NET 10 SDK — verify: `dotnet --version` (expect `10.x`)
- PostgreSQL running locally with a `smshazard` role/database (or adjust the connection string)
- EF Core tools: `dotnet tool install --global dotnet-ef`

## Local run
```bash
# 1) restore & build
dotnet build

# 2) run the unit tests (no DB needed)
dotnet test

# 3) create the initial database migration (REQUIRED before first run —
#    Program.cs calls Database.Migrate() but no migration exists yet)
dotnet ef migrations add InitialCreate \
  --project src/SMSHazard.Infrastructure \
  --startup-project src/SMSHazard.Web

# 4) run (Development uses appsettings.Development.json connection string)
dotnet run --project src/SMSHazard.Web
#   → http://localhost:5000  and  http://localhost:5000/health  → "healthy"
```

> Set your local Postgres connection string in `src/SMSHazard.Web/appsettings.Development.json`.
> In production, configuration comes from the systemd env file (see `vps-deploy-plan.md`), never from committed files.

## Configuration keys (bound from environment on the VPS)
| Key | Purpose |
|---|---|
| `ConnectionStrings__Default` | Npgsql connection string |
| `Email__Host` / `Port` / `User` / `Password` / `From` | SMTP (Mailjet: `in-v3.mailjet.com:587`, User = API Key, Password = Secret Key) |
| `Storage__AttachmentsPath` | Absolute attachments dir (outside deploy dir) |
| `DataProtection__KeysPath` | Absolute Data-Protection keys dir (persist across deploys) |

## Security notes
- No secrets in the repo. `*.env`, `secrets.json`, and `appsettings.*.Local.json` are git-ignored.
- Production secrets live in `/etc/sms-hazard/sms-hazard.env` (mode 600), loaded by systemd.

## Live application
- URL: _(added after deployment — see `22427657_SMS_Hazard/Deployment_and_Source_Links.txt`)_
- Demo credentials: _(added after deployment)_

## Documentation
Full engineering deliverables (SRS with Use Case Points estimation, Technical Debt Plan, Project Documentation, Testing Report, User Manual, diagrams) are in the `22427657_SMS_Hazard/` submission folder.

## Build status / roadmap
See `CHANGELOG.md` in the project root for the live build tracker (phases, stages, decisions, technical debt).

## Third-party components
.NET 10 / ASP.NET Core, EF Core, Npgsql, ASP.NET Core Identity, FluentValidation, MailKit/MimeKit, Hangfire & Hangfire.PostgreSql, Bootstrap, Chart.js — used under their respective open-source licences.
