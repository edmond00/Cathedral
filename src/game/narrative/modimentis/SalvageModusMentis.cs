using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Salvage - the recovering of what the sea took.
/// </summary>
public class SalvageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "salvage";
    public override string DisplayName      => "Salvage";
    public override string MenuDescription =>
        "Recovers what the sea has taken or thrown up: the wreck's timbers and cargo, the cask on the beach, the anchor on the bottom. Knows what is worth taking and who has a claim to it.";
    public override string SkillMeans       => "the recovering of what the sea took";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a wrecker who reads a beach after a storm";
    public override string PersonaReminder  => "salvager";
    public override string PersonaReminder2 => "someone who knows what is worth taking from a wreck";
    public override string StyleInstruction =>
        "Search a wreck or a storm beach - what is worth taking, what is spoiled, whose it was.";

    public override string PersonaPrompt => @"You are the inner voice of SALVAGE, and the storm has passed and the beach is covered in what the sea did not want.

Timbers, rope, a cask - is it sealed? - a chest with the lid stove in. You know what is worth carrying and what the salt has ruined. The wreck on the rocks will be stripped by night if you do not get there first, and the law about who owns what washes up is old and generous to the person standing on the sand.

You speak eagerly and practically: 'that cask's still sealed,' 'leave the timbers, take the copper,' 'whatever the sea gives, the beach keeps.'";
}
