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

        readonly Func<BaseNode, string> getClassList;
        readonly Func<BaseNode, bool> enabledInHierarchy;
        readonly Func<BaseNode, (bool hash, string value)> getTemplate;
        readonly Func<BaseNode, (ElementType type, string typeFullName)> getElementType;

        readonly string documentDirectory;
        readonly string documentName;
        readonly UxmlWriter documentXml;
        #endregion

        #region Constructors
        public UxmlBuilder(Files files, string directory, string name, Func<BaseNode, string> getClassList, Func<BaseNode, bool> enabledInHierarchy, Func<BaseNode, (bool hash, string value)> getTemplate, Func<BaseNode, (ElementType type, string typeFullName)> getElementType)
        {
            this.files = files;
            this.getClassList = getClassList;
            this.enabledInHierarchy = enabledInHierarchy;
            this.getTemplate = getTemplate;
            this.getElementType = getElementType;

            documentDirectory = directory;
            documentName = name;
            using (documentXml = new UxmlWriter(documentDirectory, documentName))
                WriteRecursively(files.document, documentXml);
        }
        #endregion

        #region Methods
        void WriteRecursively(BaseNode node, UxmlWriter uxml, bool isComponent = false)
        {
            void WriteDocumentNode(DocumentNode documentNode, UxmlWriter writer)
            {
                using (writer.ElementScope(documentNode, getClassList(node), getElementType(node)))
                {
                    writer.WriteUssStyleReference($"{documentName}.uss");

                    foreach ((string _, Component component) in files.componentSets)
                    {
                        if (component.remote)
                        {
                            Debug.LogWarning(Extensions.BuildTargetMessage($"Target {nameof(Component)} is remote", component.name, "and cannot be imported"));
                            continue;
                        }

                        writer.WriteTemplate(component.name, Path.Combine(componentsDirectoryName, $"{component.name}.uxml"));
                    }

                    documentNode.children.ForEach(child => WriteRecursively(child, writer, isComponent));
                }
            }
            void WriteCanvasNode(CanvasNode canvasNode, UxmlWriter writer)
            {
                using UxmlWriter.UxmlElementScope scope = writer.ElementScope(canvasNode, getClassList(node), getElementType(node));
                canvasNode.children.ForEach(child => WriteRecursively(child, writer, isComponent));
            }
            void WriteSliceNode(SliceNode sliceNode, UxmlWriter writer)
            {
                using UxmlWriter.UxmlElementScope elementScope = writer.ElementScope(sliceNode, getClassList(node), getElementType(node));
            }
            void WriteTextNode(TextNode textNode, UxmlWriter writer)
            {
                using (writer.ElementScope(node, getClassList(node), getElementType(node)))
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

                if (getTemplate(defaultFrameNode) is (var hash, { } template) && template.NotNullOrEmpty())
                {
                    tooltip = hash ? template : default;
                    {
                        using UxmlWriter childWriter = new(Path.Combine(documentDirectory, elementsDirectoryName), template);
                        using UxmlWriter.UxmlElementScope scope = childWriter.ElementScope(defaultFrameNode, getClassList(defaultFrameNode), getElementType(defaultFrameNode));
                        defaultFrameNode.children.ForEach(child => WriteRecursively(child, childWriter, isComponent));
                    }

                    writer.WriteTemplate(template, writer == documentXml ? Path.Combine(elementsDirectoryName, $"{template}.uxml") : $"{template}.uxml");
                }

                using UxmlWriter.UxmlElementScope frameScope = writer.ElementScope(defaultFrameNode, getClassList(defaultFrameNode), getElementType(defaultFrameNode));
                if (tooltip.NotNullOrEmpty())
                    writer.XmlWriter.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
                defaultFrameNode.children.ForEach(child => WriteRecursively(child, writer, isComponent));
            }
            void WriteDefaultShapeNode(DefaultShapeNode defaultShapeNode, UxmlWriter writer)
            {
                string tooltip = default;
                if (getTemplate(defaultShapeNode) is (var hash, { } template) && template.NotNullOrEmpty())
                {
                    tooltip = hash ? template : default;
                    {
                        using UxmlWriter childWriter = new(Path.Combine(documentDirectory, elementsDirectoryName), template);
                        using UxmlWriter.UxmlElementScope uxmlElementScope = childWriter.ElementScope(defaultShapeNode, getClassList(node), getElementType(node));
                    }

                    writer.WriteTemplate(template, writer == documentXml ? Path.Combine(elementsDirectoryName, $"{template}.uxml") : $"{template}.uxml");
                }

                using UxmlWriter.UxmlElementScope elementScope = writer.ElementScope(defaultShapeNode, getClassList(defaultShapeNode), getElementType(defaultShapeNode));

                if (tooltip.NotNullOrEmpty())
                    writer.XmlWriter.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
            }
            void WriteComponentSetNode(ComponentSetNode componentSetNode)
            {
                using UxmlWriter writer = new(Path.Combine(documentDirectory, componentsDirectoryName), componentSetNode.name);

                writer.WriteUssStyleReference($"../{documentName}.uss"); // NOTE: Temporarly we are using directory back

                using UxmlWriter.UxmlElementScope scope = writer.ElementScope(componentSetNode, getClassList(node), getElementType(node));
                componentSetNode.children.ForEach(child => WriteRecursively(child, writer, true));
            }
            void WriteInstanceNode(InstanceNode instanceNode, UxmlWriter writer)
            {
                if (files.components.TryGetValue(instanceNode.componentId, out Component component) && !component.remote &&
                    !string.IsNullOrEmpty(component.componentSetId) &&
                    files.componentSets.TryGetValue(component.componentSetId, out Component target) && !target.remote)
                {
                    string componentSetName = target.name;
                    string classList = getClassList(node);

                    writer.WriteInstance(componentSetName, instanceNode.name, classList);
                }
                else
                {
                    Debug.LogWarning(Extensions.BuildTargetMessage($"Target {nameof(Component)} for node", instanceNode.name, "is not found"));
                    WriteDefaultFrameNode(instanceNode, uxml);
                }
            }

            if (!IsVisible(node) || (!enabledInHierarchy(node) && node is not ComponentSetNode && !isComponent) || IsStateNode(node)) return;

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
            if (node is ComponentSetNode componentSet) WriteComponentSetNode(componentSet);
            if (node is ComponentNode component) WriteDefaultFrameNode(component, uxml);
            if (node is InstanceNode instance) WriteInstanceNode(instance, uxml);
            if (node is BooleanOperationNode booleanOperation) WriteDefaultFrameNode(booleanOperation, uxml);
        }
        #endregion
    }
}