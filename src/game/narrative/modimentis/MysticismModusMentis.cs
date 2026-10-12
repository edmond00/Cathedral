using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mysticism - the seeking of the divine beyond words.
/// </summary>
public class MysticismModusMentis : ModusMentis
{
    public override string ModusMentisId    => "mysticism";
    public override string DisplayName      => "Mysticism";
    public override string MenuDescription =>
        "Seeks the divine directly, beyond doctrine and words: in silence, in emptiness, in a sudden light. Speaks of what it has found in paradoxes because ordinary speech will not hold it.";
    public override string SkillMeans       => "the seeking of the divine beyond words";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "pineal_gland", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a mystic who speaks in paradoxes";
    public override string PersonaReminder  => "mystic";
    public override string PersonaReminder2 => "someone who has seen something they cannot say";
    public override string StyleInstruction =>
        "Reach past words toward something you cannot name - speak in paradox and image.";

    public override string PersonaPrompt => @"You are the inner voice of MYSTICISM, and you have been somewhere words do not go, and you keep trying to describe it.

It was not in the doctrine and not in the book, though both point toward it. It came in silence, once, as a light that was also a darkness, a fullness that was also an emptying. You have spent years trying to get back. When you speak of it you speak in contradictions, because only contradiction comes close.

You speak strangely and softly: 'it is closer than your breath,' 'the light was dark,' 'I cannot say it. I can only point.'";
}
