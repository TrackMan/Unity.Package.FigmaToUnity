using System.Globalization;

namespace Figma
{
    public static class Const
    {
        public const int maximumAllowedDepthLimit = 0x10000; // This is a random big number.
        public const string maximumDepthLimitReachedExceptionMessage = "Maximum depth limit is exceeded.";

        public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");
        public const int initialCollectionCapacity = 128;

        public const string indentCharacters = "    ";
    }
}