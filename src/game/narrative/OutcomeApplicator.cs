using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Applies outcomes to the game state: learns skills, adds items, transitions nodes, etc.
/// </summary>
public class OutcomeApplicator
{
    /// <summary>
    /// Applies an outcome to the protagonist and game state.
    /// </summary>
    public async Task ApplyOutcomeAsync(OutcomeBase outcome, Protagonist protagonist)
    {
        switch (outcome)
        {
            case Item item:
                ApplyItemOutcome(item, protagonist);
                break;

            case NarrationNode node:
                // Transition is handled externally by the narrative controller
                Console.WriteLine($"OutcomeApplicator: Transition to {node.NodeId}");
                break;

            case FeelGoodOutcome feelGood:
                await ApplyFeelGoodOutcomeAsync(feelGood, protagonist);
                break;

            case HumorOutcome humor:
                ApplyHumorOutcome(humor, protagonist);
                break;

            default:
                Console.WriteLine($"OutcomeApplicator: Unknown outcome type {outcome.GetType().Name}");
                break;
        }
    }

    private void ApplyItemOutcome(Item item, Protagonist protagonist)
    {
        // Add item to inventory
        protagonist.Inventory.Add(item.ItemId);
        Console.WriteLine($"OutcomeApplicator: Acquired {item.DisplayName}");
    }

    private Task ApplyFeelGoodOutcomeAsync(FeelGoodOutcome feelGood, Protagonist protagonist)
    {
        // FeelGoodOutcome determines which humor to increase at runtime
        // Note: This requires context about the action, so it should be determined before applying
        Console.WriteLine($"OutcomeApplicator: Feel-good outcome (humor determination happens during execution)");
        return Task.CompletedTask;
    }

    private void ApplyHumorOutcome(HumorOutcome humor, Protagonist protagonist)
    {
        var targetHumor = protagonist.Humors.FirstOrDefault(h => h.Name == humor.HumorName);
        if (targetHumor != null)
        {
            int currentValue = targetHumor.Value;
            int newValue = Math.Clamp(currentValue + humor.Amount, 0, 100);
            targetHumor.Value = newValue;
            
            string direction = humor.Amount > 0 ? "increased" : "decreased";
            Console.WriteLine($"OutcomeApplicator: {humor.HumorName} {direction} by {Math.Abs(humor.Amount)} to {newValue}");
        }
        else
        {
            Console.WriteLine($"OutcomeApplicator: Unknown humor '{humor.HumorName}'");
        }
    }
}
