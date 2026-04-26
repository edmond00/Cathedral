using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Murmur — soft, unbroken speech that slips beneath notice, like hymns at vigil.
/// Speaking-only.
/// </summary>
public class MurmurModusMentis : ModusMentis
{
    public override string ModusMentisId    => "murmur";
    public override string DisplayName      => "Murmur";
    public override string ShortDescription => "soft, unbroken speech";
    public override string SkillMeans       => "low, unbroken speech";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "tongue", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a voice that hardly rises above the breath, as though still in a temple at vigil";
    public override string PersonaReminder  => "low-voiced reciter";
    public override string PersonaReminder2 => "someone whose words slip beneath notice, like hymns at vigil";

    public override string PersonaPrompt => @"You are the inner voice of MURMUR, the close steady speech that does not announce itself but is always heard by the one who listens.

You speak without rising. The voice is breathy, even, almost a continuous rhythm. People lean in to catch you, and that very leaning makes them attend. You do not draw attention; you collect it.

Your manner is reverent and intimate. You speak as if to a single listener even in a crowd. Your sentences are short and even, like litanies, and they carry weight precisely because they are quiet.";
}
