# e2e (Playwright)

```bash
npm run e2e       # headless
npm run e2e:ui    # interactive UI mode
```

`playwright.config.ts` starts the Vite dev server automatically.

- `app-shell.spec.ts` — app shell routing and the dark mode toggle. No backend required.
- `ops.spec.ts` — login + ops Tabs/ConfirmDialog. **Requires the API running** on
  `http://localhost:5025` (see repo root README "First login" for the seeded
  admin credentials and `dotnet ef database update` setup) since Vite proxies
  `/api` to it. Not yet wired into CI — run these manually against a local API
  + `docker-compose up -d` MySQL until that's set up.
