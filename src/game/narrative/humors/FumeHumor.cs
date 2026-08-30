using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Fume humor — a corrosive inhalant that agitates middling outcomes.
/// Converts a 3 face to 4 but drains significant vital heat.
/// </summary>
public sealed class FumeHumor : BodyHumor
{
    public override string Name => "Fume";
    public override char Symbol => '\u03c8'; // ψ
    public override Vector4 Color => new(0.651f, 0.302f, 0.702f, 1.0f);           // smoky purple
    public override Vector4 BackgroundColor => new(0.090f, 0.020f, 0.098f, 1.0f); // dark purple
    public override int VitalHeat => -2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(3, 4);
}
