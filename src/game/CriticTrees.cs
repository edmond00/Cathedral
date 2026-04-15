using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// All context needed to build a full critic preamble: location, node, epoch, and goal.
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
    /// Builds the shared preamble injected at the top of every critic question.
    /// Written in third person so the critic judges as an exterior observer.
    /// </summary>
    /// <summary>
    /// When set, this item context string ("ItemName (description)") is appended to
    /// every critic preamble so that tool-missing checks don't misfire when a proper
    /// item is being used.
    /// </summary>
    public string? CombinedItemContext { get; set; } = null;

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
/// Factory for the standard Critic evaluation trees.
/// All trees use enum-choice nodes: the LLM picks one option from a constrained list.
/// </summary>
public static class CriticTrees
{
    #region Plausibility Tree

    /// <summary>
    /// Builds the plausibility tree from <see cref="Config.PlausibilityQuestions.Questions"/>.
    /// Each question becomes one independent node. All nodes are chained in order so that
    /// continueOnFailure mode visits every question even when one fails.
    /// </summary>
    public static CriticNode BuildPlausibilityTree(string actionText, CriticContext context)
    {
        var preamble = $"{context.BuildPreamble()}\n\nThe {Config.Narrative.PlayerName} wants to: \"{actionText}\"";
        var questions = Config.PlausibilityQuestions.Questions;

        // Build one CriticNode per question
        var nodes = questions.Select(q =>
        {
            var choices = q.Choices
                .Select(c => new CriticChoice(c.Id, c.Description, c.IsFailure, c.ErrorMessage))
                .ToList();
            return new CriticNode(
                name: q.Name,
                question: $"{preamble}\n\n{q.Text}",
                choices: choices);
        }).ToList();

        // Chain: each node's pass-choices branch leads to the next node
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var current = nodes[i];
            var next = nodes[i + 1];
            foreach (var choice in questions[i].Choices.Where(c => !c.IsFailure))
                current.WithBranch(choice.Id, next);
        }

        return nodes[0];
    }

    #endregion

    #region Difficulty Tree

    /// <summary>
    /// Builds the difficulty tree — a single node asking the LLM to rate difficulty in 4 levels.
    /// </summary>
    public static CriticNode BuildDifficultyTree(string actionText, CriticContext context)
    {
        return new CriticNode(
            name: "Difficulty",
            question: $"{context.BuildPreamble()}\n\nThe {Config.Narrative.PlayerName} wants to: \"{actionText}\"\n\nHow difficult is this action to perform?",
            choices: new List<CriticChoice>
            {
                new("very_easy", "trivial, no risk, anyone could do it"),
                new("easy",      "simple, low effort, minor skill required"),
                new("hard",      "difficult, significant risk or skill required"),
                new("very_hard", "extreme difficulty or serious danger"),
            });
    }

    /// <summary>Maps the chosen difficulty id to a 0.0–1.0 score.</summary>
    public static double CalculateDifficultyFromResult(CriticTreeResult result)
    {
        if (result.Trace.Count == 0) return 0.5;
        return result.FinalChosenId switch
        {
            "very_easy" => 0.1,
            "easy"      => 0.4,
            "hard"      => 0.7,
            "very_hard" => 1.0,
            _           => 0.5
        };
    }

    /// <summary>Converts a 0.0–1.0 difficulty score to a 1–10 integer scale.</summary>
    public static int DifficultyToScale(double difficulty) =>
        Math.Clamp((int)Math.Ceiling(difficulty * 10), 1, 10);

    #endregion

    #region Failure Outcome Tree

    /// <summary>
    /// Builds the failure outcome tree.
    /// Level 1 — Does the failure cause a wound? (yes/no)
    /// Level 2 — Which body part / organ is damaged? (enum of all wound targets)
    /// Level 3 — What type of wound? (enum of wounds for that target)
    /// <para>
    /// <paramref name="wildcardCandidates"/> adds body-part / organ-part locations that accept
    /// wildcard (Low-handicap) wounds. For targets already present in the named-wound list the
    /// wildcard choices are appended; otherwise a new choice + node is created.
    /// </para>
    /// </summary>
    public static CriticNode BuildFailureOutcomeTree(
        string actionText,
        CriticContext context,
        IReadOnlyList<WildcardCandidate>? wildcardCandidates = null)
    {
        var ctx = $"{context.BuildPreamble()}\n\nThe {Config.Narrative.PlayerName} failed at: \"{actionText}\"";

        // Build a mapping: targetId → list of wounds targeting it (excluding wildcards)
        var targetToWounds = WoundRegistry.All.Values
            .Where(w => w.TargetKind != WoundTargetKind.Wildcard)
            .GroupBy(w => w.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Level 3 — one wound-type node per target
        var woundTypeNodes = new Dictionary<string, CriticNode>();
        foreach (var (targetId, wounds) in targetToWounds)
        {
            var woundChoices = wounds
                .Select(w => new CriticChoice(
                    id: w.WoundId.ToString(),
                    description: $"{w.WoundName} ({w.Handicap} severity)"))
                .ToList();

            woundTypeNodes[targetId] = new CriticNode(
                name: $"WoundType_{targetId}",
                question: $"{ctx}\n\nA wound has been inflicted on '{targetId.Replace('_', ' ')}'. What type of wound is it?",
                choices: woundChoices);
        }

        // Inject wildcard wound choices for candidate locations
        if (wildcardCandidates != null && wildcardCandidates.Count > 0)
        {
            var wildcardChoices = WoundRegistry.WildcardTemplates
                .Select(w => new CriticChoice(
                    id: w.WoundId.ToString(),
                    description: $"{w.WoundName} (Low severity — superficial)"))
                .ToList();

            foreach (var candidate in wildcardCandidates)
            {
                if (woundTypeNodes.TryGetValue(candidate.TargetId, out var existingNode))
                {
                    // Append wildcard choices to the existing named-wound node
                    existingNode.Choices.AddRange(wildcardChoices);
                }
                else
                {
                    // New target only reachable via wildcard wounds
                    woundTypeNodes[candidate.TargetId] = new CriticNode(
                        name: $"WoundType_{candidate.TargetId}",
                        question: $"{ctx}\n\nA wound has been inflicted on '{candidate.DisplayName}'. What type of wound is it?",
                        choices: new List<CriticChoice>(wildcardChoices));
                }
            }
        }

        // Level 2 — body part / organ selection
        // Start from named-wound targets, then append any wildcard-only candidates
        var namedTargetIds = new HashSet<string>(targetToWounds.Keys);
        var bodyPartChoices = targetToWounds.Keys
            .OrderBy(id => id)
            .Select(id => new CriticChoice(id: id, description: GetTargetDisplayName(id)))
            .ToList();

        if (wildcardCandidates != null)
        {
            foreach (var candidate in wildcardCandidates.Where(c => !namedTargetIds.Contains(c.TargetId)))
                bodyPartChoices.Add(new CriticChoice(id: candidate.TargetId, description: candidate.DisplayName));
        }

        var bodyPartNode = new CriticNode(
            name: "BodyPartAffected",
            question: $"{ctx}\n\nWhich part of the body would be wounded by this failure?",
            choices: bodyPartChoices);

        foreach (var (targetId, woundNode) in woundTypeNodes)
            bodyPartNode.WithBranch(targetId, woundNode);

        // Level 1 — does this cause a wound at all?
        var woundCheck = new CriticNode(
            name: "WoundCheck",
            question: $"{ctx}\n\nCan failing this action cause a physical wound?",
            choices: new List<CriticChoice>
            {
                new("yes", "the failure can cause a physical wound"),
                new("no",  "the failure causes no physical wound")
            });
        woundCheck.WithBranch("yes", bodyPartNode);
        // "no" branch → null (terminal, no wound)

        return woundCheck;
    }

    /// <summary>
    /// Extracts the wound selected at the end of the failure outcome tree.
    /// Returns null if the tree determined no wound occurs.
    /// <para>
    /// When <paramref name="wildcardCandidates"/> is supplied and the selected wound is a
    /// wildcard type, a fresh instance is returned (not the registry singleton) with
    /// <see cref="Wound.WildcardZoneHint"/> set so BodyArtViewer can constrain placement.
    /// </para>
    /// </summary>
    public static Wound? GetWoundFromResult(
        CriticTreeResult result,
        IReadOnlyList<WildcardCandidate>? wildcardCandidates = null)
    {
        // Tree has 3 nodes when a wound was chosen: WoundCheck → BodyPartAffected → WoundType_X
        if (result.Trace.Count < 3) return null;

        var woundIdStr = result.FinalChosenId;
        if (woundIdStr.Length != 1) return null;
        if (!WoundRegistry.All.TryGetValue(woundIdStr[0], out var wound)) return null;

        // For wildcard wounds, create a fresh instance and attach the zone hint
        if (wound.TargetKind == WoundTargetKind.Wildcard && wildcardCandidates != null)
        {
            string chosenTargetId = result.Trace[1].ChosenId;
            var candidate = wildcardCandidates.FirstOrDefault(c => c.TargetId == chosenTargetId);
            if (candidate != null)
            {
                var freshWound = System.Activator.CreateInstance(wound.GetType()) as WildcardWound;
                if (freshWound != null)
                {
                    freshWound.WildcardZoneHint = candidate.ZoneHint;
                    return freshWound;
                }
            }
        }

        return wound;
    }

    #endregion

    #region Item Appropriateness Tree

    /// <summary>
    /// Asks the LLM critic whether a combined item can plausibly help realise an action.
    /// Only "clearly_helps" and "plausibly_helps" are passing choices.
    /// </summary>
    public static CriticNode BuildItemAppropriatenessTree(string actionText, string itemContext, CriticContext context)
    {
        return new CriticNode(
            name: "ItemAppropriateness",
            question: $"{context.BuildPreamble()}\n\nThe {Config.Narrative.PlayerName} wants to: \"{actionText}\"\nThe character is holding: {itemContext}.\n\nCan {itemContext} plausibly help to realise this action?",
            choices: new List<CriticChoice>
            {
                new("clearly_helps",    "the item directly enables or clearly assists the action"),
                new("plausibly_helps",  "the item could plausibly assist in some way"),
                new("unlikely_to_help", "the item is unlikely to be useful here", isFailure: true, errorMessage: "That item is unlikely to help with this."),
                new("cannot_help",      "the item cannot help with this action",  isFailure: true, errorMessage: "That item cannot help with this action."),
                new("makes_no_sense",   "using this item here makes no sense",    isFailure: true, errorMessage: "Using that item here makes no sense."),
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

    #region Under-Threat Opportunity Tree

    /// <summary>
    /// Asks the LLM critic how likely it is that a nearby enemy seizes an opportunity
    /// to attack during or after the attempted action.
    ///
    /// Used twice in the pipeline:
    ///   - During evaluation (step 2, visual only): if the enemy is right there, does the action
    ///     give them an opening? (informational — does not fail plausibility)
    ///   - During failure resolution (step 4b): action failed — does the enemy now attack?
    ///     "high" or "very_high" → FightTriggered.
    /// </summary>
    /// <param name="actionText">The action the character attempted.</param>
    /// <param name="threat">Who the enemy is and their proximity.</param>
    /// <param name="context">Standard critic context (setting, scene, goal).</param>
    /// <param name="actionFailed">
    ///   When true, the question notes that the action just failed.
    /// </param>
    public static CriticNode BuildUnderThreatTree(
        string actionText,
        Cathedral.Game.Scene.ThreatContext threat,
        CriticContext context,
        bool actionFailed = false)
    {
        var preamble = context.BuildPreamble();
        var failedNote = actionFailed ? " The action failed." : "";
        var threatName = threat.Threat?.DisplayName ?? "an enemy";

        string question = threat.Level switch
        {
            Cathedral.Game.Scene.ThreatLevel.Visual =>
                $"{preamble}\n\n" +
                $"The {Config.Narrative.PlayerName} attempted to: \"{actionText}\".{failedNote}\n" +
                $"Under the direct threat of {threatName} who is right here.\n\n" +
                $"What are the chances this action gives {threatName} an opportunity to harm you?",

            Cathedral.Game.Scene.ThreatLevel.Audio =>
                $"{preamble}\n\n" +
                $"The {Config.Narrative.PlayerName} attempted to: \"{actionText}\".{failedNote}\n" +
                $"Under the threat of {threatName} a few steps away.\n\n" +
                $"What are the chances the noise of this action draws {threatName} to attack?",

            _ => throw new System.ArgumentException("Cannot build under-threat tree with no threat.")
        };

        return new CriticNode(
            name: actionFailed ? "UnderThreatOpportunityFailure" : "UnderThreatOpportunity",
            question: question,
            choices: new System.Collections.Generic.List<CriticChoice>
            {
                new("very_low",  "the enemy gets no meaningful opportunity"),
                new("low",       "the enemy is unlikely to seize an opening"),
                new("high",      "the enemy is likely to take advantage and attack"),
                new("very_high", "the enemy will almost certainly attack"),
            });
    }

    /// <summary>
    /// Returns true when the critic concluded the enemy seized an opportunity to attack.
    /// "high" and "very_high" are treated as triggered.
    /// </summary>
    public static bool IsOpportunityFromResult(CriticTreeResult result) =>
        result.FinalChosenId is "high" or "very_high";

    #endregion

    #region Witness Detection Tree

    /// <summary>
    /// Asks the LLM critic how likely it is that a nearby witness perceived an illegal action.
    /// Used twice in the pipeline:
    ///   - During evaluation (step 2): baseline detection probability.
    ///   - During failure resolution (step 4b): re-asked with failure context; the answer
    ///     determines whether to trigger the "caught red-handed" confrontation dialogue.
    /// </summary>
    /// <param name="actionText">The action the character attempted.</param>
    /// <param name="witnessContext">Who is present and how they can perceive the action.</param>
    /// <param name="context">Standard critic context (setting, scene, goal).</param>
    /// <param name="actionFailed">
    ///   When true, the question notes that the action failed (failure is typically noisier).
    /// </param>
    public static CriticNode BuildWitnessDetectionTree(
        string actionText,
        Cathedral.Game.Scene.WitnessContext witnessContext,
        CriticContext context,
        bool actionFailed = false)
    {
        var preamble = context.BuildPreamble();
        var failedNote = actionFailed ? " The action failed, which may have produced more noise or commotion." : "";
        var witnessDesc = witnessContext.ToPromptDescription();

        string question = witnessContext.Type switch
        {
            Cathedral.Game.Scene.WitnessType.Audio =>
                $"{preamble}\n\n" +
                $"The {Config.Narrative.PlayerName} attempted to: \"{actionText}\".{failedNote}\n" +
                $"{witnessDesc}\n\n" +
                "What are the chances this action was heard by the witness?",

            Cathedral.Game.Scene.WitnessType.Visual =>
                $"{preamble}\n\n" +
                $"The {Config.Narrative.PlayerName} attempted to: \"{actionText}\".{failedNote}\n" +
                $"{witnessDesc}\n\n" +
                "What are the chances this action was seen or heard by the witness?",

            _ => throw new System.ArgumentException("Cannot build witness detection tree with no witness.")
        };

        return new CriticNode(
            name: actionFailed ? "WitnessDetectionFailure" : "WitnessDetection",
            question: question,
            choices: new System.Collections.Generic.List<CriticChoice>
            {
                new("very_low",  "the witness almost certainly did not notice anything"),
                new("low",       "the witness probably did not notice"),
                new("high",      "the witness likely noticed something"),
                new("very_high", "the witness almost certainly detected the action"),
            });
    }

    /// <summary>
    /// Returns true when the critic concluded the witness detected the action.
    /// "high" and "very_high" are treated as detected.
    /// </summary>
    public static bool IsWitnessDetectedFromResult(CriticTreeResult result) =>
        result.FinalChosenId is "high" or "very_high";

    #endregion

    private static string GetTargetDisplayName(string targetId) => targetId switch
    {
        "encephalon"  => "skull / brain (encephalon)",
        "visage"      => "face (visage)",
        "left_eye"    => "left eye",
        "right_eye"   => "right eye",
        "left_ear"    => "left ear",
        "right_ear"   => "right ear",
        "nose"        => "nose",
        "teeths"      => "teeth",
        "tongue"      => "tongue",
        "backbone"    => "backbone / spine",
        "pulmones"    => "lungs / ribs (pulmones)",
        "viscera"     => "viscera / intestines",
        "paunch"      => "stomach (paunch)",
        "genitories"  => "genitals",
        "left_arm"    => "left arm",
        "right_arm"   => "right arm",
        "left_hand"   => "left hand",
        "right_hand"  => "right hand",
        "left_leg"    => "left leg",
        "right_leg"   => "right leg",
        "left_foot"   => "left foot",
        "right_foot"  => "right foot",
        _             => targetId.Replace('_', ' ')
    };
}
