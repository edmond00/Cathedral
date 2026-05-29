using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Vapor humor — a stimulating inhalant that nudges weak results upward.
/// Converts a 2 face to 3, and provides mild vital heat.
/// </summary>
public sealed class VaporHumor : BodyHumor
{
    public override string Name => "Vapor";
    public override char Symbol => '\u2248'; // ≈
    public override Vector4 Color => new(0.878f, 0.820f, 0.502f, 1.0f);           // pale gold
    public override Vector4 BackgroundColor => new(0.090f, 0.078f, 0.020f, 1.0f); // dark olive
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(2, 3);
}
