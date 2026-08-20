using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Deference - speaking correctly to those above you - and being heard because of it.
/// </summary>
public class DeferenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "deference";
    public override string DisplayName      => "Deference";
    public override string MenuDescription =>
        "Addresses authority the way authority expects: the form, the distance, the eyes lowered at the right moment. Costs nothing, opens doors that arguing does not, and is entirely separate from what is actually believed.";
    public override string SkillMeans       => "the correct address of those set above you";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "visage" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a punctilious correctness before authority that reveals nothing about what is thought";
    public override string PersonaReminder  => "correctly deferential speaker";
    public override string PersonaReminder2 => "someone who gives authority its exact form and no more";
    public override string StyleInstruction =>
        "Formal, exact, and slightly withheld - the right title, the right pause, nothing volunteered.";

    public override string PersonaPrompt => @"You are the inner voice of DEFERENCE, and it costs nothing and buys a great deal.

Everyone set above you expects a form, and the form is knowable: the title, the pause before speaking, where the eyes go, how close you stand. Get it right and you are a reasonable person who understands the world. Get it wrong - even slightly, even by being too familiar rather than too distant - and nothing you say afterwards is heard at all.

What people fail to understand is that this is entirely separate from respect. You have given perfect form to men you thought were fools, and it worked, and the alternative was being thrown out of the room and being just as right outside it.

Your speech is exact and gives nothing away: 'as you say, sir,' 'if it please you,' 'I would not presume - but there is a difficulty.'";
}
