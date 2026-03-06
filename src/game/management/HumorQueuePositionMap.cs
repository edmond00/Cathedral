using System;
using System.Collections.Generic;
using System.IO;

namespace Cathedral.Game.Management;

/// <summary>
/// Loads one of the humor-queue position overlay files (hepar.txt, paunch.txt,
/// pulmones.txt, spleen.txt) and builds a bidirectional mapping between queue
/// position indices (0-48) and art-grid coordinates.
///
/// File format:
///   Each non-space character that is alphanumeric encodes a queue position index.
///   Character-to-index encoding:
///     '0'-'9'  → indices  0-9
///     'a'-'z'  → indices 10-35
///     'A'-'Z'  → indices 36-61  (files use up to 'M' = 48 for 49 positions)
///   All other characters (dots, pipe/box chars, spaces) are layout guides and ignored.
///
/// Coordinate origin: top-left of the file, x = column, y = row.
/// </summary>
public sealed class HumorQueuePositionMap
{
    private readonly Dictionary<int, (int x, int y)> _indexToPos = new();
    private readonly Dictionary<(int x, int y), int> _posToIndex = new();

    /// <summary>Maximum queue index present in this map (inclusive).</summary>
    public int MaxIndex { get; private set; } = -1;

    /// <summary>Organ id this map was loaded for (set by the caller at load time).</summary>
    public string OrganId { get; private set; }

    private HumorQueuePositionMap(string organId)
    {
        OrganId = organId;
    }

    /// <summary>
    /// Load a position map from the given file path.
    /// <paramref name="organId"/> is stored for identification only (not validated).
    /// </summary>
    public static HumorQueuePositionMap Load(string filePath, string organId)
    {
        var map = new HumorQueuePositionMap(organId);
        if (!File.Exists(filePath)) return map;

        var lines = File.ReadAllLines(filePath);
        for (int y = 0; y < lines.Length; y++)
        {
            string line = lines[y];
            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];
                int idx = CharToIndex(c);
                if (idx < 0) continue;

                map._indexToPos[idx] = (x, y);
                map._posToIndex[(x, y)] = idx;
                if (idx > map.MaxIndex) map.MaxIndex = idx;
            }
        }
        return map;
    }

    /// <summary>
    /// Get the art-grid coordinate for a queue position index.
    /// Returns false when the index has no corresponding position in the file.
    /// </summary>
    public bool TryGetPosition(int index, out int x, out int y)
    {
        if (_indexToPos.TryGetValue(index, out var pos))
        {
            x = pos.x; y = pos.y;
            return true;
        }
        x = y = 0;
        return false;
    }

    /// <summary>
    /// Get the queue position index for the given art-grid coordinate.
    /// Returns -1 when the coordinate is not a queue position cell.
    /// </summary>
    public int GetIndexAt(int x, int y) =>
        _posToIndex.TryGetValue((x, y), out int idx) ? idx : -1;

    // ── Private ───────────────────────────────────────────────────

    private static int CharToIndex(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'z') return c - 'a' + 10;
        if (c >= 'A' && c <= 'Z') return c - 'A' + 36;
        return -1;
    }
}
