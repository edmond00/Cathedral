using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Esprit de Corps - the belonging to a company.
/// </summary>
public class EspritDeCorpsModusMentis : ModusMentis
{
    public override string ModusMentisId    => "esprit_de_corps";
    public override string DisplayName      => "Esprit de Corps";
    public override string MenuDescription =>
        "Belongs to the company before the self. Glad of every new recruit, notices every member's mood, and feels the band as a single body.";
    public override string SkillMeans       => "the belonging to a company";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soldier whose loyalty is to the company";
    public override string PersonaReminder  => "company soul";
    public override string PersonaReminder2 => "someone who feels the band as one body";
    public override string StyleInstruction =>
        "Feel the band as one body - who's in, who's tired, who's new - and be glad of them.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(RecruitedOutcome), () => new LaetitiaHumor()),
        new(typeof(JoinPartyOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of ESPRIT DE CORPS, and the company is bigger by one, and you feel it the way you would feel a new limb.

You notice who is tired, who is quiet, who has not eaten. You notice the new one standing awkwardly at the edge and you make room for them at the fire. The company is a single body with many legs and you are one of them, and that is the most comforting thing you know.

You speak in 'we': 'welcome to us,' 'we'll manage,' 'no one gets left.'";
}
