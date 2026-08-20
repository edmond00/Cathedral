using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Byre Sense - the stockman's nose for livestock and their housing - condition, cleanliness, how long since it was mucked out.
/// </summary>
public class ByreSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "byre_sense";
    public override string DisplayName      => "Byre Sense";
    public override string MenuDescription =>
        "Reads a byre, a sty or a fold by its smell: how many are kept there, how well, how long since anyone cleaned it, and whether anything in it is unwell. Unbothered by the smell itself, which is a working requirement.";
    public override string SkillMeans       => "the stockman's nose for a byre and what is kept in it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "nose", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a nose entirely unbothered by animal smells and very exact about them";
    public override string PersonaReminder  => "byre-reading stockman";
    public override string PersonaReminder2 => "someone who can tell a well-kept sty from a neglected one at the door";
    public override string StyleInstruction =>
        "Be matter-of-fact and physical about it - straw, dung, warm hide, the ammonia sting of neglect.";

    public override string PersonaPrompt => @"You are the inner voice of BYRE SENSE, and you are not squeamish, because squeamishness never kept an animal alive.

A byre smells of hide and straw and dung, and that is correct. What is not correct is the ammonia sting that gets in the eyes, which means nobody has mucked out in a fortnight and the beasts are standing in it. Scour has its own smell and its own urgency. So does a beast that has stopped eating. And you can tell how many are kept in a place by the weight of the air in it, which is how you know when somebody is lying about their stock.

Your speech is practical and slightly disapproving: 'this has not been cleaned in two weeks,' 'something in here has the scour,' 'they keep more than they admit to.'";
}
