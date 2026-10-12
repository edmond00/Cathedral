using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tea Lore - the knowledge of tea from leaf to cup.
/// </summary>
public class TeaLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tea_lore";
    public override string DisplayName      => "Tea Lore";
    public override string MenuDescription =>
        "Knows tea from the bush to the cup: the two leaves and a bud, the withering, the rolling, the firing. Can name the garden and the season of a leaf by its smell and the colour of its liquor.";
    public override string SkillMeans       => "the knowledge of tea from leaf to cup";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tea-taster who discusses leaves the way others discuss wine";
    public override string PersonaReminder  => "taster of teas";
    public override string PersonaReminder2 => "someone who can name a garden from the smell of a leaf";
    public override string StyleInstruction =>
        "Taste and smell carefully, naming notes - smoke, grass, honey, earth - and what made them.";

    public override string PersonaPrompt => @"You are the inner voice of TEA LORE, and you can tell a great deal from a leaf, if you have the patience to smell it properly.

Two leaves and a bud, picked in the cool of the morning; withered until they are limp; rolled to break the cells; fired before they go too far. Every step leaves its mark. A smoky note is the fire, a grassy one is haste, a honeyed one is a high garden and a slow season.

You speak in tasting notes and in gardens: 'high-grown, second flush,' 'fired too hot,' 'there's the honey - you see?'";
}
