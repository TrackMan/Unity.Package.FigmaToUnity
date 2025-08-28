using Unity.VectorGraphics.Editor;
using UnityEditor;

#pragma warning disable S1144 // Called from Unity

namespace Figma.Core.Assets
{
    internal class ImagesPostprocessor : AssetPostprocessor
    {
        #region Methods
        void OnPreprocessAsset()
        {
            if (!assetPath.Contains("UI/Assets/Images")) return;

            if (assetImporter is SVGImporter svgImporter)
                svgImporter.SvgType = SVGType.UIToolkit;

            if (assetImporter is not TextureImporter textureImporter)
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