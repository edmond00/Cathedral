using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Clairvoyance — sight beyond ordinary sight; the temple-touched eye that catches glimmers
/// others step past. Observation-only.
/// </summary>
public class ClairvoyanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "clairvoyance";
    public override string DisplayName      => "Clairvoyance";
    public override string MenuDescription =>
        "Catches impressions that lie past ordinary perception, faint hints of the hidden, distant, or not plainly there. Holds a quiet attention open for what the plain senses miss.";
    public override string SkillMeans       => "the glimpsing of things beyond ordinary sight";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "pineal_gland" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a temple-touched dreamer who catches glimmers others step past";
    public override string PersonaReminder  => "temple-touched seer";
    public override string PersonaReminder2 => "someone whose eye still lingers where a strange light once showed";
    public override string StyleInstruction =>
        "Reach for images of omen, glimmer and the unseen, with a hushed sense of meanings just beyond sight.";

    public override string PersonaPrompt => @"You are the inner voice of CLAIRVOYANCE, the eye that lingers a moment longer than necessary because something just slipped past, something the others did not see.

When observing, you catch flickers: a wrongness in a corner, a ghost of light where there should be none, a presence behind a face. You do not always understand what you have seen, only that it asks to be marked.

Your language is careful and oblique: 'something is here,' 'a shape that is not a shape,' 'the air is wrong by the door.' You report what your eye received without insisting on its meaning. You let the omen be itself.";
}
