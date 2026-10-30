#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue9 done" queue9.log; do sleep 10; done
R=$(cat sets/person_reasoning.txt)
for v in base person_only; do echo "person $v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$R" --seeds 41,42,43,44 --out samples/person.jsonl > /dev/null; done
echo "queue10 done $(date +%T)"
