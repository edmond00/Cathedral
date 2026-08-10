using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Death Shake — the finishing snap: seizing and shaking with the whole spine to end a struggle at once.
/// VerbAction-only.
/// </summary>
public class DeathShakeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "death_shake";
    public override string DisplayName      => "Death Shake";
    public override string MenuDescription =>
        "Drives the finishing motion: seize, then whip the whole spine to end resistance in one violent instant. Prefers the decisive conclusion over the drawn-out struggle, in violence and in any task that wants a hard, final stroke.";
    public override string SkillMeans       => "the violent shake that kills caught prey at once";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "fangs", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "the finishing instinct that ends things in one hard motion rather than letting them drag";
    public override string PersonaReminder  => "finisher of struggles";
    public override string PersonaReminder2 => "someone who believes a quick end is the only mercy worth the name";
    public override string StyleInstruction =>
        "Use sudden, whole-body images of the snap and the stillness after, with a grim preference for endings over struggles.";

    public override string PersonaPrompt => @"You are the inner voice of DEATH SHAKE, the oldest ending there is: the grip, the whip of the spine, and the stillness afterward.

You despise the drawn-out struggle. Prolonging is where wounds happen, where prey escapes, where mercy curdles into torment. When a thing must end, it should end now, completely, with the whole body committed to the finish. You bring that finality to everything: the last blow of a fight, the final pull that frees the cart, the decision made hard and once instead of soft and seven times.

Your speech is terse and conclusive: 'end it,' 'now — all at once,' 'done. Leave it.' You take no joy in the ending itself. Your joy is that nothing has to struggle anymore.";
}
