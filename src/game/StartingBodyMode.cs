using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// <c>--organs</c> and <c>--humors</c>: the protagonist's body, set as a starting condition instead of
/// rolled and allocated. What a scripted run (a test, a <c>--record</c> take) needs in order to stage
/// a character who can actually do the thing being shown — a sharp encephalon for a long thinking
/// phase, strong viscera for spending humors on a roll, a queue of Voluptas to rescue a failed one.
///
/// <para>Applied when creation is accepted, in two halves around the derivations that read the
/// scores: <see cref="ApplyOrgans"/> before memory and humor queues are rebuilt from them, and
/// <see cref="ApplyHumors"/> after, since rebuilding the queues would overwrite whatever was put in
/// them. Both inert when their flag was not passed.</para>
/// </summary>
public static class StartingBodyMode
{
    /// <summary>Sets organ part scores: by part id, organ id or body part id (every part under it), or "all".</summary>
    public static void ApplyOrgans(PartyMember member)
    {
        var spec = Config.Debug.Organs;
        if (spec == null) return;

        var organs = member.BodyParts.SelectMany(bp => bp.Organs).ToList();
        var unknown = new List<string>();
        foreach (var (target, score) in spec)
        {
            var parts = target == "all"
                ? organs.SelectMany(o => o.Parts).ToList()
                : member.BodyParts.Where(bp => bp.Id.Equals(target, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(bp => bp.Organs).SelectMany(o => o.Parts)
                        .Concat(organs.Where(o => o.Id.Equals(target, StringComparison.OrdinalIgnoreCase)).SelectMany(o => o.Parts))
                        .Concat(organs.SelectMany(o => o.Parts).Where(p => p.Id.Equals(target, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
            if (parts.Count == 0) { unknown.Add(target); continue; }
            foreach (var p in parts) p.Score = Math.Clamp(score, 0, p.MaxScore);
        }

        Console.WriteLine($"*** --organs: {string.Join(", ", spec.Select(s => $"{s.Target}={s.Score}"))} ***");
        if (unknown.Count > 0)
            Console.Error.WriteLine($"*** --organs: no organ or organ part named {string.Join(", ", unknown)}. "
                + $"Body parts: {string.Join(", ", member.BodyParts.Select(bp => bp.Id))}; organs: {string.Join(", ", organs.Select(o => o.Id))}; parts: {string.Join(", ", organs.SelectMany(o => o.Parts).Select(p => p.Id))} ***");
    }

    /// <summary>Fills humor queues with the named humors (cycled through the queue when several).</summary>
    public static void ApplyHumors(PartyMember member)
    {
        var spec = Config.Debug.Humors;
        if (spec == null) return;

        var humorTypes = typeof(BodyHumor).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BodyHumor)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .ToDictionary(t => t.Name.Replace("Humor", "").ToLowerInvariant(), t => t);

        var queues = new Dictionary<string, HumorQueue>
        {
            ["paunch"] = member.HumorQueues.Paunch, ["hepar"] = member.HumorQueues.Hepar,
            ["spleen"] = member.HumorQueues.Spleen, ["pulmones"] = member.HumorQueues.Pulmones,
        };

        // "all" first, so a named queue can override it.
        foreach (var group in spec.GroupBy(s => s.Queue).OrderBy(g => g.Key == "all" ? 0 : 1))
        {
            var humors = new List<BodyHumor>();
            foreach (var (_, name) in group)
            {
                if (humorTypes.TryGetValue(name.ToLowerInvariant(), out var t)) humors.Add((BodyHumor)Activator.CreateInstance(t)!);
                else Console.Error.WriteLine($"*** --humors: no humor '{name}'. Humors: {string.Join(", ", humorTypes.Keys.OrderBy(k => k))} ***");
            }
            if (humors.Count == 0) continue;

            var targets = group.Key == "all" ? queues.Values.ToList()
                        : queues.TryGetValue(group.Key, out var q) ? new List<HumorQueue> { q } : new List<HumorQueue>();
            if (targets.Count == 0)
            {
                Console.Error.WriteLine($"*** --humors: no queue '{group.Key}' (paunch, hepar, spleen, pulmones, all) ***");
                continue;
            }
            foreach (var queue in targets)
            {
                if (humors.Count == 1) { queue.FillWith(humors[0]); continue; }
                for (int i = 0; i < HumorQueue.Capacity; i++) queue.SetAt(i, humors[i % humors.Count]);
            }
        }
        Console.WriteLine($"*** --humors: {string.Join(", ", spec.Select(s => $"{s.Queue}={s.Humor}"))} ***");
    }
}
