using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.History;

/// <summary>
/// How a historical place shapes the country around it once history is laid on the map
/// (see <c>SettlementSprawl</c>).
/// </summary>
public enum PlaceCategory
{
    /// <summary>The centre of a peopled country: cities sprawl from it, and farmland from them.</summary>
    Urban,

    /// <summary>A seat in the countryside: farmland and a few settlements sprawl from it.</summary>
    Rural,

    /// <summary>Stands alone; nothing sprawls from it.</summary>
    Isolated,
}

/// <summary>
/// Where each kind of historical place may stand, and what it does to the country around it.
///
/// <para>The one place these rules live. History asks it when a place is founded — a building event
/// only offers the kinds its region can host, and a place is put on a cell that suits it — and the
/// sprawl asks it again afterwards. Asking two copies would let a port be founded where it could
/// never sprawl from.</para>
/// </summary>
public static class PlaceSites
{
    public static PlaceCategory CategoryOf(PlaceKind kind) => kind switch
    {
        PlaceKind.Citadel or PlaceKind.Port or PlaceKind.Palace or PlaceKind.ImperialSchool
            => PlaceCategory.Urban,
        PlaceKind.Castle or PlaceKind.Fortress or PlaceKind.Temple or PlaceKind.ImperialTemple
            or PlaceKind.Commandery
            => PlaceCategory.Rural,
        _ => PlaceCategory.Isolated,
    };

    /// <summary>The category a place has today: a ruin sprawls nothing, whatever it was.</summary>
    public static PlaceCategory CategoryAtPresent(Place place)
        => place.Ruined.IsKnown ? PlaceCategory.Isolated : CategoryOf(place.Kind);

    // Ground an isolated place seeks out.
    private static readonly HashSet<string> MonasteryGround = new()
        { "mountain", "peak", "forest", "canyon", BiomeDatabase.Snowfield };
    private static readonly HashSet<string> MineGround = new()
        { "mountain", "peak", "canyon", BiomeDatabase.HotSteppe, BiomeDatabase.Snowfield, BiomeDatabase.Glacier };
    private static readonly HashSet<string> SanctuaryGround = new()
        { "forest", "peak", "mountain", "canyon", "coast", BiomeDatabase.Desert, BiomeDatabase.Jungle, BiomeDatabase.Glacier };
    private static readonly HashSet<string> PyramidGround = new()
        { "plain", "canyon", BiomeDatabase.Desert, BiomeDatabase.HotSteppe, BiomeDatabase.ColdSteppe };

    /// <summary>
    /// Whether a place of <paramref name="kind"/> may stand on <paramref name="cell"/>.
    /// <list type="bullet">
    /// <item>Urban places stand on city ground (livable, but not jungle or snowfield) — except a
    /// port, which stands on the shore and must have city ground behind it to sprawl into.</item>
    /// <item>Rural places stand on any livable ground — except a commandery, a sea fort, which stands
    /// on the shore with livable ground behind it.</item>
    /// <item>Isolated places seek their own ground: a mine the high rock, a monastery the remote
    /// heights and woods, a sanctuary the wild, a pyramid the open dry country.</item>
    /// </list>
    /// </summary>
    public static bool Fits(PlaceKind kind, SiteCell cell) => kind switch
    {
        PlaceKind.Port           => cell.Biome == "coast" && cell.NearCityLand,
        PlaceKind.Commandery     => cell.Biome == "coast" && cell.NearLivable,
        PlaceKind.Wreck          => cell.Biome == "coast",
        PlaceKind.Citadel or PlaceKind.Palace or PlaceKind.ImperialSchool
                                 => BiomeDatabase.CityBiomes.Contains(cell.Biome),
        PlaceKind.Castle or PlaceKind.Fortress or PlaceKind.Temple or PlaceKind.ImperialTemple
                                 => BiomeDatabase.LivableBiomes.Contains(cell.Biome),
        PlaceKind.Monastery      => MonasteryGround.Contains(cell.Biome),
        PlaceKind.Mine           => MineGround.Contains(cell.Biome),
        PlaceKind.Sanctuary      => SanctuaryGround.Contains(cell.Biome),
        PlaceKind.Pyramid        => PyramidGround.Contains(cell.Biome),
        PlaceKind.BurialField    => BiomeDatabase.LivableBiomes.Contains(cell.Biome) || cell.Biome == BiomeDatabase.Desert,
        _                        => false,
    };

    /// <summary>Whether <paramref name="region"/> has any free cell a <paramref name="kind"/> fits.</summary>
    public static bool CanHost(PlaceKind kind, RegionProfile region, IReadOnlySet<int> taken)
    {
        foreach (var c in region.Cells)
            if (!taken.Contains(c.Vertex) && Fits(kind, c)) return true;
        return false;
    }

    /// <summary>
    /// A free cell of <paramref name="region"/> for a <paramref name="kind"/>, or -1. Prefers a cell
    /// with no place beside it, so that two places do not crowd each other's sprawl; among those, any.
    /// Deterministic for a given <paramref name="rng"/> state: cells are scanned in vertex order.
    /// </summary>
    public static int PickSite(PlaceKind kind, RegionProfile region, IReadOnlySet<int> taken,
                               Func<int, IEnumerable<int>> neighbours, Random rng)
    {
        var fits = region.Cells.Where(c => !taken.Contains(c.Vertex) && Fits(kind, c)).Select(c => c.Vertex).ToList();
        if (fits.Count == 0) return -1;
        var roomy = fits.Where(v => !neighbours(v).Any(taken.Contains)).ToList();
        var from = roomy.Count > 0 ? roomy : fits;
        return from[rng.Next(from.Count)];
    }
}
