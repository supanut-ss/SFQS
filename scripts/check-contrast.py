#!/usr/bin/env python3
"""Check WCAG AA contrast for every foreground/background token pair.

Badges use fontSize.xs (12px), which is not "large text", so every pair here
must reach 4.5:1 in both light and dark mode.

Usage: python scripts/check-contrast.py [tokens.json]
"""
import io
import json
import sys

THRESHOLD = 4.5

# (label, foreground token path, background token path)
PAIRS = [
    ('foreground / background', 'semantic.color.foreground', 'semantic.color.background'),
    ('foreground / surface', 'semantic.color.foreground', 'semantic.color.surface'),
    ('muted-foreground / background', 'semantic.color.muted-foreground', 'semantic.color.background'),
    ('primary-foreground / primary', 'semantic.color.primary-foreground', 'semantic.color.primary'),
    ('accent-foreground / accent', 'semantic.color.accent-foreground', 'semantic.color.accent'),
    ('destructive-foreground / destructive', 'semantic.color.destructive-foreground', 'semantic.color.destructive'),
    ('success / success-bg', 'semantic.color.success', 'semantic.color.success-bg'),
    ('warning / warning-bg', 'semantic.color.warning', 'semantic.color.warning-bg'),
    ('destructive / destructive-bg', 'semantic.color.destructive', 'semantic.color.destructive-bg'),
    ('info / info-bg', 'semantic.color.info', 'semantic.color.info-bg'),
    ('highlight / highlight-bg', 'semantic.color.highlight', 'semantic.color.highlight-bg'),
    ('import / import-bg', 'semantic.freight.import', 'semantic.freight.import-bg'),
    ('export / export-bg', 'semantic.freight.export', 'semantic.freight.export-bg'),
    ('mode-fcl / bg', 'semantic.freight.mode-fcl', 'semantic.freight.mode-fcl-bg'),
    ('mode-lcl / bg', 'semantic.freight.mode-lcl', 'semantic.freight.mode-lcl-bg'),
    ('mode-air / bg', 'semantic.freight.mode-air', 'semantic.freight.mode-air-bg'),
    ('in-transit / bg', 'semantic.freight.status-in-transit', 'semantic.freight.status-in-transit-bg'),
    ('delayed / bg', 'semantic.freight.status-delayed', 'semantic.freight.status-delayed-bg'),
    ('arrived / bg', 'semantic.freight.status-arrived', 'semantic.freight.status-arrived-bg'),
    ('pending-do / bg', 'semantic.freight.status-pending-do', 'semantic.freight.status-pending-do-bg'),
]


def node_at(tokens, path):
    node = tokens
    for key in path.split('.'):
        node = node[key]
    return node


def resolve(tokens, path, overrides=None):
    """Resolve a token path to a hex value, applying dark-mode overrides first."""
    if overrides:
        try:
            node = node_at(overrides, path[len('semantic.'):]) if path.startswith('semantic.') else None
        except KeyError:
            node = None
        if node is not None:
            return resolve(tokens, node['$value'][1:-1]) if str(node['$value']).startswith('{') else node['$value']
    node = node_at(tokens, path)
    value = node['$value'] if isinstance(node, dict) else node
    while isinstance(value, str) and value.startswith('{'):
        inner = node_at(tokens, value[1:-1])
        value = inner['$value'] if isinstance(inner, dict) else inner
    return value


def luminance(hex_color):
    h = hex_color.lstrip('#')
    channels = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    channels = [x / 12.92 if x <= 0.03928 else ((x + 0.055) / 1.055) ** 2.4 for x in channels]
    return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]


def ratio(fg, bg):
    hi, lo = sorted([luminance(fg), luminance(bg)], reverse=True)
    return (hi + 0.05) / (lo + 0.05)


def run(tokens, mode, overrides=None):
    failures = []
    print('--- %s mode' % mode)
    for label, fg_path, bg_path in PAIRS:
        fg, bg = resolve(tokens, fg_path, overrides), resolve(tokens, bg_path, overrides)
        r = ratio(fg, bg)
        ok = r >= THRESHOLD
        if not ok:
            failures.append((mode, label, r))
        print('  %-34s %-8s %-8s %5.2f  %s' % (label, fg, bg, r, 'PASS' if ok else 'FAIL'))
    return failures


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else 'tokens.json'
    tokens = json.load(io.open(path, encoding='utf-8'))
    dark = tokens.get('dark', {}).get('semantic', {})

    failures = run(tokens, 'light') + run(tokens, 'dark', overrides=dark)

    if failures:
        print('\n%d pair(s) below %.1f:1' % (len(failures), THRESHOLD))
        for mode, label, r in failures:
            print('  %s: %s (%.2f)' % (mode, label, r))
        sys.exit(1)
    print('\nAll %d pairs pass %.1f:1 in both modes.' % (len(PAIRS) * 2, THRESHOLD))


if __name__ == '__main__':
    main()
