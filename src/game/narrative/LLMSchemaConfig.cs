using System.Collections.Generic;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Centralized configuration for all LLM JSON constraint schemas.
/// Makes it easier to review and edit schema definitions for different LLM response types.
/// </summary>
public static class LLMSchemaConfig
{
    #region Observation Schemas
    
    /// <summary>
    /// Basic observation schema with natural narration.
    /// Used for initial observation attempts without keyword constraints.
    /// </summary>
    public static CompositeField CreateObservationSchema(string fieldName = "what_do_i_feel_and_observe")
    {
        return new CompositeField("ObservationResponse",
            new TemplateStringField(fieldName,
                Template: "I <generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    /// <summary>
    /// Continuation observation schema — no forced 'I ' prefix.
    /// Used for focus sentences after the first sentence in a batch.
    /// </summary>
    public static CompositeField CreateContinuationObservationSchema(string fieldName = "what_do_i_feel_and_observe")
    {
        return new CompositeField("ObservationResponse",
            new TemplateStringField(fieldName,
                Template: "<generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    /// <summary>
    /// Transition observation schema — used when shifting attention from one outcome to another.
    /// Field name mirrors the question: "what catches your attention?"
    /// </summary>
    public static CompositeField CreateTransitionObservationSchema(string fieldName = "what_catches_my_attention")
    {
        return new CompositeField("TransitionObservationResponse",
            new TemplateStringField(fieldName,
                Template: "<generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }
    
    #endregion
    
    #region Thinking Schemas

    /// <summary>
    /// Call 0 (GOAL): thinking modusMentis picks which sub-outcome of an ObservationObject to pursue.
    /// Fires only when the observation has more than one sub-outcome.
    /// </summary>
    /// <param name="validGoals">List of natural-language goal strings from SubOutcomes.ToNaturalLanguageString()</param>
    public static CompositeField CreateGoalSchema(List<string> validGoals)
    {
        return new CompositeField("GoalResponse",
            new ChoiceField<string>("goal", validGoals.ToArray())
        );
    }

    /// <summary>
    /// Call 1 (WHY): thinking modusMentis explains why observing the keyword makes it want the outcome.
    /// </summary>
    public static CompositeField CreateWhySchema(string fieldName = "what_do_i_think")
    {
        return new CompositeField("WhyResponse",
            new TemplateStringField(fieldName,
                Template: "I <generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    /// <summary>
    /// Call 2 (HOW): thinking modusMentis picks which action skill to use and briefly explains.
    /// </summary>
    /// <param name="validMeans">List of "with X" approach strings the LLM can choose from</param>
    public static CompositeField CreateHowSchema(List<string> validMeans, string whyFieldName = "why")
    {
        return new CompositeField("HowResponse",
            new ChoiceField<string>("how", validMeans.ToArray()),
            new TemplateStringField(whyFieldName,
                Template: "I <generated>",
                MinGenLength: 20,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 100)
        );
    }

    /// <summary>
    /// Call 3 (WHAT): action modusMentis describes concretely what it will attempt.
    /// </summary>
    public static CompositeField CreateWhatSchema(string fieldName = "what_should_i_do")
    {
        return new CompositeField("WhatResponse",
            new TemplateStringField(fieldName,
                Template: "try to <generated>.",
                MinGenLength: 10,
                MaxGenLength: 300,
                Hint: "In a few words, what exactly will you try to do?")
        );
    }

    #endregion
    
    #region Speaking Schemas

    /// <summary>
    /// Schema for speaking modusMentis output.
    /// The template enforces that the generated text is wrapped in double-quotes,
    /// producing dialogue-style output addressed to the selected companion.
    /// </summary>
    public static CompositeField CreateSpeakingSchema()
    {
        return new CompositeField("SpeakingResponse",
            new TemplateStringField("what_do_i_say",
                Template: "<generated>",
                MinGenLength: 10,
                MaxGenLength: 400,
                FirstSentenceMaxLength: 400)
        );
    }

    #endregion

    #region Outcome Narration Schemas

    /// <summary>
    /// Schema for outcome narration text.
    /// Simple narration describing the result of an action.
    /// </summary>
    public static CompositeField CreateOutcomeNarrationSchema(string fieldName = "what_happened")
    {
        return new CompositeField("OutcomeNarration",
            new TemplateStringField(fieldName,
                Template: "I <generated>",
                MinGenLength: 30,
                MaxGenLength: 300,
                FirstSentenceMaxLength: 120)
        );
    }

    /// <summary>
    /// Schema for the feeling follow-up after outcome narration.
    /// One short sentence of sensory/emotional flavor from the modusMentis's perspective.
    /// </summary>
    public static CompositeField CreateFeelingSchema(string fieldName = "what_i_feel")
    {
        return new CompositeField("FeelingResponse",
            new TemplateStringField(fieldName,
                Template: "I <generated>",
                MinGenLength: 10,
                MaxGenLength: 150,
                FirstSentenceMaxLength: 150)
        );
    }

    #endregion
    
}
