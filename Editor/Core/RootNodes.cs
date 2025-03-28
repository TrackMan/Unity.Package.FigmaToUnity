using System;
using System.Collections.Generic;

namespace Figma.Core
{
    using Internals;

    internal class RootNodes
    {
        const int initialCollectionCapacity = 32;

        #region Fields
        readonly List<CanvasNode> canvases = new(initialCollectionCapacity);
        readonly List<ComponentSetNode> componentSets = new(initialCollectionCapacity);
        readonly List<FrameNode> frames = new(initialCollectionCapacity);
        readonly List<(DefaultShapeNode, string hash)> elements = new(initialCollectionCapacity);
        #endregion

        #region Properties
        public IReadOnlyList<CanvasNode> Canvases => canvases;
        public IReadOnlyList<ComponentSetNode> ComponentSets => componentSets;
        public IReadOnlyList<FrameNode> Frames => frames;
        public IReadOnlyList<(DefaultShapeNode node, string hash)> Elements => elements;
        #endregion

        #region Constructors
        public RootNodes(Data data, NodeMetadata nodeMetadata)
        {
            Stack<BaseNode> nodes = new();
            int i = 0;

            nodes.Push(data.document);

            while (nodes.Count > 0)
            {
                if (i++ >= Const.maximumAllowedDepthLimit)
                    throw new InvalidOperationException(Const.maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                switch (node)
                {
                    case CanvasNode canvasNode:
                        canvases.Add(canvasNode);
                        break;

                    case ComponentSetNode componentSetNode:
                        componentSets.Add(componentSetNode);
                        break;

                    case FrameNode frameNode when node.parent is CanvasNode:
                        frames.Add(frameNode);
                        break;

                    case DefaultShapeNode defaultShapeNode when nodeMetadata.GetTemplate(defaultShapeNode) is (_, { } template) && template.NotNullOrEmpty():
                        elements.Add((defaultShapeNode, template));
                        break;
                }

                // ReSharper disable CoVariantArrayConversion
                BaseNode[] children = node switch
                {
                    IChildrenMixin parentNode => parentNode.children,
                    DocumentNode documentNode => documentNode.children,
                    _ => Array.Empty<BaseNode>()
                };
                // ReSharper enable CoVariantArrayConversion
                foreach (BaseNode child in children)
                    nodes.Push(child);
            }
        }
        #endregion
    }
}