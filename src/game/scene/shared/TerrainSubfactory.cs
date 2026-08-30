using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Builders for natural-terrain points of interest used by plain, mountain, peak,
/// forest, cave and other outdoor scene factories. All methods return a freshly
/// constructed <see cref="PointOfInterest"/> with item drops already populated.
/// The caller is responsible for adding it to an Area and registering it.
/// </summary>
public static class TerrainSubfactory
{
    // ── Trees ────────────────────────────────────────────────────────────────

    public static PointOfInterest BuildOakTree() => new TreePointOfInterest(
        displayName: "Oak Tree",
        descriptions: new() { "A broad-crowned oak with deep-fissured bark and heavy boughs" },
        items: new()
        {
            new ItemElement(new Acorn()),
            new ItemElement(new Acorn()),
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "ancient", "broad-crowned", "spreading", "weathered", "rough-barked" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildBeechTree() => new TreePointOfInterest(
        displayName: "Beech Tree",
        descriptions: new() { "A tall pale beech, smooth-trunked and deep-rooted" },
        items: new()
        {
            new ItemElement(new Beechnut()),
            new ItemElement(new Beechnut()),
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "tall", "smooth", "pale", "still", "shaded" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildAshTree() => new TreePointOfInterest(
        displayName: "Ash Tree",
        descriptions: new() { "A grey-trunked ash with feather-leaved branches" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "tall", "grey", "fluttering", "open-canopied" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildBirchTree() => new TreePointOfInterest(
        displayName: "Birch Tree",
        descriptions: new() { "A slender silver-skinned birch, papery bark peeling in strips" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
            new ItemElement(new BirchSap()),
        },
        moods: new[] { "slender", "silver-barked", "papery", "trembling", "pale" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildPineTree() => new TreePointOfInterest(
        displayName: "Pine Tree",
        descriptions: new() { "A tall dark pine, resinous and heavy-needled" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
            new ItemElement(new PineSap()),
            new ItemElement(new PineCone()),
            new ItemElement(new PineNeedle()),
        },
        moods: new[] { "tall", "resinous", "dark", "wind-bent", "dense" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "petrichor" } };

    public static PointOfInterest BuildYewTree() => new TreePointOfInterest(
        displayName: "Yew Tree",
        descriptions: new() { "A squat dark yew with reddish bark and a heavy, low canopy" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "squat", "dark", "watchful", "ancient", "shadowed" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["contemplate"] = "iconography" } };

    public static PointOfInterest BuildHawthornTree() => new TreePointOfInterest(
        displayName: "Hawthorn Tree",
        descriptions: new() { "A thorny hawthorn standing alone, branches red with berries" },
        items: new()
        {
            new ItemElement(new HawthornBerry()),
            new ItemElement(new HawthornBerry()),
            new ItemElement(new Branch()),
            new ItemElement(new Thorn()),
        },
        moods: new[] { "thorny", "wind-bent", "small", "tangled", "lonely" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "hedgecraft", ["smell"] = "petrichor" } };

    public static PointOfInterest BuildWillowTree() => new TreePointOfInterest(
        displayName: "Willow Tree",
        descriptions: new() { "A weeping willow, long fronds trailing toward the wet ground" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "weeping", "trailing", "soft", "damp", "shaded" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildElderTree() => new TreePointOfInterest(
        displayName: "Elder Tree",
        descriptions: new() { "A spreading elder, dark umbels of berries weighing the branches" },
        items: new()
        {
            new ItemElement(new Elderberry()),
            new ItemElement(new Elderberry()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "shrubby", "fragrant", "spreading", "old" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose" } };

    public static PointOfInterest BuildAppleTree() => new TreePointOfInterest(
        displayName: "Apple Tree",
        descriptions: new() { "A gnarled old apple tree, branches heavy with fruit" },
        items: new()
        {
            new ItemElement(new Apple()),
            new ItemElement(new Apple()),
            new ItemElement(new AppleLeaf()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "gnarled", "laden", "shaded", "sweet", "old" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet", ["listen"] = "insect_chorus" } };

    public static PointOfInterest BuildPearTree() => new TreePointOfInterest(
        displayName: "Pear Tree",
        descriptions: new() { "A pear tree with narrow leaves and pale-green hanging fruit" },
        items: new()
        {
            new ItemElement(new Pear()),
            new ItemElement(new Pear()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "narrow-leaved", "laden", "tall", "ordered" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet", ["listen"] = "insect_chorus" } };

    public static PointOfInterest BuildPlumTree() => new TreePointOfInterest(
        displayName: "Plum Tree",
        descriptions: new() { "A plum tree, branches crowded with dusty-bloomed fruit" },
        items: new()
        {
            new ItemElement(new Plum()),
            new ItemElement(new Plum()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "crowded", "fruited", "low", "spreading" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet", ["listen"] = "insect_chorus" } };

    public static PointOfInterest BuildCherryTree() => new TreePointOfInterest(
        displayName: "Cherry Tree",
        descriptions: new() { "A cherry tree, leaves dark, branches studded with glossy red fruit" },
        items: new()
        {
            new ItemElement(new Cherry()),
            new ItemElement(new Cherry()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "dark-leaved", "laden", "modest", "tidy" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet", ["listen"] = "insect_chorus" } };

    // ── Cut / fallen wood ────────────────────────────────────────────────────

    public static PointOfInterest BuildFelledLog() => new LogPointOfInterest(
        displayName: "Felled Log",
        descriptions: new() { "A heavy log lying on its side, axe-marks fresh at one end" },
        items: new()
        {
            new ItemElement(new Log()),
            new ItemElement(new Log()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "fresh-cut", "heavy", "split", "wood-scented" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "petrichor" } };

    public static PointOfInterest BuildTreeStump() => new StumpPointOfInterest(
        displayName: "Tree Stump",
        descriptions: new() { "A weathered stump where a tree once stood, moss creeping over the bark" },
        items: new()
        {
            new ItemElement(new Mushroom()),
            new ItemElement(new Moss()),
        },
        moods: new[] { "weathered", "low", "damp", "mossy" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft" } };

    public static PointOfInterest BuildDeadfall() => new DeadfallPointOfInterest(
        displayName: "Deadfall Pile",
        descriptions: new() { "A heap of broken branches and fallen wood at the base of a hollow" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Branch()),
            new ItemElement(new Twig()),
        },
        moods: new[] { "tangled", "dry", "weathered", "splintered" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "mycology", ["smell"] = "taint_sense" } };

    // ── Rock features ────────────────────────────────────────────────────────

    public static PointOfInterest BuildBoulder() => new BoulderPointOfInterest(
        displayName: "Boulder",
        descriptions: new() { "A great half-buried boulder, the stone pitted and ringed with lichen" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
            new ItemElement(new Lichen()),
        },
        moods: new[] { "grey", "weathered", "massive", "silent", "half-buried" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    public static PointOfInterest BuildRockOutcrop() => new RockPointOfInterest(
        displayName: "Rock Outcrop",
        descriptions: new() { "A jut of bedrock breaking through the slope, edges sharp" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
            new ItemElement(new Lichen()),
        },
        moods: new[] { "sharp-edged", "exposed", "wind-scoured", "grey" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    public static PointOfInterest BuildRockFace() => new RockPointOfInterest(
        displayName: "Rock Face",
        descriptions: new() { "A sheer face of bedrock, fissured and wet in places" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
        },
        moods: new[] { "sheer", "looming", "wet", "fissured" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    public static PointOfInterest BuildFallenRocks() => new RockPointOfInterest(
        displayName: "Fallen Rocks",
        descriptions: new() { "A scatter of broken stone tumbled down from above" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Rock()),
        },
        moods: new[] { "scattered", "loose", "treacherous", "grey" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } };

    public static PointOfInterest BuildCrevice() => new CrevicePointOfInterest(
        displayName: "Crevice",
        descriptions: new() { "A narrow crevice between rocks, dark and deep" },
        items: new()
        {
            new ItemElement(new Flint()),
        },
        moods: new[] { "narrow", "dark", "deep", "echoing" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework", ["listen"] = "hollow_ear" } };

    public static PointOfInterest BuildCairn() => new CairnPointOfInterest(
        displayName: "Cairn",
        descriptions: new() { "A pile of stones left by past travellers, carefully balanced" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
        },
        moods: new[] { "balanced", "weathered", "lonely", "deliberate" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "iconography" } };

    // ── Water ────────────────────────────────────────────────────────────────

    public static PointOfInterest BuildStreamBank() => new StreamPointOfInterest(
        displayName: "Stream Bank",
        descriptions: new() { "A muddy bank where the stream cuts the earth, watercress in the slow eddy" },
        items: new()
        {
            new ItemElement(new Clay()),
            new ItemElement(new Watercress()),
            new ItemElement(new Rock()),
        },
        moods: new[] { "muddy", "wet", "cool", "slick" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "water_voice", ["smell"] = "taint_sense" } };

    public static PointOfInterest BuildGorgePool() => new PoolPointOfInterest(
        displayName: "Gorge Pool",
        descriptions: new() { "A still dark pool at the base of a gorge, edged with wet stone" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Clay()),
        },
        moods: new[] { "still", "dark", "cold", "wet" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "water_voice" } };

    // ── Vegetation patches ───────────────────────────────────────────────────

    public static PointOfInterest BuildFlowerPatch() => new FlowerPointOfInterest(
        displayName: "Flower Patch",
        descriptions: new() { "A sprawl of wildflowers in colour-clusters, bees moving between them" },
        items: new()
        {
            new ItemElement(new Daisy()),
            new ItemElement(new Poppy()),
            new ItemElement(new Clover()),
            new ItemElement(new Dandelion()),
        },
        moods: new[] { "bright", "fragrant", "scattered", "vivid" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose", ["contemplate"] = "aesthetic" } };

    public static PointOfInterest BuildBerryBush() => new BushPointOfInterest(
        displayName: "Berry Bush",
        descriptions: new() { "A thorny bush heavy with dark drupelets" },
        items: new()
        {
            new ItemElement(new Blackberry()),
            new ItemElement(new BushLeaf()),
            new ItemElement(new Thorn()),
        },
        moods: new[] { "thorny", "dense", "fruited", "tangled" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet" } };

    public static PointOfInterest BuildBilberryBush() => new BushPointOfInterest(
        displayName: "Bilberry Bush",
        descriptions: new() { "A low bilberry bush studded with small dark fruit" },
        items: new()
        {
            new ItemElement(new Bilberry()),
            new ItemElement(new BushLeaf()),
        },
        moods: new[] { "low", "tangled", "fruited", "dusky" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet" } };

    public static PointOfInterest BuildSloeBush() => new BushPointOfInterest(
        displayName: "Sloe Bush",
        descriptions: new() { "A blackthorn covered with hard blue-black sloes" },
        items: new()
        {
            new ItemElement(new Sloe()),
            new ItemElement(new Thorn()),
        },
        moods: new[] { "thorny", "blue-black", "wind-bent", "wild" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "ripelore", ["smell"] = "bouquet" } };

    public static PointOfInterest BuildMushroomCluster() => new MushroomPointOfInterest(
        displayName: "Mushroom Cluster",
        descriptions: new() { "A cluster of cap-and-stem mushrooms half-hidden in leaf-litter" },
        items: new()
        {
            new ItemElement(new Mushroom()),
            new ItemElement(new Mushroom()),
        },
        moods: new[] { "earthy", "damp", "hidden", "small" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "mycology", ["smell"] = "taint_sense", ["contemplate"] = "aesthetic" } };

    public static PointOfInterest BuildUndergrowthPatch() => new UndergrowthPointOfInterest(
        displayName: "Undergrowth Patch",
        descriptions: new() { "A snarled patch of low growth, ferns and brambles tangled together" },
        items: new()
        {
            new ItemElement(new Fern()),
            new ItemElement(new Bramble()),
            new ItemElement(new Nettle()),
            new ItemElement(new Ivy()),
        },
        moods: new[] { "tangled", "low", "shaded", "snarled" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "forage_lore", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildReedBed() => new ReedPointOfInterest(
        displayName: "Reed Bed",
        descriptions: new() { "A stand of tall reeds growing out of the soft wet ground" },
        items: new()
        {
            new ItemElement(new Reed()),
            new ItemElement(new Reed()),
            new ItemElement(new Clay()),
        },
        moods: new[] { "tall", "rustling", "wet", "papery" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork", ["listen"] = "forge_ear", ["contemplate"] = "journeyman_eye" } };

    public static PointOfInterest BuildMossBank() => new MossPointOfInterest(
        displayName: "Moss Bank",
        descriptions: new() { "A thick cushion of moss spread over rock and root" },
        items: new()
        {
            new ItemElement(new Moss()),
            new ItemElement(new Moss()),
        },
        moods: new[] { "soft", "damp", "green", "thick" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose" } };

    public static PointOfInterest BuildAlpineHerbPatch() => new HerbPointOfInterest(
        displayName: "Alpine Herb Patch",
        descriptions: new() { "A small clutch of fragrant herbs sheltered in a hollow" },
        items: new()
        {
            new ItemElement(new WildThyme()),
            new ItemElement(new WildMint()),
            new ItemElement(new Valerian()),
        },
        moods: new[] { "fragrant", "small", "sheltered", "rare" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose", ["contemplate"] = "aesthetic" } };

    public static PointOfInterest BuildLichenCrust() => new LichenPointOfInterest(
        displayName: "Lichen Crust",
        descriptions: new() { "A papery crust of grey-green lichen spread across the stone" },
        items: new()
        {
            new ItemElement(new Lichen()),
            new ItemElement(new Lichen()),
        },
        moods: new[] { "papery", "grey-green", "weathered", "thin" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore" } };

    public static PointOfInterest BuildShelteredHollow() => new HollowPointOfInterest(
        displayName: "Sheltered Hollow",
        descriptions: new() { "A small hollow out of the wind, rare alpine herbs growing in the lee" },
        items: new()
        {
            new ItemElement(new Gentian()),
            new ItemElement(new Valerian()),
        },
        moods: new[] { "sheltered", "rare", "small", "still" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bushcraft", ["listen"] = "birdsong" } };

    public static PointOfInterest BuildIceFormation() => new IcePointOfInterest(
        displayName: "Ice Formation",
        descriptions: new() { "A wind-carved sculpture of ice glittering in the cold light" },
        items: new(),
        moods: new[] { "glittering", "wind-carved", "frozen", "still" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["contemplate"] = "aesthetic" } };
}
