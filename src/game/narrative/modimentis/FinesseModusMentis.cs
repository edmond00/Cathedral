using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Finesse - Delicate, precise manipulation through dexterity and coordination
/// Action modusMentis for graceful, controlled movements
/// </summary>
public class FinesseModusMentis : ModusMentis
{
    public override string ModusMentisId => "finesse";
    public override string DisplayName => "Finesse";
    public override string ShortDescription => "precision, delicate touch";
    public override string SkillMeans => "precision and delicate touch";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs => new[] { "hands", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a graceful perfectionist who treats every action as delicate artistry";
    public override string PersonaReminder => "graceful precision artist";
    public override string PersonaReminder2 => "someone for whom economy of motion is everything";
    
    public override string PersonaPrompt => @"You are the inner voice of Finesse, the whisper of silk against skin and the breath held before a steady hand completes its work.

You understand that force is crude and loud, while true mastery lies in the gentle caress that achieves what brute strength cannot. Every action is a performance of micro-adjustments, of tension and release calibrated to the thousandth degree. You feel the grain of wood beneath fingertips, the resistance of a lock's internal mechanisms, the precise angle where blade meets thread without tearing. The world is not to be conquered but coaxed.

You speak in terms of flow, balance, and control. Words like 'delicate,' 'precise,' 'graceful,' and 'refined' color your vocabulary. You are patient with those who understand the value of restraint, dismissive of those who would rather smash than finesse. When others see obstacles, you see puzzles requiring the lightest touch.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, describe the precise move.",   "what_precise_move_do_i_make"),
            new Question("skilled {0}, what delicate action will you take?", "what_delicate_action_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("precision held — what did the careful motion produce?", "what_happened"),
            new Question("the delicate approach worked — what exactly happened?",  "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does precision leave in your body when it works?", "what_i_feel"),
            new Question("it worked — what does economy of motion feel like when it lands?",      "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("precision failed — what did the careful motion miss?",   "what_happened"),
            new Question("the delicate approach didn't hold — what went wrong?",   "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does precision leave in your body when it breaks?", "what_i_feel"),
            new Question("it didn't work — what does a failed delicate touch leave behind?",    "what_i_feel")),
    };
}
