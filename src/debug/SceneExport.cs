using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Settled;

namespace Cathedral.Debug;

/// <summary>
/// <c>--scene-export &lt;key&gt; [--biome b] [--ids a-b] [--out file]</c>: builds the scenes a settled
/// factory makes for a range of location ids and writes them as JSON — sections, areas and their kinds,
/// the paths and doors between them, and who lives where. Headless, no world: the build is a pure
/// function of the location id, exactly as the audits build it.
///
/// <para>For charts and diagrams about the game's content (tools/video), which must be drawn from the
/// generator rather than from somebody's reading of it.</para>
/// </summary>
public static class SceneExport
{
    public static int Run(string[] args)
    {
        string key = args.Length > 1 ? args[1] : "city";
        string? biome = Arg(args, "--biome");
        string outPath = Arg(args, "--out") ?? $"{key}_scenes.json";
        var (from, to) = ParseRange(Arg(args, "--ids") ?? "0-49");

        Func<SceneFactory>? make = (key, biome) switch
        {
            ("city", { } b) => () => new CitySceneFactory(b),
            _ => SettledSceneFactories.All.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Create,
        };
        if (make == null)
        {
            Console.Error.WriteLine($"--scene-export: no settled factory '{key}'. Keys: {string.Join(", ", SettledSceneFactories.All.Select(e => e.Key))}");
            return 1;
        }

        var scenes = new List<object>();
        for (int id = from; id <= to; id++)
        {
            var scene = make().Build(id);
            scenes.Add(Describe(id, scene));
        }

        var doc = new Dictionary<string, object?> { ["key"] = key, ["biome"] = biome ?? "default", ["ids"] = $"{from}-{to}", ["scenes"] = scenes };
        File.WriteAllText(outPath, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"--scene-export: {scenes.Count} {key} scene(s) written to {outPath}");
        return 0;
    }

    private static object Describe(int id, Scene scene)
    {
        var areas = scene.AllAreas;
        var index = areas.Select((a, i) => (a, i)).ToDictionary(t => t.a.Id, t => t.i);
        var sectionOf = scene.Sections.SelectMany(s => s.Areas.Select(a => (a.Id, s))).ToDictionary(t => t.Id, t => t.s);

        var edges = new HashSet<(int, int, string)>();
        foreach (var (fromId, tos) in scene.AreaGraph)
            foreach (var toId in tos)
                if (index.TryGetValue(fromId, out int a) && index.TryGetValue(toId, out int b))
                    edges.Add((Math.Min(a, b), Math.Max(a, b), "path"));
        foreach (var area in areas)
            foreach (var c in area.PointsOfInterest.OfType<ConnectorPointOfInterest>())
                if (index.TryGetValue(c.AreaA.Id, out int a) && index.TryGetValue(c.AreaB.Id, out int b) && a != b)
                    edges.Add((Math.Min(a, b), Math.Max(a, b), c.GetType().Name.Replace("PointOfInterest", "")));

        var npcs = scene.Npcs.Select(n =>
        {
            var home = scene.DayOf(n).Where(d => d.Area != null).GroupBy(d => d.Area!.Id)
                            .OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
            return new Dictionary<string, object?>
            {
                ["name"] = n.DisplayName,
                ["archetype"] = n.Entity.Archetype.ArchetypeId,
                ["named"] = n.Entity is Cathedral.Game.Npc.NpcEntity,
                ["home"] = home is { } h && index.TryGetValue(h, out int hi) ? hi : null,
            };
        }).ToList();

        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["sections"] = scene.Sections.Select(s => new Dictionary<string, object?>
            {
                ["name"] = s.DisplayName, ["interior"] = s.IsInterior, ["areas"] = s.Areas.Count,
            }).ToList(),
            ["areas"] = areas.Select((a, i) => new Dictionary<string, object?>
            {
                ["i"] = i,
                ["name"] = a.DisplayName,
                ["kind"] = a.GetType().Name,
                ["section"] = sectionOf.TryGetValue(a.Id, out var s) ? s.DisplayName : null,
                ["interior"] = sectionOf.TryGetValue(a.Id, out var s2) && s2.IsInterior,
                ["private"] = a.IsPrivate,
                ["pois"] = a.PointsOfInterest.Where(p => p is not ConnectorPointOfInterest).Select(p => p.DisplayName).ToList(),
            }).ToList(),
            ["edges"] = edges.Select(e => new object[] { e.Item1, e.Item2, e.Item3 }).ToList(),
            ["npcs"] = npcs,
        };
    }

    private static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static (int, int) ParseRange(string s)
    {
        var p = s.Split('-');
        int a = int.Parse(p[0]);
        return (a, p.Length > 1 ? int.Parse(p[1]) : a);
    }
}
