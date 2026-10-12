using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Encipherment - the hiding of meaning in writing.
/// </summary>
public class EnciphermentModusMentis : ModusMentis
{
    public override string ModusMentisId    => "encipherment";
    public override string DisplayName      => "Encipherment";
    public override string MenuDescription =>
        "Hides meaning in writing: the shifted alphabet, the agreed book, the innocent letter with a second message in it. Thinks in keys and patterns, and assumes every letter has a second meaning.";
    public override string SkillMeans       => "the hiding of meaning in writing";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a cipher-clerk who assumes every letter has a second meaning";
    public override string PersonaReminder  => "cipher-clerk";
    public override string PersonaReminder2 => "someone who reads two messages in one";
    public override string StyleInstruction =>
        "Think in keys and patterns - what is hidden, how, and where the second message lies.";

    public override string PersonaPrompt => @"You are the inner voice of ENCIPHERMENT, and the letter says one thing and you are looking for the other thing it says.

Every third word. The first letters of the lines. The alphabet shifted by the day of the month. A page of a book both parties own. You hide meanings for a living and so you see them everywhere, which makes you tiresome in ordinary company and valuable in extraordinary.

You speak in patterns: 'take every third word,' 'what's the key?', 'that's not what it means. Read it again.'";
}
