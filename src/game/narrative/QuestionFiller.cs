namespace Cathedral.Game.Narrative;

/// <summary>
/// Holds 2–4 Question variations for a single (ModusMentis, QuestionReference) pair.
/// GetNext() cycles through them in order, preventing repetition across successive calls.
///
/// Thread safety: not required — each ModusMentis maps to one LLM slot used sequentially.
/// Cursor state intentionally persists for the lifetime of the game session.
/// </summary>
public class QuestionFiller
{
    private readonly Question[] _questions;
    private int _cursor;

    public QuestionReference Reference { get; }

    public QuestionFiller(QuestionReference reference, params Question[] questions)
    {
        if (questions == null || questions.Length == 0)
            throw new ArgumentException("At least one Question variant is required.", nameof(questions));
        Reference = reference;
        _questions = questions;
    }

    /// <summary>
    /// Returns the next Question variant in the rotation and advances the cursor.
    /// </summary>
    public Question GetNext()
        => _questions[_cursor++ % _questions.Length];
}
