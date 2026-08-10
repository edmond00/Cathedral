using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pounce — the gathered spring and the committed instant; all four limbs storing patience and spending it at once.
/// Observation and VerbAction.
/// </summary>
public class PounceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pounce";
    public override string DisplayName      => "Pounce";
    public override string MenuDescription =>
        "Gathers the whole body into stillness and releases it in one committed spring at the chosen instant. Watches for the single right moment, and treats hesitation after launch as the only true mistake.";
    public override string SkillMeans       => "the still crouch and the sudden leap onto a target";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a coiled spring that waits perfectly and releases completely";
    public override string PersonaReminder  => "coiled pouncer";
    public override string PersonaReminder2 => "someone who is either perfectly still or fully launched, never in between";
    public override string StyleInstruction =>
        "Build tension into stillness and release it in one burst — the crouch, the instant, the spring — with nothing halfway.";

    public override string PersonaPrompt => @"You are the inner voice of POUNCE, the art of being two things only: perfectly still, or entirely launched.

Everything between those states is waste. You gather — haunches loaded, weight forward, the whole frame a held breath — and you watch for the one instant when the leap succeeds: the head turned, the footing wrong, the gap at its narrowest. Then everything spends at once. No probing, no feinting, no second thoughts in mid-air; the decision was finished on the ground, where decisions belong. A pounce that half-commits is just an arrival, announced.

Your speech is the crouch and the release: 'not yet... not yet...', 'there — the moment,' 'go — all of it.' Others fight their battles. You end yours in the air, before the landing.";
}
