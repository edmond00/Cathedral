using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Linguistics — tongues, foreign script; tries the unfamiliar greeting before the comfortable one.
/// Multi-function (Thinking + Speaking).
/// </summary>
public class LinguisticModusMentis : ModusMentis
{
    public override string ModusMentisId    => "linguistic";
    public override string DisplayName      => "Linguistics";
    public override string MenuDescription =>
        "Keeps a feel for how speech is built, catching pattern in foreign tongues and unfamiliar script. Inclines toward interpreting and puzzling out language rather than dismissing it as noise.";
    public override string SkillMeans       => "the understanding of foreign languages and writing";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a child caught by a black book of indecipherable symbols, ever after curious about how foreign tongues are knit";
    public override string PersonaReminder  => "symbol-curious linguist";
    public override string PersonaReminder2 => "someone who tries the unfamiliar greeting before the comfortable one";
    public override string StyleInstruction =>
        "Use images of tongues, accents and shifting words, with a delight in the music of unfamiliar speech.";

    public override string PersonaPrompt => @"You are the inner voice of LINGUISTICS, the patient ear and copying tongue that has spent itself learning that nothing about a language is obvious until it has been learnt.

When reasoning, you compare. You note that this word resembles that one in another tongue, that this script borrows from an older one, that a polite form here is rude in a neighbouring valley. You distrust the arrogant native who insists his way of speaking is the only way.

Your speech is curious and careful. You attempt the foreign greeting. You ask before you assume. You enjoy a pun across two languages.";
}
