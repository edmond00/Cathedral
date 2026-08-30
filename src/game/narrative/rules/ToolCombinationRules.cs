using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Why a combination was refused, which decides the neutral sentence the acting modus mentis is
/// given to rewrite. Each refusal is its own piece of news: told only that "it did not work", the
/// rewrite picks whichever explanation flatters the character.
/// </summary>
public enum ToolFailureKind
{
    /// <summary>Not a refusal — the combination was accepted.</summary>
    None,
    /// <summary>The critic judged the implement unequal to the act. The only kind an LLM was asked about.</summary>
    WrongTool,
    /// <summary>The act admits of no implement at all, and this one carried no exception for it.</summary>
    Senseless,
    /// <summary>The implement is made for other work; this is not what it is for.</summary>
    NotItsPurpose,
    /// <summary>The hands have no craft in them; nothing can be used for anything.</summary>
    NoProficiency,
    /// <summary>The implement would serve, and these hands are not practised enough to make it.</summary>
    BeyondSkill,
    /// <summary>The act is a blow, and the thing held is not a weapon.</summary>
    NotAWeapon,
}

/// <summary>What combining this implement with this act comes to, before anything is asked of the critic.</summary>
public enum ToolCombinationGate
{
    /// <summary>The body has no craft in its hands at all. Nothing can be used for anything.</summary>
    NoProficiency,

    /// <summary>The implement is made for this act — the verb's own reference tool, or an authored exception.</summary>
    MadeForIt,

    /// <summary>The implement is made for other work, and this is not among it.</summary>
    NotItsPurpose,

    /// <summary>The act admits of no implement, and this one carries no exception for it.</summary>
    ExcludedVerb,

    /// <summary>The act is struck with a fighting medium, and this implement is no weapon.</summary>
    NotAWeapon,

    /// <summary>Nothing settles it here; the critic must judge the implement.</summary>
    AskTheCritic,
}

/// <summary>
/// The whole of what is decided about a combined implement <b>without an LLM</b>, in one place.
///
/// <para>Every gate but one ends the matter before a request is made, which is most of the point:
/// the critic used to be asked whether an axe helps one listen to birdsong, and the answer was a
/// second or two of inference to reach a conclusion the category already carried.</para>
///
/// <para><b>The order is load-bearing.</b> Proficiency is asked first because a body that can use
/// nothing can also not use the thing it was handed, purpose or no — and that one line is the whole
/// of the rule that hands with no craft in them (score 0, or disabled by a High-handicap wound) may
/// use no implement at all, for any verb: <c>ToolUsageProficiencyStat</c> reads
/// <c>DerivedStat.GetValue</c>, which already degrades a wound-disabled organ to its worst value.
/// A verb struck with a fighting medium is asked next, because for such a verb the question is not
/// what the implement was made for but whether it is a weapon. The implement's own purpose follows,
/// and settles the matter both ways — a thing made for one work serves that work without
/// argument and no other work at all — which is why it comes before the verb's category: the
/// exception to an excluded act and the refusal of a specialised implement are one declaration read
/// in its two directions.</para>
/// </summary>
public static class ToolCombinationRules
{
    /// <summary>
    /// What the critic's verdict must be for each band to proceed. A verdict absent from this table
    /// never passes at any band — <c>cannot_serve</c>, <c>cannot_help</c>, <c>makes_no_sense</c> are
    /// refusals about the implement rather than about the hand holding it.
    ///
    /// <para>The two trees are keyed together deliberately: <c>is_the_tool</c> and
    /// <c>clearly_helps</c> are the same judgement asked of a required and an optional verb, and a
    /// body that clears one clears the other.</para>
    /// </summary>
    private static readonly Dictionary<string, ToolProficiency> Threshold = new()
    {
        ["is_the_tool"]    = ToolProficiency.Low,
        ["clearly_helps"]  = ToolProficiency.Low,
        ["serves_well"]    = ToolProficiency.Medium,
        ["plausibly_helps"] = ToolProficiency.Medium,
        ["serves_poorly"]  = ToolProficiency.High,
        ["detoured_use"]   = ToolProficiency.High,
    };

    /// <summary>
    /// Everything decidable before the critic. <paramref name="verb"/> may be null when the action
    /// carries no preselected verb, which is treated as an ordinary optional act.
    /// </summary>
    public static ToolCombinationGate Resolve(Verb? verb, Item item, ToolProficiency proficiency)
    {
        if (proficiency == ToolProficiency.None) return ToolCombinationGate.NoProficiency;

        // A blow is struck with a fighting medium, and the set of things one strikes with is closed:
        // a weapon, or the body. So both directions are settled here without a critic call — a sword
        // is what attacking is done with whatever its MadeForVerbIds say, and no argument about a
        // lantern's heft makes it a weapon. See Verb.UsesFightingMedium.
        if (verb?.UsesFightingMedium == true)
            return item is Cathedral.Fight.IWeaponItem
                ? ToolCombinationGate.MadeForIt
                : ToolCombinationGate.NotAWeapon;

        if (IsMadeFor(verb, item))               return ToolCombinationGate.MadeForIt;
        if (IsMadeForOtherWork(verb, item))      return ToolCombinationGate.NotItsPurpose;
        if (verb?.ToolUse == ToolUsage.Excluded) return ToolCombinationGate.ExcludedVerb;

        return ToolCombinationGate.AskTheCritic;
    }

    /// <summary>
    /// Whether this implement is one this act is done with — either named by the verb as a reference
    /// tool, or naming the verb among the acts it was made for. Both are matched by id, never by
    /// display name, which is content and free to change.
    /// </summary>
    public static bool IsMadeFor(Verb? verb, Item item)
    {
        if (verb == null) return false;

        return verb.ReferenceToolIds.Contains(item.ItemId)
            || item.MadeForVerbIds.Contains(verb.VerbId);
    }

    /// <summary>
    /// Whether this implement is a single-purpose thing and <b>this is not its purpose</b>. Reading
    /// lenses cannot break ore out of a seam, and the declaration that says what they are for is the
    /// same declaration that says what they are not.
    ///
    /// <para>Asked only of items that declare a purpose at all; a general implement — a knife, a
    /// rope — declares none and is judged on its merits everywhere. Note it reads the item's own
    /// list and never the verb's <c>ReferenceToolIds</c>: being CUT's reference tool says a knife is
    /// good for cutting, not that it is good for nothing else.</para>
    /// </summary>
    public static bool IsMadeForOtherWork(Verb? verb, Item item)
        => item.MadeForVerbIds.Count > 0
        && (verb == null || !item.MadeForVerbIds.Contains(verb.VerbId));

    /// <summary>The band a critic verdict demands, or null when no band clears it.</summary>
    public static ToolProficiency? RequiredFor(string chosenId)
        => Threshold.TryGetValue(chosenId ?? "", out var band) ? band : null;

    /// <summary>
    /// Whether a body of this band may act on this verdict. A verdict the table does not know is a
    /// refusal about the implement and never clears — including any id a re-authored tree introduces
    /// without updating the table, which fails closed rather than passing silently.
    /// </summary>
    public static bool VerdictClears(string chosenId, ToolProficiency proficiency)
    {
        var required = RequiredFor(chosenId);
        return required != null && proficiency >= required;
    }
}
