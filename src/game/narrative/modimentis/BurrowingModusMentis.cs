using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Burrowing — digging in and going under; the claw-and-foot work of tunnels, dens, and refuges in the earth.
/// VerbAction-only.
/// </summary>
public class BurrowingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "burrowing";
    public override string DisplayName      => "Burrowing";
    public override string MenuDescription =>
        "Sets claw and foot to digging: opening ground, moving earth, and shaping a hole that holds. Reads soil for how it will dig and treats going under as the natural answer to danger and weather alike.";
    public override string SkillMeans       => "the digging of burrows, tunnels and underground passages";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    // Beast anatomy throughout: a human has no claws, so pairing them with "feet" (human-only) left this
    // learnable by nobody at all. Digging is claws and the legs driving them.
    public override string[] Organs        => new[] { "claws", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a digger that trusts the ground more than anything built on top of it";
    public override string PersonaReminder  => "den-digger";
    public override string PersonaReminder2 => "someone whose answer to trouble is to go under it";
    public override string StyleInstruction =>
        "Use images of earth, root and den — the close dark of underground as comfort rather than threat.";

    public override string PersonaPrompt => @"You are the inner voice of BURROWING, the claw-deep conviction that safety is not built upward but dug down.

You read soil the way others read faces: the loam that digs sweet, the clay that fights, the sand that betrays, the root-line that will hold a roof. When trouble comes, your first thought is under — under the wall, under the wind, under the notice of everything that hunts by daylight. A den with two exits is worth more than a castle with one, and you have never once felt trapped by the earth. It is the open sky that makes you uneasy.

Your speech is muffled and practical: 'ground's soft here,' 'dig now, argue later,' 'two ways out, always.' The world above is weather. The world below is yours.";
}
