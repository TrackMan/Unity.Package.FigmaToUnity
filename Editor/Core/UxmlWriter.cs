using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace Figma.Core
{
    using Internals;
    using static Internals.Const;
    using static FigmaParser;

    internal class UxmlWriter
    {
        const string prefix = "unity";
        static readonly XmlWriterSettings xmlWriterSettings = new() { OmitXmlDeclaration = true, Indent = true, IndentChars = indentCharacters, NewLineOnAttributes = false };

        #region Fields
        readonly Files files;

        readonly Func<BaseNode, string> getClassList;
        readonly Func<BaseNode, bool> enabledInHierarchy;
        readonly Func<BaseNode, (bool hash, string value)> getTemplate;
        readonly Func<BaseNode, (ElementType type, string typeFullName)> getElementType;

        readonly string documentFolder;
        readonly string documentName;
        readonly XmlWriter documentXml;
        #endregion

        #region Constructors
        public UxmlWriter(Files files, string folder, string name, Func<BaseNode, string> getClassList, Func<BaseNode, bool> enabledInHierarchy, Func<BaseNode, (bool hash, string value)> getTemplate, Func<BaseNode, (ElementType type, string typeFullName)> getElementType)
        {
            this.files = files;
            this.getClassList = getClassList;
            this.enabledInHierarchy = enabledInHierarchy;
            this.getTemplate = getTemplate;
            this.getElementType = getElementType;

            documentFolder = folder;
            documentName = name;
            using (documentXml = CreateXml(documentFolder, documentName)) 
                WriteRecursively(files.document, documentXml);
        }
        #endregion

        #region Methods
        void WriteRecursively(BaseNode node, XmlWriter uxml, bool isComponent = false)
        {
            void WriteDocumentNode(DocumentNode documentNode, XmlWriter writer)
            {
                writer.WriteStartElement(prefix, "UXML", uxmlNamespace);
                WriteStart(documentNode, writer);

                writer.WriteStartElement("Style");
                writer.WriteAttributeString("src", $"{documentName}.uss");
                writer.WriteEndElement();

                foreach ((string _, Component component) in files.componentSets)
                {
                    if (component.remote)
                    {
                        Debug.LogWarning(Internals.Extensions.BuildTargetMessage($"Target {nameof(Component)} is remote", component.name, "and cannot be imported"));
                        continue;
                    }

                    writer.WriteStartElement(prefix, "Template", uxmlNamespace);
                    writer.WriteAttributeString("src", Path.Combine(componentsDirectoryName, $"{component.name}.uxml"));
                    writer.WriteAttributeString("name", component.name);
                    writer.WriteEndElement();
                }

                documentNode.children.ForEach(child => WriteRecursively(child, writer, isComponent));

                WriteEnd(writer);
                writer.WriteEndElement();
            }
            void WriteCanvasNode(CanvasNode canvasNode, XmlWriter writer)
            {
                WriteStart(canvasNode, writer);
                canvasNode.children.ForEach(child => WriteRecursively(child, writer, isComponent));
                WriteEnd(writer);
            }
            void WriteSliceNode(SliceNode sliceNode, XmlWriter writer)
            {
                WriteStart(sliceNode, writer);
                WriteEnd(writer);
            }
            void WriteTextNode(TextNode textNode, XmlWriter writer)
            {
                WriteStart(node, writer);

                string text = textNode.style.textCase switch
                {
                    TextCase.UPPER => textNode.characters.ToUpper(culture),
                    TextCase.LOWER => textNode.characters.ToLower(culture),
                    _ => textNode.characters
                };

                writer.WriteAttributeString("text", text);

                WriteEnd(writer);
            }
            void WriteDefaultFrameNode(DefaultFrameNode defaultFrameNode, XmlWriter writer)
            {
                string tooltip = default;
                if (getTemplate(defaultFrameNode) is (var hash, { } template) && template.NotNullOrEmpty())
                {
                    if (hash) tooltip = template;
                    using (XmlWriter elementUxml = CreateXml(Path.Combine(documentFolder, elementsDirectoryName), template))
                    {
                        elementUxml.WriteStartElement(prefix, "UXML", uxmlNamespace);
                        WriteStart(defaultFrameNode, elementUxml);
                        defaultFrameNode.children.ForEach(child => WriteRecursively(child, elementUxml, isComponent));
                        WriteEnd(elementUxml);
                        elementUxml.WriteEndElement();
                    }

                    writer.WriteStartElement(prefix, "Template", uxmlNamespace);
                    writer.WriteAttributeString("name", template);
                    writer.WriteAttributeString("src", writer == documentXml ? $"{elementsDirectoryName}\\{template}.uxml" : $"{template}.uxml");
                    writer.WriteEndElement();
                }

                WriteStart(defaultFrameNode, writer);
                if (tooltip.NotNullOrEmpty()) writer.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
                defaultFrameNode.children.ForEach(child => WriteRecursively(child, writer, isComponent));
                WriteEnd(writer);
            }
            void WriteDefaultShapeNode(DefaultShapeNode defaultShapeNode, XmlWriter writer)
            {
                string tooltip = default;
                if (getTemplate(defaultShapeNode) is (var hash, { } template) && template.NotNullOrEmpty())
                {
                    if (hash) tooltip = template;
                    using (XmlWriter xmlWriter = CreateXml(Path.Combine(documentFolder, elementsDirectoryName), template))
                    {
                        xmlWriter.WriteStartElement(prefix, "UXML", uxmlNamespace);
                        WriteStart(defaultShapeNode, xmlWriter);
                        WriteEnd(xmlWriter);
                        xmlWriter.WriteEndElement();
                    }

                    writer.WriteStartElement(prefix, "Template", uxmlNamespace);
                    writer.WriteAttributeString("name", template);
                    writer.WriteAttributeString("src", writer == documentXml ? Path.Combine(elementsDirectoryName, $"{template}.uxml") : $"{template}.uxml");
                    writer.WriteEndElement();
                }

                WriteStart(defaultShapeNode, writer);
                if (tooltip.NotNullOrEmpty()) writer.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
                WriteEnd(writer);
            }
            void WriteComponentSetNode(ComponentSetNode componentSetNode)
            {
                using XmlWriter xmlWriter = CreateXml(Path.Combine(documentFolder, componentsDirectoryName), componentSetNode.name);
                xmlWriter.WriteStartElement(prefix, "UXML", uxmlNamespace);

                xmlWriter.WriteStartElement("Style");
                xmlWriter.WriteAttributeString("src", $"../{documentName}.uss"); // NOTE: Temporarly we are using directory back
                xmlWriter.WriteEndElement();

                WriteStart(componentSetNode, xmlWriter);
                componentSetNode.children.ForEach(child => WriteRecursively(child, xmlWriter, true));
                WriteEnd(xmlWriter);
                xmlWriter.WriteEndElement();
            }
            void WriteInstanceNode(InstanceNode instanceNode, XmlWriter xmlWriter)
            {
                if (files.components.TryGetValue(instanceNode.componentId, out Component component) && !component.remote && 
                    !string.IsNullOrEmpty(component.componentSetId) &&
                    files.componentSets.TryGetValue(component.componentSetId, out Component target) && !target.remote)
                {
                    string componentSetName = target.name;
                    string classList = getClassList(node);

                    xmlWriter.WriteStartElement(prefix, "Instance", uxmlNamespace);
                    xmlWriter.WriteAttributeString("template", componentSetName);
                    xmlWriter.WriteAttributeString("name", instanceNode.name);

                    if (classList.NotNullOrEmpty())
                        uxml.WriteAttributeString("class", classList);

                    xmlWriter.WriteEndElement();
                }
                else
                {
                    Debug.LogWarning(Internals.Extensions.BuildTargetMessage($"Target {nameof(Component)} for node", instanceNode.name, "is not found"));
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

        #region Support Methods
        XmlWriter CreateXml(string folder, string name) => XmlWriter.Create(Path.Combine(folder, $"{name}.uxml"), xmlWriterSettings);

        void WriteStart(BaseNode node, XmlWriter uxml)
        {
            (string prefix, string elementName, string pickingMode) GetElementData(BaseNode node)
            {
                string prefix = UxmlWriter.prefix;
                string elementName = "VisualElement";
                string pickingMode = "Ignore";

                (ElementType elementType, string elementTypeFullName) = getElementType(node);
                if (elementType == ElementType.IElement)
                {
                    prefix = default;
                    elementName = elementTypeFullName;
                    pickingMode = "Position";
                }
                else if (elementType == ElementType.None)
                {
                    switch (node)
                    {
                        case TextNode when node.name.StartsWith("Inputs"):
                            elementName = "TextField";
                            pickingMode = "Position";
                            break;

                        case TextNode:
                            elementName = "Label";
                            break;
                    }

                    switch (node)
                    {
                        case DefaultFrameNode or TextNode or ComponentSetNode:
                            if (node.name.StartsWith("Buttons"))
                            {
                                elementName = "Button";
                                pickingMode = "Position";
                            }

                            if (node.name.StartsWith("Toggles"))
                            {
                                elementName = "Toggle";
                                pickingMode = "Position";
                            }

                            if (node.name.StartsWith("ScrollViews"))
                            {
                                elementName = "ScrollView";
                                pickingMode = "Position";
                            }

                            break;
                    }
                }
                else
                {
                    elementName = elementType.ToString();
                    switch (elementType)
                    {
                        case ElementType.VisualElement or
                             ElementType.BindableElement or
                             ElementType.Box or
                             ElementType.TextElement or
                             ElementType.Label or
                             ElementType.Image:
                            pickingMode = "Ignore";
                            break;

                        default:
                            pickingMode = "Position";
                            break;
                    }
                }

                return (prefix, elementName, pickingMode);
            }

            (string prefix, string elementName, string pickingMode) = GetElementData(node);

            if (prefix.NotNullOrEmpty()) uxml.WriteStartElement(prefix, elementName, uxmlNamespace);
            else uxml.WriteStartElement(elementName);

            uxml.WriteAttributeString("id", node.id);
            uxml.WriteAttributeString("name", node.name);

            string classList = getClassList(node);

            if (classList.NotNullOrEmpty()) uxml.WriteAttributeString("class", classList);
            if (pickingMode != "Position") uxml.WriteAttributeString("picking-mode", pickingMode);
        }
        void WriteEnd(XmlWriter uxml) => uxml.WriteEndElement();
        #endregion
    }
}