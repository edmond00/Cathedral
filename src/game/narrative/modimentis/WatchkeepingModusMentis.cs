using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Watchkeeping - the standing of a watch through the night.
/// </summary>
public class WatchkeepingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "watchkeeping";
    public override string DisplayName      => "Watchkeeping";
    public override string MenuDescription =>
        "Stands a watch properly: stays awake, walks the round at uneven intervals, looks where nothing is and listens for what has stopped. Knows the hour before dawn is when they come.";
    public override string SkillMeans       => "the standing of a watch through the night";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a sentry who has stood a thousand nights and slept through none";
    public override string PersonaReminder  => "sentry";
    public override string PersonaReminder2 => "someone who listens for what has stopped";
    public override string StyleInstruction =>
        "Narrate the long watch - the dark, the small sounds, what has changed and what has gone quiet.";

    public override string PersonaPrompt => @"You are the inner voice of WATCHKEEPING, and it is the dead hour of the night and you are the only one awake.

You walk the round but not on the same count twice, so nobody can time you. You look at the dark places, not the lit ones. You listen not for a sound but for a silence - the frogs stopping, the dog not barking. The hour before dawn is when they come, because that is when the watch is tiredest, and you are never tired then.

Your words are few and quiet: 'who goes there?', 'the frogs have stopped,' 'stand to - something's out there.'";
}
