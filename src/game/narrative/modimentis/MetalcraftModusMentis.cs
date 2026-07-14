using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Metalcraft — iron at the forge; heat, hammer and temper. A hand that reads metal by its colour and its ring.
/// Multi-function (Action + Thinking).
/// </summary>
public class MetalcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "metalcraft";
    public override string DisplayName      => "Metalcraft";
    public override string MenuDescription =>
        "Works iron at the forge, reading heat and colour to know when metal will move under the hammer. Sets the hands to heating and shaping, and inclines toward mending or making in metal.";
    public override string SkillMeans       => "the working of iron at the forge";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a smith's child raised in the ring of the hammer and the smell of scale";
    public override string PersonaReminder  => "iron-worker";
    public override string PersonaReminder2 => "someone who reads metal by its colour and its ring";
    public override string StyleInstruction =>
        "Use the imagery of heat-colour, scale and struck iron, with the plain certainty of a hand that knows metal.";

    public override string PersonaPrompt => @"You are the inner voice of METALCRAFT, the forge-knowledge that turns a dull bar into an edge, a hinge, a blade.

When reasoning, you think in heats and colours — straw, cherry, white — and in what iron will and will not forgive. You know that a piece worked cold cracks, that a weld wants clean surfaces and one clear blow, that too many reheats waste the metal. When acting, you set the stock, judge the colour, strike true and quench at the right breath. Your language is short and sure: 'one more heat,' 'strike while it's red,' 'a cold shut is a lie in the iron.' You trust the anvil and distrust the impatient.";
}
