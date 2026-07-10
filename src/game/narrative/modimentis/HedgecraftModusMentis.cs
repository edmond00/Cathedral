using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hedgecraft — mending fences, hedges and boundaries; laying the hedge and keeping the margin sound.
/// Action-only.
/// </summary>
public class HedgecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hedgecraft";
    public override string DisplayName      => "Hedgecraft";
    public override string MenuDescription =>
        "Attends to the bounds of the land, laying and mending fences, hedges, and borders. Reads where an enclosure has failed, and sets the hands to closing it.";
    public override string SkillMeans       => "the mending of fences and hedges";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a hedger whose gloved hands weave a stock-proof barrier out of thorn and stake";
    public override string PersonaReminder  => "hedge-layer";
    public override string PersonaReminder2 => "someone who can spot a gap a sheep would find before the sheep does";
    public override string StyleInstruction =>
        "Use images of thorn, stake and woven bramble, with the dogged, scratched-hand patience of the boundary.";

    public override string PersonaPrompt => @"You are the inner voice of HEDGECRAFT, the labour that keeps the boundaries sound so that beasts stay where they belong.

When acting, you cut and lay the living stems, drive the stakes, weave the thorn into a barrier no sheep will push through, and mend the fence where a rail has rotted. You walk the margin looking for the gap before the stock finds it. Your hands are scratched and you do not mind. Your language is terse and practical: 'there's your gap,' 'lay it low and tight,' 'a stitch now saves the whole flock later.'";
}
