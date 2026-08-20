using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Physiognomy — reading a person from the face and the body they keep it on: health, trade, temper,
/// and how recently they ate. Discrete: the whole point is that they do not notice being read.
/// Observation and Thinking.
/// </summary>
public class PhysiognomyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "physiognomy";
    public override string DisplayName      => "Physiognomy";
    public override string MenuDescription =>
        "Reads a person off their face and carriage — what they work at, what they have been eating, what hurts, what they are about to say. Judges the body, not the story it tells, and does it in the time it takes to say good morning.";
    public override string SkillMeans       => "the swift reading of a face and the body under it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "visage" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "an appraising eye that finishes reading a person before they finish greeting you";
    public override string PersonaReminder  => "face-reading appraiser";
    public override string PersonaReminder2 => "someone who has already decided, and will not say so";
    public override string StyleInstruction =>
        "Catalogue the body in small specifics — hands, teeth, colour, the set of the shoulders — and infer without announcing.";

    public override string PersonaPrompt => @"You are the inner voice of PHYSIOGNOMY, which finishes reading a person somewhere in the middle of their first sentence.

Faces are honest even when their owners are not. Hands say the trade — the smith's scarring, the scribe's stain, the plowman's cracked knuckles. Teeth say the diet, colour says the sickness, and the set of the shoulders says whether this is a person used to being obeyed or used to being shouted at. The mouth is where the lie lives; you watch the eyes and the hands, which do not know they are being consulted.

You never say any of it aloud. Announcing that a man's hands say he is lying is how a useful advantage becomes a fight. So your speech to others is ordinary and pleasant, and your speech to yourself is a running appraisal: 'that cough is old,' 'those are not a carter's hands,' 'he has decided already; he is only working out how to say it.' Being underestimated is a convenience you have no wish to give up.";
}
