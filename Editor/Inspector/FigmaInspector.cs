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
        new Figma target;

        Dictionary<MonoBehaviour, bool> selection;
        bool updating;
        bool resolvingName;
        string username;
        int progressId;

        string searchBar;
        #endregion

        #region Properties
        static string PersonalAccessToken { get => EditorPrefs.GetString(Const.prefsPatTag, string.Empty); set => EditorPrefs.SetString(Const.prefsPatTag, value); }
        #endregion

        #region Methods
        void OnEnable()
        {
            target = (Figma)base.target;

            fileKey = serializedObject.FindProperty(nameof(fileKey));
            filter = serializedObject.FindProperty(nameof(filter));
            reorder = serializedObject.FindProperty(nameof(reorder));
            fontDirectories = serializedObject.FindProperty(nameof(fontDirectories));
            waitFrameBeforeRebuild = serializedObject.FindProperty(nameof(waitFrameBeforeRebuild));

            document = target.GetComponent<UIDocument>();
            selection = target.GetComponentsInChildren<IRootElement>().Cast<MonoBehaviour>().ToDictionary(key => key, _ => true);
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPersonalAccessTokenGUI();
            DrawAssetGUI();
            if (document && document.visualTreeAsset)
                DrawFramesView();
            DrawProperties();
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

            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("You have to enter your personal access token in order to update.\n\n" +
                                                                        "You can get your token at <a href=https://figma.com>https://figma.com</a>",
                                                                        "console.warnicon"), richTextHelpBox))
                Application.OpenURL("https://www.figma.com");
        }
        void DrawAssetGUI()
        {
            // ReSharper disable once AsyncVoidMethod
            async void Update(bool downloadImages, bool pickDirectory)
            {
                try
                {
                    updating = true;

                    string[] fontDirectories = new string[this.fontDirectories.arraySize];

                    for (int index = 0; index < this.fontDirectories.arraySize; index++)
                        fontDirectories[index] = this.fontDirectories.GetArrayElementAtIndex(index).stringValue;

                    Type[] frames = selection.Where(x => x.Value).Select(x => x.Key.GetType()).ToArray();
                    bool prune = selection.All(x => x.Value);

                    document.visualTreeAsset = pickDirectory ? null : document.visualTreeAsset;

                    await UpdateWithProgressAsync(document, target, frames, prune, fileKey.stringValue, downloadImages, fontDirectories, Event.current.modifiers == EventModifiers.Control);
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

            if (string.IsNullOrEmpty(PersonalAccessToken))
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                const string downloadTooltip = "Hold `Ctrl` to copy 'figma.json' into your clipboard";

                if (updating)
                {
                    using (new EditorGUI.DisabledScope(true))
                        GUILayout.Button("Updating...");
                }
                else if (selection.Any(x => x.Value))
                {
                    bool update = GUILayout.Button(new GUIContent("Update", DocumentsOnlyIcon, downloadTooltip), GUILayout.Height(20));
                    bool downloadImages = GUILayout.Button(new GUIContent("Update with Images", DocumentWithImagesIcon, downloadTooltip), GUILayout.Width(184), GUILayout.Height(20));
                    bool resetTargetUxml = GUILayout.Button(new GUIContent(DirectoryIcon), GUILayout.Width(36));

                    if (resetTargetUxml && EditorUtility.DisplayDialog("Figma Updater", "Do you want to update images as well?", "Yes", "No"))
                        downloadImages = true;

                    if (update || downloadImages || resetTargetUxml)
                    {
                        Update(downloadImages, resetTargetUxml);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (selection.Any(x => !x.Value) && selection.Any(x => x.Value))
                EditorGUILayout.HelpBox("Selection mode does clean up unused content. In order to get rid of unused content, \"Select All\" and \"Update\"", MessageType.Warning);

            if (selection.All(x => !x.Value))
                EditorGUILayout.HelpBox("Nothing is selected for Update.", MessageType.Error);
        }
        void DrawFramesView()
        {
            using GUILayout.VerticalScope _ = new(GUI.skin.box);

            searchBar = EditorGUILayout.TextField(searchBar, EditorStyles.toolbarSearchField);

            bool clear = selection.All(x => x.Value);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent($"{(clear ? "Clear" : "Select All")} ({selection.Sum(x => x.Value.ToBit())})"), GUILayout.Width(100)))
                    foreach (MonoBehaviour frame in selection.Keys.ToArray())
                        selection[frame] = !clear;
            }

            foreach (MonoBehaviour frame in selection.Keys.OrderBy(x => x.GetType().GetCustomAttribute<UxmlAttribute>().Root))
            {
                Type elementType = frame.GetType();
                UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();

                if (uxml is null || (!string.IsNullOrWhiteSpace(searchBar) && !uxml.Root.Contains(searchBar)))
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledGroupScope(!selection[frame]))
                        EditorGUILayout.LabelField(new GUIContent(uxml.Root, uxml.Preserve.Any() ? $"Preserves {uxml.Preserve.Aggregate((x, y) => $"{x} {y}")}" : null),
                                                   uxml.Preserve.Any() ? EditorStyles.boldLabel : EditorStyles.label,
                                                   GUILayout.Width(EditorGUIUtility.labelWidth));

                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), frame, typeof(MonoBehaviour), false);

                    selection[frame] = EditorGUILayout.Toggle(selection[frame], GUILayout.Width(24));
                }
            }
        }
        void DrawProperties()
        {
            using EditorGUILayout.VerticalScope scope = new(GUI.skin.box);
            EditorGUILayout.PropertyField(fontDirectories, new GUIContent("Additional Fonts Directories"));
            EditorGUILayout.PropertyField(reorder, new GUIContent("De-root and Re-order Hierarchy"));
            EditorGUILayout.PropertyField(filter, new GUIContent("Filter by Path"));
            EditorGUILayout.PropertyField(waitFrameBeforeRebuild, new GUIContent("Wait Frame Before Rebuild"));
        }
        #endregion

        #region Support Methods
        static async Task UpdateWithProgressAsync(UIDocument document, Figma figma, IReadOnlyList<Type> frames, bool prune, string fileKey, bool downloadImages, IReadOnlyList<string> fontDirectories, bool systemCopyBuffer)
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

                AssetDatabase.StartAssetEditing();
                AssetsInfo info = new(directory, relativeDirectory, uxmlName, fontDirectories);
                FigmaDownloader figmaDownloader = new(PersonalAccessToken, fileKey, info);

                try
                {
                    await figmaDownloader.Run(downloadImages, uxmlName, frames, prune, figma.Filter, systemCopyBuffer, progress, cancellationToken.Token);

                    if (prune)
                        figmaDownloader.CleanUp(downloadImages);
                }
                finally
                {
                    if (prune)
                        figmaDownloader.CleanDirectories();
                }

                document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PathExtensions.CombinePath(info.relativeDirectory, $"{uxmlName}.{KnownFormats.uxml}"));
                stopwatch.Stop();
                EditorUtility.SetDirty(document.visualTreeAsset);

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

                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                AssetDatabase.StopAssetEditing();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(document.visualTreeAsset));
            }
        }
        static (string directory, string relativeDirectory, string product, string name) GetDirectoryAndRelativeDirectory(string assetPath)
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
        #endregion
    }
}