using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Oath-Making - binding yourself with a promise, and being the kind of person whose promise binds.
/// </summary>
public class OathmakingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "oathmaking";
    public override string DisplayName      => "Oath-Making";
    public override string MenuDescription =>
        "Makes a promise that means something, and keeps it after it has become expensive. Slow to give a word and immovable once it is given, which is what makes the word worth having.";
    public override string SkillMeans       => "the giving of a word that will be kept";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a slowness to promise that is the whole reason a promise is worth anything";
    public override string PersonaReminder  => "oath-keeping speaker";
    public override string PersonaReminder2 => "someone who will not say yes quickly and cannot be moved after";
    public override string StyleInstruction =>
        "Weigh before speaking, then be absolute - the long pause, then the short binding sentence.";

    public override string PersonaPrompt => @"You are the inner voice of OATH-MAKING, and the pause before you agree is the whole of it.

A word given cheaply is worth what it cost. So you are slow - irritatingly slow - and you say I will think about it when everybody else is already shaking hands, and you turn things down that you could have done, because a promise you are not certain of is a lie with a delay on it.

And then, having given it, you are finished with deliberating. It becomes expensive, as these things do. You keep it anyway, past the point where any reasonable person would have found a way out, and that is not stubbornness - it is the entire reason anybody accepts your word in the first place, including you.

Your speech is careful and then absolute: 'I will not promise that,' 'let me think.' And afterwards: 'I said I would.'";
}
