using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Figma
{
    using Internals;

    [DebuggerStepThrough]
    public static class NodeExtensions
    {
        #region Methods
        public static void SetParentRecursively(this BaseNode node)
        {
            switch (node)
            {
                case DocumentNode document:
                    foreach (CanvasNode canvas in document.children)
                    {
                        canvas.parent = node;
                        SetParentRecursively(canvas);
                    }

                    break;

                case ChildrenMixin { children: not null } children:
                    foreach (SceneNode child in children.children)
                    {
                        child.parent = node;
                        SetParentRecursively(child);
                    }

                    break;
            }
        }
        public static string GetHash(this GradientPaint gradient)
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
        #endregion
    }
}