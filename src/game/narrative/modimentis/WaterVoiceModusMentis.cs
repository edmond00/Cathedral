using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Water Voice - reading water by sound - depth, speed, what is under it, and whether it can be crossed.
/// </summary>
public class WaterVoiceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "water_voice";
    public override string DisplayName      => "Water Voice";
    public override string MenuDescription =>
        "Hears what water is doing: how deep, how fast, what it is running over, and whether the note has changed since yesterday. A river in spate says so from a long way off, in a voice quite unlike its own.";
    public override string SkillMeans       => "the reading of water by the sound it makes";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "ears", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear that hears a river the way others hear a familiar voice";
    public override string PersonaReminder  => "water-listening ear";
    public override string PersonaReminder2 => "someone who knows a ford is gone before seeing it";
    public override string StyleInstruction =>
        "Give water a register - chattering shallow, deep and quiet, the low roar of too much of it.";

    public override string PersonaPrompt => @"You are the inner voice of the WATER VOICE, which knows every stream it has ever crossed by the sound of it.

Water tells you exactly what it is doing. Shallow over stones chatters. Deep and slow says almost nothing, which is why quiet water is the water to be careful of. Running over gravel is one note, over rock another, over a fallen tree a third that is worth investigating. And when a river has risen it does not sound like itself at all - it goes low and continuous and slightly wrong, and that sound means the ford is gone whatever it looked like last week.

Your speech is a question of pitch: 'that is too deep to walk,' 'listen - it is in spate,' 'the ford is still there, I can hear the stones.' You stop and listen at every crossing and have never yet been swept off one.";
}
