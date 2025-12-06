using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Evaluates action difficulty and determines plausible failure consequences.
/// This is the second Critic pass that occurs AFTER the player selects an action.
/// </summary>
public class ActionDifficultyEvaluator
{
    private readonly CriticEvaluator _critic;
    
    // Possible failure consequences that can occur
    private static readonly string[] FailureConsequences = new[]
    {
        "injured",
        "lost",
        "equipment_loss",
        "exhaustion",
        "attacked",
        "disease"
    };
    
    public ActionDifficultyEvaluator(CriticEvaluator critic)
    {
        _critic = critic ?? throw new ArgumentNullException(nameof(critic));
    }
    
    /// <summary>
    /// Evaluates a selected action to determine its difficulty and possible failure consequences.
    /// This is called AFTER the player chooses an action but BEFORE execution.
    /// </summary>
    public async Task<ActionDifficultyResult> EvaluateSelectedActionAsync(
        ParsedAction selectedAction,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        Console.WriteLine($"\n🎯 Evaluating selected action difficulty: {selectedAction.ActionText}");
        
        var startTime = DateTime.Now;
        
        // Step 1: Determine difficulty
        var difficultyScore = await EvaluateDifficultyAsync(selectedAction);
        var difficulty = ScoreToDifficulty(difficultyScore);
        
        Console.WriteLine($"  📊 Difficulty: {difficulty} (score: {difficultyScore:F3})");
        
        // Step 2: Evaluate each possible failure consequence
        var failureConsequences = new Dictionary<string, double>();
        
        Console.WriteLine($"  🔍 Evaluating {FailureConsequences.Length} possible failure consequences...");
        
        foreach (var consequence in FailureConsequences)
        {
            var plausibility = await EvaluateFailureConsequencePlausibilityAsync(
                selectedAction.ActionText,
                consequence);
            
            failureConsequences[consequence] = plausibility;
            
            Console.WriteLine($"    • {consequence}: {plausibility:F3}");
        }
        
        // Step 3: Select the most plausible failure consequence
        var mostPlausibleFailure = failureConsequences
            .OrderByDescending(kvp => kvp.Value)
            .First();
        
        var duration = (DateTime.Now - startTime).TotalMilliseconds;
        
        Console.WriteLine($"  ✓ Most plausible failure: {mostPlausibleFailure.Key} ({mostPlausibleFailure.Value:F3})");
        Console.WriteLine($"  ⏱️  Evaluation duration: {duration:F0}ms\n");
        
        return new ActionDifficultyResult
        {
            Action = selectedAction,
            DifficultyScore = difficultyScore,
            Difficulty = difficulty,
            FailureConsequencePlausibilities = failureConsequences,
            MostPlausibleFailure = mostPlausibleFailure.Key,
            EvaluationDurationMs = duration
        };
    }
    
    /// <summary>
    /// Evaluates how difficult an action is to perform.
    /// Returns a score from 0.0 (trivial) to 1.0 (extremely hard).
    /// </summary>
    private async Task<double> EvaluateDifficultyAsync(ParsedAction action)
    {
        var question = $"Is the action '{action.ActionText}' easy to perform?";
        
        // Get yes/no probability from Critic
        // Yes = easy (low difficulty), No = hard (high difficulty)
        var easyScore = await _critic.EvaluateYesNoQuestion(question);
        
        // Convert to difficulty score: easy=0, hard=1
        return 1.0 - easyScore;
    }
    
    /// <summary>
    /// Evaluates if a specific failure consequence is plausible for this action.
    /// Returns a score from 0.0 (implausible) to 1.0 (very plausible).
    /// </summary>
    private async Task<double> EvaluateFailureConsequencePlausibilityAsync(
        string actionText,
        string failureConsequence)
    {
        var question = $"If the action '{actionText}' fails, could it plausibly result in '{failureConsequence}'?";
        
        return await _critic.EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Converts a difficulty score to a difficulty label.
    /// </summary>
    private static string ScoreToDifficulty(double score)
    {
        if (score < 0.15) return "trivial";
        if (score < 0.30) return "easy";
        if (score < 0.45) return "basic";
        if (score < 0.60) return "moderate";
        if (score < 0.75) return "hard";
        if (score < 0.90) return "very_hard";
        return "extreme";
    }
    
    /// <summary>
    /// Determines if an action should succeed based on difficulty score.
    /// Uses a probability-based approach: harder actions are less likely to succeed.
    /// </summary>
    public bool DetermineActionSuccess(
        double difficultyScore, 
        string? actionText = null,
        Random? rng = null, 
        bool logRoll = false)
    {
        var random = rng ?? new Random();
        
        // Convert difficulty to success probability
        // Trivial (0.0) = 95% success
        // Moderate (0.5) = 70% success
        // Extreme (1.0) = 40% success
        var successProbability = 0.95 - (difficultyScore * 0.55);
        
        var roll = random.NextDouble();
        bool success = roll < successProbability;
        
        if (logRoll)
        {
            Console.WriteLine($"[RNG ROLL] Roll: {roll:F4} vs Threshold: {successProbability:F4} → {(success ? "SUCCESS" : "FAILURE")}");
            
            // Also log to file if action text is provided
            if (!string.IsNullOrEmpty(actionText))
            {
                LLMLogger.LogRNGRoll(actionText, difficultyScore, successProbability, roll, success);
            }
        }
        
        return success;
    }
}

/// <summary>
/// Result of evaluating an action's difficulty and failure consequences.
/// </summary>
public class ActionDifficultyResult
{
    public ParsedAction Action { get; set; } = null!;
    public double DifficultyScore { get; set; }
    public string Difficulty { get; set; } = "";
    public Dictionary<string, double> FailureConsequencePlausibilities { get; set; } = new();
    public string MostPlausibleFailure { get; set; } = "";
    public double EvaluationDurationMs { get; set; }
}
