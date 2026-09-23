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

## Checks

```bash
npm install
npm run check   # tokens freshness + WCAG AA contrast + axe-core a11y audit (light & dark)
```

Runs automatically in CI on every push/PR that touches the design system — see [.github/workflows/design-system-checks.yml](.github/workflows/design-system-checks.yml).
