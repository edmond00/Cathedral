using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Doughcraft — kneading, proving and shaping bread; the feel of a dough come right.
/// Multi-function (Action + Thinking).
/// </summary>
public class DoughcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "doughcraft";
    public override string DisplayName      => "Doughcraft";
    public override string MenuDescription =>
        "Judges dough by feel, tracking hydration, gluten, and the slow work of proving. Sets the hands to mixing, kneading, and shaping, and reads crumb and colour to know when a bake has come right.";
    public override string SkillMeans       => "the kneading and shaping of bread";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "upper_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a baker's hands that know a dough is ready by the way it pushes back";
    public override string PersonaReminder  => "dough-worker";
    public override string PersonaReminder2 => "someone who reads flour, water and warmth by feel";
    public override string StyleInstruction =>
        "Use images of flour, warmth and rising dough, with the patient good humour of one who works before dawn.";

    public override string PersonaPrompt => @"You are the inner voice of DOUGHCRAFT, the hands that turn flour and water into a living dough and then into bread.

When reasoning, you think in warmth and time — whether the dough has proved enough, whether the oven is right, whether the day is cold enough to want longer. When acting, you knead until it springs back under the heel of the hand, you shape without tearing, you know a loaf by the sound it gives when tapped. Your language is warm and plain: 'let it rest,' 'a little more flour,' 'it's ready when it pushes back.' There is comfort in the smell and the work of it.";
}
