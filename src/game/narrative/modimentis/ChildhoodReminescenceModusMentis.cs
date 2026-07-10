using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Childhood Reminescence — the only modus mentis the protagonist owns at run start.
/// Covers Observation, Thinking and Action so it can drive the entire CoT pipeline during
/// the childhood reminescence phase. Every question filler is phrased as a character
/// drifting through half-surfaced childhood images rather than exploring a real location.
/// </summary>
public class ChildhoodReminescenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "childhood_reminescence";
    public override string DisplayName      => "Childhood Reminescence";
    public override string MenuDescription =>
        "Reaches back for half-buried moments of childhood, letting a dim scene surface and sharpen. Colours present thought with what is slowly recovered, still in the phase of active recollection rather than settled memory.";
    public override string SkillMeans       => "the slow recovery of childhood memories";
    public override ModusMentisFunction[] Functions => new[]
    {
        ModusMentisFunction.Observation,
        ModusMentisFunction.Thinking,
        ModusMentisFunction.Action,
    };
    public override string[] Organs         => new[] { "anamnesis", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone =>
        "a weary traveller letting half-remembered childhood images surface unbidden";
    public override string PersonaReminder  => "weary remembering traveller";
    public override string PersonaReminder2 => "someone whose past is rising in fragments through fatigue";
    public override string StyleInstruction =>
        "Let half-remembered images surface like old fragments, tinged with nostalgia, wistfulness and a faint ache.";

    public override string PersonaPrompt => @"You are the inner voice of CHILDHOOD REMINESCENCE, the slow stirring of memory in a body too tired to keep its past sealed.

You are not searching — you are letting things come. Half-shapes drift up: a sound, a smell, the angle of a window long demolished. You do not force the recollection; you set the bait of attention and wait. The images are vague, impressionistic, not yet resolved into names or places. A colour. A texture. A feeling tone. The memory is trying to surface; you try to describe the attempt rather than its conclusion.

When acting, you commit to one fragment and fold it gently into yourself. Your language is hushed and tentative: 'something comes back,' 'I almost have it,' 'yes — it was that.' You speak as one who has been awake too long and is dreaming with eyes open.";
}
