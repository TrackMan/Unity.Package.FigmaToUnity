using System;
using System.Diagnostics;

namespace Figma.Attributes
{
    using static Internals.PathExtensions;

    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class)]
    public class UxmlAttribute : Attribute
    {
        public const string prefix = "Document";

        #region Properties
        public string Root { get; }
        public string DocumentRoot { get; }
        public string[] Preserve { get; }
        public string[] DocumentPreserve { get; }
        public UxmlDownloadImages DownloadImages { get; }
        public UxmlElementTypeIdentification TypeIdentification { get; }
        #endregion

        #region Constructors
        public UxmlAttribute(string root = null, UxmlDownloadImages downloadImages = UxmlDownloadImages.Everything, UxmlElementTypeIdentification typeIdentification = UxmlElementTypeIdentification.ByName, params string[] preserve)
        {
            Root = root;

            DocumentRoot = CombinePath(prefix, root);
            Preserve = preserve;
            DocumentPreserve = (string[])preserve.Clone();

            for (int i = 0; i < preserve.Length; ++i)
                DocumentPreserve[i] = CombinePath(prefix, DocumentPreserve[i]);

            DownloadImages = downloadImages;
            TypeIdentification = typeIdentification;
        }
        #endregion
    }
}