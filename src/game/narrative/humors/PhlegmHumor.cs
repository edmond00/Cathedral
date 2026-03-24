using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Phlegm humor — a neutral binding substance present at all organ health levels.
/// No vital heat contribution. Slightly reduces dice results (-1 modifier).
/// </summary>
public sealed class PhlegmHumor : BodyHumor
{
    public override string Name => "Phlegm";
    public override char Symbol => '\u2653'; // ♓
    public override Vector4 Color => new(0.75f, 0.75f, 0.75f, 1.0f);  // LightGray75
    public override int VitalHeat => 0;
    public override TransmutingVirtue? TransmutingVirtue => new NumericModVirtue(-1);
}
