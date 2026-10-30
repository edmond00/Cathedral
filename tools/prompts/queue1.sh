#!/usr/bin/env bash
cd "$(dirname "$0")"
IT=$(cat sets/iter.txt)
echo "judge $(date +%T)"; JUDGE_PORT=8092 python3 -u ../judge/judge_bench.py --out results/judge_base.jsonl > results/judge_base.txt 2>&1
echo "critic $(date +%T)"; python3 -u critic_bench.py --variant base --out results/critic_base.jsonl > results/critic_base.txt 2>&1
for v in base reminder_is_voice voice_and_style_one_clause no_reminder; do
  echo "$v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$IT" --seeds 1,2 --out samples/iter.jsonl > /dev/null
done
echo "queue1 done $(date +%T)"
