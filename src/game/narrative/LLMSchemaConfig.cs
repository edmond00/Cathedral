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
    /// </summary>
    public static CompositeField CreateRewriteSchema(string fieldName = "text")
    {
        return new CompositeField("Rewrite",
            new TemplateStringField(fieldName,
                Template: "<generated>",
                MinGenLength: 15,
                MaxGenLength: 400,
                FirstSentenceMaxLength: 200)
        );
    }

    /// <summary>
    /// Observation rewrite schema: the styled sentence plus the single most evocative noun the
    /// persona used, which becomes the clickable keyword mapped back to the source outcome.
    /// </summary>
    public static CompositeField CreateObservationRewriteSchema()
    {
        return new CompositeField("ObservationRewrite",
            new TemplateStringField("text",
                Template: "<generated>",
                MinGenLength: 15,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 200),
            new StringField("keyword", MinLength: 2, MaxLength: 40,
                Hint: "the single most evocative noun appearing in your sentence above")
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
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    #endregion
}
