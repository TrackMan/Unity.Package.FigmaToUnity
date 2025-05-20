using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Figma.Core.Assets
{
    using Internals;
    using static Const;

    internal class GradientWriter : IDisposable
    {
        #region Fields
        static readonly XmlWriterSettings xmlWriterSettings = new()
        {
            Indent = true,
            NewLineOnAttributes = true,
            NewLineChars = Environment.NewLine,
            IndentChars = indentCharacters,
            Async = true
        };

        readonly XmlWriter writer;
        #endregion

        #region Constructors
        public GradientWriter(string xmlPath) => writer = XmlWriter.Create(xmlPath, xmlWriterSettings);
        #endregion

        #region Methods
        public async Task WriteAsync(GradientPaint gradient, CancellationToken token)
        {
            writer.WriteStartElement(KnownFormats.svg);
            writer.WriteStartElement("defs");
            switch (gradient.type)
            {
                case PaintType.GRADIENT_LINEAR:
                    writer.WriteStartElement("linearGradient");
                    writer.WriteAttributeString("id", nameof(gradient));
                    for (int i = 0; i < Mathf.Max(gradient.gradientHandlePositions.Length, 2); ++i)
                    {
                        writer.WriteAttributeString($"x{i + 1}", gradient.gradientHandlePositions[i].x.ToString("F2", Culture));
                        writer.WriteAttributeString($"y{i + 1}", gradient.gradientHandlePositions[i].y.ToString("F2", Culture));
                    }

                    break;

                case PaintType.GRADIENT_RADIAL:
                case PaintType.GRADIENT_DIAMOND:
                    writer.WriteStartElement("radialGradient");
                    writer.WriteAttributeString("id", nameof(gradient));
                    writer.WriteAttributeString("fx", gradient.gradientHandlePositions[0].x.ToString("F2", Culture));
                    writer.WriteAttributeString("fy", gradient.gradientHandlePositions[0].y.ToString("F2", Culture));
                    writer.WriteAttributeString("cx", gradient.gradientHandlePositions[0].x.ToString("F2", Culture));
                    writer.WriteAttributeString("cy", gradient.gradientHandlePositions[0].y.ToString("F2", Culture));

                    Vector2 a = new ((float)gradient.gradientHandlePositions[1].x, (float)gradient.gradientHandlePositions[1].y);
                    Vector2 b = new ((float)gradient.gradientHandlePositions[0].x, (float)gradient.gradientHandlePositions[0].y);
                    float radius = (a - b).magnitude;
                    writer.WriteAttributeString("r", radius.ToString("F2", Culture));
                    break;

                default:
                    throw new NotSupportedException();
            }

            foreach (ColorStop stop in gradient.gradientStops)
            {
                writer.WriteStartElement(nameof(stop));
                writer.WriteAttributeString("offset", stop.position.ToString("F2", Culture));
                writer.WriteAttributeString("style", $"stop-color:rgb({(byte)(stop.color.r * 255)},{(byte)(stop.color.g * 255)},{(byte)(stop.color.b * 255)});stop-opacity:{stop.color.a.ToString("F2", Culture)}");
                await writer.WriteEndElementAsync();
            }

            await writer.WriteEndElementAsync();
            token.ThrowIfCancellationRequested();

            await writer.WriteEndElementAsync();
            token.ThrowIfCancellationRequested();

            writer.WriteStartElement("rect");
            writer.WriteAttributeString("width", "100");
            writer.WriteAttributeString("height", "100");
            writer.WriteAttributeString("fill", "url(#gradient)");

            if (gradient.opacity < 1.0)
                writer.WriteAttributeString("fill-opacity", gradient.opacity.ToString("F2", Culture));

            await writer.WriteEndElementAsync();
            token.ThrowIfCancellationRequested();

            await writer.WriteEndElementAsync();
            token.ThrowIfCancellationRequested();
        }
        public void Dispose() => writer?.Close();
        #endregion
    }
}