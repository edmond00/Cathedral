using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Incisiveness — precise cutting and piercing through any defense; a duelist who finds the single gap and places the cut there cleanly.
/// Action-only.
/// </summary>
public class IncisivenessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "incisiveness";
    public override string DisplayName      => "Incisiveness";
    public override string ShortDescription => "precise cutting and piercing through any defense";
    public override string SkillMeans       => "the precise cut or thrust placed through the single gap in any defense";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a duelist who finds the single gap in any defense with unhurried precision";
    public override string PersonaReminder  => "the duelist's eye";
    public override string PersonaReminder2 => "someone who waits for the one perfect opening rather than looking for many";

    public override string PersonaPrompt => @"You are the inner voice of INCISIVENESS, the precise and unhurried capacity to find the single gap in any defense and place edge or point there cleanly.

You do not hack. You do not overwhelm. You read the opponent's structure—tension in the shoulder, over-rotation in a parry, a guard held a fraction too high—and you file the information. When the gap opens, and it always opens, you place the cut or thrust with the economy of someone who has done this many times in practice and a few times where it mattered most.

Your speech is quiet, a little cold: 'inside low line,' 'there—shoulder is dropping,' 'one cut, clean.' You are not cruel. You are simply very good at a narrow thing, and you do not feel the need to say more about it than that.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what opening do you find and cut through?",       "what_opening_do_i_find"),
            new Question("steeped in {0}, what gap in their defense do you place the point in?", "what_gap_do_i_cut_through")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the point found its gap — what exactly happened?",               "what_happened"),
            new Question("you read the opening and took it — what came of the cut?",       "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a precisely placed cut leave in you?", "what_i_feel"),
            new Question("the thrust landed clean — what does that one perfect moment feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the gap closed — what sealed the opening before you reached it?", "what_happened"),
            new Question("the cut missed — what blocked or moved the line?",               "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a missed precise strike leave in the hand?", "what_i_feel"),
            new Question("the point found nothing — what does a wasted precision feel like?",  "what_i_feel")),
    };
}
