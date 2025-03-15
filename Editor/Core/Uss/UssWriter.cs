using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Figma.Core.Uss
{
    using Internals;

    internal class UssWriter : IDisposable, IAsyncDisposable
    {
        #region Fields
        readonly StreamWriter stream;
        readonly string rootDirectory;
        int count;
        #endregion

        #region Constructors
        public UssWriter(string rootDirectory, string path)
        {
            this.rootDirectory = rootDirectory;
            stream = new StreamWriter(Path = path, false, Encoding.UTF8, 1024);
        }
        #endregion

        #region Properties
        public string Path { get; }
        #endregion

        #region Methods
        public void Write(BaseUssStyle style)
        {
            if (!style.HasAttributes)
                return;

            if (count > 0)
            {
                stream.WriteLine();
                stream.WriteLine();
            }

            stream.Write(style.BuildName());
            stream.WriteLine(" {");

            foreach ((string key, string value) in style.Attributes)
            {
                switch (key)
                {
                    case "background-image" when value.Contains("url"):
                        // Getting a relative path to image to keep the reference.
                        // Since images are located in a parent of a parent directory.
                        // Assets/Images directory and frames are in Assets/Frames/CanvasName/
                        // We need to calculate a relative path and the only way of doing this
                        // is on write operation, since UssStyle is not aware of the path, where it
                        // would be written.
                        stream.WriteLine($"\t{key}: url('{System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(Path), PathExtensions.CombinePath(rootDirectory, value[5..^2]))?.Replace('\\', '/')}');");
                        continue;

                    default:
                        stream.WriteLine($"\t{key}: {value};");
                        break;
                }
            }

            stream.Write("}");

            count++;

            Write(style.SubStyles);
        }
        public void Write(IEnumerable<BaseUssStyle> styles) => styles.OrderBy(x => x.Name).ForEach(Write);

        void IDisposable.Dispose() => stream?.Dispose();
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (stream != null)
                await stream.DisposeAsync();
        }
        #endregion
    }
}