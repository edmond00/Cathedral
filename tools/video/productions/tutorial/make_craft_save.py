"""The save the crafting take starts from (build/rec_craft_save.json): the city take's last checkpoint, moved
on to "later in the run" by hand.

    python make_craft_save.py

The run never reached crafting, and a recording cannot wait the weeks of play it would take, so the save
is given what a player would have earned by then, and the video says as much:
  - the trade, Shoemaking (the leather bench, the awl), at level 4, filed in procedural memory;
  - its implement, an awl, and a piece of tanned leather to break down, in the backpack;
  - the cobbler's leave to use his bench (the workshop's section, "Cobbler's Shop", in the city's
    AuthorizedWorkshops: a leave once given is kept for good);
  - the party on the world map at the farm, not at the city's own cell: a place cannot be re-entered
    from where it stands, and the walk back to the city is part of the scene.
Everything else is the checkpoint as it was.
"""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
SRC = HERE / "build" / "rec_city3" / "saves" / "02_routines.json"
OUT = HERE / "build" / "rec_craft_save.json"
CITY, WORKSHOP = "27788", "Cobbler's Shop"
TRADE, LEVEL = "shoemaking", 4
CARRY = ["awl", "tanned_leather"]
# No second trade: a make is rolled in the dice box and the humors may bend it, as every roll in the video.
SECOND, SECOND_LEVEL = None, 0
FARM = 7011


def item(item_id):
    return {"ItemId": item_id, "Composition": None, "Contents": None}


def main():
    save = json.loads(SRC.read_text())
    me = save["Party"]["Protagonist"]
    slots = me["Memory"]["Procedural"]
    for mm, level in ((TRADE, LEVEL), (SECOND, SECOND_LEVEL)) if SECOND else ((TRADE, LEVEL),):
        if not any(m["Id"] == mm for m in me["ModiMentis"]):
            me["ModiMentis"].append({"Id": mm, "Level": level, "Xp": 0})
        if mm not in slots:
            slots[slots.index(None)] = mm
    pack = next(i for i in me["Equipped"]["LeftHold"] if i["ItemId"] == "travelers_backpack")
    pack["Contents"] = (pack["Contents"] or []) + [item(i) for i in CARRY]
    city = save["Locations"][CITY]
    city["AuthorizedWorkshops"] = sorted(set(city.get("AuthorizedWorkshops") or []) | {WORKSHOP})
    save["AvatarVertex"] = FARM
    OUT.write_text(json.dumps(save))
    print(f"{OUT}: {TRADE} {LEVEL}, carrying {', '.join(CARRY)}, leave at {WORKSHOP} ({CITY})")


if __name__ == "__main__":
    main()
