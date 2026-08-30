using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Pulp humor — a fibrous, earthy substance that raises middling outcomes to near-success.
/// Converts a 3 face to 5 and provides good vital heat.
/// </summary>
public sealed class PulpHumor : BodyHumor
{
    public override string Name => "Pulp";
    public override char Symbol => '\u2042'; // ⁂
    public override Vector4 Color => new(0.820f, 0.702f, 0.227f, 1.0f);           // warm amber
    public override Vector4 BackgroundColor => new(0.090f, 0.071f, 0.0f, 1.0f);   // dark brown
    public override int VitalHeat => 2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(3, 5);
}
