"""The survey grid for the item-use critic: real verbs that reach it, real items a player can offer it.

Every item here is a Tool or a Weapon, because those are the only categories `GetCombinableItems`
puts in front of the critic (an item declaring `MadeForVerbIds` is settled by the gates before it).
Names and descriptions are copied from the item classes; goals are the verbs' own first-person
verbatims, as `VerbAction.ToNaturalLanguageString` hands them to the critic. Targets carry the
description their factory gives them — the current critic is never shown it, which is one of the
things this grid was built to measure.

    family     which kind of act (climb, cross, intrude, take, tiny, kill, and the eight required verbs)
    kind       improvised  — should help, and is not the obvious tool (the failure this grid is for)
               obvious     — should help, and is the tool or nearly
               refuse      — a real Tool or Weapon that cannot do this
               anchor      — absurd and unreachable in play (no such item is combinable); kept as a floor
    band       the proficiency band the verdict should demand: low / medium / high, or None when the
               degree is a matter of taste. Help/no-help is always labelled.
"""

ITEMS = {
    "iron_dagger_weapon": ("Weapon", "Iron Dagger", "A plain double-edged iron dagger with a tapered blade and a riveted pommel"),
    "short_sword": ("Weapon", "Short Sword", "A plain straight-bladed short sword in a worn leather scabbard"),
    "rope": ("Tool", "Rope", "A coil of twisted hemp rope, thick and rough-fibred"),
    "pitchfork": ("Tool", "Pitchfork", "A long-handled fork with two iron tines, for hay and dung"),
    "lariat": ("Tool", "Lariat", "A long braided rawhide rope with a running noose at one end"),
    "mattock": ("Tool", "Mattock", "A heavy iron head, an adze one side and a pick the other, on a thick haft"),
    "heavy_pick": ("Weapon", "Heavy Pick", "A miner's heavy iron pick, its point ground to a weapon edge"),
    "machete": ("Tool", "Machete", "A long broad cutting blade with a wooden grip, notched from cane"),
    "scythe": ("Tool", "Scythe", "A long curving scythe blade fitted to a tall snath"),
    "walking_staff": ("Tool", "Walking Staff", "A shoulder-high staff of seasoned ash, the foot shod with a scrap of iron"),
    "round_shield": ("Weapon", "Round Shield", "A round iron-bossed shield of limewood, the rim bound in leather"),
    "tower_shield": ("Weapon", "Tower Shield", "A tall kite-shaped shield of planked wood faced in iron plate"),
    "cavalry_saber": ("Weapon", "Cavalry Saber", "A curved single-edged saber with a brass hand guard and a sharkskin grip"),
    "knife": ("Tool", "Knife", "A short-bladed knife with a worn wooden handle, kept sharp"),
    "chisel": ("Tool", "Chisel", "A flat-bladed chisel with a wooden handle ringed in iron"),
    "shepherds_crook": ("Tool", "Shepherd's Crook", "A long ash staff with a worn horn hook, smoothed dark where the hand sits"),
    "pruning_hook": ("Tool", "Pruning Hook", "A short curved blade on an ash handle, for taking dead wood out of a tree"),
    "rake": ("Tool", "Rake", "A long-toothed wooden rake, the head bound with iron pegs"),
    "picking_basket": ("Tool", "Picking Basket", "A deep basket worn on the back, its straps dark with sweat"),
    "sickle": ("Tool", "Sickle", "A short iron sickle with a curved blade, edge nicked from rounds of harvest"),
    "fishing_net": ("Tool", "Fishing Net", "A hand-knotted net with cork floats, smelling permanently of the water"),
    "winnowing_fan": ("Tool", "Winnowing Fan", "A broad flat basket with a lip at the back, for tossing grain into the wind"),
    "mallet": ("Tool", "Mallet", "A wooden mallet, head squared off and dented from countless blows"),
    "razor": ("Tool", "Razor", "A folding steel razor with a horn handle, honed thin as a leaf"),
    "hunting_knife": ("Weapon", "Hunting Knife", "A thick-bladed hunting knife with a bone handle and a single-edged blade for gutting game"),
    "belaying_pin": ("Tool", "Belaying Pin", "A turned club of hard wood, a ship's rope-holder and a sailor's cosh"),
    "torch": ("Tool", "Torch", "A pine-resin torch on a short wooden handle, the head wrapped in charred cloth"),
    "cleaver": ("Tool", "Cleaver", "A heavy square-bladed chopper, its edge nicked by bone"),
    "flint": ("Tool", "Flint", "A sharp-edged flint nodule, one face knapped flat for striking fire"),
    "hunting_spear": ("Weapon", "Hunting Spear", "A hunting spear with a leaf-shaped iron point and a smooth ash shaft"),
    "hammer": ("Tool", "Hammer", "A blacksmith's hammer, head dulled and gleaming from rounds of strikes"),
    "fishing_line": ("Tool", "Fishing Line", "A coil of waxed linen fishing line"),
    "fish_hooks": ("Tool", "Bone Fish-Hooks", "A twist of line holding a half-dozen barbed bone hooks"),
    "war_pick": ("Weapon", "War Pick", "A single-pointed iron war pick with a hammerhead back and an oak haft"),
    "mill_pick": ("Tool", "Mill Pick", "A short double-ended pick for cutting the grooves back into a millstone"),
    "shovel": ("Tool", "Shovel", "A long-hafted shovel with a worn iron blade"),
    "hoe": ("Tool", "Hoe", "A long-hafted hoe, the blade worn to a crescent by stony ground"),
    "saw": ("Tool", "Saw", "A two-handed saw with coarse iron teeth, the blade flexed and worn"),
    "war_axe": ("Weapon", "War Axe", "A double-bitted war axe of dark iron, the hafts grooved for grip"),
    "bakers_peel": ("Tool", "Baker's Peel", "A long flat wooden shovel, the blade scorched black along its leading edge"),
    "watering_can": ("Tool", "Watering Pot", "A copper pot with a pierced rose on its long spout"),
    "bone_needle": ("Tool", "Bone Needle", "A polished bone needle carried pushed through a scrap of cloth"),
    "wooden_pipe": ("Tool", "Reed Pipe", "A five-holed pipe cut from elder, the mouthpiece worn oval"),
    "awl": ("Tool", "Awl", "A short steel spike set in a round wooden haft, for piercing leather"),
    "lancet": ("Tool", "Lancet", "A small double-edged blade in a tortoiseshell case, for letting blood"),
    "hook": ("Tool", "Hook", "A small barbed iron fish-hook"),
    "marlinspike": ("Tool", "Marlinspike", "A tapered iron spike for opening the strands of a rope"),
    "hoof_pick": ("Tool", "Hoof Pick", "A short hooked iron spike for cleaning a hoof"),
    "warhammer": ("Weapon", "Warhammer", "A flanged iron warhammer with a cross-peen head and a leather-wrapped haft"),
    "bone_dice": ("Tool", "Pair of Bone Dice", "Two yellowed dice cut from a knucklebone, the pips picked out in soot"),
    "tally_stick": ("Tool", "Tally Stick", "A hazel rod notched down one edge, the count of a season cut into it"),
    "cudgel": ("Weapon", "Cudgel", "A thick knotted branch trimmed into a fighting club, heavy at the head"),
    "wooden_spoon": ("Tool", "Wooden Spoon", "A long-handled wooden spoon, darkened with use and faintly scorched"),
    "candle": ("Tool", "Tallow Candle", "A stubby tallow candle on a clay base, half-burned and wax-spattered"),
    "whetstone": ("Tool", "Whetstone", "A pocket whetstone worn into a shallow saddle down the middle"),
    "currycomb": ("Tool", "Currycomb", "A rubbed iron comb with short teeth, for working the dust out of a coat"),
    "heavy_crossbow": ("Weapon", "Heavy Crossbow", "A heavy steel-prod crossbow with a windlass crank and a carved cheek-piece"),
    "longbow": ("Weapon", "Longbow", "A tall war longbow of laminated horn and sinew, the draw weight substantial"),
    "wooden_mallet": ("Tool", "Wooden Mallet", "A one-piece beech mallet, the face dented all round"),
    "stock_whip": ("Tool", "Stock Whip", "A long plaited leather whip on a short wooden handle"),
    # Unreachable in play (Food, Feather, an instrument): the brief's absurd floor, reported apart.
    "bread": ("Food", "Loaf of Bread", "A round brown loaf"),
    "feather": ("Other", "Feather", "A long grey feather"),
    "lute": ("Other", "Lute", "A pear-shaped lute"),
}

# How each item is grouped for the report: what a player would call it, not its C# category.
ITEM_GROUP = {
    "blade": "iron_dagger_weapon short_sword machete scythe cavalry_saber knife pruning_hook sickle razor hunting_knife cleaver war_axe saw lancet flint",
    "bludgeon": "mallet belaying_pin hammer warhammer cudgel wooden_mallet",
    "pick": "mattock heavy_pick war_pick mill_pick",
    "pole": "walking_staff shepherds_crook hunting_spear pitchfork rake hoe shovel bakers_peel",
    "cord": "rope lariat fishing_line fish_hooks fishing_net stock_whip hook",
    "spike": "chisel awl marlinspike hoof_pick bone_needle",
    "shield": "round_shield tower_shield",
    "vessel": "picking_basket winnowing_fan watering_can",
    "light": "torch candle",
    "trifle": "wooden_pipe bone_dice tally_stick wooden_spoon whetstone currycomb",
    "missile": "heavy_crossbow longbow",
    "anchor": "bread feather lute",
}
GROUP_OF = {i: g for g, ids in ITEM_GROUP.items() for i in ids.split()}

# CriticTrees.ToolPhrase over each required verb's ReferenceToolIds.
REFERENCE = {
    "cut": "a knife", "slay": "a knife", "fish": "a fishing rod or a net", "mine": "a pick",
    "cut_wood": "an axe", "dig": "a shovel", "break": "a hammer or an axe", "unlock_door": "a hairpin or a bone needle",
}

# Scenes, as WorldContext / NarrationNode describe them, and the targets acted on (factory wording).
GROVE = ("citrus grove", "on the drying floor — racks and mats of the crop laid out to dry, turned by hand")
VILLAGE = ("village", "in the lane outside the cooper's house")
WALL = "The grove house's coursed stone outside wall, with gaps enough for fingers and boot-toes going up to the eaves"
VWALL = "The cooper's house's timber outside wall, with gaps enough for fingers and boot-toes going up to the eaves"
CRAG = ("mountain", "at the foot of the crag")
CRAGT = "Shallow cups pecked into the rock a stride apart, going up to a dark alcove in the wall"
WELL = ("village", "at the well in the square")
WELLT = "The inside of the well shaft, wet stone going down into the dark"
BRAMBLES = ("forest", "at the edge of a clearing")
BRAMBLET = "A dense thicket of thorns grown across the way, too thick to see through"
MUD = "Churned ground that swallows a boot, wide enough to have to cross rather than skirt"
QUICK = ("salt marsh", "on a spit of firm ground")
QUICKT = "Grey sand that takes a foot and does not give it back, between here and the far reeds"
ICE = ("frozen coast", "on the shore ice")
ICET = "Sea ice gone thin over moving water, dark patches showing where it is weakest"
SNOW = ("glacier", "at the lip of a crevasse")
SNOWT = "A bridge of old snow over a crevasse, sagging in the middle"
TRUNK = "A fallen trunk lying across a gully, moss-slick on top, a long drop beneath"
SCREE = ("mountain", "below the pass")
SCREET = "Loose broken stone on a steep slope, sliding at every step"
LIANA = ("jungle", "on the game trail")
LIANAT = "A curtain of lianas and creeper grown across the way, thick as an arm in places"
CHIMNEY = "A smoke-hole wide enough at the head to take a body, and sooted the whole way down"
SHUTTER = "A shuttered window at shoulder height, the shutters barred on the inside"
CRYPT = ("village", "in the cooper's storeroom")
# A thing lying in the scene is described as CriticTarget.Describe does it: the item, then what holds it.
HAMT = "A smoked ham in a cloth, its rind dark and hard, from hams and sides of bacon hung from the smokehouse beams"
PEART = "A speckled green pear, narrow at the stem and heavy at the base, from a pear tree with narrow leaves and pale-green hanging fruit"
MUSHT = "A pale mushroom growing in shade at the base of a rock, from a cluster of cap-and-stem mushrooms half-hidden in leaf-litter"
THYMET = "A bundle of woody-stemmed thyme, fragrant and dry, from a low-clinging clump of thyme, fragrant in the warmth"
COMBT = "A dripping slab of dark wild honeycomb, grubs in some of the cells, sweet enough to hurt, from a wild bees' nest in a split in an oak, loud with bees"

SURVEY = [
    # ── climbing: scale_up / scale_down / climb_up / climb_down (optional) ──
    # The playtest case, verbatim, and its twin.
    ("climb", *GROVE, "scale the grove house wall", WALL, "iron_dagger_weapon", "help", "high", "improvised"),
    ("climb", *GROVE, "scale the grove house wall", WALL, "short_sword", "help", "high", "improvised"),
    ("climb", *GROVE, "scale the grove house wall", WALL, "pitchfork", "help", "high", "improvised"),
    ("climb", *GROVE, "scale the grove house wall", WALL, "lariat", "help", "medium", "improvised"),
    ("climb", *GROVE, "scale the grove house wall", WALL, "candle", "no", None, "refuse"),
    ("climb", *VILLAGE, "scale the cooper's house wall", VWALL, "hunting_knife", "help", "high", "improvised"),
    ("climb", *VILLAGE, "scale the cooper's house wall", VWALL, "bone_dice", "no", None, "refuse"),
    ("climb", *VILLAGE, "scale the cooper's house wall", VWALL, "wooden_pipe", "no", None, "refuse"),
    ("climb", *CRAG, "climb up the toe-hold trail", CRAGT, "mattock", "help", "medium", "improvised"),
    ("climb", *CRAG, "climb up the toe-hold trail", CRAGT, "war_pick", "help", "medium", "improvised"),
    ("climb", *CRAG, "climb up the toe-hold trail", CRAGT, "rope", "help", None, "improvised"),
    ("climb", *CRAG, "climb up the toe-hold trail", CRAGT, "whetstone", "no", None, "refuse"),
    ("climb", *CRAG, "climb down the toe-hold trail", CRAGT, "rope", "help", "low", "obvious"),
    ("climb", *WELL, "climb back down the well shaft", WELLT, "rope", "help", "low", "obvious"),
    ("climb", *WELL, "climb back down the well shaft", WELLT, "tally_stick", "no", None, "refuse"),

    # ── crossing (optional) ──
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "machete", "help", "low", "obvious"),
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "scythe", "help", "medium", "improvised"),
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "walking_staff", "help", "high", "improvised"),
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "round_shield", "help", "high", "improvised"),
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "cavalry_saber", "help", "medium", "improvised"),
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "bone_needle", "no", None, "refuse"),
    ("cross", *BRAMBLES, "make my way across the brambles", BRAMBLET, "tally_stick", "no", None, "refuse"),
    ("cross", "farm", "in the lower field", "make my way across the mud wallow", MUD, "walking_staff", "help", "medium", "improvised"),
    ("cross", *QUICK, "make my way across the quicksand", QUICKT, "walking_staff", "help", "medium", "improvised"),
    ("cross", *QUICK, "make my way across the quicksand", QUICKT, "hunting_spear", "help", "high", "improvised"),
    ("cross", *QUICK, "make my way across the quicksand", QUICKT, "bone_dice", "no", None, "refuse"),
    ("cross", *ICE, "make my way across the thin ice", ICET, "walking_staff", "help", "medium", "improvised"),
    ("cross", *ICE, "make my way across the thin ice", ICET, "candle", "no", None, "refuse"),
    ("cross", *SNOW, "make my way across the snow bridge", SNOWT, "tower_shield", "help", "high", "improvised"),
    ("cross", "forest", "at the edge of the gully", "make my way across the fallen trunk", TRUNK, "walking_staff", "help", "medium", "improvised"),
    ("cross", *SCREE, "make my way across the scree", SCREET, "walking_staff", "help", "low", "obvious"),
    ("cross", *SCREE, "make my way across the scree", SCREET, "wooden_spoon", "no", None, "refuse"),
    ("cross", *LIANA, "make my way across the lianas", LIANAT, "machete", "help", "low", "obvious"),
    ("cross", *LIANA, "make my way across the lianas", LIANAT, "hunting_knife", "help", "medium", "improvised"),
    ("cross", *LIANA, "make my way across the lianas", LIANAT, "heavy_crossbow", "no", None, "refuse"),

    # ── intrusion: slip_into (optional) ──
    ("intrude", "village", "up on the roof of the cooper's house", "slip in through the chimney", CHIMNEY, "rope", "help", "medium", "improvised"),
    ("intrude", "village", "up on the roof of the cooper's house", "slip in through the chimney", CHIMNEY, "bone_dice", "no", None, "refuse"),
    ("intrude", *VILLAGE, "slip in through the window", SHUTTER, "knife", "help", "medium", "improvised"),
    ("intrude", *VILLAGE, "slip in through the window", SHUTTER, "chisel", "help", "medium", "improvised"),
    ("intrude", *VILLAGE, "slip in through the window", SHUTTER, "wooden_pipe", "no", None, "refuse"),

    # ── taking: grab / gather / steal / pickpocket (optional) ──
    ("take", *CRYPT, "grab a smoked ham", HAMT, "shepherds_crook", "help", "medium", "improvised"),
    ("take", *CRYPT, "steal a smoked ham", HAMT, "pitchfork", "help", "medium", "improvised"),
    ("take", *CRYPT, "grab a smoked ham", HAMT, "currycomb", "no", None, "refuse"),
    ("take", "orchard", "under the old pear tree", "gather a pear", PEART, "pruning_hook", "help", "medium", "improvised"),
    ("take", "orchard", "under the old pear tree", "gather a pear", PEART, "rake", "help", "high", "improvised"),
    ("take", "orchard", "under the old pear tree", "gather a pear", PEART, "picking_basket", "help", "medium", "obvious"),
    ("take", "orchard", "under the old pear tree", "gather a pear", PEART, "heavy_crossbow", "no", None, "refuse"),
    ("take", "forest", "in a hollow among the roots", "gather a mushroom", MUSHT, "knife", "help", "medium", "obvious"),
    ("take", "meadow", "on the hillside", "gather a thyme", THYMET, "sickle", "help", "medium", "obvious"),
    ("take", "forest", "under a hollow oak", "gather a wild honeycomb", COMBT, "torch", "help", "medium", "improvised"),
    ("take", "village", "in the market square", "go through his pockets", "A merchant haggling at a stall, a fat purse tied at his belt", "razor", "help", "medium", "improvised"),
    ("take", "village", "in the market square", "go through his pockets", "A merchant haggling at a stall, a fat purse tied at his belt", "tower_shield", "no", None, "refuse"),

    # ── tiny creatures: catch / crush (optional; the verbatim says "in my hands") ──
    ("tiny", "meadow", "by the pond", "catch the frog in my hands", "A small green frog on a lily pad", "fishing_net", "help", "low", "obvious"),
    ("tiny", "meadow", "in the long grass", "catch the butterfly in my hands", "A white butterfly lifting from flower to flower", "winnowing_fan", "help", "high", "improvised"),
    ("tiny", "meadow", "in the long grass", "catch the butterfly in my hands", "A white butterfly lifting from flower to flower", "warhammer", "no", None, "refuse"),
    ("tiny", "desert", "among the rocks", "crush the scorpion", "A pale scorpion under the edge of a stone, tail raised", "mallet", "help", "low", "obvious"),
    ("tiny", "desert", "among the rocks", "crush the scorpion", "A pale scorpion under the edge of a stone, tail raised", "round_shield", "help", "medium", "improvised"),
    ("tiny", "desert", "among the rocks", "crush the scorpion", "A pale scorpion under the edge of a stone, tail raised", "fishing_line", "no", None, "refuse"),

    # ── killing a sleeper: murder (optional) ──
    ("kill", "village", "in the cooper's bedroom", "kill them where they lie", "The cooper asleep on a straw pallet, breathing slow", "hunting_knife", "help", "low", "obvious"),
    ("kill", "village", "in the cooper's bedroom", "kill them where they lie", "The cooper asleep on a straw pallet, breathing slow", "belaying_pin", "help", "medium", "improvised"),
    ("kill", "village", "in the cooper's bedroom", "kill them where they lie", "The cooper asleep on a straw pallet, breathing slow", "rope", "help", "medium", "improvised"),
    ("kill", "village", "in the cooper's bedroom", "kill them where they lie", "The cooper asleep on a straw pallet, breathing slow", "wooden_pipe", "no", None, "refuse"),

    # ── cut (required: a knife) ──
    ("cut", "forest", "beside the wolf carcass", "cut a pelt", "The carcass of a grey wolf, still warm", "iron_dagger_weapon", "help", "low", "improvised"),
    ("cut", "forest", "beside the boar carcass", "cut a meat", "The carcass of a boar, bristled and heavy", "cleaver", "help", "low", "improvised"),
    ("cut", "forest", "beside the boar carcass", "cut a meat", "The carcass of a boar, bristled and heavy", "sickle", "help", "medium", "improvised"),
    ("cut", "forest", "beside the boar carcass", "cut a meat", "The carcass of a boar, bristled and heavy", "scythe", "help", "high", "improvised"),
    ("cut", "forest", "beside the wolf carcass", "cut a pelt", "The carcass of a grey wolf, still warm", "flint", "help", "medium", "improvised"),
    ("cut", "forest", "beside the wolf carcass", "cut a pelt", "The carcass of a grey wolf, still warm", "wooden_mallet", "no", None, "refuse"),
    ("cut", "forest", "beside the wolf carcass", "cut a pelt", "The carcass of a grey wolf, still warm", "tally_stick", "no", None, "refuse"),

    # ── slay (required: a knife) ──
    ("slay", "plain", "in the long grass", "slay it", "A wounded deer down in the grass, too weak to rise", "hunting_spear", "help", "low", "improvised"),
    ("slay", "plain", "in the long grass", "slay it", "A wounded deer down in the grass, too weak to rise", "pitchfork", "help", "medium", "improvised"),
    ("slay", "plain", "in the long grass", "slay it", "A wounded deer down in the grass, too weak to rise", "hammer", "help", "medium", "improvised"),
    ("slay", "plain", "in the long grass", "slay it", "A wounded deer down in the grass, too weak to rise", "wooden_pipe", "no", None, "refuse"),

    # ── fish (required: a fishing rod or a net) ──
    ("fish", "coast", "on the shore of the pond", "fish the pond", "A reed-fringed pond, fish rising at the far side", "fish_hooks", "help", "low", "obvious"),
    ("fish", "coast", "on the shore of the pond", "fish the pond", "A reed-fringed pond, fish rising at the far side", "fishing_line", "help", "low", "obvious"),
    ("fish", "coast", "on the shore of the pond", "fish the pond", "A reed-fringed pond, fish rising at the far side", "hunting_spear", "help", "medium", "improvised"),
    ("fish", "coast", "on the shore of the pond", "fish the pond", "A reed-fringed pond, fish rising at the far side", "pitchfork", "help", "high", "improvised"),
    ("fish", "coast", "at the edge of the stream", "fish the stream", "A shallow stream running fast over stones, trout holding in the pools", "picking_basket", "help", "high", "improvised"),
    ("fish", "coast", "on the shore of the pond", "fish the pond", "A reed-fringed pond, fish rising at the far side", "candle", "no", None, "refuse"),
    ("fish", "coast", "on the shore of the pond", "fish the pond", "A reed-fringed pond, fish rising at the far side", "round_shield", "no", None, "refuse"),

    # ── mine (required: a pick) ──
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "heavy_pick", "help", "low", "obvious"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "mattock", "help", "low", "improvised"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "war_pick", "help", "medium", "improvised"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "chisel", "help", "high", "improvised"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "hammer", "help", "high", "improvised"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "wooden_spoon", "no", None, "refuse"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "fishing_net", "no", None, "refuse"),
    ("mine", "mountain", "at the copper seam", "work the copper seam for ore", "A seam of green-stained rock running out of sight into the wall", "bread", "no", None, "anchor"),

    # ── cut_wood (required: an axe) ──
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "saw", "help", "low", "obvious"),
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "war_axe", "help", "low", "obvious"),
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "machete", "help", "medium", "improvised"),
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "cleaver", "help", "high", "improvised"),
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "hunting_knife", "help", "high", "improvised"),
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "whetstone", "no", None, "refuse"),
    ("cut_wood", "forest", "beside the fallen oak", "cut wood from the fallen oak", "A great oak blown down across the clearing, its crown still in leaf", "bone_needle", "no", None, "refuse"),

    # ── dig (required: a shovel) ──
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "mattock", "help", "low", "improvised"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "hoe", "help", "medium", "improvised"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "bakers_peel", "help", "medium", "improvised"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "round_shield", "help", "high", "improvised"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "hunting_knife", "help", "high", "improvised"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "watering_can", "help", "high", "improvised"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "wooden_pipe", "no", None, "refuse"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "bone_needle", "no", None, "refuse"),
    ("dig", "river valley", "on the clay bank", "dig into the clay bank", "A bank of stiff yellow clay cut by the river", "feather", "no", None, "anchor"),

    # ── break (required: a hammer or an axe) ──
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "warhammer", "help", "low", "obvious"),
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "mattock", "help", "low", "improvised"),
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "cudgel", "help", "medium", "improvised"),
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "chisel", "help", "medium", "improvised"),
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "walking_staff", "help", "high", "improvised"),
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "fishing_line", "no", None, "refuse"),
    ("break", *CRYPT, "break the strongbox apart", "An iron-bound oak strongbox, padlocked", "candle", "no", None, "refuse"),

    # ── unlock_door (required: a hairpin or a bone needle) ──
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "awl", "help", "medium", "improvised"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "lancet", "help", "medium", "improvised"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "hook", "help", "medium", "improvised"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "hoof_pick", "help", "high", "improvised"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "marlinspike", "help", "high", "improvised"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "warhammer", "no", None, "refuse"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "tally_stick", "no", None, "refuse"),
    ("unlock", *VILLAGE, "unlock and open the cooper's door", "A plain timber door, the first of its kind, locked", "lute", "no", None, "anchor"),
]

VERB_OF = {"cut": "cut", "slay": "slay", "fish": "fish", "mine": "mine", "cut_wood": "cut_wood", "dig": "dig",
           "break": "break", "unlock": "unlock_door"}


def survey_cases():
    """The grid as bench cases: the same keys as critic_bench.cases, plus target, family, group, kind, band."""
    for i, (family, world, node, goal, target, item, label, band, kind) in enumerate(SURVEY):
        cat, name, desc = ITEMS[item]
        noun = name.lower()
        art = "an" if noun[0] in "aeiou" else "a"
        verb = VERB_OF.get(family)
        yield dict(id=f"survey{i:03d}", world=world, node=node, goal=goal, target=target,
                   item=f"the {noun}", item_context=f"{art} {noun} ({desc})", item_id=item,
                   tool=REFERENCE[verb] if verb else None, label=label, band=band, degree=None,
                   family=family, group=GROUP_OF[item], kind=kind)
