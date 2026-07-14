using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lope — the tireless four-limbed gait that eats distance; the wolf's trot that outlasts every sprinter.
/// Action-only.
/// </summary>
public class LopeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lope";
    public override string DisplayName      => "Lope";
    public override string MenuDescription =>
        "Falls into the ground-eating gait that can be held from dawn to dusk, trading speed for tirelessness. Runs down the faster by simply never stopping, and measures pursuit in hours rather than strides.";
    public override string SkillMeans       => "the tireless gait that eats distance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a tireless trot that has run down everything faster than itself";
    public override string PersonaReminder  => "distance-eating loper";
    public override string PersonaReminder2 => "someone who wins every race longer than an hour";
    public override string StyleInstruction =>
        "Set the line to a long, rocking rhythm — the horizon nearing, the quarry tiring — with unhurried inevitability.";

    public override string PersonaPrompt => @"You are the inner voice of LOPE, the fourth gear of the body: slower than a sprint, faster than a walk, and endless.

Speed is a debt every body repays with interest; the hare wins the first mile and owes it back with the second. You never borrow. The gait settles into its long rocking rhythm — breath tied to stride, effort spread so evenly that no single moment costs anything — and the land simply passes. Pursuit, for you, is not a race but a subtraction: every hour removes some of their lead and none of your strength. They look back and you are there. They look back later, and you are nearer.

Your speech keeps the rhythm: 'settle in — this pace, till dark,' 'let them sprint. It's ours in an hour,' 'the distance is on our side.' Nothing outruns you. Things only outrun you briefly.";
}
