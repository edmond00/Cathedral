using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A fixed-capacity FIFO-style queue of body humors associated with one humoral organ.
/// Capacity is always 49, matching the spiral art position map.
///
/// Layout:
///   Index 0 = newest (front/entrance — where new humors arrive)
///   Index 48 = oldest (back/exit edge — where humors leave or black bile accumulates)
///
/// Secretion/production: a new humor is inserted at index 0; everything shifts right;
/// the item at the back is removed (unless it is black bile, see below).
///
/// Black Bile stacking rule:
///   Black bile instances are pinned at the back of the queue. When an item must be
///   removed from the back, the first non-black-bile item (scanning from back to front)
///   is removed instead. Black bile items between that position and the back shift
///   inward to fill the gap. Once the entire queue is filled with black bile the queue
///   is "critical" and no further insertions are possible.
/// </summary>
public sealed class HumorQueue
{
    // ── Constants ─────────────────────────────────────────────────
    public const int Capacity = 49;

    // ── State ─────────────────────────────────────────────────────
    private readonly BodyHumor[] _items = new BodyHumor[Capacity];
    private readonly string _organId;

    // ── Constructor ───────────────────────────────────────────────
    public HumorQueue(string organId)
    {
        _organId = organId;
        // Pre-fill with phlegm as a safe default; replaced by FillWithSecretion()
        for (int i = 0; i < Capacity; i++)
            _items[i] = new PhlegmHumor();
    }

    // ── Read-only access ──────────────────────────────────────────

    /// <summary>Organ this queue belongs to.</summary>
    public string OrganId => _organId;

    /// <summary>
    /// Read-only view of the queue, index 0 = newest, index 48 = oldest.
    /// </summary>
    public IReadOnlyList<BodyHumor> Items => _items;

    /// <summary>Number of consecutive black bile instances pinned at the back of the queue.</summary>
    public int BlackBileStackDepth
    {
        get
        {
            int count = 0;
            for (int i = Capacity - 1; i >= 0; i--)
            {
                if (_items[i].IsBlackBile) count++;
                else break;
            }
            return count;
        }
    }

    /// <summary>True when every slot is black bile (future: character death).</summary>
    public bool IsCritical
    {
        get
        {
            for (int i = 0; i < Capacity; i++)
                if (!_items[i].IsBlackBile) return false;
            return true;
        }
    }

    /// <summary>The oldest non-black-bile item, or null when queue is critical.</summary>
    public BodyHumor? PeekConsumable()
    {
        for (int i = Capacity - 1; i >= 0; i--)
            if (!_items[i].IsBlackBile) return _items[i];
        return null;
    }

    /// <summary>Returns true if the queue contains at least one instance of humor type T.</summary>
    public bool HasHumorType<T>() where T : BodyHumor
    {
        for (int i = 0; i < Capacity; i++)
            if (_items[i] is T) return true;
        return false;
    }

    // ── Mutation ──────────────────────────────────────────────────

    /// <summary>
    /// Fill every slot with randomly secreted humors based on organ score.
    /// Used during party-member initialisation. Always produces a full queue.
    /// </summary>
    public void FillWithSecretion(int organScore, Random rng)
    {
        for (int i = 0; i < Capacity; i++)
            _items[i] = CreateSecretedHumor(organScore, rng);
    }

    /// <summary>
    /// Fill every slot with the provided humor instance.
    /// Used during protagonist creation to start with a pure Blood baseline.
    /// </summary>
    public void FillWith(BodyHumor humor)
    {
        for (int i = 0; i < Capacity; i++)
            _items[i] = humor;
    }

    /// <summary>
    /// Fill every slot by secretion based on organ score, but never produce Black Bile.
    /// Black bile's share of the distribution is redistributed proportionally across
    /// Blood, Phlegm and Yellow Bile. Used at protagonist creation.
    /// </summary>
    public void FillWithSecretionSkippingBlackBile(int organScore, Random rng)
    {
        for (int i = 0; i < Capacity; i++)
            _items[i] = CreateSecretedHumorNoBlackBile(organScore, rng);
    }

    /// <summary>
    /// Randomly overwrite roughly <paramref name="percent"/>% of the slots with Juvenescence.
    /// Each slot is independently converted with probability percent/100.
    /// Used once at protagonist creation to seed the vigour of youth.
    /// </summary>
    public void SeedJuvenescence(int percent, Random rng)
    {
        if (percent <= 0) return;
        for (int i = 0; i < Capacity; i++)
            if (rng.Next(100) < percent)
                _items[i] = new JuvenescenceHumor();
    }

    /// <summary>
    /// TEMPORARY: Directly set a slot by index, bypassing queue logic.
    /// Use only for testing humor display. Remove after verification.
    /// </summary>
    public void ForceSet(int index, BodyHumor humor) => _items[index] = humor;

    /// <summary>
    /// Organ secretion: generate a new random humor, insert at front, remove from back.
    /// If the queue is critical (all black bile) nothing happens.
    /// Returns the newly secreted humor, or null when critical.
    /// </summary>
    public BodyHumor? Secrete(int organScore, Random rng)
    {
        var humor = CreateSecretedHumor(organScore, rng);
        return InsertAtFront(humor) ? humor : null;
    }

    /// <summary>
    /// Organ production: inject a specific humor at the front (e.g., Paunch after eating,
    /// Spleen producing Melancholia after a traumatic event).
    /// Insert at front, remove from back.
    /// If the queue is critical nothing happens.
    /// </summary>
    public bool Produce(BodyHumor humor)
    {
        return InsertAtFront(humor);
    }

    /// <summary>
    /// Consume: remove the oldest non-black-bile item from the back and simultaneously
    /// secrete a new humor at the front (the organ refills itself).
    /// Returns the consumed humor, or null when the queue is critical.
    /// </summary>
    public BodyHumor? Consume(int organScore, Random rng)
    {
        int removeIdx = FindRemoveIndex();
        if (removeIdx < 0) return null; // critical

        BodyHumor consumed = _items[removeIdx];
        RemoveAtIndex(removeIdx);

        // Organ secretes a fresh humor at the front
        var newHumor = CreateSecretedHumor(organScore, rng);
        // Shift everything right from front to make room at 0
        Array.Copy(_items, 0, _items, 1, Capacity - 1);
        _items[0] = newHumor;

        return consumed;
    }

    /// <summary>
    /// Spend the tail humor as a dice-roll modifier: remove the oldest non-black-bile item
    /// (the same one <see cref="PeekConsumable"/> exposes) and shift the rest toward the back,
    /// leaving a phlegm placeholder at the front. Unlike <see cref="Consume"/> the organ does
    /// NOT secrete a replacement — the queue simply loses the spent humor and the next
    /// non-black-bile item becomes the new usable tail.
    /// Returns the removed humor, or null when the queue is critical (all black bile).
    /// </summary>
    public BodyHumor? ConsumeTailModifier()
    {
        int removeIdx = FindRemoveIndex();
        if (removeIdx < 0) return null; // critical — nothing usable

        BodyHumor consumed = _items[removeIdx];
        RemoveAtIndex(removeIdx);

        // RemoveAtIndex shifted everything from removeIdx+1 left and parked phlegm at the back.
        // Open a fresh slot at the front (no secretion) so capacity stays full.
        Array.Copy(_items, 0, _items, 1, Capacity - 1);
        _items[0] = new PhlegmHumor();

        return consumed;
    }

    // ── Private helpers ───────────────────────────────────────────

    /// <summary>
    /// Inserts a humor at index 0, shifting existing items toward the back.
    /// The item that would fall off the back is removed using the black-bile-stacking rule.
    /// Returns false if the queue is critical (all black bile).
    /// </summary>
    private bool InsertAtFront(BodyHumor humor)
    {
        int removeIdx = FindRemoveIndex();
        if (removeIdx < 0) return false; // critical — no room

        // Remove the item at removeIdx and close the gap by shifting its left neighbours right
        RemoveAtIndex(removeIdx);

        // Shift items 0..removeIdx-1 rightward to 1..removeIdx, then insert at 0
        // (RemoveAtIndex already shifted everything left; we now need a gap at 0)
        // After RemoveAtIndex the array is: [item0, item1, ..., itemR-1, next items...]
        // where the "hole" was filled. We shift 0..removeIdx-1 right then place at 0.
        //
        // Simpler: after RemoveAtIndex, indices 0..48 are valid. We want to push 0..Capacity-2
        // one step right and write at 0.
        Array.Copy(_items, 0, _items, 1, Capacity - 1);
        _items[0] = humor;
        return true;
    }

    /// <summary>
    /// Find the index of the rightmost non-black-bile item (the candidate for removal).
    /// Returns -1 when all items are black bile (critical state).
    /// </summary>
    private int FindRemoveIndex()
    {
        for (int i = Capacity - 1; i >= 0; i--)
            if (!_items[i].IsBlackBile) return i;
        return -1;
    }

    /// <summary>
    /// Remove the item at <paramref name="index"/> and shift items to the right of it
    /// one position leftward to close the gap (preserving positions of black bile at the back).
    /// </summary>
    private void RemoveAtIndex(int index)
    {
        // Shift items index+1 .. Capacity-1 one step left to fill the hole at index
        for (int i = index; i < Capacity - 1; i++)
            _items[i] = _items[i + 1];
        // The last slot would be unset; fill with phlegm as a safe default
        // (in practice this slot will be overwritten by InsertAtFront immediately)
        _items[Capacity - 1] = new PhlegmHumor();
    }

    /// <summary>
    /// Randomly create a secreted humor instance using organ-score-based probabilities.
    ///
    /// Secretion probabilities come from <see cref="HumoralSecretionTable"/> (organ score 0–3,
    /// beasts up to 4; all four always sum to 100 %).
    ///
    /// High score → mostly Blood / Phlegm; low score → Black Bile dominant.
    /// </summary>
    private static BodyHumor CreateSecretedHumor(int organScore, Random rng)
    {
        int blood     = HumoralSecretionTable.BloodPct(organScore);
        int yellow    = HumoralSecretionTable.YellowBilePct(organScore);
        int blackbile = HumoralSecretionTable.BlackBilePct(organScore);
        int phlegm    = 100 - blood - yellow - blackbile;

        int roll = rng.Next(100);
        if (roll < blood)  return new BloodHumor();
        roll -= blood;
        if (roll < phlegm) return new PhlegmHumor();
        roll -= phlegm;
        if (roll < yellow) return new YellowBileHumor();
        return new BlackBileHumor();
    }

    /// <summary>
    /// Same secretion probabilities as <see cref="CreateSecretedHumor"/> but Black Bile is
    /// excluded: its share is redistributed proportionally across Blood, Phlegm and Yellow Bile
    /// by rolling within the non-black total. Used for protagonist-creation queue fills.
    /// </summary>
    private static BodyHumor CreateSecretedHumorNoBlackBile(int organScore, Random rng)
    {
        int blood  = HumoralSecretionTable.BloodPct(organScore);
        int yellow = HumoralSecretionTable.YellowBilePct(organScore);
        int black  = HumoralSecretionTable.BlackBilePct(organScore);
        int phlegm = 100 - blood - yellow - black;

        int total = blood + phlegm + yellow;
        if (total <= 0) return new PhlegmHumor();

        int roll = rng.Next(total);
        if (roll < blood)  return new BloodHumor();
        roll -= blood;
        if (roll < phlegm) return new PhlegmHumor();
        return new YellowBileHumor();
    }
}
