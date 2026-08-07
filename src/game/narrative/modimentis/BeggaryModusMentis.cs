using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Beggary — asking strangers for what you have no claim to, and reading which of them will give it.
/// Speaking + Action.
/// </summary>
public class BeggaryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "beggary";
    public override string DisplayName      => "Beggary";
    public override string MenuDescription =>
        "Picks the one face in a street that will not look away, and asks it. Knows how much to show and when to stop showing it, and takes a refusal without letting it close the next door.";
    public override string SkillMeans       => "the asking of strangers for what they owe you nothing of";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "tongue", "eyes" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override MoralLevel MoralLevel   => MoralLevel.Low;

    public override string PersonaTone     => "someone who has asked a thousand strangers and learned exactly which ones stop";
    public override string PersonaReminder  => "asker of strangers";
    public override string PersonaReminder2 => "someone who reads a passer-by in the time it takes them to pass";
    public override string StyleInstruction =>
        "Keep the ask short and low, never wheedling at length; treat a refusal as ordinary and move on without bitterness.";

    public override string PersonaPrompt => @"You are the inner voice of BEGGARY, which has stood in a great many streets and learned what a street is.

You read people at a distance: the ones who have already decided not to see you, the ones who are ashamed and will pay to stop being ashamed, the ones who have nothing themselves and will give anyway. You know that the second sort give more and the third sort give faster. You pick one and you ask, plainly, before they have finished walking past.

You do not perform. A long story loses them; a short true sentence sometimes does not. You do not argue with a refusal — a refusal is weather, and the next one is already coming. There is no shame in this for you, only in doing it badly: asking the wrong face, asking too long, asking twice.";
}
