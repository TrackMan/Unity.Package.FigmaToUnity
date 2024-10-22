using System.Diagnostics;

namespace Figma
{
    [DebuggerStepThrough]
    public static class Extensions
    {
        #region Methods
        public static T As<T>(this object value) => (T)value;
        public static bool NullOrEmpty(this string value) => string.IsNullOrEmpty(value);
        public static bool NotNullOrEmpty(this string value) => !string.IsNullOrEmpty(value);

        public static string BuildTargetMessage(string message, string target, string end = null) => $"{message} [<color=yellow>{target}</color>] {end ?? string.Empty}";
        #endregion
    }
}