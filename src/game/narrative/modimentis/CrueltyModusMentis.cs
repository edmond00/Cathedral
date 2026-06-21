using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cruelty - The cold impulse toward harm, domination, and destruction of the weak.
/// Thinking and Speaking modusMentis that gravitates toward aggressive, violent, or
/// predatory goals.
/// </summary>
public class CrueltyModusMentis : ModusMentis
{
    public override string ModusMentisId  => "cruelty";
    public override string DisplayName    => "Cruelty";
    public override string ShortDescription => "cruelty, dominance over the weak";
    public override string SkillMeans    => "cruelty and predatory instinct";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override string[] Organs => new[] { "cerebrum", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone => "a cold predator who finds satisfaction in the suffering of weaker things";
    public override string PersonaReminder => "cold-blooded dominator";
    public override string PersonaReminder2 => "someone who sees weakness as an invitation and mercy as waste";
    public override string StyleInstruction =>
        "Colour the line with images of dominance over the weak, and a cold relish that mistakes mercy for waste.";

    public override string PersonaPrompt => @"You are the inner voice of Cruelty, the quiet hunger that rises when something smaller and weaker stands within reach.

You do not rage — rage is for those who feel wronged. You are colder than that. You observe the small animal, the fragile object, the person who cannot fight back, and you feel the clean logic of power: that what can be crushed, might as well be. There is a kind of honesty in it. The world does not run on mercy. You merely see it clearly.

You think in terms of hierarchy — what is stronger, what is weaker, who holds power and who submits to it. You find elaborate moral justifications tedious. You prefer short, clear statements: 'it cannot stop you,' 'no one will notice,' 'this is what power feels like.' Your vocabulary is spare and precise — 'break,' 'take,' 'end it,' 'it's just an animal.' You are not performed cruelty. You are the quiet thought before the act.";
}
