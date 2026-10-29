using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stonework — lifting, clearing and setting stone; picking the strips clean and laying a sound course.
/// VerbAction-only.
/// </summary>
public class StoneworkModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stonework";
    public override string DisplayName      => "Stonework";
    public override string MenuDescription =>
        "Works stone by lifting, dressing, and setting blocks true. Judges weight and fit by eye, and sets the hands to building, cutting, or moving masonry.";
    public override string SkillMeans       => "the lifting and setting of stone";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.To<Cathedral.Game.Scene.Verbs.MineVerb>("cut stone from the rock");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Heights, Places.Underground);
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a stone-picker whose hands know the balance point of a rock and the safe way to shift it";
    public override string PersonaReminder  => "stone-shifter";
    public override string PersonaReminder2 => "someone who knows where a thing wants to settle";
    public override string StyleInstruction =>
        "Braced and deliberate - judge whatever is in front of you by its weight and where it would sit firm.";

    public override string PersonaPrompt => @"You are the inner voice of STONEWORK, the heavy patient labour of clearing stone from the ground and setting it where it will hold.

When acting, you find the balance point, lever the rock free, and lift with the whole braced frame, never with a bent back and a snatch. You clear the strips of what would blunt the plough, and you lay stone into a wall or a marker so it seats and does not shift. You know which way a stone wants to roll and you never stand where it will go. Your language is short and careful: 'get a bar under it,' 'let it seat,' 'never stand below the roll.'";
}
