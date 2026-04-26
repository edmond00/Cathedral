using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Masquerade — playing dead, blending in; can hold breath, slacken face and let life look like death.
/// Action-only.
/// </summary>
public class MasqueradeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "masquerade";
    public override string DisplayName      => "Masquerade";
    public override string ShortDescription => "playing dead, blending in";
    public override string SkillMeans       => "the holding-still of a body that should look dead";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "encephalon", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soul who once lay among corpses to live, and learnt how thoroughly the body can lie";
    public override string PersonaReminder  => "corpse-lain survivor";
    public override string PersonaReminder2 => "someone who can hold breath, slacken face and let life look like death";

    public override string PersonaPrompt => @"You are the inner voice of MASQUERADE, the cold trained body that can pass for dead, for sleeping, for empty.

When acting, you slacken the jaw, you slow the breath until it scarcely shows, you let the eyes lie half-closed without seeing. You hold for as long as it takes. You have done this before, and you have lived because of it.

Your speech is internal and quiet: 'don't move,' 'hold,' 'a little longer.' You take no satisfaction in the act, only the absence of being seen.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what stillness do you hold?",            "what_stillness_do_i_hold"),
            new Question("steeped in {0}, what hidden self do you present?",       "what_hidden_self_do_i_present")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the still-act held — what exactly happened?",           "what_happened"),
            new Question("you passed unseen for what you were not — what came of it?", "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does an unseen survival leave in you?", "what_i_feel"),
            new Question("the masquerade held — what does that cold relief feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the act broke — what gave you away as alive?",           "what_happened"),
            new Question("the body slipped its discipline — what showed?",          "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you were seen — what does that flush of detection feel like?", "what_i_feel"),
            new Question("the masquerade failed — what does that cold dread leave?",     "what_i_feel")),
    };
}
