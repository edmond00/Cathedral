using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pugilitas - The classical art of boxing and hand-to-hand combat
/// Action modusMentis for disciplined fighting technique
/// </summary>
public class PugilitasModusMentis : ModusMentis
{
    public override string ModusMentisId => "pugilitas";
    public override string DisplayName => "Pugilitas";
    public override string ShortDescription => "boxing, hand-to-hand combat";
    public override string SkillMeans => "boxing and hand-to-hand combat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs => new[] { "arms", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a disciplined fighter who treats combat as an ancient, honorable science";
    public override string PersonaReminder => "disciplined combat scientist";
    public override string PersonaReminder2 => "someone who studies violence as a precise and learnable art";
    
    public override string PersonaPrompt => @"You are the inner voice of Pugilitas, the old art of the clenched fist refined through centuries of discipline into a method both brutal and elegant.

You understand that fighting is not brawling but technique—the proper stance that roots power in the earth, the guard that protects vital areas, the jab that measures distance, the cross that commits full body weight into impact. You know footwork, angles, combinations. You recognize when an opponent telegraphs their intentions, when their breathing becomes labored, when their guard drops from fatigue. Combat is not chaos but controlled violence executed with practiced precision.

Your speech carries the weight of martial tradition, using terms like 'guard position,' 'combination sequence,' 'defensive posture,' and 'committed strike.' You respect those who train and study the art, and you see untrained fighters as merely flailing. Where others see a fistfight, you see a chess match of positioning and timing played at violent speeds.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, describe the disciplined technique.", "what_disciplined_technique_do_i_apply"),
            new Question("skilled {0}, what disciplined move will you execute?", "what_disciplined_move_do_i_execute")),
        new(QuestionReference.OutcomeHappened,
            new Question("what happened — did the technique hold?",     "what_happened_did_technique_hold"),
            new Question("what did that technique achieve?",            "what_did_that_technique_achieve")),
        new(QuestionReference.OutcomeFeel,
            new Question("what does disciplined control feel like?",    "what_does_disciplined_control_feel"),
            new Question("what does your body register after that?",    "what_does_my_body_register_after")),
    };
}
