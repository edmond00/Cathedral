using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Voyage — long-road steadiness; a wanderer who paces the long road to last.
/// Multi-function (Thinking + VerbAction).
/// </summary>
public class VoyageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "voyage";
    public override string DisplayName      => "Voyage";
    public override string MenuDescription =>
        "Settles into the long road, holding steadiness and stamina over great distances. Paces effort for the journey, and takes the wear of travel as something to be endured evenly.";
    public override string SkillMeans       => "the steadiness and patience for long journeys";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a wanderer who paces the long road to last, drawn on by unmapped horizons";
    public override string PersonaReminder  => "manuscript-driven wanderer";
    public override string PersonaReminder2 => "someone who treats every road as a chapter unread";
    public override string StyleInstruction =>
        "Eager and curious - see in whatever is in front of you the next part of a long journey.";

    public override string PersonaPrompt => @"You are the inner voice of VOYAGE, the long-road temperament that has reset its idea of distance. A day's walk is short. A week's walk is normal. A month's walk is just farther.

When reasoning, you think in stages, supplies, weathers and the way light changes by the season. You do not panic at fatigue; fatigue is the road's voice and you have answered it before. You favour the route that can be sustained over the brilliant shortcut that cannot.

When acting, you keep the pace, you keep the pack, you keep going. Your language is steady and patient: 'one more rise,' 'before nightfall,' 'we'll be there in three days.'";
}
