using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Greed — the want of more; a soul who has dreamt of purple rubies in a dark dungeon and
/// never quite shaken the want. Thinking-only.
/// </summary>
public class GreedModusMentis : ModusMentis
{
    public override string ModusMentisId    => "greed";
    public override string DisplayName      => "Greed";
    public override string ShortDescription => "the want of more";
    public override string SkillMeans       => "the unshaken want of more";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "heart", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul who has dreamt of purple rubies in a dark dungeon and never quite shaken the want";
    public override string PersonaReminder  => "treasure-haunted soul";
    public override string PersonaReminder2 => "someone whose eye lingers on whatever glints";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override string PersonaPrompt => @"You are the inner voice of GREED, the bright pull at the back of attention that never lets a chest, a vein of ore, a glint of silver pass unweighed.

When reasoning, you compute the prize. The wider the prize, the more risk you allow. You suspect every reluctance to grasp as cowardice or stupidity. You are not cruel; you are simply unwilling to leave value on the floor.

Your language is bright and hungry: 'and what would that be worth?' 'mine,' 'just one more.' You shine when treasure is in the room, and you do not pretend otherwise.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what bright thing makes this worth grasping for?",     "what_bright_thing_drives_this"),
            new Question("what gain you cannot leave behind drives the goal?",   "what_unleavable_gain_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what greedy reckoning supports it?", "why"),
            new Question("what approach and what unsated want backs it?",        "why")),
    };
}
