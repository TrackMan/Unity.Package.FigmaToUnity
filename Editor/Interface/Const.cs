namespace Figma.Internals
{
    public static class Const
    {
        // Http API target
        public const string api = "https://api.figma.com/v1";

        // Directories
        public const string fontsDirectoryName = "Fonts";
        public const string framesDirectoryName = "Frames";
        public const string imagesDirectoryName = nameof(Images);
        public const string elementsDirectoryName = "Elements";
        public const string componentsDirectoryName = "Components";

        // Uxml
        public const string uxmlNamespace = "UnityEngine.UIElements";

        // Fallback written data
        /// <summary>
        /// Magenta colored image with resolution 2x2.
        /// </summary>
        public static readonly byte[] InvalidPng =
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            // IHDR chunk
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02,
            0x08, 0x06, 0x00, 0x00, 0x00, 0xF4, 0x78, 0x5A, 0xEE,
            // IDAT chunk (compressed image data)
            0x00, 0x00, 0x00, 0x11, 0x49, 0x44, 0x41, 0x54,
            0x78, 0x9C, 0x63, 0xF8, 0xCF, 0xC0, 0xC0, 0xC0,
            0xF0, 0x0F, 0x04, 0x00, 0x04, 0x00, 0x01, 0xF3,
            0x0D, 0x0E, 0x43,
            // IEND chunk
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0xAE, 0x42, 0x60, 0x82
        };
        /// <summary>
        /// Warning sign in SVG.
        /// </summary>
        public const string InvalidSvg = @"<svg width=""128"" height=""128"" viewBox=""0 0 128 128"" xmlns=""http://www.w3.org/2000/svg"">
	<defs>
		<linearGradient id=""gradYellow"" x1=""0%"" y1=""0%"" x2=""0%"" y2=""100%"">
			<stop offset=""0%"" stop-color=""#FFEA00"" />
			<stop offset=""100%"" stop-color=""#FFC400"" />
		</linearGradient>

		<filter id=""shadow"" x=""-20%"" y=""-20%"" width=""140%"" height=""140%"">
			<feDropShadow dx=""4"" dy=""4"" stdDeviation=""4"" flood-color=""rgba(0,0,0,0.5)"" />
		</filter>
	</defs>
	<polygon points=""64,8 120,120 8,120""
		fill=""url(#gradYellow)"" stroke=""#FFF"" stroke-width=""4""
		filter=""url(#shadow)"" />
	<line x1=""64"" y1=""40"" x2=""64"" y2=""80""
		stroke=""#FFF"" stroke-width=""6"" stroke-linecap=""round"" />
	<circle cx=""64"" cy=""100"" r=""6"" fill=""#FFF"" />
</svg>";
    }

    public static class KnownFormats
    {
        public const string png = nameof(png);
        public const string svg = nameof(svg);
        public const string ttf = nameof(ttf);
        public const string otf = nameof(otf);
        public const string asset = nameof(asset);
        public const string uxml = nameof(uxml);
        public const string uss = nameof(uss);
        public const string json = nameof(json);
        public const string meta = nameof(meta);
    }
}