using System;
using System.IO;
using UnityEngine;

namespace Figma.Core.Uxml
{
    using Internals;
    using static FigmaParser;

    internal class UxmlBuilder
    {
        #region Fields
        readonly Files files;
        readonly NodeMetadata nodeMetadata;

        readonly Func<BaseNode, string> getClassList;

        readonly string documentDirectory;
        readonly string documentName;
        readonly UxmlWriter documentXml;
        #endregion

        #region Constructors
        public UxmlBuilder(string directory, string name, Files files, NodeMetadata nodeMetadata, Func<BaseNode, string> getClassList)
        {
            this.files = files;
            this.nodeMetadata = nodeMetadata;
            this.getClassList = getClassList;

            documentDirectory = directory;
            documentName = name;

            using (documentXml = new UxmlWriter(documentDirectory, documentName))
                WriteNodesRecursively(files.document, documentXml);
        }
        #endregion

        #region Methods
        void WriteNodesRecursively(BaseNode node, UxmlWriter uxml, bool isComponent = false)
        {
            void WriteDocumentNode(DocumentNode documentNode, UxmlWriter writer)
            {
                using (writer.ElementScope(documentNode, getClassList(documentNode), nodeMetadata.GetElementType(documentNode)))
                {
                    writer.WriteUssStyleReference($"{documentName}.uss");
                    documentNode.children.ForEach(child => WriteNodesRecursively(child, writer));
                }
            }
            void WriteCanvasNode(CanvasNode canvasNode, UxmlWriter writer)
            {
                using UxmlWriter.UxmlElementScope scope = writer.ElementScope(canvasNode, getClassList(canvasNode), nodeMetadata.GetElementType(canvasNode));
                canvasNode.children.ForEach(child => WriteNodesRecursively(child, writer));
            }
            void WriteSliceNode(SliceNode sliceNode, UxmlWriter writer)
            {
                using UxmlWriter.UxmlElementScope elementScope = writer.ElementScope(sliceNode, getClassList(sliceNode), nodeMetadata.GetElementType(sliceNode));
            }
            void WriteTextNode(TextNode textNode, UxmlWriter writer)
            {
                using (writer.ElementScope(textNode, getClassList(textNode), nodeMetadata.GetElementType(textNode)))
                {
                    string text = textNode.style.textCase switch
                    {
                        TextCase.UPPER => textNode.characters.ToUpper(Const.culture),
                        TextCase.LOWER => textNode.characters.ToLower(Const.culture),
                        _ => textNode.characters
                    };
                    writer.XmlWriter.WriteAttributeString("text", text);
                }
            }
            void WriteDefaultFrameNode(DefaultFrameNode defaultFrameNode, UxmlWriter writer)
            {
                string tooltip = default;

                if (nodeMetadata.GetTemplate(defaultFrameNode) is (var hash, { } template) && template.NotNullOrEmpty())
                {
                    tooltip = hash ? template : default;
                    {
                        using UxmlWriter childWriter = new(Path.Combine(documentDirectory, elementsDirectoryName), template);
                        using UxmlWriter.UxmlElementScope scope = childWriter.ElementScope(defaultFrameNode, getClassList(defaultFrameNode), nodeMetadata.GetElementType(defaultFrameNode));

                        foreach (BaseNode child in defaultFrameNode.children)
                            WriteNodesRecursively(child, childWriter, isComponent);
                    }

                    writer.WriteTemplate(template, writer == documentXml ? Path.Combine(elementsDirectoryName, $"{template}.uxml") : $"{template}.uxml");
                }

                using UxmlWriter.UxmlElementScope frameScope = writer.ElementScope(defaultFrameNode, getClassList(defaultFrameNode), nodeMetadata.GetElementType(defaultFrameNode));
                if (tooltip.NotNullOrEmpty())
                    writer.XmlWriter.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
                defaultFrameNode.children.ForEach(child => WriteNodesRecursively(child, writer, isComponent));
            }
            void WriteDefaultShapeNode(DefaultShapeNode defaultShapeNode, UxmlWriter writer)
            {
                string tooltip = default;
                if (nodeMetadata.GetTemplate(defaultShapeNode) is (var hash, { } template) && template.NotNullOrEmpty())
                {
                    tooltip = hash ? template : default;
                    {
                        using UxmlWriter childWriter = new(Path.Combine(documentDirectory, elementsDirectoryName), template);
                        using UxmlWriter.UxmlElementScope uxmlElementScope = childWriter.ElementScope(defaultShapeNode, getClassList(defaultShapeNode), nodeMetadata.GetElementType(defaultShapeNode));
                    }

                    writer.WriteTemplate(template, writer == documentXml ? Path.Combine(elementsDirectoryName, $"{template}.uxml") : $"{template}.uxml");
                }

                using UxmlWriter.UxmlElementScope elementScope = writer.ElementScope(defaultShapeNode, getClassList(defaultShapeNode), nodeMetadata.GetElementType(defaultShapeNode));

                if (tooltip.NotNullOrEmpty())
                    writer.XmlWriter.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
            }
            void WriteInstanceNode(InstanceNode instanceNode, UxmlWriter writer)
            {
                if (files.components.TryGetValue(instanceNode.componentId, out Component component) && !component.remote &&
                    !string.IsNullOrEmpty(component.componentSetId) &&
                    files.componentSets.TryGetValue(component.componentSetId, out Component target) && !target.remote)
                {
                    string componentSetName = target.name;
                    string classList = getClassList(instanceNode);

                    writer.WriteInstance(componentSetName, instanceNode.name, classList);
                }
                else
                {
                    Debug.LogWarning(Extensions.BuildTargetMessage($"Target {nameof(Component)} for node", instanceNode.name, "is not found"));
                    WriteDefaultFrameNode(instanceNode, writer);
                }
            }

            if (!IsVisible(node) || (!nodeMetadata.EnabledInHierarchy(node) && node is not ComponentSetNode && !isComponent) || IsStateNode(node))
                return;

            if (node is DocumentNode document) WriteDocumentNode(document, uxml);
            if (node is CanvasNode canvas) WriteCanvasNode(canvas, uxml);
            if (node is FrameNode frame) WriteDefaultFrameNode(frame, uxml);
            if (node is GroupNode group) WriteDefaultFrameNode(group, uxml);
            if (node is SliceNode slice) WriteSliceNode(slice, uxml);
            if (node is RectangleNode rectangle) WriteDefaultShapeNode(rectangle, uxml);
            if (node is LineNode line) WriteDefaultShapeNode(line, uxml);
            if (node is EllipseNode ellipse) WriteDefaultShapeNode(ellipse, uxml);
            if (node is RegularPolygonNode regularPolygon) WriteDefaultShapeNode(regularPolygon, uxml);
            if (node is StarNode star) WriteDefaultShapeNode(star, uxml);
            if (node is VectorNode vector) WriteDefaultShapeNode(vector, uxml);
            if (node is TextNode text) WriteTextNode(text, uxml);
            if (node is ComponentSetNode componentSet) WriteDefaultShapeNode(componentSet, uxml);
            if (node is ComponentNode component) WriteDefaultFrameNode(component, uxml);
            if (node is InstanceNode instance) WriteInstanceNode(instance, uxml);
            if (node is BooleanOperationNode booleanOperation) WriteDefaultFrameNode(booleanOperation, uxml);
        }
        #endregion
    }
}