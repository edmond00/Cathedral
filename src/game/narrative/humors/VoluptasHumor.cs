using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Voluptas humor — a peak mind state of overwhelming pleasure and desire.
/// Converts any dice face to 6 (the best possible outcome) and provides strong vital heat.
/// </summary>
public sealed class VoluptasHumor : BodyHumor
{
    public override string Name => "Voluptas";
    public override char Symbol => '\u2600'; // ☀
    public override Vector4 Color => new(1.0f, 0.902f, 0.251f, 1.0f);           // bright gold
    public override Vector4 BackgroundColor => new(0.149f, 0.098f, 0.0f, 1.0f); // deep amber
    public override int VitalHeat => 3;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(-1, 6);
    public override string FeelsLike => "It makes me feel a pleasure I would do almost anything to keep.";
}
