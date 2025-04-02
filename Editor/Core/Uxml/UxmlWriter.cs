using System;
using System.Text;
using System.Xml;
using UnityEngine.UIElements;

namespace Figma.Core.Uxml
{
    using Internals;
    using static Internals.Const;
    using static Internals.PathExtensions;

    internal sealed class UxmlWriter : IDisposable
    {
        const string elementsNamespace = "ui";

        static readonly XmlWriterSettings xmlWriterSettings = new()
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = indentCharacters,
            NewLineOnAttributes = false,
            Encoding = Encoding.UTF8
        };

        #region Fields
        public readonly string filePath;
        public readonly XmlWriter xmlWriter;
        #endregion
        
        #region Constructors
        public UxmlWriter(string directory, string fileName)
        {
            filePath = CombinePath(directory, $"{fileName}.{KnownFormats.uxml}");
            xmlWriter = XmlWriter.Create(filePath, xmlWriterSettings);
            xmlWriter.WriteStartElement(elementsNamespace, "UXML", uxmlNamespace);
        }
        #endregion

        #region Methods
        public void Dispose()
        {
            xmlWriter.WriteEndElement();
            xmlWriter?.Dispose();
        }
        public void StartElement(BaseNode node, string ussClasses, (ElementType type, string typeFullName) elementTypeInfo)
        {
            (string prefix, string elementName, string pickingMode) GetElementData(BaseNode node)
            {
                string prefix = elementsNamespace;
                string elementName = nameof(VisualElement);
                PickingMode pickingMode = PickingMode.Ignore;

                if (elementTypeInfo.type == ElementType.IElement)
                {
                    prefix = null;
                    elementName = elementTypeInfo.typeFullName;
                    pickingMode = PickingMode.Position;
                }
                else if (elementTypeInfo.type == ElementType.None)
                {
                    if (node is not (DefaultFrameNode or TextNode or ComponentSetNode))
                        return (prefix, elementName, pickingMode.ToString());

                    const string inputsPrefix = "Inputs";
                    const string buttonsPrefix = "Buttons";
                    const string togglesPrefix = "Toggles";
                    const string scrollViewsPrefix = "ScrollViews";

                    if (node is TextNode) elementName = node.name.StartsWith(inputsPrefix) ? nameof(TextField) : nameof(Label);

                    if (node.name.StartsWith(buttonsPrefix)) elementName = nameof(Button);
                    else if (node.name.StartsWith(togglesPrefix)) elementName = nameof(Toggle);
                    else if (node.name.StartsWith(scrollViewsPrefix)) elementName = nameof(ScrollView);

                    pickingMode = node.name.StartsWith(buttonsPrefix) ||
                                  node.name.StartsWith(togglesPrefix) ||
                                  node.name.StartsWith(scrollViewsPrefix) ||
                                  (node is TextNode && node.name.StartsWith(inputsPrefix))
                        ? PickingMode.Position
                        : pickingMode;
                }
                else
                {
                    elementName = elementTypeInfo.type.ToString();
                    pickingMode = elementTypeInfo.type is ElementType.VisualElement or
                                                          ElementType.BindableElement or
                                                          ElementType.Box or
                                                          ElementType.TextElement or
                                                          ElementType.Label or
                                                          ElementType.Image
                        ? PickingMode.Ignore
                        : PickingMode.Position;
                }

                return (prefix, elementName, pickingMode.ToString());
            }

            (string prefix, string elementName, string pickingMode) = GetElementData(node);

            if (prefix.NotNullOrEmpty())
                xmlWriter.WriteStartElement(prefix, elementName, uxmlNamespace);
            else
                xmlWriter.WriteStartElement(elementName);

            xmlWriter.WriteAttributeString("name", node.name);
            xmlWriter.WriteAttributeString("id", node.id);

            if (ussClasses.NotNullOrEmpty())
                xmlWriter.WriteAttributeString("class", ussClasses);
            if (pickingMode != PickingMode.Position.ToString())
                xmlWriter.WriteAttributeString("picking-mode", pickingMode);
        }
        public void StartElement(string type, params (string name, string value)[] attributes)
        {
            xmlWriter.WriteStartElement(elementsNamespace, type, uxmlNamespace);

            foreach ((string name, string value) attribute in attributes)
                xmlWriter.WriteAttributeString(attribute.name, attribute.value);
        }
        public void EndElement() => xmlWriter.WriteEndElement();

        public void WriteUssStyleReference(string path)
        {
            StartElement("Style", ("src", path));
            EndElement();
        }
        public void WriteTemplate(string templateName, string templatePath)
        {
            StartElement("Template", ("name", templateName), ("src", templatePath));
            EndElement();
        }
        public void WriteInstance(string instanceName, string templateName, string classList = null)
        {
            StartElement("Instance", ("name", instanceName), ("template", templateName), ("picking-mode", "ignore"));
            if (!string.IsNullOrEmpty(classList))
                xmlWriter.WriteAttributeString("class", classList);
            EndElement();
        }
        #endregion
    }
}