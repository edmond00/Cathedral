using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a dice-roll modification that can be applied when a humor's transmuting virtue
/// is invoked during a modusMentis check. This is a descriptor only; application is handled by
/// future gameplay systems.
/// </summary>
public abstract class TransmutingVirtue
{
    /// <summary>Human-readable description of the effect shown in the UI.</summary>
    public abstract string Description { get; }
}

/// <summary>
/// A transmuting virtue that leaves the dice roll unchanged (N → N).
/// Used by humors that are present but exert no transmuting influence.
/// </summary>
public sealed class NullVirtue : TransmutingVirtue
{
    public override string Description => "N \u2192 N";
}

/// <summary>
/// Adds or subtracts a fixed amount from a dice-roll result.
/// Displayed as "N → N + X" or "N → N - X".
/// </summary>
public sealed class NumericModVirtue : TransmutingVirtue
{
    /// <summary>Amount to add (positive) or subtract (negative) from the roll.</summary>
    public int Modifier { get; }

    public NumericModVirtue(int modifier) => Modifier = modifier;

    public override string Description =>
        Modifier >= 0
            ? $"N \u2192 N + {Modifier}"
            : $"N \u2192 N - {Math.Abs(Modifier)}";
}

/// <summary>
/// Converts a specific die face to another die face.
/// When <see cref="SourceDigit"/> is -1 the conversion applies to ANY face (sets roll to
/// <see cref="TargetDigit"/> unconditionally).
/// Displayed as "{Source} → {Target}" or "N → {Target}" for the wildcard case.
/// </summary>
public sealed class DigitConversionVirtue : TransmutingVirtue
{
    /// <summary>Die face to convert from. -1 means "any face" (wildcard).</summary>
    public int SourceDigit { get; }

    /// <summary>Die face to convert to.</summary>
    public int TargetDigit { get; }

    public DigitConversionVirtue(int sourceDigit, int targetDigit)
    {
        SourceDigit = sourceDigit;
        TargetDigit = targetDigit;
    }

    public override string Description =>
        SourceDigit == -1
            ? $"N \u2192 {TargetDigit}"
            : $"{SourceDigit} \u2192 {TargetDigit}";
}

/// <summary>
/// Rerolls a specific die face to a new random value (1–6).
/// When <see cref="SourceDigit"/> is -1 the reroll applies to ANY face.
/// Displayed as "{Source} → ?" or "N → ?" for the wildcard case.
/// </summary>
public sealed class RerollVirtue : TransmutingVirtue
{
    /// <summary>Die face to reroll. -1 means "any face" (wildcard).</summary>
    public int SourceDigit { get; }

    public RerollVirtue(int sourceDigit) => SourceDigit = sourceDigit;

    public override string Description =>
        SourceDigit == -1
            ? "N \u2192 ?"
            : $"{SourceDigit} \u2192 ?";
}
