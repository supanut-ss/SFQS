#!/usr/bin/env node
/**
 * Copies the design system's generated tokens.css into src/styles so Vite
 * can import it. tokens.json / tokens.css at the repo root stay the single
 * source of truth (see ../scripts/generate-tokens.cjs) — this script never
 * edits them, only mirrors the output in for the build.
 *
 * Runs automatically before `dev` and `build` (see package.json).
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const WEB_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const REPO_ROOT = path.resolve(WEB_ROOT, '..');

const source = path.join(REPO_ROOT, 'tokens.css');
const dest = path.join(WEB_ROOT, 'src', 'styles', 'tokens.css');

if (!fs.existsSync(source)) {
  console.error(`tokens.css not found at ${source}. Run "npm run tokens:build" at the repo root first.`);
  process.exit(1);
}

fs.mkdirSync(path.dirname(dest), { recursive: true });
fs.copyFileSync(source, dest);
console.log(`Synced tokens.css -> ${path.relative(WEB_ROOT, dest)}`);
