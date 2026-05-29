using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Aqua humor — pure water that gently lifts the lowest outcome to something bearable.
/// Converts a 1 face to 3 and provides mild vital heat.
/// </summary>
public sealed class AquaHumor : BodyHumor
{
    public override string Name => "Aqua";
    public override char Symbol => '\u2652'; // ♒
    public override Vector4 Color => new(0.851f, 0.820f, 0.600f, 1.0f);           // pale gold
    public override Vector4 BackgroundColor => new(0.059f, 0.059f, 0.090f, 1.0f); // slate dark
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(1, 3);
}
