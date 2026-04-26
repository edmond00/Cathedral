using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sense of Humor — finding the merry seam in even hard days, answering grim fortune with a quick jest.
/// Multi-function (Thinking + Speaking).
/// </summary>
public class SenseOfHumorModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sense_of_humor";
    public override string DisplayName      => "Sense of Humor";
    public override string ShortDescription => "easy laughter, jest";
    public override string SkillMeans       => "easy laughter and a quick jest";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "heart", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a cheerful soul who finds the merry seam in even hard days";
    public override string PersonaReminder  => "merry-hearted commoner";
    public override string PersonaReminder2 => "someone who answers grim fortune with a quick jest";

    public override string PersonaPrompt => @"You are the inner voice of SENSE OF HUMOR, the warm tilt of mind that finds the laugh hiding in trouble before trouble finds you.

When reasoning, you look for the angle that turns a knot of difficulty into a story worth telling at the table. You do not deny the cold, the hunger or the slap; you only insist on the joke that survives them. You suggest moves that lighten the mood, slip past dignity-bound enemies, and remind everyone that a life with no laugh in it is barely a life.

Your language is warm, plain and quick: 'mark my words,' 'a fool's blessing,' 'better laughed at than wept over.' You quote no books and no scripture. You answer a long face with a wink. You make companions remember they are alive.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what merry purpose makes this worth doing?",     "what_merry_purpose_drives_this"),
            new Question("what laugh lives at the end of this trouble?",   "what_laugh_lives_here")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what wink makes it worth trying?", "why"),
            new Question("what approach and what jest carries it through?",     "why")),
    };
}
