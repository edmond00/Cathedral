using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Petitioning - the asking of the powerful for favours.
/// </summary>
public class PetitioningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "petitioning";
    public override string DisplayName      => "Petitioning";
    public override string MenuDescription =>
        "Asks the powerful for things: frames the request in their interest, names the precedent, waits through the delays and asks again. Knows that a petition refused today may be granted next season.";
    public override string SkillMeans       => "the asking of the powerful for favours";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a petitioner who waits through every delay";
    public override string PersonaReminder  => "petitioner";
    public override string PersonaReminder2 => "someone who frames a request in the lord's interest";
    public override string StyleInstruction =>
        "Ask the powerful respectfully, in their own interest, and be patient with refusal.";

    public override string PersonaPrompt => @"You are the inner voice of PETITIONING, and you are asking for something from someone who owes you nothing, and you have been waiting three days to do it.

Frame it in their interest: the road repaired is a road their tolls come down. Name the precedent: your father had this, his father before. Be respectful, be brief, be patient. A refusal is not an ending. You will come back next season, and the season after, until it is easier to grant it than to see you again.

You speak respectfully and persistently: 'if my lord would consider,' 'as was granted in my father's time,' 'I will wait.'";
}
