# Freito — Smart Freight Quotation System (SFQS)

B2B freight quotation platform (FCL / LCL / Air) — instant draft pricing with a mandatory Sale approval gate before any quote reaches a customer, and real-time freight-rate management for Operations.

## Documents

| File | Purpose |
|---|---|
| [requirements.md](requirements.md) | Business & functional requirements, data model, quotation lifecycle |
| [project-plan.md](project-plan.md) | MVP scope, milestones, success metrics |
| [technical-plan.md](technical-plan.md) | Architecture, schema, quote engine rules, API surface (React + ASP.NET Core + MySQL) |
| [work-plan.md](work-plan.md) | Master task breakdown, acceptance criteria, delegation, risks |
| [operation-worksheet.md](operation-worksheet.md) | Pricing formulas confirmed with Operation/Sale, verified against real quotes |
| [operation-open-questions.md](operation-open-questions.md) | Remaining non-blocking follow-up documents requested from Operation |
| [design-system-spec.md](design-system-spec.md) | Design tokens, decisions, and component specs |
| [ui-plan.md](ui-plan.md) | UI audit, information architecture, CI/a11y setup |

## Design system

- [tokens.json](tokens.json) — three-layer design tokens (primitive → semantic → component → dark)
- [tokens.css](tokens.css) — generated CSS custom properties (`npm run tokens:build`)
- [style-guide.html](style-guide.html) — browsable style guide with a light/dark toggle

## Backend dev setup

Requires .NET 10 SDK. Dev MySQL runs via Docker (matches `appsettings.Development.json`):

```bash
docker compose up -d                          # starts MySQL on localhost:3306 (freito_dev)
cd src/Freito.Api
dotnet ef database update --project ../Freito.Infrastructure --startup-project .
dotnet run
```

```bash
dotnet build && dotnet test   # from src/Freito.Tests or any project folder
```

### First login

A bootstrap Admin account is seeded by migration so the system isn't a chicken-and-egg problem
(no self-signup — Admin creates every other account via `POST /api/master/users`):

```
email:    admin@freito.local
password: ChangeMe123!
```

**Change this password immediately** via `PUT /api/master/users/1` after first login — it's a
well-known value checked into this repo. `Jwt:Key` in `appsettings.Development.json` is a dev-only
value too; the production deployment must set its own `Jwt:Key` (and never reuse this one).

## Checks

```bash
npm install
npm run check   # tokens freshness + WCAG AA contrast + axe-core a11y audit (light & dark)
```

Runs automatically in CI on every push/PR that touches the design system — see [.github/workflows/design-system-checks.yml](.github/workflows/design-system-checks.yml).
