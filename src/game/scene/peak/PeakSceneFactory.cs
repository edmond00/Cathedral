using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Peak;

/// <summary>
/// Builds a mountain top, laid out as a pyramid that ends in a point.
///
/// <para><b>Three bands.</b> The approach (tier 0) is the cirque and the ground below the shoulder —
/// three or four of basin, scree gully, old snowfield, tarn, col and moraine. The shoulder (tier 1) is
/// narrower — ridge, ledge, plateau, ice shelf, bivouac, false summit, cornice — and is reached from
/// the approach by one way, which on a glaciated peak may be a snow bridge over the bergschrund. The
/// summit (tier 2) is one area and nothing else, reached only by the final climb, and it sees
/// everything.</para>
///
/// <para><b>Three peaks.</b> A <i>horn</i> is a ridge and a point; a <i>dome</i> a high plateau; a
/// <i>glaciated</i> peak wears ice on its shoulder and snow in its cirque.</para>
///
/// <para><b>Ways that are not a walk.</b> The summit climb; a gendarme on the ridge to be scaled; a
/// cleft under a ledge to squeeze into; the snow bridge.</para>
/// </summary>
public class PeakSceneFactory : WildSceneFactory
{
    public PeakSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Horn, Dome, Glaciated }

    private Identity _identity;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Profile = LayoutProfile.Pyramid;
        _identity = (Identity)rng.Next(3);
        double W(double horn, double dome, double ice)
            => _identity switch { Identity.Horn => horn, Identity.Dome => dome, _ => ice };

        var approach = Draw(rng, new Option[]
        {
            new(BuildCirqueBasin, 2.0),
            new(BuildScreeGully,  W(2.0, 1.0, 0.8)),
            new(BuildSnowfield,   W(0.6, 0.8, 2.5)),
            new(BuildCirqueTarn,  W(1.0, 1.2, 0.8)),
            new(BuildCol,         1.0),
            new(BuildMoraine,     W(0.3, 0.3, 2.0)),
        }, Between(rng, 3, 4));

        var shoulder = Draw(rng, new Option[]
        {
            new(BuildRidge,          W(3.0, 0.5, 1.0)),
            new(BuildWindsweptLedge, 1.5),
            new(BuildSummitPlateau,  W(0.3, 3.0, 0.8)),
            new(BuildIceShelf,       W(0.3, 0.3, 2.5)),
            new(BuildBivouac,        1.0),
            new(BuildFalseSummit,    W(1.0, 1.5, 1.0)),
            new(BuildCornice,        W(0.8, 0.4, 1.5)),
        }, Math.Min(approach.Count - 1, Between(rng, 2, 3)));

        var summit = BuildSummit();

        // Hidden places, while the shoulder stays no wider than the approach under it.
        var shoulderWalk = shoulder.ToList();
        var ridge = shoulder.OfType<RidgeArea>().FirstOrDefault();
        Area? gendarme = ridge != null && rng.NextDouble() < 0.5 && shoulder.Count < approach.Count ? BuildGendarme() : null;
        if (gendarme != null) shoulder.Add(gendarme);
        var ledge = shoulder.OfType<LedgeArea>().FirstOrDefault();
        Area? cleft = ledge != null && rng.NextDouble() < 0.4 && shoulder.Count < approach.Count ? BuildCleft() : null;
        if (cleft != null) shoulder.Add(cleft);

        foreach (var area in approach.Concat(shoulder).Append(summit)) Populate(area, rng);

        AddSection(scene, _identity == Identity.Glaciated ? "Snow Cirque" : "Summit Approach",
            "The ground under the shoulder: a bowl of old snow and fallen stone, walled on three sides",
            seed => new WaveGenerator { Seed = seed }, approach, tier: 0);
        AddSection(scene, _identity == Identity.Dome ? "High Plateau" : "The Shoulder",
            "Exposed rock and ice high on the peak, the wind never stopping",
            seed => new WaveGenerator { Seed = seed }, shoulder, tier: 1);
        AddSection(scene, "Exposed Summit", "The very top, a few strides of rock with the sky on every side",
            seed => new WaveGenerator { Seed = seed }, new[] { summit }, tier: 2);

        string[] moods = { "narrow", "exposed", "wind-bitten" };
        static string Way(Area a, Area b) =>
            a is GullyArea || b is GullyArea ? "Gully"
            : a is RidgeArea || b is RidgeArea ? "Ridge Path"
            : "Summit Path";

        WeaveSection(rng, scene, approach, RollWeave(rng), Way, "under the peak", moods);
        WeaveSection(rng, scene, shoulderWalk, RollWeave(rng, Weave.Line, Weave.Branching, Weave.Hub), Way,
            "along the shoulder", moods);

        // One way up onto the shoulder: a path, or on a glaciated peak a snow bridge over the bergschrund.
        var low  = approach[rng.Next(approach.Count)];
        var high = shoulderWalk[rng.Next(shoulderWalk.Count)];
        if (_identity == Identity.Glaciated && rng.NextDouble() < 0.7)
            Cross(scene, low, high, CrossingKind.SnowBridge, "Bergschrund",
                "The great crack where the ice pulls away from the rock, bridged by a sag of old snow, blue darkness under it",
                new[] { "blue", "sagging", "bottomless" }, "survivalism");
        else
            OutdoorLayout.Link(scene, low, high, "Shoulder Path", moods, "up onto the shoulder");

        // The summit: one climb, from wherever on the shoulder lies under it.
        var foot = shoulderWalk.FirstOrDefault(a => a is RidgeArea or FalseSummitArea) ?? shoulderWalk[rng.Next(shoulderWalk.Count)];
        AddViewpoint(scene, foot, summit, "Summit Pitch",
            "The last pitch: a steep step of frost-shattered rock going up to nothing but sky",
            new[] { "frost-shattered", "steep", "final" }, icy: _identity == Identity.Glaciated, lesson: "summit_fever");

        if (gendarme != null)
            Scale(scene, ridge!, gendarme, ScaleKind.Stack, "Gendarme",
                "A tower of rock standing on the ridge like a sentry, its sides cracked into holds",
                new[] { "towering", "cracked", "lonely" });
        if (cleft != null)
            SlipInto(scene, ledge!, cleft, SlipKind.Crevice, "Frost Crack",
                "A crack under the ledge wide enough for a body, breathing out air colder than the wind",
                new[] { "narrow", "icy", "dark" }, lesson: "potholing");

        Furnish(rng, scene, FurnitureSubfactory.Setting.Highland, summit, gendarme, cleft);

        Console.WriteLine($"PeakSceneFactory: {_identity}, {approach.Count}/{shoulder.Count}/1 areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildCirqueBasin() => new BasinArea(
        displayName: "Cirque Basin",
        contextDescription: "in the cirque basin",
        transitionDescription: "drop into the cirque basin",
        descriptions: new() { "A bowl of still air and old snow scooped out beneath the peak, walled on three sides" },
        moods: new[] { "still", "sheltered", "cold", "walled" });

    private static Area BuildScreeGully() => new GullyArea(
        displayName: "Scree Gully",
        contextDescription: "in the scree gully",
        transitionDescription: "start up the scree gully",
        descriptions: new() { "A steep loose-rock channel cutting down the side of the peak" },
        moods: new[] { "loose", "treacherous", "narrow", "steep" });

    private static Area BuildSnowfield() => new SnowpackArea(
        displayName: "Old Snowfield",
        contextDescription: "on the old snowfield",
        transitionDescription: "kick steps up onto the old snowfield",
        descriptions: new() { "A slope of grey summer-old snow, hard as rock in the morning and soft as porridge by noon" },
        moods: new[] { "grey", "glaring", "crusted", "silent" });

    private static Area BuildCirqueTarn() => new TarnArea(
        displayName: "Cirque Tarn",
        contextDescription: "by the cirque tarn",
        transitionDescription: "come down to the tarn",
        descriptions: new() { "A round lake of green ice-water in the floor of the cirque, a skin of ice on it till midsummer" },
        moods: new[] { "green", "icy", "round", "still" });

    private static Area BuildCol() => new ColArea(
        displayName: "High Col",
        contextDescription: "on the high col",
        transitionDescription: "climb onto the high col",
        descriptions: new() { "A notch in the ridge between this peak and the next, a gate the weather comes through" },
        moods: new[] { "notched", "windy", "bare", "cold" });

    private static Area BuildMoraine() => new MoraineArea(
        displayName: "Moraine",
        contextDescription: "on the moraine",
        transitionDescription: "climb onto the moraine",
        descriptions: new() { "A raw embankment of rubble and grey silt left where the ice once reached" },
        moods: new[] { "raw", "rubbled", "grey", "unstable" });

    private static Area BuildRidge() => new RidgeArea(
        displayName: "Ridge",
        contextDescription: "on the narrow ridge",
        transitionDescription: "step onto the ridge",
        descriptions: new() { "A narrow exposed spine of rock with steep falls on either side" },
        moods: new[] { "narrow", "vertiginous", "wind-howling", "thin-aired" });

    private static Area BuildSummitPlateau() => new PlateauArea(
        displayName: "Summit Plateau",
        contextDescription: "on the summit plateau",
        transitionDescription: "step onto the summit plateau",
        descriptions: new() { "A flat high plain of stones and moss below the top, exposed to the sky on every side" },
        moods: new[] { "flat", "exposed", "vast", "wind-scoured" });

    private static Area BuildWindsweptLedge() => new LedgeArea(
        displayName: "Windswept Ledge",
        contextDescription: "on the windswept ledge",
        transitionDescription: "step onto the windswept ledge",
        descriptions: new() { "A jutting shelf of rock overlooking the world below" },
        moods: new[] { "exposed", "vast", "vertiginous", "wind-bitten" });

    private static Area BuildIceShelf() => new IceArea(
        displayName: "Ice Shelf",
        contextDescription: "on the ice shelf",
        transitionDescription: "step onto the ice shelf",
        descriptions: new() { "A frozen shelf of ice glittering against rock and sky" },
        moods: new[] { "frozen", "glittering", "still", "thin-aired" });

    private static Area BuildBivouac() => new BivouacArea(
        displayName: "Bivouac",
        contextDescription: "in the bivouac",
        transitionDescription: "duck into the bivouac",
        descriptions: new() { "A nook under an overhang with a low wall of stones built across it against the wind, big enough for two to sit out a night" },
        moods: new[] { "cramped", "sheltered", "frost-rimed", "welcome" });

    private static Area BuildFalseSummit() => new FalseSummitArea(
        displayName: "False Summit",
        contextDescription: "on the false summit",
        transitionDescription: "crest the rise you took for the top",
        descriptions: new() { "A top that from below was the summit, and is not: the real one stands beyond, higher, over a dip of rock" },
        moods: new[] { "deceiving", "windy", "breathless", "bitter" });

    private static Area BuildCornice() => new CorniceArea(
        displayName: "Cornice",
        contextDescription: "near the cornice",
        transitionDescription: "edge toward the cornice",
        descriptions: new() { "A lip of wind-packed snow curling out over the drop, with nothing under its edge but air" },
        moods: new[] { "curling", "overhanging", "treacherous", "white" });

    private static Area BuildSummit() => new SummitArea(
        displayName: "The Summit",
        contextDescription: "on the summit",
        transitionDescription: "pull yourself onto the summit",
        descriptions: new() { "The top: a few strides of shattered rock, and on every side the world falling away to the edge of sight" },
        moods: new[] { "highest", "shattered", "boundless", "thin-aired" });

    private static Area BuildGendarme() => new CragArea(
        displayName: "Gendarme",
        contextDescription: "on top of the gendarme",
        transitionDescription: "haul yourself onto the gendarme",
        descriptions: new() { "The flat crown of a rock tower standing out of the ridge, a table of stone in the sky" },
        moods: new[] { "perched", "airy", "lonely", "flat-topped" });

    private static Area BuildCleft() => new CleftArea(
        displayName: "Frost Cleft",
        contextDescription: "inside the frost cleft",
        transitionDescription: "squeeze into the frost cleft",
        descriptions: new() { "A cave-like slot under the ledge, its walls furred with hoarfrost, quiet after the wind" },
        moods: new[] { "frost-furred", "quiet", "dim", "cold" });

    // ── Contents: sparse, as a peak is ─────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        switch (area)
        {
            case BasinArea:
                p.Add(TerrainSubfactory.BuildFallenRocks());
                if (rng.NextDouble() < 0.5) p.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case GullyArea:
                p.Add(TerrainSubfactory.BuildFallenRocks());
                break;
            case SnowpackArea:
                p.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case TarnArea:
                p.Add(BuildTarnIce());
                break;
            case ColArea:
            case MoraineArea:
                p.Add(TerrainSubfactory.BuildCairn());
                p.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case RidgeArea:
            case PlateauArea:
            case LedgeArea:
                if (rng.NextDouble() < 0.6) p.Add(TerrainSubfactory.BuildCairn());
                p.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.4) p.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case IceArea:
                p.Add(TerrainSubfactory.BuildIceFormation());
                break;
            case BivouacArea:
                p.Add(BuildBivouacWall());
                break;
            case FalseSummitArea:
                p.Add(TerrainSubfactory.BuildRockOutcrop());
                break;
            case CorniceArea:
                p.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case SummitArea:
                p.Add(BuildSummitCairn());
                break;
            case CragArea:
                p.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case CleftArea:
                p.Add(TerrainSubfactory.BuildIceFormation());
                break;
        }
    }

    private static PointOfInterest BuildSummitCairn() => new CairnPointOfInterest(
        displayName: "Summit Cairn",
        descriptions: new() { "A heap of stones on the very top, each carried up by someone who stood here, a few with marks scratched into them" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "heaped", "marked", "highest", "patient" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "summit_fever", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildBivouacWall() => new HollowPointOfInterest(
        displayName: "Bivouac Wall",
        descriptions: new() { "A wall of stones piled chest-high across the overhang, a few charred sticks and a rag of old cloth behind it" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Twig()) },
        moods: new[] { "piled", "charred", "makeshift", "sheltering" },
        isNatural: false
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "survivalism", ["contemplate"] = "hearthlonging" } };

    private static PointOfInterest BuildTarnIce() => new PoolPointOfInterest(
        displayName: "Tarn Ice",
        descriptions: new() { "The tarn's edge, a rind of ice over green water so clear the stones on the bottom look close enough to touch" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new Rock()) },
        moods: new[] { "rinded", "green", "clear", "bitter" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "plunge", ["contemplate"] = "meditation" } };

    // ── Life ─────────────────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new WolfArchetype(), 0.10);

        TrySpawnCreature(rng, scene, new EagleArchetype(), 0.85);
        TrySpawnCreature(rng, scene, new RavenArchetype(), 0.50);
        TrySpawnCreature(rng, scene, new SnowHareArchetype(), _identity == Identity.Glaciated ? 0.45 : 0.20);
        TrySpawnCreature(rng, scene, new MountainGoatArchetype(), _identity == Identity.Horn ? 0.55 : 0.30);
        TrySpawnCreature(rng, scene, new ChoughArchetype(), 0.45);
        TrySpawnCreature(rng, scene, new SnowFinchArchetype(), 0.50);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Barren, 1, 2);
    }
}
