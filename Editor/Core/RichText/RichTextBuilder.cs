using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Figma.Core.RichText
{
    using Internals;

    internal sealed class TextBuilder
    {
        #region Container
        enum TagType
        {
            Bold,
            Italic,
            Underline,
            Strikethrough,
            Color,
            FontSize,
            FontWeight,
            Indent,
        }

        class Tag
        {
            #region Fields
            readonly string tag;
            readonly StringBuilder stringBuilder;

            string value;
            bool active;
            #endregion

            #region Constructor
            public Tag(StringBuilder stringBuilder, string tag)
            {
                this.tag = tag;
                this.stringBuilder = stringBuilder;
            }
            #endregion

            #region Methods
            public void Set(bool required)
            {
                if (!active && required)
                    Open();
                if (active && !required)
                    Close();

                if (!required)
                    value = null;
            }
            public void Set(bool required, string value)
            {
                switch (required)
                {
                    case true when active && this.value != value && this.value != null:
                        Close();
                        this.value = value;
                        Open();
                        return;

                    case false when !active:
                        return;

                    default:
                        this.value = value;
                        Set(required);
                        break;
                }
            }

            void Open()
            {
                stringBuilder.Append(string.IsNullOrEmpty(value) ? $"<{tag}>" : $"<{tag}={value}>");
                active = true;
            }
            void Close()
            {
                stringBuilder.Append($"</{tag}>");
                active = false;
            }
            #endregion
        }
        #endregion

        #region Fields
        readonly StringBuilder stringBuilder = new();
        readonly Dictionary<TagType, Tag> tags;
        readonly TextNode node;
        #endregion

        #region Constructors
        public TextBuilder(TextNode textNode)
        {
            tags = new Dictionary<TagType, Tag>
            {
                { TagType.Bold, new Tag(stringBuilder, "b") },
                { TagType.Italic, new Tag(stringBuilder, "i") },
                { TagType.Underline, new Tag(stringBuilder, "u") },
                { TagType.Strikethrough, new Tag(stringBuilder, "strikethrough") },
                { TagType.Color, new Tag(stringBuilder, "color") },
                { TagType.FontSize, new Tag(stringBuilder, "size") },
                { TagType.FontWeight, new Tag(stringBuilder, "font-weight") },
                { TagType.Indent, new Tag(stringBuilder, "indent") },
            };
            node = textNode;
        }
        #endregion

        #region Methods
        public string Build()
        {
            string text = node.characters;
            TextNode.Style baseStyle = node.style;
            int[] charOverrides = node.characterStyleOverrides ?? Array.Empty<int>();
            Dictionary<int, TextNode.Style> styleTable = node.styleOverrideTable ?? new Dictionary<int, TextNode.Style>();
            LineType[] lineTypes = node.lineTypes ?? Array.Empty<LineType>();
            int[] lineIndents = node.lineIndentations ?? Array.Empty<int>();

            int textLength = text.Length;
            if (charOverrides.Length < textLength)
            {
                Array.Resize(ref charOverrides, textLength);
                for (int j = node.characterStyleOverrides.Length; j < textLength; j++)
                    charOverrides[j] = 0;
            }

            int listLineIndex = 0;

            for (int i = 0, line = 0; i < textLength; i++)
            {
                char ch = text[i];

                if (i == 0 || text[i - 1] == '\n')
                {
                    if (i == 0)
                        line = 0;
                    else
                        line++;

                    int indentLevel = line < lineIndents.Length ? lineIndents[line] : 0;
                    LineType lineType = line < lineTypes.Length ? lineTypes[line] : LineType.NONE;

                    for (int s = 0; s < indentLevel; s++)
                        tags[TagType.Indent].Set(lineType != LineType.NONE, (10 * indentLevel).ToString());

                    if (lineType is LineType.UNORDERED or LineType.NONE) listLineIndex = 0;
                    if (lineType is LineType.ORDERED) stringBuilder.Append($"{++listLineIndex}. ");
                    else if (lineType is LineType.UNORDERED) stringBuilder.Append("• ");
                }

                if (ch == '\n')
                {
                    foreach (Tag tag in tags.Values)
                        tag.Set(false);

                    stringBuilder.Append('\n');
                    continue;
                }

                int styleOverrideId = i < charOverrides.Length ? charOverrides[i] : 0;
                TextNode.Style charStyle = styleTable.GetValueOrDefault(styleOverrideId, baseStyle);

                if (charStyle != null)
                {
                    tags[TagType.Bold].Set(charStyle.fontWeight >= (int)FontWeight.Bold);
                    tags[TagType.Italic].Set(charStyle.italic is true);
                    tags[TagType.Underline].Set(charStyle.textDecoration is TextDecoration.UNDERLINE);
                    tags[TagType.Strikethrough].Set( charStyle.textDecoration is TextDecoration.STRIKETHROUGH);

                    SolidPaint paint = charStyle.fills?.OfType<SolidPaint>().FirstOrDefault();
                    tags[TagType.Color].Set(paint != null, paint == null ? null : "#" + ColorUtility.ToHtmlStringRGBA((Color)paint.color));
                    tags[TagType.FontWeight].Set(charStyle.fontWeight.HasValue, charStyle.fontWeight.ToString());
                    tags[TagType.FontSize].Set(charStyle.fontSize.HasValue, charStyle.fontSize.ToString());
                }

                stringBuilder.Append(ch);
            }

            foreach (Tag tag in tags.Values)
                tag.Set(false);

            return stringBuilder.ToString();
        }
        #endregion
    }
}