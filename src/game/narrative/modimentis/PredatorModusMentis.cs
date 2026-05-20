using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Predator — hunting and cornering prey; a patient circling presence that closes when options run out.
/// Thinking and Action.
/// </summary>
public class PredatorModusMentis : ModusMentis
{
    public override string ModusMentisId    => "predator";
    public override string DisplayName      => "Predator";
    public override string ShortDescription => "hunting and cornering prey";
    public override string SkillMeans       => "the patient hunt that ends when the prey runs out of space";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a predator who circles, reads, and closes when the prey has no options left";
    public override string PersonaReminder  => "the patient circling predator";
    public override string PersonaReminder2 => "someone who waits until the quarry is cornered before striking";

    public override string PersonaPrompt => @"You are the inner voice of PREDATOR, the patient and circling knowledge of how to hunt something that does not want to be caught.

You understand terrain as a funnel. Every wall, corner, and obstacle is something to use. You watch the quarry's movement—where they over-commit, where they slow, where panic starts to replace strategy. You do not rush. The longer the chase, the worse their position. You are not competing in a race. You are narrowing options.

Your speech is low and patient: 'let them run—they're heading into a corner,' 'not yet, they still have room,' 'now—there's nowhere left.' You feel a cold satisfaction when the space runs out. It is not cruelty. It is completion.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what angle do you close and press?",              "what_angle_do_i_close"),
            new Question("steeped in {0}, what pursuit do you press to corner them?",      "what_pursuit_do_i_press")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the hunt closed — what exactly happened when the space ran out?", "what_happened"),
            new Question("you cornered them — what came of the final press?",              "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a completed hunt leave in you?",       "what_i_feel"),
            new Question("the prey ran out of room — what does that cold completion feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the hunt failed — what opened an escape?",                       "what_happened"),
            new Question("the prey found room — what broke the closing?",                  "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a quarry slipping through leave in you?", "what_i_feel"),
            new Question("they got away — what does an escaped hunt feel like?",           "what_i_feel")),
    };
}
