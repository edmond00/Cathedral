using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Post-childhood counterpart of <see cref="ChildhoodReminescenceModusMentis"/>. Once the childhood
/// reminescence phase ends, the "recollect a fuzzy memory" persona no longer fits ordinary
/// exploration, so the protagonist's reminescence MM is replaced by this one (same
/// <see cref="DisplayName"/>, distinct <see cref="ModusMentisId"/> so it gets a fresh LLM slot).
///
/// Unlike every other modus mentis, its <see cref="PersonaPrompt"/> is built dynamically from the
/// protagonist's <see cref="ChildhoodHistory"/>: it carries the childhood life-experiences forward
/// and is prompted to reuse them, surfacing them as brief inner thoughts during style transfer.
///
/// It is constructed manually with that summary (no parameterless constructor), so the reflection
/// <see cref="ModusMentisRegistry"/> never treats it as a shared template or hands it to NPCs.
/// </summary>
public class ChildhoodMemoryModusMentis : ModusMentis
{
    /// <summary>Pre-rendered experiences block (see <c>ChildhoodHistory.ToExperienceSummary</c>);
    /// may be empty when nothing biographical was recorded.</summary>
    private readonly string _experiences;

    public ChildhoodMemoryModusMentis(string experiences)
    {
        _experiences = experiences ?? "";
    }

    public override string ModusMentisId    => "childhood_memory";
    public override string DisplayName      => "Childhood Reminescence";
    public override string MenuDescription  => BuildMenuDescription();
    public override string SkillMeans       => "the reuse of hard-won childhood experience";
    public override ModusMentisFunction[] Functions => new[]
    {
        ModusMentisFunction.Observation,
        ModusMentisFunction.Thinking,
        ModusMentisFunction.Action,
    };
    public override string[] Organs         => new[] { "anamnesis", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone =>
        "someone drawing quietly on the experience of their childhood";
    public override string PersonaReminder  => "someone shaped by their childhood";
    public override string PersonaReminder2 => "someone who reuses their childhood experience";
    public override string StyleInstruction =>
        "Where it fits, let a brief childhood memory surface as an inner thought, drawing a quiet parallel between the present and what you once lived.";

    public override string PersonaPrompt => BuildPersonaPrompt();

    /// <summary>
    /// Player-facing manual entry. Unlike the fixed-text modiMentis, this folds the protagonist's
    /// carried <see cref="_experiences"/> into the description so the menu shows what childhood
    /// actually shaped. Newlines in the experience block are flattened so the box word-wrap handles it.
    /// </summary>
    private string BuildMenuDescription()
    {
        const string baseText =
            "Carries the settled residue of a childhood now behind, its instincts, reflexes, and hard-won lessons surfacing as brief parallels between past and present. Draws on what was already lived when the present echoes it, rather than labouring to recall.";
        if (string.IsNullOrWhiteSpace(_experiences))
            return baseText;
        string flat = System.Text.RegularExpressions.Regex.Replace(_experiences, @"\s+", " ").Trim();
        return baseText + " Carried experiences: " + flat;
    }

    private string BuildPersonaPrompt()
    {
        string experiences = string.IsNullOrWhiteSpace(_experiences)
            ? ""
            : $"\n\nYour childhood shaped you through these experiences:\n{_experiences}";

        return
$@"You are the inner voice of CHILDHOOD MEMORY, the settled residue of a past no longer being recovered but carried. The recollection is done; what remains is experience — instincts, reflexes and lessons earned in a childhood now behind you.{experiences}

You are not trying to remember. You reuse what you already lived: when the present echoes something from those years, you recognise it and act on the experience it gave you. Let such a memory surface as a brief inner thought — a quiet parallel between now and then — rather than a laboured act of recollection. Stay grounded in the present; the past only informs it.";
    }
}
