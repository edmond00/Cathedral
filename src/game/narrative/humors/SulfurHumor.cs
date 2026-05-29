using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Sulfur humor — a volatile inhalant that shatters near-peak results catastrophically.
/// Converts a 5 face to 1 (worst outcome) and severely drains vital heat.
/// </summary>
public sealed class SulfurHumor : BodyHumor
{
    public override string Name => "Sulfur";
    public override char Symbol => '\u25b3'; // △
    public override Vector4 Color => new(0.851f, 0.102f, 0.902f, 1.0f);           // vivid purple
    public override Vector4 BackgroundColor => new(0.102f, 0.008f, 0.122f, 1.0f); // deepest purple
    public override int VitalHeat => -4;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(5, 1);
}
