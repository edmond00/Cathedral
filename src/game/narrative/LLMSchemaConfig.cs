using System.Collections.Generic;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Centralized configuration for the LLM JSON constraint schemas used by the narration pipeline.
/// </summary>
public static class LLMSchemaConfig
{
    // Persona rewrites no longer go through a JSON schema: PersonaRewriter emits the styled sentence as
    // raw text via JsonConstraintGenerator.GenerateRawTextGrammar, so a nested quotation can no longer
    // close a JSON string and cut generation off mid-sentence.

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
