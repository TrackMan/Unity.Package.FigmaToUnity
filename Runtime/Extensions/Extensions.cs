using System.Diagnostics;

namespace Figma.Internals
{
    [DebuggerStepThrough]
    internal static class Extensions
    {
        #region Methods
        internal static T As<T>(this object value) => (T)value;
        internal static bool NullOrEmpty(this string value) => string.IsNullOrEmpty(value);
        internal static bool NotNullOrEmpty(this string value) => !string.IsNullOrEmpty(value);
        internal static bool Invalid(this float value) => float.IsNaN(value) || float.IsInfinity(value);

        internal static string BuildTargetMessage(string message, string target, string end = null) => $"{message} [<color=yellow>{target}</color>] {end ?? string.Empty}";
        #endregion
    }
}