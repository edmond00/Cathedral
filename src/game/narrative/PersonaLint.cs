using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The <c>--mm-audit</c> section on persona wording: which modi mentis's <see cref="ModusMentis.StyleInstruction"/>
/// or <see cref="ModusMentis.PersonaReminder2"/> is written as a list of things. A ✗ line fails the audit.
///
/// <para><b>Why.</b> Both fields are restated in every narration request, and the shipped model treats what
/// they name as present. A style that named its craft's objects ("Use images of rendering fat, the dip and the
/// cooling coat, wicks, wax and the steady flame") turned a greeting into "I will dip the wick in the tallow…
/// twenty times", and a reminder written as a picture ("someone who finds the hidden nest by listening") put a
/// nest in nine answers out of sixteen. Screened across the whole catalogue and blind-scored (tools/prompts,
/// NOTES.md, "Persona fields"), the worst personas went from fidelity 0.25 to 1.41 of 2 once the two fields
/// followed the rules below.</para>
///
/// <list type="number">
/// <item><b>A style is a manner, then a lens on whatever is in the scene</b>: "Slow, patient and exact - judge
/// things by the patience in them, well made or rank." Never the persona's own objects or acts, and never
/// "images of…" or "narrate the…", which open such a list. The manner is what lets a one-line action carry
/// the persona without inventing an act for it.</item>
/// <item><b>A reminder is a disposition, not a picture</b>: "someone who listens before looking", not "someone
/// who finds the hidden nest by listening".</item>
/// </list>
///
/// <para><b>How a thing is told.</b> A word counts when it is a distinct noun and concrete. Both are predicted
/// from the word's GloVe vector, which the game already loads (<see cref="WordEmbedding"/>), by two linear fits
/// to the Brysbaert, Warriner &amp; Kuperman (2014) norms, made in tools/prompts (persona_lint.py is the Python
/// twin of this class; the weights are data/concreteness_glove100.json): concreteness, 1 abstract to 5
/// concrete, held-out r = 0.79, a word rated 3.8 or more counting (oil 4.9, wick 4.7, silver 4.5; delay 2.8,
/// patience 2.1); and noun or not, held-out accuracy 0.83. A field naming two or more such words, or a style
/// with a props phrase, is flagged. Across the catalogue as it was, the count ranked the personas by how much
/// of their style the model carried into scenes (Spearman 0.46). Without the vectors (<c>--no-embeddings</c>)
/// the section says so and checks only the props phrase.</para>
/// </summary>
public static class PersonaLint
{
    public const float ThingRating = 3.8f;
    public const float NounScore = 0.5f;
    public const int StyleThingsMax = 2, ReminderThingsMax = 2;

    private static readonly Regex PropsPhrase = new(
        @"\b(images? of|imagery of|narrate (the|by|through)|describe (the|smoke|water)|use (the )?images)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Word = new("[a-z]+", RegexOptions.Compiled);

    /// <summary>Function words, and the words a style is written with rather than about.</summary>
    private static readonly HashSet<string> Ignored = new((
        "a about above after again against all also am an and any anything are around as at back be " +
        "because been before being below between both but by can clipped color colour could did do does " +
        "doing down dry during each even ever every everything feel feeling few for from front further " +
        "give had has have having he her here hers him his how i if image imagery images in into is it " +
        "its judge just keep let line lines look make many me meet might more most much must my next no " +
        "nor not nothing notice now of off old on once one ones only open or other our out over own " +
        "picture put reach read same see sentence sentences she should single small so some something " +
        "still such take than that the their them then there these they thing things this those though " +
        "through to tone too treat under until up upon very voice was way we were what whatever when " +
        "where which while who whoever whom why will with word words would yet you your ").Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private const float ConcretenessBias = 3.252103f;
    private static readonly float[] ConcretenessWeights =
    {
        0.015711f, 0.091368f, -0.063993f, -0.203897f, -0.036270f, -0.053972f, 0.173607f, 0.077669f,
        0.068611f, -0.065818f, -0.109096f, -0.087660f, -0.057747f, 0.253374f, -0.074951f, 0.228553f,
        0.066810f, -0.142367f, 0.076960f, -0.177147f, -0.097216f, 0.015253f, 0.054206f, 0.107223f,
        0.089644f, 0.159623f, -0.117340f, -0.067042f, 0.010188f, -0.006106f, 0.028592f, 0.020785f,
        0.251749f, 0.116182f, 0.005130f, 0.044130f, -0.045185f, 0.066168f, 0.222389f, -0.019937f,
        0.040358f, -0.062035f, -0.062799f, -0.011951f, -0.023613f, 0.106114f, -0.238399f, -0.071784f,
        0.033374f, 0.123245f, -0.119892f, 0.030818f, -0.043681f, 0.198916f, -0.057359f, -0.059032f,
        -0.262003f, 0.040426f, 0.146539f, 0.087388f, 0.057617f, 0.075649f, 0.037105f, 0.143105f,
        -0.054332f, 0.060289f, 0.030775f, 0.018430f, -0.010089f, 0.019887f, -0.049383f, 0.105640f,
        0.103834f, -0.013631f, -0.020099f, 0.003524f, 0.108748f, 0.057101f, 0.131426f, 0.043885f,
        0.085641f, -0.100012f, 0.059784f, -0.017763f, 0.289208f, -0.024933f, 0.044933f, -0.034831f,
        0.171560f, 0.147034f, 0.155707f, -0.017919f, 0.106884f, -0.001391f, -0.002142f, 0.022471f,
        0.102431f, 0.013604f, 0.153070f, -0.163328f,
    };
    private const float NounBias = 0.462272f;
    private static readonly float[] NounWeights =
    {
        0.034411f, 0.030750f, 0.089691f, -0.128556f, -0.052001f, -0.045040f, 0.133822f, -0.057711f,
        -0.004540f, 0.024398f, -0.040720f, 0.032284f, -0.054443f, 0.051517f, -0.007635f, 0.098250f,
        0.017965f, -0.006362f, 0.003378f, -0.025862f, -0.086152f, -0.057554f, -0.091894f, -0.022758f,
        0.029160f, 0.091463f, 0.015019f, -0.032452f, -0.013798f, -0.101432f, -0.022181f, 0.025573f,
        0.107963f, 0.073244f, 0.004303f, 0.031420f, 0.069507f, 0.034703f, 0.003148f, -0.099517f,
        0.002479f, 0.047024f, -0.035104f, -0.024718f, -0.043946f, -0.066407f, -0.040960f, -0.031156f,
        0.083511f, 0.061403f, -0.025600f, 0.006014f, -0.016594f, 0.241296f, -0.017474f, 0.011101f,
        -0.122162f, -0.071839f, 0.056398f, 0.097847f, 0.005750f, -0.079054f, 0.086503f, 0.003014f,
        -0.046066f, -0.083200f, -0.000595f, -0.099356f, 0.075499f, -0.003142f, 0.014685f, 0.001767f,
        0.035294f, 0.017457f, 0.012695f, 0.015994f, 0.076161f, 0.026685f, -0.039119f, 0.010356f,
        -0.036975f, -0.058825f, 0.029129f, -0.022928f, 0.063452f, 0.080483f, -0.075592f, -0.077814f,
        0.076997f, -0.006672f, 0.037347f, -0.039294f, 0.069736f, 0.108683f, 0.047893f, 0.082480f,
        0.096010f, -0.017636f, 0.035643f, -0.037322f,
    };

    private static float Dot(float[] w, float bias, float[] v)
    {
        float s = bias;
        for (int i = 0; i < w.Length && i < v.Length; i++) s += w[i] * v[i];
        return s;
    }

    /// <summary>The distinct concrete nouns <paramref name="text"/> names, in order of first appearance.</summary>
    public static List<string> Things(string? text)
    {
        var found = new List<string>();
        if (string.IsNullOrEmpty(text) || !WordEmbedding.IsReady) return found;
        foreach (Match m in Word.Matches(text.ToLowerInvariant()))
        {
            var w = m.Value;
            if (w.Length < 3 || Ignored.Contains(w) || found.Contains(w)) continue;
            var v = WordEmbedding.GetVector(w);
            if (v == null) continue;
            if (Dot(ConcretenessWeights, ConcretenessBias, v) >= ThingRating && Dot(NounWeights, NounBias, v) >= NounScore)
                found.Add(w);
        }
        return found;
    }

    /// <summary>What is wrong with <paramref name="mm"/>'s wording, one entry per field; empty when nothing is.</summary>
    public static List<string> Check(ModusMentis mm)
    {
        var faults = new List<string>();
        var styleThings = Things(mm.StyleInstruction);
        bool props = PropsPhrase.IsMatch(mm.StyleInstruction ?? "");
        if (styleThings.Count >= StyleThingsMax || props)
            faults.Add("style names " + (styleThings.Count > 0 ? string.Join(", ", styleThings) : "no things")
                       + (props ? ", with a props phrase" : ""));
        var reminderThings = Things(mm.PersonaReminder2);
        if (reminderThings.Count >= ReminderThingsMax)
            faults.Add("reminder names " + string.Join(", ", reminderThings));
        return faults;
    }

    public static string Section(List<ModusMentis> all)
    {
        var sb = new StringBuilder();
        sb.AppendLine("── persona wording ──");
        if (!WordEmbedding.IsReady)
            sb.AppendLine("  note: no word vectors loaded; only the props phrase is checked");
        var faulty = all.Select(m => (m, faults: Check(m))).Where(x => x.faults.Count > 0).ToList();
        sb.AppendLine($"  {(faulty.Count == 0 ? "✓" : "✗")} {faulty.Count} of {all.Count} modi mentis have a style "
                      + "or reminder written as things (a style is a manner and a lens; a reminder is a disposition)");
        foreach (var (m, faults) in faulty)
            sb.AppendLine($"    ✗ {m.ModusMentisId}: {string.Join("; ", faults)}");
        return sb.ToString();
    }
}
