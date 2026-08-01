namespace Cathedral.Game.Narrative.Preview;

/// <summary>
/// Receives streaming output from a single persona rewrite: the raw token deltas as they arrive,
/// then the final fully-sanitized text once generation completes.
///
/// This is the seam the LLM/rewriter layers depend on — kept deliberately minimal so
/// <see cref="PersonaRewriter"/> and <see cref="Cathedral.LLM.LlamaServerManager"/> know nothing
/// about the UI-facing preview session. <c>PreviewPart</c> (see <c>LlmPreviewSession</c>) implements it.
/// </summary>
public interface ILlmPreviewSink
{
    /// <summary>Called for each raw token delta as it streams from the model.</summary>
    void OnToken(string rawToken);

    /// <summary>
    /// Declares the words this request handed the model (see
    /// <see cref="Cathedral.Game.Narrative.Sanitizer.SourceVocabulary"/>), so the live gate applies the
    /// same exemption the committed sanitizer will. Without it a game-authored name freezes the preview
    /// on a word the final text keeps anyway, and the player watches the box stop mid-sentence.
    /// <para>Optional: sinks that do no detection need not implement it.</para>
    /// </summary>
    void OnSourceVocabulary(System.Collections.Generic.IReadOnlySet<string>? vocabulary) { }

    /// <summary>
    /// Called once with the final, fully-sanitized text after generation and the post-hoc sanitizer
    /// rewrite complete. This is the authoritative text; it supersedes anything shown incrementally.
    /// </summary>
    void OnComplete(string finalText);
}
