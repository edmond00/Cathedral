using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Sugar humor — a sweet excess that trades near-success for pleasant stability.
/// Converts a 5 face to 4 (a comfortable but not peak result) and provides strong vital heat.
/// </summary>
public sealed class SugarHumor : BodyHumor
{
    public override string Name => "Sugar";
    public override char Symbol => '\u2666'; // ♦
    public override Vector4 Color => new(0.949f, 0.878f, 0.322f, 1.0f);           // warm gold
    public override Vector4 BackgroundColor => new(0.102f, 0.082f, 0.0f, 1.0f);   // dark amber
    public override int VitalHeat => 3;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(5, 4);
}
