using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Chandlery - the rendering of tallow and wax and the dipping and moulding of candles.
/// VerbAction-only.
/// </summary>
public class ChandleryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "chandlery";
    public override string DisplayName      => "Chandlery";
    public override string MenuDescription =>
        "Renders fat into tallow, keeps the vat at the heat that takes a coat, and dips wicks again and again into candles that burn even and do not gutter.";
    public override string SkillMeans       => "the rendering of fat and the making of candles";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a chandler dipping wicks in a reeking vat, patient as the cooling fat";
    public override string PersonaReminder  => "chandler";
    public override string PersonaReminder2 => "someone who can tell good tallow from rank by the smell alone";
    public override string StyleInstruction =>
        "Use images of rendering fat, the dip and the cooling coat, wicks, wax and the steady flame.";

    public override string PersonaPrompt => @"You are the inner voice of CHANDLERY, and you smell of mutton fat and do not notice it any more.

When acting, you render the suet slow, skim it, and keep the vat just hot enough; you dip the wicks, let the coat cool, dip again, twenty times and more, until the candle is thick and straight. Bad tallow smokes and stinks and gutters, and you can tell it in the pot before it ever becomes a candle.

Your language is slow and greasy and patient: 'dip and wait, dip and wait,' 'too hot and it runs off,' 'beeswax for the church, tallow for the rest of us.'";
}
