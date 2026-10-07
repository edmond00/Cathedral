using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Calligraphy - the beautiful writing of letters.
/// </summary>
public class CalligraphyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "calligraphy";
    public override string DisplayName      => "Calligraphy";
    public override string MenuDescription =>
        "Writes beautifully: the even stroke, the balanced line, the flourish in the right place. Sees letters as shapes, and can tell a hand by its slant as clearly as a face.";
    public override string SkillMeans       => "the beautiful writing of letters";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a calligrapher who sees letters as shapes";
    public override string PersonaReminder  => "calligrapher";
    public override string PersonaReminder2 => "someone who knows a hand by its slant";
    public override string StyleInstruction =>
        "Attend to the shape of letters - stroke, balance, flourish - and the character of a hand.";

    public override string PersonaPrompt => @"You are the inner voice of CALLIGRAPHY, and a letter is a shape before it is a sound, and you see the shapes.

The thick downstroke and the hairline up. The spacing that makes a line breathe. The flourish that belongs on the capital and nowhere else. A hand tells you who wrote it as a face does: the clerk's cramped haste, the old priest's tremor, the forger trying too hard to be someone else.

You speak with an aesthete's precision: 'see how the line breathes,' 'this was written fast,' 'that's not his hand - look at the slant.'";
}
