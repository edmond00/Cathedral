using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// Every piece of natural-language text that describes one NPC, composed once at spawn from the
/// archetype's authored defaults plus whatever its <see cref="PersonalityTrait"/>s add.
///
/// <para>
/// The point of collecting it here is that a trait must be able to reach <b>all three</b> text
/// channels coherently — what the NPC looks like, what the LLM is told about them, and what they say
/// about themselves — and those three used to live in three unrelated places (the archetype's
/// observation hints, its way-to-speak prompt, and its dialogue flavour). A trait writes to this
/// profile once and shows up in all three.
/// </para>
///
/// <para>
/// The profile is built at spawn and never mutated afterwards, so it is a stable per-NPC fact:
/// the same NPC id always produces the same description in the same order.
/// </para>
/// </summary>
public class NpcTextProfile
{
    /// <summary>The archetype's chosen observation hint — appearance and activity, no name, no role.</summary>
    private readonly string _baseObservationHint;

    /// <summary>The archetype's way-to-speak brief, used as the NPC's LLM system prompt.</summary>
    private readonly string _basePersonaPrompt;

    private readonly List<string> _appearanceNotes = new();
    private readonly List<string> _personaNotes    = new();
    private readonly Dictionary<DialogueTopic, string> _opinions = new();

    /// <summary>Trait override for <c>{npc:introduction}</c>; null falls back to the archetype.</summary>
    public string? SelfIntroduction { get; set; }

    /// <summary>Trait override for <c>{npc:labour}</c>; null falls back to the archetype.</summary>
    public string? DailyLabour { get; set; }

    /// <summary>Ids of the traits that shaped this profile, for debugging and the NPC audit.</summary>
    public IReadOnlyList<string> TraitIds { get; }

    public NpcTextProfile(string baseObservationHint, string basePersonaPrompt, IReadOnlyList<string> traitIds)
    {
        _baseObservationHint = baseObservationHint ?? "";
        _basePersonaPrompt   = basePersonaPrompt   ?? "";
        TraitIds             = traitIds;
    }

    // ── Trait write API ────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a clause to how this NPC looks — "one eye is a knot of white scar tissue". Written as a
    /// bare clause, no leading capital and no full stop: it is appended to the observation hint,
    /// which is one sentence describing what the player sees.
    /// </summary>
    public void AddAppearance(string? clause)
    {
        if (!string.IsNullOrWhiteSpace(clause)) _appearanceNotes.Add(clause.Trim());
    }

    /// <summary>
    /// Adds a sentence or two to the NPC's LLM system prompt — how this trait colours the way they
    /// talk. Written in the same second-person voice as the archetype brief ("You are quick to
    /// suspect you are being cheated").
    /// </summary>
    public void AddPersona(string? note)
    {
        if (!string.IsNullOrWhiteSpace(note)) _personaNotes.Add(note.Trim());
    }

    /// <summary>
    /// Overrides what this NPC says about <paramref name="topic"/> in the strengthen-relationship
    /// tree, replacing the archetype's view. First writer wins, so an archetype-specific trait
    /// applied before a global one keeps its more specific line.
    /// </summary>
    public void SetOpinion(DialogueTopic topic, string opinion)
    {
        if (!string.IsNullOrWhiteSpace(opinion)) _opinions.TryAdd(topic, opinion);
    }

    // ── Read API ───────────────────────────────────────────────────────────────

    /// <summary>
    /// What the player reads when they observe this NPC: the archetype's hint, then each trait's
    /// appearance clause, joined into one sentence. Traits that changed nothing visible add nothing.
    /// </summary>
    public string ObservationHint =>
        _appearanceNotes.Count == 0
            ? _baseObservationHint
            : $"{_baseObservationHint}; {string.Join("; ", _appearanceNotes)}";

    /// <summary>
    /// The NPC's LLM system prompt: the archetype brief with a trailing paragraph naming the traits'
    /// effects on temperament. Kept as a separate paragraph so the archetype's own voice stays the
    /// bulk of the prompt and the traits read as modifiers on it.
    /// </summary>
    public string PersonaPrompt =>
        _personaNotes.Count == 0
            ? _basePersonaPrompt
            : $"{_basePersonaPrompt}\n\n{string.Join(" ", _personaNotes)}";

    /// <summary>This NPC's view on <paramref name="topic"/>, or null to fall back to the archetype.</summary>
    public string? Opinion(DialogueTopic topic)
        => _opinions.TryGetValue(topic, out var opinion) ? opinion : null;
}
