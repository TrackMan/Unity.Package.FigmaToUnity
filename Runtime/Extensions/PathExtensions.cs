using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Figma.Internals
{
    public static class PathExtensions
    {
        #region Const
        public const char unixPathSeperator = '/';
        #endregion

        #region Methods
        internal static string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => Directory.GetFiles(path, searchPattern, searchOption).Select(x => x.Replace('\\', unixPathSeperator)).ToArray();
        internal static string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                throw new ArgumentNullException(nameof(paths));

            StringBuilder path = new();

            for (int i = 0; i < paths.Length; i++)
            {
                if (string.IsNullOrEmpty(paths[i]))
                    continue;

                path.Append(paths[i].Replace('\\', unixPathSeperator));

                if (i < paths.Length - 1 && paths[i][paths[i].Length - 1] != unixPathSeperator && !string.IsNullOrEmpty(paths[i + 1]))
                    path.Append(unixPathSeperator);
            }

            return path.ToString();
        }
        internal static string GetRelativePath(string from, string to) => CombinePath(Path.GetRelativePath(Path.GetDirectoryName(from), Path.GetDirectoryName(to))?.Replace('\\', unixPathSeperator), Path.GetFileName(to));
        internal static string RemoveExtension(string path) => CombinePath(Path.GetDirectoryName(path)?.Replace('\\', unixPathSeperator), Path.GetFileNameWithoutExtension(path));

        internal static bool IsSeparator(this char ch) => ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar;
        internal static bool EqualsTo(this string path, string value, int startIndex = 0)
        {
            if (path.Length - startIndex != value.Length) return false;

            int i, length = value.Length;
            for (i = 0; i < length; ++i)
            {
                if (path[startIndex + i].IsSeparator() && value[i].IsSeparator() || path[startIndex + i] == value[i])
                    continue;

                return false;
            }

            return i == length;
        }
        internal static bool BeginsWith(this string path, string value, int startIndex = 0)
        {
            if (path.Length - startIndex < value.Length)
                return false;

            int i, length;
            for (i = 0, length = value.Length; i < length; ++i)
            {
                if (path[startIndex + i].IsSeparator() && value[i].IsSeparator() || path[startIndex + i] == value[i])
                    continue;

                return false;
            }

            return startIndex + i == path.Length || path[startIndex + i].IsSeparator();
        }
        #endregion
    }
}