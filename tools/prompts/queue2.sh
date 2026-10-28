#!/usr/bin/env bash
cd "$(dirname "$0")"
for v in no_strict craftsman how_well; do
  echo "critic $v $(date +%T)"; python3 -u critic_bench.py --variant $v --out results/critic_$v.jsonl > results/critic_$v.txt 2>&1
done
echo "queue2 done $(date +%T)"
