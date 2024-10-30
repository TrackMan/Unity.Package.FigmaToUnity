using System;
using System.Collections.Generic;
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
    using Attributes;
    using Internals;

    [CustomEditor(typeof(Figma), true)]
    [SuppressMessage("Roslynator", "RCS1213:Remove unused member declaration.")]
    public class FigmaInspector : Editor
    {
        #region Consts
        static readonly Regex regex = new(@"[^/\\]+$", RegexOptions.Compiled);
        string documentsOnlyIcon => EditorGUIUtility.isProSkin ? "d_Refresh@2x" : "Refresh@2x";
        string documentWithImagesIcon => EditorGUIUtility.isProSkin ? "d_RawImage Icon" : "RawImage Icon";
        string folderIcon => EditorGUIUtility.isProSkin ? "d_Project" : "Project";
        string logOutIcon => EditorGUIUtility.isProSkin ? "d_Import" : "Import";
        #endregion

        #region Fields
        SerializedProperty title;
        SerializedProperty filter;
        SerializedProperty reorder;
        SerializedProperty waitFrameBeforeRebuild;
        SerializedProperty fontsDirs;
        UIDocument document;

        bool occupied;
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
            title = serializedObject.FindProperty(nameof(title));
            filter = serializedObject.FindProperty(nameof(filter));
            reorder = serializedObject.FindProperty(nameof(reorder));
            waitFrameBeforeRebuild = serializedObject.FindProperty(nameof(waitFrameBeforeRebuild));
            fontsDirs = serializedObject.FindProperty(nameof(fontsDirs));
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
            const string loggedInIcon = "TestPassed";

            async void GetName(string personalAccessToken)
            {
                occupied = true;
                FigmaTokenTest test = new(personalAccessToken);
                bool result = await test.TestAsync();

                if (!result)
                    return;

                username = test.me.handle;
                PersonalAccessToken = personalAccessToken;
                occupied = false;
            }

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                if (PersonalAccessToken.NotNullOrEmpty())
                {
                    if (username.NullOrEmpty() && !occupied) 
                        GetName(PersonalAccessToken);

                    EditorGUILayout.LabelField(EditorGUIUtility.IconContent(loggedInIcon), GUILayout.Width(20));
                    EditorGUILayout.LabelField("You're logged in as", GUILayout.Width(108));
                    EditorGUILayout.LabelField(username, EditorStyles.boldLabel);

                    if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(logOutIcon).image, "Log Out"), GUILayout.Width(25), GUILayout.Height(25)))
                        PersonalAccessToken = string.Empty;
                }
                else
                {
                    EditorGUILayout.PrefixLabel("Figma PAT");

                    string token = EditorGUILayout.TextField(PersonalAccessToken);

                    if (GUI.changed)
                        GetName(token);
                }
            }

            if (!PersonalAccessToken.NotNullOrEmpty())
                EditorGUILayout.HelpBox("You have to enter your personal access token in order to update.\n\nYou can get your token at https://figma.com/", MessageType.Warning);
        }
        void DrawAssetGUI()
        {
            async void Update(bool downloadImages, bool forceUpdate)
            {
                IEnumerable<string> GetFontsDirs()
                {
                    foreach (SerializedProperty fontsDir in fontsDirs)
                        yield return fontsDir.stringValue;
                }

                if (forceUpdate) document.visualTreeAsset = null;

                await UpdateTitleWithProgressAsync(document, (Figma)target, title.stringValue, downloadImages, GetFontsDirs().ToArray(), Event.current.modifiers == EventModifiers.Control);
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.PropertyField(title);

                VisualTreeAsset visualTreeAsset = document.visualTreeAsset;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Asset", visualTreeAsset, typeof(VisualTreeAsset), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    const string downloadTooltip = "Hold Ctrl to copy figma.json into your clipboard";

                    using EditorGUI.DisabledScope _ = new(!PersonalAccessToken.NotNullOrEmpty());
                    bool updateUI = GUILayout.Button(new GUIContent("Update UI", EditorGUIUtility.IconContent(documentsOnlyIcon).image, downloadTooltip), GUILayout.Height(20));
                    bool downloadImages = GUILayout.Button(new GUIContent("Update UI & Images", EditorGUIUtility.IconContent(documentWithImagesIcon).image, downloadTooltip), GUILayout.Width(184), GUILayout.Height(20));
                    bool forceUpdate = GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture(folderIcon)), GUILayout.Width(36));

                    if (forceUpdate && EditorUtility.DisplayDialog("Figma Updater", "Do you want to update images as well?", "Yes", "No")) downloadImages = true;

                    if (updateUI || downloadImages || forceUpdate)
                    {
                        Update(downloadImages, forceUpdate);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
        void DrawFigmaGUI()
        {
            using EditorGUILayout.VerticalScope scope = new(GUI.skin.box);
            EditorGUILayout.PropertyField(reorder, new GUIContent("De-root and Re-order Hierarchy"));
            EditorGUILayout.PropertyField(filter, new GUIContent("Filter by Path"));
            EditorGUILayout.PropertyField(fontsDirs, new GUIContent("Additional Fonts Directories"));
            EditorGUILayout.PropertyField(waitFrameBeforeRebuild, new GUIContent("Wait Frame Before Rebuild"));

            using EditorGUI.DisabledScope disabledScope = new(true);
            if (!document || !document.visualTreeAsset) return;

            foreach (MonoBehaviour element in document.GetComponentsInChildren<IRootElement>().Cast<MonoBehaviour>())
            {
                Type elementType = element.GetType();
                UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();
                if (uxml is not null)
                {
                    EditorGUILayout.ObjectField(new GUIContent(uxml.Root), element, typeof(MonoBehaviour), true);
                    foreach (string root in uxml.Preserve)
                    {
                        using EditorGUILayout.HorizontalScope horizontalScope = new();
                        EditorGUILayout.PrefixLabel(root);
                        EditorGUILayout.LabelField($"Preserved by {elementType.Name}");
                    }
                }
            }
        }
        #endregion

        #region Support Methods
        static async Task UpdateTitleWithProgressAsync(UIDocument document, Figma figma, string title, bool downloadImages, IReadOnlyCollection<string> fontDirs, bool systemCopyBuffer)
        {
            string GetAssetPath()
            {
                if (document.visualTreeAsset) return AssetDatabase.GetAssetPath(document.visualTreeAsset);

                string path = EditorUtility.SaveFilePanel($"Save {nameof(VisualTreeAsset)}", Application.dataPath, document.name, "uxml");
                if (path.NotNullOrEmpty() && Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.dataPath))) return path;

                PackageInfo packageInfo = PackageInfo.GetAllRegisteredPackages().First(x => Path.GetFullPath(path).StartsWith(Path.GetFullPath(x.resolvedPath)));
                return Path.Combine(packageInfo.assetPath, $"{Path.GetFullPath(path).Replace(Path.GetFullPath(packageInfo.resolvedPath), string.Empty)}");
            }
            (string folder, string relativeFolder, string product, string name) GetFolderAndRelativeFolder(string assetPath)
            {
                string folder;
                string relativeFolder;
                string product;
                if (assetPath.StartsWith("Packages"))
                {
                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
                    folder = $"{packageInfo.resolvedPath}{Path.GetDirectoryName(assetPath.Replace(packageInfo.assetPath, string.Empty))}";
                    relativeFolder = Path.GetDirectoryName(assetPath);
                    product = packageInfo.displayName;
                }
                else
                {
                    folder = Path.GetDirectoryName(assetPath);
                    relativeFolder = Path.GetRelativePath(Directory.GetCurrentDirectory(), folder);
                    product = Application.productName;
                }

                return (folder, relativeFolder, product, regex.Match(assetPath).Value);
            }

            (string folder, string relativeFolder, string product, string uxmlName) = GetFolderAndRelativeFolder(GetAssetPath());
            if (folder.NullOrEmpty() || relativeFolder.NullOrEmpty()) return;

            string display = $"Figma {product}{(downloadImages ? " (Images)" : string.Empty)}";
            int progress = Progress.Start(display, default, Progress.Options.Managed);
            using CancellationTokenSource cancellationToken = new();
            try
            {
                Progress.RegisterCancelCallback(progress, () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    cancellationToken.Cancel();
                    return true;
                });

                await UpdateTitleAsync(document, figma, progress, title, folder, relativeFolder, uxmlName, systemCopyBuffer, downloadImages, fontDirs, cancellationToken.Token);

                Debug.Log($"{display} OK");
                Progress.Finish(progress);
            }
            catch (Exception exception)
            {
                Progress.Finish(progress, Progress.Status.Failed);

                if (!exception.Message.Contains("404") || exception is not OperationCanceledException) throw;

                Debug.LogException(exception);
            }
            finally
            {
                Progress.UnregisterCancelCallback(progress);
            }
        }
        static async Task UpdateTitleAsync(UIDocument document, Figma figma, int progress, string title, string folder, string relativeFolder, string uxmlName, bool systemCopyBuffer, bool downloadImages, IReadOnlyCollection<string> fontDirs, CancellationToken token)
        {
            IReadOnlyCollection<Type> elements = figma.GetComponentsInChildren<IRootElement>().Select(x => x.GetType()).ToArray();

            string name = figma.name;
            FigmaUpdater updater = new(PersonalAccessToken, title, folder, relativeFolder, fontDirs);

            await updater.DownloadAssetsAsync(name, downloadImages, elements, figma.Filter, systemCopyBuffer, progress, token);
            updater.WriteUssUxml(name, progress);
            updater.Cleanup(name);

            AssetDatabase.Refresh();

            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(relativeFolder, uxmlName));
            EditorUtility.SetDirty(document);
        }
        #endregion
    }
}