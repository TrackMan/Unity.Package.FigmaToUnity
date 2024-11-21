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
        public static implicit operator Length4Property(Double? value) => new(new LengthProperty[] { value!.Value });
        public static implicit operator Length4Property(Double value) => new(new LengthProperty[] { value });
        public static implicit operator Length4Property(Double[] values)
        {
            LengthProperty[] properties = new LengthProperty[values.Length];
            for (int i = 0; i < values.Length; i++) properties[i] = values[i];
            return new Length4Property(properties);
        }
        public static implicit operator Length4Property(string value)
        {
            string[] values = value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            LengthProperty[] properties = new LengthProperty[values.Length];
            for (int i = 0; i < values.Length; i++) properties[i] = values[i];
            return new Length4Property(properties);
        }
        public static implicit operator string(Length4Property value)
        {
            if (value is { unit: Unit.None, properties: not null })
            {
                string[] values = new string[value.properties.Length];
                for (int i = 0; i < values.Length; i++) values[i] = value.properties[i];
                return string.Join(" ", values);
            }

            return new LengthProperty(value.unit);
        }

        public static Length4Property operator +(Length4Property a) => a;
        public static Length4Property operator -(Length4Property a) => new(a.properties.Select(x => -x).ToArray());
        public static Length4Property operator +(Length4Property a, Double b) => new(a.properties.Select(x => x + b).ToArray());
        public static Length4Property operator -(Length4Property a, Double b) => new(a.properties.Select(x => x - b).ToArray());
        #endregion
    }
}