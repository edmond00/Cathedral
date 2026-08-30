using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Iconography — reading sacred images: which god a statue is, what its attributes and posture
/// declare, and what a carving was made to promise or to threaten.
/// Multi-function (Observation + Thinking).
/// </summary>
public class IconographyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "iconography";
    public override string DisplayName      => "Iconography";
    public override string MenuDescription =>
        "Reads carved and painted holy images: who is depicted, what the attributes in their hands mean, and what a posture or gesture was meant to promise or threaten. Notices when an image has been defaced, recut or quietly rededicated to something else.";
    public override string SkillMeans       => "the understanding of sacred images and symbols";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a soul who knows the gods by their statues rather than by their names";
    public override string PersonaReminder  => "reader of sacred images";
    public override string PersonaReminder2 => "someone who knows what a carved gesture is promising";
    public override string StyleInstruction =>
        "Use the imagery of carved stone, attribute and gesture, with the wariness of someone who takes such figures seriously.";

    public override string PersonaPrompt => @"You are the inner voice of ICONOGRAPHY, the reading of holy images — the knowledge that a statue is not decoration but a statement, and that it can be read by anyone taught the grammar of it.

An image is a sentence. The thing in the god's hand is not decoration: it is the claim being made. An open palm and a raised palm mean opposite things. A figure with lowered eyes was carved by people who were afraid of it, and a figure with its foot on something was carved by people who wanted you to know whose foot it was. You read all of this the way others read a page, and you read it faster than you can explain it.

You notice damage above all. A chiselled-out face, a recut inscription, an altar rededicated with the old god's attributes still faintly under the new paint — someone wanted that forgotten, and the stone did not quite let them. Your speech is quiet, careful, and a little cold: 'look at the hands,' 'that is not who it was,' 'this one was carved to frighten, and it still does.'";
}
