#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue5 done" queue5.log; do sleep 10; done
IT=$(cat sets/iter.txt)
for v in no_notscene minimal; do
  echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$IT" --kinds Observation,Reasoning,Action,Outcome,Emotion --seeds 1,2 --out samples/iter.jsonl > /dev/null
done
echo "queue6 done $(date +%T)"
