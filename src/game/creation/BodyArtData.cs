using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenTK.Mathematics;

namespace Cathedral.Game.Creation;

/// <summary>
/// Info about a single organ part from organs.csv.
/// </summary>
public record OrganPartInfo(char IdChar, string OrganPartName, string OrganName, string BodyPartName);

/// <summary>
/// Bounding box for a body part region in art coordinates.
/// </summary>
public record ArtBounds(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Width => MaxX - MinX + 1;
    public int Height => MaxY - MinY + 1;
    public int CenterX => (MinX + MaxX) / 2;
    public int CenterY => (MinY + MaxY) / 2;
}

/// <summary>
/// Loads and holds all data from a body art folder (ascii_art.txt, layer_map.txt,
/// layer_colors.csv, organs.txt, organs.csv, parts.txt, parts.csv).
/// Provides spatial queries: which organ/body-part is at a given cell, bounding boxes, etc.
/// </summary>
public class BodyArtData
{
    // Grid dimensions
    public int Width { get; private set; }
    public int Height { get; private set; }

    // Parallel grids (all same dimensions)
    public char[,] ArtGrid { get; private set; } = null!;       // Visual glyphs
    public int[,] LayerGrid { get; private set; } = null!;      // Brightness layer index (-1 = none)
    public char[,] OrganGrid { get; private set; } = null!;     // Organ-part id char ('░' = empty)
    public int[,] PartsGrid { get; private set; } = null!;      // Body-part index (-1 = empty)

    // Lookup tables
    public Dictionary<int, Vector4> LayerColors { get; private set; } = null!;
    public Dictionary<char, OrganPartInfo> OrganPartInfos { get; private set; } = null!;
    public Dictionary<int, string> PartIndexToName { get; private set; } = null!;

    // Mapping from 7 part-index names to 5 BodyPart.Id values
    private static readonly Dictionary<string, string> PartNameToBodyPartId = new()
    {
        { "brain", "brain" },
        { "face", "face" },
        { "torso", "torso" },
        { "left_arm", "upper_limbs" },
        { "right_arm", "upper_limbs" },
        { "left_leg", "lower_limbs" },
        { "right_leg", "lower_limbs" }
    };

    // Cached spatial data
    private Dictionary<string, ArtBounds>? _bodyPartBoundsCache;
    private Dictionary<string, ArtBounds>? _rawPartBoundsCache;
    private Dictionary<char, List<(int x, int y)>>? _organPartCellsCache;

    /// <summary>
    /// Load all data from the given folder.
    /// </summary>
    public static BodyArtData Load(string folderPath)
    {
        var data = new BodyArtData();

        string asciiPath = Path.Combine(folderPath, "ascii_art.txt");
        string layerMapPath = Path.Combine(folderPath, "layer_map.txt");
        string layerColorsPath = Path.Combine(folderPath, "layer_colors.csv");
        string organsMapPath = Path.Combine(folderPath, "organs.txt");
        string organsCsvPath = Path.Combine(folderPath, "organs.csv");
        string partsMapPath = Path.Combine(folderPath, "parts.txt");
        string partsCsvPath = Path.Combine(folderPath, "parts.csv");

        // Load ASCII art grid
        var asciiLines = File.ReadAllLines(asciiPath);
        data.Height = asciiLines.Length;
        data.Width = asciiLines.Length > 0 ? asciiLines.Max(l => l.Length) : 0;

        data.ArtGrid = new char[data.Width, data.Height];
        for (int y = 0; y < data.Height; y++)
            for (int x = 0; x < data.Width; x++)
                data.ArtGrid[x, y] = x < asciiLines[y].Length ? asciiLines[y][x] : ' ';

        // Load layer map grid
        var layerLines = File.ReadAllLines(layerMapPath);
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
                    else if (c >= 'A' && c <= 'Z') layerIndex = 10 + (c - 'A');
                }
                data.LayerGrid[x, y] = layerIndex;
            }
        }

        // Load layer colors CSV
        data.LayerColors = new Dictionary<int, Vector4>();
        var colorLines = File.ReadAllLines(layerColorsPath);
        for (int i = 1; i < colorLines.Length; i++)
        {
            var parts = ParseCsvLine(colorLines[i]);
            if (parts.Count >= 6)
            {
                int idx = int.Parse(parts[0], CultureInfo.InvariantCulture);
                float r = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float g = float.Parse(parts[3], CultureInfo.InvariantCulture);
                float b = float.Parse(parts[4], CultureInfo.InvariantCulture);
                float a = float.Parse(parts[5], CultureInfo.InvariantCulture);
                data.LayerColors[idx] = new Vector4(r, g, b, a);
            }
        }

        // Load organs.csv → id char to OrganPartInfo
        data.OrganPartInfos = new Dictionary<char, OrganPartInfo>();
        var organCsvLines = File.ReadAllLines(organsCsvPath);
        for (int i = 1; i < organCsvLines.Length; i++)
        {
            var parts = ParseCsvLine(organCsvLines[i]);
            if (parts.Count >= 4)
            {
                char idChar = parts[0].Length > 0 ? parts[0][0] : '?';
                data.OrganPartInfos[idChar] = new OrganPartInfo(idChar, parts[1], parts[2], parts[3]);
            }
        }

        // Load organs.txt grid
        var organLines = File.ReadAllLines(organsMapPath);
        data.OrganGrid = new char[data.Width, data.Height];
        for (int y = 0; y < data.Height; y++)
        {
            string line = y < organLines.Length ? organLines[y] : "";
            for (int x = 0; x < data.Width; x++)
                data.OrganGrid[x, y] = x < line.Length ? line[x] : '░';
        }

        // Load parts.csv → part index to name
        data.PartIndexToName = new Dictionary<int, string>();
        var partCsvLines = File.ReadAllLines(partsCsvPath);
        foreach (var line in partCsvLines)
        {
            var parts = ParseCsvLine(line);
            if (parts.Count >= 2 && int.TryParse(parts[1], out int idx))
                data.PartIndexToName[idx] = parts[0];
        }

        // Load parts.txt grid
        var partLines = File.ReadAllLines(partsMapPath);
        data.PartsGrid = new int[data.Width, data.Height];
        for (int y = 0; y < data.Height; y++)
        {
            string line = y < partLines.Length ? partLines[y] : "";
            for (int x = 0; x < data.Width; x++)
            {
                int partIndex = -1;
                if (x < line.Length)
                {
                    char c = line[x];
                    if (c >= '0' && c <= '9') partIndex = c - '0';
                }
                data.PartsGrid[x, y] = partIndex;
            }
        }

        Console.WriteLine($"BodyArtData: Loaded {data.Width}x{data.Height} art, {data.LayerColors.Count} layers, {data.OrganPartInfos.Count} organ parts");
        return data;
    }

    /// <summary>
    /// Get the organ part id character at art coordinates, or null if empty/out-of-bounds.
    /// </summary>
    public char? GetOrganPartCharAt(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        char c = OrganGrid[x, y];
        if (c == '░' || c == ' ') return null;
        // Only return if it's a known organ part id
        return OrganPartInfos.ContainsKey(c) ? c : null;
    }

    /// <summary>
    /// Get the OrganPartInfo at art coordinates, or null if not an organ part.
    /// </summary>
    public OrganPartInfo? GetOrganPartInfoAt(int x, int y)
    {
        var c = GetOrganPartCharAt(x, y);
        if (c == null) return null;
        return OrganPartInfos.TryGetValue(c.Value, out var info) ? info : null;
    }

    /// <summary>
    /// Get the body part id at art coordinates (mapped from 7 parts to 5 body parts).
    /// Returns null if empty/out-of-bounds.
    /// </summary>
    public string? GetBodyPartIdAt(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        int partIndex = PartsGrid[x, y];
        if (partIndex < 0) return null;
        if (!PartIndexToName.TryGetValue(partIndex, out var partName)) return null;
        return PartNameToBodyPartId.TryGetValue(partName, out var bodyPartId) ? bodyPartId : null;
    }

    /// <summary>
    /// Get the raw part name (7-value) at art coordinates.
    /// </summary>
    public string? GetPartNameAt(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        int partIndex = PartsGrid[x, y];
        if (partIndex < 0) return null;
        return PartIndexToName.TryGetValue(partIndex, out var name) ? name : null;
    }

    /// <summary>
    /// Get the bounding box of all cells belonging to a body part id (5-value system).
    /// </summary>
    public ArtBounds? GetBodyPartBounds(string bodyPartId)
    {
        EnsureBoundsCache();
        return _bodyPartBoundsCache!.TryGetValue(bodyPartId, out var bounds) ? bounds : null;
    }

    /// <summary>
    /// Get the bounding box of all cells belonging to a raw part name (7-value system:
    /// brain, face, torso, left_arm, right_arm, left_leg, right_leg).
    /// Useful for drawing per-side boxes for limbs.
    /// </summary>
    public ArtBounds? GetRawPartBounds(string rawPartName)
    {
        EnsureRawBoundsCache();
        return _rawPartBoundsCache!.TryGetValue(rawPartName, out var bounds) ? bounds : null;
    }

    /// <summary>
    /// Get all cell positions for a specific organ part id character.
    /// </summary>
    public List<(int x, int y)> GetOrganPartCells(char organPartIdChar)
    {
        EnsureCellsCache();
        return _organPartCellsCache!.TryGetValue(organPartIdChar, out var cells) ? cells : new List<(int, int)>();
    }

    /// <summary>
    /// Get the layer color for a cell at art coordinates.
    /// </summary>
    public Vector4 GetLayerColorAt(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return Config.Colors.Black;
        int layerIndex = LayerGrid[x, y];
        return LayerColors.TryGetValue(layerIndex, out var c) ? c : Config.Colors.Black;
    }

    /// <summary>
    /// Check if a cell is part of the body (non-background).
    /// </summary>
    public bool IsBodyCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return PartsGrid[x, y] >= 0;
    }

    // ── Private helpers ──────────────────────────────────────

    private void EnsureBoundsCache()
    {
        if (_bodyPartBoundsCache != null) return;
        _bodyPartBoundsCache = new Dictionary<string, ArtBounds>();

        // Collect all body part ids with their cell positions
        var positions = new Dictionary<string, (int minX, int minY, int maxX, int maxY)>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var bpId = GetBodyPartIdAt(x, y);
                if (bpId == null) continue;

                if (!positions.ContainsKey(bpId))
                    positions[bpId] = (x, y, x, y);
                else
                {
                    var (minX, minY, maxX, maxY) = positions[bpId];
                    positions[bpId] = (Math.Min(minX, x), Math.Min(minY, y), Math.Max(maxX, x), Math.Max(maxY, y));
                }
            }
        }

        foreach (var (id, (minX, minY, maxX, maxY)) in positions)
            _bodyPartBoundsCache[id] = new ArtBounds(minX, minY, maxX, maxY);
    }

    private void EnsureRawBoundsCache()
    {
        if (_rawPartBoundsCache != null) return;
        _rawPartBoundsCache = new Dictionary<string, ArtBounds>();

        var positions = new Dictionary<string, (int minX, int minY, int maxX, int maxY)>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var rawName = GetPartNameAt(x, y);
                if (rawName == null) continue;

                if (!positions.ContainsKey(rawName))
                    positions[rawName] = (x, y, x, y);
                else
                {
                    var (mX, mY, mxX, mxY) = positions[rawName];
                    positions[rawName] = (Math.Min(mX, x), Math.Min(mY, y), Math.Max(mxX, x), Math.Max(mxY, y));
                }
            }
        }

        foreach (var (name, (minX, minY, maxX, maxY)) in positions)
            _rawPartBoundsCache[name] = new ArtBounds(minX, minY, maxX, maxY);
    }

    private void EnsureCellsCache()
    {
        if (_organPartCellsCache != null) return;
        _organPartCellsCache = new Dictionary<char, List<(int x, int y)>>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var c = GetOrganPartCharAt(x, y);
                if (c == null) continue;

                if (!_organPartCellsCache.ContainsKey(c.Value))
                    _organPartCellsCache[c.Value] = new List<(int, int)>();
                _organPartCellsCache[c.Value].Add((x, y));
            }
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes) { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        fields.Add(current.ToString());
        return fields;
    }
}
