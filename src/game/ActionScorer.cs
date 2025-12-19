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
    /// Scores all actions using the Critic evaluator with enhanced filtering questions.
    /// First pass evaluation includes: skill coherence, consequence plausibility, context coherence,
    /// location coherence, and action specificity.
    /// Returns actions sorted by total score (highest first).
    /// </summary>
    public async Task<List<ScoredAction>> ScoreActionsAsync(
        List<ParsedAction> actions,
        PlayerAction? previousAction = null,
        string? currentSublocation = null,
        LocationBlueprint? blueprint = null)
    {
        var scoredActions = new List<ScoredAction>();
        
        Console.WriteLine($"\n🔍 Critic First Pass: Evaluating {actions.Count} actions...");
        Console.WriteLine($"   Checking: skill coherence, consequence plausibility, context, location fit, specificity");
        
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var sw = Stopwatch.StartNew();
            
            Console.WriteLine($"\n  [{i + 1}/{actions.Count}] Evaluating: {TruncateText(action.ActionText, 50)}");
            
            // 1. Evaluate action-skill coherence
            Console.WriteLine($"    • Checking skill coherence with '{action.Skill}'...");
            var skillScore = await _critic.EvaluateActionSkillCoherence(
                action.ActionText, action.Skill);
            Console.WriteLine($"      → Score: {skillScore:F3}");
            
            // 2. Evaluate action-consequence plausibility
            Console.WriteLine($"    • Checking consequence plausibility ('{action.SuccessConsequence}')...");
            var consequenceScore = await _critic.EvaluateActionConsequencePlausibility(
                action.ActionText, action.SuccessConsequence);
            Console.WriteLine($"      → Score: {consequenceScore:F3}");
            
            // 3. Evaluate context coherence with previous action
            var contextScore = 1.0; // Default for first turn
            if (previousAction != null)
            {
                Console.WriteLine($"    • Checking context coherence with previous action...");
                contextScore = await EvaluateContextCoherence(action, previousAction);
                Console.WriteLine($"      → Score: {contextScore:F3}");
            }
            
            // 4. Evaluate location coherence (NEW)
            var locationScore = 1.0; // Default if no location info
            if (!string.IsNullOrEmpty(currentSublocation) && blueprint != null)
            {
                Console.WriteLine($"    • Checking location coherence...");
                locationScore = await EvaluateLocationCoherence(action, currentSublocation, blueprint);
                Console.WriteLine($"      → Score: {locationScore:F3}");
            }
            
            // 5. Evaluate action specificity (NEW)
            Console.WriteLine($"    • Checking action specificity...");
            var specificityScore = await EvaluateActionSpecificity(action);
            Console.WriteLine($"      → Score: {specificityScore:F3}");
            
            sw.Stop();
            
            // Calculate composite score with updated weights
            // Adjusted weights to account for 5 criteria
            var totalScore = (skillScore * 0.25) + 
                           (consequenceScore * 0.25) + 
                           (contextScore * 0.20) +
                           (locationScore * 0.15) +
                           (specificityScore * 0.15);
            
            scoredActions.Add(new ScoredAction
            {
                Action = action,
                SkillScore = skillScore,
                ConsequenceScore = consequenceScore,
                ContextScore = contextScore,
                LocationScore = locationScore,
                SpecificityScore = specificityScore,
                TotalScore = totalScore,
                EvaluationDurationMs = sw.Elapsed.TotalMilliseconds
            });
            
            Console.WriteLine($"    ✓ Total Score: {totalScore:F3} (duration: {sw.Elapsed.TotalMilliseconds:F0}ms)");
        }
        
        // Sort by total score descending (best actions first)
        var sorted = scoredActions.OrderByDescending(a => a.TotalScore).ToList();
        
        Console.WriteLine($"\n✓ Critic First Pass Complete");
        Console.WriteLine($"  Top 3 actions:");
        for (int i = 0; i < Math.Min(3, sorted.Count); i++)
        {
            Console.WriteLine($"    {i + 1}. {TruncateText(sorted[i].Action.ActionText, 50)} (score: {sorted[i].TotalScore:F3})");
        }
        Console.WriteLine();
        
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
    /// Evaluates whether an action makes sense in the current location/sublocation.
    /// Checks if the action is coherent with the surroundings and environmental conditions.
    /// </summary>
    private async Task<double> EvaluateLocationCoherence(
        ParsedAction action,
        string currentSublocation,
        LocationBlueprint blueprint)
    {
        // Get sublocation description
        if (!blueprint.Sublocations.TryGetValue(currentSublocation, out var sublocationData))
        {
            return 0.5; // Neutral if sublocation not found
        }
        
        var question = $@"Location: {blueprint.LocationType}
Sublocation: {sublocationData.Name} - {sublocationData.Description}

Action being considered: {action.ActionText}

Does this action make sense in this specific location and its surroundings?";
        
        return await _critic.EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Evaluates whether an action is specific and concrete (not abstract or too general).
    /// Higher scores for precise, concrete actions; lower scores for vague, abstract ones.
    /// </summary>
    private async Task<double> EvaluateActionSpecificity(ParsedAction action)
    {
        var question = $@"Action: {action.ActionText}

Is this action specific and concrete (rather than abstract or overly general)?";
        
        return await _critic.EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Performs a skill check with d20 roll against difficulty.
    /// Used by the narrative system for action execution.
    /// </summary>
    public bool RollSkillCheck(Narrative.Skill skill, int difficulty, Narrative.Avatar avatar)
    {
        // Get relevant body part value
        int bodyPartValue = GetBodyPartValueForSkill(skill, avatar);
        
        // Roll d20
        var random = new Random();
        int roll = random.Next(1, 21);
        
        int total = roll + bodyPartValue;
        bool success = total >= difficulty;
        
        Console.WriteLine($"ActionScorer: Skill check - {skill.DisplayName} (BP:{bodyPartValue}) + d20({roll}) = {total} vs DC {difficulty} → {(success ? "SUCCESS" : "FAILURE")}");
        
        return success;
    }
    
    /// <summary>
    /// Gets the body part value most relevant to a skill.
    /// Maps skill function to appropriate body part.
    /// </summary>
    private int GetBodyPartValueForSkill(Narrative.Skill skill, Narrative.Avatar avatar)
    {
        // Use primary body part from skill's BodyParts array
        string bodyPartName = skill.BodyParts.Length > 0 
            ? skill.BodyParts[0].ToLower() 
            : "hands";
        
        return avatar.BodyParts.TryGetValue(bodyPartName, out int value) ? value : 5;
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
