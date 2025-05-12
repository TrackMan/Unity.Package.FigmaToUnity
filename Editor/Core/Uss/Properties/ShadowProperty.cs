using System.Text.RegularExpressions;

namespace Figma.Core.Uss
{
    internal struct ShadowProperty
    {
        static readonly Regex regex = new(@"(?<offsetHorizontal>\d+[px]*)\s+(?<offsetVertical>\d+[px]*)\s+(?<blurRadius>\d+[px]*)\s+(?<color>(rgba\([\d,\.\s]+\))|#\w{2,8}|[^#][\w-]+)");

        #region Fields
        readonly LengthProperty offsetHorizontal;
        readonly LengthProperty offsetVertical;
        readonly LengthProperty blurRadius;
        readonly ColorProperty color;
        #endregion

        #region Constructors
        internal ShadowProperty(LengthProperty offsetHorizontal, LengthProperty offsetVertical, LengthProperty blurRadius, ColorProperty color)
        {
            this.offsetHorizontal = offsetHorizontal;
            this.offsetVertical = offsetVertical;
            this.blurRadius = blurRadius;
            this.color = color;
        }
        ShadowProperty(string value)
        {
            Match match = regex.Match(value);
            offsetHorizontal = match.Groups[nameof(offsetHorizontal)].Value;
            offsetVertical = match.Groups[nameof(offsetVertical)].Value;
            blurRadius = match.Groups[nameof(blurRadius)].Value;
            color = match.Groups[nameof(color)].Value;
        }
        #endregion

        #region Operators
        public static implicit operator ShadowProperty(string value) => new(value);
        public static implicit operator string(ShadowProperty value) => $"{value.offsetHorizontal} {value.offsetVertical} {value.blurRadius} {value.color}";
        #endregion
    }
}