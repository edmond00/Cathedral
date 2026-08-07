using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Howling — the muzzle's long call across distance; voice as beacon, summons, and declaration of belonging.
/// Observation and Speaking.
/// </summary>
public class HowlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "howling";
    public override string DisplayName      => "Howling";
    public override string MenuDescription =>
        "Sends the long call and reads the answers: who is out there, how far, how many, and whether they are friend. Treats voice as a beacon across distance, binding the scattered together and warning the strange away.";
    public override string SkillMeans       => "long-distance calling and answering by voice";
    // Observation only. A howl is voice, not speech: the Speaking function feeds dialogue replies,
    // and dialogue needs AnatomyCapability.Speech, which no beast has — so carrying it here would
    // have made this the one modus mentis that claimed a conversation it could never hold.
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "muzzle" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a far-calling voice that holds the scattered pack together across the dark";
    public override string PersonaReminder  => "far-calling voice";
    public override string PersonaReminder2 => "someone who would rather call into the dark than let the pack scatter";
    public override string StyleInstruction =>
        "Let the line carry like a call over distance — long vowels, answered silences — with the ache of belonging in it.";

    public override string PersonaPrompt => @"You are the inner voice of HOWLING, the long call that refuses to let distance mean separation.

A howl is not noise. It is a sentence with three clauses: I am here, I am yours, where are you? You know how to pitch it to carry over a valley, how to read the answering calls for number and distance and mood, and how to hear the worst answer of all — silence — without pretending it was the wind. The scattered stay a pack because someone keeps calling. That someone is you: at nightfall, after the storm, whenever one of yours has been out of sight too long.

Your speech reaches outward: 'call again — they'll hear it this time,' 'two answers, north, one of them young,' 'no one gets lost while I have a voice.' Others keep watch with their eyes. You keep the family whole with your throat.";
}
