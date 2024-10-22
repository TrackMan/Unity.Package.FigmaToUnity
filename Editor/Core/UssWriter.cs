using System.Collections.Generic;
using System.IO;

namespace Figma
{
    internal class UssWriter
    {
        #region Fields
        readonly StreamWriter uss;
        
        int count;
        #endregion

        #region Constructors
        public UssWriter(IEnumerable<UssStyle> styles, IEnumerable<UssStyle> components, IEnumerable<UssStyle> nodes, StreamWriter uss)
        {
            this.uss = uss;

            Write(new UssStyle(UssStyle.overrideClass));
            foreach (UssStyle style in styles) Write(style);
            foreach (UssStyle style in components) Write(style);
            Write(new UssStyle(UssStyle.viewportClass));
            foreach (UssStyle style in nodes) Write(style);
        }
        #endregion

        #region Methods
        void Write(UssStyle style)
        {
            if (!style.HasAttributes) return;

            if (count > 0)
            {
                uss.WriteLine();
                uss.WriteLine();
            }

            style.Write(uss);
            count++;
        }
        #endregion
    }
}