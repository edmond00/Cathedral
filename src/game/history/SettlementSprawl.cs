using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.History;

/// <summary>What a settled cell is, as the location factories and the map read it.</summary>
public enum SiteRole
{
    /// <summary>A historical place still standing.</summary>
    Historic,
    /// <summary>A historical place in ruins.</summary>
    Ruin,
    /// <summary>A city around an urban place.</summary>
    City,
    /// <summary>A field, orchard, grove, plantation, cellar, garden, paddy or vineyard.</summary>
    Agriculture,
    /// <summary>A village, burg, hamlet, townlet or fort.</summary>
    Settlement,
    /// <summary>A farm, stable, sheepfold, ranch or pasture.</summary>
    Livestock,
}

/// <summary>
/// One settled cell: what location stands there (<see cref="Key"/>, a <c>BiomeDatabase.Locations</c>
/// key), what role it plays, and which historical place it belongs to — the place itself for a
/// historical site, the place it sprawled from for everything else.
/// </summary>
public sealed record SiteLocation(string Key, SiteRole Role, Place Origin)
{
    /// <summary>The historical place standing on this very cell, when the cell is one.</summary>
    public Place? Place => Role is SiteRole.Historic or SiteRole.Ruin ? Origin : null;
}

/// <summary>Every settled cell of a world, by vertex.</summary>
public sealed class SettlementMap
{
    public SettlementMap(IReadOnlyDictionary<int, SiteLocation> sites) => Sites = sites;

    public IReadOnlyDictionary<int, SiteLocation> Sites { get; }

    public SiteLocation? At(int vertex) => Sites.TryGetValue(vertex, out var s) ? s : null;

    public int Count(SiteRole role) => Sites.Values.Count(s => s.Role == role);

    /// <summary>Sprawled ground — cities, farmland, settlements, stock — as opposed to history's own places.</summary>
    public bool IsSprawl(int vertex)
        => Sites.TryGetValue(vertex, out var s) && s.Role is SiteRole.City or SiteRole.Agriculture
                                                    or SiteRole.Settlement or SiteRole.Livestock;

    public static readonly SettlementMap Empty = new(new Dictionary<int, SiteLocation>());
}

/// <summary>
/// The second pass of world generation: once history has put its places on the map, the peopled
/// country is grown around them.
///
/// <para><b>Backwards on purpose.</b> In the world a country fills with farms first and its great
/// buildings rise among them; here the buildings come first, from history, and the farms are grown
/// round them afterwards. The order is invisible to a player and it is what lets the map agree with
/// the chronicle: wherever history built, people live.</para>
///
/// <para><b>Three kinds of place</b> (<see cref="PlaceSites.CategoryOf"/>):
/// <list type="bullet">
/// <item><b>Urban</b> — one to three cities beside it, on city ground; then each city sprawls as a
/// rural place. An urban place with no room for a city sprawls as rural itself.</item>
/// <item><b>Rural</b> — farmland in a cascade: one to three agriculture locations beside it, then one
/// to three beside each of those, two or three levels deep. A third of them are then made settlements
/// or stock.</item>
/// <item><b>Isolated</b>, and every ruin — nothing.</item>
/// </list></para>
///
/// <para>Sprawl stays on livable ground, never takes a cell already taken, and tries the other free
/// neighbours of the same centre when one is blocked; when none is free that branch simply stops, so
/// space is the only limit on how far a country grows. Places sprawl in the order history founded
/// them, so the oldest seats have the first pick of the land. The world's settlement density scales
/// how many locations each step tries to place.</para>
///
/// <para>A pure function of its inputs and its <see cref="Random"/>: the game and the audits build the
/// same map from the same world.</para>
/// </summary>
public static class SettlementSprawl
{
    /// <summary>The share of farmland that becomes a settlement or stock.</summary>
    public const double SettledShare = 0.33;

    /// <summary>The location key a historical place stands as on the map.</summary>
    public static string LocationKeyOf(Place place) => place.Ruined.IsKnown ? SettlementTable.Ruin : place.Kind switch
    {
        PlaceKind.Citadel        => "citadel",
        PlaceKind.Castle         => "castle",
        PlaceKind.Port           => "port",
        PlaceKind.Fortress       => "fortress",
        PlaceKind.Temple         => "temple",
        PlaceKind.Sanctuary      => "sanctuary",
        PlaceKind.Mine           => "mine",
        PlaceKind.Monastery      => "monastery",
        PlaceKind.Commandery     => "commandery",
        PlaceKind.ImperialTemple => "imperial temple",
        PlaceKind.ImperialSchool => "imperial school",
        PlaceKind.Pyramid        => "pyramid",
        PlaceKind.BurialField    => "burial field",
        PlaceKind.Palace         => "palace",
        _                        => "wreck",
    };

    /// <param name="biomeAt">Each vertex's biome.</param>
    /// <param name="neighbours">The sphere's adjacency.</param>
    /// <param name="places">History's places, in founding order.</param>
    /// <param name="blocked">Cells already holding something no sprawl may take (a cave). A historical
    /// place may still stand on one: history placed it first.</param>
    /// <param name="realmAt">The realm holding each vertex's region today, or null. <b>A place's country
    /// never spreads across a border</b>: every cell it takes is held by the realm that holds the place
    /// itself, so a castle on a frontier farms its own side of it and no further.</param>
    /// <param name="density">The world's settlement density.</param>
    public static SettlementMap Build(int vertexCount, Func<int, string> biomeAt, Func<int, IEnumerable<int>> neighbours,
                                      IEnumerable<Place> places, Func<int, bool> blocked, Func<int, Realm?> realmAt,
                                      float density, Random rng)
    {
        var sites = new Dictionary<int, SiteLocation>();
        var ordered = places.Where(p => p.Vertex >= 0 && p.Vertex < vertexCount).ToList();

        // History's own places first, every one of them, so that no sprawl lands on a later place's cell.
        foreach (var p in ordered)
            sites[p.Vertex] = new SiteLocation(LocationKeyOf(p), p.Ruined.IsKnown ? SiteRole.Ruin : SiteRole.Historic, p);

        bool Free(int v) => !sites.ContainsKey(v) && !blocked(v);

        // The free neighbours of a centre that accept a location, in a fixed order, shuffled by rng.
        List<int> FreeAround(int centre, HashSet<string> ground, Realm? realm)
        {
            var list = neighbours(centre)
                .Where(n => Free(n) && ground.Contains(biomeAt(n)) && ReferenceEquals(realmAt(n), realm))
                .OrderBy(n => n).ToList();
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
            return list;
        }

        int Branches()
        {
            int k = rng.Next(1, 4);
            return Math.Clamp((int)Math.Round(k * density), 1, 5);
        }

        var farmland = new List<int>();

        void Rural(int centre, Place origin, Realm? realm)
        {
            var frontier = new List<int> { centre };
            int depth = rng.Next(2, 4);
            for (int level = 0; level < depth && frontier.Count > 0; level++)
            {
                var next = new List<int>();
                foreach (int node in frontier)
                {
                    var room = FreeAround(node, BiomeDatabase.LivableBiomes, realm);
                    foreach (int v in room.Take(Branches()))
                    {
                        var column = SettlementTable.Agriculture[biomeAt(v)];
                        sites[v] = new SiteLocation(column[rng.Next(column.Length)], SiteRole.Agriculture, origin);
                        farmland.Add(v);
                        next.Add(v);
                    }
                }
                frontier = next;
            }
        }

        foreach (var p in ordered)
        {
            var realm = realmAt(p.Vertex);
            switch (PlaceSites.CategoryAtPresent(p))
            {
                case PlaceCategory.Urban:
                    var cities = FreeAround(p.Vertex, BiomeDatabase.CityBiomes, realm).Take(Branches()).ToList();
                    foreach (int c in cities) sites[c] = new SiteLocation(SettlementTable.City, SiteRole.City, p);
                    if (cities.Count == 0) Rural(p.Vertex, p, realm);
                    foreach (int c in cities) Rural(c, p, realm);
                    break;
                case PlaceCategory.Rural:
                    Rural(p.Vertex, p, realm);
                    break;
            }
        }

        // A third of the farmland is where its people live, or keep their stock.
        foreach (int v in farmland)
        {
            if (rng.NextDouble() >= SettledShare) continue;
            string biome = biomeAt(v);
            var stock = SettlementTable.Livestock[biome];
            var homes = SettlementTable.Settlements[biome];
            bool asStock = stock.Length > 0 && rng.NextDouble() < 0.5;
            var origin = sites[v].Origin;
            sites[v] = asStock
                ? new SiteLocation(stock[rng.Next(stock.Length)], SiteRole.Livestock, origin)
                : new SiteLocation(homes[rng.Next(homes.Length)], SiteRole.Settlement, origin);
        }

        return new SettlementMap(sites);
    }
}
