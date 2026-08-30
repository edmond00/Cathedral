using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Venom humor — a lethal draught that destroys peak outcomes entirely.
/// Converts a 6 face to 1 (the worst outcome) and catastrophically drains vital heat.
/// </summary>
public sealed class VenomHumor : BodyHumor
{
    public override string Name => "Venom";
    public override char Symbol => '\u2297'; // ⊗
    public override Vector4 Color => new(0.902f, 0.051f, 0.949f, 1.0f);           // vivid violet
    public override Vector4 BackgroundColor => new(0.102f, 0.0f, 0.122f, 1.0f);   // void purple
    public override int VitalHeat => -5;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(6, 1);
}
