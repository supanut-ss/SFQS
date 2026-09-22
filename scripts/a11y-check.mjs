#!/usr/bin/env node
/**
 * Automated accessibility check for style-guide.html using axe-core.
 *
 * Serves the project root over a plain HTTP server (axe needs same-origin
 * script injection, which a bare file:// page won't give it), loads the
 * page in headless Chromium via Puppeteer, and runs axe in both light and
 * dark mode. Exits non-zero and prints details on any violation.
 *
 * Usage: node scripts/a11y-check.mjs [path-to-html]  (default: style-guide.html)
 */
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const TARGET = process.argv[2] || 'style-guide.html';
const AXE_PATH = path.join(ROOT, 'node_modules', 'axe-core', 'axe.min.js');

const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json' };

function serveStatic(root) {
  return http.createServer((req, res) => {
    const reqPath = decodeURIComponent(req.url.split('?')[0]);
    const filePath = path.join(root, reqPath === '/' ? '/index.html' : reqPath);
    if (!filePath.startsWith(root)) { res.writeHead(403); res.end(); return; }
    fs.readFile(filePath, (err, data) => {
      if (err) { res.writeHead(404); res.end('Not found'); return; }
      res.writeHead(200, { 'Content-Type': MIME[path.extname(filePath)] || 'application/octet-stream' });
      res.end(data);
    });
  });
}

async function runAxe(page) {
  return page.evaluate(async () => {
    const results = await window.axe.run(document, { resultTypes: ['violations'] });
    return results.violations.map((v) => ({
      id: v.id,
      impact: v.impact,
      help: v.help,
      nodes: v.nodes.map((n) => n.target.join(' ')),
    }));
  });
}

async function main() {
  if (!fs.existsSync(AXE_PATH)) {
    console.error(`axe-core not found at ${AXE_PATH}. Run "npm install" first.`);
    process.exit(2);
  }

  const server = serveStatic(ROOT);
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const { port } = server.address();

  const browser = await puppeteer.launch({ headless: true, args: ['--no-sandbox'] });
  let exitCode = 0;

  try {
    const page = await browser.newPage();
    await page.goto(`http://127.0.0.1:${port}/${TARGET}`, { waitUntil: 'load' });
    await page.addScriptTag({ path: AXE_PATH });

    for (const mode of ['light', 'dark']) {
      if (mode === 'dark') {
        await page.evaluate(() => document.documentElement.classList.add('dark'));
      }
      const violations = await runAxe(page);
      if (violations.length === 0) {
        console.log(`[${mode}] 0 violations`);
      } else {
        exitCode = 1;
        console.log(`[${mode}] ${violations.length} violation(s):`);
        for (const v of violations) {
          console.log(`  - ${v.id} (${v.impact}): ${v.help}`);
          for (const target of v.nodes) console.log(`      ${target}`);
        }
      }
    }
  } finally {
    await browser.close();
    server.close();
  }

  if (exitCode === 0) {
    console.log('\naxe-core: all checks passed in light and dark mode.');
  } else {
    console.error('\naxe-core: violations found (see above).');
  }
  process.exit(exitCode);
}

main().catch((err) => {
  console.error(err);
  process.exit(2);
});
