using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.Game.Dialogue.Executors;

namespace Cathedral.Game.Dialogue.Phases;

/// <summary>
/// Pairs Speaking skills with conversation outcomes and generates one quoted replica per pair,
/// up to the protagonist's Tongue stat maximum.
/// </summary>
public class PlayerReplicaPhaseController
{
    private readonly PlayerReplicaExecutor _executor;
    private readonly ModusMentisRegistry _registry;

    public PlayerReplicaPhaseController(PlayerReplicaExecutor executor, ModusMentisRegistry registry)
    {
        _executor = executor;
        _registry = registry;
    }

    /// <summary>
    /// Produces a list of (ModusMentis, ConversationOutcome, replica text) tuples.
    /// <para>Progress callback is invoked after each replica is generated, with the current count and total.</para>
    /// </summary>
    public async Task<List<ReplicaOption>> GenerateReplicasAsync(
        NpcInstance npc,
        int maxReplicas,
        Action<int, int>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var speakingSkills = _registry.GetSpeakingModiMentis();
        if (speakingSkills.Count == 0)
            return new List<ReplicaOption>();

        var positiveOutcomes = npc.CurrentSubjectNode.PossiblePositiveOutcomes;
        if (positiveOutcomes.Count == 0)
            return new List<ReplicaOption>();

        // Shuffle skills for variety, then take up to maxReplicas
        var rng = new Random();
        var shuffled = speakingSkills.OrderBy(_ => rng.Next()).Take(maxReplicas).ToList();

        var results = new List<ReplicaOption>();
        int total = shuffled.Count;

        for (int i = 0; i < shuffled.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var skill = shuffled[i];
            // Cycle through outcomes so each skill gets a distinct outcome hint
            var outcome = positiveOutcomes[i % positiveOutcomes.Count];

            string replica = await _executor.GenerateReplicaAsync(
                skill, npc.CurrentSubjectNode, outcome, npc.Persona.PersonaTone);

            results.Add(new ReplicaOption(skill, outcome, replica));
            onProgress?.Invoke(i + 1, total);
        }

        return results;
    }
}

/// <summary>A generated player reply option, ready to be rendered and selected.</summary>
public record ReplicaOption(
    ModusMentis Skill,
    ConversationOutcome TargetOutcome,
    string ReplicaText);
