using System;
using System.Threading.Tasks;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Applies outcomes to the game state: learns modiMentis, adds items, transitions nodes, etc.
/// Fight and dialogue outcomes are not applied here — they signal mode transitions
/// handled by the narrative controller.
/// </summary>
public class OutcomeApplicator
{
    /// <summary>
    /// Fired when an outcome requires entering fight mode.
    /// The narrative controller subscribes to this to pause narration and start combat.
    /// </summary>
    public event Action<FightOutcome>? FightRequested;

    /// <summary>
    /// Fired when an outcome requires entering dialogue mode.
    /// The narrative controller subscribes to this to pause narration and start conversation.
    /// </summary>
    public event Action<DialogueOutcome>? DialogueRequested;

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

            case FightOutcome fight:
                Console.WriteLine($"OutcomeApplicator: Fight with {fight.Target.DisplayName}");
                FightRequested?.Invoke(fight);
                break;

            case DialogueOutcome dialogue:
                Console.WriteLine($"OutcomeApplicator: Dialogue with {dialogue.Target.DisplayName}");
                DialogueRequested?.Invoke(dialogue);
                break;

            default:
                Console.WriteLine($"OutcomeApplicator: Unknown outcome type {outcome.GetType().Name}");
                break;
        }
    }

    private void ApplyItemOutcome(Item item, Protagonist protagonist)
    {
        bool placed = protagonist.TryAcquireItem(item);
        if (!placed)
            Console.WriteLine($"OutcomeApplicator: Could not place '{item.DisplayName}' anywhere — inventory full.");
        else
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
        // TODO: Route HumorOutcome into the appropriate HumorQueue via protagonist.HumorQueues
        // For now, log the intent and skip — queue-routing will be implemented in a future phase.
        string direction = humor.Amount > 0 ? "produce" : "consume";
        Console.WriteLine($"OutcomeApplicator: {direction} {Math.Abs(humor.Amount)}x '{humor.HumorName}' (queue routing not yet implemented)");
    }
}
