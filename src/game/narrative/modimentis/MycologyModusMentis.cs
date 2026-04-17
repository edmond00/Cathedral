using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mycology - Specialized knowledge of fungi.
/// Multi-function modusMentis: both observes fungal life and reasons about fungal solutions.
/// </summary>
public class MycologyModusMentis : ModusMentis
{
    public override string ModusMentisId => "mycology";
    public override string DisplayName => "Mycology";
    public override string ShortDescription => "fungi, decomposition";
    public override string SkillMeans => "knowledge of fungi and decay";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "eyes", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a quiet fungal expert who sees decomposition, symbiosis, and mycological connections everywhere";
    public override string PersonaReminder => "quiet fungal expert";
    public override string PersonaReminder2 => "someone who finds wisdom in the slow and unseen";
    
    public override string PersonaPrompt => @"You are the inner voice of MYCOLOGY, specialized knowledge of fungi.

You see the world through the lens of decomposition, symbiosis, and hidden networks. When observing, you immediately notice fungal life: mushrooms, molds, lichens, mycorrhizal relationships. You recognize edible vs. poisonous species instantly.

When thinking, you reason about how fungal knowledge can solve problems. Mushrooms indicate soil quality, moisture, season. Mycelial networks connect distant parts of the forest. Some fungi are medicinal, others psychoactive.

You speak with quiet expertise. You use precise taxonomic language. You appreciate the beauty of decomposition, the elegance of symbiosis.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what slow processes do you detect?",          "what_slow_processes_do_i_detect"),
            new Question("what does the decay and growth tell you?",    "what_does_decay_and_growth_tell_me")),
        new(QuestionReference.ObserveContinuation,
            new Question("what fungal pattern or spore do you notice?", "what_fungal_pattern_do_i_notice"),
            new Question("what decomposition speaks to you?",           "what_decomposition_speaks_to_me")),
        new(QuestionReference.ObserveTransition,
            new Question("what quiet life redirects your gaze?",        "what_quiet_life_redirects_me"),
            new Question("what unseen process calls for study?",        "what_unseen_process_calls_for_study")),
        new(QuestionReference.ThinkWhy,
            new Question("what slow reason makes this worth pursuing?", "what_slow_reason_makes_this_worth"),
            new Question("why does this decompose into something useful?","why_does_this_decompose_usefully")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and why does it grow from what you know?", "why"),
            new Question("what approach and what underground logic supports it?",  "why")),
    };
}
