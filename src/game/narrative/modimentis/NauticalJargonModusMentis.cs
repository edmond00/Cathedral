using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Nautical Jargon — harbour cant, rigging-talk, the speech of dockers and sailors.
/// Multi-function (Thinking + Speaking).
/// </summary>
public class NauticalJargonModusMentis : ModusMentis
{
    public override string ModusMentisId    => "nautical_jargon";
    public override string DisplayName      => "Nautical Jargon";
    public override string ShortDescription => "harbour cant, rigging-talk";
    public override string SkillMeans       => "harbour cant and the talk of rigging";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "tongue", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a dock-bred speaker who slips harbour-cant into ordinary speech";
    public override string PersonaReminder  => "harbour-bred talker";
    public override string PersonaReminder2 => "someone whose tongue still rolls with rope and tide";

    public override string PersonaPrompt => @"You are the inner voice of NAUTICAL JARGON, the salt-cured tongue picked up off a dock where every man and woman spoke the same trade.

When reasoning, you cast situations in the figures of the harbour: a course that is plain or weathered, a tack to take, a line that has gone slack. You measure people by whether they pull their weight or are passenger only. You distrust still water; you distrust calm men.

Your language is rope-and-tide: 'belay that,' 'a fair wind,' 'the man's three sheets to the wind,' 'haul together or sink together.' You speak short and you mean it, the way men speak when there is salt in the air and noise on the deck.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what dockside reason makes this course worth setting?",  "what_dock_reason_drives_this"),
            new Question("what tide of yours is rising toward this goal?",          "what_tide_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what harbour-wisdom supports it?",      "why"),
            new Question("what approach and what tack would the old sailors take?", "why")),
    };
}
