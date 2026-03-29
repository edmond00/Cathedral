using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Factory for the standard Critic evaluation trees.
/// All trees use enum-choice nodes: the LLM picks one option from a constrained list.
/// </summary>
public static class CriticTrees
{
    #region Plausibility Tree

    /// <summary>
    /// Builds the plausibility tree — a linear chain of yes/no questions.
    /// Choosing "no" at any node stops the tree immediately with a rejection message.
    /// </summary>
    public static CriticNode BuildPlausibilityTree(string actionText, string contextDescription)
    {
        var ctx = $"Context: {contextDescription}\nThe {Config.Narrative.PlayerName} wants to: \"{actionText}\"";

        CriticNode YesNo(string name, string questionBody, string errorMessage)
        {
            return new CriticNode(
                name: name,
                question: $"{ctx}\n\n{questionBody}",
                choices: new List<CriticChoice>
                {
                    new("yes", "this is true"),
                    new("no", "this is false or unclear",
                        isFailure: true, errorMessage: errorMessage)
                });
        }

        var notContradictory = YesNo(
            "NotContradictory",
            "Is this action consistent with what just happened and doesn't contradict recent events?",
            "This contradicts what just occurred");

        var hasRequiredElements = YesNo(
            "HasRequiredElements",
            "Are the objects, people, or elements needed for this action available or present?",
            "You don't have what's needed to do this");
        hasRequiredElements.WithBranch("yes", notContradictory);

        var contextAppropriate = YesNo(
            "ContextAppropriate",
            "Does this action make sense given the current location and situation?",
            "This action doesn't fit the current situation");
        contextAppropriate.WithBranch("yes", hasRequiredElements);

        var reasonableTimeframe = YesNo(
            "ReasonableTimeframe",
            "Is this action short enough to be completed in less than one hour?",
            "This action would take too long to complete");
        reasonableTimeframe.WithBranch("yes", contextAppropriate);

        var physicallyPossible = YesNo(
            "PhysicallyPossible",
            "Is this action physically possible for a human to attempt?",
            "This action is physically impossible");
        physicallyPossible.WithBranch("yes", reasonableTimeframe);

        return physicallyPossible;
    }

    #endregion

    #region Difficulty Tree

    /// <summary>
    /// Builds the difficulty tree — a single node asking the LLM to rate difficulty in 5 levels.
    /// </summary>
    public static CriticNode BuildDifficultyTree(string actionText, string contextDescription)
    {
        return new CriticNode(
            name: "Difficulty",
            question: $"Context: {contextDescription}\nThe {Config.Narrative.PlayerName} wants to: \"{actionText}\"\n\nHow difficult is this action to perform?",
            choices: new List<CriticChoice>
            {
                new("very_easy", "trivial, no risk, anyone could do it"),
                new("easy",      "simple, low effort, minor skill required"),
                new("moderate",  "requires care or sustained effort"),
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
            "easy"      => 0.3,
            "moderate"  => 0.5,
            "hard"      => 0.7,
            "very_hard" => 0.9,
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
    /// </summary>
    public static CriticNode BuildFailureOutcomeTree(string actionText, string contextDescription)
    {
        var ctx = $"Context: {contextDescription}\nThe {Config.Narrative.PlayerName} failed at: \"{actionText}\"";

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

        // Level 2 — body part / organ selection
        var bodyPartChoices = targetToWounds.Keys
            .OrderBy(id => id)
            .Select(id => new CriticChoice(id: id, description: GetTargetDisplayName(id)))
            .ToList();

        var bodyPartNode = new CriticNode(
            name: "BodyPartAffected",
            question: $"{ctx}\n\nWhich part of the body would be wounded by this failure?",
            choices: bodyPartChoices);

        foreach (var (targetId, woundNode) in woundTypeNodes)
            bodyPartNode.WithBranch(targetId, woundNode);
        // Any target without a specific wound node (shouldn't happen) leads to null (terminal)

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
    /// </summary>
    public static Wound? GetWoundFromResult(CriticTreeResult result)
    {
        // Tree has 3 nodes when a wound was chosen: WoundCheck → BodyPartAffected → WoundType_X
        if (result.Trace.Count < 3) return null;

        var woundIdStr = result.FinalChosenId;
        if (woundIdStr.Length == 1 && WoundRegistry.All.TryGetValue(woundIdStr[0], out var wound))
            return wound;

        return null;
    }

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

    #endregion
}
