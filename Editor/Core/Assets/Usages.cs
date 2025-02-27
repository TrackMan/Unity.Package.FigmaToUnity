using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Figma.Core.Assets
{
    using Internals;

    internal sealed class Usages
    {
        const int initialCollectionCapacity = 64;

        #region Fields
        internal ConcurrentBag<string> Files { get; } = new();
        
        internal List<string> MissingComponents { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> ImageFillNodes { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> PngNodes { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> SvgNodes { get; } = new(initialCollectionCapacity);
        internal Dictionary<string, GradientPaint> Gradients { get; } = new(initialCollectionCapacity);
        #endregion

        #region Methods
        public void RecordFiles(params string[] items) => items.ForEach(item => Files.Add(item));
        #endregion
    }
}