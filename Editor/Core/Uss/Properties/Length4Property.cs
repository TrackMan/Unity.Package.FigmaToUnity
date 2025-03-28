using System;
using System.Linq;

namespace Figma.Core.Uss
{
    internal readonly struct Length4Property
    {
        #region Fields
        readonly Unit unit;
        readonly LengthProperty[] properties;
        #endregion

        #region Properties
        internal LengthProperty this[int index] { get => properties[index]; set => properties[index] = value; }
        #endregion

        #region Constructors
        internal Length4Property(Unit unit)
        {
            this.unit = unit;
            properties = new LengthProperty[] { new(unit), new(unit), new(unit), new(unit) };
        }
        internal Length4Property(LengthProperty[] properties)
        {
            unit = Unit.None;
            this.properties = properties;
        }
        #endregion

        #region Operators
        public static implicit operator Length4Property(Unit unit) => new(unit);
        public static implicit operator Length4Property(double? value) => new(new LengthProperty[] { value!.Value });
        public static implicit operator Length4Property(double value) => new(new LengthProperty[] { value });
        public static implicit operator Length4Property(double[] values) => new(values.Select(x => (LengthProperty) x).ToArray());
        public static implicit operator Length4Property(string value) => new(value.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                                                                                  .Select(x => (LengthProperty)x)
                                                                                  .ToArray());
        public static implicit operator string(Length4Property value) => value is { unit: Unit.None, properties: not null }
            ? string.Join(" ", value.properties.Select(p => (string)p))
            : new LengthProperty(value.unit);

        public static Length4Property operator +(Length4Property a) => a;
        public static Length4Property operator -(Length4Property a) => new(a.properties.Select(x => -x).ToArray());
        public static Length4Property operator +(Length4Property a, double b) => new(a.properties.Select(x => x + b).ToArray());
        public static Length4Property operator -(Length4Property a, double b) => new(a.properties.Select(x => x - b).ToArray());
        #endregion
    }
}