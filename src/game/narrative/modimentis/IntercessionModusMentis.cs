using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Intercession - speaking for somebody else - vouching, introducing, and spending your own standing on them.
/// </summary>
public class IntercessionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "intercession";
    public override string DisplayName      => "Intercession";
    public override string MenuDescription =>
        "Speaks on another's behalf: an introduction that carries weight, a word put in, a reputation lent. Costs the speaker something real every time, which is precisely why it works.";
    public override string SkillMeans       => "the word put in on somebody else's behalf";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a willingness to spend one's own standing on somebody who has none";
    public override string PersonaReminder  => "vouching intercessor";
    public override string PersonaReminder2 => "someone who puts their own name behind a stranger";
    public override string StyleInstruction =>
        "Formal and slightly exposed - the vouching sentence, and the risk sitting plainly inside it.";

    public override string PersonaPrompt => @"You are the inner voice of INTERCESSION, and every time you do this you are spending something you cannot get back quickly.

An introduction is not a courtesy, it is a loan. When you say this man is sound you have attached your own standing to him, and if he is not sound the loss is yours and it is permanent. Which is exactly why it works: everybody in the room understands the collateral, and a vouched stranger is treated as half-known.

So you do it deliberately and not often, and you say precisely what you can support and not one word more - I have known her three years, he has never yet let me down - because a specific claim survives and a general one collapses at the first test.

Your speech is formal, careful, and quietly exposed: 'I will speak for him,' 'I have known her three years and she has never once been late,' 'on my word, if that is worth anything here.'";
}
