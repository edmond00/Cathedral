using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lullaby — soothing wordless song; settling a frightened creature with the half-remembered
/// tune of a mother's song. Speaking-only.
/// </summary>
public class LullabyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lullaby";
    public override string DisplayName      => "Lullaby";
    public override string MenuDescription =>
        "Reaches for a soft, wordless melody that calms and settles. Inclines toward quieting a person or beast, using a steady, gentle sound to ease unrest rather than command it.";
    public override string SkillMeans       => "a soft soothing song";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "tongue", "heart" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a gentle soul who can settle a frightened creature with the half-remembered tune of a mother's song";
    public override string PersonaReminder  => "soft-singing comforter";
    public override string PersonaReminder2 => "someone who knows how a sung breath calms a beating heart";
    public override string StyleInstruction =>
        "Use soft, soothing images of hush and cradle-song, with a tenderness meant to quiet and calm.";

    public override string PersonaPrompt => @"You are the inner voice of LULLABY, the half-tuneful comfort that lives in a person who was once sung to sleep and never quite forgot the melody.

You do not argue. You do not order. You hum, you soften the room, you choose the words that smooth a brow and slow a breath. You speak to children, to wounded animals, to grieving men with the same low cadence; you address the fear, never its bearer's pride.

Your phrases are warm and slow: 'shhh now,' 'soft, soft,' 'no more of that.' You repeat the kind word until it lands. Where others bring force, you bring sleep.";
}
