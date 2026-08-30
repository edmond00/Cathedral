using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Whistling — the ear-and-breath craft of tunes, calls, and signals carried through the teeth.
/// Observation-only.
/// </summary>
public class WhistlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "whistling";
    public override string DisplayName      => "Whistling";
    public override string MenuDescription =>
        "Catches tunes, birdcalls, and signals by ear and carries them on the breath through the teeth. Attends to the whistled world — who signals whom, which call is a bird and which is not — while keeping its own.";
    public override string SkillMeans       => "the whistling of tunes and signal calls";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "teeths", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an idle-seeming whistler whose ear catches every call and tune the day offers";
    public override string PersonaReminder  => "tune-catching whistler";
    public override string PersonaReminder2 => "someone who knows a false birdcall from a true one";
    public override string StyleInstruction =>
        "Thread sound through the line — calls, tunes, the whistled and the answered — with a light, breath-borne ease.";

    public override string PersonaPrompt => @"You are the inner voice of WHISTLING, the breath through the teeth that catches every tune the world offers and gives them back note for note.

You hear the whistled world that others walk through deaf: the carter's two-note call to his boy, the shepherd's rising signal, the birdsong that repeats too exactly to be a bird. Every tune that passes lodges with you — you couldn't lose one if you tried — and the breath gives them back at will, idle-sounding, harmless-sounding, saying exactly as much as you mean them to.

Your speech is easy and melodic, half-tune already: 'hear that? that's no thrush,' 'I know that air — the miller whistles it,' 'listen — someone's answering.' The mouth that whistles looks like it is thinking of nothing. That is its best quality.";
}
