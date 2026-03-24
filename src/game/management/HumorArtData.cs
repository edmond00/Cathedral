using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;

namespace Cathedral.Game.Management;

/// <summary>
/// Lightweight art-data loader used by the Humors management tab.
/// Only loads the three shared files (ascii_art.txt, layer_map.txt, layer_colors.csv);
/// it does not load organ/part data unlike <see cref="Cathedral.Game.Creation.BodyArtData"/>.
/// </summary>
public sealed class HumorArtData
{
    public int Width  { get; private set; }
    public int Height { get; private set; }

    /// <summary>Visual glyph for each cell.</summary>
    public char[,] ArtGrid { get; private set; } = null!;

    /// <summary>Layer index for each cell (-1 = no layer / background).</summary>
    public int[,] LayerGrid { get; private set; } = null!;

    /// <summary>Color per layer index.</summary>
    public Dictionary<int, Vector4> LayerColors { get; private set; } = null!;

    private HumorArtData() { }

    /// <summary>Load all art data from the given folder.</summary>
    public static HumorArtData Load(string folderPath)
    {
        var data = new HumorArtData();

        // ── ASCII art grid ────────────────────────────────────────
        var asciiLines = File.ReadAllLines(Path.Combine(folderPath, "ascii_art.txt"));
        data.Height = asciiLines.Length;
        data.Width  = asciiLines.Length > 0 ? asciiLines.Max(l => l.Length) : 0;

        data.ArtGrid = new char[data.Width, data.Height];
        for (int y = 0; y < data.Height; y++)
            for (int x = 0; x < data.Width; x++)
                data.ArtGrid[x, y] = x < asciiLines[y].Length ? asciiLines[y][x] : ' ';

        // ── Layer map grid ────────────────────────────────────────
        var layerLines = File.ReadAllLines(Path.Combine(folderPath, "layer_map.txt"));
        data.LayerGrid = new int[data.Width, data.Height];
        for (int y = 0; y < data.Height; y++)
        {
            string line = y < layerLines.Length ? layerLines[y] : "";
            for (int x = 0; x < data.Width; x++)
            {
                int layerIndex = -1;
                if (x < line.Length)
                {
                    char c = line[x];
                    if (c >= '0' && c <= '9') layerIndex = c - '0';
                    else if (c >= 'A' && c <= 'Z') layerIndex = c - 'A' + 10;
                }
                data.LayerGrid[x, y] = layerIndex;
            }
        }

        // ── Layer colors ──────────────────────────────────────────
        data.LayerColors = new Dictionary<int, Vector4>();
        var colorLines = File.ReadAllLines(Path.Combine(folderPath, "layer_colors.csv"));
        foreach (var line in colorLines.Skip(1)) // skip header
        {
            var parts = line.Split(',');
            if (parts.Length < 5) continue;
            if (!int.TryParse(parts[0].Trim(), out int layerIdx)) continue;
            if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r)) continue;
            if (!float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float g)) continue;
            if (!float.TryParse(parts[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float b)) continue;
            float a = 1f;
            if (parts.Length >= 6)
                float.TryParse(parts[5].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out a);
            data.LayerColors[layerIdx] = new Vector4(r, g, b, a);
        }

        return data;
    }

    /// <summary>Returns the layer color for the cell at (x, y), or a fallback dim gray.</summary>
    public Vector4 GetLayerColorAt(int x, int y)
    {
        int idx = LayerGrid[x, y];
        return LayerColors.TryGetValue(idx, out var c) ? c : new Vector4(0.3f, 0.3f, 0.3f, 1f);
    }

    /// <summary>True when the cell has a layer index ≥ 0 and a non-space glyph.</summary>
    public bool IsDrawnCell(int x, int y)
    {
        char c = ArtGrid[x, y];
        return c != ' ' && c != '\0' && LayerGrid[x, y] >= 0;
    }
}
