using System;

namespace Figma
{
    using Internals;
    
    /// <summary>
    /// Represents either an integer or a number with a fractional component.
    /// </summary>
    internal struct NumberProperty
    {
        #region Fields
        readonly Double value;
        #endregion

        #region Constructors
        NumberProperty(Double value) => this.value = value;
        #endregion

        #region Operators
        public static implicit operator NumberProperty(Double? value) => new(value!.Value);
        public static implicit operator NumberProperty(Double value) => new(value);
        public static implicit operator NumberProperty(string value) => new(Double.Parse(value, Const.culture));
        public static implicit operator string(NumberProperty value) => value.value.ToString("F2", Const.culture).Replace(".00", string.Empty);

        public static NumberProperty operator +(NumberProperty a) => a;
        public static NumberProperty operator -(NumberProperty a) => new(-a.value);
        public static NumberProperty operator +(NumberProperty a, Double b) => new(a.value + b);
        public static NumberProperty operator -(NumberProperty a, Double b) => new(a.value - b);
        #endregion
    }
}
