namespace Figma.Core.Uss
{
    using Internals;

    /// <summary>
    /// Represents a whole number.
    /// </summary>
    internal struct IntegerProperty
    {
        #region Fields
        readonly int value;
        #endregion

        #region Constructors
        IntegerProperty(int value) => this.value = value;
        #endregion

        #region Operators
        public static implicit operator IntegerProperty(int? value) => new(value!.Value);
        public static implicit operator IntegerProperty(int value) => new(value);
        public static implicit operator IntegerProperty(string value) => new(int.Parse(value));
        public static implicit operator string(IntegerProperty value) => value.value.ToString(Const.culture);

        public static IntegerProperty operator +(IntegerProperty a) => a;
        public static IntegerProperty operator -(IntegerProperty a) => new(-a.value);
        public static IntegerProperty operator +(IntegerProperty a, int b) => new(a.value + b);
        public static IntegerProperty operator -(IntegerProperty a, int b) => new(a.value - b);
        #endregion
    }
}