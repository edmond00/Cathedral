using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Central lookup for anatomy factories. Returns the singleton factory for a given anatomy type.
/// </summary>
public static class AnatomyFactoryRegistry
{
    public static IAnatomyFactory GetFactory(AnatomyType anatomyType) =>
        anatomyType switch
        {
            AnatomyType.Human => HumanAnatomyFactory.Instance,
            AnatomyType.Beast => BeastAnatomyFactory.Instance,
            _ => throw new ArgumentOutOfRangeException(nameof(anatomyType), anatomyType, null)
        };
}
