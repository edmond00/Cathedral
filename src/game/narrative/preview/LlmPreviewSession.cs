using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative.Sanitizer;

namespace Cathedral.Game.Narrative.Preview;

/// <summary>One rendered block in the preview box: its display text and whether it is the dimmer,
/// parenthesized "free" reasoning (the persona's unconstrained inner thought) vs the normal rewrite.</summary>
public readonly record struct PreviewSegment(string Text, bool IsFree);

/// <summary>
/// Immutable view of the preview box for one rendered frame. The renderer reads only this.
/// </summary>
public readonly record struct PreviewSnapshot(bool Active, string Title, IReadOnlyList<PreviewSegment> Segments, bool Complete)
{
    /// <summary>Flat text (segments joined by newlines) — used by the CLI `preview` report.</summary>
    public string DisplayText => string.Join("\n", Segments.Select(s => s.Text));
    public static readonly PreviewSnapshot Inactive = new(false, "", System.Array.Empty<PreviewSegment>(), false);
}

/// <summary>
/// Drives the live "text being generated" preview box shown over the narration/dialogue menu while an
/// LLM streams a persona rewrite. One session is owned per generation flow (narration, dialogue).
///
/// Producer side (the generation orchestrators, on a background task): call <see cref="BeginPart"/>
/// (or <see cref="BeginDialoguePart"/>) per request, stream tokens into the returned part's sink,
/// then <see cref="PreviewPart.AttachCommit"/> the buffer-append and <see cref="PreviewPart.MarkComplete"/>.
/// Subsequent parts keep generating in the background ("generate ahead") and queue up.
///
/// Consumer side (UI thread): read <see cref="Snapshot"/> each frame; call <see cref="TryContinue"/>
/// when the CONTINUE button is clicked — it commits the current part and advances to the next.
///
/// All shared state is guarded by a single lock so the background producer and the UI reader are safe.
/// </summary>
public sealed class LlmPreviewSession
{
    private readonly object _lock = new();
    private PreviewPart? _current;
    private readonly Queue<PreviewPart> _pending = new();
    private bool _productionEnded;
    private bool _active;
    private string _lastTitle = "";

    /// <summary>True while the box should be shown (generation in flight or awaiting CONTINUE).</summary>
    public bool IsActive { get { lock (_lock) return _active; } }

    /// <summary>Snapshot for the renderer. Never touches producer state directly.</summary>
    public PreviewSnapshot Snapshot()
    {
        lock (_lock)
        {
            if (!_active) return PreviewSnapshot.Inactive;
            if (_current != null)
                return new PreviewSnapshot(true, _current.Title, _current.Segments, _current.IsComplete);
            // Committed the last shown part but the next one hasn't been begun yet → dots, keep last title.
            return new PreviewSnapshot(true, _lastTitle, System.Array.Empty<PreviewSegment>(), false);
        }
    }

    // ── Producer API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Begins a single-request preview part (one streamed segment): thinking / action / outcome / NPC
    /// line. <paramref name="transform"/> post-processes each displayed line (e.g. dialogue's placeholder
    /// → real name substitution) so the preview shows what the committed text will.
    /// </summary>
    public PreviewPart BeginPart(string title, Func<string, string>? transform = null)
    {
        lock (_lock)
        {
            var part = new PreviewPart(title, _lock, accumulate: false, linePrefix: "", transform: transform);
            part.StartSegment();
            _active = true;
            _lastTitle = title;
            if (_current == null) _current = part;
            else _pending.Enqueue(part);
            return part;
        }
    }

    /// <summary>
    /// Begins an accumulating preview part: several streamed segments are appended as separate lines
    /// (each prefixed with <paramref name="linePrefix"/>, e.g. <c>" - "</c> for a dialogue reply list,
    /// or <c>""</c> to just stack them). Start each segment with <see cref="PreviewPart.NextSegment"/>;
    /// the part completes only when the whole batch is done (<see cref="PreviewPart.MarkComplete"/>).
    /// Used for the two-sentence observation, the three-line speaking block, and the dialogue replies.
    /// </summary>
    public PreviewPart BeginAccumulatingPart(string title, string linePrefix = "", Func<string, string>? transform = null)
    {
        lock (_lock)
        {
            var part = new PreviewPart(title, _lock, accumulate: true, linePrefix: linePrefix, transform: transform);
            _active = true;
            _lastTitle = title;
            if (_current == null) _current = part;
            else _pending.Enqueue(part);
            return part;
        }
    }

    /// <summary>Signals that no more parts will be begun. When the queue drains, the session ends.</summary>
    public void EndProduction()
    {
        lock (_lock)
        {
            _productionEnded = true;
            if (_current == null && _pending.Count == 0) _active = false;
        }
    }

    // ── Consumer API (UI thread) ─────────────────────────────────────────────────

    /// <summary>
    /// CONTINUE handler: if the current part is complete, commit it and advance to the next part
    /// (or end the session). Returns true if it advanced, false if there was nothing to continue yet.
    /// </summary>
    public bool TryContinue()
    {
        Action? commit = null;
        lock (_lock)
        {
            if (_current == null || !_current.IsComplete) return false;
            commit = _current.CommitAction;
            if (_pending.Count > 0)
            {
                _current = _pending.Dequeue();
                _lastTitle = _current.Title;
            }
            else
            {
                _current = null;
                if (_productionEnded) _active = false;
            }
        }
        commit?.Invoke(); // run the buffer-append outside the lock
        return true;
    }

    /// <summary>Force-clears the session (e.g. on cancellation / mode exit). Does not run pending commits.</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _current = null;
            _pending.Clear();
            _productionEnded = false;
            _active = false;
            _lastTitle = "";
        }
    }
}

/// <summary>
/// One preview part = one displayed unit gated by a single CONTINUE. A single part holds exactly one
/// streamed segment (replaced on finalize); an accumulating part stacks several streamed segments as
/// separate lines, each prefixed with <c>_linePrefix</c> (e.g. <c>" - "</c> for a dialogue reply list).
/// </summary>
public sealed class PreviewPart
{
    private readonly object _lock;
    private readonly bool _accumulate;
    private readonly string _linePrefix;
    private readonly Func<string, string>? _transform; // display post-process (e.g. placeholder → real name)

    private string _title;
    private Action? _commit;
    private bool _complete;

    // Finalized segments and their kind (free reasoning vs normal rewrite).
    private readonly List<(string text, bool isFree)> _finalizedSegments = new();
    private string _liveTail = "";               // gated, whole-word text of the segment being streamed
    private bool _liveIsFree;                     // kind of the segment currently streaming

    // Active-segment incremental extraction state.
    private readonly StringBuilder _rawJson = new();
    private string _lastSeenCandidate = "";
    private bool _frozen;                          // a forbidden term appeared → stop updating until finalize
    private bool _currentIsFree;                   // kind of the segment being fed right now

    public PreviewPart(string title, object lockObj, bool accumulate, string linePrefix, Func<string, string>? transform = null)
    {
        _title      = title;
        _lock       = lockObj;
        _accumulate = accumulate;
        _linePrefix = linePrefix ?? "";
        _transform  = transform;
    }

    private string Display(string text)
    {
        // The stream carries the scene's false names; the preview always shows the real in-world names.
        // Any explicit per-part transform is composed on top (it is now the same ToReal, so idempotent).
        string real = NameFaking.Real(text);
        return _transform != null ? _transform(real) : real;
    }

    public string Title { get { lock (_lock) return _title; } }
    public bool IsComplete { get { lock (_lock) return _complete; } }
    internal Action? CommitAction { get { lock (_lock) return _commit; } }

    /// <summary>
    /// The blocks to render: each finalized segment (free reasoning wrapped in <c>(…)</c>, normal
    /// segments with the line prefix) plus the segment currently streaming.
    /// </summary>
    public IReadOnlyList<PreviewSegment> Segments
    {
        get
        {
            lock (_lock)
            {
                var list = new List<PreviewSegment>();
                foreach (var (text, isFree) in _finalizedSegments)
                    list.Add(new PreviewSegment(isFree ? $"({text})" : _linePrefix + text, isFree));
                if (_liveTail.Length > 0)
                    list.Add(new PreviewSegment(_liveIsFree ? $"({_liveTail})" : _linePrefix + _liveTail, _liveIsFree));
                return list;
            }
        }
    }

    /// <summary>Attach the closure that appends this part's block to the scroll buffer on CONTINUE.</summary>
    public void AttachCommit(Action commit) { lock (_lock) _commit = commit; }

    /// <summary>Mark this part done so CONTINUE becomes available. Call AFTER AttachCommit.</summary>
    public void MarkComplete() { lock (_lock) _complete = true; }

    // ── Streaming sinks ──────────────────────────────────────────────────────────

    /// <summary>Sink for the single-request case (the one segment was already started by BeginPart).</summary>
    public ILlmPreviewSink Sink => new PreviewSegmentSink(this);

    /// <summary>
    /// Start a new streamed segment on an accumulating part, optionally switching the title (e.g. to the
    /// next reply's modus mentis). <paramref name="isFree"/> marks unconstrained persona reasoning, which
    /// renders dimmer and parenthesized, and streams as raw text (not the JSON <c>"text"</c> field).
    /// </summary>
    public ILlmPreviewSink NextSegment(string? title = null, bool isFree = false)
    {
        lock (_lock) { if (title != null) _title = title; }
        StartSegment(isFree);
        return new PreviewSegmentSink(this);
    }

    /// <summary>Reset per-segment extraction state (called when a new segment starts streaming).</summary>
    internal void StartSegment(bool isFree = false)
    {
        lock (_lock)
        {
            _rawJson.Clear();
            _lastSeenCandidate = "";
            _liveTail = "";
            _frozen = false;
            _currentIsFree = isFree;
            _liveIsFree = isFree;
        }
    }

    /// <summary>Feed one raw token delta: extract the growing text (JSON <c>"text"</c> field, or the raw
    /// stream for free reasoning) and gate it on whole words + the sanitizer.</summary>
    internal void FeedToken(string raw)
    {
        lock (_lock)
        {
            _rawJson.Append(raw);
            if (_frozen) return;

            // Free reasoning is unconstrained raw text; rewrites arrive as a JSON {"text": …} object.
            string extracted = _currentIsFree ? _rawJson.ToString() : TextFieldExtractor.Extract(_rawJson.ToString());
            string candidate = UpToLastWordBoundary(extracted);
            if (candidate.Length == 0 || candidate == _lastSeenCandidate) return;
            _lastSeenCandidate = candidate;

            // Layer 1 (deterministic replacement) then Layers 2/3 detection — no LLM rewrite here.
            // Detection runs on the pre-transform text (as the committed sanitizer does); the transform
            // (e.g. placeholder → real name) is applied only to what is displayed.
            string layered = ForbiddenWordsDictionary.Apply(candidate);
            var (anach, names) = TextSanitizationPipeline.Detect(layered);
            if (anach.Count == 0 && names.Count == 0)
                _liveTail = Display(layered.TrimEnd());
            else
                _frozen = true; // keep the last clean tail; the flagged word never shows
        }
    }

    /// <summary>
    /// Finalize the active segment. A rewrite is finalized with its authoritative post-sanitizer text;
    /// free reasoning (which has no rewrite) is finalized with the Layer-1'd text if clean, else the last
    /// clean gated tail. Empty free segments are dropped.
    /// </summary>
    internal void FinalizeSegment(string finalText)
    {
        lock (_lock)
        {
            if (_currentIsFree)
            {
                string layered = ForbiddenWordsDictionary.Apply(finalText.Trim());
                var (anach, names) = TextSanitizationPipeline.Detect(layered);
                // _liveTail is already transformed; transform the clean full text to match.
                string shown = (anach.Count == 0 && names.Count == 0) ? Display(layered.TrimEnd()) : _liveTail;
                if (shown.Length > 0) _finalizedSegments.Add((shown, true));
            }
            else
            {
                if (!_accumulate) _finalizedSegments.Clear();
                _finalizedSegments.Add((Display(finalText), false));
            }
            _liveTail = "";
            _frozen = false;
            _rawJson.Clear();
            _lastSeenCandidate = "";
        }
    }

    // ── Word-boundary helper ─────────────────────────────────────────────────────

    /// <summary>
    /// Trims a partial string back to its last completed word so an in-flight final word (which could
    /// still turn into a forbidden term once finished) is never previewed. Returns "" if no word is
    /// complete yet.
    /// </summary>
    private static string UpToLastWordBoundary(string s)
    {
        if (s.Length == 0) return s;
        if (IsBoundary(s[^1])) return s;
        for (int k = s.Length - 1; k >= 0; k--)
            if (IsBoundary(s[k])) return s.Substring(0, k + 1);
        return "";
    }

    private static bool IsBoundary(char c) => char.IsWhiteSpace(c) || char.IsPunctuation(c);
}

/// <summary>Builds the box title for a modus mentis — just the name (no level dots).</summary>
public static class PreviewTitles
{
    public static string For(ModusMentis mm) => mm.DisplayName.ToUpper();
}

/// <summary>Adapts a <see cref="PreviewPart"/> segment to the <see cref="ILlmPreviewSink"/> the rewriter calls.</summary>
internal sealed class PreviewSegmentSink : ILlmPreviewSink
{
    private readonly PreviewPart _part;
    public PreviewSegmentSink(PreviewPart part) => _part = part;
    public void OnToken(string rawToken) => _part.FeedToken(rawToken);
    public void OnComplete(string finalText) => _part.FinalizeSegment(finalText);
}

/// <summary>
/// Incrementally extracts the value of the JSON <c>"text"</c> field from a partial, still-streaming
/// document (e.g. <c>{"text": "Hello wor</c>). Honours the common JSON escapes; stops cleanly at the
/// end of the available buffer so it can be called after every token.
/// </summary>
internal static class TextFieldExtractor
{
    public static string Extract(string raw)
    {
        int i = raw.IndexOf("\"text\"", StringComparison.Ordinal);
        if (i < 0) return "";
        i = raw.IndexOf(':', i + 6);
        if (i < 0) return "";
        i++;
        while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
        if (i >= raw.Length || raw[i] != '"') return "";
        i++; // past opening quote

        var sb = new StringBuilder();
        while (i < raw.Length)
        {
            char c = raw[i];
            if (c == '\\')
            {
                if (i + 1 >= raw.Length) break; // incomplete escape at buffer end
                char n = raw[i + 1];
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append(' ');  break; // render tabs as space in the box
                    case 'r': break;
                    case '"': sb.Append('"');  break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/');  break;
                    case 'u':
                        if (i + 5 < raw.Length &&
                            int.TryParse(raw.Substring(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp))
                        {
                            sb.Append((char)cp);
                            i += 6;
                            continue;
                        }
                        return sb.ToString(); // incomplete \uXXXX at buffer end
                    default: sb.Append(n); break;
                }
                i += 2;
            }
            else if (c == '"')
            {
                break; // closing quote → value complete
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }
}
