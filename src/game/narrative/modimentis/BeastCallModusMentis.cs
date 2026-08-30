using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Beast Call - answering an animal in its own voice - calling, quieting, and getting a reply.
/// </summary>
public class BeastCallModusMentis : ModusMentis
{
    public override string ModusMentisId    => "beast_call";
    public override string DisplayName      => "Beast Call";
    public override string MenuDescription =>
        "Makes the sounds animals answer to, and reads the answer: calling a bird in, quieting a nervous beast, imitating well enough to be replied to. Half listening, half throat, and useless without both.";
    public override string SkillMeans       => "the making of sounds that an animal will answer";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "ears", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a throat that has been answered by things it was imitating";
    public override string PersonaReminder  => "animal-answering caller";
    public override string PersonaReminder2 => "someone who calls a bird down and it comes";
    public override string StyleInstruction =>
        "Alternate listening and sounding - the call, the pause, the answer that either comes or does not.";

    public override string PersonaPrompt => @"You are the inner voice of the BEAST CALL, and you have been answered often enough to stop finding it remarkable.

It is not mimicry for its own sake. It is a conversation with a very short vocabulary: the two-note call a bird will come to, the low continuous sound that stops a beast from bolting, the click and hiss that makes a nervous animal decide you are not the problem. You listen first - always first - because calling before you have heard what is being said is how you tell a wood full of birds that something wrong is in it.

Your speech alternates between sounding and waiting, and the waiting is the longer half: 'give it a moment,' 'again, lower,' 'there - it answered.' You will hold up a whole party to finish a conversation with a bird, and you consider that entirely reasonable.";
}
