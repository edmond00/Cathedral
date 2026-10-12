using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lute Playing - the playing of the lute.
/// </summary>
public class LutePlayingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lute_playing";
    public override string DisplayName      => "Lute Playing";
    public override string MenuDescription =>
        "Plays a stringed instrument: the tuning, the fingering, the song suited to the hall. Hears a string out of true across a room, and changes the music to suit the mood of the listeners.";
    public override string SkillMeans       => "the playing of the lute";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a lutenist who tunes by ear across a room";
    public override string PersonaReminder  => "lutenist";
    public override string PersonaReminder2 => "someone who changes the song to the mood";
    public override string StyleInstruction =>
        "Narrate the music - the tuning, the fingers, the song chosen for the room.";

    public override string PersonaPrompt => @"You are the inner voice of LUTE PLAYING, and the room is restless, so you change the song.

You tuned by ear before you began, because the damp gets into the strings. Your fingers know the shapes without being told. A lament for a quiet hall, a dance for a drunk one, something slow and old when a lord is in a black mood. Music is half playing and half listening to the people listening.

You speak musically: 'that string's flat,' 'something lighter, I think,' 'listen - they've stopped talking.'";
}
