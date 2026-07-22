using System.Collections.Generic;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Centralized configuration for the LLM JSON constraint schemas used by the narration pipeline.
/// </summary>
public static class LLMSchemaConfig
{
    #region Persona Rewrite Schemas

    /// <summary>
    /// Generic persona-rewrite schema: a single styled-text field. Used for every neutral→persona
    /// rewrite (reasoning, action, outcome, speaking, dialogue) that does not surface a keyword.
    /// <paramref name="forcedPrefix"/>, when set, becomes a literal prefix the generated value must
    /// start with (e.g. <c>"I "</c> to force a first-person opening) — encoded straight into the GBNF.
    /// </summary>
    public static CompositeField CreateRewriteSchema(string fieldName = "text", string? forcedPrefix = null)
    {
        return new CompositeField("Rewrite",
            new TemplateStringField(fieldName,
                Template: string.IsNullOrEmpty(forcedPrefix) ? "<generated>" : $"{forcedPrefix}<generated>",
                MinGenLength: 15,
                MaxGenLength: Config.Narrative.MaxNarrativeTextLength)
        );
    }

    #endregion

    #region Decision Schemas

    /// <summary>
    /// Constrained single-choice schema (the surviving thinking decisions: goal and skill).
    /// </summary>
    public static CompositeField CreateChoiceSchema(string fieldName, List<string> options)
    {
        return new CompositeField("Choice",
            new ChoiceField<string>(fieldName, options.ToArray())
        );
    }

    #endregion

    #region Sanitizer Schema

    /// <summary>
    /// Single free-text field used by <see cref="Sanitizer.TextSanitizationPipeline"/> when it
    /// rewrites text to scrub forbidden/anachronistic words.
    /// </summary>
    public static CompositeField CreateContinuationObservationSchema(string fieldName = "rewritten_text")
    {
        return new CompositeField("ObservationResponse",
            new TemplateStringField(fieldName,
                Template: "<generated>",
                MinGenLength: 20,
                MaxGenLength: Config.Narrative.MaxNarrativeTextLength)
        );
    }

    #endregion
}
