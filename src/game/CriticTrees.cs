using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// All context needed to build the item-use critic preamble: location, node, epoch, and goal.
/// </summary>
public class CriticContext
{
    public NarrationNode Node { get; }
    public WorldContext WorldContext { get; }
    public int LocationId { get; }
    public string GoalDescription { get; }

    public CriticContext(NarrationNode node, WorldContext worldContext, int locationId, string goalDescription)
    {
        Node = node;
        WorldContext = worldContext;
        LocationId = locationId;
        GoalDescription = goalDescription;
    }

    /// <summary>
    /// When set, this item context string ("ItemName (description)") is appended to
    /// every critic preamble so the judgment is framed around the item in use.
    /// </summary>
    public string? CombinedItemContext { get; set; } = null;

    /// <summary>
    /// Builds the shared preamble injected at the top of every item-use question.
    /// Written in third person so the critic judges as an exterior observer.
    /// </summary>
    public string BuildPreamble()
    {
        string worldDesc = WorldContext.GenerateContextDescription(LocationId);
        string nodeDesc  = Node.GenerateEnrichedContextDescription(LocationId);
        string goalLine  = GoalDescription.Length > 0
            ? $"The character's goal is to {GoalDescription}."
            : "";
        string itemLine  = CombinedItemContext != null
            ? $"The character is using: {CombinedItemContext}."
            : "";
        return string.Join("\n", new[]
        {
            "Setting: a medieval world, pre-industrial, no firearms or modern technology.",
            $"The scene: a {worldDesc}. The character is {nodeDesc}.",
            goalLine,
            itemLine
        }.Where(s => s.Length > 0));
    }
}

/// <summary>
/// Factory for the Item-Use Critic's evaluation trees. The critic LLM is now scoped to judging
/// whether an item helps accomplish an action; all other adjudication
/// (plausibility, difficulty, wounds, witnesses, threats) has moved to the modus-mentis persona-fit
/// enum and to deterministic coded rules. A couple of pure helpers used by the difficulty/movement
/// display also live here.
/// </summary>
public static class CriticTrees
{
    #region Difficulty / movement helpers (no LLM)

    /// <summary>
    /// Returns true if the action's leading verb is a movement/exit verb
    /// (enter, go, leave, move). Used to decide whether Continue exits to world view
    /// or restarts observation in the current scene.
    /// </summary>
    public static bool IsMovementVerb(string actionText)
    {
        var verb = actionText.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.ToLowerInvariant() ?? "";
        return verb is "enter" or "go" or "leave" or "move";
    }

    /// <summary>
    /// Converts a 1–10 difficulty level to a 0.0–1.0 score used for success probability.
    /// Level 1 → 0.0, level 10 → 1.0.
    /// </summary>
    public static double DifficultyLevelToScore(int level) =>
        (Math.Clamp(level, 1, 10) - 1) / 9.0;

    #endregion

    #region Item Appropriateness Tree

    /// <summary>
    /// Asks the LLM critic whether a combined item can plausibly help realise an action, using a
    /// neutral goal-based phrasing.
    ///
    /// <para>The three non-failure choices are <b>degrees, not a pass</b>: which of them the acting
    /// body may act upon is decided afterwards by its hands, in
    /// <c>ToolCombinationRules.VerdictClears</c> — clearly at Low, plausibly at Medium, detoured at
    /// High. The critic judges the implement and knows nothing of who is holding it, which is what
    /// keeps this question answerable at all.</para>
    ///
    /// <para><b>A new choice id must be added to that threshold table</b>, or it fails closed and
    /// no body will ever clear it.</para>
    /// </summary>
    public static CriticNode BuildItemAppropriatenessTree(string goalText, string itemName, CriticContext context)
    {
        return new CriticNode(
            name: "ItemAppropriateness",
            question: $"{context.BuildPreamble()}\n\nThe character wants to {goalText}.\nThey are holding: {itemName}.\n\nCompared to attempting this with bare hands, does {itemName} provide a meaningful advantage for this action?",
            choices: new List<CriticChoice>
            {
                new("clearly_helps",    "the item provides a clear, direct advantage over bare hands"),
                new("plausibly_helps",  "the item offers a real but modest advantage over bare hands"),
                new("detoured_use",     "the item could help through creative use, though barely more than bare hands"),
                new("cannot_help",      "the item offers no meaningful advantage over bare hands for this action", isFailure: true, errorMessage: "That item cannot help with this action."),
                new("makes_no_sense",   "using this item here makes no sense compared to bare hands",             isFailure: true, errorMessage: "Using that item here makes no sense."),
            });
    }

    #endregion

    #region Tool Substitution Tree

    /// <summary>
    /// Asks the LLM critic whether the item the player actually combined can serve as the tool a
    /// tool-gated verb calls for. Used instead of <see cref="BuildItemAppropriatenessTree"/> whenever
    /// the verb declares <c>ReferenceToolIds</c> — the question there is "is this better than bare
    /// hands", which is the wrong question when bare hands are not on the table at all.
    ///
    /// <para>The reference tool is named in the question so the critic judges against a standard
    /// rather than against nothing. Naming it is the whole difference: "could a rock hammer do the
    /// work of a pickaxe" is answerable, where "does a rock hammer help" is not.</para>
    ///
    /// <para>The first three choices are degrees rather than a pass, and are keyed to the same
    /// bands as the neutral tree's — <c>is_the_tool</c> with <c>clearly_helps</c> at Low,
    /// <c>serves_well</c> with <c>plausibly_helps</c> at Medium, <c>serves_poorly</c> with
    /// <c>detoured_use</c> at High. <c>serves_poorly</c> is deliberately not a refusal: improvising
    /// a tool should be allowed to a practised hand and then punished by the dice, not forbidden
    /// outright. See <c>ToolCombinationRules</c>.</para>
    /// </summary>
    public static CriticNode BuildToolSubstitutionTree(string goalText, string referenceToolPhrase,
                                                       string itemName, CriticContext context)
    {
        return new CriticNode(
            name: "ToolSubstitution",
            question: $"{context.BuildPreamble()}\n\nThe character wants to {goalText}.\nThis work is normally done with {referenceToolPhrase}.\nThey are holding: {itemName}.\n\nCan {itemName} do the work of {referenceToolPhrase} here?",
            choices: new List<CriticChoice>
            {
                new("is_the_tool",    "the item is that tool, or near enough to be the same thing"),
                new("serves_well",    "the item is a different thing but would do the work properly"),
                new("serves_poorly",  "the item could be made to work, clumsily and with difficulty"),
                new("cannot_serve",   "the item cannot do this work, however it is handled",
                    isFailure: true, errorMessage: "That is not the tool this work needs."),
                new("makes_no_sense", "using this item for this work makes no sense at all",
                    isFailure: true, errorMessage: "That item is no use for this at all."),
            });
    }

    /// <summary>
    /// "a pickaxe" / "a fishing rod or a net" — the or-list of articled tool names for the critic
    /// question. Ids the registry does not know are printed as themselves rather than dropped, so a
    /// bad id shows up in the critic trace instead of silently narrowing the question.
    /// </summary>
    public static string ToolPhrase(IReadOnlyList<string> toolIds)
    {
        var names = toolIds
            .Select(id => ItemRegistry.Instance.All.FirstOrDefault(i => i.ItemId == id)?.WithArticle()
                          ?? id.Replace('_', ' '))
            .ToList();

        if (names.Count == 0) return "a proper tool";
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.Take(names.Count - 1)) + " or " + names[^1];
    }

    #endregion

    // There was an Item Consumption Tree here, asked after every successful combination: "was this
    // item used up in the process?". It is gone, because the question no longer has two answers.
    //
    // What may be combined with an act is a tool, a weapon, or an item declaring itself made for
    // some single act — and none of those is spent by being used. A pick is not eaten by a seam. So
    // the tree spent a request per successful combination to arrive at "no" nearly every time, and
    // the times it did not were the interesting ones only in the sense that they were wrong: it
    // destroyed the knife a carcass had just been opened with often enough to matter.
    //
    // It was worse than a wasted request under --playground, where every critic choice is drawn at
    // random: a script combining a tool twice lost it to the first use about half the time, which
    // is a test failing for a reason with nothing to do with what it tested.
    //
    // Should a consumable ever become combinable, this comes back — but it comes back as a property
    // of the item, answerable without asking anybody.
}
