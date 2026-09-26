# e2e (Playwright)

```bash
npm run e2e       # headless
npm run e2e:ui    # interactive UI mode
```

`playwright.config.ts` starts the Vite dev server automatically.

- `app-shell.spec.ts` — app shell routing and the dark mode toggle. No backend required.
- `ops.spec.ts` — login + ops Tabs/ConfirmDialog.
- `quotation-detail.spec.ts` — creates a quotation via the public Instant Quote
  form, then exercises the "+ Add Section" menu on the quotation detail page
  (add/remove a modular section, Escape-to-close).

The last two require the API running on `http://localhost:5025` (see repo
root README "First login" for the seeded admin credentials) since Vite
proxies `/api` to it, plus `docker-compose up -d` for MySQL and
`dotnet ef database update --project ../src/Freito.Infrastructure --startup-project ../src/Freito.Api`
applied. CI (`.github/workflows/web-e2e.yml`) sets all of this up
automatically on every push/PR touching `web/**` or `src/**`.
