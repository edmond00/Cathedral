using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A special outcome that represents positive emotional response without
/// specifying which humor changes. The specific humor is determined at runtime
/// by the Critic LLM based on the action's context.
/// 
/// This outcome is always available as a fallback option alongside concrete outcomes.
/// It has no keywords because it's always available regardless of what keyword was selected.
/// </summary>
public class FeelGoodOutcome : OutcomeBase
{
    public override string DisplayName => "Feel Good";
    
    public override string ToNaturalLanguageString()
    {
        return "feel good about the action";
    }
    
    /// <summary>
    /// Uses the Critic to determine which humor should increase based on the action.
    /// Evaluates each producible humor and selects the one with highest coherence score.
    /// </summary>
    /// <param name="actionText">The action that was performed</param>
    /// <param name="context">The narrative context</param>
    /// <param name="difficultyEvaluator">Evaluator with access to Critic</param>
    /// <returns>The humor that should be produced and by how much</returns>
    public async Task<(BodyHumor humor, int amount)> DetermineHumorChangeAsync(
        string actionText,
        string context,
        ActionDifficultyEvaluator difficultyEvaluator)
    {
        // Producible humors (Black Bile is pinned at queue back; never produced via FeelGood)
        var producibleHumors = new BodyHumor[]
        {
            new BloodHumor(),
            new PhlegmHumor(),
            new YellowBileHumor(),
            new MelancholiaHumor(),
        };

        var humorScores = new Dictionary<BodyHumor, double>();

        // Evaluate each humor using the Critic
        foreach (var humor in producibleHumors)
        {
            var question = $"In the following context : {context}\nThe {Config.Narrative.PlayerName} succeeded at the following action : \"{actionText}\",\nDoes this action resonate with the essence of {humor.Name}?";
            var score = await difficultyEvaluator.EvaluateCoherence(question);
            humorScores[humor] = score;
        }

        // Select humor with highest score
        var selectedHumor = humorScores.OrderByDescending(kvp => kvp.Value).First().Key;

        // Amount based on how confident the Critic is (1-5)
        var confidence = humorScores[selectedHumor];
        int amount = confidence switch
        {
            >= 0.8 => 5,  // Very confident
            >= 0.6 => 3,  // Moderately confident
            >= 0.4 => 2,  // Somewhat confident
            _ => 1        // Low confidence
        };

        Console.WriteLine($"FeelGood: Selected {selectedHumor.Name} +{amount} (confidence: {confidence:F2})");

        return (selectedHumor, amount);
    }
}
