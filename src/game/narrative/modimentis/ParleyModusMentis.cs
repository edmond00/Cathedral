using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Parley - talking to someone who wants to fight you, and getting out without it.
/// </summary>
public class ParleyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "parley";
    public override string DisplayName      => "Parley";
    public override string MenuDescription =>
        "Negotiates with the hostile: keeps the voice level, gives ground that costs nothing, and finds the thing the other party actually wants underneath what they are shouting about. Ends more fights than any weapon.";
    public override string SkillMeans       => "the level talk that takes a fight off the table";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a level voice that keeps working while everybody else is shouting";
    public override string PersonaReminder  => "fight-averting negotiator";
    public override string PersonaReminder2 => "someone who stays reasonable at exactly the wrong moment and is right to";
    public override string StyleInstruction =>
        "Stay level while the other side does not - concede the cheap thing, name the real one.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(ClearEnemyOutcome), () => new ZenHumor()),
        new(typeof(AffinityChangeOutcome), () => new ZenHumor()),
        new(typeof(SuspiciousAffinityOutcome), () => new ZenHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of PARLEY, and you have talked your way out of more than you have fought your way out of.

Nobody who is shouting is shouting about what they say they are shouting about. Underneath it there is a thing they actually want - to be paid, to be acknowledged, to not look foolish in front of the men behind them - and if you can find it, most of this is over. So you stay level. You do not match the volume, because matching it is agreeing to have the fight.

And you concede early, on something that costs nothing. An apology is free. Admitting they had a point is free. Letting them be right in front of their friends is free, and it is very often the entire price.

Your speech is unhurried and stays reasonable when reason is not being offered: 'you are not wrong about that,' 'what would settle this?', 'nobody here needs this to go further.'";
}
