namespace Figma.Core
{
    using Internals;

    internal class StyleSlot : Style
    {
        #region Fields
        public bool Text { get; }
        public string Slot { get; }
        #endregion

        #region Constructors
        public StyleSlot(bool text, string slot, Style style)
        {
            Text = text;
            Slot = slot;
            styleType = style.styleType;
            key = style.key;
            name = style.name;
            description = style.description;
        }
        #endregion

        #region Methods
        public override string ToString() => $"text={Text} slot={Slot} styleType={styleType} key={key} name={name} description={description}";
        #endregion
    }
}