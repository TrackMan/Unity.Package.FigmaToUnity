using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable S1144 // Unused private types or members should be removed

namespace Figma
{
    using Core;
    using Core.Assets;
    using Core.Uss;
    using Core.Uxml;
    using Internals;
    using static Internals.Const;
    using static Internals.PathExtensions;

    internal sealed class FigmaWriter
    {
        #region Fields
        readonly Data data;
        readonly NodeMetadata nodeMetadata;
        readonly Usages usages;
        readonly StylesPreprocessor stylesPreprocessor;
        #endregion

        #region Properties
        DocumentNode Document => data.document;
        #endregion

        #region Constructors
        internal FigmaWriter(Data data, StylesPreprocessor stylesPreprocessor, NodeMetadata nodeMetadata, Usages usages)
        {
            this.nodeMetadata = nodeMetadata;
            this.data = data;
            this.usages = usages;
            this.stylesPreprocessor = stylesPreprocessor;
        }
        #endregion

        #region Methods
        internal async Task WriteAsync(string directory, string name, bool overrideGlobal = false)
        {
            RootNodes rootNodes = new(data, nodeMetadata);
            // We do need this, since the Data.componentSets do not contain updated names.
            Dictionary<string, ComponentSetNode> componentSets = rootNodes.ComponentSets
                                                                          .OrderBy(x => x.id) // Ordering to avoid index confusion, since order in the Collection could vary from one request to another.
                                                                          .ToList()
                                                                          .IndexRedundantNames(x => x.name,
                                                                                               (componentSet, postfix) => componentSet.name += postfix,
                                                                                               index => index == 0 ? string.Empty : "-" + index)
                                                                          .ToDictionary(x => x.id);

            KeyValuePair<BaseNode, UssStyle>[] nodeStyleFiltered = stylesPreprocessor.NodeStyleMap.Where(x => StylesPreprocessor.IsVisible(x.Key) && (nodeMetadata.EnabledInHierarchy(x.Key) || x.Key is ComponentSetNode)).ToArray();
            UssStyle[] nodeStyleStatelessFiltered = nodeStyleFiltered.Select(x => x.Value).ToArray();
            UssStyle[] globalStaticStyles = stylesPreprocessor.Styles.Select(x => x.style).Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();

            // Writing global USS styles
            string globalUssPath = CombinePath(directory, $"{name}.{KnownFormats.uss}");

            if (overrideGlobal)
            {
                await using UssWriter globalUssWriter = new(directory, globalUssPath);
                globalUssWriter.Write(UssStyle.overrideClass);
                globalUssWriter.Write(UssStyle.viewportClass);
                globalUssWriter.Write(globalStaticStyles.IndexRedundantNames(x => x.Name, (style, postfix) => style.Name += postfix, index => "-" + (index + 1).NumberToWords()));
            }

            // Writing UXML files
            UxmlBuilder builder = new(data, nodeMetadata, globalUssPath, stylesPreprocessor.GetClassList);
            Dictionary<string, IReadOnlyList<string>> framesPaths = new(rootNodes.Frames.Count);

            foreach (CanvasNode canvasNode in rootNodes.Canvases)
                framesPaths.Add(canvasNode.name, new List<string>());

            void WriteFrame(FrameNode frameNode)
            {
                Dictionary<string, string> templates = new();

                void FindTemplates(BaseNode root)
                {
                    Stack<BaseNode> nodes = new();
                    nodes.Push(root);
                    int i = 0;

                    while (nodes.Count > 0)
                    {
                        BaseNode node = nodes.Pop();

                        if (node is InstanceNode instanceNode)
                        {
                            Component component = data.components[instanceNode.componentId];

                            if (component == null || component.remote || string.IsNullOrEmpty(component.componentSetId))
                                return;

                            Component componentSet = data.componentSets[component.componentSetId];

                            if (componentSet == null || componentSet.remote)
                                return;

                            string template = componentSets[component.componentSetId].name;
                            templates[template] = CombinePath(directory, componentsDirectoryName, $"{template}.{KnownFormats.uxml}");
                        }
                        else if (nodeMetadata.GetTemplate(node) is (_, { } template) && template.NotNullOrEmpty())
                        {
                            templates[template] = CombinePath(directory, elementsDirectoryName, $"{template}.{KnownFormats.uxml}");
                        }

                        if (node is DefaultFrameNode frameNode)
                            foreach (SceneNode child in frameNode.children)
                                nodes.Push(child);

                        if (i++ > maximalDepthLimit)
                            throw new InvalidOperationException(maximalDepthLimitMessage);
                    }
                }

                string rootDirectory = CombinePath(directory, framesDirectoryName, frameNode.parent.name);

                if (!Directory.Exists(rootDirectory))
                    Directory.CreateDirectory(rootDirectory);

                using UssWriter ussWriter = new(directory, CombinePath(rootDirectory, $"{frameNode.name}.{KnownFormats.uss}"));
                ussWriter.Write(stylesPreprocessor.GetStyles(frameNode).IndexRedundantNames(x => x.Name, (style, postfix) => style.Name += postfix, index => "-" + (index + 1).NumberToWords()));

                FindTemplates(frameNode);

                string uxmlPath = builder.CreateFrame(rootDirectory, new[] { globalUssPath, ussWriter.Path }, templates, frameNode);
                framesPaths[frameNode.parent.name].As<List<string>>().Add(uxmlPath);

                usages.RecordFiles(uxmlPath, ussWriter.Path);
                templates.Clear();
            }
            void WriteComponentSet(ComponentSetNode componentSet)
            {
                using UssWriter ussWriter = new(directory, CombinePath(directory, componentsDirectoryName, $"{componentSet.name}.{KnownFormats.uss}"));
                ussWriter.Write(stylesPreprocessor.GetStyles(componentSet).IndexRedundantNames(x => x.Name, (style, postfix) => style.Name += postfix, index => "-" + (index + 1).NumberToWords()));

                string uxmlPath = builder.CreateComponentSet(CombinePath(directory, componentsDirectoryName), new[] { globalUssPath, ussWriter.Path }, componentSet);
                usages.RecordFiles(uxmlPath, ussWriter.Path);
            }
            void WriteTemplate((DefaultShapeNode element, string template) node)
            {
                (bool isHash, string hashedTemplates) = nodeMetadata.GetTemplate(node.element);

                using UssWriter ussWriter = new(directory, CombinePath(directory, elementsDirectoryName, $"{(isHash ? hashedTemplates : node.template)}.{KnownFormats.uss}"));
                ussWriter.Write(stylesPreprocessor.GetStyles(node.element).IndexRedundantNames(x => x.Name, (style, postfix) => style.Name += postfix, index => "-" + (index + 1).NumberToWords()));

                string uxmlPath = builder.CreateElement(CombinePath(directory, elementsDirectoryName), new[] { globalUssPath, ussWriter.Path }, node.element, node.template);
                usages.RecordFiles(uxmlPath, ussWriter.Path);
            }

            List<Task> tasks = new(rootNodes.Frames.Count + rootNodes.ComponentSets.Count + rootNodes.Elements.Count);
            tasks.AddRange(rootNodes.Frames.Select(x => Task.Run(() => WriteFrame(x))));
            tasks.AddRange(rootNodes.ComponentSets.Select(x => Task.Run(() => WriteComponentSet(x))));
            tasks.AddRange(rootNodes.Elements.Select(x => Task.Run(() => WriteTemplate(x))));

            await Task.WhenAll(tasks);

            // Creating main UXML document
            if (overrideGlobal)
                builder.CreateDocument(directory, name, data.document, framesPaths);
        }
        #endregion
    }
}