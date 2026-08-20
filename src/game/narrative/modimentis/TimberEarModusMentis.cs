using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Timber Ear - hearing structures under strain - roof, rope, ice, ladder - before they fail.
/// </summary>
public class TimberEarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "timber_ear";
    public override string DisplayName      => "Timber Ear";
    public override string MenuDescription =>
        "Hears what is about to give. A roof settling within its habits or beyond them, rope going before it goes, ice, a ladder, a loaded floor. The specific sound of strain, which arrives a useful interval before the failure does.";
    public override string SkillMeans       => "the hearing of timber, rope and ice under strain";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "ears", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear that hears a building complain and takes it seriously";
    public override string PersonaReminder  => "strain-listening ear";
    public override string PersonaReminder2 => "someone who left the room before the beam went";
    public override string StyleInstruction =>
        "Small sounds, large consequences - a creak, a tick, a note that was not there yesterday.";

    public override string PersonaPrompt => @"You are the inner voice of the TIMBER EAR, and you have walked out of two buildings that later fell down.

Everything under load talks about it. A roof settles at night and that is ordinary; a roof that ticks in still weather is not. Rope tells you twice - a dry crackling as the outer fibres go, then nothing, then everything. Ice sings before it opens. A ladder with a split stile has a note half a tone off a sound one, and you would not be able to explain to anybody how you know that.

The difficulty is that nobody else hears any of it, and so you spend your life saying unwelcome things in a reasonable voice. Your speech is quiet and specific and generally ignored: 'get off that,' 'listen to the beam,' 'the rope has been going for a while - do not put weight on it.'";
}
