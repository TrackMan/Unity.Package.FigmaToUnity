using System;

namespace Figma
{
    [Flags]
    public enum UxmlDownloadImages
    {
        Everything = 0,
        Nothing = 1,
        ImageFills = 1 << 1,
        RenderAsPng = 1 << 2,
        RenderAsSvg = 1 << 3,
        ByElements = 1 << 4
    }

    public enum UxmlElementTypeIdentification
    {
        ByName,
        ByElementType
    }

    public enum ElementDownloadImage
    {
        Auto,
        Download,
        Ignore
    }

    [Flags]
    public enum CopyStyleMask
    {
        None = 0,
        Text = 1,
        Position = 1 << 1,
        Size = 1 << 2,
        Flex = 1 << 3,
        Display = 1 << 4,
        Padding = 1 << 5,
        Margins = 1 << 6,
        Borders = 1 << 7,
        Slicing = 1 << 8,
        Font = 1 << 9,
        All = Text | Position | Size | Flex | Display | Padding | Margins | Borders | Slicing | Font
    }
}