#!/usr/bin/env node
/**
 * Fails if tokens.css is out of sync with tokens.json.
 *
 * generate-tokens.cjs is deterministic, so re-running it and diffing against
 * the committed tokens.css catches "edited tokens.json but forgot to
 * regenerate" before it reaches review.
 *
 * Usage: node scripts/check-tokens-fresh.mjs
 */
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const TOKENS_JSON = path.join(ROOT, 'tokens.json');
const TOKENS_CSS = path.join(ROOT, 'tokens.css');
const GENERATOR = path.join(ROOT, 'scripts', 'generate-tokens.cjs');

const fresh = execFileSync('node', [GENERATOR, '--config', TOKENS_JSON], { encoding: 'utf-8' });
const committed = fs.existsSync(TOKENS_CSS) ? fs.readFileSync(TOKENS_CSS, 'utf-8') : '';

if (fresh !== committed) {
  console.error('tokens.css is out of sync with tokens.json.');
  console.error('Run: npm run tokens:build');
  process.exit(1);
}

console.log('tokens.css matches tokens.json.');
