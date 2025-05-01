using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Figma.Core.Assets
{
    using Internals;
    using static Internals.PathExtensions;

    internal sealed class CachedAssets
    {
        #region Fields
        readonly string targetFilePath;
        #endregion

        #region Constructor
        internal CachedAssets(string directory, string name) => targetFilePath = CombinePath(directory, $"{nameof(CachedAssets)}-{name}.{KnownFormats.json}");
        #endregion

        #region Properties
        internal Dictionary<string, string> Map { get; private set; }
        #endregion

        #region Operators
        internal string this[string key]
        {
            get => Map.GetValueOrDefault(key, key);
            set => Map[key] = value;
        }
        #endregion

        #region Methods
        internal async Task LoadAsync(CancellationToken token) => Map = File.Exists(targetFilePath) ? JsonUtility.FromJson<Dictionary<string, string>>(await File.ReadAllTextAsync(targetFilePath, token)) : new();
        internal async Task SaveAsync() => await File.WriteAllTextAsync(targetFilePath, JsonUtility.ToJson(Map, prettyPrint: true));
        #endregion
    }
}