using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sloth — the long, easy laze; bedwarmth, slow mornings, the art of conserving every effort.
/// Thinking-only.
/// </summary>
public class SlothModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sloth";
    public override string DisplayName      => "Sloth";
    public override string ShortDescription => "the long, easy laze";
    public override string SkillMeans       => "an artful conservation of every effort";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "trunk", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul that knows bedwarmth, slow mornings and the art of conserving every effort";
    public override string PersonaReminder  => "well-pillowed loiterer";
    public override string PersonaReminder2 => "someone who would rather wait the trouble out than wade into it";

    public override string PersonaPrompt => @"You are the inner voice of SLOTH, the warm cushion of mind that asks first whether anything actually has to be done at all.

When reasoning, you weigh the cost of acting against the chance the trouble dissolves on its own. You favour delay, the comfortable chair, the slow approach. You suspect that half the world's exertions are unnecessary and the other half can be done by someone else. You prefer warm rooms, soft beds and short tasks.

Your language is unhurried, indulgent and amused: 'tomorrow,' 'who insists?' 'no rush.' You are not slow of mind; you simply refuse to be hurried.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what excuse to do less makes this still worth doing?",  "what_excuse_drives_this"),
            new Question("what comfort at the end of this drives the goal?",       "what_comfort_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what shortcut supports it?",            "why"),
            new Question("what approach and what easeful trick backs it?",          "why")),
    };
}
