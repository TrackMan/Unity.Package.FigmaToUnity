using System;
using Figma.Internals;

namespace Figma.Core.Uss
{
    /// <summary>
    /// Represents a duration value from Figma API.
    /// </summary>
    internal readonly struct DurationProperty
    {
        #region Fields
        readonly double value;
        readonly TimeUnit unit;
        #endregion

        #region Constructors
        internal DurationProperty(TimeUnit unit)
        {
            value = default;
            this.unit = unit;
        }
        internal DurationProperty(double value, TimeUnit unit)
        {
            this.value = value;
            this.unit = unit;
        }
        #endregion

        #region Operators
        public static implicit operator DurationProperty(TimeUnit value) => new(default, value);
        public static implicit operator DurationProperty(double? value) => new(value!.Value, TimeUnit.Millisecond);
        public static implicit operator DurationProperty(double value) => new(value, TimeUnit.Millisecond);
        public static implicit operator DurationProperty(string value)
        {
            if (Enum.TryParse(value, true, out TimeUnit unit)) return new DurationProperty(unit);
            if (value.ToLower(Const.culture).Contains("ms")) return new DurationProperty(double.Parse(value.ToLower(Const.culture).Replace("ms", string.Empty), Const.culture), TimeUnit.Millisecond);
            if (value.ToLower(Const.culture).Contains("s")) return new DurationProperty(double.Parse(value.ToLower(Const.culture).Replace("s", string.Empty), Const.culture), TimeUnit.Second);

            return default;
        }
        public static implicit operator string(DurationProperty value)
        {
            return value.unit switch
            {
                TimeUnit.Default => $"0ms",
                TimeUnit.Millisecond => $"{value.value.ToString("F2", Const.culture).Replace(".00", string.Empty)}ms",
                TimeUnit.Second => $"{value.value.ToString("F2", Const.culture).Replace(".00", string.Empty)}s",
                _ => throw new ArgumentException(nameof(value))
            };
        }

        public static DurationProperty operator +(DurationProperty a) => a;
        public static DurationProperty operator -(DurationProperty a) => new(-a.value, a.unit);
        public static DurationProperty operator +(DurationProperty a, double b) => new(a.value + b, a.unit);
        public static DurationProperty operator -(DurationProperty a, double b) => new(a.value - b, a.unit);

        public static bool operator ==(DurationProperty a, TimeUnit b) => a.unit == b;
        public static bool operator !=(DurationProperty a, TimeUnit b) => a.unit != b;

        public override bool Equals(object obj) => obj is DurationProperty property && value == property.value && unit == property.unit;
        public override int GetHashCode() => HashCode.Combine(value, unit);
        public override string ToString() => this;
        #endregion
    }
}