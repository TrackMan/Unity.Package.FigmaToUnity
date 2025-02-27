using UnityEditor;
using UnityEngine;

namespace Figma.Inspectors
{
    public static class Styles
    {
        internal static readonly string SuccessColor = EditorGUIUtility.isProSkin ? "#00ff00" : "#00aa00";

        static readonly string prefix = EditorGUIUtility.isProSkin ? "d_" : string.Empty;

        internal static readonly Texture DirectoryIcon = EditorGUIUtility.IconContent($"{prefix}Project").image;
        internal static readonly Texture LoggedInIcon = EditorGUIUtility.IconContent("TestPassed").image;
        internal static readonly Texture LogOutIcon = EditorGUIUtility.IconContent($"{prefix}Import").image;
        internal static readonly Texture DocumentsOnlyIcon = EditorGUIUtility.IconContent($"{prefix}Refresh@2x").image;
        internal static readonly Texture DocumentWithImagesIcon = EditorGUIUtility.IconContent($"{prefix}RawImage Icon").image;
    }
}