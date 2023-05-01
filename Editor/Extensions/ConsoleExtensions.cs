using System;
using UnityEngine;

namespace Figma
{
    public static class ConsoleExtensions
    {
        #region Methods
        public static void Clear()
        {
            Debug.ClearDeveloperConsole();
            Type.GetType("UnityEditor.LogEntries,UnityEditor").GetMethod("Clear").Invoke(default, default);
        }
        #endregion
    }
}