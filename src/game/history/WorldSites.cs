namespace Cathedral.Game.History;

/// <summary>
/// The settled map of the world being played, for the location factories to read.
///
/// <para>A scene factory receives only its location id, which is the vertex it stands on — so the
/// question "which historical place is this, and whose" has to be answerable from the vertex. The
/// world publishes its <see cref="SettlementMap"/> and history here once both are built, and a
/// factory looks itself up. Audits and tests build factories with no world behind them: then
/// <see cref="At"/> answers null and a factory falls back to a generic place of its kind.</para>
/// </summary>
public static class WorldSites
{
    public static SettlementMap Map { get; private set; } = SettlementMap.Empty;
    public static WorldHistory? History { get; private set; }
    private static Func<int, string?>? _biomeAt;

    public static void Publish(SettlementMap map, WorldHistory? history, Func<int, string?>? biomeAt = null)
    {
        Map = map;
        History = history;
        _biomeAt = biomeAt;
    }

    public static void Clear() => Publish(SettlementMap.Empty, null);

    /// <summary>
    /// The biome under <paramref name="vertex"/> — the ground a settled location stands on, which the
    /// sprawl never changes — or null when no world is published. Decides how a place is built: a
    /// village of the cold steppe is logs, one of the plain timber and daub.
    /// </summary>
    public static string? BiomeAt(int vertex) => _biomeAt?.Invoke(vertex);

    /// <summary>The settled location at <paramref name="vertex"/>, or null.</summary>
    public static SiteLocation? At(int vertex) => Map.At(vertex);

    /// <summary>The name of the region <paramref name="place"/> stands in, or null.</summary>
    public static string? RegionNameOf(Place place)
        => History != null && place.Region >= 0 && place.Region < History.RegionNames.Length ? History.RegionNames[place.Region] : null;

    /// <summary>Who holds the region <paramref name="place"/> stands in today, or null.</summary>
    public static Realm? HolderOf(Place place)
        => History != null && place.Region >= 0 ? History.OwnerOf(place.Region) : null;
}
