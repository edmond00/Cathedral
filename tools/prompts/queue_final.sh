#!/usr/bin/env bash
cd "$(dirname "$0")"
until curl -s localhost:8092/health | grep -q ok; do sleep 3; done
D=$(cat sets/final_dev.txt); H=$(cat sets/final_heldout.txt)
for v in base final; do
  echo "dev $v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$D" --seeds 51,52 --out samples/final.jsonl > /dev/null
  echo "held $v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$H" --seeds 51,52 --out samples/final.jsonl > /dev/null
done
for v in base current; do
  echo "critic dev $v $(date +%T)"; python3 -u critic_bench.py --variant $v --out results/final_critic_dev_$v.jsonl > results/final_critic_dev_$v.txt 2>&1
  echo "critic held $v $(date +%T)"; python3 -u critic_bench.py --variant $v --heldout --out results/final_critic_held_$v.jsonl > results/final_critic_held_$v.txt 2>&1
done
echo "final done $(date +%T)"
