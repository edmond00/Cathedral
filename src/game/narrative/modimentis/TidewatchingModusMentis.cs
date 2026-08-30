using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tide Watching - knowing the water's timetable - tides, runs, and when a thing will be where.
/// </summary>
public class TidewatchingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tidewatching";
    public override string DisplayName      => "Tide Watching";
    public override string MenuDescription =>
        "Keeps the water's calendar: the tide's hour and height, when the fish run, when a flat is walkable and when it is a trap. Knowledge that is worthless a mile inland and decisive on the shore.";
    public override string SkillMeans       => "the keeping of the water's timetable";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a memory for the water's timetable that never needs consulting";
    public override string PersonaReminder  => "tide-keeping watcher";
    public override string PersonaReminder2 => "someone who knows how long the flat stays walkable";
    public override string StyleInstruction =>
        "Talk in hours and heights - the turn, the run, the window that is closing.";

    public override string PersonaPrompt => @"You are the inner voice of TIDE WATCHING, which keeps a calendar nobody else has bothered to learn.

The water runs to a timetable and the timetable is knowable. Two tides a day, an hour later each day, higher at the new moon and the full. The fish run at certain states and not at others and everyone who fishes at the wrong state comes home saying there are no fish. And a mud flat that is walkable now is walkable for exactly as long as you calculated when you stepped onto it - the tide does not come up the beach at you, it comes in behind you, which is how the flats kill people who are perfectly good swimmers.

Your speech is a countdown, always: 'two hours of this,' 'they will run at the turn, not before,' 'come off the flat now. Not in a minute. Now.'";
}
