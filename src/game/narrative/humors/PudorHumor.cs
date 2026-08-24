using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Pudor humor — shame: the mind turned against itself.
///
/// <para>Deliberately NOT a fourth rung of the 6→5 / 6→4 / 6→3 ladder that Melancholia, Choler and
/// Nervus already form. Shame does not spoil a peak, it spoils the ordinary competent result — so it
/// hollows out the middle of the die instead, which is the one part of the range no other mind state
/// touches.</para>
/// </summary>
public sealed class PudorHumor : BodyHumor
{
    public override string Name => "Pudor";
    public override char Symbol => '♄'; // ♄ Saturn — the heavy, self-punishing planet
    public override Vector4 Color => new(0.549f, 0.478f, 0.600f, 1.0f);           // leaden violet
    public override Vector4 BackgroundColor => new(0.071f, 0.051f, 0.090f, 1.0f); // dark purple
    public override int VitalHeat => -2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(4, 2);
    public override string FeelsLike => "It makes me feel ashamed of myself.";
}
