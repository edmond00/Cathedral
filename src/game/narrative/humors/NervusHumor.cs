using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Nervus humor — a state of anxiety and nervous collapse.
/// Converts a 6 face to 3, severely undermining peak results, and drains vital heat.
/// </summary>
public sealed class NervusHumor : BodyHumor
{
    public override string Name => "Nervus";
    public override char Symbol => '\u26a1'; // ⚡
    public override Vector4 Color => new(0.749f, 0.302f, 0.749f, 1.0f);           // violet
    public override Vector4 BackgroundColor => new(0.078f, 0.0f, 0.102f, 1.0f);   // deep purple
    public override int VitalHeat => -2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(6, 3);
    public override string FeelsLike => "It makes me feel afraid, and unsteady with it.";
}
