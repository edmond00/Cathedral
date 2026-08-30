using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Work;
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
///
/// <para><b>The field set</b>, by what it lets an authored line do:</para>
/// <list type="table">
///   <item><term>who they are</term><description>
///     <c>{npc:name}</c>, <c>{npc:role}</c>, <c>{npc:job}</c>, <c>{npc:description}</c>,
///     <c>{npc:age}</c>, <c>{npc:relation}</c>, <c>{npc:location}</c>, <c>{you:name}</c>
///   </description></item>
///   <item><term>how they present themselves</term><description>
///     <c>{npc:introduction}</c> — a first-meeting self-description in their own terms;
///     <c>{npc:workplace}</c>, <c>{npc:craft}</c>, <c>{npc:labour}</c>
///   </description></item>
///   <item><term>what they think</term><description>
///     <c>{npc:opinion_weather}</c> and one token per <see cref="DialogueTopic"/> — the archetype's
///     own view, falling back to a neutral villager's answer
///   </description></item>
///   <item><term>what they trade</term><description>
///     <c>{npc:sells}</c>, <c>{npc:buys}</c>, <c>{npc:wares}</c> (named examples from their own
///     catalogue), <c>{you:goods}</c> (what the player is actually carrying that they buy)
///   </description></item>
///   <item><term>what work is on offer</term><description>
///     <c>{npc:job_title}</c>, <c>{npc:job_offer}</c> (with article), <c>{npc:job_pay}</c> —
///     the job the REQUEST_JOB verb picked, so the dialogue names the same post the action did
///   </description></item>
/// </list>
/// </summary>
public static class DialogueTemplate
{
    private static readonly Regex FieldPattern = new(@"\{([a-zA-Z]+):([a-zA-Z_]+)\}", RegexOptions.Compiled);

    /// <summary>Prefix of the per-topic opinion tokens, e.g. <c>{npc:opinion_harvest}</c>.</summary>
    private const string OpinionPrefix = "npc:opinion_";

    /// <summary>Allowed <c>"scope:field" → resolver</c> map. One line per new field.</summary>
    private static readonly Dictionary<string, Func<DialogueContext, string>> Fields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ── Identity ──────────────────────────────────────────────────────
            ["you:name"]         = c => c.Names.Placeholder("you") ?? c.Pc.DisplayName,
            ["npc:name"]         = c => c.Names.Placeholder("npc") ?? c.Npc.DisplayName,
            ["npc:job"]          = c => c.Npc.Archetype.BuildRoleClause(LocationNoun(c)),
            ["npc:role"]         = c => c.Npc.Archetype.RoleNoun,
            ["npc:description"]  = c => NpcLabelResolver.DescribePerson(c.Npc),
            ["npc:relation"]     = c => RelationWord(c.Relation),
            ["npc:location"]     = c => c.World?.DisplayName ?? "",
            ["npc:age"]          = c => AgeWord(c.Npc),

            // ── Self-presentation ─────────────────────────────────────────────
            // Introduction and labour read off the NPC, not the archetype: a personality trait can
            // override how this particular person presents themselves and what their day is like.
            ["npc:introduction"] = c => c.Npc.SelfIntroduction,
            ["npc:workplace"]    = c => c.Npc.Archetype.Workplace,
            ["npc:craft"]        = c => c.Npc.Archetype.Craft,
            ["npc:labour"]       = c => c.Npc.DailyLabour,

            // ── Trade ─────────────────────────────────────────────────────────
            ["npc:sells"]        = c => c.Npc.SellTag?.Label() ?? "what little I have",
            ["npc:buys"]         = c => c.Npc.BuyTag?.Label()  ?? "little enough",
            ["npc:wares"]        = c => Wares(c),
            ["you:goods"]        = c => CarriedGoods(c),

            // ── A third person ────────────────────────────────────────────────
            // The one someone is being asked ABOUT, not the one being spoken to. Every field falls
            // back to a vague noun phrase rather than an empty string: a tree that names a third
            // party in one branch may not have one in another, and "the man" reads better than a
            // hole in the sentence.
            ["third:name"]     = c => c.ThirdParty == null ? "them"
                                    : c.Names.Placeholder("third") ?? c.ThirdParty.DisplayName,
            ["third:role"]     = c => c.ThirdParty?.Archetype.RoleNoun ?? "someone",
            ["third:job"]      = c => c.ThirdParty?.Archetype.BuildRoleClause(LocationNoun(c)) ?? "someone here",
            ["third:craft"]    = c => c.ThirdParty?.Archetype.Craft ?? "their work",
            ["third:relation"] = c => ThirdRelation(c),

            // ── Work ──────────────────────────────────────────────────────────
            ["npc:job_title"]    = c => c.Npc.PendingJobOffer?.Title ?? "a hand",
            ["npc:job_offer"]    = c => c.Npc.PendingJobOffer?.WithArticle() ?? "a hand",
            ["npc:job_pay"]      = c => JobPay(c),
        };

    /// <summary>
    /// How the speaker stands to the third party — what an apprentice calls their blacksmith.
    /// Read off the archetypes rather than off an affinity table, because this is a standing
    /// relationship in the world, not a feeling either of them has about the player.
    /// </summary>
    private static string ThirdRelation(DialogueContext c)
    {
        if (c.ThirdParty == null) return "someone I know";

        var speaker = c.Npc.Archetype as NamedNpcArchetype;
        return speaker != null && speaker.CanIntroduceToArchetypes.Contains(c.ThirdParty.Archetype.ArchetypeId)
            ? speaker.IntroductionRelation
            : "someone I know";
    }

    /// <summary>Replaces every recognised <c>{scope:field}</c> token in <paramref name="text"/>.</summary>
    public static string Expand(string? text, DialogueContext ctx)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        return FieldPattern.Replace(text!, m =>
        {
            string key = $"{m.Groups[1].Value}:{m.Groups[2].Value}";
            var resolver = Resolve(key);
            if (resolver != null)
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

    /// <summary>
    /// Whether <paramref name="key"/> (in <c>scope:field</c> form) is a field this expander knows.
    /// Used by the dialogue audit to catch a typo'd token before it ever reaches a player.
    /// </summary>
    public static bool IsKnownField(string key) => Resolve(key) != null;

    /// <summary>
    /// The resolver for a <c>scope:field</c> key: a table entry, or — for the one open-ended family —
    /// a per-topic opinion lookup parsed out of the <c>opinion_&lt;topic&gt;</c> suffix.
    /// </summary>
    private static Func<DialogueContext, string>? Resolve(string key)
    {
        if (Fields.TryGetValue(key, out var resolver)) return resolver;

        // Opinions resolve per person, not per trade: a trait can give this individual a different
        // view on a topic from the rest of their craft.
        if (key.StartsWith(OpinionPrefix, StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<DialogueTopic>(key[OpinionPrefix.Length..], ignoreCase: true, out var topic))
            return c => c.Npc.OpinionOn(topic);

        return null;
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

    /// <summary>
    /// A bare age word for <c>{npc:age}</c> — "young", "grown", "old". Same bands as
    /// <see cref="NpcLabelResolver.DescribePerson"/>, without the gendered noun, so a line can say
    /// "I'm too {npc:age} for that" without committing to man or woman.
    /// </summary>
    private static string AgeWord(NpcEntity npc)
    {
        double years = npc.Combatant.GetAgeDays() / LifetimeStat.DaysPerYear;
        return years < 18 ? "young" : years < 50 ? "grown" : "old";
    }

    /// <summary>
    /// Two or three things actually in this NPC's sell catalogue, named: "a sickle, a hinge and a
    /// pair of shears". Deterministic per NPC (the catalogue is seeded by npc id), so a merchant
    /// always boasts of the same goods. Falls back to the tag label when they sell nothing.
    /// </summary>
    private static string Wares(DialogueContext c)
    {
        var offers = c.Npc.BuyCatalog?.Offers;
        if (offers == null || offers.Count == 0) return c.Npc.SellTag?.Label() ?? "odds and ends";

        // The offer's name, so a drink is boasted of as "a bottle of ale" rather than bare "ale".
        var names = offers.Take(3).Select(o => o.WithArticle()).ToList();
        return JoinNaturally(names);
    }

    /// <summary>
    /// What the player is carrying that this NPC would actually buy — named, so the offer is
    /// concrete instead of "some goods". Falls back to the NPC's buy-tag label when the player has
    /// nothing of interest (the tree still reads correctly; the trade menu will simply be empty).
    /// </summary>
    private static string CarriedGoods(DialogueContext c)
    {
        if (c.Npc.BuyTag is not { } tag) return "what I'm carrying";

        // GetAllItems(), not Inventory: TryAcquireItem never writes to the overflow list, so
        // reading Inventory here made this token always fall back to the bare tag label.
        var names = c.Pc.GetAllItems()
            .Where(i => i.Tags.Contains(tag))
            .Select(i => i.WithArticle())
            .Distinct()
            .Take(3)
            .ToList();

        return names.Count > 0 ? JoinNaturally(names) : tag.Label();
    }

    /// <summary>
    /// The offered job's pay, phrased the way a villager would state it: "a copper a day", "a
    /// copper for every twelve days of it". Reads <see cref="Job.DaysPerCoin"/> straight — the same
    /// number the work menu will pay out on. The denomination is read from the job rather than
    /// assumed, though every wage is copper today, as every price is.
    /// </summary>
    private static string JobPay(DialogueContext c)
    {
        var job = c.Npc.PendingJobOffer;
        if (job == null) return "whatever the work is worth";

        string coin = job.PayCoin.ToString().ToLowerInvariant();
        int days = Math.Max(1, (int)Math.Round(job.DaysPerCoin));
        return days == 1 ? $"a {coin} a day" : $"a {coin} for every {days} days of it";
    }

    /// <summary>"a, b and c" — the Oxford-free list join these lines want.</summary>
    private static string JoinNaturally(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}",
    };
}
