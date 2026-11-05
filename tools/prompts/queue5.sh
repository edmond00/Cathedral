#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue4 done" queue4.log; do sleep 10; done
IT=$(cat sets/iter.txt)
echo "spoken_compact $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version spoken_compact --variant spoken_compact --ids "$IT" --kinds DialogueReplica,Speaking --seeds 1,2 --out samples/iter.jsonl > /dev/null
for v in neutral_fixes with_hour short_setting; do
  echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$IT" --seeds 1,2 --out samples/iter.jsonl > /dev/null
done
echo "queue5 done $(date +%T)"
