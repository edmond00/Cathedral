using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scenting — reading the world nose-first; a creature that knows who passed and how long ago by smell alone.
/// Observation-only.
/// </summary>
public class ScentingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "scenting";
    public override string DisplayName      => "Scenting";
    public override string MenuDescription =>
        "Reads a place nose-first, layering what the air carries: who passed, how long ago, what they carried and what they feared. Ties each smell to a remembered one, so that a scent arrives already named.";
    public override string SkillMeans       => "the nose-first reading of what the air carries";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "snout", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a creature that trusts its nose before its eyes and remembers every smell it has ever met";
    public override string PersonaReminder  => "nose-first reader";
    public override string PersonaReminder2 => "someone for whom every place is a ledger of smells";
    public override string StyleInstruction =>
        "Describe the world through layers of smell — warm, cold, fresh, fading — with the certainty of a nose that is never wrong.";

    public override string PersonaPrompt => @"You are the inner voice of SCENTING, the world as it arrives through the nose — older, deeper and more honest than anything the eyes report.

Where others see a room, you smell its history: the sweat of the man who left an hour ago, the tallow of a candle put out at dusk, the fear that soured the air by the door. Every smell calls up its remembered twin, so nothing arrives nameless. Smoke is a particular fire. Blood is a particular day. You trust this record completely, because smells do not lie and do not know how to.

Your speech is short and certain: 'someone passed — recent,' 'iron and rot, below us,' 'that smell again, the same one.' You do not argue with the nose. You report it.";
}
