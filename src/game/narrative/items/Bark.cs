using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>A flake of bark from a tree trunk. Shared by apple tree and pine tree.</summary>
public sealed class Bark : Item
{
    public override string ItemId => "bark";
    public override string DisplayName => "Bark";
    public override string Description => "A rough flake of bark from a tree trunk";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a flake of rough <bark> from the trunk"),
        KeywordInContext.Parse("the scored <surface> of old tree bark"),
    };
}
