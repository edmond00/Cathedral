#!/usr/bin/env bash
# Runs after the screening: the style-manner rule on its 20 dev personas, old and new fields, seeds 2 and 3.
cd "$(dirname "$0")"
while pgrep -f "[s]creen.py --out samples/screen.jsonl --seed 1" >/dev/null; do sleep 30; done
IDS=$(python3 -c "import json;print(','.join(json.load(open('sets/rule_style_dev.json'))))")
for seed in 2 3; do
  python3 -u screen.py --out samples/rule_style.jsonl --seed $seed --only "$IDS" --version current
  python3 -u screen.py --out samples/rule_style.jsonl --seed $seed --override rules/style_manner.json --version style_manner
done
echo "rules done $(date +%T)"
