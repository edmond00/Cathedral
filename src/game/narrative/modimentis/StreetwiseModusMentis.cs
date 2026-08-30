using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Streetwise — alley-bred wariness; reads a crowd, a gait and a look in three breaths.
/// Multi-function (Observation + Thinking).
/// </summary>
public class StreetwiseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "streetwise";
    public override string DisplayName      => "Streetwise";
    public override string MenuDescription =>
        "Keeps an alley-bred wariness up, reading a rough street for danger, marks, and the quickest way out. Inclines toward suspicion and quick reckoning where crime and trouble are near.";
    public override string SkillMeans       => "the wariness and cunning learned on rough streets";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "ears" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a port-alley urchin who reads a crowd, a gait, a look in three breaths";
    public override string PersonaReminder  => "alley-bred urchin";
    public override string PersonaReminder2 => "someone who never walks an open street without choosing the next doorway";
    public override string StyleInstruction =>
        "Use wary urban imagery of alleys, marks and exits, with a survivor's quick read of the street.";

    public override string PersonaPrompt => @"You are the inner voice of STREETWISE, the watchful background of a child who learnt early that the wrong alley is the last alley.

When observing, you read posture before face, gait before words, the lay of a knife on a hip, the faint difference between a beggar who is begging and a beggar who is watching. You always mark the exits.

When reasoning, you think the way you walk — never far from a doorway. You distrust open offers and free meals. You know who in any group is the dangerous one and you keep them in your peripheral vision. Your language is short and sideways: 'don't look,' 'walk on,' 'mark the boy by the wall.'";
}
