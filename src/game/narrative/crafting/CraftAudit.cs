using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Crafting;

/// <summary>
/// <c>--craft-audit</c>: the crafting content read as a whole — what the materials are held by, what each
/// trade makes, where its station stands and whether its material can be had there. Headless: it builds
/// every location factory over a few ids, as the will and verb audits do.
///
/// <para>Fails (✗) on dead content, the kind that fails silently: a station kind no factory builds, a
/// trade whose station stands nowhere, a station material no item holds, a recipe a trade could never pay
/// for (its cost above what the largest item of the station's material gives back is fine — the pool adds
/// up — but a recipe of a material nothing holds is not). Notes (·) a raw material no sampled place
/// offers on the ground or for sale.</para>
///
/// <para>With a path after the flag it also writes the numbers as JSON, for reports.</para>
/// </summary>
public static class CraftAudit
{
    private const int Samples = 6;

    public static string BuildReport(string? jsonPath = null)
    {
        var sb      = new StringBuilder();
        var fails   = new List<string>();
        var trades  = CraftingModusMentis.All.OrderBy(t => CraftingStations.Noun(t.Station)).ThenBy(t => t.ModusMentisId).ToList();
        var items   = ItemRegistry.Instance.All.ToList();

        sb.AppendLine("=== CRAFT AUDIT ===");
        sb.AppendLine($"{trades.Count} crafting modi mentis, {CraftingStations.All.Count} station kinds, "
                    + $"{CraftMaterials.All.Length} materials, {items.Count(i => ItemMaterials.Of(i).Count > 0)} of {items.Count} items hold a material");
        sb.AppendLine();

        // ── Materials ─────────────────────────────────────────────────────────
        sb.AppendLine("── materials ──");
        sb.AppendLine($"  {"material",-9} {"items",5} {"units",6}  {"stations",-40} trades");
        var materialRows = new List<object>();
        foreach (var m in CraftMaterials.All)
        {
            var holders  = items.Where(i => ItemMaterials.Holds(i, m)).ToList();
            var stations = CraftingStations.All.Where(s => CraftingStations.MaterialOf(s) == m).ToList();
            var workers  = trades.Where(t => t.Material == m).ToList();
            int units    = holders.Sum(ItemMaterials.Units);
            sb.AppendLine($"  {m.Label(),-9} {holders.Count,5} {units,6}  {string.Join(", ", stations.Select(CraftingStations.Noun)),-40} "
                        + string.Join(", ", workers.Select(w => w.ModusMentisId)));
            if (stations.Count > 0 && holders.Count == 0)
                fails.Add($"✗ {m.Label()} is worked at {string.Join(", ", stations.Select(CraftingStations.Noun))} but no item holds it");
            materialRows.Add(new { material = m.Label(), refined = m.IsRefined(), from = m.RefinedFrom()?.Label(), items = holders.Count, units,
                                   stations = stations.Select(CraftingStations.Noun).ToList(), trades = workers.Select(w => w.ModusMentisId).ToList(),
                                   sample = holders.OrderByDescending(ItemMaterials.Units).Take(6).Select(i => i.DisplayName).ToList() });
        }
        sb.AppendLine();

        // ── Trades ────────────────────────────────────────────────────────────
        sb.AppendLine("── trades ──");
        var tradeRows = new List<object>();
        foreach (var t in trades)
        {
            var recipes = t.RecipePrototypes.ToList();
            sb.AppendLine($"  {t.ModusMentisId,-16} {CraftingStations.Noun(t.Station),-15} + {t.ToolPrototype?.DisplayName ?? "?",-15} "
                        + $"works {t.Material.Label(),-8} → {string.Join(", ", recipes.Select(r => $"{r.DisplayName} ({CraftingModusMentis.Cost(r)})"))}");
            tradeRows.Add(new
            {
                id = t.ModusMentisId, name = t.DisplayName, station = CraftingStations.Noun(t.Station), material = t.Material.Label(),
                tool = t.ToolPrototype?.DisplayName, organs = t.Organs, places = t.WillPlaces.Select(p => p.Name.Replace("Area", "")).ToList(),
                recipes = recipes.Select(r => new { name = r.DisplayName, cost = CraftingModusMentis.Cost(r), category = r.Category.ToString(),
                                                    materials = ItemMaterials.Of(r).Select(x => x.Label()).ToList(), price = r.PriceReference }).ToList(),
                refines = recipes.Any(r => ItemMaterials.Of(r).Any(x => x.IsRefined() && x.RefinedFrom() == t.Material)),
            });
        }
        int craftable = items.Count(i => trades.Any(t => t.Recipes.Contains(i.GetType())));
        sb.AppendLine($"  {craftable} distinct items can be made, {trades.Sum(t => t.Recipes.Count)} recipes in all");
        sb.AppendLine();

        // ── Where the stations stand, and what is there to work ──────────────
        var stationKinds   = new Dictionary<Type, HashSet<string>>();
        var stationCount   = new Dictionary<Type, int>();
        var stationFree    = new Dictionary<Type, int>();
        var groundUnits    = CraftMaterials.All.ToDictionary(m => m, _ => 0);
        var groundKinds    = CraftMaterials.All.ToDictionary(m => m, _ => new HashSet<string>());
        var sellerKinds    = CraftMaterials.All.ToDictionary(m => m, _ => new HashSet<string>());
        var npcTrades      = new Dictionary<string, int>();
        // Per item id: how often it lies about, in how many kinds of place, how many people carry it and sell it.
        var itemGround     = new Dictionary<string, int>();
        var itemGroundKinds= new Dictionary<string, HashSet<string>>();
        var itemCarried    = new Dictionary<string, int>();
        var itemSold       = new Dictionary<string, HashSet<string>>();
        void Count(Dictionary<string, int> d, string id) => d[id] = d.GetValueOrDefault(id) + 1;
        void Mark(Dictionary<string, HashSet<string>> d, string id, string kind) => (d.TryGetValue(id, out var h) ? h : d[id] = new()).Add(kind);
        int builds = 0, npcs = 0;
        var kindsWithStation = new HashSet<string>();
        var allKinds = new HashSet<string>();

        foreach (var (label, build, samples) in VerbAudit.Factories())
        for (int id = 1; id <= Math.Min(Samples, samples); id++)
        {
            Scene.Scene scene;
            try { scene = build(id); } catch { continue; }
            builds++;
            allKinds.Add(label);
            foreach (var area in scene.AllAreas)
            foreach (var poi in area.PointsOfInterest)
            {
                if (CraftingStations.MaterialOf(poi) != null)
                {
                    var type = poi.GetType();
                    (stationKinds.TryGetValue(type, out var set) ? set : stationKinds[type] = new()).Add(label);
                    stationCount[type] = stationCount.GetValueOrDefault(type) + 1;
                    kindsWithStation.Add(label);
                    var section = scene.SectionOf(area);
                    if (section == null || CraftingAccess.MasterOf(scene, section) == null)
                        stationFree[type] = stationFree.GetValueOrDefault(type) + 1;
                }
                foreach (var ie in poi.Items)
                {
                    Count(itemGround, ie.Item.ItemId);
                    Mark(itemGroundKinds, ie.Item.ItemId, label);
                }
                foreach (var ie in poi.Items)
                    foreach (var m in ItemMaterials.Of(ie.Item))
                    {
                        groundUnits[m] += ItemMaterials.Units(ie.Item);
                        groundKinds[m].Add(label);
                    }
            }
            foreach (var sn in scene.Npcs)
            {
                if (sn.Entity is not NpcEntity npc) continue;
                npcs++;
                foreach (var held in npc.Combatant.GetAllItems()) Count(itemCarried, held.ItemId);
                foreach (var t in npc.Combatant.ModiMentis.OfType<CraftingModusMentis>())
                    npcTrades[t.ModusMentisId] = npcTrades.GetValueOrDefault(t.ModusMentisId) + 1;
                try
                {
                    foreach (var offer in npc.BuyCatalog?.Offers ?? (IReadOnlyList<Npc.Trade.TradeOffer>)Array.Empty<Npc.Trade.TradeOffer>())
                    {
                        Mark(itemSold, offer.Prototype.ItemId, label);
                        foreach (var m in ItemMaterials.Of(offer.Prototype)) sellerKinds[m].Add(label);
                    }
                }
                catch { /* a catalogue that cannot be built outside play sells nothing here */ }
            }
        }

        sb.AppendLine($"── stations ({builds} builds, {allKinds.Count} location kinds, {Samples} ids each) ──");
        var stationRows = new List<object>();
        foreach (var s in CraftingStations.All)
        {
            int n = stationCount.GetValueOrDefault(s), free = stationFree.GetValueOrDefault(s);
            var kinds = stationKinds.TryGetValue(s, out var k) ? k.OrderBy(x => x).ToList() : new List<string>();
            var mark = n == 0 ? "✗" : "✓";
            sb.AppendLine($"  {mark} {CraftingStations.Noun(s),-15} {n,4} built, {free,3} free  in {kinds.Count,2} kinds: {string.Join(", ", kinds.Take(12))}{(kinds.Count > 12 ? ", …" : "")}");
            if (n == 0) fails.Add($"✗ station {CraftingStations.Noun(s)} is built by no factory");
            stationRows.Add(new { station = CraftingStations.Noun(s), material = CraftingStations.MaterialOf(s)!.Value.Label(), built = n, free, kinds });
        }
        foreach (var t in trades.Where(t => stationCount.GetValueOrDefault(t.Station) == 0))
            fails.Add($"✗ {t.ModusMentisId} works a {CraftingStations.Noun(t.Station)}, which stands nowhere");
        sb.AppendLine($"  {kindsWithStation.Count} of {allKinds.Count} location kinds hold at least one station");
        sb.AppendLine();

        sb.AppendLine("── what there is to work ──");
        sb.AppendLine($"  {"material",-9} {"ground units",12} {"kinds with it",14} {"kinds selling",14}");
        var supplyRows = new List<object>();
        foreach (var m in CraftMaterials.All)
        {
            bool worked = CraftingStations.All.Any(s => CraftingStations.MaterialOf(s) == m);
            var note = worked && groundKinds[m].Count == 0 && sellerKinds[m].Count == 0 ? "  · offered nowhere sampled" : "";
            sb.AppendLine($"  {m.Label(),-9} {groundUnits[m],12} {groundKinds[m].Count,14} {sellerKinds[m].Count,14}{note}");
            supplyRows.Add(new { material = m.Label(), groundUnits = groundUnits[m], groundKinds = groundKinds[m].Count, sellerKinds = sellerKinds[m].Count });
        }
        sb.AppendLine();

        sb.AppendLine($"── people who know a trade ({npcs} people built) ──");
        foreach (var (id, n) in npcTrades.OrderByDescending(kv => kv.Value))
            sb.AppendLine($"  {id,-16} {n,4}");
        sb.AppendLine();

        // ── What an item is for, beyond its matter ───────────────────────────
        // An item with none of these offices can only be sold, or broken down at a station. That is right
        // for ore, timber and fleece; for a padlock or a book it is a thing with a use in life and none here.
        var inert = items.Where(i => Uses(i).Count == 0).ToList();
        sb.AppendLine($"── items with no use but their price and their matter ({inert.Count} of {items.Count}) ──");
        foreach (var g in inert.GroupBy(i => i.Tags.Count > 0 ? i.Tags[0].ToString() : "untagged").OrderByDescending(g => g.Count()))
            sb.AppendLine($"  · {g.Key,-12} {g.Count(),3}  {string.Join(", ", g.Select(i => i.DisplayName).Take(10))}{(g.Count() > 10 ? ", …" : "")}");
        sb.AppendLine();

        if (fails.Count == 0) sb.AppendLine("No failures — every station stands somewhere and every worked material is held by something.");
        else
        {
            sb.AppendLine($"--- {fails.Count} FAILURE(S) ---");
            foreach (var f in fails) sb.AppendLine("  " + f);
        }

        if (jsonPath != null)
            System.IO.File.WriteAllText(jsonPath, JsonSerializer.Serialize(new
            {
                builds, kinds = allKinds.Count, kindsWithStation = kindsWithStation.Count, npcs, craftable, items = items.Count,
                holding = items.Count(i => ItemMaterials.Of(i).Count > 0),
                materials = materialRows, trades = tradeRows, stations = stationRows, supply = supplyRows,
                npcTrades = npcTrades.OrderByDescending(kv => kv.Value).Select(kv => new { id = kv.Key, n = kv.Value }).ToList(),
                table = items.Where(i => ItemMaterials.Of(i).Count > 0).Select(i => new
                {
                    name = i.DisplayName, category = i.Category.ToString(), weight = i.Weight.ToString(), units = ItemMaterials.Units(i),
                    materials = ItemMaterials.Of(i).Select(x => x.Label()).ToList(),
                    madeBy = trades.Where(t => t.Recipes.Contains(i.GetType())).Select(t => t.ModusMentisId).ToList(),
                }).ToList(),
                census = items.Select(i => new
                {
                    id = i.ItemId, name = i.DisplayName, category = i.Category.ToString(), price = i.PriceReference,
                    tags = i.Tags.Select(t => t.ToString()).ToList(), uses = Uses(i),
                    materials = ItemMaterials.Of(i).Select(x => x.Label()).ToList(),
                    units = ItemMaterials.Of(i).Count > 0 ? ItemMaterials.Units(i) : 0,
                    madeBy = trades.Where(t => t.Recipes.Contains(i.GetType())).Select(t => t.ModusMentisId).ToList(),
                    toolOf = trades.Where(t => t.Tool == i.GetType()).Select(t => t.ModusMentisId).ToList(),
                    ground = itemGround.GetValueOrDefault(i.ItemId),
                    groundKinds = itemGroundKinds.TryGetValue(i.ItemId, out var gk) ? gk.Count : 0,
                    carried = itemCarried.GetValueOrDefault(i.ItemId),
                    soldIn = itemSold.TryGetValue(i.ItemId, out var sk) ? sk.Count : 0,
                }).ToList(),
            }, new JsonSerializerOptions { WriteIndented = true }));

        return sb.ToString();
    }

    /// <summary>
    /// The offices an item holds beside being sold or broken down: fought with, worn, eaten, holding
    /// things, combined with an act (as a tool, or by an act it was made for), or a trade's implement.
    /// </summary>
    public static List<string> Uses(Item i)
    {
        var u = new List<string>();
        if (i.Category == ItemCategory.Weapon)   u.Add("weapon");
        if (i.Category == ItemCategory.Wearing)  u.Add("worn");
        if (i.Category == ItemCategory.Consumable) u.Add("eaten");
        if (i.Category == ItemCategory.Container || i is IContainer) u.Add("container");
        if (i.Category == ItemCategory.Tool && Rules.ToolCombinationRules.IsCombinable(i)) u.Add("tool");
        if (i.MadeForVerbIds.Count > 0)          u.Add("made-for");
        if (CraftingModusMentis.IsCraftTool(i))  u.Add("trade-tool");
        return u;
    }
}
