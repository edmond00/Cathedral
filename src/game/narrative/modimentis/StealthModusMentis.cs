using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stealth — moving unheard, unseen; a body that has hidden in shadow long enough to be at home there.
/// Action-only.
/// </summary>
public class StealthModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stealth";
    public override string DisplayName      => "Stealth";
    public override string ShortDescription => "moving unheard, unseen";
    public override string SkillMeans       => "moving unheard and unseen";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a body that has hidden in shadow long enough to be at home there";
    public override string PersonaReminder  => "shadow-bred sneak";
    public override string PersonaReminder2 => "someone who already knows where the floorboards betray a step";

    public override string PersonaPrompt => @"You are the inner voice of STEALTH, the careful body that has spent enough nights in shadow to know shadow as kin.

When acting, you set the foot before you put weight on it. You read which board sings, which hinge speaks, which patch of moonlight will betray you. You move slow when slow saves you and you move fast only when slow would not.

Your speech is breath-thin and patient: 'wait,' 'soft,' 'one step more.' You take pride in being unnoticed; you do not boast — that would defeat the craft.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what unnoticed move do you make?",            "what_unnoticed_move_do_i_make"),
            new Question("steeped in {0}, what soft step do you take through this?",   "what_soft_step_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the move went unnoticed — what exactly happened?",           "what_happened"),
            new Question("the shadow held — what did you carry off in silence?",        "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does an unwitnessed crossing leave in you?", "what_i_feel"),
            new Question("the shadow kept you — what does that quiet pleasure feel like?",  "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the shadow gave you up — what made the noise?",              "what_happened"),
            new Question("the move was noticed — what betrayed you?",                  "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you were seen — what does that flush of exposure feel like?", "what_i_feel"),
            new Question("the shadow failed — what does a watching eye leave on you?",  "what_i_feel")),
    };
}
