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
            foreach (IBaseNodeMixin node in data.document.Flatten())
            {
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
            }
        }
        #endregion
    }
}