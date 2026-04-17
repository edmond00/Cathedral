namespace Cathedral.Game.Narrative;

/// <summary>
/// A single variation of a CoT question.
/// PromptText is the question shown to the LLM in the prompt.
/// JsonFieldName is the JSON key used in the schema, GBNF grammar, and response parsing.
/// Both change together so the LLM echoes the field name vocabulary naturally.
///
/// JsonFieldName must be [a-z_]+ only — it is embedded literally into the GBNF grammar
/// as a JSON key (JsonConstraintGenerator embeds it verbatim) and used as the GetProperty key.
///
/// For QuestionReference.ThinkWhat, PromptText uses {0} as a placeholder for
/// actionModusMentis.ShortDescription. Callers format it with string.Format.
/// </summary>
public record Question(string PromptText, string JsonFieldName);
