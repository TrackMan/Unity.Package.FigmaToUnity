using System;
using System.Diagnostics;
using System.IO;

namespace Figma.Attributes
{
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
        public UxmlDownloadImages ImageFiltering { get; }
        public UxmlElementTypeIdentification TypeIdentification { get; }
        #endregion

        #region Constructors
        public UxmlAttribute(string root = default, UxmlDownloadImages imageFiltering = UxmlDownloadImages.Everything, UxmlElementTypeIdentification typeIdentification = UxmlElementTypeIdentification.ByName, params string[] preserve)
        {
            Root = root;
            DocumentRoot = Path.Combine(prefix, root);
            Preserve = preserve;
            DocumentPreserve = (string[])preserve.Clone();

            for (int i = 0; i < preserve.Length; ++i)
                DocumentPreserve[i] = Path.Combine(prefix, DocumentPreserve[i]);

            ImageFiltering = imageFiltering;
            TypeIdentification = typeIdentification;
        }
        #endregion
    }
}