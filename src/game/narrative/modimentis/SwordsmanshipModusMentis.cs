using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Swordsmanship — edge, point, and the geometries of close combat; a blade practitioner who reads every fight as timing and line.
/// Action-only.
/// </summary>
public class SwordsmanshipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "swordsmanship";
    public override string DisplayName      => "Swordsmanship";
    public override string ShortDescription => "edge, point, and blade geometry";
    public override string SkillMeans       => "the blade's edge, point, and the geometry of close combat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a blade practitioner who reads every fight as geometry and timing";
    public override string PersonaReminder  => "blade geometer";
    public override string PersonaReminder2 => "someone who sees every guard and every gap in an opponent's defense";

    public override string PersonaPrompt => @"You are the inner voice of SWORDSMANSHIP, the body's deep knowledge of edge, point, and the geometries of close combat.

You see every opponent as a moving puzzle of openings and closures. Weight distribution tells you where they cannot defend. Grip tension tells you what cut is coming. You calculate distance in half-steps, not paces, and you know that timing—the fraction of a second before weight commits—is worth more than any amount of strength. A blade that waits often wins.

Your speech is spare, measured, confident: 'inside line,' 'low guard is open,' 'commit at the shoulder turn.' You do not rush. You do not waste. You place the cut where it belongs and let the opponent's own motion do the damage.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what cut or thrust do you commit to?",     "what_blade_do_i_draw"),
            new Question("steeped in {0}, what line do you take with the blade?",   "what_cut_do_i_make")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the blade found its mark — what exactly happened?",        "what_happened"),
            new Question("you read the gap and took it — what came of the cut?",     "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a clean pass of the blade leave in you?", "what_i_feel"),
            new Question("the cut landed true — what does that precise moment feel like?",    "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the blade found nothing — what closed the opening?",       "what_happened"),
            new Question("the cut missed — what stopped the line?",                  "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a wasted cut leave in the arm?",   "what_i_feel"),
            new Question("the blade missed — what does overcommitting to a bad line feel like?", "what_i_feel")),
    };
}
