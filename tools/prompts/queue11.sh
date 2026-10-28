#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue10 done" queue10.log; do sleep 10; done
IT=$(cat sets/iter.txt)
for v in no_style base; do echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$IT" --kinds Observation,Reasoning,Action,Outcome,Emotion --seeds 5,6 --out samples/r4.jsonl > /dev/null; done
echo "queue11 done $(date +%T)"
