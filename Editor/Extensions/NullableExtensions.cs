using System;
using System.Diagnostics;

namespace Figma
{
    using boolean = Boolean;
    using number = Double;

    [DebuggerStepThrough]
    public static class NullableExtensions
    {
        #region Methods
        public static bool IsEmptyOrTrue(this boolean? value)
        {
            return !value.HasValue || value.Value;
        }
        public static bool HasValueAndTrue(this boolean? value)
        {
            return value.HasValue && value.Value;
        }
        public static bool HasValueAndFalse(this boolean? value)
        {
            return value.HasValue && !value.Value;
        }
        public static bool HasPositive(this number? value)
        {
            return value.HasValue && value.Value > 0;
        }
        public static bool IsValue<T>(this T? source, T dest) where T : struct, Enum
        {
            return source.HasValue && source.Value.Equals(dest);
        }
        #endregion
    }
}