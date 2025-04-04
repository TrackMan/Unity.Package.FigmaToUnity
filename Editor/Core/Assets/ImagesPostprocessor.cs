using System.IO;
using System.Linq;
using Unity.VectorGraphics.Editor;
using UnityEditor;

#pragma warning disable S1144 // Called from Unity

namespace Figma.Core.Assets
{
    using Internals;
    using static Internals.Const;

    internal class ImagesPostprocessor : AssetPostprocessor
    {
        #region Methods
        void OnPreprocessAsset()
        {
            DirectoryInfo parentDirectory = Directory.GetParent(assetPath)?.Parent;
            
            if (parentDirectory is null || !Directory.GetFiles(parentDirectory!.FullName, "*." + KnownFormats.uxml).Any()) 
                return;

            if (assetPath.Contains(imagesDirectoryName) && assetImporter is SVGImporter svgImporter)
                svgImporter.SvgType = SVGType.UIToolkit;

            if (!assetPath.Contains(imagesDirectoryName) || assetImporter is not TextureImporter textureImporter) 
                return;

            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.mipmapEnabled = false;

            TextureImporterPlatformSettings androidOverrides = textureImporter.GetPlatformTextureSettings("Android");
            androidOverrides.overridden = true;
            androidOverrides.format = TextureImporterFormat.ETC2_RGBA8Crunched;
            androidOverrides.compressionQuality = 90;
            textureImporter.SetPlatformTextureSettings(androidOverrides);
        }
        #endregion
    }
}