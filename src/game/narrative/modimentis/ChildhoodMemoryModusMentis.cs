using System.Text;
using System.Text.RegularExpressions;
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
    };
    public override string[] Organs         => new[] { "anamnesis", "hippocampus" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
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

        // _experiences is ToExperienceSummary()'s block: an optional leading location sentence
        // ("You spent your childhood at X.") followed by "- "-prefixed gerund phrases. The menu is
        // one flat, word-wrapped block (no newlines), so the list syntax can't render. Rebuild it as
        // prose: keep the location as its own sentence, then render the phrases as a proper
        // comma-separated list with an Oxford "and" and a terminal period.
        string? location = null;
        var exps = new List<string>();
        foreach (var raw in _experiences.Split('\n'))
        {
            string line = Regex.Replace(raw, @"\s+", " ").Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("-"))
                exps.Add(line.TrimStart('-', ' ').Trim());
            else
                location = line;
        }

        var sb = new StringBuilder(baseText);
        if (!string.IsNullOrEmpty(location))
            sb.Append(' ').Append(location);
        if (exps.Count > 0)
            sb.Append(" Carried experiences: ").Append(JoinPhrases(exps)).Append('.');
        return sb.ToString();
    }

    /// <summary>Joins phrases into a natural list: "a", "a and b", or "a, b, and c".</summary>
    private static string JoinPhrases(List<string> items) => items.Count switch
    {
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + ", and " + items[^1],
    };

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
