#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue2 done" queue2.log; do sleep 10; done
IT=$(cat sets/iter.txt)
for v in sentence_last closed_world sentence_last+closed_world; do
  echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$IT" --seeds 1,2 --out samples/iter.jsonl > /dev/null
done
echo "queue3 done $(date +%T)"
