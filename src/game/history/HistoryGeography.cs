using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.History;

/// <summary>
/// One cell of a region as history sees it: where it is on the sphere and what kind of ground it is.
/// What <see cref="PlaceSites"/> reads to decide whether a place of a given kind may stand there.
/// </summary>
/// <param name="Vertex">The sphere vertex.</param>
/// <param name="Biome">Its biome name.</param>
/// <param name="OnWater">It borders sea or ocean.</param>
/// <param name="NearLivable">It borders livable ground (<see cref="BiomeDatabase.LivableBiomes"/>).</param>
/// <param name="NearCityLand">It borders ground a city may stand on (<see cref="BiomeDatabase.CityBiomes"/>).</param>
public readonly record struct SiteCell(int Vertex, string Biome, bool OnWater, bool NearLivable, bool NearCityLand);

/// <summary>
/// What history needs to know about one region of the map: where it is, what borders it, what kind
/// of country it is, and the cells a place may be built on. Built once from the generated world;
/// history reads it and never changes it.
/// </summary>
public sealed class RegionProfile
{
    public required int Id { get; init; }
    public required int LandmassId { get; init; }
    public required int CellCount { get; init; }
    public required int SeedVertex { get; init; }

    /// <summary>Land neighbours, sorted, so iteration never depends on a hash set's order.</summary>
    public required int[] Neighbours { get; init; }

    /// <summary>At least one of its cells touches sea or ocean.</summary>
    public required bool Coastal { get; init; }

    /// <summary>Open plain.</summary>
    public required int PlainCells { get; init; }

    /// <summary>Forest and jungle together: the wooded country.</summary>
    public required int ForestCells { get; init; }

    /// <summary>The high ground: mountain and peak, and what heat or cold made of them.</summary>
    public required int MountainCells { get; init; }

    /// <summary>Livable cells of any climate (<see cref="BiomeDatabase.LivableBiomes"/>).</summary>
    public required int LivableCells { get; init; }

    /// <summary>
    /// How much of the region people can live on, weighted by how well: open plain counts whole, the
    /// hot and cold steppes a little over half, jungle, mountain and snowfield a fraction. What history
    /// reads to choose a capital and where a realm is most likely to rise. It once counted tilled field
    /// double; field no longer exists before history runs, since it sprawls from history's places.
    /// </summary>
    public required int Habitability { get; init; }

    /// <summary>Every cell of the region, in vertex order: where its places may be built.</summary>
    public required IReadOnlyList<SiteCell> Cells { get; init; }

    public bool IsMountainous => MountainCells * 2 >= CellCount;
    public bool IsForested => ForestCells * 2 >= CellCount;
}

/// <summary>
/// The whole map as history sees it: every region, the landmasses they sit on, and how thickly the
/// world's variant settles it.
///
/// <para>Built by <see cref="Build"/> from the same inputs the region division itself reads, so the
/// game (<c>MicroworldInterface</c>) and the headless audit feed it identically.</para>
/// </summary>
public sealed class HistoryGeography
{
    public HistoryGeography(IReadOnlyList<RegionProfile> regions, int landmassCount, float density,
                            Func<int, IEnumerable<int>> neighbours)
    {
        Regions = regions;
        LandmassCount = landmassCount;
        Density = density;
        Neighbours = neighbours;
    }

    public IReadOnlyList<RegionProfile> Regions { get; }
    public int LandmassCount { get; }

    /// <summary>The variant's <see cref="WorldShape.SettlementDensity"/>: how often history builds.</summary>
    public float Density { get; }

    /// <summary>The sphere's adjacency, for choosing a site with room around it.</summary>
    public Func<int, IEnumerable<int>> Neighbours { get; }

    public bool AnyCoastal => Regions.Any(r => r.Coastal);

    /// <summary>How each livable biome counts toward <see cref="RegionProfile.Habitability"/>.</summary>
    public static float HabitabilityWeight(string biome) => biome switch
    {
        "plain"                    => 1.0f,
        BiomeDatabase.HotSteppe    => 0.6f,
        BiomeDatabase.ColdSteppe   => 0.6f,
        BiomeDatabase.Jungle       => 0.4f,
        "mountain"                 => 0.3f,
        BiomeDatabase.Snowfield    => 0.2f,
        _                          => 0f,
    };

    /// <param name="biomeAt">The biome name of a vertex, or null where there is none.</param>
    /// <param name="density">The variant's settlement density; 1 at the baseline.</param>
    public static HistoryGeography Build(WorldRegionMap map, int vertexCount,
                                         Func<int, IEnumerable<int>> neighboursOf,
                                         Func<int, string?> biomeAt, float density = 1f)
    {
        int count = map.Regions.Count;
        var coastal = new bool[count];
        var forest = new int[count];
        var mountain = new int[count];
        var plain = new int[count];
        var livable = new int[count];
        var habitability = new float[count];
        var cells = new List<SiteCell>[count];
        for (int r = 0; r < count; r++) cells[r] = new List<SiteCell>();

        for (int v = 0; v < vertexCount; v++)
        {
            int r = map.RegionAt(v);
            if (r < 0) continue;
            string biome = biomeAt(v) ?? "";
            switch (biome)
            {
                case "plain":                  plain[r]++;    break;
                case "forest":
                case BiomeDatabase.Jungle:     forest[r]++;   break;
                case "mountain":
                case "peak":
                case BiomeDatabase.HotSteppe:
                case BiomeDatabase.Snowfield:
                case BiomeDatabase.Glacier:    mountain[r]++; break;
            }
            if (BiomeDatabase.LivableBiomes.Contains(biome)) livable[r]++;
            habitability[r] += HabitabilityWeight(biome);

            bool onWater = false, nearLivable = false, nearCity = false;
            foreach (int w in neighboursOf(v))
            {
                string? b = biomeAt(w);
                if (b == null) continue;
                if (BiomeDatabase.WaterBiomes.Contains(b)) onWater = true;
                if (BiomeDatabase.LivableBiomes.Contains(b)) nearLivable = true;
                if (BiomeDatabase.CityBiomes.Contains(b)) nearCity = true;
            }
            if (onWater) coastal[r] = true;
            cells[r].Add(new SiteCell(v, biome, onWater, nearLivable, nearCity));
        }

        var regions = map.Regions.Select(r => new RegionProfile
        {
            Id = r.Id,
            LandmassId = r.LandmassId,
            CellCount = r.CellCount,
            SeedVertex = r.SeedVertex,
            Neighbours = r.Neighbours.OrderBy(n => n).ToArray(),
            Coastal = coastal[r.Id],
            PlainCells = plain[r.Id],
            ForestCells = forest[r.Id],
            MountainCells = mountain[r.Id],
            LivableCells = livable[r.Id],
            Habitability = (int)MathF.Round(habitability[r.Id]),
            Cells = cells[r.Id],
        }).ToList();

        return new HistoryGeography(regions, map.LandmassCount, density, neighboursOf);
    }
}
