"""Labelled cases for the choice judge: what a given modus mentis should choose, by decision kind.

Each case names modi mentis by id; their descriptions are read from the game source. A case is right
when the judge's decision (the game's rule over the letter scores) lands in `ok`. Willingness cases
come in pairs — one modus mentis made for the act (sign +1, should assent), one foreign to it (-1,
should dissent) — so the scale is tested in both directions, crimes included (`crim`).

When you add a modus mentis or a judge, add cases here that it must get right, including at least one
where the right answer is the decline and one where an obvious wrong answer is on the list.
"""

def _situation(place, area, obj=None):
    return f"You are in {place}, in the {area}." + (f" Your attention is on {obj}." if obj else "")

DECLINE_GOAL = "do something else entirely"
DECLINE_FOCUS = "let your focus go, nothing else here is worth attending to"
KEEP_YES = "yes - it touches what this faculty knows, does or cares about"
KEEP_NO = "no - it is nothing to this faculty"

WILLINGNESS = [  # place, area, object, act, made for it, foreign to it, criminal or violent
    ("a forest", "clearing", "a grey wolf", "attack the grey wolf", "ferocity", "dance", 1),
    ("a town", "merchant's street", "a merchant's house", "slip into the merchant's house unseen", "sneak_art", "boasting", 1),
    ("a village", "churchyard", "an open grave plot", "dig the grave plot", "gravework", "dance", 0),
    ("a mountain", "foot of the cliff", "a sheer cliff", "climb up the sheer cliff", "clambering", "stillness", 0),
    ("a town", "back alley", "a drunk lout", "attack the drunk lout", "brawling", "midwifery", 1),
    ("a village", "green", "a minstrel singing", "sing along with the minstrel", "busking", "rage", 0),
    ("a village", "storehouse", "a locked chest", "break the lock of the chest", "forcing", "lute_playing", 1),
    ("a town", "market square", "a merchant", "pickpocket the merchant", "cutpurse", "pilgrimage", 1),
    ("a town", "market square", "a baker's stall", "steal a loaf from the baker's stall", "petty_thief", "obsequies", 1),
    ("a castle", "guardroom", "a sleeping guard", "stab the sleeping guard", "cold_blood", "bonesetting", 1),
    ("a village", "churchyard", "a fresh grave", "rob the fresh grave", "grave_robbing", "anointing", 1),
    ("a farm", "barnyard", "a barn", "set fire to the barn", "arson_fire", "husbandry", 1),
    ("a village", "square", "a well", "poison the well", "poisoning", "water_bearing", 1),
    ("a farm", "byre", "a cow", "milk the cow", "dairycraft", "rage", 0),
    ("a farm", "sheepfold", "a sheep", "shear the sheep", "shearing", "pugilitas", 0),
    ("a town", "scriptorium", "a sealed letter", "forge the seal of the letter", "forgery", "brute_force", 1),
    ("a town", "city gate", "a gate guard", "bribe the gate guard", "bribery", "discipline", 1),
    ("a forest", "riverbank", "a wide river", "swim across the wide river", "natation", "tailoring", 0),
    ("a farm", "yard", "a fattened pig", "butcher the fattened pig", "butchery", "incensing", 0),
    ("a village", "green", "a feast", "dance at the feast", "dance", "stoneface", 0),
]
MEANS = [  # thinking, place, area, goal, action options, acceptable
    ("caution", "a town", "city gate", "get past the guard at the gate", ["sneak_art", "bribery", "brawling", "cookery", "dance"], ["sneak_art"]),
    ("cruelty", "a town", "city gate", "get past the guard at the gate", ["sneak_art", "bribery", "brawling", "cookery", "dance"], ["brawling"]),
    ("bargaining", "a town", "city gate", "get past the guard at the gate", ["sneak_art", "bribery", "brawling", "cookery", "dance"], ["bribery"]),
    ("curiosity", "a village", "storehouse", "open the locked chest", ["lockpicking", "forcing", "lute_playing", "cookery", "dance"], ["lockpicking", "forcing"]),
    ("herblore", "a village", "hall kitchen", "cook a stew for the hall", ["cookery", "brawling", "lockpicking", "gravework", "sneak_art"], ["cookery"]),
    ("piety", "a town", "market square", "earn a few coins in the market", ["busking", "card_sharping", "cutpurse", "brawling"], ["busking"]),
    ("covetousness", "a town", "market square", "earn a few coins in the market", ["busking", "card_sharping", "cutpurse", "brawling"], ["card_sharping", "cutpurse"]),
    ("mycology", "a forest", "old oak", "dig up the truffles under the oak", ["hard_labor", "lockpicking", "lute_playing", "bribery"], ["hard_labor"]),
    ("cruelty", "a castle", "dungeon", "make the prisoner talk", ["pugilitas", "bribery", "lute_playing", "cookery"], ["pugilitas"]),
    ("mercy", "a farm", "stable", "calm the frightened horse", ["horsemanship", "brawling", "forcing", "butchery"], ["horsemanship"]),
    ("caution", "a forest", "riverbank", "cross the river", ["natation", "fording", "rage", "lute_playing"], ["fording"]),
    ("temerity", "a forest", "riverbank", "cross the river", ["natation", "fording", "rage", "lute_playing"], ["natation"]),
    ("greed", "a town", "market square", "get the merchant's coins", ["cutpurse", "card_sharping", "hard_labor", "pilgrimage"], ["cutpurse", "card_sharping"]),
    ("herblore", "a village", "cottage", "treat the wounded leg", ["simpling", "bonesetting", "butchery", "forcing"], ["simpling"]),
    ("anatomy_lore", "a village", "cottage", "treat the wounded leg", ["simpling", "bonesetting", "butchery", "forcing"], ["bonesetting"]),
]
GOAL = [  # thinking, place, area, object, goals, acceptable (DECLINE_GOAL is always appended)
    ("herblore", "a forest", "clearing", "a herb patch", ["gather the herb", "attack the rabbit", "sit and wait", "climb up the oak"], ["gather the herb"]),
    ("covetousness", "a town", "market square", "a merchant", ["pickpocket the merchant", "greet the merchant", "sit and wait", "listen to the merchant"], ["pickpocket the merchant"]),
    ("piety", "a village", "chapel", "a shrine", ["contemplate the shrine", "steal the offering bowl", "provoke the priest", "sit and wait"], ["contemplate the shrine"]),
    ("cruelty", "a village", "yard", "a chained dog", ["attack the chained dog", "appease the chained dog", "listen to the chained dog", "sit and wait"], ["attack the chained dog"]),
    ("curiosity", "a ruin", "collapsed hall", "a strange carving", ["examine the strange carving closely", "sit and wait", "move to the road"], ["examine the strange carving closely"]),
    ("gluttony", "a village", "alehouse", "a meat pie on the counter", ["grab the meat pie", "examine the counter closely", "provoke the alewife", "sit and wait"], ["grab the meat pie"]),
    ("caution", "a forest", "clearing", "a brown bear", ["attack the brown bear", "provoke the brown bear", "move away to the road", "examine the brown bear closely"], ["move away to the road"]),
    ("mycology", "a forest", "glade", "a rotting log", ["gather the mushrooms", "cut the rotting log", "listen to the woodpecker", "sit and wait"], ["gather the mushrooms"]),
    ("mercy", "a town", "church steps", "a beggar", ["give alms to the beggar", "provoke the beggar", "pickpocket the beggar", "sit and wait"], ["give alms to the beggar"]),
    ("sloth", "a field", "wheat field", "a wheat field", ["sit and wait", "gather the wheat", "dig the field", "climb the fence"], ["sit and wait"]),
    ("greed", "a town", "market square", "a money-changer's table", ["snatch the coins from the table", "greet the money-changer", "listen to the street crier", "sit and wait"], ["snatch the coins from the table"]),
    ("astronomy", "a hill", "summit", "the night sky", ["study the stars", "gather the wild thyme", "walk down to the village", "sit and wait"], ["study the stars"]),
    ("spite", "a village", "green", "a boasting rival", ["provoke the boasting rival", "praise the boasting rival", "sit and wait", "walk away to the road"], ["provoke the boasting rival"]),
    ("wanderlust", "a plain", "crossroads", "a signpost", ["follow the road to the far hills", "sit and wait", "examine the signpost closely", "go back to the village"], ["follow the road to the far hills"]),
    ("thrift", "a village", "alehouse", "the innkeeper", ["buy the costly roast", "ask for the cheapest pottage", "sit and wait", "provoke the innkeeper"], ["ask for the cheapest pottage"]),
    ("reverence", "a village", "chapel", "an altar", ["pray at the altar", "steal the candlesticks", "examine the altar closely", "sit and wait"], ["pray at the altar"]),
    ("misanthropy", "a town", "market square", "a chattering crowd", ["leave the crowd for the quiet lane", "greet the crowd", "join the dancing", "listen to the gossip"], ["leave the crowd for the quiet lane"]),
    ("archeology", "a ruin", "buried cellar", "buried pottery", ["dig up the pottery shards", "attack the crow", "sit and wait", "walk to the road"], ["dig up the pottery shards"]),
    ("herblore", "a town", "smithy", "an anvil", ["strike the anvil", "provoke the smith", "buy a horseshoe"], [DECLINE_GOAL]),
    ("piety", "a town", "tavern", "a drunkard", ["provoke the drunkard", "steal the drunkard's purse", "challenge the drunkard to a drinking match"], [DECLINE_GOAL]),
    ("gluttony", "a town", "smithy", "the bellows", ["work the bellows", "strike the anvil", "examine the tongs closely"], [DECLINE_GOAL]),
]
FOCUS = [  # observation, place, area, objects, acceptable
    ("herblore", "a forest", "clearing", ["a herb patch", "a fallen log", "a grey wolf", "a woodpecker"], ["a herb patch"]),
    ("coin_eye", "a town", "market square", ["a money-changer's table", "a well", "a stray dog", "a fruit stall"], ["a money-changer's table"]),
    ("woodcraft", "a village", "green", ["an old oak", "a well", "a minstrel", "a stray dog"], ["an old oak"]),
    ("mycology", "a forest", "glade", ["a rotting log", "a deer", "a stream", "a ruined wall"], ["a rotting log"]),
    ("birdsong", "a farm", "orchard", ["a blackbird singing", "an apple tree", "a wheelbarrow", "a beehive"], ["a blackbird singing"]),
    ("gluttony", "a village", "alehouse", ["a meat pie on the counter", "a drunkard", "a hearth", "a lute on the wall"], ["a meat pie on the counter"]),
    ("dread", "a village", "graveyard", ["an open grave", "a yew tree", "a gate", "a raven"], ["an open grave", "a raven"]),
    ("architecture", "a town", "cathedral square", ["the cathedral facade", "a beggar", "a pigeon", "a market stall"], ["the cathedral facade"]),
    ("physiognomy", "a village", "alehouse", ["a scarred mercenary", "a barrel", "a hearth", "a mug"], ["a scarred mercenary"]),
    ("eavesdropping", "a village", "alehouse", ["two men whispering", "a barrel", "a hearth", "a cat"], ["two men whispering"]),
    ("horse_sense", "a farm", "stable", ["a lame horse", "a pitchfork", "a hay bale", "a water trough"], ["a lame horse"]),
    ("veinsight", "a mountain", "quarry", ["a seam of ore in the rock", "a cart", "a quarryman", "a crow"], ["a seam of ore in the rock"]),
    ("sky_reading", "a castle", "cellar", ["a barrel", "a rat", "a cobwebbed shelf"], [DECLINE_FOCUS]),
    ("herd_eye", "a monastery", "scriptorium", ["a lectern", "a manuscript", "an inkpot"], [DECLINE_FOCUS]),
]
KEEP = [  # place, area, object, keeps it, drops it
    ("a forest", "clearing", "a herb patch", "herblore", "soldiery"),
    ("a town", "market square", "a money-changer's table", "coin_eye", "birdsong"),
    ("a village", "green", "an old oak", "woodcraft", "coin_eye"),
    ("a farm", "orchard", "a blackbird singing", "birdsong", "veinsight"),
    ("a farm", "stable", "a lame horse", "horse_sense", "architecture"),
    ("a town", "cathedral square", "the cathedral facade", "architecture", "gluttony"),
    ("a village", "alehouse", "a meat pie on the counter", "gluttony", "sky_reading"),
    ("a village", "graveyard", "an open grave", "gravesight", "pomology"),
    ("a farm", "pasture", "a flock of sheep", "herd_eye", "sky_reading"),
    ("a forest", "clearing", "a deer track", "spoor_eye", "architecture"),
    ("a village", "alehouse", "a scarred mercenary", "physiognomy", "mycology"),
    ("a desert", "salt pan", "a slip face", "exploration", "gluttony"),
]
_BAKER = ("the baker, a stout man with floury arms", "buying bread", "the baker says the bread is fresh this morning",
          ["praise the baker's craft", "offer half the asking price", "insult the baker's bread", "ask about the baker's family"])
_SOLDIER = ("the soldier, a wounded veteran", "the war", "the soldier says he lost his brother at the ford",
            ["offer comfort for his brother's death", "ask how many enemies he killed", "mock him for losing", "ask about the road north"])
_PRIEST = ("the priest, an old village priest", "the chapel", "the priest says the chapel roof leaks",
           ["offer to pray with him", "joke about the leaking roof", "ask what the chapel's relics would sell for", "promise to pay for the roof"])
_GUARD = ("the guard, a bored town guard", "the gate toll", "the guard says the toll is two coins",
          ["pay the toll without fuss", "tell the guard the toll is robbery", "offer the guard a bribe to pass free", "ask politely to be let through for less"])
REPLY = [  # speaking, conversation, acceptable
    ("flattery", _BAKER, ["praise the baker's craft"]),
    ("invective", _BAKER, ["insult the baker's bread"]),
    ("bargaining", _BAKER, ["offer half the asking price", "insult the baker's bread"]),  # running down the goods is haggling
    ("courtesy", _BAKER, ["praise the baker's craft", "ask about the baker's family"]),
    ("mercy", _SOLDIER, ["offer comfort for his brother's death"]),
    ("cruelty", _SOLDIER, ["mock him for losing"]),
    ("piety", _PRIEST, ["offer to pray with him"]),
    ("sense_of_humor", _PRIEST, ["joke about the leaking roof"]),
    ("simony", _PRIEST, ["ask what the chapel's relics would sell for"]),
    ("insolence", _GUARD, ["tell the guard the toll is robbery"]),
    ("deference", _GUARD, ["pay the toll without fuss", "ask politely to be let through for less"]),
    ("bribery", _GUARD, ["offer the guard a bribe to pass free"]),
]

def all_cases(MM):
    """Every case as dict(kind, mm, situation, options, decline_index, ok | sign, crim, id).
    Options are written as the game writes them (second person; ChoiceJudge turns them to the third)."""
    out = []
    for place, area, obj, act, pos, neg, crim in WILLINGNESS:
        for mm, sign in ((pos, 1), (neg, -1)):
            out.append(dict(kind="Willingness", mm=mm, sign=sign, crim=crim, id=f"{act}|{mm}",
                            situation=_situation(place, area, obj) + f" You are considering whether to {act}."))
    for th, place, area, goal, opts, ok in MEANS:
        labels = [f"{MM[a]['name']}: {MM[a]['means']}" for a in opts]
        out.append(dict(kind="Means", mm=th, id=f"{goal}|{th}", decline_index=-1, options=labels,
                        ok=[f"{MM[a]['name']}: {MM[a]['means']}" for a in ok],
                        situation=_situation(place, area) + f" Your goal is to {goal}."))
    for th, place, area, obj, goals, ok in GOAL:
        out.append(dict(kind="Goal", mm=th, id=f"{obj}|{th}", options=goals + [DECLINE_GOAL], decline_index=len(goals),
                        ok=ok, situation=_situation(place, area, obj)))
    for mm, place, area, objs, ok in FOCUS:
        out.append(dict(kind="Focus", mm=mm, id=f"{area}|{mm}", options=[f"focus on {o}" for o in objs] + [DECLINE_FOCUS],
                        decline_index=len(objs), ok=[o if o == DECLINE_FOCUS else f"focus on {o}" for o in ok],
                        situation=_situation(place, area)))
    for place, area, obj, pos, neg in KEEP:
        for mm, sign in ((pos, 1), (neg, -1)):
            out.append(dict(kind="KeepFocus", mm=mm, id=f"{obj}|{mm}", options=[KEEP_YES, KEEP_NO], decline_index=1,
                            ok=[KEEP_YES] if sign > 0 else [KEEP_NO], fixed=True,
                            question=f"Does {obj} hold anything for this faculty?",
                            situation=_situation(place, area, obj)))
    for mm, (who, subject, said, opts), ok in REPLY:
        out.append(dict(kind="Reply", mm=mm, id=f"{subject}|{mm}", options=opts, decline_index=-1, ok=ok,
                        situation=f"You are speaking with {who}. The conversation is about {subject}. Just before this: {said}."))
    return out
