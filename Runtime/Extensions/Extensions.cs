using System;
using System.Collections.Generic;
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
        internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T element in enumerable)
                action(element);
        }
#if UNITY_EDITOR
        internal static string BuildTargetMessage(string message, string target, string end = null) => $"{message} [<color={(UnityEditor.EditorGUIUtility.isProSkin ? "yellow" : "aaaa00")}>{target}</color>] {end ?? string.Empty}";
#else
        internal static string BuildTargetMessage(string message, string target, string end = null) => $"{message} [{target}] {end ?? string.Empty}";
#endif
        #endregion
    }
}