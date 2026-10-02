using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.History;

/// <summary>
/// What history needs to know about one region of the map: where it is, what borders it, and what kind
/// of country it is. Built once from the generated world; history reads it and never changes it.
/// </summary>
public sealed class RegionProfile
{
    public required int Id { get; init; }
    public required int LandmassId { get; init; }
    public required int CellCount { get; init; }
    public required int SeedVertex { get; init; }

    /// <summary>Land neighbours, sorted, so iteration never depends on a hash set's order.</summary>
    public required int[] Neighbours { get; init; }

    /// <summary>At least one of its cells touches sea or ocean. A port can only stand in such a region.</summary>
    public required bool Coastal { get; init; }

    public required int FieldCells { get; init; }
    public required int ForestCells { get; init; }
    public required int MountainCells { get; init; }
    public required int PlainCells { get; init; }
    public required int CityCells { get; init; }

    /// <summary>Farmable and livable ground: fields count double, plain and city once.</summary>
    public int Habitability => FieldCells * 2 + PlainCells + CityCells * 2;

    public bool IsMountainous => MountainCells * 2 >= CellCount;
    public bool IsForested => ForestCells * 2 >= CellCount;
}

/// <summary>
/// The whole map as history sees it: every region, and the landmasses they sit on.
///
/// <para>Built by <see cref="Build"/> from the same inputs the region division itself reads, so the
/// game (<c>MicroworldInterface</c>) and the headless audit feed it identically.</para>
/// </summary>
public sealed class HistoryGeography
{
    public HistoryGeography(IReadOnlyList<RegionProfile> regions, int landmassCount)
    {
        Regions = regions;
        LandmassCount = landmassCount;
    }

    public IReadOnlyList<RegionProfile> Regions { get; }
    public int LandmassCount { get; }

    public bool AnyCoastal => Regions.Any(r => r.Coastal);

    /// <param name="biomeAt">The biome name of a vertex, or null where there is none.</param>
    public static HistoryGeography Build(WorldRegionMap map, int vertexCount,
                                         Func<int, IEnumerable<int>> neighboursOf,
                                         Func<int, string?> biomeAt)
    {
        int count = map.Regions.Count;
        var coastal = new bool[count];
        var field = new int[count];
        var forest = new int[count];
        var mountain = new int[count];
        var plain = new int[count];
        var city = new int[count];

        for (int v = 0; v < vertexCount; v++)
        {
            int r = map.RegionAt(v);
            if (r < 0) continue;
            // The climate's ground is read as the temperate ground it was: a people can live in a
            // jungle as in a forest, and on desert or cold steppe as on open plain, and the high
            // ground stays high whatever the weather. The lore's own peoples of the hot and cold
            // worlds would otherwise have nowhere habitable to stand.
            switch (biomeAt(v))
            {
                case "field":    field[r]++;    break;
                case "forest":
                case BiomeDatabase.Jungle:     forest[r]++;   break;
                case "mountain":
                case "peak":
                case BiomeDatabase.HotSteppe:
                case BiomeDatabase.Snowfield:
                case BiomeDatabase.Glacier:    mountain[r]++; break;
                case "plain":
                case BiomeDatabase.Desert:
                case BiomeDatabase.Canyon:
                case BiomeDatabase.ColdSteppe:
                case BiomeDatabase.SeaIce:     plain[r]++;    break;
                case "city":     city[r]++;     break;
            }
            if (!coastal[r])
                foreach (int w in neighboursOf(v))
                {
                    string? b = biomeAt(w);
                    if (b != null && BiomeDatabase.WaterBiomes.Contains(b)) { coastal[r] = true; break; }
                }
        }

        var regions = map.Regions.Select(r => new RegionProfile
        {
            Id = r.Id,
            LandmassId = r.LandmassId,
            CellCount = r.CellCount,
            SeedVertex = r.SeedVertex,
            Neighbours = r.Neighbours.OrderBy(n => n).ToArray(),
            Coastal = coastal[r.Id],
            FieldCells = field[r.Id],
            ForestCells = forest[r.Id],
            MountainCells = mountain[r.Id],
            PlainCells = plain[r.Id],
            CityCells = city[r.Id],
        }).ToList();

        return new HistoryGeography(regions, map.LandmassCount);
    }
}
