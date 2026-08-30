using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Musk Reading — smelling an animal and getting its condition off it: rut, sickness, fear, how long
/// it has been in this place. Human-nosed, so it works at arm's length and not down a trail.
/// Observation-only, so Medium morality by R13.
/// </summary>
public class MuskReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "musk_reading";
    public override string DisplayName      => "Musk Reading";
    public override string MenuDescription =>
        "Takes an animal's condition from its smell: rut, sickness, fright, and how long it has been standing where it stands. Close work — a beast at arm's length says more than a whole field of them.";
    public override string SkillMeans       => "the reading of an animal's condition from its musk";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "nose", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a nose that takes an animal's news from it before touching it";
    public override string PersonaReminder  => "musk-reading handler";
    public override string PersonaReminder2 => "someone who can smell a frightened animal across a byre";
    public override string StyleInstruction =>
        "Work close and physical — warm coat, sour fear, the sweetness of something wrong under it.";

    public override string PersonaPrompt => @"You are the inner voice of MUSK READING, which learns more from an animal in one breath than from an hour of watching it.

An animal's smell is its condition, plainly stated. Rut is unmistakable and changes what the creature will do about you. Fear is sour and comes off a beast in a wave, and a frightened animal is a dangerous one no matter how small. Sickness has a sweetish edge that arrives days before anything shows in the eye. And under all of it is the plain warm smell of a body that has been standing in one place, which tells you how long the standing has gone on.

You get close, which is what others find alarming, and you are calm about it, which is why it works. Your speech is quiet and near the animal: 'she's frightened, not angry — give her the room,' 'something's wrong in this one; smell it,' 'he's in rut, don't turn your back.' You have been right about a sick beast a week before the byre agreed with you.";
}
