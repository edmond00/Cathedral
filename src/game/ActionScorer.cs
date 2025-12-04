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
        var outcomeDescription = previousAction.WasSuccessful ? "succeeded" : "failed";
        var question = $"The player just attempted to '{previousAction.ActionText}' and it {outcomeDescription}. " +
                      $"Would '{currentAction.ActionText}' make sense as a logical next action in this situation?";
        
        // Use the Critic's yes/no evaluation
        return await _critic.EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Selects the top-k highest scored actions.
    /// </summary>
    public List<ParsedAction> SelectTopK(List<ScoredAction> scoredActions, int k)
    {
        return scoredActions
            .Take(k)
            .Select(s => s.Action)
            .ToList();
    }
    
    /// <summary>
    /// Gets statistics about the scoring results.
    /// </summary>
    public ScoringStatistics GetStatistics(List<ScoredAction> scoredActions)
    {
        if (scoredActions.Count == 0)
        {
            return new ScoringStatistics();
        }
        
        return new ScoringStatistics
        {
            TotalActions = scoredActions.Count,
            AverageScore = scoredActions.Average(a => a.TotalScore),
            HighestScore = scoredActions.Max(a => a.TotalScore),
            LowestScore = scoredActions.Min(a => a.TotalScore),
            AverageSkillScore = scoredActions.Average(a => a.SkillScore),
            AverageConsequenceScore = scoredActions.Average(a => a.ConsequenceScore),
            AverageContextScore = scoredActions.Average(a => a.ContextScore),
            TotalEvaluationTime = scoredActions.Sum(a => a.EvaluationDurationMs)
        };
    }
    
    /// <summary>
    /// Helper to truncate text for display.
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
/// Statistics about action scoring results.
/// </summary>
public class ScoringStatistics
{
    public int TotalActions { get; set; }
    public double AverageScore { get; set; }
    public double HighestScore { get; set; }
    public double LowestScore { get; set; }
    public double AverageSkillScore { get; set; }
    public double AverageConsequenceScore { get; set; }
    public double AverageContextScore { get; set; }
    public double TotalEvaluationTime { get; set; }
}
