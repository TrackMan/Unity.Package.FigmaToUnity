using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Figma.Core.Assets
{
    using Internals;
    using static Internals.Const;

    internal class AssetsInfo
    {
        #region Fields
        internal readonly string directory;
        internal readonly string relativeDirectory;
        internal readonly CachedAssets cachedAssets;
        internal readonly ConcurrentBag<string> modifiedContent;

        readonly IReadOnlyList<string> fontDirectories;
        #endregion

        #region Constructors
        internal AssetsInfo(string directory, string relativeDirectory, string remapsFileName, IReadOnlyList<string> fontDirectories)
        {
            this.directory = directory;
            this.relativeDirectory = relativeDirectory;
            this.fontDirectories = fontDirectories;

            modifiedContent = new ConcurrentBag<string>();
            cachedAssets = new CachedAssets(directory, remapsFileName);
        }
        #endregion

        #region Methods
        internal bool GetAssetPath(string name, string extension, out string path)
        {
            switch (extension)
            {
                case KnownFormats.otf or KnownFormats.ttf:
                    path = GetFontPath(name, extension);
                    return path.NotNullOrEmpty();

                case KnownFormats.asset:
                    string fontAssetPath = GetFontPath(name, extension);
                    string fontDirectoryPath = Path.GetDirectoryName(fontAssetPath);
                    string file = $"{name} SDF.{extension}";
                    path = fontDirectoryPath.NotNullOrEmpty() ? file : PathExtensions.CombinePath(fontDirectoryPath, file);
                    return fontAssetPath.NotNullOrEmpty();

                case KnownFormats.png or KnownFormats.svg:
                    string mappedName = cachedAssets[name];
                    path = PathExtensions.CombinePath(imagesDirectoryName, $"{mappedName}.{extension}");
                    return File.Exists(PathExtensions.CombinePath(directory, path));

                default:
                    throw new NotSupportedException(extension);
            }
        }
        internal void AddModifiedFiles(params string[] items) => items.ForEach(item => modifiedContent.Add(item));
        internal string GetAbsolutePath(string path) => PathExtensions.CombinePath(directory, path);
        #endregion

        #region Support Methods
        string GetFontPath(string name, string extension)
        {
            string file = $"{name}.{extension}";
            string localFontsPath = PathExtensions.CombinePath(fontsDirectoryName, file);

            string relativePath = PathExtensions.CombinePath(relativeDirectory, localFontsPath);
            if (File.Exists(FileUtil.GetPhysicalPath(relativePath)))
                return PathExtensions.unixPathSeperator + relativePath;

            foreach (string fontsDirectory in fontDirectories)
            {
                string projectFontPath = PathExtensions.CombinePath(fontsDirectory, file);
                if (File.Exists(FileUtil.GetPhysicalPath(projectFontPath)))
                    return PathExtensions.unixPathSeperator + projectFontPath;
            }

            return null;
        }
        #endregion
    }
}