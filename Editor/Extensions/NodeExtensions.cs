using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Figma
{
    using Internals;

    [DebuggerStepThrough]
    internal static class NodeExtensions
    {
        #region Methods
        internal static IEnumerable<IBaseNodeMixin> Flatten(this IBaseNodeMixin root, Func<IBaseNodeMixin, bool> filter = null)
        {
            Stack<IBaseNodeMixin> nodes = new();
            nodes.Push(root);

            if (root is DocumentNode documentNode)
                foreach (CanvasNode canvasNode in documentNode.children)
                    nodes.Push(canvasNode);

            for (int depth = 0; depth < Const.maximumAllowedDepthLimit; depth++)
            {
                if (nodes.Count == 0)
                    yield break;

                IBaseNodeMixin node = nodes.Pop();

                if (filter != null && !filter(node))
                    continue;

                yield return node;

                if (node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }

            throw new InvalidOperationException(Const.maximumDepthLimitReachedExceptionMessage);
        }
        internal static bool IsRootNode(this IBaseNodeMixin node) => node is DocumentNode or CanvasNode or ComponentNode || node.parent is CanvasNode or ComponentNode;
        internal static bool IsSvgNode(this IBaseNodeMixin node) => node is LineNode or EllipseNode or RegularPolygonNode or StarNode or VectorNode ||
                                                                    (node is BooleanOperationNode && node.Flatten().Any(x => x is not BooleanOperationNode && IsVisible(x) && IsSvgNode(x)));
        internal static bool IsVisible(this IBaseNodeMixin node) => (node is not ISceneNodeMixin scene || scene.visible) && (node.parent == null || node.parent.IsVisible());
        internal static bool HasImage(this IBaseNodeMixin node) => node is IGeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);

        internal static void SetParent(this BaseNode node)
        {
            switch (node)
            {
                case DocumentNode document:
                    foreach (CanvasNode canvas in document.children)
                    {
                        canvas.parent = node;
                        SetParent(canvas);
                    }

                    break;

                case IChildrenMixin { children: not null } children:
                    foreach (SceneNode child in children.children)
                    {
                        child.parent = node;
                        SetParent(child);
                    }

                    break;
            }
        }
        internal static string GetHash(this GradientPaint gradient)
        {
            using SHA1CryptoServiceProvider sha1 = new();
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write((int)gradient.type);
            foreach (ColorStop stop in gradient.gradientStops)
            {
                writer.Write(stop.position);
                writer.Write(stop.color.r);
                writer.Write(stop.color.g);
                writer.Write(stop.color.b);
                writer.Write(stop.color.a);
            }

            foreach (Vector position in gradient.gradientHandlePositions)
            {
                writer.Write(position.x);
                writer.Write(position.y);
            }

            byte[] bytes = stream.ToArray();
            byte[] hashBytes = sha1.ComputeHash(bytes);

            StringBuilder hashBuilder = new();
            foreach (byte @byte in hashBytes)
                hashBuilder.Append(@byte.ToString("x2"));

            return hashBuilder.ToString();
        }
        internal static string GetFullPath(this IBaseNodeMixin node)
        {
            string result = string.Empty;

            for (int depth = 0; depth < Const.maximumAllowedDepthLimit; depth++)
            {
                if (node == null)
                    return result;

                result = string.IsNullOrEmpty(result) ? node.name : node.name + PathExtensions.pathSeparator + result;
                node = node.parent;
            }

            throw new InvalidOperationException(Const.maximumDepthLimitReachedExceptionMessage);
        }
        #endregion
    }
}