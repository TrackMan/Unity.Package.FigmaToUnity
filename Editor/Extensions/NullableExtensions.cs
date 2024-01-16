using System;
using System.Diagnostics;

namespace Figma
{
    [DebuggerStepThrough]
    public static class NullableExtensions
    {
        #region Methods
        public static bool IsEmptyOrTrue(this bool? value) => !value.HasValue || value.Value;
        public static bool HasValueAndTrue(this bool? value) => value == true;
        public static bool HasValueAndFalse(this bool? value) => value == false;
        public static bool HasPositive(this double? value) => value is > 0;
        public static bool IsValue<T>(this T? source, T dest) where T : struct, Enum => source.HasValue && source.Value.Equals(dest);
        #endregion
    }
}