using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The usual groups of area kinds a will declares as its <see cref="ModusMentis.WillPlaces"/> — the
/// places a player would expect it to find something to do in. Types, never names: an area kind that
/// is renamed or removed is a build error here, and <c>--will-audit</c> plans every will from each of
/// its places, so a group that names a kind no factory builds simply measures nothing and shows it.
///
/// <para>A group is what a place <i>is</i>, not what is in it today: herblore belongs in a meadow
/// whether or not this meadow grows anything, and the audit's related coverage is how far the content
/// lives up to the declaration.</para>
/// </summary>
public static class Places
{
    /// <summary>Several groups (or single kinds) as one declaration, without repeats.</summary>
    public static IReadOnlyList<Type> Of(params IEnumerable<Type>[] groups) => groups.SelectMany(g => g).Distinct().ToList();

    public static readonly Type[] Woodland =
    {
        typeof(ForestArea), typeof(ClearingArea), typeof(CoppiceArea), typeof(CrownArea), typeof(GroveArea),
        typeof(HollowayArea), typeof(ThicketArea), typeof(DeadwoodArea), typeof(RiverwoodArea),
        typeof(UnderstoreyArea), typeof(KrummholzArea), typeof(RavineArea),
    };

    public static readonly Type[] Grassland =
    {
        typeof(MeadowArea), typeof(GrasslandArea), typeof(HeathArea), typeof(HedgerowArea), typeof(WaterMeadowArea),
        typeof(CommonArea), typeof(SavannaArea), typeof(HillArea), typeof(ShielingArea), typeof(TundraArea),
        typeof(TussockArea),
    };

    /// <summary>Wet ground, and the dark damp under it: where things rot and grow out of the rot.</summary>
    public static readonly Type[] Damp =
    {
        typeof(BogArea), typeof(CarrArea), typeof(FenArea), typeof(SwampArea), typeof(MangroveArea), typeof(MuskegArea),
        typeof(SeepArea), typeof(WellspringArea), typeof(GrottoArea), typeof(DitchsideArea),
    };

    public static readonly Type[] MushroomCellar = { typeof(SpawnroomArea), typeof(CellarwayArea) };

    public static readonly Type[] Underground =
    {
        typeof(ChamberArea), typeof(TunnelArea), typeof(ShaftArea), typeof(ChasmArea), typeof(PitArea), typeof(SinkholeArea),
        typeof(ColonyArea), typeof(AlcoveArea), typeof(SeamArea), typeof(GrottoArea), typeof(SeaCaveArea),
    };

    /// <summary>Fresh water: what is fished, swum, waded.</summary>
    public static readonly Type[] FreshWater =
    {
        typeof(StreamArea), typeof(PondArea), typeof(PoolArea), typeof(RiverArea), typeof(LakeArea), typeof(TarnArea),
        typeof(FordArea), typeof(OxbowArea), typeof(WaterholeArea), typeof(RiverbankArea), typeof(WaterfallArea),
        typeof(CanalArea), typeof(WatersideArea),
    };

    public static readonly Type[] Shore =
    {
        typeof(BeachArea), typeof(ShoreArea), typeof(ShingleArea), typeof(MudflatArea), typeof(SaltmarshArea),
        typeof(EstuaryArea), typeof(IsletArea), typeof(RookeryArea), typeof(SeaCaveArea), typeof(SandbarArea),
        typeof(QuayArea),
    };

    /// <summary>Steep ground and high places: what is climbed and looked out from.</summary>
    public static readonly Type[] Heights =
    {
        typeof(CliffArea), typeof(CragArea), typeof(RidgeArea), typeof(SummitArea), typeof(LedgeArea), typeof(ScreeArea),
        typeof(ColArea), typeof(CouloirArea), typeof(EyrieArea), typeof(FalseSummitArea), typeof(CleftArea),
        typeof(InselbergArea), typeof(KopjeArea), typeof(MesaArea), typeof(RimArea), typeof(TablelandArea),
        typeof(NunatakArea), typeof(CorniceArea), typeof(GullyArea),
    };

    public static readonly Type[] Desert =
    {
        typeof(DuneArea), typeof(RegArea), typeof(WadiArea), typeof(OasisArea), typeof(YardangArea), typeof(BadlandsArea),
        typeof(WashArea), typeof(SlotArea),
    };

    public static readonly Type[] Ice =
    {
        typeof(IceArea), typeof(FloeArea), typeof(HummockArea), typeof(BergArea), typeof(PolynyaArea), typeof(CrevasseArea),
        typeof(IcefallArea), typeof(MoraineArea), typeof(SnowpackArea), typeof(DriftArea), typeof(NeveArea),
        typeof(IcefootArea), typeof(HauloutArea), typeof(FrostfieldArea), typeof(PingoArea), typeof(EskerArea),
    };

    /// <summary>Where wild beasts drink, lie up and are met.</summary>
    public static readonly Type[] Wilds =
    {
        typeof(DenArea), typeof(WallowArea), typeof(LickArea), typeof(WaterholeArea), typeof(ThornbrushArea),
        typeof(TermitaryArea), typeof(SavannaArea), typeof(ThicketArea), typeof(ClearingArea), typeof(RiverwoodArea),
    };

    /// <summary>The cultivated ground of a farm, field, orchard or plantation.</summary>
    public static readonly Type[] Fields =
    {
        typeof(StripArea), typeof(CropArea), typeof(OrchardArea), typeof(GroveArea), typeof(PlantationArea),
        typeof(VineyardArea), typeof(PaddyArea), typeof(TerraceArea), typeof(GardenArea), typeof(VegetableArea),
        typeof(HerbArea), typeof(SeedbedArea), typeof(HedgebankArea), typeof(WindbreakArea),
    };

    /// <summary>Where stock is kept.</summary>
    public static readonly Type[] Stock =
    {
        typeof(PenArea), typeof(PigstyArea), typeof(CoopArea), typeof(PaddockArea), typeof(ByreArea), typeof(FoldArea),
        typeof(CorralArea), typeof(WarrenArea), typeof(LambingArea), typeof(ShearingArea), typeof(DairyArea),
    };

    /// <summary>The open ways of a settlement, where its people pass.</summary>
    public static readonly Type[] Streets =
    {
        typeof(StreetArea), typeof(AlleyArea), typeof(SquareArea), typeof(LaneArea), typeof(GreenArea), typeof(RowArea),
        typeof(WalkArea), typeof(GatewayArea), typeof(MarketArea), typeof(QuayArea),
    };

    /// <summary>Where people drink and eat together.</summary>
    public static readonly Type[] Taverns =
    {
        typeof(AlehouseArea), typeof(TeahouseArea), typeof(RefectoryArea), typeof(HallArea),
    };

    /// <summary>Where a craft is worked and its goods sold.</summary>
    public static readonly Type[] Shops =
    {
        typeof(WorkshopArea), typeof(ForgeArea), typeof(BakeryArea), typeof(MillArea), typeof(MarketArea),
        typeof(FishmarketArea), typeof(CountinghouseArea), typeof(CobbleryArea), typeof(TailoryArea),
        typeof(ChandleryArea), typeof(ButcheryArea), typeof(TanneryArea), typeof(PotteryArea),
        typeof(DispensaryArea), typeof(BarbershopArea), typeof(RopewalkArea), typeof(ShipyardArea),
    };

    /// <summary>The inside of a dwelling: where people sleep, eat, and keep what they own.</summary>
    public static readonly Type[] Dwellings =
    {
        typeof(BedroomArea), typeof(DormitoryArea), typeof(KitchenArea), typeof(PantryArea), typeof(StoreArea),
        typeof(LoftArea), typeof(LandingArea), typeof(RoomArea), typeof(SolarArea), typeof(CellarArea),
    };

    /// <summary>A stronghold's martial parts.</summary>
    public static readonly Type[] Stronghold =
    {
        typeof(ArmouryArea), typeof(GuardroomArea), typeof(GatehouseArea), typeof(BattlementsArea), typeof(RampartArea),
        typeof(ParadeArea), typeof(OutworksArea), typeof(GatefrontArea), typeof(WallfootArea), typeof(DungeonArea),
    };

    /// <summary>Holy ground.</summary>
    public static readonly Type[] Sacred =
    {
        typeof(ChapelArea), typeof(SanctumArea), typeof(CloisterArea), typeof(CryptArea), typeof(CellArea),
        typeof(HengeArea), typeof(ScriptoriumArea),
    };

    /// <summary>Where the dead are laid, or were.</summary>
    public static readonly Type[] Graves =
    {
        typeof(CryptArea), typeof(BarrowArea), typeof(GroundArea), typeof(PitArea), typeof(GreenArea),
        typeof(LaneArea),
    };

    /// <summary>What is left of the past.</summary>
    public static readonly Type[] Ruins = { typeof(RuinArea), typeof(BarrowArea), typeof(ChamberArea), typeof(CryptArea) };

    /// <summary>Where wealth is kept.</summary>
    public static readonly Type[] Treasure = { typeof(TreasuryArea), typeof(CountinghouseArea), typeof(SolarArea), typeof(ChanceryArea) };

    /// <summary>Learning and its books.</summary>
    public static readonly Type[] Learning =
    {
        typeof(LibraryArea), typeof(ScriptoriumArea), typeof(ArchiveArea), typeof(ClassroomArea), typeof(LectureArea),
        typeof(ObservatoryArea), typeof(ChanceryArea),
    };

    /// <summary>Every inhabited place: what a mind that wants people wants.</summary>
    public static readonly Type[] Settled = Streets.Concat(Taverns).Concat(Shops).ToArray();

    /// <summary>Open country of every climate — the ground a walker or a hunter means.</summary>
    public static readonly Type[] Wild = Woodland.Concat(Grassland).Concat(Damp).Concat(Heights).Concat(Desert)
                                                 .Concat(Ice).Concat(Wilds).Concat(Shore).Distinct().ToArray();

    /// <summary>The roofs and tops of buildings, and lofts, reached from outside.</summary>
    public static readonly Type[] Roofs = { typeof(RoofArea), typeof(LoftArea), typeof(BattlementsArea) };
}
