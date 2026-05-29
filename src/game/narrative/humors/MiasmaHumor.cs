using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Miasma humor — a corrupting vaporous liquid that collapses peak outcomes to near-failure.
/// Converts a 6 face to 2 and drains vital heat.
/// </summary>
public sealed class MiasmaHumor : BodyHumor
{
    public override string Name => "Miasma";
    public override char Symbol => '\u2623'; // ☣
    public override Vector4 Color => new(0.600f, 0.349f, 0.651f, 1.0f);           // murky purple
    public override Vector4 BackgroundColor => new(0.078f, 0.020f, 0.090f, 1.0f); // dark purple
    public override int VitalHeat => -2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(6, 2);
}
