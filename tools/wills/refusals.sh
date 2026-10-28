#!/usr/bin/env bash
# Count will refusals in play over a fixed route, for one build of the game.
#
#   tools/wills/refusals.sh <path-to-Cathedral-binary> <out-dir>
#
# Each run arrives in a location (--location-type, --location-id) holding the same eight wills, and has
# three of them think about the first keyword of the opening block (`click keyword 0`, Think, the will
# by id). Every thinking a will does ends one of three ways, which ThinkingExecutor logs:
#   "wills '…' — no way here."          nothing it wants can be done in this place
#   "wills '…' — '…' is not on the way." something can, but not through what it was asked about
#   "wills it — proposing '…'"          a step toward something it wants
# The first two are refusals. --playground and --seed make the route repeatable.
set -u
BIN="$1"; OUT="$2"; mkdir -p "$OUT"
WILLS=(herblore mycology brawling felling berrying lockpicking sextonry anglery)
PLACES=(forest plain village city "mushroom cellar")
for place in "${PLACES[@]}"; do
  for id in 1 2 3; do
    for start in 0 3; do
      tag="$(echo "$place" | tr ' ' _)-$id-$start"
      {
        echo wait; echo "click menu New"; echo "wait mode ProtagonistCreation 20"; echo "click continue"
        echo "wait mode WorldView 60"; echo "travel here"; echo "wait mode LocationInteraction 60"; echo "advance 12 180"
        for k in 0 1 2; do
          w=${WILLS[$(( (start + k) % ${#WILLS[@]} ))]}
          echo "click keyword 0"; echo "choose 0"; echo "choose $w"; echo "advance 12 180"
        done
        echo quit
      } > "$OUT/$tag.cli"
      ALSOFT_DRIVERS=null timeout 300 xvfb-run -a "$BIN" --playground --skip-childhood --seed 42 --no-encounters \
        --auto-dialogue --allow-reentry --silent --hidden --grant-mm "$(IFS=,; echo "${WILLS[*]}")" \
        --location-type "$place" --location-id "$id" --period morning --cli-timeout 240 \
        --cli-script "$OUT/$tag.cli" --log-file "$OUT/$tag.log" > "$OUT/$tag.out" 2>&1
    done
  done
done
no_way=$(cat "$OUT"/*.out | grep -c "— no way here\.")
not_on=$(cat "$OUT"/*.out | grep -c "is not on the way\.")
prop=$(cat "$OUT"/*.out | grep -c "wills it — proposing")
echo "no_way=$no_way not_on_the_way=$not_on proposing=$prop"
