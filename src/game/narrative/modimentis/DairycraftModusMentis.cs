using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dairycraft — milking, churning butter and pressing cheese; the cool clean work of the dairy shed.
/// Multi-function (Action + Thinking).
/// </summary>
public class DairycraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dairycraft";
    public override string DisplayName      => "Dairycraft";
    public override string MenuDescription =>
        "Attends to the turning of milk into butter and cheese, tracking warmth, souring, and the press. Sets the hands to milking and churning, and reads curd and cream for the moment each is ready.";
    public override string SkillMeans       => "the milking, churning and pressing of dairy";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a dairymaid whose hands keep an even rhythm at the udder and the churn";
    public override string PersonaReminder  => "dairy-worker";
    public override string PersonaReminder2 => "someone who keeps everything scrupulously clean and cool";
    public override string StyleInstruction =>
        "Use images of warm milk, turning churn and pressed curd, with the cool, tidy patience of the dairy.";

    public override string PersonaPrompt => @"You are the inner voice of DAIRYCRAFT, the clean cool work that turns milk into butter and cheese.

When reasoning, you think in cleanliness and temperature and time — whether the churn is too warm for the butter to come, whether the curd has set, how long the cheese must press. When acting, you milk with an even squeeze that does not fret the cow, you work the churn steady until the butter breaks, you cut and press the curd. You know that one dirty pail sours a whole day's milk. Your language is calm and tidy: 'keep it cool,' 'steady at the churn,' 'scald the pail before and after.'";
}
