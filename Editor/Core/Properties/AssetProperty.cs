using System;

namespace Figma
{
    /// <summary>
    /// Represents an asset in a Resources folder or represents an asset specified by a path, it can be expressed as either a relative path or an absolute path.
    /// </summary>
    internal struct AssetProperty
    {
        #region Fields
        readonly string url;
        readonly string resource;
        readonly Unit unit;
        #endregion

        #region Constructors
        AssetProperty(Unit unit)
        {
            url = default;
            resource = default;
            this.unit = unit;
        }
        AssetProperty(string value)
        {
            url = default;
            resource = default;
            unit = default;

            if (value.StartsWith("url")) url = value;
            else if (value.StartsWith("resource")) resource = value;
            else throw new NotSupportedException();
        }
        #endregion

        #region Operators
        public static implicit operator AssetProperty(Unit value) => new(value);
        public static implicit operator AssetProperty(string value) => Enum.TryParse(value, true, out Unit unit) ? new AssetProperty(unit) : new AssetProperty(value);
        public static implicit operator string(AssetProperty value)
        {
            if (value.url.NotNullOrEmpty()) return value.url;
            if (value.resource.NotNullOrEmpty()) return value.resource;

            return value.unit switch
            {
                Unit.None => "none",
                Unit.Initial => "initial",
                _ => throw new ArgumentException(value)
            };
        }
        #endregion
    }
}