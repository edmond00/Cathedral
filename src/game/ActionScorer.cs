using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Scores and filters actions using the Critic evaluator.
/// Evaluates action-skill coherence, action-consequence plausibility, and context coherence.
/// </summary>
public class ActionScorer
{
    private readonly CriticEvaluator _critic;
    
    // Scoring weights
    private const double SkillWeight = 0.4;
    private const double ConsequenceWeight = 0.4;
    private const double ContextWeight = 0.2;
    
    public ActionScorer(CriticEvaluator critic)
    {
        _critic = critic ?? throw new ArgumentNullException(nameof(critic));
    }
    
    /// <summary>
    /// Scores all actions using the Critic evaluator.
    /// Evaluates skill coherence, consequence plausibility, and optional context coherence.
    /// Returns actions sorted by total score (highest first).
    /// </summary>
    public async Task<List<ScoredAction>> ScoreActionsAsync(
        List<ParsedAction> actions,
        PlayerAction? previousAction = null)
    {
        var scoredActions = new List<ScoredAction>();
        
        Console.WriteLine($"\n🔍 Critic is evaluating {actions.Count} actions...");
        
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var sw = Stopwatch.StartNew();
            
            // Evaluate action-skill coherence
            var skillScore = await _critic.EvaluateActionSkillCoherence(
                action.ActionText, action.Skill);
            
            // Evaluate action-consequence plausibility
            var consequenceScore = await _critic.EvaluateActionConsequencePlausibility(
                action.ActionText, action.SuccessConsequence);
            
            // Evaluate context coherence with previous action
            var contextScore = 1.0; // Default for first turn
            if (previousAction != null)
            {
                contextScore = await EvaluateContextCoherence(action, previousAction);
            }
            
            sw.Stop();
            
            // Calculate composite score (weighted average)
            var totalScore = (skillScore * SkillWeight) + 
                           (consequenceScore * ConsequenceWeight) + 
                           (contextScore * ContextWeight);
            
            scoredActions.Add(new ScoredAction
            {
                Action = action,
                SkillScore = skillScore,
                ConsequenceScore = consequenceScore,
                ContextScore = contextScore,
                TotalScore = totalScore,
                EvaluationDurationMs = sw.Elapsed.TotalMilliseconds
            });
            
            // Progress indicator
            Console.Write($"  [{i + 1}/{actions.Count}] ");
            Console.Write($"Action: {TruncateText(action.ActionText, 40)} ");
            Console.WriteLine($"→ Score: {totalScore:F3}");
        }
        
        // Sort by total score descending (best actions first)
        var sorted = scoredActions.OrderByDescending(a => a.TotalScore).ToList();
        
        Console.WriteLine($"✓ Critic evaluation complete\n");
        
        return sorted;
    }
    
    /// <summary>
    /// Evaluates whether an action makes sense given the previous action.
    /// Uses the Critic to assess narrative/contextual coherence.
    /// </summary>
    private async Task<double> EvaluateContextCoherence(
        ParsedAction currentAction,
        PlayerAction previousAction)
    {
        // Build a question that captures the context
        var question = $@"Previous action: {previousAction.ActionText}
Previous outcome: {(previousAction.WasSuccessful ? "Success" : "Failure")} - {previousAction.Outcome}

Current action being considered: {currentAction.ActionText}

Does this new action make logical sense as a follow-up to the previous action and its outcome?";

        // Use narrative quality evaluation as a proxy for context coherence
        return await _critic.EvaluateNarrativeQuality(question, "logical and coherent sequence");
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
