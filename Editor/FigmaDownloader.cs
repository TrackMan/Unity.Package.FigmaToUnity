using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Figma
{
    using Core;
    using Core.Assets;
    using Internals;
    using static Internals.Const;
    using static Internals.PathExtensions;

    internal class FigmaDownloader : Api
    {
        #region Consts
        const int maxConcurrentRequests = 5;
        const int maxComponentsIdsInOneRequest = 400;
        #endregion

        #region Fields
        readonly AssetsInfo info;

        string componentsDirectoryPath;
        string elementsDirectoryPath;
        string imagesDirectoryPath;
        string framesDirectoryPath;

        FigmaWriter figmaWriter;
        Usages usages;
        NodeMetadata nodeMetadata;
        StylesPreprocessor stylesPreprocessor;
        #endregion

        #region Constructors
        internal FigmaDownloader(string personalAccessToken, string fileKey, AssetsInfo info) : base(personalAccessToken, fileKey) => this.info = info;
        #endregion

        #region Methods
        internal async Task Run(bool downloadImages, string uxmlName, IReadOnlyCollection<Type> frames, bool prune, bool filter, bool systemCopyBuffer, int progress, CancellationToken token)
        {
            framesDirectoryPath = ToAbsolutePath(framesDirectoryName);
            imagesDirectoryPath = ToAbsolutePath(imagesDirectoryName);
            elementsDirectoryPath = ToAbsolutePath(elementsDirectoryName);
            componentsDirectoryPath = ToAbsolutePath(componentsDirectoryName);

            void CreateDirectories()
            {
                Directory.CreateDirectory(framesDirectoryPath);
                Directory.CreateDirectory(imagesDirectoryPath);
                Directory.CreateDirectory(elementsDirectoryPath);
                Directory.CreateDirectory(componentsDirectoryPath);
            }

            int steps = downloadImages ? 5 : 4;

            CreateDirectories();

            await info.cachedAssets.Load(token);

            Progress.Report(progress, 1, steps, "Downloading file");

            List<string> visibleSceneNodes = new(32);

            if (filter)
            {
                Progress.SetDescription(progress, "Filtering nodes");

                Data shallowData = await GetAsync<Data>($"files/{fileKey}?depth=2", token);
                shallowData.document.SetParent();

                NodeMetadata shallowMetadata = new(shallowData.document, frames, true, false, true);
                visibleSceneNodes.AddRange(shallowData.document.children.SelectMany(x => x.children).Where(shallowMetadata.EnabledInHierarchy).Select(node => node.id));

                Progress.SetDescription(progress, string.Empty);
            }

            string idsString = string.Empty;

            if (visibleSceneNodes.Any())
                idsString = $"?ids={string.Join(",", visibleSceneNodes)}";

            string json = await GetJsonAsync($"files/{fileKey}{idsString}", token);

            if (systemCopyBuffer)
                GUIUtility.systemCopyBuffer = json;

            Progress.Report(progress, 2, steps, "Parsing file");

            Data data = await ConvertOnBackgroundAsync<Data>(json, token);
            data.document.SetParent();

            usages = new Usages();
            nodeMetadata = new NodeMetadata(data.document, frames, filter);
            stylesPreprocessor = new StylesPreprocessor(data, info, nodeMetadata, usages);
            figmaWriter = new FigmaWriter(data, stylesPreprocessor, nodeMetadata, usages);

            Progress.Report(progress, 3, steps, "Downloading missing components");
            await DownloadDocumentsAsync(token);

            if (downloadImages)
            {
                Progress.Report(progress, 4, steps, "Downloading images");
                Progress.SetDescription(progress, "Writing Gradients");
                await WriteGradientsAsync(token);
                Progress.SetDescription(progress, "Downloading image fills");
                await GetImageFillsAsync(progress, token);
                Progress.SetDescription(progress, $"Downloading {KnownFormats.png} files");
                await GetImageNodesAsync(progress, usages.PngNodes, UxmlDownloadImages.RenderAsPng, KnownFormats.png, token);
                Progress.SetDescription(progress, $"Downloading {KnownFormats.svg} files");
                await GetImageNodesAsync(progress, usages.SvgNodes, UxmlDownloadImages.RenderAsSvg, KnownFormats.svg, token);
                await info.cachedAssets.Save();
            }

            Progress.SetStepLabel(progress, string.Empty);

            Progress.Report(progress, steps, steps, "Updating *.uss/*.uxml files");
            stylesPreprocessor.Run();
            await figmaWriter.WriteAsync(info.directory, uxmlName, prune);
        }
        internal void CleanUp(bool cleanImages = false)
        {
            void CleanDirectory(string directory, string[] filters)
            {
                IEnumerable<string> target = filters.SelectMany(filter => GetFiles(directory, filter, SearchOption.AllDirectories))
                                                    .Where(fileName => !usages.Files.Contains(fileName));

                foreach (string file in target)
                {
                    FileUtil.DeleteFileOrDirectory(file);

                    string meta = $"{file}.{KnownFormats.meta}";
                    if (File.Exists(meta))
                        FileUtil.DeleteFileOrDirectory(meta);
                }
            }

            string[] textExtensions = { $"*.{KnownFormats.uxml}", $"*.{KnownFormats.uss}" };
            CleanDirectory(componentsDirectoryPath, textExtensions);
            CleanDirectory(elementsDirectoryPath, textExtensions);
            CleanDirectory(framesDirectoryPath, textExtensions);

            if (!cleanImages)
                return;

            string[] imagesExtensions = { $"*.{KnownFormats.svg}", $"*.{KnownFormats.png}" };
            CleanDirectory(imagesDirectoryPath, imagesExtensions);
        }
        public void CleanDirectories()
        {
            void RemoveEmptyDirectory(string path)
            {
                // This should be recursive, but it is good as is.
                if (Directory.Exists(path) && Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
                    FileUtil.DeleteFileOrDirectory(path);
            }

            RemoveEmptyDirectory(componentsDirectoryPath);
            RemoveEmptyDirectory(elementsDirectoryPath);
            RemoveEmptyDirectory(framesDirectoryPath);
            RemoveEmptyDirectory(imagesDirectoryPath);

            if (Directory.Exists(framesDirectoryPath))
                Directory.EnumerateDirectories(framesDirectoryPath).ForEach(RemoveEmptyDirectory);
        }
        #endregion

        #region Support Methods
        async Task DownloadDocumentsAsync(CancellationToken token)
        {
            async Task<IEnumerable<Nodes.Document>> GetMissingComponentsAsync(IEnumerable<string> components) =>
                (await Task.WhenAll(components.Chunk(maxComponentsIdsInOneRequest)
                                              .Select(chunk => GetAsync<Nodes>($"files/{fileKey}/nodes?ids={string.Join(",", chunk.Distinct())}", token))))
                .SelectMany(node => node.nodes.Values.Where(value => value != null));

            if (usages.MissingComponents.Count > 0)
                foreach (Nodes.Document value in await GetMissingComponentsAsync(usages.MissingComponents))
                    stylesPreprocessor.AddMissingComponent(value.document, value.styles);
        }
        async Task GetImageFillsAsync(int progress, CancellationToken token)
        {
            BaseNode[] nodes = usages.ImageFillNodes.Where(x => nodeMetadata.ShouldDownload(x, UxmlDownloadImages.ImageFills)).ToArray();

            if (!nodes.Any())
                return;

            Data.Images filesImages = await GetAsync<Data.Images>($"files/{fileKey}/images", token);
            IEnumerable<string> imageRefs = nodes.OfType<IGeometryMixin>().Select(y => y.fills.OfType<ImagePaint>().First().imageRef);

            IEnumerable<KeyValuePair<string, string>> urls = filesImages.meta.images.Where(item => imageRefs.Contains(item.Key));
            await urls.ForEachParallelAsync(maxConcurrentRequests, x => GetImageAsync(x.Key, x.Value, KnownFormats.png, progress, token), token);
        }
        async Task GetImageNodesAsync(int progress, IEnumerable<BaseNode> targetNodes, UxmlDownloadImages downloadImages, string extension, CancellationToken token)
        {
            BaseNode[] nodes = targetNodes.Where(x => nodeMetadata.ShouldDownload(x, downloadImages)).ToArray();

            if (!nodes.Any())
                return;

            int i = 0;
            IEnumerable<IGrouping<int, string>> groups = nodes.Select(x => x.id).GroupBy(_ => i++ / 100);

            Task<Images>[] tasks = groups.Select(x => GetAsync<Images>($"images/{fileKey}?ids={string.Join(",", x)}&format={extension}", token)).ToArray();
            await Task.WhenAll(tasks);

            IEnumerable<KeyValuePair<string, string>> urls = tasks.SelectMany(x => x.Result.images);
            await urls.ForEachParallelAsync(maxConcurrentRequests, x => GetImageAsync(x.Key, x.Value, extension, progress, token), token);
        }
        async Task GetImageAsync(string nodeID, string url, string extension, int progress, CancellationToken token)
        {
            (bool fileExists, string assetPath) = info.GetAssetPath(nodeID, extension);

            if (usages.Files.Contains(ToAbsolutePath(assetPath)))
                return;

            Progress.SetStepLabel(progress, url);

            // Using HttpClient here instead of UnityWebRequest since UnityWebRequest causes deadlock
            HttpClient client = new();
            headers.ForEach(x => client.DefaultRequestHeaders.Add(x.Key, x.Value));

            if (fileExists && info.cachedAssets.Map.TryGetValue(nodeID, out string etag))
                client.DefaultRequestHeaders.Add("If-None-Match", $"\"{etag}\"");

            HttpResponseMessage response = await client.GetAsync(url, token);

            // Sometimes, one image could be shared by 2 nodes and be occupied, in order to avoid that
            // we are checking if someone already wrote the key.
            bool isResolved = info.cachedAssets.Map.ContainsValue(nodeID);

            if (response.Headers.TryGetValues("ETag", out IEnumerable<string> values))
                info.cachedAssets[nodeID] = values.First().Trim('"');

            // This is invoked again, since the line above is inserting a value for cachedAssets and now the path is updated.
            assetPath = info.GetAssetPath(nodeID, extension).path;
            string path = ToAbsolutePath(assetPath);

            if (usages.Files.Contains(path))
                return;

            usages.Files.Add(path);

            if (response.StatusCode == HttpStatusCode.OK && !isResolved)
            {
                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                if (bytes.Length == 0)
                {
                    Debug.LogWarning($"Response is empty for node={nodeID}, url={url}");

                    if (extension == KnownFormats.svg)
                        await File.WriteAllTextAsync(path, InvalidSvg, token);
                    else
                        await File.WriteAllBytesAsync(path, InvalidPng, token);
                }
                else
                {
                    try
                    {
                        await File.WriteAllBytesAsync(path, bytes, token);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
        }
        async Task WriteGradientsAsync(CancellationToken token)
        {
            foreach ((string key, GradientPaint gradient) in usages.Gradients)
            {
                string xmlPath = ToAbsolutePath(info.GetAssetPath(key, KnownFormats.svg).path);
                using GradientWriter writer = new(xmlPath);
                await writer.WriteAsync(gradient, token);
                usages.Files.Add(xmlPath);
            }
        }
        string ToAbsolutePath(string path) => CombinePath(info.directory, path);
        #endregion
    }
}