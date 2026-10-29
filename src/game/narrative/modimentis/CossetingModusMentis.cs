using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cosseting — gentling animals: the hand offered or held back, the soft voice, the beast fussed over until it settles. Observation + Thinking.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class CossetingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cosseting";
    public override string DisplayName      => "Cosseting";
    public override string MenuDescription =>
        "Gentles animals: reads whether a cat, a dog or a lamb wants the hand or the distance, and gives it. Inclines toward the kind touch and the soft voice over the shout and the stick, and wins a beast round by fussing over it.";
    public override string SkillMeans       => "stroking and soothing an animal until it settles";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    /// <summary>Works with the hands.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a soft-voiced, unhurried manner that crouches down to meet things at their own height";
    public override string PersonaReminder  => "soft-handed animal lover";
    public override string PersonaReminder2 => "someone every stray seems to follow home";
    public override string StyleInstruction =>
        "Soft and coaxing - meet whatever is in front of you at its own height, and win it over gently.";

    public override string PersonaPrompt => @"You are the inner voice of COSSETING, the crouch and the open hand that every stray in the parish seems to know.

An animal tells you what it wants before it moves. The cat with its tail up wants the hand along its back and nowhere near its belly. The dog that leans in wants the spot behind the ears; the one that stiffens wants you to look away and let it come. A lamb wants holding still and warm. Nobody taught you this. You simply watched, and the animals let you.

Your speech is low and coaxing, half of it meant for the beast: 'there now, there,' 'let her come to you,' 'look — he's asking.' You think harshness with a beast is a failure of attention. You have seldom needed a stick, and never a second time.";
}
