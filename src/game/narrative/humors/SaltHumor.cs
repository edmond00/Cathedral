using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Salt humor — a preserving mineral that stabilises the worst outcomes into something bearable.
/// Converts a 1 face to 2 and provides mild vital heat.
/// </summary>
public sealed class SaltHumor : BodyHumor
{
    public override string Name => "Salt";
    public override char Symbol => '\u2296'; // ⊖
    public override Vector4 Color => new(0.820f, 0.780f, 0.522f, 1.0f);           // pale gold
    public override Vector4 BackgroundColor => new(0.078f, 0.078f, 0.059f, 1.0f); // dark gray
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(1, 2);
}
