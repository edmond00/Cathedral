#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue6 done" queue6.log; do sleep 10; done
IT=$(cat sets/iter.txt)
echo "base seeds 3,4 $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version base --variant base --ids "$IT" --kinds Observation,Reasoning,Action,Outcome,Emotion --seeds 3,4 --out samples/iter.jsonl > /dev/null
echo "queue7 done $(date +%T)"
