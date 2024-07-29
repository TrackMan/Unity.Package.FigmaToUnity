using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Trackman;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Figma.Inspectors
{
    using Attributes;

    [CustomEditor(typeof(Figma), true)]
    [SuppressMessage("Roslynator", "RCS1213:Remove unused member declaration.")]
    public class FigmaInspector : Editor
    {
        const string documentsOnlyIcon = "d_Refresh@2x";
        const string documentWithImagesIcon = "d_RawImage Icon";
        const string folderIcon = "d_Project";
        static Regex regex = new(@"[^/\\]+$");

        #region Fields
        SerializedProperty title;
        SerializedProperty filter;
        SerializedProperty reorder;
        SerializedProperty waitFrameBeforeRebuild;
        SerializedProperty fontsDirs;
        UIDocument document;
        #endregion

        #region Properties
        static string PersonalAccessToken
        {
            get => EditorPrefs.GetString("Figma/Editor/PAT", "");
            set => EditorPrefs.SetString("Figma/Editor/PAT", value);
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
            OnPersonalAccessTokenGUI();
            OnAssetGUI();
            OnFigmaGUI();
            serializedObject.ApplyModifiedProperties();
        }

        void OnPersonalAccessTokenGUI()
        {
            async void TryPasteToken(string personalAccessToken)
            {
                FigmaTokenTest test = new(personalAccessToken);
                bool result = await test.TestAsync();
                if (result) PersonalAccessToken = personalAccessToken;
            }

            if (PersonalAccessToken.NotNullOrEmpty())
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Personal Access Token OK");
                GUI.color = Color.white;
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("P4_DeletedLocal@2x", "Clear the Token"), GUILayout.Width(36), GUILayout.Height(20)))
                    PersonalAccessToken = "";
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.yellow;
                EditorGUILayout.PrefixLabel("Personal Access Token");
                GUI.color = Color.white;
                string token = EditorGUILayout.TextField(PersonalAccessToken);
                if (GUI.changed) TryPasteToken(token);
                EditorGUILayout.EndHorizontal();
            }
        }
        void OnAssetGUI()
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

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(title);

            VisualTreeAsset visualTreeAsset = document.visualTreeAsset;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Asset", visualTreeAsset, typeof(VisualTreeAsset), true);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();

            bool updateUI = GUILayout.Button(new GUIContent("Update UI", EditorGUIUtility.IconContent(documentsOnlyIcon).image), GUILayout.Height(20));
            bool downloadImages = GUILayout.Button(new GUIContent("Update UI & Images", EditorGUIUtility.IconContent(documentWithImagesIcon).image), GUILayout.Width(184), GUILayout.Height(20));
            bool forceUpdate = GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture(folderIcon)), GUILayout.Width(36));
            if (forceUpdate && EditorUtility.DisplayDialog("Figma Updater", "Do you want to update images as well?", "Yes", "No")) downloadImages = true;

            if (updateUI || downloadImages || forceUpdate)
            {
                Update(downloadImages, forceUpdate);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        void OnFigmaGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(reorder, new GUIContent("De-root and Re-order Hierarchy"));
            EditorGUILayout.PropertyField(filter, new GUIContent("Filter by Path"));
            EditorGUILayout.PropertyField(fontsDirs, new GUIContent("Additional Fonts Directories"));
            EditorGUILayout.PropertyField(waitFrameBeforeRebuild, new GUIContent("Wait Frame Before Rebuild"));
            EditorGUI.BeginDisabledGroup(true);

            if (document && document.visualTreeAsset)
            {
                foreach (MonoBehaviour element in document.GetComponentsInChildren<IRootElement>().Cast<MonoBehaviour>())
                {
                    Type elementType = element.GetType();
                    UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();
                    if (uxml is not null)
                    {
                        EditorGUILayout.ObjectField(new GUIContent(uxml.Root), element, typeof(MonoBehaviour), true);
                        foreach (string root in uxml.Preserve)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PrefixLabel(root);
                            EditorGUILayout.LabelField($"Preserved by {elementType.Name}");
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
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
                return $"{packageInfo.assetPath}/{Path.GetFullPath(path).Replace(Path.GetFullPath(packageInfo.resolvedPath), "")}";
            }
            (string folder, string relativeFolder, string product, string name) GetFolderAndRelativeFolder(string assetPath)
            {
                string folder;
                string relativeFolder;
                string product;
                if (assetPath.StartsWith("Packages"))
                {
                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
                    folder = $"{packageInfo.resolvedPath}{Path.GetDirectoryName(assetPath.Replace(packageInfo.assetPath, ""))}";
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

            string display = $"Figma {product}{(downloadImages ? " (Images)" : "")}";
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
                cancellationToken.Dispose();
            }
        }
        static async Task UpdateTitleAsync(UIDocument document, Figma figma, int progress, string title, string folder, string relativeFolder, string uxmlName, bool systemCopyBuffer, bool downloadImages, IReadOnlyCollection<string> fontDirs, CancellationToken token)
        {
            void LoadUxml()
            {
                document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(relativeFolder.Contains("Packages") ? $"{relativeFolder}\\{uxmlName}" : Path.Combine(relativeFolder, uxmlName));
                EditorUtility.SetDirty(document);
            }

            IReadOnlyCollection<Type> elements = figma.GetComponentsInChildren<IRootElement>().Select(x => x.GetType()).ToArray();

            string name = figma.name;
            FigmaUpdater updater = new(PersonalAccessToken, title, folder, relativeFolder, fontDirs);
            await updater.DownloadAssetsAsync(name, downloadImages, elements, figma.Filter, systemCopyBuffer, progress, token);
            updater.WriteUssUxml(name, progress);
            updater.Cleanup(name);
            AssetDatabase.Refresh();
            LoadUxml();
        }
        #endregion
    }
}