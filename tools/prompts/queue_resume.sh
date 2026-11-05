#!/usr/bin/env bash
cd "$(dirname "$0")"
until curl -s localhost:8092/health | grep -q ok; do sleep 3; done
S=$(cat sets/spoken_dev.txt); Y=$(cat sets/styled_dev.txt); R=$(cat sets/person_reasoning.txt); IT=$(cat sets/iter.txt); L=$(cat sets/lesson_dev.txt)
run() { echo "$1 $2 $(date +%T)"; python3 -u run_samples.py --cases cases.jsonl --version $2 --variant $2 --ids "$3" --seeds $4 --out samples/$1.jsonl ${5:-} > /dev/null; }
run spoken spoken_compact "$S" 21,22
run styled base "$Y" 31,32
run styled no_notscene "$Y" 31,32
run person base "$R" 41,42,43,44
run person person_only "$R" 41,42,43,44
run r4 no_style "$IT" 5,6 "--kinds Observation,Reasoning,Action,Outcome,Emotion"
run r4 base "$IT" 5,6 "--kinds Observation,Reasoning,Action,Outcome,Emotion"
run lesson base "$L" 13,14
run lesson lesson_new "$L" 13,14
echo "resume done $(date +%T)"
