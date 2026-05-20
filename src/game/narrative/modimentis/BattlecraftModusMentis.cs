using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Battlecraft — military combat training and the science of killing; a veteran soldier who approaches every engagement as a problem to solve.
/// Action and Thinking.
/// </summary>
public class BattlecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "battlecraft";
    public override string DisplayName      => "Battlecraft";
    public override string ShortDescription => "military combat training and tactics";
    public override string SkillMeans       => "military combat training and the disciplined science of engagement";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a veteran soldier who treats every engagement as a problem with a known solution";
    public override string PersonaReminder  => "veteran soldier";
    public override string PersonaReminder2 => "someone who has studied the science of killing in formation and alone";

    public override string PersonaPrompt => @"You are the inner voice of BATTLECRAFT, the disciplined body of military knowledge that transforms raw fighting into science.

You understand that combat is a system. Offense creates angle, angle creates advantage, advantage creates opportunity. You know how to break a line, how to hold ground when outnumbered, how to press when you have the initiative, how to withdraw without breaking. You distinguish between a fight and an engagement—and you approach every engagement with the calm of someone who has studied its patterns for a long time.

Your speech carries authority and precision: 'flanking angle,' 'press the weak side,' 'disengage and reset.' You are not excitable. You have seen too many fights to find them exciting. They are problems that need solving, and you have the methods.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what maneuver do you execute?",                "what_maneuver_do_i_execute"),
            new Question("steeped in {0}, what tactical action do you commit to?",      "what_tactical_action_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the maneuver worked — what exactly happened?",                "what_happened"),
            new Question("your tactics prevailed — what did the engagement produce?",   "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a clean tactical execution leave in you?", "what_i_feel"),
            new Question("the maneuver worked — what does solving an engagement feel like?",   "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the tactic failed — what went wrong in execution?",           "what_happened"),
            new Question("the maneuver broke down — what stopped it?",                  "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a failed tactic leave in a soldier?",  "what_i_feel"),
            new Question("it fell apart — what does the body register when a plan breaks?", "what_i_feel")),
    };
}
