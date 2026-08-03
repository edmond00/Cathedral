using System.Linq;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks a tool-gated verb (mine, dig, fish, cut wood, break) attempted with bare hands.
/// Deterministic and absolute, like the other coded rules: no amount of resolve gets a seam out of a
/// rock face with fingernails, so this is settled before the LLM is asked anything.
///
/// <para>Whether the item the player <i>did</i> combine is good enough is a different question and a
/// far less clear-cut one — a rock hammer for a pickaxe, a cloak for a net — so it is left to the
/// item-use critic (<c>CriticTrees.BuildToolSubstitutionTree</c>). This rule only catches the case
/// where there is nothing to judge.</para>
///
/// <para>The refusal names the tools by display name rather than by id, because it is re-voiced in
/// the acting modus mentis and read by the player as their own thought.</para>
/// </summary>
public class RequiredToolRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        var verb = ctx.Action.PreselectedOutcome?.VerbView.Verb;
        if (verb == null || !verb.RequiresTool)      return ActionRuleResult.Pass();
        if (ctx.Action.CombinedItem != null)         return ActionRuleResult.Pass();

        return ActionRuleResult.Fail($"I would need {ToolPhrase(verb.ReferenceToolIds)} for that.");
    }

    /// <summary>
    /// "a pickaxe" / "a rod or a net" — the reference tools as an or-list of articled display names,
    /// falling back to the raw ids for anything the registry does not know (which <c>--verb-audit</c>
    /// reports separately, since such a verb can never be performed).
    /// </summary>
    private static string ToolPhrase(System.Collections.Generic.IReadOnlyList<string> toolIds)
    {
        var names = toolIds
            .Select(id => ItemRegistry.Instance.All.FirstOrDefault(i => i.ItemId == id))
            .Select((item, index) => item != null ? item.WithArticle() : toolIds[index].Replace('_', ' '))
            .ToList();

        if (names.Count == 0) return "a tool";
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.Take(names.Count - 1)) + " or " + names[^1];
    }
}
