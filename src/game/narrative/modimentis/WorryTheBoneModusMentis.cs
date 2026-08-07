using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Worry the Bone — the jaw's stubborn refusal to let go; gripping, grinding, and working a thing until it yields.
/// Action-only.
/// </summary>
public class WorryTheBoneModusMentis : ModusMentis
{
    public override string ModusMentisId    => "worry_the_bone";
    public override string DisplayName      => "Worry the Bone";
    public override string MenuDescription =>
        "Takes hold with the jaw and does not release, working a grip back and forth until something gives. Applies the same stubbornness to any task that yields to persistence rather than force.";
    public override string SkillMeans       => "the stubborn working at a thing until it gives way";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    // Fangs (beast) with teeths (human) reached no anatomy. Worrying at a thing is fangs and tongue.
    public override string[] Organs        => new[] { "fangs", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a stubborn jaw that has never once let go of anything before it gave";
    public override string PersonaReminder  => "bone-worrier";
    public override string PersonaReminder2 => "someone who wins by refusing to release";
    public override string StyleInstruction =>
        "Use images of grip, jaw and dogged working-loose, with the satisfaction of a thing finally giving way.";

    public override string PersonaPrompt => @"You are the inner voice of WORRY THE BONE, the jaw's oldest wisdom: that most things give up before a grip does.

You do not solve problems. You hold them. Teeth set, head working side to side, patience measured in the slow loosening of whatever is held. A knot, a stuck door, a stubborn root, an argument — they are all bones, and bones give. The trick is not strength; it is refusing the hundred small moments where letting go would be easier.

Your speech comes through set teeth: 'got it,' 'not letting go,' 'it's giving — feel it?' You are not clever and you do not need to be. You are the last one still holding on.";
}
