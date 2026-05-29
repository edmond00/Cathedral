using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Choler humor — a state of wrath and bile-fueled aggression.
/// Converts a 6 face to 4, destabilising peak results, and drains vital heat.
/// </summary>
public sealed class CholerHumor : BodyHumor
{
    public override string Name => "Choler";
    public override char Symbol => '\u2642'; // ♂
    public override Vector4 Color => new(0.702f, 0.251f, 0.800f, 1.0f);           // bright purple
    public override Vector4 BackgroundColor => new(0.102f, 0.020f, 0.129f, 1.0f); // deep purple
    public override int VitalHeat => -2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(6, 4);
}
