using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Expands <c>{scope:field}</c> tokens embedded in a neutral replica/intent against the live
/// <see cref="DialogueContext"/>, so authored lines can pull in conversational context ("the reeve of
/// the field", "an old man", "a friend"). Expansion runs <b>before</b> the LLM rewrite, and name
/// fields yield placeholder names so the LLM never sees a real name.
///
/// <para>
/// The allowed field set is predetermined and documented below; unknown tokens are left verbatim and
/// logged. Add a field with a single entry in <see cref="Fields"/>.
/// </para>
/// </summary>
public static class DialogueTemplate
{
    private static readonly Regex FieldPattern = new(@"\{([a-zA-Z]+):([a-zA-Z_]+)\}", RegexOptions.Compiled);

    /// <summary>Allowed <c>"scope:field" → resolver</c> map. One line per new field.</summary>
    private static readonly Dictionary<string, Func<DialogueContext, string>> Fields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["you:name"]        = c => c.Names.Placeholder("you") ?? c.Pc.DisplayName,
            ["npc:name"]        = c => c.Names.Placeholder("npc") ?? c.Npc.DisplayName,
            ["npc:job"]         = c => c.Npc.Archetype.BuildRoleClause(LocationNoun(c)),
            ["npc:role"]        = c => c.Npc.Archetype.RoleNoun,
            ["npc:description"] = c => NpcLabelResolver.DescribePerson(c.Npc),
            ["npc:relation"]    = c => RelationWord(c.Relation),
            ["npc:location"]    = c => c.World?.DisplayName ?? "",
        };

    /// <summary>Replaces every recognised <c>{scope:field}</c> token in <paramref name="text"/>.</summary>
    public static string Expand(string? text, DialogueContext ctx)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        return FieldPattern.Replace(text!, m =>
        {
            string key = $"{m.Groups[1].Value}:{m.Groups[2].Value}";
            if (Fields.TryGetValue(key, out var resolver))
            {
                try { return resolver(ctx); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"DialogueTemplate: field '{{{key}}}' failed: {ex.Message}");
                    return "";
                }
            }
            Console.Error.WriteLine($"DialogueTemplate: unknown field '{{{key}}}' left verbatim.");
            return m.Value;
        });
    }

    private static string LocationNoun(DialogueContext c) => (c.World?.DisplayName ?? "").ToLowerInvariant();

    /// <summary>A plain relation noun phrase for <c>{npc:relation}</c> (distinct from the UI labels).</summary>
    private static string RelationWord(AffinityLevel level) => level switch
    {
        AffinityLevel.Stranger             => "a stranger",
        AffinityLevel.AnnoyingAcquaintance => "an unwelcome acquaintance",
        AffinityLevel.DistantAcquaintance  => "a distant acquaintance",
        AffinityLevel.CloseAcquaintance    => "an acquaintance",
        AffinityLevel.DistantFriend        => "a friend",
        AffinityLevel.CloseFriend          => "a close friend",
        AffinityLevel.Suspicious           => "an uneasy acquaintance",
        _                                  => "someone",
    };
}
