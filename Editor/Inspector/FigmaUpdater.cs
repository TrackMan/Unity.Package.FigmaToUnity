using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Figma.Inspectors
{
    using Internals;
    using static Internals.Const;

    internal class FigmaUpdater : FigmaApi
    {
        const int maxConcurrentRequests = 5;
        const string images = FigmaParser.images;
        const string elements = FigmaParser.elements;

        #region Containers
#pragma warning disable S1144 // Called from Unity
        class ImageWatcher : AssetPostprocessor
        {
            #region Methods
            void OnPreprocessAsset()
            {
                DirectoryInfo parentDirectory = Directory.GetParent(assetPath)?.Parent;
                if (parentDirectory is null || !Directory.GetFiles(parentDirectory!.FullName, "*.uxml").Any()) return;

                if (assetPath.Contains(images) && assetImporter is SVGImporter svgImporter)
                    svgImporter.SvgType = SVGType.UIToolkit;

                if (!assetPath.Contains(images) || assetImporter is not TextureImporter textureImporter) return;

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
#pragma warning restore S1144
        #endregion

        #region Fields
        readonly string folder;
        readonly string relativeFolder;
        readonly IReadOnlyCollection<string> fontDirs;

        string remapsFilename;
        Dictionary<string, string> remaps;

        FigmaParser parser;
        NodeMetadata nodeMetadata;
        #endregion

        #region Constructors
        internal FigmaUpdater(string personalAccessToken, string title, string folder, string relativeFolder, IReadOnlyCollection<string> fontDirs) : base(personalAccessToken, title)
        {
            this.folder = folder;
            this.relativeFolder = relativeFolder;
            this.fontDirs = fontDirs;
        }
        #endregion

        #region Methods
        internal async Task DownloadAssetsAsync(string name, bool downloadImages, IReadOnlyCollection<Type> elements, bool filter, bool systemCopyBuffer, int progress, CancellationToken token)
        {
            async Task<Files> GetFilesAsync()
            {
                Progress.Report(progress, 1, 5, "Downloading title");

                List<string> visibleSceneNodes = new();
                if (filter)
                {
                    Progress.SetDescription(progress, "Filtering nodes");

                    Files shallowFiles = await GetAsync<Files>($"files/{title}?depth=2", token);
                    shallowFiles.document.SetParentRecursively();

                    NodeMetadata shallowMetadata = new(shallowFiles.document, elements, true, false, true);

                    foreach (SceneNode node in shallowFiles.document.children.SelectMany(x => x.children))
                        if (shallowMetadata.EnabledInHierarchy(node))
                            visibleSceneNodes.Add(node.id);

                    Progress.SetDescription(progress, string.Empty);
                }

                string idsString = string.Empty;
                if (visibleSceneNodes.Count > 0) idsString = $"?ids={string.Join(",", visibleSceneNodes)}";

                string json = await GetJsonAsync($"files/{title}{idsString}", token);
                if (systemCopyBuffer) GUIUtility.systemCopyBuffer = json;

                Progress.Report(progress, 2, 5, "Parsing title");
                Files files = await ConvertOnBackgroundAsync<Files>(json, token);
                files.document.SetParentRecursively();
                return files;
            }
            async Task DownloadMissingComponentsAsync()
            {
                async Task<IEnumerable<Nodes.Document>> GetMissingComponentsAsync(IEnumerable<string> components)
                {
                    Nodes nodes = await GetAsync<Nodes>($"files/{title}/nodes?ids={string.Join(",", components.Distinct())}", token);
                    return nodes.nodes.Values.Where(value => value is not null);
                }

                Progress.Report(progress, 3, 5, "Downloading missing components");
                if (parser.MissingComponents.Count > 0)
                    foreach (Nodes.Document value in await GetMissingComponentsAsync(parser.MissingComponents))
                        parser.AddMissingComponent(value.document, value.styles);
            }
            async Task DownloadImagesAsync()
            {
                Progress.Report(progress, 4, 5, "Downloading images");
                await this.DownloadImagesAsync(progress, token);
            }

            CreateDirectories();
            await LoadRemapsAsync(name, token);

            Files files = await GetFilesAsync();
            nodeMetadata = new NodeMetadata(files.document, elements, filter);
            parser = new FigmaParser(files.document, files.styles, nodeMetadata.EnabledInHierarchy);
            await DownloadMissingComponentsAsync();
            if (downloadImages) 
                await DownloadImagesAsync();
        }
        internal void WriteUssUxml(string name, int progress)
        {
            Progress.Report(progress, 5, 5, "Updating *.uss/*.uxml files");

            parser.Run(GetAssetPath, GetAssetSize);
            parser.Write(folder, name, nodeMetadata.EnabledInHierarchy, nodeMetadata.GetTemplate, nodeMetadata.GetElementType);
        }
        internal void Cleanup(string name)
        {
            string uxmlContents = File.ReadAllText(Path.Combine(folder, $"{name}.uxml"));
            foreach (string path in Directory.GetFiles(Path.Combine(folder, elements), "*.uxml"))
            {
                string filename = Path.GetFileName(path);
                string relativePath = Path.Combine(relativeFolder, elements, filename);
                if (uxmlContents.Contains(filename)) continue;

                Debug.LogWarning($"Removing obsolete uxml {relativePath}");
                FileUtil.DeleteFileOrDirectory(path);
            }

            string ussContents = File.ReadAllText(Path.Combine(folder, $"{name}.uss"));
            foreach (string path in Directory.GetFiles(Path.Combine(folder, elements), "*.png"))
            {
                string filename = Path.GetFileName(path);
                string relativePath = Path.Combine(relativeFolder, elements, filename);
                if (ussContents.Contains(filename)) continue;

                Debug.LogWarning($"Removing obsolete image {relativePath}");
                FileUtil.DeleteFileOrDirectory(path);
            }

            foreach (string path in Directory.GetFiles(Path.Combine(folder, elements), "*.svg"))
            {
                string filename = Path.GetFileName(path);
                string relativePath = Path.Combine(relativeFolder, elements, filename);
                if (ussContents.Contains(filename)) continue;

                Debug.LogWarning($"Removing obsolete image {relativePath}");
                FileUtil.DeleteFileOrDirectory(path);
            }
        }
        #endregion

        #region Support Methods
        void CreateDirectories()
        {
            Directory.CreateDirectory(Path.Combine(folder, images));
            Directory.CreateDirectory(Path.Combine(folder, elements));
        }

        async Task LoadRemapsAsync(string name, CancellationToken token)
        {
            remapsFilename = Path.Combine(folder, $"remaps_{name}.json");
            remaps = File.Exists(remapsFilename) ? JsonUtility.FromJson<Dictionary<string, string>>(await File.ReadAllTextAsync(remapsFilename, token)) : new Dictionary<string, string>();
        }
        async Task SaveRemapsAsync() => await File.WriteAllTextAsync(remapsFilename, JsonUtility.ToJson(remaps, prettyPrint: true));

        async Task DownloadImagesAsync(int progress, CancellationToken token)
        {
            async Task WriteInvalidSvgAsync(string assetPath)
            {
                XmlWriter writer = XmlWriter.Create(Path.Combine(folder, assetPath), new XmlWriterSettings { Indent = true, NewLineOnAttributes = true, IndentChars = indentCharacters, Async = true });
                writer.WriteStartElement(KnownFormats.svg);
                {
                    writer.WriteStartElement("rect");
                    writer.WriteAttributeString("width", "100");
                    writer.WriteAttributeString("height", "100");
                    writer.WriteAttributeString("fill", "magenta");
                    await writer.WriteEndElementAsync();
                    await Task.Delay(0, token);
                }

                await writer.WriteEndElementAsync();
                await Task.Delay(0, token);

                writer.Close();
            }
            async Task WriteInvalidPngAsync(string assetPath) => await File.WriteAllBytesAsync(Path.Combine(folder, assetPath), invalidPng, token);
            async Task GetImageAsync(string nodeID, string url, string extension)
            {
                (bool fileExists, _) = GetAssetPath(nodeID, extension);

                Progress.SetStepLabel(progress, url);

                // Using HttpClient here instead of UnityWebRequest because using of UnityWebRequest causes deadlock
                HttpClient client = new();
                foreach (KeyValuePair<string, string> header in headers) client.DefaultRequestHeaders.Add(header.Key, header.Value);
                if (fileExists && remaps.TryGetValue(nodeID, out string etag)) client.DefaultRequestHeaders.Add("If-None-Match", $"\"{etag}\"");
                HttpResponseMessage response = await client.GetAsync(url, token);

                if (response.Headers.TryGetValues("ETag", out IEnumerable<string> values))
                    remaps[nodeID] = values.First().Trim('"');

                (bool _, string assetPath) = GetAssetPath(nodeID, extension);
                string relativePath = Path.Combine(relativeFolder, assetPath).Replace('\\', '/');
                ;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                    switch (bytes.Length)
                    {
                        case 0:
                            Debug.LogWarning($"Response is empty for node={nodeID}, url={url}");
                            switch (extension)
                            {
                                case KnownFormats.svg:
                                    await WriteInvalidSvgAsync(assetPath);
                                    break;

                                default:
                                    await WriteInvalidPngAsync(assetPath);
                                    break;
                            }

                            break;

                        default:
                            await File.WriteAllBytesAsync(relativePath, bytes, token);
                            break;
                    }
                }
            }
            async Task WriteGradientsAsync(int progress, CancellationToken token)
            {
                Progress.SetDescription(progress, "Write Gradients");

                foreach ((string key, GradientPaint gradient) in parser.Gradients)
                {
                    XmlWriter writer = XmlWriter.Create(Path.Combine(folder, GetAssetPath(key, KnownFormats.svg).path), new XmlWriterSettings
                    {
                        Indent = true,
                        NewLineOnAttributes = true,
                        NewLineChars = Environment.NewLine,
                        IndentChars = indentCharacters,
                        Async = true
                    });
                    writer.WriteStartElement(KnownFormats.svg);
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
                                        writer.WriteAttributeString($"x{i + 1}", gradient.gradientHandlePositions[i].x.ToString("F2", culture));
                                        writer.WriteAttributeString($"y{i + 1}", gradient.gradientHandlePositions[i].y.ToString("F2", culture));
                                    }

                                    break;

                                case PaintType.GRADIENT_RADIAL:
                                case PaintType.GRADIENT_DIAMOND:
                                    writer.WriteStartElement("radialGradient");
                                    writer.WriteAttributeString("id", "gradient");
                                    writer.WriteAttributeString("fx", $"{gradient.gradientHandlePositions[0].x.ToString("F2", culture)}");
                                    writer.WriteAttributeString("fy", $"{gradient.gradientHandlePositions[0].y.ToString("F2", culture)}");
                                    writer.WriteAttributeString("cx", $"{gradient.gradientHandlePositions[0].x.ToString("F2", culture)}");
                                    writer.WriteAttributeString("cy", $"{gradient.gradientHandlePositions[0].y.ToString("F2", culture)}");

                                    float radius = Vector2.Distance(new Vector2((float)gradient.gradientHandlePositions[1].x, (float)gradient.gradientHandlePositions[1].y), new Vector2((float)gradient.gradientHandlePositions[0].x, (float)gradient.gradientHandlePositions[0].y));
                                    writer.WriteAttributeString("r", $"{radius.ToString("F2", culture)}");
                                    break;

                                default:
                                    throw new NotSupportedException();
                            }

                            foreach (ColorStop stop in gradient.gradientStops)
                            {
                                writer.WriteStartElement("stop");
                                writer.WriteAttributeString("offset", stop.position.ToString("F2", culture));
                                writer.WriteAttributeString("style", $"stop-color:rgb({(byte)(stop.color.r * 255)},{(byte)(stop.color.g * 255)},{(byte)(stop.color.b * 255)});stop-opacity:{stop.color.a.ToString("F2", culture)}");
                                await writer.WriteEndElementAsync();
                            }

                            await writer.WriteEndElementAsync();
                            await Task.Delay(0, token);
                        }

                        await writer.WriteEndElementAsync();
                        await Task.Delay(0, token);

                        writer.WriteStartElement("rect");
                        writer.WriteAttributeString("width", "100");
                        writer.WriteAttributeString("height", "100");
                        writer.WriteAttributeString("fill", "url(#gradient)");

                        if (gradient.opacity.HasValue)
                            writer.WriteAttributeString("fill-opacity", gradient.opacity.Value.ToString("F2", culture));

                        await writer.WriteEndElementAsync();
                        await Task.Delay(0, token);
                    }

                    await writer.WriteEndElementAsync();
                    await Task.Delay(0, token);

                    writer.Close();
                }
            }
            async Task GetImageFillsAsync()
            {
                Progress.SetDescription(progress, "Downloading image fills");

                BaseNode[] nodes = parser.ImageFillNodes.Where(x => nodeMetadata.ShouldDownload(x, UxmlDownloadImages.ImageFills)).ToArray();
                if (!nodes.Any()) return;

                Files.Images filesImages = await GetAsync<Files.Images>($"files/{title}/images", token);
                IEnumerable<string> imageRefs = nodes.OfType<GeometryMixin>().Select(y => y.fills.OfType<ImagePaint>().First().imageRef);

                IEnumerable<KeyValuePair<string, string>> urls = filesImages.meta.images.Where(item => imageRefs.Contains(item.Key));
                await urls.ForEachParallelAsync(maxConcurrentRequests, x => GetImageAsync(x.Key, x.Value, KnownFormats.png), token);
            }
            async Task GetImageNodesAsync(IEnumerable<BaseNode> targetNodes, UxmlDownloadImages downloadImages, string extension)
            {
                Progress.SetDescription(progress, $"Downloading {downloadImages} images");

                BaseNode[] nodes = targetNodes.Where(x => nodeMetadata.ShouldDownload(x, downloadImages)).ToArray();
                if (!nodes.Any()) return;

                int i = 0;
                IEnumerable<IGrouping<int, string>> groups = nodes.Select(x => x.id).GroupBy(_ => i++ / 100);

                Task<Images>[] tasks = groups.Select(x => GetAsync<Images>($"images/{title}?ids={string.Join(",", x)}&format={extension}", token)).ToArray();
                await Task.WhenAll(tasks);

                IEnumerable<KeyValuePair<string, string>> urls = tasks.SelectMany(x => x.Result.images);
                await urls.ForEachParallelAsync(maxConcurrentRequests, x => GetImageAsync(x.Key, x.Value, extension), token);
            }

            await WriteGradientsAsync(progress, token);
            await GetImageFillsAsync();
            await GetImageNodesAsync(parser.PngNodes, UxmlDownloadImages.RenderAsPng, KnownFormats.png);
            await GetImageNodesAsync(parser.SvgNodes, UxmlDownloadImages.RenderAsSvg, KnownFormats.svg);
            await SaveRemapsAsync();

            Progress.SetStepLabel(progress, string.Empty);
        }

        (bool valid, string path) GetAssetPath(string name, string extension)
        {
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

            switch (extension)
            {
                case KnownFormats.otf or KnownFormats.ttf:
                    string fontPath = GetFontPath(name, extension);
                    return (fontPath.NotNullOrEmpty(), fontPath);

                case KnownFormats.asset:
                    string fontAssetPath = GetFontPath(name, extension);
                    return (fontAssetPath.NotNullOrEmpty(), $"{Path.GetDirectoryName(fontAssetPath)}/{name} SDF.{extension}");

                case KnownFormats.png or KnownFormats.svg:
                    remaps.TryGetValue(name, out string mappedName);
                    string filename = $"{images}/{mappedName ?? name}.{extension}";
                    return (File.Exists(Path.Combine(folder, filename)), filename);

                default:
                    throw new NotSupportedException(extension);
            }
        }

        (bool valid, int width, int height) GetAssetSize(string name, string extension)
        {
            (bool valid, string path) = GetAssetPath(name, extension);
            switch (extension)
            {
                case KnownFormats.png:
                    if (!valid) return (false, -1, -1);

                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Path.Combine(relativeFolder, path));
                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                    return (true, width, height);

                case KnownFormats.svg:
                    if (!valid) return (false, -1, -1);

                    SVGImporter svgImporter = (SVGImporter)AssetImporter.GetAtPath(Path.Combine(relativeFolder, path));
                    Object vectorImage = AssetDatabase.LoadMainAssetAtPath(Path.Combine(relativeFolder, path));

                    if (!svgImporter || !vectorImage) return (false, -1, -1);

                    if (vectorImage.GetType().GetField("size", BindingFlags.NonPublic | BindingFlags.Instance) is not { } fieldInfo)
                        return (true, svgImporter ? svgImporter.TextureWidth : -1, svgImporter ? svgImporter.TextureHeight : -1);

                    Vector2 size = (Vector2)fieldInfo.GetValue(vectorImage);
                    return (true, Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));

                default:
                    throw new NotSupportedException(extension);
            }
        }
        #endregion
    }
}