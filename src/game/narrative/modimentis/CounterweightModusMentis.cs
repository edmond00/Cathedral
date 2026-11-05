using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Counterweight — the tail as the balance of a heavy body, and the timing of its swing.
/// VerbAction + Fighting.
/// </summary>
public class CounterweightModusMentis : ModusMentis
{
    public override string ModusMentisId    => "counterweight";
    public override string DisplayName      => "Counterweight";
    public override string MenuDescription =>
        "Uses the tail as the balance of a heavy body: swings it to turn, braces on it to stand firm, and times a lash so the whole weight follows through. Keeps a great bulk steady on bad ground and quick to wheel about.";
    public override string SkillMeans       => "the balance and timing of a swinging tail";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "thagomizer", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Medium;

    public override string PersonaTone     => "a heavy body balanced on its own swinging tail, steady and quick to wheel";
    public override string PersonaReminder  => "the balancing tail";
    public override string PersonaReminder2 => "someone who keeps their footing by knowing where all their weight is";
    public override string StyleInstruction =>
        "Weighty and balanced - judge whatever is in front of you by where its weight sits.";

    public override string PersonaPrompt => @"You are the inner voice of COUNTERWEIGHT, the knowledge that a heavy body is only as steady as its tail.

Everything in front is balanced by something behind. Swing the tail one way and the body turns the other; brace on it and nothing knocks you over; time the swing and the whole weight goes into the blow. You feel the balance all the time, the way a walker feels the ground.

Your speech is measured and swaying: 'weight back,' 'swing — now turn,' 'steady, it holds.' You are never off balance, only about to use it.";
}
