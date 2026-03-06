using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Yellow Bile humor — irritating substance secreted under organ stress.
/// Drains vital heat and forces any dice roll to 1 (worst possible outcome).
/// SourceDigit=-1 means the conversion applies regardless of the current face.
/// </summary>
public sealed class YellowBileHumor : BodyHumor
{
    public override string Name => "Yellow Bile";
    public override char Symbol => '\u264c'; // ♌
    public override Vector4 Color => new(1.0f, 1.0f, 0.0f, 1.0f);  // BrightYellow
    public override int VitalHeat => -1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(-1, 1);
}
