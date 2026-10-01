using System.Diagnostics;
using Cathedral.Game.History.Engine;
using Cathedral.Game.History.Generation;
using Cathedral.Game.History.Worlds;

namespace Cathedral.Game.History;

/// <summary>
/// Builds a world's history from the master seed and the world's regions.
///
/// <para>Called by <c>MicroworldInterface.GenerateWorld</c> once the regions exist, and by
/// <c>--history-audit</c> on worlds it builds headless. Both reseed <see cref="GameRng"/> to the world's
/// seed first, so the history is a pure function of the seed, exactly as the terrain is: two draws
/// from two named streams (<c>history-language</c> for the names, <c>history</c> for everything else),
/// so a change to how events are drawn never renames a world's places.</para>
/// </summary>
public static class WorldHistoryGenerator
{
    /// <summary>How many moons the sky holds, for finding a generic world's ordinal from its seed.</summary>
    private const int SkyMoonCount = 383;

    /// <param name="worldSeed">
    /// The world's seed; the run's master seed when omitted. Passing it explicitly is what lets a
    /// world that is not the run's (a moon previewed in the viewer, an audit sample) be generated on
    /// any thread without reseeding <see cref="GameRng"/>.
    /// </param>
    public static WorldHistory Generate(HistoryGeography geography, int? worldSeed = null)
    {
        var sw = Stopwatch.StartNew();
        int seed = worldSeed ?? GameRng.MasterSeed;
        var profile = WorldProfiles.ForSeed(seed);

        int ordinal = LoreMoons.OrdinalForSeed(seed);
        if (ordinal < 0) ordinal = Cathedral.Glyph.SkyMoons.OrdinalForSeed(seed, SkyMoonCount);

        var language = new WorldLanguage(GameRng.ForWorld(seed, "history-language"));
        var world = profile.CreateWorld(ordinal, language);
        var history = new WorldHistory(world, profile, geography, language);
        for (int r = 0; r < geography.Regions.Count; r++) history.RegionNames[r] = language.PlaceName();

        var sim = new HistorySimulation(history, GameRng.ForWorld(seed, "history"));
        profile.SowAll(sim);
        sim.Run();

        history.Hash = WorldHistory.ComputeHash(history.Chronology, history);
        history.GenerationMilliseconds = sw.ElapsedMilliseconds;
        return history;
    }
}
