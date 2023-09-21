using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;
using Trackman;
using Debug = UnityEngine.Debug;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Figma.Inspectors
{
    using Attributes;
    using global;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    [CustomEditor(typeof(Figma), true)]
    [SuppressMessage("Roslynator", "RCS1213:Remove unused member declaration.")]
    public class FigmaInspector : Editor
    {
        const string uiDocumentsOnlyIcon = "d_Refresh@2x";
        const string uiDocumentWithImagesIcon = "d_RawImage Icon";
        const string folderIcon = "d_Project";
        const int maxConcurrentRequests = 5;
        const string api = "https://api.figma.com/v1";
        static readonly string[] propertiesToCut = { "componentProperties" };

        #region Fields
        SerializedProperty title;
        SerializedProperty filter;
        SerializedProperty reorder;
        SerializedProperty fontsDirs;

        UIDocument document;
        List<PackageInfo> packages = new();
        #endregion

        #region Properties
        static string PAT
        {
            get => EditorPrefs.GetString("Figma/Editor/PAT", "");
            set => EditorPrefs.SetString("Figma/Editor/PAT", value);
        }
        #endregion

        #region Methods
        void OnEnable()
        {
            async void UpdatePackages()
            {
                ListRequest listRequest = Client.List(true, false);
                while (!listRequest.IsCompleted) await new WaitForUpdate();
                packages.Clear();
                packages.AddRange(listRequest.Result);
            }

            title = serializedObject.FindProperty(nameof(title));
            filter = serializedObject.FindProperty(nameof(filter));
            reorder = serializedObject.FindProperty(nameof(reorder));
            fontsDirs = serializedObject.FindProperty(nameof(fontsDirs));

            document = ((MonoBehaviour)target).GetComponent<UIDocument>();
            UpdatePackages();
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnPersonalAccessTokenGUI();
            OnAssetGUI();
            if (serializedObject.context is null) OnFigmaGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void OnPersonalAccessTokenGUI()
        {
            if (PAT.NotNullOrEmpty())
            {
                GUILayout.BeginHorizontal();
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Personal Access Token OK");
                GUI.color = Color.white;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button(new GUIContent("X", "Remove old PAT and enter new PAT"), GUILayout.Width(25), GUILayout.Height(25))) PAT = "";
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            else
            {
                PAT = EditorGUILayout.TextField("Personal Access Token", PAT);
            }
        }
        void OnAssetGUI()
        {
            IEnumerable<string> FontsDirs()
            {
                foreach (SerializedProperty fontsDir in fontsDirs)
                    yield return fontsDir.stringValue;
            }
            async void Update(string assetPath, bool downloadImages)
            {
                string folder;
                string relativeFolder;

                if (assetPath.NullOrEmpty())
                {
                    assetPath = EditorUtility.SaveFilePanel("Save VisualTreeAsset", Application.dataPath, document.name, "uxml");
                    if (!Path.GetFullPath(assetPath).StartsWith(Path.GetFullPath(Application.dataPath)))
                    {
                        string path = assetPath;
                        PackageInfo packageInfo = packages.Find(x => Path.GetFullPath(path).StartsWith($"{Path.GetFullPath(x.resolvedPath)}\\"));
                        assetPath = $"{packageInfo.assetPath}/{Path.GetFullPath(assetPath).Replace(Path.GetFullPath(packageInfo.resolvedPath), "")}";
                    }
                }

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

                if (!folder.NotNullOrEmpty()) return;

                await UpdateTitleAsync(document, (Figma)target, title.stringValue, folder, relativeFolder, Event.current.modifiers == EventModifiers.Control, downloadImages, FontsDirs().ToArray());
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(title);

            VisualTreeAsset visualTreeAsset = document.visualTreeAsset;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Asset", visualTreeAsset, typeof(VisualTreeAsset), true);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            bool forceUpdate = default;
            bool downloadImages = false;

            if (GUILayout.Button(new GUIContent("Update UI", EditorGUIUtility.IconContent(uiDocumentsOnlyIcon).image), GUILayout.Height(20)) ||
                (downloadImages = GUILayout.Button(new GUIContent("Update UI & Images", EditorGUIUtility.IconContent(uiDocumentWithImagesIcon).image), GUILayout.Width(184), GUILayout.Height(20))) ||
                (forceUpdate = GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture(folderIcon)), GUILayout.Width(36))))
                Update(forceUpdate ? default : AssetDatabase.GetAssetPath(visualTreeAsset),
                       forceUpdate ? EditorUtility.DisplayDialog("Figma Updater", "Do you want to update images as well?", "Yes", "No") : downloadImages);

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
        static async Task UpdateDocumentAsync(UIDocument document, Figma figma, string title, bool downloadImages, bool systemCopyBuffer, IReadOnlyCollection<string> fontDirs)
        {
            string folder;
            string relativeFolder;
            string assetPath = AssetDatabase.GetAssetPath(document.visualTreeAsset);

            if (assetPath.NullOrEmpty())
                throw new NotSupportedException();

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

            if (folder.NotNullOrEmpty())
                await UpdateTitleAsync(document, figma, title, folder, relativeFolder, systemCopyBuffer, downloadImages, fontDirs);
        }
        static async Task UpdateTitleAsync(UIDocument document, Figma figma, string title, string folder, string relativeFolder, bool systemCopyBuffer, bool downloadImages, IReadOnlyCollection<string> fontDirs)
        {
            if (!Directory.Exists(Path.Combine(folder, "Images"))) Directory.CreateDirectory(Path.Combine(folder, "Images"));
            if (!Directory.Exists(Path.Combine(folder, "Elements"))) Directory.CreateDirectory(Path.Combine(folder, "Elements"));

            string processName = $"Figma Update {figma.name}{(downloadImages ? " & Images" : string.Empty)}";

            using CancellationTokenSource cancellationToken = new();
            int progress = Progress.Start(processName, default, Progress.Options.Managed);
            Progress.RegisterCancelCallback(progress, () =>
            {
                cancellationToken.Cancel();
                return true;
            });

            try
            {
                await UpdateTitleAsync(document, figma, progress, title, folder, relativeFolder, systemCopyBuffer, downloadImages, fontDirs, cancellationToken.Token);
            }
            catch (Exception exception)
            {
                Progress.Finish(progress, Progress.Status.Failed);

                if (!exception.Message.Contains("404") || (exception is not OperationCanceledException)) throw;
                Debug.LogException(exception);
            }
            finally
            {
                Progress.UnregisterCancelCallback(progress);
                NodeMetadata.Clear(document);
                cancellationToken.Dispose();
            }
        }
        static async Task UpdateTitleAsync(UIDocument document, Figma figma, int progress, string title, string folder, string relativeFolder, bool systemCopyBuffer, bool downloadImages, IReadOnlyCollection<string> fontDirs, CancellationToken token)
        {
            async Task AddMissingComponentsAsync(FigmaParser parser, Dictionary<string, string> headers, DocumentNode documentNode)
            {
                if (parser.MissingComponents.Count > 0)
                {
                    Nodes nodes = JsonUtility.FromJson<Nodes>(await $"{api}/files/{title}/nodes?ids={string.Join(",", parser.MissingComponents.Distinct())}".HttpGetAsync(headers, cancellationToken: token));
                    foreach (Nodes.Document value in nodes.nodes.Values.Where(value => value is not null))
                    {
                        value.document.parent = documentNode;
                        value.document.SetParentRecursively();
                        parser.AddMissingComponent(value.document, value.styles);
                    }
                }
            }
            void InitializeMetadata(DocumentNode documentNode)
            {
                MonoBehaviour[] elements = figma.GetComponentsInChildren<IRootElement>().Cast<MonoBehaviour>().ToArray();
                NodeMetadata.Initialize(document, figma, elements, documentNode);
            }
            async Task<Func<bool>> DownloadImagesAsync(FigmaParser parser, Dictionary<string, string> remaps, Dictionary<string, string> defaultRequestHeaders)
            {
                async Task WriteGradientsAsync(List<string> importGradient, List<string> requiredImages)
                {
                    Progress.SetDescription(progress, "Write Gradients");
                    foreach ((string key, GradientPaint gradient) in parser.Gradients)
                    {
                        CultureInfo defaultCulture = CultureInfo.GetCultureInfo("en-US");
                        XmlWriter writer = XmlWriter.Create(Path.Combine(folder, $"{GetAssetPath(key, "svg").path}"), new XmlWriterSettings
                        {
                            Indent = true,
                            NewLineOnAttributes = true,
                            IndentChars = "    ",
                            Async = true
                        });
                        writer.WriteStartElement("svg");
                        {
                            writer.WriteStartElement("defs");
                            {
                                switch (gradient.type)
                                {
                                    case PaintType.GRADIENT_LINEAR:
                                        writer.WriteStartElement("linearGradient");
                                        writer.WriteAttributeString("id", "gradient");
                                        for (int i = 0; i < Mathf.Max(gradient.gradientHandlePositions.Length, 2); ++i)
                                        {
                                            writer.WriteAttributeString($"x{i + 1}", gradient.gradientHandlePositions[i].x.ToString("F2", defaultCulture));
                                            writer.WriteAttributeString($"y{i + 1}", gradient.gradientHandlePositions[i].y.ToString("F2", defaultCulture));
                                        }

                                        break;

                                    case PaintType.GRADIENT_RADIAL:
                                    case PaintType.GRADIENT_DIAMOND:
                                        writer.WriteStartElement("radialGradient");
                                        writer.WriteAttributeString("id", "gradient");
                                        writer.WriteAttributeString("fx", $"{gradient.gradientHandlePositions[0].x.ToString("F2", defaultCulture)}");
                                        writer.WriteAttributeString("fy", $"{gradient.gradientHandlePositions[0].y.ToString("F2", defaultCulture)}");
                                        writer.WriteAttributeString("cx", $"{gradient.gradientHandlePositions[0].x.ToString("F2", defaultCulture)}");
                                        writer.WriteAttributeString("cy", $"{gradient.gradientHandlePositions[0].y.ToString("F2", defaultCulture)}");

                                        float radius = Vector2.Distance(new Vector2((float)gradient.gradientHandlePositions[1].x, (float)gradient.gradientHandlePositions[1].y), new Vector2((float)gradient.gradientHandlePositions[0].x, (float)gradient.gradientHandlePositions[0].y));
                                        writer.WriteAttributeString("r", $"{radius.ToString("F2", defaultCulture)}");
                                        break;

                                    default:
                                        throw new NotSupportedException();
                                }

                                foreach (ColorStop stop in gradient.gradientStops)
                                {
                                    writer.WriteStartElement("stop");
                                    writer.WriteAttributeString("offset", stop.position.ToString("F2", defaultCulture));
                                    writer.WriteAttributeString("style", $"stop-color:rgb({(byte)(stop.color.r * 255)},{(byte)(stop.color.g * 255)},{(byte)(stop.color.b * 255)});stop-opacity:{stop.color.a.ToString("F2", defaultCulture)}");
                                    await writer.WriteEndElementAsync();
                                }

                                await writer.WriteEndElementAsync();
                            }

                            await writer.WriteEndElementAsync();

                            writer.WriteStartElement("rect");
                            writer.WriteAttributeString("width", "100");
                            writer.WriteAttributeString("height", "100");
                            writer.WriteAttributeString("fill", "url(#gradient)");

                            if (gradient.opacity.HasValue)
                            {
                                writer.WriteAttributeString("fill-opacity", gradient.opacity.Value.ToString("F2", defaultCulture));
                            }

                            await writer.WriteEndElementAsync();
                        }

                        await writer.WriteEndElementAsync();
                        writer.Close();

                        string relativePath = Path.Combine(relativeFolder, $"{GetAssetPath(key, "svg").path}").Replace('\\', '/');

                        importGradient.Add(relativePath);
                        requiredImages.Add(relativePath);
                    }
                }

                List<string> importPng = new();
                List<string> importGradient = new();
                List<string> requiredImages = new();
                List<(string path, int width, int height)> importSvg = new();

                string imagesPath = Path.Combine(folder, "Images");
                Directory.CreateDirectory(imagesPath);
                IEnumerable<string> existingPngs = Directory.GetFiles(imagesPath, "*.png").Select(x => x.Replace('\\', '/'));
                IEnumerable<string> existingSvg = Directory.GetFiles(imagesPath, "*.svg").Select(x => x.Replace('\\', '/'));

                void AddPngImport(string _, string path) => importPng.Add(path);
                Func<KeyValuePair<string, string>, Task> DownloadMethodFor(string extension, Action<string, string> addForImport)
                {
                    HttpClient http = new();
                    foreach (KeyValuePair<string, string> header in defaultRequestHeaders) http.DefaultRequestHeaders.Add(header.Key, header.Value);

                    async Task GetAsync(KeyValuePair<string, string> urlByNodeID)
                    {
                        string nodeID = urlByNodeID.Key;
                        string url = urlByNodeID.Value;
                        (bool fileExists, string _) = GetAssetPath(nodeID, extension);

                        token.ThrowIfCancellationRequested();

                        Progress.SetStepLabel(progress, $"{url}");

                        if (fileExists && remaps.TryGetValue(nodeID, out string etag))
                            http.DefaultRequestHeaders.Add("If-None-Match", $"\"{etag}\"");
                        else
                            http.DefaultRequestHeaders.Remove("If-None-Match");

                        HttpResponseMessage response = await http.GetAsync(url, token);

                        if (response.Headers.TryGetValues("ETag", out IEnumerable<string> values))
                            remaps[nodeID] = values.First().Trim('"');

                        (bool _, string assetPath) = GetAssetPath(nodeID, extension);
                        string relativePath = Path.Combine(relativeFolder, assetPath).Replace('\\', '/');

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            await File.WriteAllBytesAsync(relativePath, await response.Content.ReadAsByteArrayAsync(), token);
                            addForImport(nodeID, relativePath);
                        }

                        requiredImages.Add(relativePath);
                    }
                    return GetAsync;
                }
                bool CleanupAfter()
                {
                    Progress.SetDescription(progress, "Remove dangling images");

                    IEnumerable<string> existingImages = existingPngs.Concat(existingSvg);

                    foreach (string filename in existingImages.Select(Path.GetFileName).Except(requiredImages.Select(Path.GetFileName)))
                    {
                        string fullPath = Path.Combine(imagesPath, filename);

                        Debug.Log($"[FigmaInspector] Removing dangling file {fullPath}");

                        File.Delete(fullPath);
                        File.Delete($"{fullPath}.meta");

                        string value = Path.GetFileNameWithoutExtension(filename);
                        foreach (KeyValuePair<string, string> pair in remaps.Where(v => v.Value == value).ToArray())
                        {
                            remaps.Remove(pair.Key, out string _);
                        }
                    }

                    SaveRemaps(remaps);
                    return true;
                }

                Task fillsSyncTask = Task.CompletedTask;
                Task svgToPngSyncTask = Task.CompletedTask;
                Task svgSyncTask = Task.CompletedTask;

                if (parser.ImageFillNodes.Any(x => x.ShouldDownload(UxmlDownloadImages.ImageFills)))
                {
                    Progress.SetDescription(progress, "Downloading image fills");

                    byte[] bytes = await $"{api}/files/{title}/images".HttpGetAsync(defaultRequestHeaders, cancellationToken: token);
                    Files.Images filesImages = JsonUtility.FromJson<Files.Images>(bytes);

                    IEnumerable<string> imageRefs = parser.ImageFillNodes.Where(x => x.ShouldDownload(UxmlDownloadImages.ImageFills)).Cast<GeometryMixin>().Select(y => y.fills.OfType<ImagePaint>().First().imageRef);
                    IEnumerable<KeyValuePair<string, string>> images = filesImages.meta.images.Where(item => imageRefs.Contains(item.Key));

                    fillsSyncTask = images.ForEachParallelAsync(maxConcurrentRequests, DownloadMethodFor("png", AddPngImport), token);
                }
                if (parser.PngNodes.Any(x => x.ShouldDownload(UxmlDownloadImages.RenderAsPng)))
                {
                    Progress.SetDescription(progress, "Downloading png images");
                    int i = 0;
                    IEnumerable<IGrouping<int, string>> items = parser.PngNodes.Where(x => x.ShouldDownload(UxmlDownloadImages.RenderAsPng)).Select(y => y.id).GroupBy(_ => i++ / 100);
                    Task<byte[]>[] tasks = items.Select((group) => $"{api}/images/{title}?ids={string.Join(",", group)}&format=png".HttpGetAsync(defaultRequestHeaders, cancellationToken: token)).ToArray();
                    await Task.WhenAll(tasks);
                    IEnumerable<KeyValuePair<string, string>> images = tasks.SelectMany(t => JsonUtility.FromJson<Images>(t.Result).images);
                    svgToPngSyncTask = images.ForEachParallelAsync(maxConcurrentRequests, DownloadMethodFor("png", AddPngImport), token);
                }
                if (parser.SvgNodes.Any(x => x.ShouldDownload(UxmlDownloadImages.RenderAsSvg)))
                {
                    void AddSvgImport(string id, string path)
                    {
                        BaseNode node = parser.SvgNodes.Find(x => x.id == id);
                        if (node is LayoutMixin layout) importSvg.Add((path, (int)layout.absoluteBoundingBox.width, (int)layout.absoluteBoundingBox.height));
                    }

                    Progress.SetDescription(progress, "Downloading svg images");
                    int i = 0;

                    IEnumerable<IGrouping<int, BaseNode>> nodesGroups = parser.SvgNodes.Where(x => x.ShouldDownload(UxmlDownloadImages.RenderAsSvg)).GroupBy(_ => i++ / 100);
                    Task<byte[]>[] tasks = nodesGroups.Select((nodes) => $"{api}/images/{title}?ids={string.Join(",", nodes.Select(x => x.id))}&format=svg".HttpGetAsync(defaultRequestHeaders, cancellationToken: token)).ToArray();
                    await Task.WhenAll(tasks);
                    IEnumerable<KeyValuePair<string, string>> images = tasks.SelectMany(t => JsonUtility.FromJson<Images>(t.Result).images);
                    svgSyncTask = images.ForEachParallelAsync(maxConcurrentRequests, DownloadMethodFor("svg", AddSvgImport), token);
                }

                try
                {
                    AssetDatabase.StartAssetEditing();
                    await Task.WhenAll(fillsSyncTask, svgToPngSyncTask, svgSyncTask);

                    await WriteGradientsAsync(importGradient, requiredImages);
                }
                finally
                {
                    SaveRemaps(remaps);
                    Progress.SetStepLabel(progress, "");
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.ImportAsset(Path.Combine(relativeFolder, "Images"), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);

                Progress.SetDescription(progress, "Importing png...");
                foreach (TextureImporter importer in importPng.Select(relativePath => (TextureImporter)AssetImporter.GetAtPath(relativePath)))
                {
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.mipmapEnabled = false;
                    EditorUtility.SetDirty(importer);
                }

                Progress.SetDescription(progress, "Importing svg...");
                foreach (SVGImporter importer in importSvg.Select(value => (SVGImporter)AssetImporter.GetAtPath(value.path)))
                {
#if VECTOR_GRAPHICS_RASTER
                    importer.SvgType = SVGType.Texture2D;
                    importer.KeepTextureAspectRatio = false;
                    importer.TextureWidth = Mathf.CeilToInt(value.width);
                    importer.TextureHeight = Mathf.CeilToInt(value.height);
                    importer.SampleCount = 8;
#else
                    importer.SvgType = SVGType.UIToolkit;
#endif
                    EditorUtility.SetDirty(importer);
                }

                Progress.SetDescription(progress, "Importing gradients...");
                foreach (SVGImporter importer in importGradient.Select(path => (SVGImporter)AssetImporter.GetAtPath(path)))
                {
                    importer.SvgType = SVGType.UIToolkit;
                    EditorUtility.SetDirty(importer);
                }

                AssetDatabase.ImportAsset(Path.Combine(relativeFolder, "Images"), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
                return CleanupAfter;
            }

            #region GetFontPath, GetAssetPath, GetAssetSize
            string remapsFilename = $"{folder}/remaps_{figma.name}.json";
            // ReSharper disable once RedundantAssignment
            Dictionary<string, string> remaps = File.Exists(remapsFilename) ? JsonUtility.FromJson<Dictionary<string, string>>(await File.ReadAllTextAsync(remapsFilename, token)) : new();

            void SaveRemaps(Dictionary<string, string> dictionary) => File.WriteAllText(remapsFilename, JsonUtility.ToJson(dictionary, prettyPrint: true));
            string GetFontPath(string name, string extension)
            {
                string localFontsPath = $"Fonts/{name}.{extension}";
                if (File.Exists(FileUtil.GetPhysicalPath($"{relativeFolder}/{localFontsPath}")))
                    return localFontsPath;

                foreach (string fontsDir in fontDirs)
                {
                    string projectFontPath = $"{fontsDir}/{name}.{extension}";
                    if (File.Exists(FileUtil.GetPhysicalPath(projectFontPath)))
                        return $"/{projectFontPath}";
                }
                return default;
            }
            (bool valid, string path) GetAssetPath(string name, string extension)
            {
                switch (extension)
                {
                    case "otf":
                    case "ttf":
                        string fontPath = GetFontPath(name, extension);
                        return (fontPath.NotNullOrEmpty(), $"{fontPath}");
                    case "asset":
                        string fontAssetPath = GetFontPath(name, extension);
                        return (fontAssetPath.NotNullOrEmpty(), $"{Path.GetDirectoryName(fontAssetPath)}/{name} SDF.{extension}");

                    case "png":
                    case "svg":
                        remaps.TryGetValue(name, out string mappedName);
                        string filename = $"Images/{mappedName ?? name}.{extension}";
                        return (File.Exists(Path.Combine(folder, filename)), filename);

                    default:
                        throw new NotSupportedException();
                }
            }
            (bool valid, int width, int height) GetAssetSize(string name, string extension)
            {
                (bool valid, string path) = GetAssetPath(name, extension);
                switch (extension)
                {
                    case "png":
                        if (valid)
                        {
                            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Path.Combine(relativeFolder, path));
                            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                            return (true, width, height);
                        }
                        return (false, -1, -1);

                    case "svg":
                        if (valid)
                        {
                            SVGImporter importer = (SVGImporter)AssetImporter.GetAtPath(Path.Combine(relativeFolder, path));
                            UnityEngine.Object vectorImage = AssetDatabase.LoadMainAssetAtPath(Path.Combine(relativeFolder, path));

                            if (vectorImage.GetType().GetField("size", BindingFlags.NonPublic | BindingFlags.Instance) is { } fieldInfo)
                            {
                                Vector2 size = (Vector2)fieldInfo.GetValue(vectorImage);
                                return (true, Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));
                            }

                            return (true, importer.TextureWidth == -1 ? -1 : importer.TextureWidth, importer.TextureHeight == -1 ? -1 : importer.TextureHeight);
                        }
                        return (false, -1, -1);

                    default:
                        throw new NotSupportedException();
                }
            }
            #endregion

            Progress.Report(progress, 1, 5, "Downloading nodes");
            Dictionary<string, string> headers = new() { { "X-FIGMA-TOKEN", PAT } };
            string json = Encoding.UTF8.GetString(await $"{api}/files/{title}".HttpGetAsync(headers, cancellationToken: token));
            if (systemCopyBuffer) GUIUtility.systemCopyBuffer = json;

            Progress.Report(progress, 2, 5, "Parsing JSON");
            Files files = await Task.Run(() => Task.FromResult(JsonUtility.FromJson<Files>(json)), token);
            await Awaiters.NextFrame;
            files.document.SetParentRecursively();

            InitializeMetadata(files.document);
            FigmaParser parser = new(files.document, files.styles, GetAssetPath, GetAssetSize);

            Progress.Report(progress, 3, 5, "Downloading missing nodes");
            await AddMissingComponentsAsync(parser, headers, files.document);

            Progress.Report(progress, 4, 5, "Downloading images");
            Func<bool> cleanupImages = default;
            if (downloadImages) cleanupImages = await DownloadImagesAsync(parser, remaps, headers);

            try
            {
                Progress.Report(progress, 5, 5, "Updating uss/uxml files");
                AssetDatabase.StartAssetEditing();

                foreach (string path in Directory.GetFiles(Path.Combine(folder, "Elements"), "*.uxml")) File.Delete(path);
                parser.Run();
                parser.Write(folder, figma.name);

                cleanupImages?.Invoke();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Progress.Finish(progress);
            Debug.Log($"Figma Update {figma.name} OK");
        }
        #endregion
    }
}