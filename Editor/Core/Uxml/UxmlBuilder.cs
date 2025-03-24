using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityAsyncAwaitUtil;
using UnityEngine;

namespace Figma.Core.Uxml
{
    using Internals;
    using static Internals.Const;
    using static Internals.Extensions;
    using static Internals.PathExtensions;

    internal class UxmlBuilder
    {
        #region Fields
        readonly Data data;
        readonly NodeMetadata nodeMetadata;
        readonly string globalUssFilePath;
        readonly Func<BaseNode, string> getClassList;
        #endregion

        #region Constructors
        public UxmlBuilder(Data data, NodeMetadata nodeMetadata, string globalUssFilePath, Func<BaseNode, string> getClassList)
        {
            this.data = data;
            this.nodeMetadata = nodeMetadata;
            this.globalUssFilePath = globalUssFilePath;
            this.getClassList = getClassList;
        }
        #endregion

        #region Methods
        public void CreateDocument(string directory, string fileName, DocumentNode documentNode, IReadOnlyDictionary<string, IReadOnlyList<string>> framesPaths)
        {
            string CreateTemplateName(string path) => RemoveExtension(path).Replace('/', '-');

            using UxmlWriter writer = new(directory, fileName);

            writer.WriteUssStyleReference(GetRelativePath(writer.FilePath, globalUssFilePath));
            writer.StartElement(documentNode, getClassList(documentNode), nodeMetadata.GetElementType(documentNode));

            foreach (string templatePath in framesPaths.SelectMany(framesPath => framesPath.Value))
            {
                string path = GetRelativePath(writer.FilePath, templatePath);
                writer.WriteTemplate(CreateTemplateName(path), path);
            }

            foreach ((string name, IReadOnlyList<string> scope) in framesPaths)
            {
                if (scope.Count == 0)
                    continue;

                writer.StartElement(nameof(UnityEngine.UIElements.VisualElement), ("class", "unity-viewport"), (nameof(name), name));
                foreach (string path in scope)
                {
                    string frameName = Path.GetFileNameWithoutExtension(path);
                    string templateName = CreateTemplateName(GetRelativePath(writer.FilePath, path));
                    writer.WriteInstance(frameName, templateName, Uss.UssStyle.viewportClass.Name);
                }
                writer.EndElement();
            }

            writer.EndElement();
        }
        public string CreateFrame(string directory, string[] ussStyleFilesPath, IReadOnlyDictionary<string, string> templates, DefaultFrameNode frameNode)
        {
            using UxmlWriter writer = new(directory, frameNode.name);

            WriteStyles(ussStyleFilesPath, writer);

            foreach ((string templateName, string templatePath) in templates)
                writer.WriteTemplate(templateName, GetRelativePath(writer.FilePath, templatePath));

            WriteNodesRecursively(frameNode, writer);

            return writer.FilePath;
        }
        public string CreateComponentSet(string directory, string[] ussStyleFilesPath, ComponentSetNode componentSetNode)
        {
            using UxmlWriter writer = new(directory, componentSetNode.name);

            WriteStyles(ussStyleFilesPath, writer);
            WriteNodesRecursively(componentSetNode, writer);

            return writer.FilePath;
        }
        public string CreateElement(string directory, string[] ussStyleFilesPath, DefaultShapeNode node, string template)
        {
            (bool isHash, string hashedTemplate) = nodeMetadata.GetTemplate(node);
            using UxmlWriter writer = new(directory, isHash ? hashedTemplate : template);

            WriteStyles(ussStyleFilesPath, writer);

            if (node is DefaultFrameNode parent)
                foreach (SceneNode child in parent.children)
                    WriteNodesRecursively(child, writer);

            return writer.FilePath;
        }

        void WriteNodesRecursively(BaseNode node, UxmlWriter uxml, bool isComponent = false)
        {
            void WriteCanvasNode(CanvasNode canvasNode, UxmlWriter writer)
            {
                writer.StartElement(canvasNode, getClassList(canvasNode), nodeMetadata.GetElementType(canvasNode));
                canvasNode.children.ForEach(child => WriteNodesRecursively(child, writer));
                writer.EndElement();
            }
            void WriteSectionNode(SectionNode sectionNode, UxmlWriter writer)
            {
                writer.StartElement(sectionNode, getClassList(sectionNode), nodeMetadata.GetElementType(sectionNode));
                sectionNode.children.ForEach(child => WriteNodesRecursively(child, writer));
                writer.EndElement();

                throw new NotImplementedException(nameof(WriteSectionNode));
            }
            void WriteSliceNode(SliceNode sliceNode, UxmlWriter writer)
            {
                writer.StartElement(sliceNode, getClassList(sliceNode), nodeMetadata.GetElementType(sliceNode));
                writer.EndElement();
            }
            void WriteTextNode(TextNode textNode, UxmlWriter writer)
            {
                writer.StartElement(textNode, getClassList(textNode), nodeMetadata.GetElementType(textNode));

                string text = textNode.style.textCase switch
                {
                    TextCase.UPPER => textNode.characters.ToUpper(Culture),
                    TextCase.LOWER => textNode.characters.ToLower(Culture),
                    _ => textNode.characters
                };
                writer.XmlWriter.WriteAttributeString("text", text);
                writer.EndElement();
            }
            void WriteDefaultFrameNode(DefaultFrameNode defaultFrameNode, UxmlWriter writer)
            {
                WriteDefaultShapeNode(defaultFrameNode, writer, false);
                defaultFrameNode.children.ForEach(child => WriteNodesRecursively(child, writer, isComponent));
                writer.EndElement();
            }
            void WriteDefaultShapeNode(DefaultShapeNode defaultShapeNode, UxmlWriter writer, bool closeElement = true)
            {
                string tooltip = null;
                if (nodeMetadata.GetTemplate(defaultShapeNode) is (var hash, { } template) && template.NotNullOrEmpty())
                    tooltip = hash ? template : null;

                writer.StartElement(defaultShapeNode, getClassList(defaultShapeNode), nodeMetadata.GetElementType(defaultShapeNode));
                if (tooltip.NotNullOrEmpty())
                    writer.XmlWriter.WriteAttributeString(nameof(tooltip), tooltip!); // Use tooltip as a storage for hash template name
                if (closeElement)
                    writer.EndElement();
            }
            void WriteInstanceNode(InstanceNode instanceNode, UxmlWriter writer)
            {
                if (data.components.TryGetValue(instanceNode.componentId, out Component component) && !component.remote &&
                    !string.IsNullOrEmpty(component.componentSetId) &&
                    data.componentSets.TryGetValue(component.componentSetId, out Component target) && !target.remote)
                {
                    string componentSetName = target.name;
                    string classList = getClassList(instanceNode);

                    writer.WriteInstance(instanceNode.name, componentSetName, classList);
                }
                else
                {
                    // Since this code only runs from Parallel, outside of Unity scope
                    // We cannot use Debug.Log() without returning to Unity's thread
                    SyncContextUtil.UnitySynchronizationContext.Post(_ => Debug.LogWarning(BuildTargetMessage($"Target {nameof(Component)} for node", instanceNode.name, "is not found")), null);
                    WriteDefaultFrameNode(instanceNode, writer);
                }
            }

            if (!StylesPreprocessor.IsVisible(node) || (!nodeMetadata.EnabledInHierarchy(node) && node is not ComponentSetNode && !isComponent))
                return;

            if (node is CanvasNode canvas) WriteCanvasNode(canvas, uxml);
            if (node is FrameNode frame) WriteDefaultFrameNode(frame, uxml);
            if (node is GroupNode group) WriteDefaultFrameNode(group, uxml);
            if (node is ComponentSetNode componentSet) WriteDefaultFrameNode(componentSet, uxml);
            if (node is SliceNode slice) WriteSliceNode(slice, uxml);
            if (node is RectangleNode rectangle) WriteDefaultShapeNode(rectangle, uxml);
            if (node is LineNode line) WriteDefaultShapeNode(line, uxml);
            if (node is EllipseNode ellipse) WriteDefaultShapeNode(ellipse, uxml);
            if (node is RegularPolygonNode regularPolygon) WriteDefaultShapeNode(regularPolygon, uxml);
            if (node is StarNode star) WriteDefaultShapeNode(star, uxml);
            if (node is VectorNode vector) WriteDefaultShapeNode(vector, uxml);
            if (node is TextNode text) WriteTextNode(text, uxml);
            if (node is ComponentNode component) WriteDefaultFrameNode(component, uxml);
            if (node is InstanceNode instance) WriteInstanceNode(instance, uxml);
            if (node is BooleanOperationNode booleanOperation) WriteDefaultFrameNode(booleanOperation, uxml);
            if (node is SectionNode sectionNode) WriteDefaultFrameNode(sectionNode, uxml); // WriteSectionNode(sectionNode, uxml);
        }
        #endregion

        #region Support Methods
        void WriteStyles(string[] styles, UxmlWriter writer) => styles.ForEach(ussPath => writer.WriteUssStyleReference(CombinePath(GetRelativePath(writer.FilePath, ussPath))));
        #endregion
    }
}