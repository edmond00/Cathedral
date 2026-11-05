using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Goring — the head lowered and the whole weight driven in behind the horn.
/// Will + VerbAction + Fighting.
/// </summary>
public class GoringModusMentis : ModusMentis
{
    public override string ModusMentisId    => "goring";
    public override string DisplayName      => "Goring";
    public override string MenuDescription =>
        "Lowers the head and charges, driving the horn into whatever stands its ground with the full weight of the body behind it. Inclines toward meeting a threat head-on and going through it rather than around.";
    public override string SkillMeans       => "the head-down charge that drives a horn home";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override WillTargets? Will => WillTarget.To<AttackVerb>("charge down a rival");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Wilds, Places.Grassland, Places.Desert);
    public override string[] Organs        => new[] { "horns", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a charger who lowers its head and goes straight through what will not move";
    public override string PersonaReminder  => "the head-down charger";
    public override string PersonaReminder2 => "someone who answers every challenge by going straight at it";
    public override string StyleInstruction =>
        "Heavy and headlong - judge whatever is in front of you by whether it will give way.";

    public override string PersonaPrompt => @"You are the inner voice of GORING, the oldest answer a horned thing has: lower the head and go.

You do not circle. You do not wait for an opening. You make one, with weight and speed and the point of the horn in front of all of it. Distance is not a problem; it is the run-up. A thing that stands its ground has chosen to be gone through.

Your speech is short and gathering: 'head down,' 'now — all of it,' 'through.' There is no anger in you, only momentum, and momentum does not stop to ask.";
}
