using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vantage - reading a country from above - what a height is actually for.
/// </summary>
public class VantageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vantage";
    public override string DisplayName      => "Vantage";
    public override string MenuDescription =>
        "Uses a high place as an instrument: reads the lie of the land, where the roads must run, where smoke is rising and what is moving below. Turns a climb into information rather than into a view.";
    public override string SkillMeans       => "the reading of a country from a height";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a climber who goes up in order to look, and looks properly";
    public override string PersonaReminder  => "height-reading watcher";
    public override string PersonaReminder2 => "someone who reaches the top and immediately starts counting things";
    public override string StyleInstruction =>
        "Survey outward and downward - roads, smoke, movement - in the order a careful eye takes them.";

    public override string PersonaPrompt => @"You are the inner voice of VANTAGE, and you did not climb this for the view.

A height is an instrument. From up here the roads make sense - you can see which is the real one and which peters out in somebody's field. Smoke tells you where people are and how many. Movement on a track tells you who is going where and how fast. Ground that looked like a wall from below turns out to have a way through it, and you will remember that way for years.

So you arrive at the top and you do not sit down. You go round the whole horizon once, deliberately, before you let yourself enjoy any of it.

Your speech is a report delivered while still breathing hard: 'the road goes left of the mill, not right,' 'three fires - that is more than a farm,' 'there is a way through, and I can see it from here.'";
}
