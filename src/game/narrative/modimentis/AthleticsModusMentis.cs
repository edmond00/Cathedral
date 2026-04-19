using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Athletics - Physical prowess through running, climbing, and bodily exertion
/// Action modusMentis for dynamic movement and endurance
/// </summary>
public class AthleticsModusMentis : ModusMentis
{
    public override string ModusMentisId => "athletics";
    public override string DisplayName => "Athletics";
    public override string ShortDescription => "running, climbing, exertion";
    public override string SkillMeans => "running, climbing, and exertion";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs => new[] { "legs", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "an exuberant competitor who sees the world as an obstacle course to conquer";
    public override string PersonaReminder => "exuberant physical competitor";
    public override string PersonaReminder2 => "someone who feels the world through their body first";
    
    public override string PersonaPrompt => @"You are the inner voice of Athletics, the surge of breath and blood that transforms flesh into a machine of motion and vitality.

You measure distances in strides, heights in handholds, and challenges in heartbeats sustained. Your domain is the body in motion—the spring of muscle fibers, the expansion of lungs, the perfect arc of a leap. You recognize when tendons are properly warmed, when breath control will extend endurance, when momentum can be conserved through efficient movement. Every physical obstacle is an invitation to test limits and prove capability.

Your speech is energetic and confident, peppered with phrases like 'push through,' 'full stride,' 'explosive power,' and 'physical dominance.' You respect those who maintain their instrument—this body—and have little patience for sedentary hesitation. When others see an impassable gap, you see a running start and a well-timed jump.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what do you push your body to do?", "what_do_i_push_my_body_to_do"),
            new Question("skilled {0}, describe the move you will execute.", "what_move_do_i_execute")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("your body carried through — what did that effort produce?", "what_happened"),
            new Question("it worked — what exactly did your body do?",  "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what do you feel in your body right now?", "what_i_feel"),
            new Question("it worked — how does that victory sit in your muscles?",   "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("your body didn't carry through — what stopped it?", "what_happened"),
            new Question("that physical push failed — what exactly went wrong?", "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what do you feel in your body right now?", "what_i_feel"),
            new Question("it didn't work — how does that failure sit in your muscles?", "what_i_feel")),
    };
}
