using System;
using System.IO;
using System.Xml;
using UnityEngine.UIElements;

namespace Figma.Core.Uxml
{
    using Internals;
    using static Internals.Const;

    internal sealed class UxmlWriter : IDisposable
    {
        internal class UxmlElementScope : IDisposable
        {
            #region Fields
            UxmlWriter owner;
            #endregion

            #region Constructor
            public UxmlElementScope(UxmlWriter owner, BaseNode node, string ussStyles, (ElementType type, string typeFullName) elementTypeInfo)
            {
                this.owner = owner;
                this.owner.StartElement(node, ussStyles, elementTypeInfo);
            }
            #endregion

            #region Methods
            public void Dispose() => owner.EndElement();
            #endregion
        }

        const string prefix = "unity";

        static readonly XmlWriterSettings xmlWriterSettings = new() { OmitXmlDeclaration = true, Indent = true, IndentChars = indentCharacters, NewLineOnAttributes = false };

        #region Constructors
        public UxmlWriter(string directory, string fileName)
        {
            XmlWriter = XmlWriter.Create(Path.Combine(directory, $"{fileName}.uxml"), xmlWriterSettings);
            XmlWriter.WriteStartElement(prefix, "UXML", uxmlNamespace);
        }
        #endregion

        #region Properties
        public XmlWriter XmlWriter { get; }
        #endregion

        #region Methods
        public void Dispose()
        {
            XmlWriter.WriteEndElement();
            XmlWriter?.Dispose();
        }
        public void StartElement(BaseNode node, string ussClasses, (ElementType type, string typeFullName) elementTypeInfo)
        {
            (string prefix, string elementName, string pickingMode) GetElementData(BaseNode node)
            {
                string prefix = UxmlWriter.prefix;
                string elementName = nameof(VisualElement);
                PickingMode pickingMode = PickingMode.Ignore;

                if (elementTypeInfo.type == ElementType.IElement)
                {
                    prefix = default;
                    elementName = elementTypeInfo.typeFullName;
                    pickingMode = PickingMode.Position;
                }
                else if (elementTypeInfo.type == ElementType.None)
                {
                    if (node is not (DefaultFrameNode or TextNode or ComponentSetNode)) 
                        return (prefix, elementName, pickingMode.ToString());
                    
                    const string buttonsPrefix = "Buttons";
                    const string inputsPrefix = "Inputs";
                    const string togglesPrefix = "Toggles";
                    const string scrollViewsPrefix = "ScrollViews";
                        
                    if (node is TextNode) elementName = node.name.StartsWith(inputsPrefix) ? nameof(TextField) : nameof(Label);

                    if (node.name.StartsWith(buttonsPrefix)) elementName = nameof(Button);
                    else if (node.name.StartsWith(togglesPrefix)) elementName = nameof(Toggle);
                    else if (node.name.StartsWith(scrollViewsPrefix)) elementName = nameof(ScrollView);

                    pickingMode = node.name.StartsWith(buttonsPrefix) || 
                                  node.name.StartsWith(togglesPrefix) || 
                                  node.name.StartsWith(scrollViewsPrefix) || 
                                  (node is TextNode && node.name.StartsWith(inputsPrefix)) ? PickingMode.Position : pickingMode;
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
                        ? PickingMode.Ignore : PickingMode.Position;
                }

                return (prefix, elementName, pickingMode.ToString());
            }

            (string prefix, string elementName, string pickingMode) = GetElementData(node);

            if (prefix.NotNullOrEmpty()) XmlWriter.WriteStartElement(prefix, elementName, uxmlNamespace);
            else XmlWriter.WriteStartElement(elementName);

            XmlWriter.WriteAttributeString("id", node.id);
            XmlWriter.WriteAttributeString("name", node.name);

            if (ussClasses.NotNullOrEmpty()) XmlWriter.WriteAttributeString("class", ussClasses);
            if (pickingMode != PickingMode.Position.ToString()) XmlWriter.WriteAttributeString("picking-mode", pickingMode);
        }
        public void EndElement() => XmlWriter.WriteEndElement();
        public UxmlElementScope ElementScope(BaseNode node, string ussClasses, (ElementType type, string typeFullName) elementTypeInfo) => new(this, node, ussClasses, elementTypeInfo);

        public void WriteUssStyleReference(string path)
        {
            XmlWriter.WriteStartElement("Style");
            XmlWriter.WriteAttributeString("src", path);
            XmlWriter.WriteEndElement();
        }
        public void WriteTemplate(string templateName, string templatePath)
        {
            XmlWriter.WriteStartElement(prefix, "Template", uxmlNamespace);
            XmlWriter.WriteAttributeString("src", templatePath);
            XmlWriter.WriteAttributeString("name", templateName);
            XmlWriter.WriteEndElement();
        }
        public void WriteInstance(string templateName, string instanceName, string classList)
        {
            XmlWriter.WriteStartElement(prefix, "Instance", uxmlNamespace);
            XmlWriter.WriteAttributeString("template", templateName);
            XmlWriter.WriteAttributeString("name", instanceName);

            if (classList.NotNullOrEmpty())
                XmlWriter.WriteAttributeString("class", classList);

            XmlWriter.WriteEndElement();
        }
        #endregion
    }
}