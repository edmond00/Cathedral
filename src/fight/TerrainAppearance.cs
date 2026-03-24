using OpenTK.Mathematics;

namespace Cathedral.Fight
{
    public record TerrainAppearance(char[] Chars, Vector4 TextColor, Vector4 BgColor)
    {
        public char PickChar(System.Random rng) => Chars[rng.Next(Chars.Length)];
    }
}
