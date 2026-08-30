using System;
using System.IO;
using System.Reflection;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Loads a terminal shader, from disk during development and from the assembly otherwise.
    ///
    /// <para><b>One copy of each shader, which is the whole point of this class.</b> Both renderers
    /// used to carry their own <c>LoadShaderSource</c> plus a pair of embedded shader strings, so
    /// the same vertex shader existed three times: the file under <c>src/terminal/Shaders/</c>,
    /// <c>TerminalRenderer</c>'s copy and <c>PopupRenderer</c>'s. `src/` is not in the shipped
    /// payload, so a package ran the copies — and <c>TerminalRenderer</c>'s had drifted, missing
    /// <c>uGlyphScale</c> entirely. Every shipped build drew its main terminal text at scale 1.0
    /// while development drew it at 1.2, and the popup beside it disagreed. Nothing failed, because
    /// a uniform a shader does not declare resolves to -1 and setting it is a defined no-op.</para>
    ///
    /// <para>The files are now <c>EmbeddedResource</c>s, so the fallback is the same bytes as the
    /// file rather than a hand-maintained transcription of it. Drift is not fixed here, it is made
    /// impossible: there is nothing left to keep in sync.</para>
    ///
    /// <para><b>Disk still wins when present</b>, which is what keeps a shader editable without a
    /// rebuild. That also means development and a package can still differ — but only while a file
    /// is edited and not rebuilt, which is a state you are in deliberately, rather than one you can
    /// be in for years without knowing.</para>
    /// </summary>
    public static class ShaderSource
    {
        private static readonly string DiskDirectory = Path.Combine("src", "terminal", "Shaders");

        /// <summary>
        /// Reads <paramref name="filename"/> (e.g. <c>terminal.vert</c>). Throws when it is in
        /// neither place, because a terminal with no shader has nothing to degrade to.
        /// </summary>
        public static string Load(string filename)
        {
            string diskPath = Path.Combine(DiskDirectory, filename);
            if (File.Exists(diskPath))
                return File.ReadAllText(diskPath);

            // LogicalName is set in the csproj, so the resource is named for the file rather than
            // for its namespace-mangled path — see the EmbeddedResource item there.
            string resourceName = $"Shaders/{filename}";
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException(
                    $"Shader '{filename}' is neither at {diskPath} nor embedded as '{resourceName}'. "
                  + "Embedded shaders are declared by the EmbeddedResource item in Cathedral.csproj; "
                  + "a new shader file must be covered by it.");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
