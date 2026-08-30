using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Opium humor — a soporific inhalant that numbs and scrambles outcomes.
/// Rerolls a 3 face to a random new value, and lightly drains vital heat.
/// </summary>
public sealed class OpiumHumor : BodyHumor
{
    public override string Name => "Opium";
    public override char Symbol => '\u263e'; // ☾
    public override Vector4 Color => new(0.549f, 0.349f, 0.651f, 1.0f);           // muted violet
    public override Vector4 BackgroundColor => new(0.071f, 0.020f, 0.090f, 1.0f); // dark purple
    public override int VitalHeat => -1;
    public override TransmutingVirtue? TransmutingVirtue => new RerollVirtue(3);
}
