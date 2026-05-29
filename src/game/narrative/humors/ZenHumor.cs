using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Zen humor — a state of equanimity and unpredictable clarity.
/// Rerolls any dice face to a new random value (1–6). Provides mild vital heat.
/// </summary>
public sealed class ZenHumor : BodyHumor
{
    public override string Name => "Zen";
    public override char Symbol => '\u262f'; // ☯
    public override Vector4 Color => new(0.780f, 0.749f, 0.549f, 1.0f);           // pale gold
    public override Vector4 BackgroundColor => new(0.102f, 0.102f, 0.071f, 1.0f); // dark olive
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new RerollVirtue(-1);
}
