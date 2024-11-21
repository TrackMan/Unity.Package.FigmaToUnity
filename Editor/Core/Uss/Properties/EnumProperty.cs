using System;
using System.Text.RegularExpressions;

namespace Figma.Core.Uss
{
#pragma warning disable CS0660, CS0661
    internal struct EnumProperty<T> where T : struct, Enum
#pragma warning restore CS0660, CS0661
    {
        // ReSharper disable StaticMemberInGenericType
        static readonly Regex enumParserRegexString = new("(?<name>([a-z]+\\-?))", RegexOptions.Compiled);
        static readonly Regex enumParserRegexValue = new("(?<name>([A-Z][a-z]+)?)", RegexOptions.Compiled);
        // ReSharper restore StaticMemberInGenericType

        #region Fields
        T value;
        readonly Unit unit;
        #endregion

        #region Constructors
        EnumProperty(T value)
        {
            this.value = value;
            unit = Unit.None;
        }
        EnumProperty(Unit unit)
        {
            value = default;
            this.unit = unit;
        }
        #endregion

        #region Operators
        public static implicit operator EnumProperty<T>(Unit unit) => new(unit);
        public static implicit operator EnumProperty<T>(T value) => new(value);
        public static implicit operator EnumProperty<T>(string value) => Enum.TryParse(enumParserRegexString.Replace(value, "${name}").Replace("-", string.Empty), true, out T result) ? new EnumProperty<T>(result) : default;
        public static implicit operator string(EnumProperty<T> value) => value.unit == Unit.None ? enumParserRegexValue.Replace(value.value.ToString(), "${name}-").ToLower().TrimEnd('-') : "initial";

        public static bool operator ==(EnumProperty<T> a, T b) => a.value.Equals(b);
        public static bool operator !=(EnumProperty<T> a, T b) => !a.value.Equals(b);

        public override string ToString() => this;
        #endregion
    }
}