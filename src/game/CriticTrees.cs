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
/// whether an item helps accomplish an action and whether it is consumed; all other adjudication
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
    /// neutral goal-based phrasing. "clearly_helps", "plausibly_helps", and "detoured_use" pass.
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

    #region Item Consumption Tree

    /// <summary>
    /// Asks the LLM critic whether an item was consumed while performing an action.
    /// "definitely_consumed" and "probably_consumed" map to consumed = true.
    /// </summary>
    public static CriticNode BuildItemConsumptionTree(string actionText, string itemContext, CriticContext context)
    {
        return new CriticNode(
            name: "ItemConsumption",
            question: $"{context.BuildPreamble()}\n\nThe {Config.Narrative.PlayerName} performed: \"{actionText}\"\nUsing: {itemContext}.\n\nWas {itemContext} consumed, destroyed, or used up in the process?",
            choices: new List<CriticChoice>
            {
                new("definitely_consumed",    "the item was certainly used up or destroyed"),
                new("probably_consumed",      "the item was very likely consumed or rendered unusable"),
                new("possibly_consumed",      "the item might have been partially consumed"),
                new("probably_not_consumed",  "the item was probably not consumed"),
                new("definitely_not_consumed","the item was not consumed and is still intact"),
            });
    }

    /// <summary>Returns true when the critic decided the item was consumed.</summary>
    public static bool IsItemConsumedFromResult(CriticTreeResult result) =>
        result.FinalChosenId is "definitely_consumed" or "probably_consumed";

    #endregion
}
