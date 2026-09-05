// WorldRegions.cs - Divides the generated world into regions.
//
// A region is a contiguous patch of land. The division happens in four steps, each of which
// answers a different question about the map:
//
//   1. LANDMASSES  - what is separated by water? Connected components of the land graph. Nothing
//                    ever crosses a coast, so an isle is never part of a continent's region.
//   2. SEEDS       - where does a region grow from? The peaks of the settlement field: the same
//                    Perlin layer that decides field and city tiles, smoothed until its extrema
//                    are broad "cores of habitation" rather than single-vertex speckle, and read
//                    only where somebody could actually live - off the mountains, and inland
//                    enough that the smoothing had real country to average over. A large landmass
//                    with several such cores is divided between them; one with too few gets more
//                    added at its far ends until no region is larger than a province.
//   3. GROWTH      - how far does each seed reach? A multi-source Dijkstra whose edge weight is
//                    the biome's travel duration, so a region spreads cheaply across plain and
//                    field and expensively across forest, mountain and peak. The frontier settles
//                    where two seeds meet at equal travel cost - which is to say, on the ridges
//                    and the deep woods. Nobody draws the borders; the terrain does.
//   4. PALETTE     - which colour does each region get? One of its own: the palette is built to the
//                    region count and spread over the whole hue wheel, dealt out by a DSATUR
//                    colouring of the region adjacency graph and then rearranged until the closest
//                    pair of bordering colours anywhere is as far apart as swapping can make it.
//
// Everything here is a pure function of the generated world: no RNG, no clock, no ordering that
// depends on a hash table's enumeration. The same world always divides the same way.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

using Vector3 = OpenTK.Mathematics.Vector3;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>One region of the world: a contiguous patch of land grown from a single seed.</summary>
    public sealed class WorldRegion
    {
        /// <summary>Index into <see cref="WorldRegionMap.Regions"/>. Stable for one generated world.</summary>
        public int Id { get; internal set; }

        /// <summary>The vertex the region grew from - its most habitable point.</summary>
        public int SeedVertex { get; internal set; }

        /// <summary>Which landmass this region belongs to. Regions never span two landmasses.</summary>
        public int LandmassId { get; internal set; }

        /// <summary>How many world cells the region covers.</summary>
        public int CellCount { get; internal set; }

        /// <summary>Ids of the regions this one shares a land border with.</summary>
        public HashSet<int> Neighbours { get; } = new();

        /// <summary>
        /// Index into <see cref="WorldRegionMap.Palette"/>. Unique across the world — no two regions
        /// anywhere share a colour — and chosen so that bordering regions are as far apart on it as
        /// the palette allows.
        /// </summary>
        public int PaletteIndex { get; internal set; }
    }

    /// <summary>
    /// The world data <see cref="WorldRegionMap.Build"/> needs, supplied as delegates so the division
    /// can be computed (and tested) without a sphere, a window or a biome database.
    /// </summary>
    public sealed class WorldRegionInput
    {
        public required int VertexCount { get; init; }

        /// <summary>The mesh neighbours of a vertex.</summary>
        public required Func<int, IEnumerable<int>> Neighbours { get; init; }

        /// <summary>False for sea and ocean; regions only cover land.</summary>
        public required Func<int, bool> IsLand { get; init; }

        /// <summary>
        /// Land where the settlement layer is what decides the biome at all — that is, land that is
        /// not mountain or peak. <c>DetermineBiome</c> tests the mountain layer <b>before</b> the
        /// field and city thresholds, so above the treeline the settlement noise decides nothing and
        /// its value there is not evidence of anything. Regions still grow across the mountains;
        /// they are just never centred on one.
        /// </summary>
        public required Func<int, bool> IsSettleable { get; init; }

        /// <summary>
        /// The Perlin layer that classifies fields and cities. <b>Lower is more settled</b>, matching
        /// <c>DetermineBiome</c>, which reads city below -0.58 and field below -0.38.
        /// </summary>
        public required Func<int, float> SettlementNoise { get; init; }

        /// <summary>Days of foot travel to cross this cell - the cost that shapes the borders.</summary>
        public required Func<int, float> StepCostDays { get; init; }

        public required Func<int, Vector3> Position { get; init; }
    }

    /// <summary>
    /// The world's division into regions: which region and which landmass each vertex belongs to,
    /// and the regions themselves with their borders and colours.
    /// </summary>
    public sealed class WorldRegionMap
    {
        private readonly int[] _regionOf;
        private readonly int[] _landmassOf;

        /// <summary>Every region, indexed by <see cref="WorldRegion.Id"/>.</summary>
        public IReadOnlyList<WorldRegion> Regions { get; }

        /// <summary>How many separate landmasses the world has.</summary>
        public int LandmassCount { get; }

        /// <summary>
        /// The overlay's colours, indexed by <see cref="WorldRegion.PaletteIndex"/>. Owned by the
        /// map rather than being a static table, because it is built to the region count — which is
        /// what lets every region have a colour nothing else on the world wears.
        /// </summary>
        public IReadOnlyList<WorldRegionPalette.Swatch> Palette { get; }

        private WorldRegionMap(int[] regionOf, int[] landmassOf, List<WorldRegion> regions,
                               int landmassCount, WorldRegionPalette.Swatch[] palette)
        {
            _regionOf = regionOf;
            _landmassOf = landmassOf;
            Regions = regions;
            LandmassCount = landmassCount;
            Palette = palette;
        }

        /// <summary>The region covering <paramref name="vertex"/>, or -1 for water.</summary>
        public int RegionAt(int vertex)
            => vertex >= 0 && vertex < _regionOf.Length ? _regionOf[vertex] : -1;

        /// <summary>The landmass containing <paramref name="vertex"/>, or -1 for water.</summary>
        public int LandmassAt(int vertex)
            => vertex >= 0 && vertex < _landmassOf.Length ? _landmassOf[vertex] : -1;

        /// <summary>The palette swatch for <paramref name="vertex"/>, or null for water.</summary>
        public WorldRegionPalette.Swatch? SwatchAt(int vertex)
        {
            int r = RegionAt(vertex);
            return r < 0 ? null : Palette[Regions[r].PaletteIndex];
        }

        // -- Construction ------------------------------------------------------------

        public static WorldRegionMap Build(WorldRegionInput input)
        {
            int n = input.VertexCount;

            // Materialise everything the loops below read repeatedly. The smoothing pass alone
            // touches every land vertex's neighbours a couple of hundred times, and a delegate
            // call per touch is the difference between a blink and a visible stall.
            var adjacency = new int[n][];
            var isLand    = new bool[n];
            var settled   = new bool[n];
            var noise     = new float[n];
            var stepCost  = new float[n];
            var positions = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                adjacency[i] = input.Neighbours(i).ToArray();
                isLand[i]    = input.IsLand(i);
                settled[i]   = input.IsSettleable(i);
                noise[i]     = input.SettlementNoise(i);
                stepCost[i]  = input.StepCostDays(i);
                positions[i] = input.Position(i);
            }

            var (landmassOf, landmassCount) = FindLandmasses(n, adjacency, isLand);
            var (habitability, coverage) = SmoothHabitability(n, adjacency, settled, noise);

            // Where a region's heart may be put: settleable ground with enough settleable ground
            // around it. See MinSeedCoverage for why the second half is not redundant.
            var isCore = new bool[n];
            for (int v = 0; v < n; v++)
                isCore[v] = settled[v] && coverage[v] >= Config.WorldRegions.MinSeedCoverage;

            var seeds = ChooseSeeds(n, adjacency, isLand, isCore, settled, landmassOf, landmassCount,
                                    habitability, positions);

            var (regionOf, dist) = GrowRegions(n, adjacency, isLand, stepCost, seeds);
            SplitOversizedRegions(n, adjacency, isLand, isCore, stepCost, seeds, ref regionOf, ref dist);
            AbsorbSmallRegions(n, adjacency, regionOf, seeds.Count);

            var regions = Compact(n, regionOf, seeds, landmassOf);
            BuildAdjacency(n, adjacency, regionOf, regions);

            var palette = WorldRegionPalette.Build(regions.Count);
            Colour(regions, palette);
            ImproveWorstBorders(regions, palette);

            return new WorldRegionMap(regionOf, landmassOf, regions, landmassCount, palette);
        }

        /// <summary>Connected components of the land graph - one per continent or isle.</summary>
        private static (int[] landmassOf, int count) FindLandmasses(int n, int[][] adjacency, bool[] isLand)
        {
            var landmassOf = new int[n];
            Array.Fill(landmassOf, -1);
            int count = 0;
            var stack = new Stack<int>();

            for (int start = 0; start < n; start++)
            {
                if (!isLand[start] || landmassOf[start] >= 0) continue;

                int id = count++;
                stack.Push(start);
                landmassOf[start] = id;
                while (stack.Count > 0)
                {
                    int v = stack.Pop();
                    foreach (int w in adjacency[v])
                    {
                        if (!isLand[w] || landmassOf[w] >= 0) continue;
                        landmassOf[w] = id;
                        stack.Push(w);
                    }
                }
            }
            return (landmassOf, count);
        }

        /// <summary>
        /// The settlement field, negated so high means habitable, then diffused until its features
        /// are region-sized.
        ///
        /// <para>The raw layer is sampled at a scale of three world units - four vertices or so -
        /// which is the right frequency for scattering fields and woods and far too fine to seed a
        /// region: every other vertex is a local extremum. Diffusion is what turns that speckle into
        /// the broad envelope underneath it, whose peaks are the places where farmland actually
        /// clusters.</para>
        ///
        /// <para><b>Two fields are diffused, not one</b>, and the second is what makes the first
        /// mean anything. Sea and mountain carry no settlement value, so they enter the sum as zero
        /// — and a coastal vertex, having fewer land neighbours to spread into, would then simply
        /// <i>keep</i> more of its own heat than an inland one and read as the most habitable place
        /// for miles. That is not a fact about the world, it is the boundary of the domain, and it
        /// put half the region seeds of a test world on a beach. So the mask is diffused by the very
        /// same kernel and the value divided by it: what comes out is the weighted mean over the
        /// settleable land in the neighbourhood, with the boundary divided back out of it.</para>
        ///
        /// <para>Where nothing settleable is in reach at all the mask is zero and there is nothing to
        /// divide by; those vertices fall back to the raw layer, which is all the evidence there is.</para>
        /// </summary>
        private static (float[] habitability, float[] coverage) SmoothHabitability(
            int n, int[][] adjacency, bool[] settleable, float[] noise)
        {
            var h = new float[n];
            var m = new float[n];
            for (int i = 0; i < n; i++)
            {
                h[i] = settleable[i] ? -noise[i] : 0f;
                m[i] = settleable[i] ? 1f : 0f;
            }

            var nextH = new float[n];
            var nextM = new float[n];
            for (int pass = 0; pass < Config.WorldRegions.SmoothingPasses; pass++)
            {
                for (int v = 0; v < n; v++)
                {
                    float sumH = h[v], sumM = m[v];
                    int cnt = 1;
                    foreach (int w in adjacency[v])
                    {
                        sumH += h[w];
                        sumM += m[w];
                        cnt++;
                    }
                    nextH[v] = sumH / cnt;
                    nextM[v] = sumM / cnt;
                }
                (h, nextH) = (nextH, h);
                (m, nextM) = (nextM, m);
            }

            var result = new float[n];
            for (int v = 0; v < n; v++)
                result[v] = m[v] > 1e-6f ? h[v] / m[v] : -noise[v];
            return (result, m);
        }

        /// <summary>
        /// Picks the vertices regions grow from: strict local maxima of the smoothed habitability
        /// among the land a region's heart may sit on, taken best-first and rejected when they crowd
        /// a seed already taken on the same landmass.
        ///
        /// <para>Every landmass gets at least one seed however small, however uniform and however
        /// mountainous it is, through a fallback that relaxes the two conditions in turn — the
        /// interior requirement first, then the settleable one. An atoll is a region; a bare crag is
        /// a region centred on the crag.</para>
        /// </summary>
        private static List<int> ChooseSeeds(int n, int[][] adjacency, bool[] isLand, bool[] isCore,
                                             bool[] settleable, int[] landmassOf, int landmassCount,
                                             float[] habitability, Vector3[] positions)
        {
            var candidates = new List<int>();
            for (int v = 0; v < n; v++)
            {
                if (!isCore[v]) continue;
                bool isMax = true;
                foreach (int w in adjacency[v])
                {
                    if (!isLand[w]) continue;
                    if (habitability[w] >= habitability[v]) { isMax = false; break; }
                }
                if (isMax) candidates.Add(v);
            }

            // Best first, vertex index breaking ties, so the result never depends on the order the
            // scan happened to find equal peaks in.
            candidates.Sort((a, b) =>
            {
                int c = habitability[b].CompareTo(habitability[a]);
                return c != 0 ? c : a.CompareTo(b);
            });

            float minSep = Config.WorldRegions.MinSeedSeparation;
            var seeds = new List<int>();
            var seedsByLandmass = new List<int>[landmassCount];
            for (int i = 0; i < landmassCount; i++) seedsByLandmass[i] = new List<int>();

            foreach (int v in candidates)
            {
                if (seeds.Count >= Config.WorldRegions.MaxRegions) break;
                int mass = landmassOf[v];
                bool crowded = seedsByLandmass[mass]
                    .Any(s => Vector3.Distance(positions[s], positions[v]) < minSep);
                if (crowded) continue;
                seeds.Add(v);
                seedsByLandmass[mass].Add(v);
            }

            // A landmass with no strict maximum of its own - a small isle, or one flat enough that
            // its peak was suppressed by a neighbour's - still has to be somewhere. Give it its most
            // habitable vertex, preferring a settleable one: a bare crag of an island is still a
            // region, and it has to be centred on the crag.
            var best = new int[landmassCount];           // any land at all
            var bestSettleable = new int[landmassCount];  // land somebody could live on
            var bestCore = new int[landmassCount];        // land somebody could live on, inland
            Array.Fill(best, -1);
            Array.Fill(bestSettleable, -1);
            Array.Fill(bestCore, -1);
            for (int v = 0; v < n; v++)
            {
                if (!isLand[v]) continue;
                int mass = landmassOf[v];
                if (best[mass] < 0 || habitability[v] > habitability[best[mass]]) best[mass] = v;
                if (settleable[v] &&
                    (bestSettleable[mass] < 0 || habitability[v] > habitability[bestSettleable[mass]]))
                    bestSettleable[mass] = v;
                if (isCore[v] &&
                    (bestCore[mass] < 0 || habitability[v] > habitability[bestCore[mass]]))
                    bestCore[mass] = v;
            }
            for (int mass = 0; mass < landmassCount; mass++)
            {
                if (bestCore[mass] >= 0) best[mass] = bestCore[mass];
                else if (bestSettleable[mass] >= 0) best[mass] = bestSettleable[mass];
            }
            for (int mass = 0; mass < landmassCount; mass++)
            {
                if (seedsByLandmass[mass].Count > 0 || best[mass] < 0) continue;
                seeds.Add(best[mass]);
                seedsByLandmass[mass].Add(best[mass]);
            }

            return seeds;
        }

        /// <summary>
        /// Multi-source Dijkstra from the seeds, weighting each step by the travel duration of the
        /// two cells it joins. Returns the region index (position in <paramref name="seeds"/>) and
        /// the accumulated travel cost for every land vertex; water stays at -1.
        /// </summary>
        private static (int[] regionOf, float[] dist) GrowRegions(
            int n, int[][] adjacency, bool[] isLand, float[] stepCost, List<int> seeds)
        {
            var regionOf = new int[n];
            var dist = new float[n];
            Array.Fill(regionOf, -1);
            Array.Fill(dist, float.MaxValue);

            var frontier = new PriorityQueue<int, float>();
            for (int s = 0; s < seeds.Count; s++)
            {
                int v = seeds[s];
                // Two seeds on one vertex would leave a region empty, because the vertex goes to
                // whichever was enqueued first. Compact() drops the empty one, but its id would
                // still be pointing at the wrong seed.
                if (regionOf[v] >= 0) continue;
                regionOf[v] = s;
                dist[v] = 0f;
                frontier.Enqueue(v, 0f);
            }

            while (frontier.TryDequeue(out int v, out float d))
            {
                if (d > dist[v]) continue;   // stale entry
                foreach (int w in adjacency[v])
                {
                    if (!isLand[w]) continue;
                    float nd = d + 0.5f * (stepCost[v] + stepCost[w]);
                    if (nd >= dist[w]) continue;
                    dist[w] = nd;
                    regionOf[w] = regionOf[v];
                    frontier.Enqueue(w, nd);
                }
            }

            return (regionOf, dist);
        }

        /// <summary>
        /// Adds seeds until no region is larger than <c>Config.WorldRegions.MaxRegionCells</c>, each
        /// new one at the point of the offending region that is furthest - in travel cost - from the
        /// seed that owns it.
        ///
        /// <para>Habitability peaks alone leave a smooth continent under-divided: a landmass whose
        /// settlement field has one broad hump becomes one region the size of a hemisphere. This is
        /// the floor under that, and it is farthest-point seeding rather than a grid cut, so the new
        /// border still falls on whatever mountain or wood was holding the region's far end apart
        /// from its core.</para>
        /// </summary>
        private static void SplitOversizedRegions(
            int n, int[][] adjacency, bool[] isLand, bool[] isCore, float[] stepCost,
            List<int> seeds, ref int[] regionOf, ref float[] dist)
        {
            for (int iteration = 0; iteration < Config.WorldRegions.MaxSplitPasses; iteration++)
            {
                var counts = new int[seeds.Count];
                // The far end of the region, and the far end of the part of it anyone could live in.
                // A seed is where a region is reckoned from, so the second is preferred wherever it
                // exists — splitting a wide province at the summit of the range that divides it
                // would put the new centre on the one spot in it with nobody on it.
                var farthest = new int[seeds.Count];
                var farthestDist = new float[seeds.Count];
                var farthestCore = new int[seeds.Count];
                var farthestCoreDist = new float[seeds.Count];
                Array.Fill(farthest, -1);
                Array.Fill(farthestCore, -1);

                for (int v = 0; v < n; v++)
                {
                    int r = regionOf[v];
                    if (r < 0) continue;
                    counts[r]++;
                    if (farthest[r] < 0 || dist[v] > farthestDist[r])
                    {
                        farthest[r] = v;
                        farthestDist[r] = dist[v];
                    }
                    if (isCore[v] && (farthestCore[r] < 0 || dist[v] > farthestCoreDist[r]))
                    {
                        farthestCore[r] = v;
                        farthestCoreDist[r] = dist[v];
                    }
                }

                var added = new List<int>();
                for (int r = 0; r < counts.Length; r++)
                {
                    if (counts[r] <= Config.WorldRegions.MaxRegionCells) continue;
                    if (seeds.Count + added.Count >= Config.WorldRegions.MaxRegions) break;
                    int pick = farthestCore[r] >= 0 ? farthestCore[r] : farthest[r];
                    if (pick >= 0 && !seeds.Contains(pick)) added.Add(pick);
                }

                if (added.Count == 0) break;
                seeds.AddRange(added);
                (regionOf, dist) = GrowRegions(n, adjacency, isLand, stepCost, seeds);
            }
        }

        /// <summary>
        /// Folds any region below <c>Config.WorldRegions.MinRegionCells</c> into whichever neighbour
        /// it shares the most border with. A small isle has no land neighbour and so keeps its own
        /// region however small it is - an island is a region because it is an island, not because
        /// it is big.
        /// </summary>
        private static void AbsorbSmallRegions(int n, int[][] adjacency, int[] regionOf, int regionCount)
        {
            for (int pass = 0; pass < Config.WorldRegions.MaxAbsorbPasses; pass++)
            {
                var counts = new int[regionCount];
                for (int v = 0; v < n; v++)
                    if (regionOf[v] >= 0) counts[regionOf[v]]++;

                // Smallest first: absorbing one may lift its host over the threshold, and the next
                // smallest should then be measured against the merged size.
                var small = Enumerable.Range(0, regionCount)
                    .Where(r => counts[r] > 0 && counts[r] < Config.WorldRegions.MinRegionCells)
                    .OrderBy(r => counts[r]).ThenBy(r => r)
                    .ToList();
                if (small.Count == 0) return;

                bool merged = false;
                foreach (int r in small)
                {
                    if (counts[r] == 0) continue;   // already absorbed earlier in this pass

                    var border = new Dictionary<int, int>();
                    for (int v = 0; v < n; v++)
                    {
                        if (regionOf[v] != r) continue;
                        foreach (int w in adjacency[v])
                        {
                            int o = regionOf[w];
                            if (o < 0 || o == r) continue;
                            border[o] = border.TryGetValue(o, out int c) ? c + 1 : 1;
                        }
                    }
                    if (border.Count == 0) continue;   // an isle of its own; leave it be

                    int host = border.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
                    for (int v = 0; v < n; v++)
                        if (regionOf[v] == r) regionOf[v] = host;
                    counts[host] += counts[r];
                    counts[r] = 0;
                    merged = true;
                }
                if (!merged) return;
            }
        }

        /// <summary>Renumbers the surviving regions 0..k-1 and builds their records.</summary>
        private static List<WorldRegion> Compact(int n, int[] regionOf, List<int> seeds, int[] landmassOf)
        {
            var remap = new Dictionary<int, int>();
            var regions = new List<WorldRegion>();
            var counts = new List<int>();

            for (int v = 0; v < n; v++)
            {
                int r = regionOf[v];
                if (r < 0) continue;
                if (!remap.TryGetValue(r, out int id))
                {
                    id = regions.Count;
                    remap[r] = id;
                    regions.Add(new WorldRegion
                    {
                        Id = id,
                        SeedVertex = seeds[r],
                        LandmassId = landmassOf[seeds[r]],
                    });
                    counts.Add(0);
                }
                regionOf[v] = id;
                counts[id]++;
            }

            for (int i = 0; i < regions.Count; i++) regions[i].CellCount = counts[i];
            return regions;
        }

        private static void BuildAdjacency(int n, int[][] adjacency, int[] regionOf, List<WorldRegion> regions)
        {
            for (int v = 0; v < n; v++)
            {
                int r = regionOf[v];
                if (r < 0) continue;
                foreach (int w in adjacency[v])
                {
                    int o = regionOf[w];
                    if (o < 0 || o == r) continue;
                    regions[r].Neighbours.Add(o);
                    regions[o].Neighbours.Add(r);
                }
            }
        }

        /// <summary>
        /// Gives every region a colour of its own, arranged so that a border is always a visible
        /// edge.
        ///
        /// <para>The palette is built to the region count, so uniqueness is free and the only real
        /// question is <i>which</i> colour goes where. Regions are taken in DSATUR order — the one
        /// whose neighbours already wear the most distinct colours goes first, ties broken by
        /// degree — because the hardest region to place is the one hemmed in by decisions already
        /// made. Each then takes the unused colour <b>furthest from the colours around it</b>.</para>
        ///
        /// <para>Maximising the distance is right here and was wrong when the palette had eight
        /// entries: there, every bordering region drove to one of the two extremes and the map came
        /// out in two tones. With a colour per region there is nothing to exhaust — taking the
        /// furthest one still leaves every other region a colour of its own — so the greedy choice
        /// costs nothing and buys the widest borders available.</para>
        /// </summary>
        private static void Colour(List<WorldRegion> regions, WorldRegionPalette.Swatch[] palette)
        {
            var taken = new bool[palette.Length];
            foreach (var r in regions) r.PaletteIndex = -1;

            for (int done = 0; done < regions.Count; done++)
            {
                WorldRegion? pick = null;
                int bestSat = -1, bestDeg = -1;
                foreach (var r in regions)
                {
                    if (r.PaletteIndex >= 0) continue;
                    int sat = r.Neighbours.Count(id => regions[id].PaletteIndex >= 0);
                    if (sat > bestSat || (sat == bestSat && r.Neighbours.Count > bestDeg))
                    {
                        pick = r; bestSat = sat; bestDeg = r.Neighbours.Count;
                    }
                }
                if (pick == null) break;

                var neighbourColours = pick.Neighbours
                    .Select(id => regions[id].PaletteIndex)
                    .Where(c => c >= 0)
                    .ToList();

                int chosen = -1;
                float bestDistance = float.MinValue;
                for (int c = 0; c < palette.Length; c++)
                {
                    if (taken[c]) continue;
                    // An unbordered region — an isle of its own — has nothing to be far from, so it
                    // takes the first free colour and leaves the interesting ones to regions that
                    // have neighbours to be told apart from.
                    float d = neighbourColours.Count == 0
                        ? 0f
                        : neighbourColours.Min(o => WorldRegionPalette.Distance(palette[c], palette[o]));
                    if (chosen < 0 || d > bestDistance) { chosen = c; bestDistance = d; }
                }

                pick.PaletteIndex = chosen;
                taken[chosen] = true;
            }
        }

        /// <summary>
        /// Raises the worst border on the world by swapping colours between two regions.
        ///
        /// <para>The greedy assignment colours regions one at a time and never revisits one, so the
        /// regions it reaches last are handed whatever the earlier ones left — and the closest pair
        /// of bordering colours anywhere on the world is almost always one of those. Swapping is the
        /// cheap repair: a swap is a permutation, so every region still ends with a colour nobody
        /// else has, and only the objective changes.</para>
        ///
        /// <para>Hill-climbing on (worst border, then the sum of all of them). Each pass finds the
        /// closest bordering pair and tries giving one of the two a different region's colour,
        /// taking the first exchange that improves the pair without costing more elsewhere. It stops
        /// the moment nothing helps, which on a real world is well inside the pass budget.</para>
        /// </summary>
        private static void ImproveWorstBorders(List<WorldRegion> regions, WorldRegionPalette.Swatch[] palette)
        {
            // The border pairs, once: every unordered pair of regions that share ground.
            var edges = new List<(int A, int B)>();
            foreach (var r in regions)
                foreach (int o in r.Neighbours)
                    if (o > r.Id) edges.Add((r.Id, o));
            if (edges.Count == 0) return;   // a world of nothing but isles: no border to improve

            float Gap(int a, int b) =>
                WorldRegionPalette.Distance(palette[regions[a].PaletteIndex],
                                            palette[regions[b].PaletteIndex]);

            (float Worst, float Sum, int Edge) Evaluate()
            {
                float worst = float.MaxValue, sum = 0f;
                int at = 0;
                for (int i = 0; i < edges.Count; i++)
                {
                    float d = Gap(edges[i].A, edges[i].B);
                    sum += d;
                    if (d < worst) { worst = d; at = i; }
                }
                return (worst, sum, at);
            }

            var current = Evaluate();
            for (int pass = 0; pass < Config.WorldRegions.MaxColourSwapPasses; pass++)
            {
                var (a, b) = edges[current.Edge];
                bool improved = false;

                foreach (int fixedEnd in new[] { a, b })
                {
                    for (int other = 0; other < regions.Count && !improved; other++)
                    {
                        if (other == fixedEnd) continue;

                        (regions[fixedEnd].PaletteIndex, regions[other].PaletteIndex) =
                            (regions[other].PaletteIndex, regions[fixedEnd].PaletteIndex);

                        var candidate = Evaluate();
                        if (candidate.Worst > current.Worst ||
                            (candidate.Worst == current.Worst && candidate.Sum > current.Sum))
                        {
                            current = candidate;
                            improved = true;
                        }
                        else
                        {
                            (regions[fixedEnd].PaletteIndex, regions[other].PaletteIndex) =
                                (regions[other].PaletteIndex, regions[fixedEnd].PaletteIndex);
                        }
                    }
                    if (improved) break;
                }

                if (!improved) return;
            }
        }
    }

    /// <summary>
    /// The colours the region overlay draws with: one per region, so that no two regions anywhere
    /// on the world share one, and neighbours are as far apart as the wheel allows.
    ///
    /// <para><b>These are real colours, which the rest of the map has none of.</b> The sphere's
    /// fragment shader normally throws the vertex hue away — it reduces the colour to a luminance
    /// and re-tints it from the category in the alpha channel, which is what keeps the world to
    /// grayscale nature, ochre building and purple sea. Thirty regions cannot be told apart in three
    /// tones, so the overlay sets <see cref="OverlayCategory"/> instead and the shader takes the rgb
    /// as given. It is the one caller that opts out, and it is a developer view, so the world's own
    /// palette is not what it has to obey.</para>
    ///
    /// <para>Hues are spaced by the golden angle rather than divided evenly. An even division of a
    /// count that is not known until the world is generated gives no guarantee about any prefix of
    /// it, while the golden angle spreads <i>every</i> prefix: the first ten of forty colours are
    /// already well apart, and so are the first ten of eighty. Lightness and saturation cycle
    /// underneath on periods coprime to nothing in particular, which separates hues that come round
    /// close to one another after many turns.</para>
    /// </summary>
    public static class WorldRegionPalette
    {
        /// <summary>A region colour, as the shader will draw it. Components are 0..1.</summary>
        public readonly record struct Swatch(float R, float G, float B, string Name);

        /// <summary>
        /// The alpha that tells the sphere's fragment shader "this rgb is already the colour". Above
        /// every category the world itself uses (1 nature, 2 water, 3 building, 4 field), so adding
        /// it took a branch and changed nothing that was already drawn.
        /// </summary>
        public const float OverlayCategory = 5.0f;

        // 0.6180339887 turns per step — the golden angle, ~137.5 degrees.
        private const float HueStep = 0.6180339887f;

        /// <summary>
        /// How far apart two bordering regions' colours must be, on <see cref="Distance"/>'s 0..1
        /// scale, for the border to read as a border. For scale: red against orange is about 0.33
        /// and red against blue about 0.75, so 0.40 is comfortably past "these are two colours" and
        /// well short of demanding opposites — which cannot be had for fifty regions at once anyway.
        /// A generated world sits around 0.45-0.55; this is the floor a script asserts against, and
        /// it is what a palette or seeding change that muddies one seed's map would trip.
        /// </summary>
        public const float MinBorderContrast = 0.40f;

        /// <summary>
        /// Builds <paramref name="count"/> distinct colours. Kept bright: these are glyphs on black,
        /// and a dark swatch on the far side of the sphere reads as unlit ground rather than as a
        /// region.
        /// </summary>
        public static Swatch[] Build(int count)
        {
            var swatches = new Swatch[Math.Max(count, 1)];
            for (int i = 0; i < swatches.Length; i++)
            {
                float hue = (i * HueStep) % 1f;
                float value = 0.74f + 0.26f * ((i % 3) / 2f);        // 0.74, 0.87, 1.00
                float sat   = 0.55f + 0.45f * ((i % 2) == 0 ? 1f : 0f); // 1.00, 0.55
                var (r, g, b) = HsvToRgb(hue, sat, value);
                swatches[i] = new Swatch(r, g, b, Name(hue, sat, value));
            }
            return swatches;
        }

        /// <summary>
        /// How far apart two colours look, 0..1. The "redmean" weighting — a cheap approximation of
        /// perceptual distance that is markedly better than plain Euclidean rgb, which rates a
        /// blue-black pair as far apart as a green-white one.
        /// </summary>
        public static float Distance(in Swatch a, in Swatch b)
        {
            float rm = (a.R + b.R) * 0.5f;
            float dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
            float d2 = (2f + rm) * dr * dr + 4f * dg * dg + (3f - rm) * db * db;
            return (float)Math.Sqrt(d2) / 3f;   // sqrt(9) is the largest the weights allow
        }

        private static (float r, float g, float b) HsvToRgb(float h, float s, float v)
        {
            float i = (float)Math.Floor(h * 6f);
            float f = h * 6f - i;
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);
            return ((int)i % 6) switch
            {
                0 => (v, t, p),
                1 => (q, v, p),
                2 => (p, v, t),
                3 => (p, q, v),
                4 => (t, p, v),
                _ => (v, p, q),
            };
        }

        private static readonly string[] HueNames =
        {
            "red", "orange", "amber", "yellow", "lime", "green",
            "emerald", "teal", "cyan", "azure", "blue", "indigo",
            "violet", "purple", "magenta", "rose",
        };

        /// <summary>
        /// A readable name, so the CLI listing says something a person can picture. The hue angle is
        /// carried along because sixteen names over a wheel of fifty-odd colours collide: two
        /// genuinely different colours can both be "bright yellow", and a reader comparing the
        /// listing against the screen would take that for a bug.
        /// </summary>
        private static string Name(float hue, float sat, float value)
        {
            string tone = HueNames[Math.Min((int)(hue * HueNames.Length), HueNames.Length - 1)];
            string qualifier = sat < 0.8f
                ? (value > 0.93f ? "pale" : "muted")
                : (value > 0.93f ? "bright" : value > 0.80f ? "" : "deep");
            string named = qualifier.Length == 0 ? tone : $"{qualifier} {tone}";
            return $"{named} {(int)Math.Round(hue * 360f)}deg";
        }
    }
}
