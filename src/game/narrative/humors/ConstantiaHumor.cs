using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constantia humor — steadfastness: the hardening that answers one's own hurt.
///
/// <para>The only mind state that works the BOTTOM of the die. Voluptas (N→6), Laetitia (5→6),
/// Melancholia (6→5), Choler (6→4) and Nervus (6→3) all key on the top faces, so a body in a bad
/// state had no mind state that could rescue a 1 — only the peak reward could help it. This is the
/// counterweight to Nervus, and it is what <c>obduracy</c> pays out.</para>
/// </summary>
public sealed class ConstantiaHumor : BodyHumor
{
    public override string Name => "Constantia";
    public override char Symbol => '♑'; // ♑ Capricorn — the mountain goat
    public override Vector4 Color => new(0.769f, 0.655f, 0.420f, 1.0f);           // pale bronze
    public override Vector4 BackgroundColor => new(0.102f, 0.078f, 0.031f, 1.0f); // dark bronze
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(1, 3);
    public override string FeelsLike => "It makes me feel harder than I was.";
}
