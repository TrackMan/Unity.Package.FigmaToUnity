namespace Figma.Core.Uss
{
    /// <summary>
    /// Represents either an integer or a number with a fractional component.
    /// </summary>
    internal struct NumberProperty
    {
        #region Fields
        readonly double value;
        #endregion

        #region Constructors
        NumberProperty(double value) => this.value = value;
        #endregion

        #region Operators
        public static implicit operator NumberProperty(double? value) => new(value!.Value);
        public static implicit operator NumberProperty(double value) => new(value);
        public static implicit operator NumberProperty(string value) => new(double.Parse(value, Const.Culture));
        public static implicit operator string(NumberProperty value) => value.value.ToString("F2", Const.Culture).Replace(".00", string.Empty);

        public static NumberProperty operator +(NumberProperty a) => a;
        public static NumberProperty operator -(NumberProperty a) => new(-a.value);
        public static NumberProperty operator +(NumberProperty a, double b) => new(a.value + b);
        public static NumberProperty operator -(NumberProperty a, double b) => new(a.value - b);
        #endregion
    }
}