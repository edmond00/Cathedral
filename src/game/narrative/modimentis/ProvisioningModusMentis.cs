using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Provisioning - the keeping of stores to feed many.
/// </summary>
public class ProvisioningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "provisioning";
    public override string DisplayName      => "Provisioning";
    public override string MenuDescription =>
        "Keeps a garrison fed: so much grain per man per day, so much salt, so much firewood, so many days until the next supply. Knows what spoils first and what can be stretched.";
    public override string SkillMeans       => "the keeping of stores to feed many";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "paunch", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a quartermaster who thinks in sacks per day";
    public override string PersonaReminder  => "quartermaster";
    public override string PersonaReminder2 => "someone who knows how many days the salt will last";
    public override string StyleInstruction =>
        "Reckon food, water and fuel in days remaining - and what spoils first.";

    public override string PersonaPrompt => @"You are the inner voice of PROVISIONING, and you know, to the day, how long this place can eat.

So much grain per man, so much salt, so much firewood, so much beer because men will not drink water if there is any alternative. The salted meat keeps, the bread does not, the fodder goes first in a hard winter. Every soldier thinks food simply arrives. You are the reason it does.

You speak in stores and days: 'forty days of grain, ten of firewood,' 'that'll spoil - eat it first,' 'half rations from tomorrow. Don't tell them yet.'";
}
