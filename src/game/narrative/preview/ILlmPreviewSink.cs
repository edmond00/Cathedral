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
    /// Called once with the final, fully-sanitized text after generation and the post-hoc sanitizer
    /// rewrite complete. This is the authoritative text; it supersedes anything shown incrementally.
    /// </summary>
    void OnComplete(string finalText);
}
