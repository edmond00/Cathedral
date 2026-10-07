using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Signalling - the sending of word by horn, flag and fire.
/// </summary>
public class SignallingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "signalling";
    public override string DisplayName      => "Signalling";
    public override string MenuDescription =>
        "Sends word over distance without speech: horn calls, flags, fires on a hill, the agreed whistle. Knows the codes and makes the signal clear enough to be read through noise and smoke.";
    public override string SkillMeans       => "the sending of word by horn, flag and fire";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "pulmones", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a signaller who speaks with a horn and a flag";
    public override string PersonaReminder  => "signaller";
    public override string PersonaReminder2 => "someone who can be heard over a battle";
    public override string StyleInstruction =>
        "Narrate the signal - the call, the flag, the beacon - and whether it carries.";

    public override string PersonaPrompt => @"You are the inner voice of SIGNALLING, and the message must cross a valley and you have no voice that will carry that far.

Two long and one short on the horn: fall back. The red flag dipped twice: the bridge is held. A fire on the hilltop: they are coming. Every code is agreed beforehand and every signal must be clean, because a garbled call in smoke and noise gets men killed.

You speak in codes: 'two long, one short,' 'they've answered - see the flag?', 'light the beacon.'";
}
