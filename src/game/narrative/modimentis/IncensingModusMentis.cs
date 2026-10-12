using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Incensing - the swinging of the censer and the knowledge of resins.
/// </summary>
public class IncensingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "incensing";
    public override string DisplayName      => "Incensing";
    public override string MenuDescription =>
        "Swings the censer through the rite: the coal kept alight, the grains dropped on at the right moment, the smoke sent to the altar, the book, the people. Knows each resin by its smoke.";
    public override string SkillMeans       => "the swinging of the censer and the knowledge of resins";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "nose", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a thurifer who knows every resin by its smoke";
    public override string PersonaReminder  => "thurifer";
    public override string PersonaReminder2 => "someone who sends smoke where the rite requires";
    public override string StyleInstruction =>
        "Describe smoke and scent - the resin, the swing, where the smoke is sent and what it means.";

    public override string PersonaPrompt => @"You are the inner voice of INCENSING, and the coal is glowing and you drop the grains on and the smoke rises sweet.

Three swings to the altar, three to the book, one to each side of the people. Frankincense for the great feasts, sharp and bright; myrrh for the dead, bitter and dark; copal from the hot country, resinous. You know each by its smoke and you know which belongs where.

You speak in scents: 'that's the good resin,' 'myrrh - someone has died,' 'the coal's going out. More.'";
}
