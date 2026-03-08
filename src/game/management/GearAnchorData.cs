using System;
using System.Collections.Generic;
using System.IO;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Management;

/// <summary>
/// Loads equipment anchor positions from the gears.txt visual diagram.
/// Scans each line of the diagram for the alias strings listed in gears.csv,
/// then records (row, col) of the first character of the alias.
/// </summary>
public class GearAnchorData
{
    private readonly Dictionary<EquipmentAnchor, (int Row, int Col)> _positions = new();

    // ── Alias lookup: gears.csv alias → EquipmentAnchor ──────────
    private static readonly Dictionary<string, EquipmentAnchor> AliasToAnchor = new(StringComparer.OrdinalIgnoreCase)
    {
        { "headgear",    EquipmentAnchor.Headgear      },
        { "eyewear",     EquipmentAnchor.Eyewear        },
        { "neckwear",    EquipmentAnchor.Neckwear       },
        { "outerwear",   EquipmentAnchor.Outerwear      },
        { "bodywear",    EquipmentAnchor.Bodywear       },
        { "underwear",   EquipmentAnchor.Underwear      },
        { "belt gear",   EquipmentAnchor.BeltGear       },
        { "r. handwear", EquipmentAnchor.RightHandwear  },
        { "l. handwear", EquipmentAnchor.LeftHandwear   },
        { "r. hold",     EquipmentAnchor.RightHold      },
        { "l. hold",     EquipmentAnchor.LeftHold       },
        { "legwear",     EquipmentAnchor.Legwear        },
        { "footwear",    EquipmentAnchor.Footwear       },
    };

    private GearAnchorData() { }

    /// <summary>
    /// Load anchor positions by scanning <c>gears.txt</c> inside <paramref name="artFolder"/>.
    /// </summary>
    /// <param name="artFolder">Path to the folder containing gears.txt (e.g. "assets/art/body/full_body").</param>
    public static GearAnchorData Load(string artFolder)
    {
        var data = new GearAnchorData();
        string gearsPath = Path.Combine(artFolder, "gears.txt");

        if (!File.Exists(gearsPath))
        {
            Console.WriteLine($"GearAnchorData: gears.txt not found at '{gearsPath}'.");
            return data;
        }

        string[] lines = File.ReadAllLines(gearsPath);

        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row];
            foreach (var (alias, anchor) in AliasToAnchor)
            {
                if (data._positions.ContainsKey(anchor)) continue; // already found

                int col = line.IndexOf(alias, StringComparison.OrdinalIgnoreCase);
                if (col >= 0)
                    data._positions[anchor] = (row, col + alias.Length / 2);
            }
        }

        // Report any missing anchors
        foreach (EquipmentAnchor anchor in Enum.GetValues<EquipmentAnchor>())
        {
            if (!data._positions.ContainsKey(anchor))
                Console.WriteLine($"GearAnchorData: No position found for anchor '{anchor}' in gears.txt.");
        }

        return data;
    }

    /// <summary>
    /// Get the (row, col) position of the anchor label within the gears.txt art grid.
    /// Returns false when the anchor was not found.
    /// </summary>
    public bool TryGetPosition(EquipmentAnchor anchor, out int row, out int col)
    {
        if (_positions.TryGetValue(anchor, out var pos))
        {
            row = pos.Row;
            col = pos.Col;
            return true;
        }
        row = col = 0;
        return false;
    }
}
