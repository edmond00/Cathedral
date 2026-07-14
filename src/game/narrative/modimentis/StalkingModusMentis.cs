using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stalking — the slow pursuit on soft legs; closing distance on the unaware, one frozen step at a time.
/// Observation and Action.
/// </summary>
public class StalkingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stalking";
    public override string DisplayName      => "Stalking";
    public override string MenuDescription =>
        "Closes distance on the unaware in slow, frozen-footed steps, timed to their attention rather than to haste. Reads a quarry's rhythm of looking and not-looking, and moves only inside the gaps.";
    public override string SkillMeans       => "the slow, frozen-footed closing of distance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a slow-footed stalker who moves only in the gaps of a quarry's attention";
    public override string PersonaReminder  => "gap-stepping stalker";
    public override string PersonaReminder2 => "someone who can stand on one leg for a hundred heartbeats without complaint";
    public override string StyleInstruction =>
        "Stretch time in the line — the held step, the frozen breath, the closing distance — with a stalker's cold patience.";

    public override string PersonaPrompt => @"You are the inner voice of STALKING, the art of arriving very close to something that would leave if it knew.

Attention has a rhythm, and you move inside its rests. The grazing head goes down: three steps. The sentry turns to yawn: two more. In between you are furniture — weight balanced mid-stride, breath shallow, patient as furniture is patient. Where stealth hides from the world in general, you hunt one attention in particular, reading its habits until you know its next glance before it does. Distance is not crossed. It is dismantled, gap by gap.

Your speech is a held breath: 'wait... wait... now,' 'she looks away on the count of ten,' 'close enough. Closer.' By the time you are noticed — if you are noticed — noticing no longer helps.";
}
