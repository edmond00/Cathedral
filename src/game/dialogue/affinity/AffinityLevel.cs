namespace Cathedral.Game.Dialogue.Affinity;

/// <summary>
/// Represents the affinity level between an NPC and a party member.
/// The integer value is used as bonus dice in dialogue skill checks.
/// </summary>
public enum AffinityLevel
{
    Stranger            = 0,
    AnnoyingAcquaintance = 1,
    DistantAcquaintance  = 2,
    CloseAcquaintance    = 3,
    DistantFriend        = 4,
    CloseFriend          = 5,
    /// <summary>Post-reconcile state: the NPC is no longer hostile but remains deeply wary. Gives 0 bonus dice.</summary>
    Suspicious          = 6,
}

public static class AffinityLevelExtensions
{
    /// <summary>
    /// Returns the bonus dice count added to skill checks.
    /// Stranger gives 0 extra dice; CloseFriend gives 5.
    /// Suspicious gives 0 (same as Stranger — trust has not been earned back).
    /// </summary>
    public static int BonusDice(this AffinityLevel level) => level switch
    {
        AffinityLevel.Suspicious => 0,
        _                        => (int)level,
    };

    /// <summary>
    /// Returns a display string describing the relationship from the party member's perspective.
    /// e.g. "a stranger", "your close friend Aldric Holt"
    /// </summary>
    public static string ToDisplayName(this AffinityLevel level, string npcName) => level switch
    {
        AffinityLevel.Stranger             => $"a stranger",
        AffinityLevel.AnnoyingAcquaintance => $"an annoying acquaintance ({npcName})",
        AffinityLevel.DistantAcquaintance  => $"a distant acquaintance ({npcName})",
        AffinityLevel.CloseAcquaintance    => $"an acquaintance ({npcName})",
        AffinityLevel.DistantFriend        => $"a friend ({npcName})",
        AffinityLevel.CloseFriend          => $"your close friend {npcName}",
        AffinityLevel.Suspicious           => $"a suspicious acquaintance ({npcName})",
        _                                  => npcName,
    };

    /// <summary>
    /// First-person relation phrase used when building an NPC's contextual label for LLM prompts,
    /// e.g. "my friend Godric Reeve". Distinct from <see cref="ToDisplayName"/> (second-person
    /// "your …", used by the dialogue UI). Stranger is handled by the caller (name omitted), so it
    /// falls through to the bare name here.
    /// </summary>
    public static string ToFirstPersonRelation(this AffinityLevel level, string name) => level switch
    {
        AffinityLevel.AnnoyingAcquaintance
            or AffinityLevel.DistantAcquaintance
            or AffinityLevel.CloseAcquaintance => $"my acquaintance {name}",
        AffinityLevel.DistantFriend            => $"my friend {name}",
        AffinityLevel.CloseFriend              => $"my close friend {name}",
        AffinityLevel.Suspicious               => $"{name}, an uneasy acquaintance of mine",
        _                                      => name,
    };

    /// <summary>Returns a short label shown in the dialogue header (no NPC name).</summary>
    public static string ToShortLabel(this AffinityLevel level) => level switch
    {
        AffinityLevel.Stranger             => "Stranger",
        AffinityLevel.AnnoyingAcquaintance => "Annoying Acq.",
        AffinityLevel.DistantAcquaintance  => "Distant Acq.",
        AffinityLevel.CloseAcquaintance    => "Acquaintance",
        AffinityLevel.DistantFriend        => "Distant Friend",
        AffinityLevel.CloseFriend          => "Close Friend",
        AffinityLevel.Suspicious           => "Suspicious",
        _                                  => "Unknown",
    };

    /// <summary>Increments affinity by one step, clamped to <paramref name="max"/>.</summary>
    public static AffinityLevel Increment(this AffinityLevel level, AffinityLevel max = AffinityLevel.CloseFriend)
        => (AffinityLevel)Math.Min((int)level + 1, (int)max);

    /// <summary>Decrements affinity by one step, clamped to <paramref name="min"/>.</summary>
    public static AffinityLevel Decrement(this AffinityLevel level, AffinityLevel min = AffinityLevel.AnnoyingAcquaintance)
        => (AffinityLevel)Math.Max((int)level - 1, (int)min);
}
