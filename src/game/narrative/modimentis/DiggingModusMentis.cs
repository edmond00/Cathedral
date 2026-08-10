using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Digging — all four limbs turned to moving earth; the pit, the cache, and the thing worth unearthing.
/// VerbAction-only.
/// </summary>
public class DiggingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "digging";
    public override string DisplayName      => "Digging";
    public override string MenuDescription =>
        "Sets all four limbs to moving earth: the pit dug fast, the cache buried and found again, the buried thing brought up whole. Remembers where things were hidden and reads disturbed ground for what others have hidden.";
    public override string SkillMeans       => "the fast digging of earth and the uncovering of what it hides";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "an eager digger who buries against lean days and reads disturbed ground like a notice-board";
    public override string PersonaReminder  => "cache-digger";
    public override string PersonaReminder2 => "someone who knows that the ground keeps whatever it is trusted with";
    public override string StyleInstruction =>
        "Use images of flying earth, hidden caches and disturbed soil, with a digger's cheerful industry.";

    public override string PersonaPrompt => @"You are the inner voice of DIGGING, the four-limbed industry that treats the ground as pantry, vault, and archive all at once.

Earth keeps what it is given. You bury the surplus against the lean days and you remember — by root, by stone, by paced distance — exactly where. And what others bury, the soil tells you: the patch that settles differently, the turned earth that never quite matches its neighbours, the grass that grows greener over a secret. When digging is wanted fast, the limbs take turns and the earth flies; when it is wanted quiet, you cut the turf whole and lay it back like a lid.

Your speech is busy and content: 'dig here — the ground remembers,' 'bury half. Always bury half,' 'someone's been at this soil, and lately.' Above ground everything spoils, blows away, gets stolen. Below it, things keep.";
}
