namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// The global trait pool — quirks any person in the world can be dealt, regardless of trade.
/// Registered from the themed partial files beside this one; this file only names the order.
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    private void RegisterGlobalTraits()
    {
        RegisterBodyTraits();        // how they are built, and what has happened to them
        RegisterTemperTraits();      // what they are like to deal with
        RegisterViceTraits();        // appetites and failings
        RegisterVirtueTraits();      // the good ones
        RegisterHistoryTraits();     // what they did before, and what marked them
    }
}
