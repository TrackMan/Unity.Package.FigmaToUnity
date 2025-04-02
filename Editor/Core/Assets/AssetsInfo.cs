using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
        internal (bool exists, string path) GetAssetPath(string name, string extension)
        {
            switch (extension)
            {
                case KnownFormats.otf or KnownFormats.ttf:
                    string fontPath = GetFontPath(name, extension);
                    return (fontPath.NotNullOrEmpty(), fontPath);

                case KnownFormats.asset:
                    string fontAssetPath = GetFontPath(name, extension);
                    string fontDirectoryPath = Path.GetDirectoryName(fontAssetPath);
                    string target = string.IsNullOrEmpty(fontDirectoryPath) ? $"{name} SDF.{extension}" : PathExtensions.CombinePath(fontDirectoryPath, $"{name} SDF.{extension}");
                    return (fontAssetPath.NotNullOrEmpty(), target);

                case KnownFormats.png or KnownFormats.svg:
                    string mappedName = cachedAssets[name];
                    string filename = PathExtensions.CombinePath(imagesDirectoryName, $"{mappedName}.{extension}");
                    return (File.Exists(PathExtensions.CombinePath(directory, filename)), filename);

                default:
                    throw new NotSupportedException(extension);
            }
        }
        internal (bool valid, int width, int height) GetAssetSize(string name, string extension)
        {
            (bool valid, string path) = GetAssetPath(name, extension);
            switch (extension)
            {
                case KnownFormats.png:
                    if (!valid)
                        return (false, -1, -1);

                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PathExtensions.CombinePath(relativeDirectory, path));
                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                    return (true, width, height);

                case KnownFormats.svg:
                    if (!valid)
                        return (false, -1, -1);

                    SVGImporter svgImporter = (SVGImporter)AssetImporter.GetAtPath(PathExtensions.CombinePath(relativeDirectory, path));
                    Object vectorImage = AssetDatabase.LoadMainAssetAtPath(PathExtensions.CombinePath(relativeDirectory, path));

                    if (!svgImporter || !vectorImage)
                        return (false, -1, -1);

                    if (vectorImage.GetType().GetField("size", BindingFlags.NonPublic | BindingFlags.Instance) is not { } fieldInfo)
                        return (true, svgImporter ? svgImporter.TextureWidth : -1, svgImporter ? svgImporter.TextureHeight : -1);

                    Vector2 size = (Vector2)fieldInfo.GetValue(vectorImage);
                    return (true, Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));

                default:
                    throw new NotSupportedException(extension);
            }
        }
        internal void AddModifiedFiles(params string[] items) => items.ForEach(item => modifiedContent.Add(item));
        #endregion

        #region Support Methods
        string GetFontPath(string name, string extension)
        {
            string localFontsPath = PathExtensions.CombinePath(fontsDirectoryName, $"{name}.{extension}");

            if (File.Exists(FileUtil.GetPhysicalPath(PathExtensions.CombinePath(relativeDirectory, localFontsPath))))
                return "/" + PathExtensions.CombinePath(relativeDirectory, localFontsPath);

            foreach (string fontsDirectory in fontDirectories)
            {
                string projectFontPath = PathExtensions.CombinePath(fontsDirectory, $"{name}.{extension}");
                if (File.Exists(FileUtil.GetPhysicalPath(projectFontPath)))
                    return "/" + projectFontPath;
            }

            return null;
        }
        #endregion
    }
}