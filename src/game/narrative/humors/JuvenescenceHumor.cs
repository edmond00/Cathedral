using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Juvenescence humor — the vigour of youth. Never secreted by an organ; it is seeded into
/// the protagonist's queues at character creation in proportion to the heart-derived
/// <c>youthfulness</c> stat. Provides strong vital heat and nudges any dice result two steps higher.
/// </summary>
public sealed class JuvenescenceHumor : BodyHumor
{
    public override string Name => "Juvenescence";
    public override char Symbol => '\u2648'; // Aries — the spring / youth sign
    public override Vector4 Color => new(0.98f, 0.88f, 0.52f, 1.0f);              // bright warm gold
    public override Vector4 BackgroundColor => new(0.13f, 0.10f, 0.02f, 1.0f);    // deep amber
    public override int VitalHeat => 2;
    public override TransmutingVirtue? TransmutingVirtue => new NumericModVirtue(2);
}
