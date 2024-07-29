namespace Figma
{
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
}