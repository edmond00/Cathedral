using System;
using System.IO;
using System.Text;

namespace Cathedral.LLM;

/// <summary>
/// Reads the handful of header fields that identify a GGUF file.
///
/// <para>This exists because the game's model deliberately has no name: it is always
/// <c>model.gguf</c>, and changing models means replacing that file. Without a way to look inside,
/// nothing — no log line, no bug report — could say which model an install is actually running.
/// The identity is in the file: architecture, parameter count and a free-text name are all header
/// metadata, so it is recoverable.</para>
///
/// <para>Only the header is read. The tensor data that makes up the other two gigabytes is never
/// touched, so this costs one small sequential read.</para>
///
/// <para>Every failure returns null. A GGUF that is truncated, from a future spec version, or not a
/// GGUF at all must not stop the game from starting — llama.cpp is the component entitled to reject
/// a model, and it will do so with a better message than this could.</para>
/// </summary>
public static class GgufMetadata
{
    /// <summary>'G','G','U','F' as a little-endian uint32.</summary>
    private const uint Magic = 0x46554747;

    /// <summary>
    /// Sanity ceilings. A malformed or hostile header could otherwise ask this to allocate a string
    /// of 2^64 bytes or loop over billions of key-value pairs before failing.
    /// </summary>
    private const ulong MaxKvCount = 1 << 20;
    private const ulong MaxStringLength = 1 << 20;

    /// <summary>What could be recovered from a GGUF header. Any field may be null.</summary>
    /// <param name="Name"><c>general.name</c> — e.g. "qwen2.5-3b-instruct".</param>
    /// <param name="Architecture"><c>general.architecture</c> — e.g. "qwen2".</param>
    public sealed record Info(string? Name, string? Architecture)
    {
        /// <summary>
        /// The best available label for a UI, or "unknown model" when the header gave up nothing.
        /// </summary>
        public string DisplayName => Name ?? Architecture ?? "unknown model";
    }

    /// <summary>
    /// Reads <paramref name="path"/>'s header. Returns null if it is unreadable or not a GGUF.
    /// </summary>
    public static Info? Read(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            if (reader.ReadUInt32() != Magic) return null;
            reader.ReadUInt32();               // spec version — fields we read are stable across v2/v3
            reader.ReadUInt64();               // tensor count
            ulong kvCount = reader.ReadUInt64();
            if (kvCount > MaxKvCount) return null;

            string? name = null, architecture = null;

            for (ulong i = 0; i < kvCount; i++)
            {
                var key = ReadString(reader);
                if (key == null) break;

                var type = (ValueType)reader.ReadUInt32();

                if (type == ValueType.String && key == "general.name")
                    name = ReadString(reader);
                else if (type == ValueType.String && key == "general.architecture")
                    architecture = ReadString(reader);
                else if (!SkipValue(reader, type))
                    break;   // an unknown type means every following offset is guesswork

                if (name != null && architecture != null) break;
            }

            return name == null && architecture == null ? null : new Info(name, architecture);
        }
        catch
        {
            return null;
        }
    }

    private enum ValueType : uint
    {
        UInt8 = 0, Int8 = 1, UInt16 = 2, Int16 = 3, UInt32 = 4, Int32 = 5,
        Float32 = 6, Bool = 7, String = 8, Array = 9, UInt64 = 10, Int64 = 11, Float64 = 12
    }

    /// <summary>A GGUF string: a 64-bit byte count followed by UTF-8. Null if implausibly long.</summary>
    private static string? ReadString(BinaryReader reader)
    {
        ulong length = reader.ReadUInt64();
        if (length > MaxStringLength) return null;
        var bytes = reader.ReadBytes((int)length);
        return bytes.Length != (int)length ? null : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Advances past a value we do not want. Returns false if the type is unrecognised, which makes
    /// the rest of the header unwalkable — sizes are implicit in the types, so one unknown entry
    /// desynchronises every offset after it.
    /// </summary>
    private static bool SkipValue(BinaryReader reader, ValueType type)
    {
        switch (type)
        {
            case ValueType.UInt8:
            case ValueType.Int8:
            case ValueType.Bool:
                reader.BaseStream.Seek(1, SeekOrigin.Current); return true;
            case ValueType.UInt16:
            case ValueType.Int16:
                reader.BaseStream.Seek(2, SeekOrigin.Current); return true;
            case ValueType.UInt32:
            case ValueType.Int32:
            case ValueType.Float32:
                reader.BaseStream.Seek(4, SeekOrigin.Current); return true;
            case ValueType.UInt64:
            case ValueType.Int64:
            case ValueType.Float64:
                reader.BaseStream.Seek(8, SeekOrigin.Current); return true;

            case ValueType.String:
                return ReadString(reader) != null;

            case ValueType.Array:
            {
                var elementType = (ValueType)reader.ReadUInt32();
                ulong count = reader.ReadUInt64();

                // Strings are variable-width, so the only way past them is one at a time. This is
                // the tokenizer vocabulary — 150k entries on a Qwen model — and is why the loop
                // stops as soon as both wanted keys are found.
                if (elementType == ValueType.String)
                {
                    if (count > MaxKvCount * 8) return false;
                    for (ulong i = 0; i < count; i++)
                        if (ReadString(reader) == null) return false;
                    return true;
                }

                int width = FixedWidthOf(elementType);
                if (width == 0) return false;
                reader.BaseStream.Seek((long)count * width, SeekOrigin.Current);
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>Byte width of a fixed-size value type, or 0 if it has none.</summary>
    private static int FixedWidthOf(ValueType type) => type switch
    {
        ValueType.UInt8 or ValueType.Int8 or ValueType.Bool => 1,
        ValueType.UInt16 or ValueType.Int16 => 2,
        ValueType.UInt32 or ValueType.Int32 or ValueType.Float32 => 4,
        ValueType.UInt64 or ValueType.Int64 or ValueType.Float64 => 8,
        _ => 0
    };
}
