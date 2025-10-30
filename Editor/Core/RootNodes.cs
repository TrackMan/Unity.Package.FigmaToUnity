using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Figma.Core
{
    using Internals;
    using Attributes;

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

                    case DefaultShapeNode defaultShapeNode when nodeMetadata.GetTemplate(defaultShapeNode) is (var isHash, { } template) && template.NotNullOrEmpty():
                        if (!isHash && elements.Any(x => x.hash == template))
                        {
                            Debug.LogWarning($"Duplicate hash was found: {template}. This might happen when [{nameof(QueryAttribute)}] is inherited in multiple classes. " +
                                             "This could also happen when you have a template with the same name. In order to fix that in that case, please use \"Hash = true\" parameter.");
                            break;
                        }

                        elements.Add((defaultShapeNode, template));
                        break;
                }
            }
        }
        #endregion
    }
}