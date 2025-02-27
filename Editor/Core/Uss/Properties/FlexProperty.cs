namespace Figma.Core.Uss
{
    internal struct FlexProperty
    {
        #region Operators
        public static implicit operator FlexProperty(string _) => new();
        public static implicit operator string(FlexProperty _) => null;
        #endregion
    }
}