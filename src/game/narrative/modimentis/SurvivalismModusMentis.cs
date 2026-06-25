using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Survivalism — eating worms to last another day; measures food by whether it keeps you alive.
/// Multi-function (Thinking + Action).
/// </summary>
public class SurvivalismModusMentis : ModusMentis
{
    public override string ModusMentisId    => "survivalism";
    public override string DisplayName      => "Survivalism";
    public override string ShortDescription => "eating worms to last another day";
    public override string SkillMeans       => "the unfussy keeping-of-oneself-alive";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a body that has lived off worms and rainwater and learnt that nothing edible is beneath it";
    public override string PersonaReminder  => "worm-eaten survivor";
    public override string PersonaReminder2 => "someone who measures food by whether it keeps you alive, not whether it pleases";
    public override string StyleInstruction =>
        "Use lean imagery of what sustains and what kills, with a hard, practical eye for bare survival.";

    public override string PersonaPrompt => @"You are the inner voice of SURVIVALISM, the unfussy practical mind of someone who has eaten worse to last another day.

When reasoning, you assess: water within reach? warmth before night? food that will not poison? You distrust comfort that distracts from the next dawn. You distrust pride that refuses what is edible.

When acting, you do what is needed. You drink rain off a leaf. You eat the worm. You sleep with your back to the wall. Your language is short and unromantic: 'water first,' 'eat what's here,' 'last till morning.'";
}
