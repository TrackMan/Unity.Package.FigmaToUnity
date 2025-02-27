using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Figma.Inspectors
{
    using Core;
    using Attributes;
    using Internals;
    using static Styles;

    [CustomEditor(typeof(Figma), true)]
    [SuppressMessage("Roslynator", "RCS1213:Remove unused member declaration.")]
    public class FigmaInspector : Editor
    {
        #region Const
        static readonly Regex regex = new(@"[^/\\]+$", RegexOptions.Compiled);
        #endregion

        #region Fields
        SerializedProperty fileKey;
        SerializedProperty filter;
        SerializedProperty reorder;
        SerializedProperty fontDirectories;
        SerializedProperty waitFrameBeforeRebuild;
        UIDocument document;

        bool updating;
        bool resolvingName;
        string username;
        #endregion

        #region Properties
        static string PersonalAccessToken
        {
            get => EditorPrefs.GetString(Const.patTarget, string.Empty);
            set => EditorPrefs.SetString(Const.patTarget, value);
        }
        #endregion

        #region Methods
        void OnEnable()
        {
            fileKey = serializedObject.FindProperty(nameof(fileKey));
            filter = serializedObject.FindProperty(nameof(filter));
            reorder = serializedObject.FindProperty(nameof(reorder));
            fontDirectories = serializedObject.FindProperty(nameof(fontDirectories));
            waitFrameBeforeRebuild = serializedObject.FindProperty(nameof(waitFrameBeforeRebuild));
            document = ((MonoBehaviour)target).GetComponent<UIDocument>();
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPersonalAccessTokenGUI();
            DrawAssetGUI();
            DrawFigmaGUI();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawPersonalAccessTokenGUI()
        {
            // ReSharper disable once AsyncVoidMethod
            async void GetNameAsync(string personalAccessToken)
            {
                resolvingName = true;
                TokenTest test = new(personalAccessToken);
                bool result = await test.TestAsync();

                if (!result)
                    return;

                username = test.me.handle;
                PersonalAccessToken = personalAccessToken;
                resolvingName = false;
            }

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                if (PersonalAccessToken.NotNullOrEmpty())
                {
                    if (username.NullOrEmpty() && !resolvingName)
                        GetNameAsync(PersonalAccessToken);

                    EditorGUILayout.LabelField(EditorGUIUtility.TrIconContent(LoggedInIcon), GUILayout.Width(20));
                    EditorGUILayout.LabelField("You're logged in as", GUILayout.Width(108));
                    EditorGUILayout.LabelField(username, EditorStyles.boldLabel);

                    if (GUILayout.Button(new GUIContent(LogOutIcon, "Log Out"), GUILayout.Width(25), GUILayout.Height(25)))
                        PersonalAccessToken = string.Empty;
                }
                else
                {
                    EditorGUILayout.PrefixLabel("Figma PAT");

                    string token = EditorGUILayout.TextField(PersonalAccessToken);

                    if (GUI.changed)
                        GetNameAsync(token);
                }
            }

            if (PersonalAccessToken.NotNullOrEmpty())
                return;

            GUIStyle richTextHelpBox = new(EditorStyles.helpBox) { richText = true };

            string message = "You have to enter your personal access token in order to update.\n\nYou can get your token at <a href=https://figma.com>https://figma.com</a>";

            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon(message, "console.warnicon"), richTextHelpBox))
                Application.OpenURL("https://www.figma.com");
        }
        void DrawAssetGUI()
        {
            async void Update(bool downloadImages, bool pickDirectory)
            {
                try
                {
                    updating = true;
                    string[] fontDirectories = new string[this.fontDirectories.arraySize];

                    for (int index = 0; index < this.fontDirectories.arraySize; index++)
                        fontDirectories[index] = this.fontDirectories.GetArrayElementAtIndex(index).stringValue;

                    document.visualTreeAsset = pickDirectory ? null : document.visualTreeAsset;

                    await UpdateWithProgressAsync(document, (Figma)target, fileKey.stringValue, downloadImages, fontDirectories, Event.current.modifiers == EventModifiers.Control);
                }
                finally
                {
                    updating = false;
                }
            }

            using EditorGUILayout.VerticalScope __ = new(GUI.skin.box);
            EditorGUILayout.PropertyField(fileKey);

            VisualTreeAsset visualTreeAsset = document.visualTreeAsset;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Asset", visualTreeAsset, typeof(VisualTreeAsset), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                const string downloadTooltip = "Hold `Ctrl` to copy 'figma.json' into your clipboard";

                using EditorGUI.DisabledScope _ = new(!PersonalAccessToken.NotNullOrEmpty() || updating);
                bool updateUI = GUILayout.Button(new GUIContent("Update UI", DocumentsOnlyIcon, downloadTooltip), GUILayout.Height(20));
                bool downloadImages = GUILayout.Button(new GUIContent("Update UI & Images", DocumentWithImagesIcon, downloadTooltip), GUILayout.Width(184), GUILayout.Height(20));
                bool resetTargetUxml = GUILayout.Button(new GUIContent(DirectoryIcon), GUILayout.Width(36));

                if (resetTargetUxml && EditorUtility.DisplayDialog("Figma Updater", "Do you want to update images as well?", "Yes", "No"))
                    downloadImages = true;

                if (!updateUI && !downloadImages && !resetTargetUxml)
                    return;

                Update(downloadImages, resetTargetUxml);
            }
            GUIUtility.ExitGUI();
        }
        void DrawFigmaGUI()
        {
            using EditorGUILayout.VerticalScope scope = new(GUI.skin.box);
            EditorGUILayout.PropertyField(reorder, new GUIContent("De-root and Re-order Hierarchy"));
            EditorGUILayout.PropertyField(filter, new GUIContent("Filter by Path"));
            EditorGUILayout.PropertyField(fontDirectories, new GUIContent("Additional Fonts Directories"));
            EditorGUILayout.PropertyField(waitFrameBeforeRebuild, new GUIContent("Wait Frame Before Rebuild"));

            using EditorGUI.DisabledScope disabledScope = new(true);

            if (!document || !document.visualTreeAsset)
                return;

            foreach (MonoBehaviour element in document.GetComponentsInChildren<IRootElement>().Cast<MonoBehaviour>())
            {
                Type elementType = element.GetType();
                UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();

                if (uxml is null)
                    continue;

                EditorGUILayout.ObjectField(new GUIContent(uxml.Root), element, typeof(MonoBehaviour), true);
                foreach (string root in uxml.Preserve)
                {
                    using EditorGUILayout.HorizontalScope horizontalScope = new();
                    EditorGUILayout.PrefixLabel(root);
                    EditorGUILayout.LabelField($"Preserved by {elementType.Name}");
                }
            }
        }
        #endregion

        #region Support Methods
        static async Task UpdateWithProgressAsync(UIDocument document, Figma figma, string fileKey, bool downloadImages, IReadOnlyList<string> fontDirectories, bool systemCopyBuffer)
        {
            string GetAssetPath()
            {
                if (document.visualTreeAsset)
                    return AssetDatabase.GetAssetPath(document.visualTreeAsset);

                string path = EditorUtility.SaveFilePanel($"Save {nameof(VisualTreeAsset)}", Application.dataPath, document.name, KnownFormats.uxml);

                if (path.NotNullOrEmpty() && Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.dataPath)))
                    return path;

                PackageInfo packageInfo = PackageInfo.GetAllRegisteredPackages().First(x => Path.GetFullPath(path).StartsWith(Path.GetFullPath(x.resolvedPath)));

                return PathExtensions.CombinePath(packageInfo.assetPath, Path.GetFullPath(path).Replace(Path.GetFullPath(packageInfo.resolvedPath), string.Empty));
            }
            (string directory, string relativeDirectory, string product, string name) GetDirectoryAndRelativeDirectory(string assetPath)
            {
                if (!assetPath.StartsWith("Packages"))
                    return (Path.GetDirectoryName(assetPath),
                            Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetDirectoryName(assetPath)),
                            Application.productName,
                            regex.Match(assetPath).Value);

                PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
                return (packageInfo.resolvedPath + Path.GetDirectoryName(assetPath.Replace(packageInfo.assetPath, string.Empty)),
                        Path.GetDirectoryName(assetPath),
                        packageInfo.displayName,
                        regex.Match(assetPath).Value);
            }

            (string directory, string relativeDirectory, string product, string uxmlName) = GetDirectoryAndRelativeDirectory(GetAssetPath());
            uxmlName = Path.GetFileNameWithoutExtension(uxmlName);

            if (directory.NullOrEmpty() || relativeDirectory.NullOrEmpty())
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();

            string display = $"Figma {product}" + (downloadImages ? " (Images)" : string.Empty);
            int progress = Progress.Start(display, null, Progress.Options.Managed);

            using CancellationTokenSource cancellationToken = new();

            try
            {
                Progress.RegisterCancelCallback(progress, () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    cancellationToken.Cancel();
                    return true;
                });

                AssetsInfo info = new(directory, relativeDirectory, uxmlName, fontDirectories);

                await UpdateAsync(document, figma, progress, fileKey, info, uxmlName, systemCopyBuffer, downloadImages, cancellationToken.Token);

                stopwatch.Stop();

                Debug.Log($"{display} is <color={SuccessColor}>updated successfully</color> in {(float)stopwatch.ElapsedMilliseconds / 1000}s");
                Progress.Finish(progress);
            }
            catch (Exception exception)
            {
                Progress.Finish(progress, Progress.Status.Failed);

                if (!exception.Message.Contains("404") || exception is not OperationCanceledException)
                    throw;

                Debug.LogException(exception);
            }
            finally
            {
                Progress.UnregisterCancelCallback(progress);
                stopwatch.Stop();
            }
        }
        static async Task UpdateAsync(UIDocument document, Figma figma, int progress, string fileKey, AssetsInfo info, string uxmlName, bool systemCopyBuffer, bool downloadImages, CancellationToken token)
        {
            IReadOnlyCollection<Type> frames = figma.GetComponentsInChildren<IRootElement>().Select(x => x.GetType()).ToArray();

            FigmaUpdater figmaUpdater = new(PersonalAccessToken, fileKey, info);

            try
            {
                await figmaUpdater.Run(downloadImages, uxmlName, frames, figma.Filter, systemCopyBuffer, progress, token);
                figmaUpdater.CleanUp(downloadImages);
            }
            finally
            {
                figmaUpdater.CleanDirectories();
            }

            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PathExtensions.CombinePath(info.relativeDirectory, $"{uxmlName}.{KnownFormats.uxml}"));
            EditorUtility.SetDirty(document);

            AssetDatabase.Refresh();
        }
        #endregion
    }
}