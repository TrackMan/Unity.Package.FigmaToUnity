using System;

namespace Figma
{
    using Internals;
    using InternalsExtensions;

    /// <summary>
    /// Represents a color. You can define a color with a #hexadecimal code, the rgb() or rgba() function, or a color keyword (for example, blue or transparent).
    /// </summary>
    internal readonly struct ColorProperty
    {
        #region Fields
        readonly string rgba;
        readonly string rgb;
        readonly string hex;
        readonly string name;
        #endregion

        #region Constructors
        internal ColorProperty(RGBA color, Double? opacity = 1, float alphaMult = 1)
        {
            rgba = $"rgba({(byte)(color.r * 255.0f)},{(byte)(color.g * 255.0f)},{(byte)(color.b * 255.0f)},{(color.a * (opacity ?? alphaMult)).ToString("F2", Const.culture).Replace(".00", string.Empty)})";
            rgb = default;
            hex = default;
            name = default;
        }
        ColorProperty(string value)
        {
            rgba = default;
            rgb = default;
            hex = default;
            name = default;

            if (value.StartsWith("rgba")) rgba = value;
            else if (value.StartsWith("rgb")) rgb = value;
            else if (value.StartsWith('#')) hex = value;
            else name = value;
        }
        #endregion

        #region Operators
        public static implicit operator ColorProperty(Unit _) => new();
        public static implicit operator ColorProperty(RGBA value) => new(value);
        public static implicit operator ColorProperty(string value) => new(value);
        public static implicit operator string(ColorProperty value)
        {
            if (value.rgba.NotNullOrEmpty()) return value.rgba;
            if (value.rgb.NotNullOrEmpty()) return value.rgb;
            if (value.hex.NotNullOrEmpty()) return value.hex;
            if (value.name.NotNullOrEmpty()) return value.name;

            return "initial";
        }
        public override string ToString() => this;
        #endregion
    }
}