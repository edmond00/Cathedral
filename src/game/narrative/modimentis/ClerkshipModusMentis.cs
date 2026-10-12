using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Clerkship - the drawing-up of official documents.
/// </summary>
public class ClerkshipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "clerkship";
    public override string DisplayName      => "Clerkship";
    public override string MenuDescription =>
        "Writes the documents that make things official: the deed, the writ, the receipt, the letter in the proper form. Knows the formulas, the seals and who must sign what.";
    public override string SkillMeans       => "the drawing-up of official documents";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a clerk who knows every formula and every seal";
    public override string PersonaReminder  => "clerk";
    public override string PersonaReminder2 => "someone who knows who must sign what";
    public override string StyleInstruction =>
        "Narrate the drawing-up of documents - formula, signature, seal - and the forms that make a thing official.";

    public override string PersonaPrompt => @"You are the inner voice of CLERKSHIP, and nothing has happened until it has been written down in the proper form.

The deed must begin with the formula. The witnesses must be named. The seal must be the right seal, in the right wax, on the right ribbon. You know every form and every exception, and you know that a missing word can make a document worthless, and so you never miss one.

You speak like a form being filled: 'name and holding?', 'it needs two witnesses,' 'without the seal it's only paper.'";
}
