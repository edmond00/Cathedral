using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game;

/// <summary>
/// <c>--grant-item &lt;id[,id…]&gt;</c>: puts named items straight into a new protagonist's pack.
///
/// <para><b>What it is for.</b> Five verbs are tool-gated — <c>fish</c> (rod or net), <c>mine</c>
/// (pick), <c>cut_wood</c> (axe), <c>dig</c> (shovel), <c>break</c> (hammer or axe) — and
/// <c>RequiredToolRule</c> refuses them outright without one. The starting kit is random, so whether
/// a script can exercise any of those is a coin flip on the seed, and their <c>success.cli</c> is
/// simply unwritable without this. <c>--weapons</c> exists for the same reason on the fight side and
/// hands out a fixed loadout; this takes a list, because the tool a test needs depends on the verb
/// under test.</para>
///
/// <para>Inert at its default, like every debug flag. An unknown id is reported on stderr and skipped
/// rather than throwing — a typo in a test's flags should name itself, not abort the run before the
/// script has said anything.</para>
/// </summary>
public static class GrantItemMode
{
    /// <summary>Item ids to grant, from the command line. Empty means the flag was not passed.</summary>
    public static IReadOnlyList<string> ItemIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Grants the requested items to <paramref name="protagonist"/>. Call immediately after creating
    /// one, beside <see cref="WeaponsMode.ApplyIfActive"/>.
    /// </summary>
    public static void ApplyIfActive(Protagonist protagonist)
    {
        if (ItemIds.Count == 0) return;

        var granted = new List<string>();
        foreach (var id in ItemIds)
        {
            var item = ItemRegistry.Instance.All.FirstOrDefault(
                i => string.Equals(i.ItemId, id, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                Console.Error.WriteLine($"[debug] --grant-item: no item with id '{id}' — skipped.");
                continue;
            }

            // A fresh instance per grant: the registry hands out catalogue prototypes, and putting
            // the shared one in an inventory would be the same object in every holder that asked.
            var copy = ItemRegistry.NewInstance(item);
            protagonist.AcquireItem(copy);
            granted.Add(copy.DisplayName);
        }

        if (granted.Count == 0) return;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"*** --grant-item: protagonist starts with {string.Join(", ", granted)} ***");
        Console.ResetColor();
    }
}
