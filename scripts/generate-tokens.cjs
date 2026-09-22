#!/usr/bin/env node
/**
 * Generate CSS custom properties from tokens.json.
 *
 * Unlike a flat resolver, token references are emitted as var() so that the
 * dark-mode block only has to override the semantic layer: component tokens
 * point at semantic vars and follow along automatically.
 *
 * Usage: node scripts/generate-tokens.cjs --config tokens.json -o tokens.css
 */

const fs = require('fs');
const path = require('path');

function parseArgs() {
  const args = process.argv.slice(2);
  const options = { config: 'tokens.json', output: null };
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--config' || args[i] === '-c') options.config = args[++i];
    else if (args[i] === '--output' || args[i] === '-o') options.output = args[++i];
  }
  return options;
}

/** `{semantic.color.primary}` -> `--color-primary` (primitive keeps its prefix) */
function refToVarName(ref) {
  const parts = ref.slice(1, -1).split('.');
  const scoped = parts[0] === 'primitive' ? parts : parts.slice(1);
  return '--' + scoped.join('-');
}

function isRef(value) {
  return typeof value === 'string' && value.startsWith('{') && value.endsWith('}');
}

function resolveRef(ref, tokens) {
  let node = tokens;
  for (const key of ref.slice(1, -1).split('.')) node = node && node[key];
  if (node && node.$value !== undefined) {
    return isRef(node.$value) ? resolveRef(node.$value, tokens) : node.$value;
  }
  return null;
}

function flatten(obj, prefix, tokens, out = {}) {
  for (const [key, value] of Object.entries(obj)) {
    if (!value || typeof value !== 'object') continue;
    const currentPath = [...prefix, key];
    if (value.$value !== undefined) {
      const name = '--' + currentPath.join('-');
      out[name] = isRef(value.$value)
        ? `var(${refToVarName(value.$value)})`
        : value.$value;
    } else {
      flatten(value, currentPath, tokens, out);
    }
  }
  return out;
}

function block(title, selector, vars) {
  if (!Object.keys(vars).length) return '';
  const body = Object.entries(vars).map(([k, v]) => `  ${k}: ${v};`).join('\n');
  return `\n/* === ${title} === */\n${selector} {\n${body}\n}\n`;
}

function generateCSS(tokens) {
  const primitive = flatten(tokens.primitive || {}, ['primitive'], tokens);
  const semantic = flatten(tokens.semantic || {}, [], tokens);
  const component = flatten(tokens.component || {}, [], tokens);
  const dark = {
    ...flatten(tokens.dark?.semantic || {}, [], tokens),
    ...flatten(tokens.dark?.component || {}, [], tokens),
  };

  return [
    '/* Design Tokens - Auto-generated */',
    '/* Do not edit directly - modify tokens.json instead */',
    block('PRIMITIVES', ':root', primitive),
    block('SEMANTIC', ':root', semantic),
    block('COMPONENTS', ':root', component),
    block('DARK MODE', '.dark', dark),
    block('DARK MODE (system preference)', '@media (prefers-color-scheme: dark) { :root:not([data-theme="light"])', dark).replace(/\n}\n$/, '\n} }\n'),
  ].join('');
}

/** Fail loudly on a reference that points at nothing — a silent var() typo is worse. */
function validate(tokens) {
  const broken = [];
  (function walk(obj, prefix) {
    for (const [key, value] of Object.entries(obj)) {
      if (!value || typeof value !== 'object') continue;
      const currentPath = [...prefix, key];
      if (value.$value !== undefined) {
        if (isRef(value.$value) && resolveRef(value.$value, tokens) === null) {
          broken.push(`${currentPath.join('.')} -> ${value.$value}`);
        }
      } else {
        walk(value, currentPath);
      }
    }
  })(tokens, []);
  return broken;
}

function main() {
  const options = parseArgs();
  const configPath = path.resolve(process.cwd(), options.config);
  const tokens = JSON.parse(fs.readFileSync(configPath, 'utf-8'));

  const broken = validate(tokens);
  if (broken.length) {
    console.error('Broken token references:\n  ' + broken.join('\n  '));
    process.exit(1);
  }

  const css = generateCSS(tokens);
  if (options.output) {
    fs.writeFileSync(path.resolve(process.cwd(), options.output), css);
    console.log(`Generated: ${options.output}`);
  } else {
    process.stdout.write(css);
  }
}

main();
