using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Alcohol humor — an intoxicant that destabilises near-peak outcomes unpredictably.
/// Rerolls a 5 face to a random new value and heavily drains vital heat.
/// </summary>
public sealed class AlcoholHumor : BodyHumor
{
    public override string Name => "Alcohol";
    public override char Symbol => '\u2650'; // ♐
    public override Vector4 Color => new(0.702f, 0.200f, 0.749f, 1.0f);           // deep purple
    public override Vector4 BackgroundColor => new(0.090f, 0.020f, 0.098f, 1.0f); // dark purple
    public override int VitalHeat => -3;
    public override TransmutingVirtue? TransmutingVirtue => new RerollVirtue(5);
}
