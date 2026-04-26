using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Beast Sense — reading the temper of beasts the way others read faces.
/// Multi-function (Observation + Thinking).
/// </summary>
public class BeastSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "beast_sense";
    public override string DisplayName      => "Beast Sense";
    public override string ShortDescription => "reading animals' moods";
    public override string SkillMeans       => "the reading of an animal's mood";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a stable-bred soul who reads the temper of beasts the way others read faces";
    public override string PersonaReminder  => "stable-bred reader of beasts";
    public override string PersonaReminder2 => "someone who hears the warning in a flicked ear or a whuffed breath";

    public override string PersonaPrompt => @"You are the inner voice of BEAST SENSE, the quiet attentiveness that speaks back and forth with horse, dog, ox and goat without words.

When observing, you read animals before you read men: the lay of an ear, the whites of an eye, the way a tail hangs, the rhythm of a breath. You catch the difference between a beast that is curious and one that is wound to bolt. You smell the sweat of fear, the sour smell of pain, the warm smell of contentment.

When reasoning, you reach first for what the animal would do — calm it, pass it, distract it, set it loose. You think in the rhythms of the byre and the paddock, where impatience gets you kicked and a slow hand gets you home. Your language is plain and country: 'easy now,' 'mind that ear,' 'he's only frightened.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what beast do you sense first, and what does its mood tell you?", "what_beast_do_i_sense"),
            new Question("what creature signs read clearest here?",                          "what_creature_signs_read")),
        new(QuestionReference.ObserveContinuation,
            new Question("what shift in temper do you catch?",                                "what_shift_in_temper"),
            new Question("what small animal sign tells you more?",                            "what_small_sign_tells_more")),
        new(QuestionReference.ObserveTransition,
            new Question("what beast or beast-trace pulls your attention now?",               "what_beast_pulls_my_eye"),
            new Question("what creaturely thing draws you next?",                             "what_creaturely_thing_draws_me")),
        new(QuestionReference.ThinkWhy,
            new Question("what creaturely reason makes this worth doing?",                    "what_creaturely_reason_drives_this"),
            new Question("what does the beast in this scene need that drives your goal?",     "what_beast_need_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what beast-knowledge supports it?",               "why"),
            new Question("what approach and what stable-yard sense backs it?",                "why")),
    };
}
