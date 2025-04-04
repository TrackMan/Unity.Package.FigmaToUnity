namespace Figma
{
    enum Unit
    {
        Default,
        None,
        Initial,
        Auto,
        Pixel,
        Degrees,
        Percent
    }

    enum Align
    {
        Auto,
        FlexStart,
        FlexEnd,
        Center,
        Stretch
    }

    enum FlexDirection
    {
        Row,
        RowReverse,
        Column,
        ColumnReverse
    }

    enum FlexWrap
    {
        Nowrap,
        Wrap,
        WrapReverse
    }

    enum JustifyContent
    {
        FlexStart,
        FlexEnd,
        Center,
        SpaceBetween,
        SpaceAround
    }

    enum Position
    {
        Absolute,
        Relative
    }

    enum Visibility
    {
        Visible,
        Hidden
    }

    enum OverflowClip
    {
        PaddingBox,
        ContentBox
    }

    enum Display
    {
        Flex,
        None
    }

    enum FontStyle
    {
        Normal,
        Italic,
        Bold,
        BoldAndItalic
    }

    enum TextAlign
    {
        UpperLeft,
        MiddleLeft,
        LowerLeft,
        UpperCenter,
        MiddleCenter,
        LowerCenter,
        UpperRight,
        MiddleRight,
        LowerRight
    }

    enum EasingFunction
    {
        EaseIn,
        EaseOut,
        EaseInAndOut,
        Linear,
        Slow,
        CustomSpring
    }

    enum Wrap
    {
        Normal,
        Nowrap,
    }

    public enum ElementType
    {
        None,

        //Base elements
        VisualElement,
        BindableElement,

        //Utilities
        Box,
        TextElement,
        Label,
        Image,
        IMGUIContainer,
        Foldout,

        //Templates
        Template,
        Instance,
        TemplateContainer,

        //Controls
        Button,
        RepeatButton,
        Toggle,
        Scroller,
        Slider,
        SliderInt,
        MinMaxSlider,
        EnumField,
        MaskField,
        LayerField,
        LayerMaskField,
        TagField,
        ProgressBar,

        //Text input
        TextField,
        IntegerField,
        LongField,
        FloatField,
        DoubleField,
        Vector2Field,
        Vector2IntField,
        Vector3Field,
        Vector3IntField,
        Vector4Field,
        RectField,
        RectIntField,
        BoundsField,
        BoundsIntField,

        //Complex widgets
        PropertyField,
        PropertyControlInt,
        PropertyControlLong,
        PropertyControlFloat,
        PropertyControlDouble,
        PropertyControlString,
        ColorField,
        CurveField,
        GradientField,
        ObjectField,

        //Toolbar
        Toolbar,
        ToolbarButton,
        ToolbarToggle,
        ToolbarMenu,
        ToolbarSearchField,
        ToolbarPopupSearchField,
        ToolbarSpacer,

        //Views and windows
        ListView,
        ScrollView,
        TreeView,
        PopupWindow,

        IElement
    }

    enum TimeUnit
    {
        Default,
        Millisecond,
        Second
    }

    enum PseudoClass
    {
        None = 0,
        Hover,
        Active,
        Inactive,
        Focus,
        Selected,
        Disabled,
        Enabled,
        Checked,
        Root
    }

    enum FontWeight
    {
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Regular = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900
    }
}