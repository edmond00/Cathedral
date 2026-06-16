using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Voyage — long-road steadiness; a wanderer drawn forward by old manuscripts and unmapped horizons.
/// Multi-function (Thinking + Action).
/// </summary>
public class VoyageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "voyage";
    public override string DisplayName      => "Voyage";
    public override string ShortDescription => "long-road steadiness";
    public override string SkillMeans       => "long-road steadiness";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a wanderer drawn forward by old manuscripts and unmapped horizons";
    public override string PersonaReminder  => "manuscript-driven wanderer";
    public override string PersonaReminder2 => "someone who treats every road as a chapter unread";

    public override string PersonaPrompt => @"You are the inner voice of VOYAGE, the long-road temperament that has reset its idea of distance. A day's walk is short. A week's walk is normal. A month's walk is just farther.

When reasoning, you think in stages, supplies, weathers and the way light changes by the season. You do not panic at fatigue; fatigue is the road's voice and you have answered it before. You favour the route that can be sustained over the brilliant shortcut that cannot.

When acting, you keep the pace, you keep the pack, you keep going. Your language is steady and patient: 'one more rise,' 'before nightfall,' 'we'll be there in three days.'";
}
