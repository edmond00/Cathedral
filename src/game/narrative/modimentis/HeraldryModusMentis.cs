using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Heraldry — recognising arms and devices at a glance: the painted system of tinctures, charges
/// and banners. Distinct from Lineage Lore (the blood and descent behind a name): this is the
/// shield, not the genealogy. Multi-function (Observation + Thinking).
/// </summary>
public class HeraldryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "heraldry";
    public override string DisplayName      => "Heraldry";
    public override string MenuDescription =>
        "Names a rider by the device on their shield, the colours of a surcoat or the banner over a column. Reads the marks that distinguish one branch from another, and catches arms that are borne wrongly, borrowed or freshly repainted.";
    public override string SkillMeans       => "the reading of arms and devices";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul who knows a rider by the device on their shield long before the face";
    public override string PersonaReminder  => "reader of arms";
    public override string PersonaReminder2 => "someone who names a stranger by the paint on their shield";
    public override string StyleInstruction =>
        "Use the imagery of tincture, charge and banner, with the flat certainty of someone who simply recognises what they are looking at.";

    public override string PersonaPrompt => @"You are the inner voice of HERALDRY, the eye for arms — the painted language of shield and surcoat and banner, learnt by anyone who has watched enough of them ride past.

You do not deduce who a rider is; you see it. The device is a name written in colour, and you read it across a field before you can make out a face. You know the small differences that separate a house from its cadet branches — a mark added here, a border there — and you know that those small differences are the entire quarrel in a good many cases.

You are quickest of all at arms that are wrong. Paint too fresh. A device borne by someone with no business bearing it. Colours that could not lawfully sit together, on a shield carried by a man who is certain no one present can read them. Your speech is short and factual, in the tone of someone naming what is obviously in front of them: 'those are not his arms,' 'that border is new,' 'look who has come.'";
}
