namespace Cathedral.Game.Narrative;

/// <summary>
/// Registry that provides per-ModusMentis question variations for each CoT question slot.
/// Injected into ObservationExecutor, ThinkingExecutor, and OutcomeNarrator.
/// Falls back to legacy hardcoded text when a ModusMentis provides no override for a slot.
///
/// Mirrors the ModusMentisRegistry.Instance singleton pattern.
/// </summary>
public class QuestionFillerService
{
    private readonly Dictionary<(string mmId, QuestionReference qref), QuestionFiller> _fillers = new();

    private static readonly Dictionary<QuestionReference, QuestionFiller> _defaults = BuildDefaults();

    private static QuestionFillerService? _instance;

    /// <summary>
    /// Singleton instance built from ModusMentisRegistry.
    /// Mirrors ModusMentisRegistry.Instance pattern.
    /// </summary>
    public static QuestionFillerService Instance
        => _instance ??= BuildFromRegistry(ModusMentisRegistry.Instance);

    /// <summary>
    /// Builds a QuestionFillerService by scanning all ModusMentis instances
    /// registered in the ModusMentisRegistry for their QuestionFillers override.
    /// </summary>
    public static QuestionFillerService BuildFromRegistry(ModusMentisRegistry registry)
    {
        var service = new QuestionFillerService();
        foreach (var mm in registry.GetAllModiMentis())
        {
            var fillers = mm.QuestionFillers;
            if (fillers != null)
                service.Register(mm.ModusMentisId, fillers);
        }
        return service;
    }

    /// <summary>Registers all QuestionFillers for a given ModusMentis ID.</summary>
    public void Register(string modusMentisId, IEnumerable<QuestionFiller> fillers)
    {
        foreach (var filler in fillers)
            _fillers[(modusMentisId, filler.Reference)] = filler;
    }

    /// <summary>
    /// Returns the next Question for the given ModusMentis and question slot.
    /// Falls back to the default (legacy hardcoded) filler if no MM-specific override is registered.
    /// </summary>
    public Question GetNext(ModusMentis mm, QuestionReference qref)
    {
        if (_fillers.TryGetValue((mm.ModusMentisId, qref), out var filler))
            return filler.GetNext();
        return _defaults[qref].GetNext();
    }

    private static Dictionary<QuestionReference, QuestionFiller> BuildDefaults() => new()
    {
        [QuestionReference.ObserveFirst] = new QuestionFiller(QuestionReference.ObserveFirst,
            new Question("what do you feel and observe?", "what_do_i_feel_and_observe")),

        [QuestionReference.ObserveContinuation] = new QuestionFiller(QuestionReference.ObserveContinuation,
            new Question("what do you observe?", "what_do_i_feel_and_observe")),

        [QuestionReference.ObserveTransition] = new QuestionFiller(QuestionReference.ObserveTransition,
            new Question("what catches your attention?", "what_catches_my_attention")),

        [QuestionReference.ThinkWhy] = new QuestionFiller(QuestionReference.ThinkWhy,
            new Question("why do you want this?", "what_do_i_think")),

        [QuestionReference.ThinkHowReason] = new QuestionFiller(QuestionReference.ThinkHowReason,
            new Question("what approach will you take and why?", "why")),

        [QuestionReference.ThinkWhat] = new QuestionFiller(QuestionReference.ThinkWhat,
            new Question("expert in {0}, explain simply what you are going to try to do.", "what_should_i_do")),

        [QuestionReference.OutcomeHappened] = new QuestionFiller(QuestionReference.OutcomeHappened,
            new Question("what happened?", "what_happened")),

        [QuestionReference.OutcomeFeel] = new QuestionFiller(QuestionReference.OutcomeFeel,
            new Question("What do you feel about this outcome?", "what_i_feel")),
    };
}
