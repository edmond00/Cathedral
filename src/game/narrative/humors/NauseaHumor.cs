using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Nausea humor — disgust: the body's recoil from what it has been made to touch.
///
/// <para>Its transmutation is the exact mirror of <see cref="LaetitiaHumor"/> (5→6): the two fight
/// over the near-success. Disgust is a milder drain than terror, hence −1 rather than −2.</para>
/// </summary>
public sealed class NauseaHumor : BodyHumor
{
    public override string Name => "Nausea";
    public override char Symbol => '♏'; // ♏ Scorpio
    public override Vector4 Color => new(0.620f, 0.420f, 0.702f, 1.0f);           // bilious violet
    public override Vector4 BackgroundColor => new(0.082f, 0.039f, 0.098f, 1.0f); // dark purple
    public override int VitalHeat => -1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(5, 3);
    public override string FeelsLike => "It makes me feel sick to the stomach.";
}
