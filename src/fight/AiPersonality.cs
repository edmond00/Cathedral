namespace Cathedral.Fight;

/// <summary>
/// Three-axis combat personality used by <see cref="FightAI"/> to score candidate actions.
/// Every weight lives in <c>[0..1]</c> and shifts the utility planner's bias:
///
///   • <see cref="Aggression"/> — favors big damaging attacks, tolerates slip risk, ignores defense.
///   • <see cref="Caution"/>    — leans on defensive skills + safer routes when HP is low.
///   • <see cref="Cunning"/>    — uses body-part aim, stacks status effects, varies its decisions more.
/// </summary>
public readonly record struct AiPersonality(double Aggression, double Caution, double Cunning)
{
    /// <summary>Balanced baseline — what unrecognised archetypes get.</summary>
    public static AiPersonality Default => new(0.40, 0.40, 0.40);

    /// <summary>Brute: charges in, takes risks, rarely defends.</summary>
    public static AiPersonality Brute => new(0.85, 0.15, 0.30);

    /// <summary>Skirmisher: balanced attacker that aims and reads the fight.</summary>
    public static AiPersonality Skirmisher => new(0.55, 0.45, 0.70);

    /// <summary>Coward: leans defensive, avoids slip-risk tiles.</summary>
    public static AiPersonality Coward => new(0.20, 0.85, 0.40);

    /// <summary>Mastermind: deliberate, body-part aim, mixes utility skills.</summary>
    public static AiPersonality Mastermind => new(0.50, 0.55, 0.90);

    /// <summary>
    /// Derive a sensible default from the NPC-level personality flags exposed by
    /// <c>NpcEntity</c>: a brave NPC trends toward Aggressive, an authority figure toward Cunning.
    /// </summary>
    public static AiPersonality FromArchetypeFlags(bool isBrave, int authorityLevel)
    {
        double aggression = 0.40 + (isBrave ? 0.30 : 0.00);
        double caution    = 0.40 - (isBrave ? 0.10 : 0.00) + (authorityLevel > 0 ? 0.05 : 0.00);
        double cunning    = 0.40 + (authorityLevel >= 2 ? 0.25 : 0.10 * authorityLevel);
        return new AiPersonality(
            System.Math.Clamp(aggression, 0, 1),
            System.Math.Clamp(caution,    0, 1),
            System.Math.Clamp(cunning,    0, 1));
    }
}
