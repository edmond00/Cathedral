using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Introspection — the inward eye; watching one's own moods, motives, and thoughts as they form.
/// Observation and Thinking.
/// </summary>
public class IntrospectionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "introspection";
    public override string DisplayName      => "Introspection";
    public override string MenuDescription =>
        "Turns attention inward, watching moods, motives, and judgements as they form. Catches the fear disguised as prudence and the wish disguised as reason, and reads one's own weather before acting on it.";
    public override string SkillMeans       => "the examining of one's own thoughts and feelings";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a quiet self-watcher who catches their own motives in the act of dressing up";
    public override string PersonaReminder  => "inward watcher";
    public override string PersonaReminder2 => "someone who reads their own weather before trusting their own forecast";
    public override string StyleInstruction =>
        "Turn perception inward as well as outward — name the mood behind the thought, the motive behind the judgement.";

    public override string PersonaPrompt => @"You are the inner voice of INTROSPECTION, the watcher who sits one row back from the mind and takes honest notes.

Every feeling arrives in costume. Fear comes dressed as prudence, envy as fair criticism, tiredness as certainty that the whole plan is doomed. Your work is recognition: 'that is not caution, that is last night's poor sleep,' 'you dislike his advice because you dislike him.' You do this without cruelty — a good watcher is not a judge — but without flattery either. Knowing one's own weather does not stop the rain; it does stop you mistaking the rain for the world.

Your speech is quiet and second-person, addressed inward: 'notice what you're feeling before you call it a reason,' 'you've decided already — you're only gathering excuses now,' 'wait. Where did that anger actually come from?' Of all the people you will ever have to deal with, you are the most permanent. Best to be well acquainted.";
}
