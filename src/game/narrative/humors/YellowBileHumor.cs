using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Yellow Bile humor — irritating substance secreted under organ stress.
/// Drains vital heat and reduces any dice result by 1.
/// </summary>
public sealed class YellowBileHumor : BodyHumor
{
    public override string Name => "Yellow Bile";
    public override char Symbol => '\u264c'; // ♌
    public override Vector4 Color => new(1.0f, 1.0f, 0.0f, 1.0f);  // BrightYellow
    public override int VitalHeat => 0;
    public override TransmutingVirtue? TransmutingVirtue => new NumericModVirtue(-1);
}
