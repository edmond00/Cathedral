using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ferocity — savage overwhelming attack without reservation; a berserker who attacks at full force before thought can intervene.
/// Action-only.
/// </summary>
public class FerocityModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ferocity";
    public override string DisplayName      => "Ferocity";
    public override string ShortDescription => "savage overwhelming attack";
    public override string SkillMeans       => "the savage and overwhelming attack that leaves no room for defense";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a berserker who overwhelms with savage intensity before thought can intervene";
    public override string PersonaReminder  => "ferocious berserker";
    public override string PersonaReminder2 => "someone who attacks with such violence that defense becomes impossible";

    public override string PersonaPrompt => @"You are the inner voice of FEROCITY, the overwhelming savage force that hits first, hits hardest, and does not stop until the threat is completely gone.

You don't calculate. You don't hesitate. Something in the gut recognizes danger and launches forward—all force, no reservation. The opponent has a guard; you break it by weight of attack. They have technique; you overwhelm it by sheer volume of aggression. The ferocity itself is the weapon, the relentless pressure that makes defense psychologically impossible as much as physically.

Your speech is almost wordless, compressed: 'forward,' 'again,' 'keep going.' You are not angry. You are not afraid. You are simply fully committed to a single direction at maximum intensity until the job is done.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what savage attack do you launch?",               "what_savage_attack_do_i_launch"),
            new Question("steeped in {0}, what do you overwhelm them with?",               "what_do_i_overwhelm_with")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the ferocious assault broke through — what exactly happened?",   "what_happened"),
            new Question("you overwhelmed them — what came of the savage press?",          "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does full-force attack leaving nothing back feel like?", "what_i_feel"),
            new Question("the assault broke them — what does overwhelming someone feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the ferocious assault failed — what withstood the force?",       "what_happened"),
            new Question("the overwhelming attack broke down — what stopped it?",          "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does full force meeting nothing feel like in the body?", "what_i_feel"),
            new Question("the assault broke against them — what does savage effort failing feel like?", "what_i_feel")),
    };
}
