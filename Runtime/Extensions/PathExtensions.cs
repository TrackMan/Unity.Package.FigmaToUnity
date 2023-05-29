using System.IO;

namespace Figma
{
    public static class PathExtensions
    {
        #region Methods
        public static bool IsSeparator(this char ch) => ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar;
        public static bool EqualsTo(this string path, string value, int startIndex = 0)
        {
            if (path.Length - startIndex != value.Length) return false;

            int i, length = value.Length;
            for (i = 0; i < length; ++i)
            {
                if (path[startIndex + i].IsSeparator() && value[i].IsSeparator()) continue;
                if (path[startIndex + i] == value[i]) continue;
                return false;
            }

            return i == length;
        }
        public static bool BeginsWith(this string path, string value, int startIndex = 0)
        {
            if (path.Length - startIndex < value.Length) return false;

            int i, length;
            for (i = 0, length = value.Length; i < length; ++i)
            {
                if (path[startIndex + i].IsSeparator() && value[i].IsSeparator()) continue;
                if (path[startIndex + i] == value[i]) continue;
                return false;
            }

            return (startIndex + i) == path.Length || path[startIndex + i].IsSeparator();
        }
        #endregion
    }
}