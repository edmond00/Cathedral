#!/usr/bin/env bash
cd "$(dirname "$0")"
until grep -q "queue8 done" queue8.log; do sleep 10; done
S=$(cat sets/spoken_dev.txt); Y=$(cat sets/styled_dev.txt)
for v in base spoken_compact; do echo "spoken $v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$S" --seeds 21,22 --out samples/spoken.jsonl > /dev/null; done
for v in base no_notscene; do echo "styled $v $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $v --variant $v --ids "$Y" --seeds 31,32 --out samples/styled.jsonl > /dev/null; done
echo "queue9 done $(date +%T)"
