using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Builders for the points of interest the hot and cold country shares between biomes — the
/// counterpart of <see cref="TerrainSubfactory"/>, which holds the temperate ones. A builder used by
/// one factory only lives in that factory.
///
/// <para>Every one returns a fresh object with its items placed, its senses set and its lessons
/// declared, and every lesson id is one <c>--verb-audit</c> resolves. Trees are
/// <see cref="TreePointOfInterest"/> whatever their species, so a palm is cut, climbed past and
/// listened to exactly as an oak is.</para>
/// </summary>
public static class ClimateTerrainSubfactory
{
    private static ItemElement I(Item item) => new(item);

    private static Dictionary<string, string> Lessons(params (string Verb, string Mm)[] pairs)
    {
        var d = new Dictionary<string, string>();
        foreach (var (verb, mm) in pairs) d[verb] = mm;
        return d;
    }

    // ── Hot trees ─────────────────────────────────────────────────────────────

    public static PointOfInterest BuildDatePalm() => new TreePointOfInterest(
        displayName: "Date Palm",
        descriptions: new() { "A tall date palm, its trunk scaled with old leaf-bases, heavy amber clusters under the crown" },
        items: new() { I(new Date()), I(new Date()), I(new PalmFrond()), I(new Twig()) },
        moods: new[] { "tall", "rustling", "laden", "sun-struck" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Lessons(("examine", "ripelore"), ("listen", "wind_reading"), ("smell", "bouquet")) };

    public static PointOfInterest BuildAcacia() => new TreePointOfInterest(
        displayName: "Acacia",
        descriptions: new() { "A flat-topped acacia, its crown spread like a parasol over a pool of thin shade, its branches all thorn" },
        items: new() { I(new Thornwood()), I(new AcaciaGum()), I(new Thorn()) },
        moods: new[] { "flat-crowned", "thorny", "lonely", "shade-casting" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "woodcraft"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildBaobab() => new TreePointOfInterest(
        displayName: "Baobab",
        descriptions: new() { "A baobab with a trunk like a cistern, grey and swollen, its few branches held up like roots against the sky" },
        items: new() { I(new BaobabFruit()), I(new BaobabFruit()), I(new Bark()), I(new Branch()) },
        moods: new[] { "swollen", "ancient", "grey", "absurd" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = Lessons(("examine", "woodcraft"), ("contemplate", "awe"), ("listen", "hollow_ear")) };

    public static PointOfInterest BuildKapok() => new TreePointOfInterest(
        displayName: "Kapok Tree",
        descriptions: new() { "A kapok rising on plank buttresses taller than a man, its trunk studded with blunt spines, lost in the canopy above" },
        items: new() { I(new Branch()), I(new Bark()), I(new Liana()) },
        moods: new[] { "towering", "buttressed", "dim", "ancient" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = Lessons(("examine", "woodcraft"), ("contemplate", "awe"), ("listen", "birdsong")) };

    public static PointOfInterest BuildStranglerFig() => new TreePointOfInterest(
        displayName: "Strangler Fig",
        descriptions: new() { "A fig that grew down from a branch and wrapped its host in a lattice of roots; the host is gone and the lattice stands hollow" },
        items: new() { I(new JungleFig()), I(new JungleFig()), I(new Branch()) },
        moods: new[] { "latticed", "hollow", "patient", "sinister" },
        isNatural: true
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = Lessons(("examine", "woodcraft"), ("contemplate", "vanitas"), ("listen", "insect_chorus"), ("smell", "bouquet")) };

    public static PointOfInterest BuildMahogany() => new TreePointOfInterest(
        displayName: "Mahogany Tree",
        descriptions: new() { "A straight red-barked mahogany, its first branch higher than a house, heavy seed-pods hanging" },
        items: new() { I(new Branch()), I(new Bark()), I(new Bark()) },
        moods: new[] { "straight", "red-barked", "tall", "valuable" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = Lessons(("examine", "woodcraft")) };

    public static PointOfInterest BuildCacaoTree() => new TreePointOfInterest(
        displayName: "Cacao Tree",
        descriptions: new() { "A small understorey tree with ridged pods growing straight out of its trunk, yellow and red" },
        items: new() { I(new CacaoPod()), I(new CacaoPod()), I(new Branch()) },
        moods: new[] { "small", "shaded", "laden", "odd" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "ripelore"), ("smell", "bouquet"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildWildBanana() => new TreePointOfInterest(
        displayName: "Wild Banana",
        descriptions: new() { "A wild banana with leaves as long as a man, torn to ribbons by the rain, a stiff hand of fruit under them" },
        items: new() { I(new Banana()), I(new Banana()), I(new PalmFrond()) },
        moods: new[] { "ragged", "broad-leaved", "dripping", "green" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "forage_lore"), ("smell", "bouquet"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildJuniper() => new TreePointOfInterest(
        displayName: "Juniper",
        descriptions: new() { "A twisted juniper growing out of a crack in the rock, half its trunk dead and silver, the rest blue with berries" },
        items: new() { I(new JuniperBerry()), I(new JuniperBerry()), I(new Branch()) },
        moods: new[] { "twisted", "silvered", "stubborn", "fragrant" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "woodcraft"), ("smell", "apothecary_nose"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildPinyon() => new TreePointOfInterest(
        displayName: "Pinyon Pine",
        descriptions: new() { "A squat round pinyon pine, sticky with resin, its small cones open on the nuts" },
        items: new() { I(new PinyonNut()), I(new PinyonNut()), I(new PineSap()), I(new PineCone()) },
        moods: new[] { "squat", "resinous", "rounded", "dark" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "forage_lore"), ("smell", "petrichor"), ("contemplate", "aesthetic")) };

    // ── Cold trees ────────────────────────────────────────────────────────────

    public static PointOfInterest BuildStuntedFir() => new TreePointOfInterest(
        displayName: "Stunted Fir",
        descriptions: new() { "A fir no taller than a child, all its growth on the lee side, flattened by a century of wind into a crouch" },
        items: new() { I(new Twig()), I(new PineNeedle()), I(new PineSap()) },
        moods: new[] { "crouched", "wind-shorn", "dark", "ancient" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Smell: true), VerbModiMentis = Lessons(("examine", "wind_reading"), ("contemplate", "vanitas"), ("smell", "petrichor")) };

    public static PointOfInterest BuildDwarfBirch() => new TreePointOfInterest(
        displayName: "Dwarf Birch",
        descriptions: new() { "A birch that grows along the ground instead of up it, its round leaves red at the edges" },
        items: new() { I(new Twig()), I(new Bark()), I(new BirchSap()) },
        moods: new[] { "low", "creeping", "red-edged", "tough" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = Lessons(("examine", "woodcraft")) };

    // ── Bushes and herbs ──────────────────────────────────────────────────────

    public static PointOfInterest BuildCactus() => new CactusPointOfInterest(
        displayName: "Prickly Pear Cactus",
        descriptions: new() { "A sprawl of flat spined pads, one grown out of another, red fruit along their rims" },
        items: new() { I(new PricklyPear()), I(new PricklyPear()), I(new Thorn()) },
        moods: new[] { "spined", "sprawling", "fleshy", "sun-hard" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "survivalism"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildMyrrhBush() => new BushPointOfInterest(
        displayName: "Myrrh Bush",
        descriptions: new() { "A knotted thorny shrub with papery bark, red tears of resin dried where the bark is split" },
        items: new() { I(new Myrrh()), I(new Thorn()), I(new Twig()) },
        moods: new[] { "knotted", "thorny", "fragrant", "dry" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "herblore"), ("smell", "perfumery"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildThornBush() => new BushPointOfInterest(
        displayName: "Thorn Bush",
        descriptions: new() { "A grey wait-a-bit thorn, every twig hooked backward to catch whatever brushes it" },
        items: new() { I(new Thorn()), I(new Thorn()), I(new Thornwood()) },
        moods: new[] { "hooked", "grey", "grasping", "dry" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = Lessons(("examine", "hedgecraft")) };

    public static PointOfInterest BuildAloe() => new HerbPointOfInterest(
        displayName: "Aloe Clump",
        descriptions: new() { "A rosette of thick toothed leaves, grey-green, a spike of orange flowers rising from the middle" },
        items: new() { I(new AloeLeaf()), I(new AloeLeaf()) },
        moods: new[] { "fleshy", "toothed", "grey-green", "medicinal" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "herblore"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildCrowberryMat() => new BushPointOfInterest(
        displayName: "Crowberry Mat",
        descriptions: new() { "A low mat of needle-leaved heath over the stones, studded with shining black berries" },
        items: new() { I(new Crowberry()), I(new Crowberry()), I(new Twig()) },
        moods: new[] { "low", "matted", "glossy", "cold" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "ripelore"), ("smell", "bouquet"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildCloudberryPatch() => new BushPointOfInterest(
        displayName: "Cloudberry Patch",
        descriptions: new() { "A patch of crinkled leaves in the wet moss, each plant holding up a single amber berry" },
        items: new() { I(new Cloudberry()), I(new Cloudberry()) },
        moods: new[] { "low", "wet", "amber", "scattered" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "ripelore"), ("smell", "bouquet"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildLabradorTea() => new HerbPointOfInterest(
        displayName: "Labrador Tea",
        descriptions: new() { "Knee-high shrubs with leathery leaves rolled under at the edge, rust-furred, smelling of resin when crushed" },
        items: new() { I(new LabradorTea()), I(new LabradorTea()) },
        moods: new[] { "leathery", "resinous", "low", "rust-backed" },
        isNatural: true
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = Lessons(("examine", "herblore"), ("smell", "apothecary_nose"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildReindeerMoss() => new LichenPointOfInterest(
        displayName: "Reindeer Moss",
        descriptions: new() { "A spread of grey-white lichen, branched like a forest seen from far above, crisp underfoot" },
        items: new() { I(new ReindeerMoss()), I(new ReindeerMoss()), I(new Lichen()) },
        moods: new[] { "grey-white", "crisp", "spreading", "branched" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "herblore"), ("contemplate", "aesthetic")) };

    // ── Ground ────────────────────────────────────────────────────────────────

    public static PointOfInterest BuildBleachedBones() => new BonesPointOfInterest(
        displayName: "Bleached Bones",
        descriptions: new() { "The scattered skeleton of some large beast, scoured white, the skull a little apart from the rest" },
        items: new() { I(new Bone()), I(new Bone()), I(new Skull()), I(new Horn()) },
        moods: new[] { "bleached", "scattered", "silent", "picked-clean" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "creature_lore"), ("contemplate", "vanitas")) };

    public static PointOfInterest BuildShedAntlers() => new BonesPointOfInterest(
        displayName: "Shed Antlers",
        descriptions: new() { "A pair of antlers dropped where a reindeer shed them, gnawed at the tips by something small" },
        items: new() { I(new Antler()), I(new Antler()) },
        moods: new[] { "branched", "gnawed", "pale", "dropped" }
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = Lessons(("examine", "spoor_reading")) };

    public static PointOfInterest BuildStele(string where) => new StelePointOfInterest(
        displayName: "Weathered Stele",
        descriptions: new() { $"An upright slab carved in a script nobody here can read, {where}" },
        items: new() { I(new Rock()) },
        moods: new[] { "weathered", "carved", "illegible", "forgotten" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "decipher"), ("contemplate", "ruin_sense")) };

    public static PointOfInterest BuildPetroglyphs() => new PetroglyphPointOfInterest(
        displayName: "Petroglyphs",
        descriptions: new() { "Figures pecked into the dark varnish of the rock: horned beasts, spirals, hands, a line of walking men" },
        items: new(),
        moods: new[] { "pecked", "ancient", "patient", "strange" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "archeology"), ("contemplate", "iconography")) };

    /// <summary>A stand of tussock grass; what it yields depends on the country, hence the two makers.</summary>
    public static PointOfInterest BuildTussockGrass(System.Func<Item> yield, System.Func<Item> second, string description) => new TussockPointOfInterest(
        displayName: "Tussock Grass",
        descriptions: new() { description },
        items: new() { I(yield()), I(yield()), I(second()) },
        moods: new[] { "tufted", "wind-combed", "dry", "whispering" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = Lessons(("examine", "forage_lore"), ("listen", "wind_reading"), ("contemplate", "aesthetic")) };

    public static PointOfInterest BuildSnowDrift() => new DriftPointOfInterest(
        displayName: "Snow Drift",
        descriptions: new() { "A drift of wind-packed snow, its crest curled over like a breaking wave and frozen there" },
        items: new() { I(new IceShard()) },
        moods: new[] { "curled", "wind-packed", "blue-shadowed", "smooth" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = Lessons(("examine", "weather_ear"), ("contemplate", "aesthetic"), ("listen", "wind_reading")) };

    public static PointOfInterest BuildErratic() => new BoulderPointOfInterest(
        displayName: "Glacial Erratic",
        descriptions: new() { "A boulder the size of a cottage, of a stone found nowhere near, set down where the ice let go of it" },
        items: new() { I(new Rock()), I(new Lichen()) },
        moods: new[] { "huge", "alien", "lichened", "alone" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "stonework"), ("contemplate", "awe")) };

    public static PointOfInterest BuildIceSculpture(string name, string description) => new IcePointOfInterest(
        displayName: name,
        descriptions: new() { description },
        items: new() { I(new IceShard()), I(new IceShard()) },
        moods: new[] { "glittering", "blue", "frozen", "still" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = Lessons(("examine", "thermodynamics"), ("contemplate", "aesthetic")) };
}
