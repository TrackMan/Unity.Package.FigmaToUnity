using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Trackman;
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

        #region Fields
        SerializedProperty title;
        SerializedProperty filter;
        SerializedProperty reorder;
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
            (string folder, string relativeFolder) GetFolderAndRelativeFolder(string assetPath)
            {
                string folder;
                string relativeFolder;
                if (assetPath.StartsWith("Packages"))
                {
                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
                    folder = $"{packageInfo.resolvedPath}{Path.GetDirectoryName(assetPath.Replace(packageInfo.assetPath, ""))}";
                    relativeFolder = Path.GetDirectoryName(assetPath);
                }
                else
                {
                    folder = Path.GetDirectoryName(assetPath);
                    relativeFolder = Path.GetRelativePath(Directory.GetCurrentDirectory(), folder);
                }
                return (folder, relativeFolder);
            }

            (string folder, string relativeFolder) = GetFolderAndRelativeFolder(GetAssetPath());
            if (folder.NullOrEmpty() || relativeFolder.NullOrEmpty()) return;

            int progress = Progress.Start($"Figma Update {figma.name}{(downloadImages ? " & Images" : string.Empty)}", default, Progress.Options.Managed);
            using CancellationTokenSource cancellationToken = new();
            try
            {
                Progress.RegisterCancelCallback(progress, () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    cancellationToken.Cancel();
                    return true;
                });

                await UpdateTitleAsync(figma, progress, title, folder, relativeFolder, systemCopyBuffer, downloadImages, fontDirs, cancellationToken.Token);

                Debug.Log($"Figma Update {figma.name} OK");
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
        static async Task UpdateTitleAsync(Figma figma, int progress, string title, string folder, string relativeFolder, bool systemCopyBuffer, bool downloadImages, IReadOnlyCollection<string> fontDirs, CancellationToken token)
        {
            IReadOnlyCollection<Type> elements = figma.GetComponentsInChildren<IRootElement>().Select(x => x.GetType()).ToArray();

            string name = figma.name;
            FigmaUpdater updater = new(PersonalAccessToken, title, folder, relativeFolder, fontDirs);
            await updater.DownloadAssetsAsync(name, downloadImages, elements, figma.Filter, systemCopyBuffer, progress, token);
            updater.ImportTextures();
            updater.WriteUssUxml(name, progress);
            updater.ImportElements(name);
            updater.ImportFinal(name);
            updater.Cleanup(name);
        }
        #endregion
    }
}