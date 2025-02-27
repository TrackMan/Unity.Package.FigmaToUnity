namespace Figma.Core.Uss
{
    internal struct CursorProperty
    {
        #region Operators
        public static implicit operator CursorProperty(string _) => new();
        public static implicit operator string(CursorProperty _) => null;
        #endregion
    }
}