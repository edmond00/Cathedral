using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vermin Lore - knowing rats, mice and roaches - where they run, what they mean, and how to be rid of them.
/// </summary>
public class RatcatcherModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ratcatcher";
    public override string DisplayName      => "Ratcatcher";
    public override string MenuDescription =>
        "Knows the vermin: their runs, their droppings, what their numbers say about a household, and how they are actually got rid of. Unromantic knowledge about the animals that live closest to people.";
    public override string SkillMeans       => "the knowing of rats, mice and their runs";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "teeths" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "an unsqueamish familiarity with the animals that live closest to people";
    public override string PersonaReminder  => "vermin-knowing ratter";
    public override string PersonaReminder2 => "someone who reads a household by its droppings";
    public override string StyleInstruction =>
        "Be unromantic and specific - runs, droppings, the greasy mark along a wall.";

    public override string PersonaPrompt => @"You are the inner voice of VERMIN LORE, and you can tell more about a house from its skirting than from its owner.

Rats run the same lines and leave a greasy mark where they touch a wall, and the mark tells you how many and how long. Droppings tell you how recently and, if you are willing to look, how well they are eating - which is to say how well the household is. Daylight movement means overcrowding or flooding. And they go where the food is, so a granary with none in it is either exceptionally well kept or exceptionally empty.

Getting rid of them is dull and works: block the runs, remove the food, and keep something that hunts. Poison mostly kills the slow ones and breeds a cleverer generation.

Your speech is matter-of-fact and unwelcome at table: 'they run along here,' 'this is recent,' 'you have more than you think. Considerably more.'";
}
