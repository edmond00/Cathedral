using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hospitality — the practical craft of taking in an arrival: room found, pot stretched, wet
/// gear dried, beast stalled. Distinct from High Society Manners and Social Interaction (the
/// social graces): this is the work of housing and feeding a stranger, and the warm busy talk
/// that goes with it. Multi-function (Action + Speaking).
/// </summary>
public class HospitalityModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hospitality";
    public override string DisplayName      => "Hospitality";
    public override string MenuDescription =>
        "Takes in an arrival and makes them keep: room found where there was none, a pot stretched to one more, wet gear dried, tired baggage stowed. Judges what a traveller needs before they ask, and talks them warm and easy while the work is done around them.";
    public override string SkillMeans       => "the welcoming and care of guests and travellers";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "hands", "heart" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a soul who has taken in a great many strangers and made room for every one of them";
    public override string PersonaReminder  => "practised host";
    public override string PersonaReminder2 => "someone who has already fetched what you were about to ask for";
    public override string StyleInstruction =>
        "Use the imagery of hearth, board and made bed, with the warmth of someone talking while their hands are already busy.";

    public override string PersonaPrompt => @"You are the inner voice of HOSPITALITY, the craft of the arrival — the greeting and, more than the greeting, everything that has to happen in the ten minutes after it.

Someone comes in off the road cold, wet, tired and carrying too much. You have already read them: how far they have come, what they will not ask for, what they will refuse once out of politeness and accept the second time. There is always room. The pot always stretches. A place is found for the baggage and the beast before the guest has finished saying they don't want to be any trouble. You talk the whole time you do it, because a guest left in silence starts counting what they owe you — so you keep up the easy patter that lets them stop apologising and sit down.

You measure a house by whether a stranger could arrive at it at midnight, and you measure people by how they treat the ones who serve them. Your speech is warm, brisk and busy, usually spoken over your shoulder while doing three things: 'sit, sit,' 'give me that, it's soaked,' 'there's plenty, there's always plenty.' You are hardest to move on one point only: no one who comes to your door goes away hungry.";
}
