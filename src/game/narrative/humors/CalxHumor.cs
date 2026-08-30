using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Calx humor — a calcified mineral essence that consolidates good outcomes to near-success.
/// Converts a 4 face to 5 and provides strong vital heat.
/// </summary>
public sealed class CalxHumor : BodyHumor
{
    public override string Name => "Calx";
    public override char Symbol => '\u25c7'; // ◇
    public override Vector4 Color => new(0.949f, 0.820f, 0.278f, 1.0f);           // bright amber
    public override Vector4 BackgroundColor => new(0.110f, 0.082f, 0.0f, 1.0f);   // dark amber
    public override int VitalHeat => 3;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(4, 5);
}
