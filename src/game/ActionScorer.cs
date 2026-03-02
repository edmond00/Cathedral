using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Scores and filters actions using the Critic evaluator with tree-based evaluation.
/// Uses binary decision trees for action-skill coherence, action-consequence plausibility, 
/// context coherence, location coherence, and action specificity.
/// </summary>
public class ActionScorer
{
    private readonly CriticEvaluator _critic;
    
    public ActionScorer(CriticEvaluator critic)
    {
        _critic = critic ?? throw new ArgumentNullException(nameof(critic));
    }
    
    /// <summary>
    /// Scores all actions using the Critic evaluator with tree-based evaluation.
    /// Each action is evaluated against a decision tree of checks.
    /// Returns actions sorted by total score (highest first).
    /// </summary>
    public async Task<List<ScoredAction>> ScoreActionsAsync(
        List<ParsedAction> actions,
        PlayerAction? previousAction = null,
        string? currentSublocation = null,
        LocationBlueprint? blueprint = null)
    {
        var scoredActions = new List<ScoredAction>();
        
        Console.WriteLine($"\n🔍 Critic Tree Evaluation: Evaluating {actions.Count} actions...");
        
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var sw = Stopwatch.StartNew();
            
            Console.WriteLine($"\n  [{i + 1}/{actions.Count}] Evaluating: {TruncateText(action.ActionText, 50)}");
            
            // Build the evaluation tree for this action
            var evaluationTree = BuildActionEvaluationTree(
                action, 
                previousAction, 
                currentSublocation, 
                blueprint);
            
            // Evaluate the tree
            var treeResult = await _critic.EvaluateTreeAsync(evaluationTree);
            
            sw.Stop();
            
            // Calculate scores from tree result
            var scoredAction = new ScoredAction
            {
                Action = action,
                TreeResult = treeResult,
                TotalScore = CalculateScoreFromTreeResult(treeResult),
                EvaluationDurationMs = sw.Elapsed.TotalMilliseconds
            };
            
            // Extract individual scores from trace for backward compatibility
            ExtractIndividualScores(scoredAction, treeResult);
            
            scoredActions.Add(scoredAction);
            
            Console.WriteLine($"    ✓ Total Score: {scoredAction.TotalScore:F3} (failures: {treeResult.FailureCount}, duration: {sw.Elapsed.TotalMilliseconds:F0}ms)");
        }
        
        // Sort by total score descending (best actions first)
        var sorted = scoredActions.OrderByDescending(a => a.TotalScore).ToList();
        
        Console.WriteLine($"\n✓ Critic Evaluation Complete");
        Console.WriteLine($"  Top 3 actions:");
        for (int i = 0; i < Math.Min(3, sorted.Count); i++)
        {
            var sa = sorted[i];
            Console.WriteLine($"    {i + 1}. {TruncateText(sa.Action.ActionText, 50)} (score: {sa.TotalScore:F3}, failures: {sa.TreeResult?.FailureCount ?? 0})");
        }
        Console.WriteLine();
        
        return sorted;
    }
    
    /// <summary>
    /// Builds a binary evaluation tree for an action.
    /// The tree structure is a linear chain where all checks must pass.
    /// </summary>
    private CriticNode BuildActionEvaluationTree(
        ParsedAction action,
        PlayerAction? previousAction,
        string? currentSublocation,
        LocationBlueprint? blueprint)
    {
        // Start with skill coherence check
        var root = CriticQuestions.SkillCoherence(action.ActionText, action.Skill, 0.5);
        var current = root;
        
        // Chain: consequence plausibility
        var consequenceNode = CriticQuestions.ConsequencePlausibility(
            action.ActionText, 
            action.SuccessConsequence, 
            0.5);
        current.SuccessBranch = consequenceNode;
        current = consequenceNode;
        
        // Chain: context coherence (if previous action exists)
        if (previousAction != null)
        {
            var contextNode = CriticQuestions.ContextCoherence(
                action.ActionText,
                previousAction.ActionText,
                previousAction.WasSuccessful ? "Success" : "Failure",
                0.5);
            current.SuccessBranch = contextNode;
            current = contextNode;
        }
        
        // Chain: location coherence (if location info exists)
        if (!string.IsNullOrEmpty(currentSublocation) && blueprint != null)
        {
            if (blueprint.Sublocations.TryGetValue(currentSublocation, out var sublocationData))
            {
                var locationNode = new CriticNode(
                    name: "LocationCoherence",
                    question: $"Location: {blueprint.LocationType}\nSublocation: {sublocationData.Name} - {sublocationData.Description}\n\nDoes the action '{action.ActionText}' make sense in this specific location?",
                    threshold: 0.5,
                    errorMessage: "Action does not fit the current location"
                );
                current.SuccessBranch = locationNode;
                current = locationNode;
            }
        }
        
        // Chain: action specificity
        var specificityNode = CriticQuestions.ActionSpecificity(action.ActionText, 0.5);
        current.SuccessBranch = specificityNode;
        
        return root;
    }
    
    /// <summary>
    /// Calculates a score from the tree result.
    /// Score is based on: number of passed checks / total checks evaluated,
    /// weighted by the individual node scores.
    /// </summary>
    private double CalculateScoreFromTreeResult(CriticTreeResult treeResult)
    {
        if (treeResult.Trace.Count == 0)
            return 0.0;
        
        // Calculate weighted average of all scores
        // Successful nodes contribute their full score, failed nodes contribute 0
        double totalScore = 0;
        foreach (var nodeResult in treeResult.Trace)
        {
            if (nodeResult.Success)
            {
                totalScore += nodeResult.Score;
            }
            else
            {
                // Failed node - add partial score based on how close it was
                totalScore += nodeResult.Score * 0.5; // Penalized but not zero
            }
        }
        
        return totalScore / treeResult.Trace.Count;
    }
    
    /// <summary>
    /// Extracts individual scores from tree result for backward compatibility.
    /// </summary>
    private void ExtractIndividualScores(ScoredAction scoredAction, CriticTreeResult treeResult)
    {
        foreach (var nodeResult in treeResult.Trace)
        {
            switch (nodeResult.NodeName)
            {
                case "SkillCoherence":
                    scoredAction.SkillScore = nodeResult.Score;
                    break;
                case "ConsequencePlausibility":
                    scoredAction.ConsequenceScore = nodeResult.Score;
                    break;
                case "ContextCoherence":
                    scoredAction.ContextScore = nodeResult.Score;
                    break;
                case "LocationCoherence":
                    scoredAction.LocationScore = nodeResult.Score;
                    break;
                case "ActionSpecificity":
                    scoredAction.SpecificityScore = nodeResult.Score;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Performs a skill check with d20 roll against difficulty.
    /// Used by the narrative system for action execution.
    /// </summary>
    public bool RollSkillCheck(Narrative.Skill skill, int difficulty, Narrative.Protagonist protagonist)
    {
        // Get relevant organ score
        int organScore = GetOrganScoreForSkill(skill, protagonist);
        
        // Roll d20
        var random = new Random();
        int roll = random.Next(1, 21);
        
        int total = roll + organScore;
        bool success = total >= difficulty;
        
        Console.WriteLine($"ActionScorer: Skill check - {skill.DisplayName} (Organ:{organScore}) + d20({roll}) = {total} vs DC {difficulty} → {(success ? "SUCCESS" : "FAILURE")}");
        
        return success;
    }
    
    /// <summary>
    /// Gets the organ score most relevant to a skill.
    /// Uses the skill's primary organ.
    /// </summary>
    private int GetOrganScoreForSkill(Narrative.Skill skill, Narrative.Protagonist protagonist)
    {
        return protagonist.GetOrganScoreForSkill(skill);
    }
    
    /// <summary>
    /// Truncates text for display purposes.
    /// </summary>
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }
}
