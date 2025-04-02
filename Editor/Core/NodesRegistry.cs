using System.Collections.Generic;
using System.Linq;

namespace Figma.Core
{
    using Internals;

    internal sealed class NodesRegistry
    {
        #region Fields
        internal List<string> MissingComponents { get; } = new(Const.initialCollectionCapacity);
        internal List<IBaseNodeMixin> ImageFills { get; } = new(Const.initialCollectionCapacity);
        internal List<IBaseNodeMixin> Pngs { get; } = new(Const.initialCollectionCapacity);
        internal List<IBaseNodeMixin> Svgs { get; } = new(Const.initialCollectionCapacity);
        internal Dictionary<string, GradientPaint> Gradients { get; } = new(Const.initialCollectionCapacity);
        #endregion

        public NodesRegistry(Data data, NodeMetadata nodeMetadata)
        {
            List<IBaseNodeMixin> nodes = data.document.children.SelectMany(canvas => canvas.Flatten(node => node.IsVisible() &&
                                                                                                            nodeMetadata.EnabledInHierarchy(node) &&
                                                                                                            node.parent is not BooleanOperationNode)).ToList();

            MissingComponents.AddRange(nodes.OfType<InstanceNode>()
                                            .Where(instance => data.document.Flatten().Any(node => node.id == instance.componentId))
                                            .Select(instance => instance.componentId));

            Pngs.AddRange(nodes.Where(node => node is not BooleanOperationNode && node.IsSvgNode() && node.HasImage()));
            Svgs.AddRange(nodes.Where(node => node.IsSvgNode() && !node.HasImage()));
            ImageFills.AddRange(nodes.Where(node => node is not BooleanOperationNode && !node.IsSvgNode() && node.HasImage()));

            foreach (GradientPaint gradient in nodes.OfType<IGeometryMixin>()
                                                    .Where(x => x is not BooleanOperationNode)
                                                    .SelectMany(x => x.fills.OfType<GradientPaint>()))
                Gradients.TryAdd(gradient.GetHash(), gradient);
        }
    }
}