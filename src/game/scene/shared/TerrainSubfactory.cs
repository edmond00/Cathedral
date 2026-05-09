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

    public static PointOfInterest BuildOakTree() => new(
        displayName: "Oak Tree",
        descriptions: new() { "A broad-crowned oak with deep-fissured bark and heavy boughs" },
        items: new()
        {
            new ItemElement(new Acorn()),
            new ItemElement(new Acorn()),
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "ancient", "broad-crowned", "spreading", "weathered", "rough-barked" }
    );

    public static PointOfInterest BuildBeechTree() => new(
        displayName: "Beech Tree",
        descriptions: new() { "A tall pale beech, smooth-trunked and deep-rooted" },
        items: new()
        {
            new ItemElement(new Beechnut()),
            new ItemElement(new Beechnut()),
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "tall", "smooth", "pale", "still", "shaded" }
    );

    public static PointOfInterest BuildAshTree() => new(
        displayName: "Ash Tree",
        descriptions: new() { "A grey-trunked ash with feather-leaved branches" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "tall", "grey", "fluttering", "open-canopied" }
    );

    public static PointOfInterest BuildBirchTree() => new(
        displayName: "Birch Tree",
        descriptions: new() { "A slender silver-skinned birch, papery bark peeling in strips" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
            new ItemElement(new BirchSap()),
        },
        moods: new[] { "slender", "silver-barked", "papery", "trembling", "pale" }
    );

    public static PointOfInterest BuildPineTree() => new(
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
        moods: new[] { "tall", "resinous", "dark", "wind-bent", "dense" }
    );

    public static PointOfInterest BuildYewTree() => new(
        displayName: "Yew Tree",
        descriptions: new() { "A squat dark yew with reddish bark and a heavy, low canopy" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "squat", "dark", "watchful", "ancient", "shadowed" }
    );

    public static PointOfInterest BuildHawthornTree() => new(
        displayName: "Hawthorn Tree",
        descriptions: new() { "A thorny hawthorn standing alone, branches red with berries" },
        items: new()
        {
            new ItemElement(new HawthornBerry()),
            new ItemElement(new HawthornBerry()),
            new ItemElement(new Branch()),
            new ItemElement(new Thorn()),
        },
        moods: new[] { "thorny", "wind-bent", "small", "tangled", "lonely" }
    );

    public static PointOfInterest BuildWillowTree() => new(
        displayName: "Willow Tree",
        descriptions: new() { "A weeping willow, long fronds trailing toward the wet ground" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "weeping", "trailing", "soft", "damp", "shaded" }
    );

    public static PointOfInterest BuildElderTree() => new(
        displayName: "Elder Tree",
        descriptions: new() { "A spreading elder, dark umbels of berries weighing the branches" },
        items: new()
        {
            new ItemElement(new Elderberry()),
            new ItemElement(new Elderberry()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "shrubby", "fragrant", "spreading", "old" }
    );

    public static PointOfInterest BuildAppleTree() => new(
        displayName: "Apple Tree",
        descriptions: new() { "A gnarled old apple tree, branches heavy with fruit" },
        items: new()
        {
            new ItemElement(new Apple()),
            new ItemElement(new Apple()),
            new ItemElement(new AppleLeaf()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "gnarled", "laden", "shaded", "sweet", "old" }
    );

    public static PointOfInterest BuildPearTree() => new(
        displayName: "Pear Tree",
        descriptions: new() { "A pear tree with narrow leaves and pale-green hanging fruit" },
        items: new()
        {
            new ItemElement(new Pear()),
            new ItemElement(new Pear()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "narrow-leaved", "laden", "tall", "ordered" }
    );

    public static PointOfInterest BuildPlumTree() => new(
        displayName: "Plum Tree",
        descriptions: new() { "A plum tree, branches crowded with dusty-bloomed fruit" },
        items: new()
        {
            new ItemElement(new Plum()),
            new ItemElement(new Plum()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "crowded", "fruited", "low", "spreading" }
    );

    public static PointOfInterest BuildCherryTree() => new(
        displayName: "Cherry Tree",
        descriptions: new() { "A cherry tree, leaves dark, branches studded with glossy red fruit" },
        items: new()
        {
            new ItemElement(new Cherry()),
            new ItemElement(new Cherry()),
            new ItemElement(new Branch()),
        },
        moods: new[] { "dark-leaved", "laden", "modest", "tidy" }
    );

    // ── Cut / fallen wood ────────────────────────────────────────────────────

    public static PointOfInterest BuildFelledLog() => new(
        displayName: "Felled Log",
        descriptions: new() { "A heavy log lying on its side, axe-marks fresh at one end" },
        items: new()
        {
            new ItemElement(new Log()),
            new ItemElement(new Log()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "fresh-cut", "heavy", "split", "wood-scented" }
    );

    public static PointOfInterest BuildTreeStump() => new(
        displayName: "Tree Stump",
        descriptions: new() { "A weathered stump where a tree once stood, moss creeping over the bark" },
        items: new()
        {
            new ItemElement(new Mushroom()),
            new ItemElement(new Moss()),
        },
        moods: new[] { "weathered", "low", "damp", "mossy" }
    );

    public static PointOfInterest BuildDeadfall() => new(
        displayName: "Deadfall Pile",
        descriptions: new() { "A heap of broken branches and fallen wood at the base of a hollow" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Branch()),
            new ItemElement(new Twig()),
        },
        moods: new[] { "tangled", "dry", "weathered", "splintered" }
    );

    // ── Rock features ────────────────────────────────────────────────────────

    public static PointOfInterest BuildBoulder() => new(
        displayName: "Boulder",
        descriptions: new() { "A great half-buried boulder, the stone pitted and ringed with lichen" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
            new ItemElement(new Lichen()),
        },
        moods: new[] { "grey", "weathered", "massive", "silent", "half-buried" }
    );

    public static PointOfInterest BuildRockOutcrop() => new(
        displayName: "Rock Outcrop",
        descriptions: new() { "A jut of bedrock breaking through the slope, edges sharp" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
            new ItemElement(new Lichen()),
        },
        moods: new[] { "sharp-edged", "exposed", "wind-scoured", "grey" }
    );

    public static PointOfInterest BuildRockFace() => new(
        displayName: "Rock Face",
        descriptions: new() { "A sheer face of bedrock, fissured and wet in places" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
        },
        moods: new[] { "sheer", "looming", "wet", "fissured" }
    );

    public static PointOfInterest BuildFallenRocks() => new(
        displayName: "Fallen Rocks",
        descriptions: new() { "A scatter of broken stone tumbled down from above" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Rock()),
        },
        moods: new[] { "scattered", "loose", "treacherous", "grey" }
    );

    public static PointOfInterest BuildCrevice() => new(
        displayName: "Crevice",
        descriptions: new() { "A narrow crevice between rocks, dark and deep" },
        items: new()
        {
            new ItemElement(new Flint()),
        },
        moods: new[] { "narrow", "dark", "deep", "echoing" }
    );

    public static PointOfInterest BuildCairn() => new(
        displayName: "Cairn",
        descriptions: new() { "A pile of stones left by past travellers, carefully balanced" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Flint()),
        },
        moods: new[] { "balanced", "weathered", "lonely", "deliberate" }
    );

    // ── Water ────────────────────────────────────────────────────────────────

    public static PointOfInterest BuildStreamBank() => new(
        displayName: "Stream Bank",
        descriptions: new() { "A muddy bank where the stream cuts the earth, watercress in the slow eddy" },
        items: new()
        {
            new ItemElement(new Clay()),
            new ItemElement(new Watercress()),
            new ItemElement(new Rock()),
        },
        moods: new[] { "muddy", "wet", "cool", "slick" }
    );

    public static PointOfInterest BuildGorgePool() => new(
        displayName: "Gorge Pool",
        descriptions: new() { "A still dark pool at the base of a gorge, edged with wet stone" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Clay()),
        },
        moods: new[] { "still", "dark", "cold", "wet" }
    );

    // ── Vegetation patches ───────────────────────────────────────────────────

    public static PointOfInterest BuildFlowerPatch() => new(
        displayName: "Flower Patch",
        descriptions: new() { "A sprawl of wildflowers in colour-clusters, bees moving between them" },
        items: new()
        {
            new ItemElement(new Daisy()),
            new ItemElement(new Poppy()),
            new ItemElement(new Clover()),
            new ItemElement(new Dandelion()),
        },
        moods: new[] { "bright", "fragrant", "scattered", "vivid" }
    );

    public static PointOfInterest BuildBerryBush() => new(
        displayName: "Berry Bush",
        descriptions: new() { "A thorny bush heavy with dark drupelets" },
        items: new()
        {
            new ItemElement(new Blackberry()),
            new ItemElement(new BushLeaf()),
            new ItemElement(new Thorn()),
        },
        moods: new[] { "thorny", "dense", "fruited", "tangled" }
    );

    public static PointOfInterest BuildBilberryBush() => new(
        displayName: "Bilberry Bush",
        descriptions: new() { "A low bilberry bush studded with small dark fruit" },
        items: new()
        {
            new ItemElement(new Bilberry()),
            new ItemElement(new BushLeaf()),
        },
        moods: new[] { "low", "tangled", "fruited", "dusky" }
    );

    public static PointOfInterest BuildSloeBush() => new(
        displayName: "Sloe Bush",
        descriptions: new() { "A blackthorn covered with hard blue-black sloes" },
        items: new()
        {
            new ItemElement(new Sloe()),
            new ItemElement(new Thorn()),
        },
        moods: new[] { "thorny", "blue-black", "wind-bent", "wild" }
    );

    public static PointOfInterest BuildMushroomCluster() => new(
        displayName: "Mushroom Cluster",
        descriptions: new() { "A cluster of cap-and-stem mushrooms half-hidden in leaf-litter" },
        items: new()
        {
            new ItemElement(new Mushroom()),
            new ItemElement(new Mushroom()),
        },
        moods: new[] { "earthy", "damp", "hidden", "small" }
    );

    public static PointOfInterest BuildUndergrowthPatch() => new(
        displayName: "Undergrowth Patch",
        descriptions: new() { "A snarled patch of low growth, ferns and brambles tangled together" },
        items: new()
        {
            new ItemElement(new Fern()),
            new ItemElement(new Bramble()),
            new ItemElement(new Nettle()),
            new ItemElement(new Ivy()),
        },
        moods: new[] { "tangled", "low", "shaded", "snarled" }
    );

    public static PointOfInterest BuildReedBed() => new(
        displayName: "Reed Bed",
        descriptions: new() { "A stand of tall reeds growing out of the soft wet ground" },
        items: new()
        {
            new ItemElement(new Reed()),
            new ItemElement(new Reed()),
            new ItemElement(new Clay()),
        },
        moods: new[] { "tall", "rustling", "wet", "papery" }
    );

    public static PointOfInterest BuildMossBank() => new(
        displayName: "Moss Bank",
        descriptions: new() { "A thick cushion of moss spread over rock and root" },
        items: new()
        {
            new ItemElement(new Moss()),
            new ItemElement(new Moss()),
        },
        moods: new[] { "soft", "damp", "green", "thick" }
    );

    public static PointOfInterest BuildAlpineHerbPatch() => new(
        displayName: "Alpine Herb Patch",
        descriptions: new() { "A small clutch of fragrant herbs sheltered in a hollow" },
        items: new()
        {
            new ItemElement(new WildThyme()),
            new ItemElement(new WildMint()),
            new ItemElement(new Valerian()),
        },
        moods: new[] { "fragrant", "small", "sheltered", "rare" }
    );

    public static PointOfInterest BuildLichenCrust() => new(
        displayName: "Lichen Crust",
        descriptions: new() { "A papery crust of grey-green lichen spread across the stone" },
        items: new()
        {
            new ItemElement(new Lichen()),
            new ItemElement(new Lichen()),
        },
        moods: new[] { "papery", "grey-green", "weathered", "thin" }
    );

    public static PointOfInterest BuildShelteredHollow() => new(
        displayName: "Sheltered Hollow",
        descriptions: new() { "A small hollow out of the wind, rare alpine herbs growing in the lee" },
        items: new()
        {
            new ItemElement(new Gentian()),
            new ItemElement(new Valerian()),
        },
        moods: new[] { "sheltered", "rare", "small", "still" }
    );

    public static PointOfInterest BuildIceFormation() => new(
        displayName: "Ice Formation",
        descriptions: new() { "A wind-carved sculpture of ice glittering in the cold light" },
        items: new(),
        moods: new[] { "glittering", "wind-carved", "frozen", "still" }
    );
}
