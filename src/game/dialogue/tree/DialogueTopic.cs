namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A subject of ordinary conversation an NPC can hold an opinion about. Topics are the spine of the
/// "strengthen relationship" tree — one branch per topic — and the key of the archetype opinion table
/// (<see cref="Cathedral.Game.Npc.NamedNpcArchetype.OpinionOn"/>), which authored replicas reach
/// through the <c>{npc:opinion_&lt;topic&gt;}</c> template token.
///
/// <para>
/// Everything here is deliberately parochial and pre-modern: the weather, the beasts, the harvest,
/// what the neighbours are saying. A villager of this world has no vocabulary for anything wider.
/// </para>
/// </summary>
public enum DialogueTopic
{
    /// <summary>Rain, wind, sun, frost — the day's own sky.</summary>
    Weather,

    /// <summary>The turning year and the work each part of it demands.</summary>
    Seasons,

    /// <summary>The crop: how it stands, how it came in, whether it will last.</summary>
    Harvest,

    /// <summary>Bread, ale, pottage, what is on the table and what is not.</summary>
    Food,

    /// <summary>Livestock and working animals — oxen, sheep, hens, dogs.</summary>
    Beasts,

    /// <summary>The woods, the moor, the fell — everything outside the tilled ground.</summary>
    Wilds,

    /// <summary>Rivers, wells, rain-water, the sea; floods and droughts.</summary>
    Water,

    /// <summary>Household and kin — parents, children, the people under one roof.</summary>
    Kin,

    /// <summary>Aches, agues, remedies, and who has been abed.</summary>
    Health,

    /// <summary>Labour and craft: the doing of one's own trade.</summary>
    Work,

    /// <summary>Idleness, sleep, feast days, the evening after the work is done.</summary>
    Rest,

    /// <summary>Tales, songs, sayings, and the things the old folk repeat.</summary>
    Stories,

    /// <summary>The folk hereabouts — who is quarrelling, who is marrying, who is owed.</summary>
    Neighbours,

    /// <summary>Markets, prices, coin, and the honesty of a bargain.</summary>
    Trade,

    /// <summary>Dreams, portents, luck, and the signs people read into things.</summary>
    Omens,

    /// <summary>Roads, travellers, and whatever lies past the last field one knows.</summary>
    Roads,
}

public static class DialogueTopicExtensions
{
    /// <summary>
    /// The token suffix used in replicas: <c>{npc:opinion_weather}</c>. Kept as the lower-cased enum
    /// name so <see cref="Cathedral.Game.Dialogue.Runtime.DialogueTemplate"/> can parse a token back
    /// into a topic without a second table.
    /// </summary>
    public static string TokenSuffix(this DialogueTopic topic) => topic.ToString().ToLowerInvariant();

    /// <summary>A short noun phrase naming the topic, for intents and prompts ("the weather").</summary>
    public static string Label(this DialogueTopic topic) => topic switch
    {
        DialogueTopic.Weather    => "the weather",
        DialogueTopic.Seasons    => "the turning of the year",
        DialogueTopic.Harvest    => "the harvest",
        DialogueTopic.Food       => "food and drink",
        DialogueTopic.Beasts     => "beasts and livestock",
        DialogueTopic.Wilds      => "the wild country",
        DialogueTopic.Water      => "rivers and rain",
        DialogueTopic.Kin        => "family and household",
        DialogueTopic.Health     => "health and ailments",
        DialogueTopic.Work       => "their work",
        DialogueTopic.Rest       => "rest and feast days",
        DialogueTopic.Stories    => "old tales and songs",
        DialogueTopic.Neighbours => "the folk hereabouts",
        DialogueTopic.Trade      => "prices and bargains",
        DialogueTopic.Omens      => "dreams and omens",
        DialogueTopic.Roads      => "roads and travellers",
        _                        => "things in general",
    };
}
