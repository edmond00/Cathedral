// Tags observation texts with Catalyst and writes the game's exact keyword candidates, for tools/keywords.
// Usage: kwtag bin/Debug/net8.0-windows/catalyst-models <in.jsonl> <out.jsonl>
using System.Text.Json;
using Catalyst;
using Mosaik.Core;
using Cathedral.Game.Narrative;

var store = args[0]; var inp = args[1]; var outp = args[2];
await NounExtractor.InitializeAsync(store);
Catalyst.Models.English.Register();
var pipe = await Pipeline.ForAsync(Language.English);
using var w = new StreamWriter(outp);
foreach (var line in File.ReadLines(inp))
{
    var o = JsonDocument.Parse(line).RootElement;
    var text = o.GetProperty("text").GetString()!;
    var doc = new Document(text, Language.English);
    pipe.ProcessSingle(doc);
    var toks = new List<object>();
    foreach (var span in doc) foreach (var t in span)
        toks.Add(new { s = t.Value, l = t.Lemma, p = t.POS.ToString() });
    var cands = NounExtractor.ExtractKeywordCandidates(text).Select(c => new[] { c.Surface, c.Lemma }).ToList();
    w.WriteLine(JsonSerializer.Serialize(new { id = o.GetProperty("id").GetInt32(), toks, cands }));
}
Console.WriteLine("done");
