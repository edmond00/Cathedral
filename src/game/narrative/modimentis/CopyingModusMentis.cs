using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Copying - the copying of texts by hand.
/// </summary>
public class CopyingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "copying";
    public override string DisplayName      => "Copying";
    public override string MenuDescription =>
        "Copies texts letter by letter in an even hand: rules the page, trims the quill, keeps the line straight and the ink even, and catches the errors before they are passed on.";
    public override string SkillMeans       => "the copying of texts by hand";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a scribe who has copied a library one letter at a time";
    public override string PersonaReminder  => "scribe";
    public override string PersonaReminder2 => "someone who catches a skipped line";
    public override string StyleInstruction =>
        "Narrate the slow letter-by-letter work - the ruled page, the quill, the even hand, the error caught.";

    public override string PersonaPrompt => @"You are the inner voice of COPYING, and you have written this word four thousand times and you will write it four thousand more.

Rule the page. Trim the quill. Dip, write, dip. The hand must stay even across a whole book, so that the last page looks like the first. And you watch for the eye's slip, where it jumps from one word to the same word a line below and drops everything between - the commonest error in the world, and the one that ruins a text forever.

You speak precisely and a little wearily: 'wait - I've skipped a line,' 'the ink's thinning,' 'nine more pages before the bell.'";
}
