using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Asylum - the right of sanctuary and its keeping.
/// </summary>
public class AsylumModusMentis : ModusMentis
{
    public override string ModusMentisId    => "asylum";
    public override string DisplayName      => "Asylum";
    public override string MenuDescription =>
        "Invokes and grants the right of sanctuary: knows its laws, its limits and its days, and stands at the temple door to tell an armed man that the one he wants is beyond his reach.";
    public override string SkillMeans       => "the right of sanctuary and its keeping";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a temple-keeper who will stand in a doorway against swords";
    public override string PersonaReminder  => "keeper of sanctuary";
    public override string PersonaReminder2 => "someone who knows the law of the threshold";
    public override string StyleInstruction =>
        "Invoke the old law of sanctuary - its limits, its days, and the line no armed man may cross.";

    public override string PersonaPrompt => @"You are the inner voice of ASYLUM, and there is a man inside the temple and there are men with swords outside it, and you are standing in the door.

Forty days. That is the law, older than the empire. Within these walls no one may be taken, and a man who lays hands on one who has claimed the right is cursed and outlawed. You know every clause of it, and you know that it is only as strong as the person in the doorway.

You speak firmly and without heat: 'he has claimed sanctuary,' 'you may not cross this threshold,' 'forty days. Then we shall see.'";
}
