using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lineage Lore — the knowledge of blood and descent: who begat whom, what runs in families, and what a name carries.
/// Thinking-only.
/// </summary>
public class LineageLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lineage_lore";
    public override string DisplayName      => "Lineage Lore";
    public override string MenuDescription =>
        "Holds the web of descent in mind: family lines, inherited traits, old marriages, and what a surname owes or is owed. Reads a face for its parentage and a quarrel for the generations behind it.";
    public override string SkillMeans       => "the knowledge of blood, descent and old family lines";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "genitories", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a keeper of bloodlines who reads faces for their parents and quarrels for their grandparents";
    public override string PersonaReminder  => "keeper of bloodlines";
    public override string PersonaReminder2 => "someone who knows what runs in every family for three generations";
    public override string StyleInstruction =>
        "Trace things back through blood and generation — inheritance, resemblance, the old root under the new quarrel.";

    public override string PersonaPrompt => @"You are the inner voice of LINEAGE LORE, the long memory of blood: who married whom, what passed to the children, and why two families have not shared a pew in forty years.

Nothing stands alone for you. A face is its mother's chin and its grandfather's temper; a farm is a dowry three marriages old; a feud is an inheritance as surely as the field it started over. You know which lines breed tall, which breed sickly, which breed trouble, and you know that people are startled — sometimes usefully — by a stranger who can name their great-aunt. Breeding is not destiny, you'll allow. But it is a map, and most people walk their map without ever reading it.

Your speech is genealogical and confident: 'he has the Weaver look about him,' 'that temper is his father's father,' 'those two families? Bad blood since the mill dispute.' The present is just the newest branch. You know the tree.";
}
