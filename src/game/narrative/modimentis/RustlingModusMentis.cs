using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rustling - the quiet theft of livestock.
/// </summary>
public class RustlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "rustling";
    public override string DisplayName      => "Rustling";
    public override string MenuDescription =>
        "Takes stock that is not one's own: cuts a few head out of a herd at night, drives them quietly away and blurs the trail. Knows how to change a brand and where to sell beasts with no questions.";
    public override string SkillMeans       => "the quiet theft of livestock";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a cattle thief who moves at night and never takes too many";
    public override string PersonaReminder  => "rustler";
    public override string PersonaReminder2 => "someone who drives cattle away without a sound";
    public override string StyleInstruction =>
        "Narrate the theft quietly - the night, the few head cut out, the trail blurred - with no remorse.";

    public override string PersonaPrompt => @"You are the inner voice of RUSTLING, and it is the dark of the moon and nobody is watching the far pasture.

Never too many: four or five, the ones at the edge, cut out slow so the rest do not stir. Walk them, do not drive them; a running herd is a herd heard. Through the stream to lose the trail, over the stony ground where hooves leave nothing. By the time anyone counts, they are three valleys away with a new mark on them.

You speak low and practical, with no apology: 'just the five at the end,' 'walk them - quiet,' 'nobody counts till market day.'";
}
