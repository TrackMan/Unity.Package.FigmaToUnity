using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Figma.Core.Uss
{
    internal class UssWriter : IDisposable, IAsyncDisposable
    {
        #region Fields
        readonly StreamWriter stream;
        int count;
        #endregion

        #region Constructors
        public UssWriter(string path) => stream = new StreamWriter(path);
        #endregion

        #region Methods
        public void Write(BaseUssStyle style)
        {
            if (!style.HasAttributes) return;

            if (count > 0)
            {
                stream.WriteLine();
                stream.WriteLine();
            }

            stream.Write(style.BuildName());
            stream.WriteLine(" {");
            
            foreach ((string key, string value) in style.Attributes)
            {
                if (key == "--unity-font-missing")
                    continue;
                
                stream.WriteLine($"\t{key}: {value};");
            }

            stream.Write("}");

            count++;

            Write(style.SubStyles);
        }
        public void Write(IEnumerable<BaseUssStyle> styles)
        {
            foreach (BaseUssStyle style in styles)
                Write(style);
        }

        void IDisposable.Dispose() => stream?.Dispose();
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (stream != null) 
                await stream.DisposeAsync();
        }
        #endregion
    }
}