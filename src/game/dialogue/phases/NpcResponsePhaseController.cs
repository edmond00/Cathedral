using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Executors;
using Cathedral.Game.Dialogue.Phases;

namespace Cathedral.Game.Dialogue.Phases;

/// <summary>
/// Handles a single player replica exchange:
/// 1. Compute skill check using N-dice system (N = skill level, count 6s vs difficulty)
/// 2. Apply outcome (affinity change or node transition)
/// 3. Generate NPC response
/// </summary>
public class NpcResponsePhaseController
{
    private readonly NpcResponseExecutor _executor;
    private readonly Random _rng = new();

    public NpcResponsePhaseController(NpcResponseExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Compute the dice count (= skill level) and difficulty (= sixes needed),
    /// but does NOT roll yet — called before the animation starts.
    /// </summary>
    public static (int DiceCount, int Difficulty) ComputeDiceParams(NpcInstance npc, ReplicaOption selected)
    {
        int   numberOfDice = Math.Clamp(selected.Skill.Level, 1, 10);
        float baseDiff     = npc.CurrentSubjectNode.BaseDifficultyScore;
        // Positive affinity reduces difficulty; negative affinity increases it
        float affinityMod  = (npc.Affinity - npc.InitialAffinity) * 0.005f;
        float adjDiff      = Math.Clamp(baseDiff - affinityMod, 0.05f, 0.95f);
        int   difficulty   = Math.Clamp((int)Math.Ceiling(adjDiff * numberOfDice), 1, numberOfDice);
        return (numberOfDice, difficulty);
    }

    /// <summary>
    /// Rolls <paramref name="numberOfDice"/> d6 dice with the given pre-computed difficulty,
    /// applies the outcome, and generates the NPC response.
    /// </summary>
    public async Task<ExchangeResult> ExecuteAsync(
        NpcInstance npc,
        ReplicaOption selected,
        int numberOfDice,
        int difficulty,
        CancellationToken cancellationToken = default)
    {
        // Roll N dice (1-6 each)
        int[] diceValues = new int[numberOfDice];
        for (int i = 0; i < numberOfDice; i++)
            diceValues[i] = _rng.Next(1, 7);

        int  sixes     = diceValues.Count(v => v == 6);
        bool succeeded = sixes >= difficulty;

        // Apply outcome
        if (succeeded)
            ApplyPositiveOutcome(npc, selected.TargetOutcome);
        else
            npc.Affinity = Math.Clamp(npc.Affinity + npc.CurrentSubjectNode.NegativeOutcome.AffinityDelta, 0f, 100f);

        // Generate NPC response
        string npcResponse = await _executor.GenerateResponseAsync(
            npc, selected.ReplicaText, succeeded, selected.TargetOutcome, cancellationToken);

        return new ExchangeResult(
            DiceCount:         numberOfDice,
            DiceRollDifficulty: difficulty,
            FinalDiceValues:   diceValues,
            Succeeded:         succeeded,
            NpcResponse:       npcResponse,
            AffinityDelta:     succeeded
                ? (selected.TargetOutcome is AffinityOutcome ao ? ao.AffinityDelta : 0f)
                : npc.CurrentSubjectNode.NegativeOutcome.AffinityDelta,
            NodeTransition:    succeeded && selected.TargetOutcome is NodeTransitionOutcome nto
                ? nto.TargetNode
                : null);
    }

    private static void ApplyPositiveOutcome(NpcInstance npc, ConversationOutcome outcome)
    {
        switch (outcome)
        {
            case AffinityOutcome affinityOutcome:
                npc.Affinity = Math.Clamp(npc.Affinity + affinityOutcome.AffinityDelta, 0f, 100f);
                break;
            case NodeTransitionOutcome transitionOutcome:
                npc.CurrentSubjectNode = transitionOutcome.TargetNode;
                break;
        }
    }
}

/// <summary>Result of a single player-replica skill check exchange.</summary>
public record ExchangeResult(
    int                      DiceCount,
    int                      DiceRollDifficulty,
    int[]                    FinalDiceValues,
    bool                     Succeeded,
    string                   NpcResponse,
    float                    AffinityDelta,
    ConversationSubjectNode? NodeTransition);

