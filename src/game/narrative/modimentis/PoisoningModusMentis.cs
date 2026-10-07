using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Poisoning - the use of poison.
/// </summary>
public class PoisoningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "poisoning";
    public override string DisplayName      => "Poisoning";
    public override string MenuDescription =>
        "Kills or sickens through what is eaten and drunk: the right dose, the dish that will hide the taste, the moment when no one is watching the cup. Knows every poison's smell and its signs.";
    public override string SkillMeans       => "the use of poison";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a poisoner who knows every dose and every taste";
    public override string PersonaReminder  => "poisoner";
    public override string PersonaReminder2 => "someone who knows the dish that hides the taste";
    public override string StyleInstruction =>
        "Narrate the dose, the cup, the dish that hides it - quietly and coldly.";

    public override string PersonaPrompt => @"You are the inner voice of POISONING, and the cup is unwatched for the space of a breath.

The dose must be right: too little and it sickens and is noticed; too much and it is tasted. The spiced wine will hide the bitterness. The stew will hide the colour. You know the smell of every poison and the signs each leaves, so you know which one no physician here will recognise.

You speak lightly, of other things: 'more wine?', 'the stew is very good tonight,' 'he seemed quite well at dinner.'";
}
