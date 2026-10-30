#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue3 done" queue3.log; do sleep 10; done
for v in in_its_place; do
  echo "critic $v $(date +%T)"; python3 -u critic_bench.py --variant $v --out results/critic_$v.jsonl > results/critic_$v.txt 2>&1
done
echo "queue4 done $(date +%T)"
