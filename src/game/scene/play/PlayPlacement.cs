using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// Lays the plays over a finished scene: a staircase's rail to slide down, flat stones by a pond, a
/// rope swing in a grove, knucklebones on a guardroom board — so that about a quarter of the areas a
/// player walks into has something to play at (<c>--play-audit</c> measures it).
///
/// <para>Three routes put a play in the world, and this is the third. A factory sets
/// <see cref="PointOfInterest.Play"/> on one of its own objects when the play belongs to that object
/// (the jungle's hanging lianas, a loft's heap of hay, a field's scarecrow); a creature's archetype
/// declares one (a stray cat, hens); and this pass does the rest, by kind of area, after everything else
/// is built. It does two things:</para>
///
/// <list type="bullet">
/// <item>gives an existing connector a play: a staircase's rail, the flat stones of a creek or a pond;</item>
/// <item>adds a new plaything (<c>PlayPoiKinds.cs</c>) to an area of a kind it suits, at that kind's
///   chance, unless the area offers a play already.</item>
/// </list>
///
/// <para>Drawn from a generator of its own seeded with the location id, as wild growth is, so nothing
/// built before or after it moves. Every area takes the same two draws whether or not it is a candidate,
/// so one area's play never shifts another's. Off where paths are (<see cref="SceneFactory"/>'s
/// <c>LaysPaths</c>): the test location must not change under its scripts, and builds its plaything by
/// hand.</para>
/// </summary>
public static class PlayPlacement
{
    /// <summary>One kind of area's chance of a plaything, and the playthings it may be.</summary>
    private sealed record Rule(Type[] Areas, double Chance, Func<Area, int, PointOfInterest>[] Makers);

    private static Rule R(double chance, Type[] areas, params Func<Area, int, PointOfInterest>[] makers)
        => new(areas, chance, makers);

    private static Type[] T(params Type[] types) => types;

    // ── The playthings ────────────────────────────────────────────────────────

    private static PointOfInterest Pebbles(Area a, int r)    => new PebblesPointOfInterest("Flat Pebbles");
    private static PointOfInterest Swing(Area a, int r)      => new SwingPointOfInterest("Rope Swing");
    private static PointOfInterest Liana(Area a, int r)      => new LianaPointOfInterest("Hanging Liana",
        new() { "A liana hanging free from the canopy, thick as a wrist and long enough to swing on" },
        moods: new[] { "hanging", "swaying" }) { Play = new SwingPlay(), Senses = SensoryProfile.Examinable };
    private static PointOfInterest IceSlide(Area a, int r)   => new SlidePointOfInterest("Ice Slide");
    private static PointOfInterest Snowbank(Area a, int r)   => new SnowbankPointOfInterest("Snowbank");
    private static PointOfInterest Snow(Area a, int r)       => new SnowPointOfInterest("Fresh Snow");
    private static PointOfInterest Slipface(Area a, int r)   => new SlipfacePointOfInterest("Dune Slipface");
    private static PointOfInterest Sand(Area a, int r)       => new SandPointOfInterest("Wet Sand");
    private static PointOfInterest Puddle(Area a, int r)     => new PuddlePointOfInterest("Mud Puddle");
    private static PointOfInterest Leaves(Area a, int r)     => new LeavesPointOfInterest("Leaf Drift");
    private static PointOfInterest Knuckles(Area a, int r)   => new KnucklebonesPointOfInterest("Knucklebones");
    private static PointOfInterest Hopscotch(Area a, int r)  => new HopscotchPointOfInterest("Chalked Hopscotch");
    private static PointOfInterest Bank(Area a, int r)       => new BankPointOfInterest("Grassy Bank");
    private static PointOfInterest Leap(Area a, int r)       => new LeapPointOfInterest("Jumping Rock");
    private static PointOfInterest Stones(Area a, int r)     => new StonesPointOfInterest("Stepping Stones");
    private static PointOfInterest Grass(Area a, int r)      => new GrassPointOfInterest("Long Grass");
    private static PointOfInterest Shallows(Area a, int r)   => new ShallowsPointOfInterest("Shallows");
    private static PointOfInterest Mark(Area a, int r)       => new MarkPointOfInterest("Throwing Mark");

    /// <summary>Hay or straw heaped loose — the one plaything that is an existing kind (<see cref="HayPointOfInterest"/>).</summary>
    private static PointOfInterest Hay(Area a, int r) => new HayPointOfInterest(
        r % 2 == 0 ? "Loose Hay" : "Straw Heap", new() { "Hay and straw heaped high and loose, soft enough to land in" },
        moods: new[] { "loose", "dusty", "sweet" }) { Play = new TumblePlay("jump down into {0} and roll about in it"), Senses = SensoryProfile.Odorous };

    /// <summary>The echo of open rock, or of a vault under a building.</summary>
    private static PointOfInterest Echo(Area a, int r) => a is CryptArea or CellarArea or RuinArea or DungeonArea or ChapelArea
        ? new EchoPointOfInterest("Echoing Vault", new() { "A low stone vault that gives back every footstep twice" })
        : new EchoPointOfInterest("Echoing Rock");

    /// <summary>A child's plaything left lying — which one, from the area's pick.</summary>
    private static PointOfInterest Toy(Area a, int r) => (r % 3) switch
    {
        0 => new ToyPointOfInterest("Spinning Top", new() { "A painted wooden top and the little whip that drives it, left on the floor" }),
        1 => new ToyPointOfInterest("Cup and Ball", new() { "A wooden cup on a handle, a ball tied to it by a length of string" }),
        _ => new ToyPointOfInterest("Bowling Hoop", new() { "A wooden hoop and the stick that drives it, propped against the wall" }),
    };

    // ── Where, and how often ──────────────────────────────────────────────────
    // First matching rule wins, so an area kind belongs to one rule. Chances were tuned against
    // --play-audit; read its by-kind table before changing one.

    private static readonly Rule[] Rules =
    {
        // Games of the idle and the off duty.
        R(0.60, T(typeof(AlehouseArea), typeof(TeahouseArea)), Knuckles),
        R(0.40, T(typeof(GuardroomArea), typeof(GatehouseArea), typeof(DormitoryArea)), Knuckles),
        R(0.15, T(typeof(RefectoryArea), typeof(DungeonArea), typeof(ParadeArea), typeof(CellArea)), Knuckles),

        // A household's toys.
        R(0.28, T(typeof(BedroomArea), typeof(SolarArea), typeof(ClassroomArea), typeof(HallArea)), Toy),
        R(0.10, T(typeof(KitchenArea), typeof(LandingArea), typeof(WorkshopArea), typeof(StoreArea), typeof(PantryArea)), Toy),

        // Streets: the chalked squares.
        R(0.45, T(typeof(SquareArea), typeof(StreetArea), typeof(RowArea), typeof(WalkArea), typeof(CourtyardArea)), Hopscotch),
        R(0.20, T(typeof(AlleyArea), typeof(CloisterArea), typeof(GatewayArea)), Hopscotch),
        R(0.50, T(typeof(GreenArea), typeof(CommonArea)), Bank, Swing, Hopscotch),

        // Hay and straw.
        R(0.40, T(typeof(BarnArea), typeof(LoftArea), typeof(ThreshingArea), typeof(FodderArea), typeof(ByreArea)), Hay),
        R(0.12, T(typeof(ShedArea), typeof(TackroomArea), typeof(DryingArea), typeof(PackingArea), typeof(PressArea)), Hay),

        // Mud.
        R(0.50, T(typeof(PigstyArea), typeof(WallowArea), typeof(DitchArea), typeof(DitchsideArea)), Puddle),
        R(0.25, T(typeof(YardArea), typeof(LaneArea), typeof(PenArea), typeof(PaddockArea), typeof(CorralArea),
                  typeof(FenArea), typeof(BogArea), typeof(MuskegArea), typeof(CarrArea), typeof(SwampArea),
                  typeof(PaddyArea), typeof(TroughArea), typeof(FoldArea), typeof(LambingArea)), Puddle),

        // Woods and trees: leaves to kick, a rope on a branch.
        R(0.17, T(typeof(ForestArea), typeof(GroveArea), typeof(OrchardArea), typeof(WindbreakArea), typeof(CoppiceArea),
                  typeof(HollowayArea), typeof(DeadwoodArea), typeof(RiverwoodArea), typeof(ThicketArea),
                  typeof(GardenArea), typeof(VineyardArea), typeof(KrummholzArea)), Leaves, Leaves, Swing),

        // The jungle's hanging vines.
        R(0.32, T(typeof(CanopyArea), typeof(UnderstoreyArea), typeof(PlantationArea), typeof(MangroveArea)), Liana),

        // Grass: banks to roll down, blades to whistle on.
        R(0.17, T(typeof(HedgebankArea), typeof(HillArea), typeof(SlopeArea), typeof(ValleyArea), typeof(TerraceArea),
                  typeof(ClearingArea), typeof(ShielingArea), typeof(HedgerowArea), typeof(VegetableArea), typeof(HerbArea), typeof(SeedbedArea)), Bank),
        R(0.24, T(typeof(MeadowArea), typeof(GrasslandArea), typeof(HeathArea), typeof(WaterMeadowArea), typeof(SavannaArea),
                  typeof(TussockArea), typeof(TundraArea)), Grass, Bank),

        // Still water and its edges: stones to skip, a rope over the water, shallows.
        R(0.32, T(typeof(PondArea), typeof(LakeArea), typeof(RiverArea), typeof(OxbowArea), typeof(CanalArea),
                  typeof(WatersideArea), typeof(ShingleArea)), Pebbles, Pebbles, Shallows),
        R(0.32, T(typeof(RiverbankArea), typeof(ShoreArea)), Pebbles, Swing, Shallows),
        R(0.39, T(typeof(BeachArea), typeof(SandbarArea), typeof(EstuaryArea), typeof(IsletArea)), Sand, Sand, Shallows),

        // Running water: stones to hop.
        R(0.32, T(typeof(StreamArea), typeof(FordArea), typeof(BurnArea), typeof(SeepArea), typeof(WashArea), typeof(WadiArea)), Stones, Shallows),

        // Deep pools under rock: a jump.
        R(0.32, T(typeof(PoolArea), typeof(WaterfallArea), typeof(TarnArea), typeof(CenoteArea), typeof(WellspringArea),
                  typeof(OasisArea), typeof(WaterholeArea)), Leap, Leap, Shallows),

        // Ice and snow.
        R(0.32, T(typeof(IceArea), typeof(FloeArea), typeof(FrostfieldArea), typeof(IcefootArea), typeof(PingoArea),
                  typeof(HummockArea), typeof(ThawArea), typeof(LeadArea), typeof(BrashArea)), IceSlide),
        R(0.28, T(typeof(SnowpackArea), typeof(DriftArea), typeof(NeveArea), typeof(RunoutArea), typeof(BarrensArea),
                  typeof(EskerArea), typeof(MoraineArea), typeof(OutwashArea)), Snowbank, Snow),

        // Sand.
        R(0.35, T(typeof(DuneArea)), Slipface),
        R(0.15, T(typeof(RegArea), typeof(PanArea)), Slipface),

        // Rock that answers a shout.
        R(0.32, T(typeof(GorgeArea), typeof(ChasmArea), typeof(CleftArea), typeof(SlotArea), typeof(RavineArea),
                  typeof(SeaCaveArea), typeof(AmphitheatreArea), typeof(SinkholeArea), typeof(OverhangArea)), Echo),
        R(0.10, T(typeof(ChamberArea), typeof(ShaftArea), typeof(GrottoArea), typeof(TunnelArea), typeof(PitArea),
                  typeof(AlcoveArea), typeof(EntranceArea), typeof(FlowstoneArea), typeof(ColonyArea), typeof(BasinArea),
                  typeof(CliffArea), typeof(CragArea), typeof(BoulderArea), typeof(ScreeArea), typeof(RimArea),
                  typeof(CryptArea), typeof(CellarArea), typeof(RuinArea), typeof(BarrowArea), typeof(ChapelArea)), Echo),

        // Walls to pelt from.
        R(0.12, T(typeof(BattlementsArea), typeof(RampartArea), typeof(OutworksArea), typeof(WallfootArea),
                  typeof(GatefrontArea), typeof(RoofArea)), Mark),
    };

    // ── The pass ──────────────────────────────────────────────────────────────

    /// <summary>The generator plays are drawn from: its own stream, so nothing built before or after it moves.</summary>
    private static Random PlayRandom(int locationId) => new(unchecked(locationId * 0x61C88647 + 0x3B9A));

    /// <summary>A building's staircase is slid down by its rail this often.</summary>
    private const double StairChance = 0.25;

    /// <summary>Flat stones lie along a creek, a pond or a river this often.</summary>
    private const double SkimmingWaterChance = 0.5;

    public static void Place(Scene scene, int locationId)
    {
        var rng = PlayRandom(locationId);

        // Connectors first, each taking one draw. Distinct, since a connector stands in both its areas.
        foreach (var connector in scene.AllAreas.SelectMany(a => a.PointsOfInterest).OfType<ConnectorPointOfInterest>().Distinct().ToList())
        {
            double draw = rng.NextDouble();
            if (connector.Play != null) continue;
            switch (connector)
            {
                case StairPointOfInterest when draw < StairChance:
                    connector.Play = new SlideDownPlay("slide down {0} on my seat, bumping from step to step to the bottom", needsHands: true);
                    break;
                case WaterCrossingPointOfInterest { Kind: WaterKind.Creek or WaterKind.Pond or WaterKind.River }
                    when draw < SkimmingWaterChance:
                    connector.Play = new SkippingStonesPlay();
                    break;
            }
        }

        foreach (var area in scene.AllAreas.ToList())
        {
            double draw = rng.NextDouble();
            int    pick = rng.Next(1 << 16);

            var rule = Rules.FirstOrDefault(r => r.Areas.Any(t => t.IsInstanceOfType(area)));
            if (rule == null || draw >= rule.Chance) continue;
            if (area.PointsOfInterest.Any(p => p.Play != null)) continue;

            // The maker is handed what is left of the same pick, never the stream: a maker that drew
            // would shift every area after this one.
            var poi = rule.Makers[pick % rule.Makers.Length](area, pick / rule.Makers.Length);
            area.PointsOfInterest.Add(poi);
            poi.Register(scene);
        }
    }

    /// <summary>One of every kind of play the game has, for the audit's lesson check.</summary>
    public static IEnumerable<PlayActivity> Catalogue()
    {
        var area = new MeadowArea("Audit Meadow", "in the meadow", "walk to the meadow", new() { "A meadow" }, Array.Empty<string>());
        foreach (var make in Rules.SelectMany(r => r.Makers).Distinct())
            if (make(area, 0).Play is { } play) yield return play;

        yield return new SkippingStonesPlay();
        yield return new SlideDownPlay("slide down {0}", needsHands: true);
        yield return new PettingPlay("stroke {0}", 2, claws: false);
        yield return new ChasePlay("chase {0}");
        yield return new PoolJumpPlay();
        yield return new BalanceWalkPlay();
        yield return new ThrowAtMarkPlay("throw at {0}");
        yield return new GrassWhistlePlay();
        yield return new PaddlePlay();
        yield return new MudPuddlePlay();
        yield return new IceSlidePlay();
    }
}
