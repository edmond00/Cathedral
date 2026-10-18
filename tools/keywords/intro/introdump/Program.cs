// Builds real scenes from the game's factories and writes, per sampled area and period, what the area
// introduction would be given: context, description, period phrase, the observable pool, and the
// neutral text itself (NeutralNarration.AreaIntroduction).
//   dotnet run --project tools/keywords/intro/introdump -- scenes.jsonl [per-factory]
//   dotnet run --project tools/keywords/intro/introdump -- link gen.jsonl gen_tags.jsonl out.jsonl
// The second form rebuilds each case's scene and runs the game's IntroductionKeywordLinker on it, so
// the C# rule can be scored with ibench.py against the same labels as its Python twin.
using System.Text.Json;
using Cathedral.Game.Scene;
using Cathedral.Game.Narrative;
using Cathedral.Game;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Plain; using Cathedral.Game.Scene.Forest; using Cathedral.Game.Scene.Cave; using Cathedral.Game.Scene.Mountain; using Cathedral.Game.Scene.Peak; using Cathedral.Game.Scene.Coast;

if (args[0] == "link") { LinkMode.Run(args); return; }
var outPath = args[0];
int perFactory = args.Length > 1 ? int.Parse(args[1]) : 4;
var rng = new Random(20261016);

var factories = new List<(string Key, Func<SceneFactory> Make)>
{
    ("plain", () => new PlainSceneFactory()), ("forest", () => new ForestSceneFactory()),
    ("cave", () => new CaveSceneFactory()), ("mountain", () => new MountainSceneFactory()),
    ("peak", () => new PeakSceneFactory()), ("coast", () => new CoastSceneFactory()),
};
foreach (var c in Cathedral.Game.Scene.Shared.ClimateSceneFactories.All) factories.Add((c.Biome, c.Create));
foreach (var s in Cathedral.Game.Scene.Settled.SettledSceneFactories.All) factories.Add((s.Key, s.Create));

using var w = new StreamWriter(outPath);
int n = 0;
foreach (var (key, make) in factories)
{
    for (int k = 0; k < perFactory; k++)
    {
        int id = rng.Next(1, 20000);
        Scene scene;
        try { scene = make().Build(id); } catch (Exception ex) { Console.Error.WriteLine($"{key} {id}: {ex.Message}"); continue; }
        var areas = scene.AllAreas.ToList();
        var area = areas[rng.Next(areas.Count)];
        var period = TimePeriodExtensions.Random(rng);
        try
        {
            var view = scene.View(new PoV(area, period));
            var node = SceneViewAdapter.ToNarrationNode(view);
            var pool = node.GetAllDirectConcreteOutcomes().OfType<ObservationObject>()
                .GroupBy(o => o.NeutralName.ToLowerInvariant()).Select(g => g.First()).ToList();
            foreach (var o in pool) (o as INpcContextLabelStampable)?.StampContextLabel(null, null, id);
            string? desc = null;
            if (area.Descriptions.Count > 0)
                desc = area.Descriptions[SceneViewAdapter.DescriptionRng(area, id).Next(area.Descriptions.Count)];
            var sights = pool.OrderBy(_ => rng.Next()).Take(5).ToList();
            var neutral = NeutralNarration.AreaIntroduction(area.ContextDescription, period.Phrase(), desc, sights.Select(o => o.NeutralPhrase).ToList());
            w.WriteLine(JsonSerializer.Serialize(new
            {
                neutral, sights = sights.Select(o => pool.IndexOf(o)),
                key, id, area = area.DisplayName, context = area.ContextDescription, description = desc,
                period = period.Phrase(),
                pool = pool.Select(o => new
                {
                    phrase = o.NeutralPhrase, name = o.NeutralName, lemma = o.ReferenceLemma,
                    person = o is SyntheticNpcObservationObject, type = o.GetType().Name,
                }),
            }));
            n++;
        }
        catch (Exception ex) { Console.Error.WriteLine($"{key} {id} {area.DisplayName}: {ex.Message}"); }
    }
}
Console.WriteLine($"{n} areas from {factories.Count} factories");

static class LinkMode
{
    // link <cases.jsonl> <tags.jsonl> <out.jsonl>: rebuilds each case's scene and runs the game's linker.
    public static void Run(string[] args)
    {
        var factories = new Dictionary<string, Func<SceneFactory>>(StringComparer.OrdinalIgnoreCase)
        {
            ["plain"] = () => new PlainSceneFactory(), ["forest"] = () => new ForestSceneFactory(),
            ["cave"] = () => new CaveSceneFactory(), ["mountain"] = () => new MountainSceneFactory(),
            ["peak"] = () => new PeakSceneFactory(), ["coast"] = () => new CoastSceneFactory(),
        };
        foreach (var c in Cathedral.Game.Scene.Shared.ClimateSceneFactories.All) factories[c.Biome] = c.Create;
        foreach (var st in Cathedral.Game.Scene.Settled.SettledSceneFactories.All) factories[st.Key] = st.Create;
        var periods = Enum.GetValues<TimePeriod>().ToDictionary(p => p.Phrase());
        var tags = File.ReadLines(args[2]).Select(l => JsonDocument.Parse(l).RootElement).ToDictionary(e => e.GetProperty("id").GetInt32());
        using var w = new StreamWriter(args[3]);
        int i = 0;
        foreach (var line in File.ReadLines(args[1]))
        {
            var c = JsonDocument.Parse(line).RootElement; int n = i++;
            var scene = factories[c.GetProperty("key").GetString()!]().Build(c.GetProperty("id").GetInt32());
            var area = scene.AllAreas.First(a => a.DisplayName == c.GetProperty("area").GetString());
            int id = c.GetProperty("id").GetInt32();
            var node = SceneViewAdapter.ToNarrationNode(scene.View(new PoV(area, periods[c.GetProperty("period").GetString()!])));
            var pool = node.GetAllDirectConcreteOutcomes().OfType<ObservationObject>()
                .GroupBy(o => o.NeutralName.ToLowerInvariant()).Select(g => g.First()).ToList();
            foreach (var o in pool) (o as INpcContextLabelStampable)?.StampContextLabel(null, null, id);
            var names = c.GetProperty("pool").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
            var cands = tags[n].GetProperty("cands").EnumerateArray().Select(x => (x[0].GetString()!, x[1].GetString()!)).ToList();
            var links = IntroductionKeywordLinker.Link(c.GetProperty("text").GetString()!, area.ContextDescription, pool.Cast<NarrativeAnchor>().ToList(), cands);
            w.WriteLine(JsonSerializer.Serialize(new { n, links = links.Select(l => new object[] { l.Keyword, names.IndexOf(((ObservationObject)l.Anchor).NeutralName) }) }));
        }
    }
}
