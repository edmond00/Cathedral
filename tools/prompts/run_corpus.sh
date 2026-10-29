#!/usr/bin/env bash
# Plays every corpus playthrough in scripts/runs.tsv, one after another, into runs/<name>/.
cd "$(dirname "$0")"
while IFS=$'\t' read -r -u 3 name flags; do
    [ -n "${ONLY:-}" ] && [[ " $ONLY " != *" $name "* ]] && continue
    [ -f "runs/$name/cli.txt" ] && { echo "skip $name"; continue; }
    echo "== $name $(date +%T)"
    eval bash run_game.sh "$PWD/scripts/corpus_$name.cli" "$PWD/runs/$name" $flags
done 3< scripts/runs.tsv
echo "corpus done $(date +%T)"
