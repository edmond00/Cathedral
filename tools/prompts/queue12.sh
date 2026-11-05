#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue11 done" queue11.log; do sleep 10; done
L=$(cat sets/lesson_dev.txt)
for v in base lesson_new; do echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$L" --seeds 13,14 --out samples/lesson.jsonl > /dev/null; done
echo "queue12 done $(date +%T)"
