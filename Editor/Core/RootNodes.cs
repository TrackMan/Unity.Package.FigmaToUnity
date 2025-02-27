using System.Collections.Generic;

namespace Figma.Core
{
    using Internals;
    
    internal class RootNodes
    {
        const int initialCollectionCapacity = 32;

        #region Fields
        readonly NodeMetadata nodeMetadata;
        
        List<CanvasNode> canvases = new(initialCollectionCapacity);
        List<ComponentSetNode> componentSets = new(initialCollectionCapacity);
        List<FrameNode> frames = new(initialCollectionCapacity);
        List<(DefaultShapeNode, string hash)> elements = new(initialCollectionCapacity);
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
            this.nodeMetadata = nodeMetadata;

            foreach (CanvasNode child in data.document.children) 
                FindItemsRecursively(child);
        }
        #endregion

        #region Methods
        void FindItemsRecursively(BaseNode node)
        {
            if (node is CanvasNode canvasNode) canvases.Add(canvasNode);
            if (node is ComponentSetNode componentSetNode) componentSets.Add(componentSetNode);
            if (node is FrameNode frameNode && node.parent is CanvasNode) frames.Add(frameNode);
            if (node is DefaultShapeNode defaultShapeNode && nodeMetadata.GetTemplate(defaultShapeNode) is (_, { } template) && template.NotNullOrEmpty()) elements.Add((defaultShapeNode, template));

            if (node is IChildrenMixin parentNode)
                foreach (SceneNode child in parentNode.children)
                    FindItemsRecursively(child);
        }
        #endregion
    }
}