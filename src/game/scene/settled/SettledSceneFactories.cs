using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Settled;

/// <summary>
/// Every location of the settled country and the factory that builds it — the one list the launcher
/// registers from and the audits sweep, so a location the sprawl can put on the map is a location
/// every audit has built. The counterpart of <see cref="Shared.ClimateSceneFactories"/>.
///
/// <para><see cref="All"/> is one entry per location key. <see cref="AuditVariants"/> adds the same
/// factories forced onto other ground they can stand on — a city of the mountains, a village of the
/// cold steppe — because the climate changes what they are built of, and a material is exactly the
/// kind of thing that breaks one climate and not another.</para>
/// </summary>
public static class SettledSceneFactories
{
    public sealed record Entry(string Key, string AuditLabel, Func<SceneFactory> Create);

    public static readonly IReadOnlyList<Entry> All = Build().ToList();

    private static IEnumerable<Entry> Build()
    {
        foreach (var key in SettlementTable.AllAgriculture)
            yield return new(key, key.ToUpperInvariant(), () => new Agriculture.AgricultureSceneFactory(key));
        foreach (var key in SettlementTable.AllLivestock)
            yield return new(key, key.ToUpperInvariant(), key == "farm"
                ? () => new Farm.FarmSceneFactory()
                : () => new Livestock.LivestockSceneFactory(key));
        foreach (var key in SettlementTable.AllSettlements)
            yield return new(key, key.ToUpperInvariant(), () => new Village.VillageSceneFactory(key));
        yield return new(SettlementTable.City, "CITY", () => new CitySceneFactory());
        foreach (var key in BiomeDatabase.HistoricLocations)
            yield return new(key, key.ToUpperInvariant(), () => new HistoricSceneFactory(HistoricSceneFactory.KindOf(key)));
        yield return new(SettlementTable.Ruin, "RUIN", () => new RuinSceneFactory());
    }

    /// <summary>The same factories on other ground: one per city biome, and the settlements' other columns.</summary>
    public static readonly IReadOnlyList<Entry> AuditVariants = new Entry[]
    {
        new(SettlementTable.City, "CITY (MOUNTAIN)",    () => new CitySceneFactory("mountain")),
        new(SettlementTable.City, "CITY (HOT STEPPE)",  () => new CitySceneFactory(BiomeDatabase.HotSteppe)),
        new(SettlementTable.City, "CITY (COLD STEPPE)", () => new CitySceneFactory(BiomeDatabase.ColdSteppe)),
        new("village", "VILLAGE (COLD STEPPE)",         () => new Village.VillageSceneFactory("village", BiomeDatabase.ColdSteppe)),
        new("burg",    "BURG (SNOWFIELD)",              () => new Village.VillageSceneFactory("burg", BiomeDatabase.Snowfield)),
        new("citadel", "CITADEL (HOT STEPPE)",          () => new HistoricSceneFactory(History.PlaceKind.Citadel, BiomeDatabase.HotSteppe)),
        new(SettlementTable.Ruin, "RUIN (JUNGLE)",      () => new RuinSceneFactory(BiomeDatabase.Jungle)),
    };

    /// <summary>Every entry an audit should build: one per location, then the variants.</summary>
    public static IEnumerable<Entry> ForAudit => All.Concat(AuditVariants);
}
