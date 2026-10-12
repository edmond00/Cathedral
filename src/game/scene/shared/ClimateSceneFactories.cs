using System;
using System.Collections.Generic;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// The scene factory for each climate biome, keyed by the biome's name — the one list the launcher
/// registers from and every audit sweeps (<c>--verb-audit</c>, <c>--building-audit</c>,
/// <c>--outcome-audit</c>, the lesson sweep and the verb probe).
///
/// <para>The temperate factories are named by hand in each of those six places, and a seventh
/// would have to be. One list means a climate biome cannot be playable and unaudited, or audited and
/// unplayable.</para>
/// </summary>
public static class ClimateSceneFactories
{
    public sealed record Entry(string Biome, string AuditLabel, Func<SceneFactory> Create);

    public static readonly IReadOnlyList<Entry> All = new Entry[]
    {
        new(BiomeDatabase.Desert,     "DESERT",      () => new Desert.DesertSceneFactory()),
        new(BiomeDatabase.HotSteppe,  "HOT STEPPE",  () => new HotSteppe.HotSteppeSceneFactory()),
        new(BiomeDatabase.Jungle,     "JUNGLE",      () => new Jungle.JungleSceneFactory()),
        new(BiomeDatabase.Canyon,     "CANYON",      () => new Canyon.CanyonSceneFactory()),
        new(BiomeDatabase.SeaIce,     "SEA ICE",     () => new SeaIce.SeaIceSceneFactory()),
        new(BiomeDatabase.Glacier,    "GLACIER",     () => new Glacier.GlacierSceneFactory()),
        new(BiomeDatabase.Snowfield,  "SNOWFIELD",   () => new Snowfield.SnowfieldSceneFactory()),
        new(BiomeDatabase.ColdSteppe, "COLD STEPPE", () => new ColdSteppe.ColdSteppeSceneFactory()),
    };
}
