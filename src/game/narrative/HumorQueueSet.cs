using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Holds the four humoral organ queues for a single party member.
/// Each queue is permanently associated with one organ:
///   Hepar (liver), Paunch (stomach), Pulmones (lungs), Spleen.
/// </summary>
public sealed class HumorQueueSet
{
    // ── Four queues ───────────────────────────────────────────────
    public HumorQueue Hepar    { get; } = new HumorQueue("hepar");
    public HumorQueue Paunch   { get; } = new HumorQueue("paunch");
    public HumorQueue Pulmones { get; } = new HumorQueue("pulmones");
    public HumorQueue Spleen   { get; } = new HumorQueue("spleen");

    /// <summary>
    /// Returns the queue associated with the given organ id, or null when not found.
    /// </summary>
    public HumorQueue? GetByOrganId(string organId) => organId switch
    {
        "hepar"    => Hepar,
        "paunch"   => Paunch,
        "pulmones" => Pulmones,
        "spleen"   => Spleen,
        _          => null
    };

    /// <summary>All four queues in a stable iteration order.</summary>
    public IEnumerable<HumorQueue> All
    {
        get
        {
            yield return Hepar;
            yield return Paunch;
            yield return Pulmones;
            yield return Spleen;
        }
    }

    /// <summary>Total number of black bile instances across all four queues.</summary>
    public int TotalBlackBileCount
    {
        get
        {
            int total = 0;
            foreach (var q in All)
                for (int i = 0; i < HumorQueue.Capacity; i++)
                    if (q.Items[i].IsBlackBile) total++;
            return total;
        }
    }

    /// <summary>Sum of VitalHeat for every humor slot across all four queues.</summary>
    public int TotalVitalHeat
    {
        get
        {
            int total = 0;
            foreach (var q in All)
                for (int i = 0; i < HumorQueue.Capacity; i++)
                    total += q.Items[i].VitalHeat;
            return total;
        }
    }

    /// <summary>True when every queue is critical (entirely black bile).</summary>
    public bool IsFullyCritical =>
        Hepar.IsCritical && Paunch.IsCritical && Pulmones.IsCritical && Spleen.IsCritical;

    // ── Initialisation ────────────────────────────────────────────

    /// <summary>
    /// Fill all four queues with humors secreted from the corresponding organ scores.
    /// Call this after the party member's body parts have been initialised.
    ///
    /// For each organ the secretion probabilities are computed from that organ's current score,
    /// so a high-Hepar character will have mostly Blood in their Hepar queue.
    /// </summary>
    public void Initialize(PartyMember member, Random rng)
    {
        foreach (var queue in All)
        {
            var organ = member.GetOrganById(queue.OrganId);
            int score = organ?.Score ?? 5;
            queue.FillWithSecretion(score, rng);
        }
    }

    // ── Gameplay API (future implementation hooks) ────────────────

    /// <summary>
    /// Attempt to obtain vital heat from the specified queue.
    /// Consumes the oldest non-black-bile humor and returns its VitalHeat value.
    /// Returns 0 when the queue is critical.
    /// TODO: hook into travel / attack energy system.
    /// </summary>
    public int ConsumeVitalHeat(string organId, int organScore, Random rng)
    {
        var queue = GetByOrganId(organId);
        if (queue == null) return 0;
        var consumed = queue.Consume(organScore, rng);
        return consumed?.VitalHeat ?? 0;
    }

    /// <summary>
    /// Produce a specific humor instance into the specified organ's queue
    /// (e.g., Paunch produces BloodHumor after eating, Spleen produces MelancholiaHumor
    /// after a traumatic narrative event).
    /// Returns false when the queue is critical.
    /// TODO: hook into item-use and outcome-application systems.
    /// </summary>
    public bool ProduceHumor(string organId, BodyHumor humor)
    {
        return GetByOrganId(organId)?.Produce(humor) ?? false;
    }

    /// <summary>
    /// Apply secretion to all four queues simultaneously (called at the end of a turn
    /// or after a significant event). Uses each organ's current score.
    /// TODO: hook into turn / rest system.
    /// </summary>
    public void SecretionCycle(PartyMember member, Random rng)
    {
        foreach (var queue in All)
        {
            var organ = member.GetOrganById(queue.OrganId);
            int score = organ?.Score ?? 5;
            queue.Secrete(score, rng);
        }
    }
}
