using System;

namespace Figma.Core.Uss
{
    using Internals;

    /// <summary>
    /// Represents a distance value.
    /// </summary>
    internal readonly struct LengthProperty
    {
        #region Fields
        readonly Double value;
        readonly Unit unit;
        #endregion

        #region Constructors
        internal LengthProperty(Unit unit)
        {
            value = default;
            this.unit = unit;
        }
        internal LengthProperty(Double value, Unit unit)
        {
            this.value = value;
            this.unit = unit;
        }
        #endregion

        #region Operators
        public static implicit operator LengthProperty(Unit value) => new(default, value);
        public static implicit operator LengthProperty(Double? value) => new(value!.Value, Unit.Pixel);
        public static implicit operator LengthProperty(Double value) => new(value, Unit.Pixel);
        public static implicit operator LengthProperty(string value)
        {
            if (Enum.TryParse(value, true, out Unit unit)) return new LengthProperty(unit);
            if (value.Contains("px")) return new LengthProperty(Double.Parse(value.ToLower().Replace("px", string.Empty), Const.culture), Unit.Pixel);
            if (value.Contains("deg")) return new LengthProperty(Double.Parse(value.ToLower().Replace("deg", string.Empty), Const.culture), Unit.Degrees);
            if (value.Contains('%')) return new LengthProperty(Double.Parse(value.Replace("%", string.Empty), Const.culture), Unit.Percent);

            return default;
        }
        public static implicit operator string(LengthProperty value)
        {
            return value.unit switch
            {
                Unit.Pixel => $"{(int)Math.Round(value.value)}px",
                Unit.Degrees => $"{value.value.ToString("F2", Const.culture).Replace(".00", string.Empty)}deg",
                Unit.Percent => $"{value.value.ToString("F2", Const.culture).Replace(".00", string.Empty)}%",
                Unit.Auto => "auto",
                Unit.None => "none",
                Unit.Initial => "initial",
                Unit.Default => "0px",
                _ => throw new ArgumentException(value)
            };
        }

        public static LengthProperty operator +(LengthProperty a) => a;
        public static LengthProperty operator -(LengthProperty a) => new(-a.value, a.unit);
        public static LengthProperty operator +(LengthProperty a, Double b) => new(a.value + b, a.unit);
        public static LengthProperty operator -(LengthProperty a, Double b) => new(a.value - b, a.unit);

        public static bool operator ==(LengthProperty a, Unit b) => a.unit == b;
        public static bool operator !=(LengthProperty a, Unit b) => a.unit != b;

        public override bool Equals(object obj) => obj is LengthProperty property && value == property.value && unit == property.unit;
        public override int GetHashCode() => HashCode.Combine(value, unit);
        public override string ToString() => this;
        #endregion
    }
}