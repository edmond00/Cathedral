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

    /// <summary>
    /// True when this virtue can legally modify a die currently showing <paramref name="dieValue"/>.
    /// Drives which dice become clickable once the humor is selected during a roll.
    /// </summary>
    public abstract bool CanApplyTo(int dieValue);

    /// <summary>
    /// Returns the new face value after applying this virtue to a die currently showing
    /// <paramref name="dieValue"/>. Callers must guard with <see cref="CanApplyTo"/> first.
    /// </summary>
    public abstract int Apply(int dieValue, Random rng);
}

/// <summary>
/// A transmuting virtue that leaves the dice roll unchanged (N → N).
/// Used by humors that are present but exert no transmuting influence.
/// </summary>
public sealed class NullVirtue : TransmutingVirtue
{
    public override string Description => "N \u2192 N";

    /// <summary>A null virtue exerts no influence, so no die is ever clickable.</summary>
    public override bool CanApplyTo(int dieValue) => false;

    public override int Apply(int dieValue, Random rng) => dieValue;
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

    /// <summary>A numeric shift applies to any die.</summary>
    public override bool CanApplyTo(int dieValue) => true;

    public override int Apply(int dieValue, Random rng) => Math.Clamp(dieValue + Modifier, 1, 6);
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

    /// <summary>Applies to any die when wildcard (-1), otherwise only to the matching source face.</summary>
    public override bool CanApplyTo(int dieValue) => SourceDigit == -1 || dieValue == SourceDigit;

    public override int Apply(int dieValue, Random rng) => TargetDigit;
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

    /// <summary>Applies to any die when wildcard (-1), otherwise only to the matching source face.</summary>
    public override bool CanApplyTo(int dieValue) => SourceDigit == -1 || dieValue == SourceDigit;

    public override int Apply(int dieValue, Random rng) => rng.Next(1, 7);
}
