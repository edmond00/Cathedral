#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue7 done" queue7.log; do sleep 10; done
L=$(cat sets/lesson_dev.txt)
for v in base managed_only lesson_new lesson_named; do
  echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$L" --seeds 11,12 --out samples/lesson.jsonl > /dev/null
done
echo "queue8 done $(date +%T)"
