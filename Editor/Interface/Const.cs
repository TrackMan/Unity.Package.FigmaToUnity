using System.Globalization;

namespace Figma.Internals
{
    public static class Const
    {
        public const string api = "https://api.figma.com/v1";

        public const string patTarget = "Figma/Editor/PAT";

        public const string uxmlNamespace = "UnityEngine.UIElements";
        public const string indentCharacters = "    ";

        public static readonly CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
        
        public static readonly byte[] invalidPng = {
            137, 80, 78, 71, 13, 10, 26, 10, // PNG signature
            // IHDR chunk
            0, 0, 0, 13, 73, 72, 68, 82, 
            0, 0, 0, 2, 0, 0, 0, 2, 8, 6, 0, 0, 0, 244, 120, 90, 238, 
            // IDAT chunk (compressed image data)
            0, 0, 0, 17, 73, 68, 65, 84, 
            120, 156, 99, 248, 207, 192, 192, 192, 240, 15, 4, 0, 4, 0, 1, 243, 13, 14, 67, 
            // IEND chunk
            0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 
        };

    }

    public static class KnownFormats
    {
        public const string png = nameof(png);
        public const string svg = nameof(svg);
        public const string ttf = nameof(ttf);
        public const string otf = nameof(otf);
        public const string asset = nameof(asset);
    }
}