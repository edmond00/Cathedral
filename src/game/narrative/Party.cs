using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Holds global, party-wide state that is not attached to any single party member.
/// Currently this is the shared coin wallet used by the buy/sell economy.
///
/// Coins have no weight and occupy no inventory slots — the wallet is just three counters,
/// one per <see cref="CoinType"/>, and can grow without bound. Denominations are kept
/// strictly separate: there is no automatic conversion between gold, silver and copper.
/// </summary>
public class Party
{
    /// <summary>Number of gold coins held by the party.</summary>
    public int Gold { get; private set; }

    /// <summary>Number of silver coins held by the party.</summary>
    public int Silver { get; private set; }

    /// <summary>Number of copper coins held by the party.</summary>
    public int Copper { get; private set; }

    public Party(int gold = 0, int silver = 0, int copper = 0)
    {
        Gold   = Math.Max(0, gold);
        Silver = Math.Max(0, silver);
        Copper = Math.Max(0, copper);
    }

    /// <summary>Returns the current count of the given coin type.</summary>
    public int Get(CoinType type) => type switch
    {
        CoinType.Gold   => Gold,
        CoinType.Silver => Silver,
        CoinType.Copper => Copper,
        _               => 0,
    };

    /// <summary>Adds (or, with a negative amount, removes) coins of one type. Never drops below zero.</summary>
    public void Add(CoinType type, int amount)
    {
        switch (type)
        {
            case CoinType.Gold:   Gold   = Math.Max(0, Gold   + amount); break;
            case CoinType.Silver: Silver = Math.Max(0, Silver + amount); break;
            case CoinType.Copper: Copper = Math.Max(0, Copper + amount); break;
        }
    }

    /// <summary>True when the party holds at least <paramref name="amount"/> coins of the given type.</summary>
    public bool CanAfford(CoinType type, int amount) => Get(type) >= amount;

    /// <summary>
    /// Spends <paramref name="amount"/> coins of one type if the party can afford it.
    /// Returns false (and changes nothing) when there are not enough coins of that type.
    /// </summary>
    public bool TrySpend(CoinType type, int amount)
    {
        if (amount < 0 || !CanAfford(type, amount)) return false;
        Add(type, -amount);
        return true;
    }
}
