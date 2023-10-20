using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using Trackman;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable S1144 // Unused private types or members should be removed

namespace Figma
{
    using global;
    using number = Double;

    class FigmaParser
    {
        internal const string images = "Images";
        internal const string elements = "Elements";
        internal static readonly CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

        class StyleSlot : Style
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

        class UssStyle
        {
            internal const string viewportClass = "unity-viewport";
            internal const string overrideClass = "unity-base-override";
            readonly string[] fontWeights = { "Thin", "ExtraLight", "Light", "Regular", "Medium", "SemiBold", "Bold", "ExtraBold", "Black" };

            #region Containers
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

            enum ScaleMode
            {
                StretchToFill,
                ScaleAndCrop,
                ScaleToFit
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

            enum Wrap
            {
                Normal,
                Nowrap,
            }

            /// <summary>
            /// Represents a distance value.
            /// </summary>
            readonly struct LengthProperty
            {
                #region Fields
                readonly number value;
                readonly Unit unit;
                #endregion

                #region Constructors
                internal LengthProperty(Unit unit)
                {
                    value = default;
                    this.unit = unit;
                }
                internal LengthProperty(number value, Unit unit)
                {
                    this.value = value;
                    this.unit = unit;
                }
                #endregion

                #region Operators
                public static implicit operator LengthProperty(Unit value) => new(default, value);
                public static implicit operator LengthProperty(number? value) => new(value!.Value, Unit.Pixel);
                public static implicit operator LengthProperty(number value) => new(value, Unit.Pixel);
                public static implicit operator LengthProperty(string value)
                {
                    if (Enum.TryParse(value, true, out Unit unit)) return new LengthProperty(unit);
                    if (value.Contains("px")) return new LengthProperty(number.Parse(value.ToLower().Replace("px", ""), culture), Unit.Pixel);
                    if (value.Contains("deg")) return new LengthProperty(number.Parse(value.ToLower().Replace("deg", ""), culture), Unit.Degrees);
                    if (value.Contains('%')) return new LengthProperty(number.Parse(value.Replace("%", ""), culture), Unit.Percent);
                    return default;
                }
                public static implicit operator string(LengthProperty value)
                {
                    return value.unit switch
                    {
                        Unit.Pixel => $"{(int)Math.Round(value.value)}px",
                        Unit.Degrees => $"{value.value.ToString("F2", culture).Replace(".00", "")}deg",
                        Unit.Percent => $"{value.value.ToString("F2", culture).Replace(".00", "")}%",
                        Unit.Auto => "auto",
                        Unit.None => "none",
                        Unit.Initial => "initial",
                        Unit.Default => "0px",
                        _ => throw new ArgumentException(value)
                    };
                }

                public static LengthProperty operator +(LengthProperty a) => a;
                public static LengthProperty operator -(LengthProperty a) => new(-a.value, a.unit);
                public static LengthProperty operator +(LengthProperty a, number b) => new(a.value + b, a.unit);
                public static LengthProperty operator -(LengthProperty a, number b) => new(a.value - b, a.unit);

                public static bool operator ==(LengthProperty a, Unit b) => a.unit == b;
                public static bool operator !=(LengthProperty a, Unit b) => a.unit != b;

                public override bool Equals(object obj) => obj is LengthProperty property && value == property.value && unit == property.unit;
                public override int GetHashCode() => HashCode.Combine(value, unit);
                public override string ToString() => this;
                #endregion
            }

            /// <summary>
            /// Represents either an integer or a number with a fractional component.
            /// </summary>
            struct NumberProperty
            {
                #region Fields
                readonly number value;
                #endregion

                #region Constructors
                NumberProperty(number value) => this.value = value;
                #endregion

                #region Operators
                public static implicit operator NumberProperty(number? value) => new(value!.Value);
                public static implicit operator NumberProperty(number value) => new(value);
                public static implicit operator NumberProperty(string value) => new(number.Parse(value, culture));
                public static implicit operator string(NumberProperty value) => value.value.ToString("F2", culture).Replace(".00", "");

                public static NumberProperty operator +(NumberProperty a) => a;
                public static NumberProperty operator -(NumberProperty a) => new(-a.value);
                public static NumberProperty operator +(NumberProperty a, number b) => new(a.value + b);
                public static NumberProperty operator -(NumberProperty a, number b) => new(a.value - b);
                #endregion
            }

            /// <summary>
            /// Represents a whole number.
            /// </summary>
            struct IntegerProperty
            {
                #region Fields
                readonly int value;
                #endregion

                #region Constructors
                IntegerProperty(int value) => this.value = value;
                #endregion

                #region Operators
                public static implicit operator IntegerProperty(int? value) => new(value!.Value);
                public static implicit operator IntegerProperty(int value) => new(value);
                public static implicit operator IntegerProperty(string value) => new(int.Parse(value));
                public static implicit operator string(IntegerProperty value) => value.value.ToString(culture);

                public static IntegerProperty operator +(IntegerProperty a) => a;
                public static IntegerProperty operator -(IntegerProperty a) => new(-a.value);
                public static IntegerProperty operator +(IntegerProperty a, int b) => new(a.value + b);
                public static IntegerProperty operator -(IntegerProperty a, int b) => new(a.value - b);
                #endregion
            }

            /// <summary>
            /// Represents a color. You can define a color with a #hexadecimal code, the rgb() or rgba() function, or a color keyword (for example, blue or transparent).
            /// </summary>
            readonly struct ColorProperty
            {
                #region Fields
                readonly string rgba;
                readonly string rgb;
                readonly string hex;
                readonly string name;
                #endregion

                #region Constructors
                internal ColorProperty(RGBA color, number? opacity = 1, float alphaMult = 1.0f)
                {
                    rgba = $"rgba({(byte)(color.r * 255.0f)},{(byte)(color.g * 255.0f)},{(byte)(color.b * 255.0f)},{(color.a * (opacity ?? alphaMult)).ToString("F2", culture).Replace(".00", "")})";
                    rgb = default;
                    hex = default;
                    name = default;
                }
                ColorProperty(string value)
                {
                    rgba = default;
                    rgb = default;
                    hex = default;
                    name = default;

                    if (value.StartsWith("rgba")) rgba = value;
                    else if (value.StartsWith("rgb")) rgb = value;
                    else if (value.StartsWith('#')) hex = value;
                    else name = value;
                }
                #endregion

                #region Operators
                public static implicit operator ColorProperty(Unit _) => new();
                public static implicit operator ColorProperty(RGBA value) => new(value);
                public static implicit operator ColorProperty(string value) => new(value);
                public static implicit operator string(ColorProperty value)
                {
                    if (value.rgba.NotNullOrEmpty()) return value.rgba;
                    if (value.rgb.NotNullOrEmpty()) return value.rgb;
                    if (value.hex.NotNullOrEmpty()) return value.hex;
                    if (value.name.NotNullOrEmpty()) return value.name;
                    return "initial";
                }
                public override string ToString() => this;
                #endregion
            }

            /// <summary>
            /// Represents an asset in a Resources folder or represents an asset specified by a path, it can be expressed as either a relative path or an absolute path.
            /// </summary>
            struct AssetProperty
            {
                #region Fields
                readonly string url;
                readonly string resource;
                readonly Unit unit;
                #endregion

                #region Constructors
                AssetProperty(Unit unit)
                {
                    url = default;
                    resource = default;
                    this.unit = unit;
                }
                AssetProperty(string value)
                {
                    url = default;
                    resource = default;
                    unit = default;

                    if (value.StartsWith("url")) url = value;
                    else if (value.StartsWith("resource")) resource = value;
                    else throw new NotSupportedException();
                }
                #endregion

                #region Operators
                public static implicit operator AssetProperty(Unit value) => new(value);
                public static implicit operator AssetProperty(string value) => Enum.TryParse(value, true, out Unit unit) ? new AssetProperty(unit) : new AssetProperty(value);
                public static implicit operator string(AssetProperty value)
                {
                    if (value.url.NotNullOrEmpty()) return value.url;
                    if (value.resource.NotNullOrEmpty()) return value.resource;
                    return value.unit switch
                    {
                        Unit.None => "none",
                        Unit.Initial => "initial",
                        _ => throw new ArgumentException(value)
                    };
                }
                #endregion
            }

#pragma warning disable CS0660, CS0661
            struct EnumProperty<T> where T : struct, Enum
#pragma warning restore CS0660, CS0661
            {
                // ReSharper disable StaticMemberInGenericType
                static readonly Regex enumParserRegexString = new("(?<name>([a-z]+\\-?))", RegexOptions.Compiled);
                static readonly Regex enumParserRegexValue = new("(?<name>([A-Z][a-z]+)?)", RegexOptions.Compiled);
                // ReSharper restore StaticMemberInGenericType

                #region Fields
                T value;
                readonly Unit unit;
                #endregion

                #region Constructors
                EnumProperty(T value)
                {
                    this.value = value;
                    unit = Unit.None;
                }
                EnumProperty(Unit unit)
                {
                    value = default;
                    this.unit = unit;
                }
                #endregion

                #region Operators
                public static implicit operator EnumProperty<T>(Unit unit) => new(unit);
                public static implicit operator EnumProperty<T>(T value) => new(value);
                public static implicit operator EnumProperty<T>(string value) => Enum.TryParse(enumParserRegexString.Replace(value, "${name}").Replace("-", ""), true, out T result) ? new EnumProperty<T>(result) : default;
                public static implicit operator string(EnumProperty<T> value) => value.unit == Unit.None ? enumParserRegexValue.Replace(value.value.ToString(), "${name}-").ToLower().TrimEnd('-') : "initial";

                public static bool operator ==(EnumProperty<T> a, T b) => a.value.Equals(b);
                public static bool operator !=(EnumProperty<T> a, T b) => !a.value.Equals(b);

                public override string ToString() => this;
                #endregion
            }

            struct ShadowProperty
            {
                static readonly Regex regex = new(@"(?<offsetHorizontal>\d+[px]*)\s+(?<offsetVertical>\d+[px]*)\s+(?<blurRadius>\d+[px]*)\s+(?<color>(rgba\([\d,\.\s]+\))|#\w{2,8}|[^#][\w-]+)");

                #region Fields
                readonly LengthProperty offsetHorizontal;
                readonly LengthProperty offsetVertical;
                readonly LengthProperty blurRadius;
                readonly ColorProperty color;
                #endregion

                #region Constructors
                internal ShadowProperty(LengthProperty offsetHorizontal, LengthProperty offsetVertical, LengthProperty blurRadius, ColorProperty color)
                {
                    this.offsetHorizontal = offsetHorizontal;
                    this.offsetVertical = offsetVertical;
                    this.blurRadius = blurRadius;
                    this.color = color;
                }
                ShadowProperty(string value)
                {
                    Match match = regex.Match(value);
                    offsetHorizontal = match.Groups["offsetHorizontal"].Value;
                    offsetVertical = match.Groups["offsetVertical"].Value;
                    blurRadius = match.Groups["blurRadius"].Value;
                    color = match.Groups["color"].Value;
                }
                #endregion

                #region Operators
                public static implicit operator ShadowProperty(string value) => new(value);
                public static implicit operator string(ShadowProperty value) => $"{value.offsetHorizontal} {value.offsetVertical} {value.blurRadius} {value.color}";
                #endregion
            }

            readonly struct Length4Property
            {
                #region Fields
                readonly Unit unit;
                readonly LengthProperty[] properties;
                #endregion

                #region Properties
                internal LengthProperty this[int index] { get => properties[index]; set => properties[index] = value; }
                #endregion

                #region Constructors
                internal Length4Property(Unit unit)
                {
                    this.unit = unit;
                    properties = new LengthProperty[] { new(unit), new(unit), new(unit), new(unit) };
                }
                internal Length4Property(LengthProperty[] properties)
                {
                    unit = Unit.None;
                    this.properties = properties;
                }
                #endregion

                #region Operators
                public static implicit operator Length4Property(Unit unit) => new(unit);
                public static implicit operator Length4Property(number? value) => new(new LengthProperty[] { value!.Value });
                public static implicit operator Length4Property(number value) => new(new LengthProperty[] { value });
                public static implicit operator Length4Property(number[] values)
                {
                    LengthProperty[] properties = new LengthProperty[values.Length];
                    for (int i = 0; i < values.Length; i++) properties[i] = values[i];
                    return new Length4Property(properties);
                }
                public static implicit operator Length4Property(string value)
                {
                    string[] values = value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    LengthProperty[] properties = new LengthProperty[values.Length];
                    for (int i = 0; i < values.Length; i++) properties[i] = values[i];
                    return new Length4Property(properties);
                }
                public static implicit operator string(Length4Property value)
                {
                    if (value is { unit: Unit.None, properties: not null })
                    {
                        string[] values = new string[value.properties.Length];
                        for (int i = 0; i < values.Length; i++) values[i] = value.properties[i];
                        return string.Join(" ", values);
                    }

                    return new LengthProperty(value.unit);
                }

                public static Length4Property operator +(Length4Property a) => a;
                public static Length4Property operator -(Length4Property a) => new(a.properties.Select(x => -x).ToArray());
                public static Length4Property operator +(Length4Property a, number b) => new(a.properties.Select(x => x + b).ToArray());
                public static Length4Property operator -(Length4Property a, number b) => new(a.properties.Select(x => x - b).ToArray());
                #endregion
            }

            struct FlexProperty
            {
                #region Operators
                public static implicit operator FlexProperty(string value) => default;
                public static implicit operator string(FlexProperty value) => default;
                #endregion
            }

            struct CursorProperty
            {
                #region Operators
                public static implicit operator CursorProperty(string value) => default;
                public static implicit operator string(CursorProperty value) => default;
                #endregion
            }
            #endregion

            #region Fields
            readonly Func<string, string, (bool valid, string path)> getAssetPath;
            readonly Func<string, string, (bool valid, int width, int height)> getAssetSize;

            readonly List<UssStyle> inherited = new();
            readonly Dictionary<string, string> defaults = new();
            readonly Dictionary<string, string> attributes = new();
            #endregion

            #region Properties
            public string Name { get; set; }
            public bool HasAttributes => attributes.Count > 0;

            // Box model
            // Dimensions
            LengthProperty width { get => Get("width"); set => Set("width", value); }
            LengthProperty height { get => Get("height"); set => Set("height", value); }
            LengthProperty minWidth { get => Get("min-width"); set => Set("min-width", value); }
            LengthProperty minHeight { get => Get("min-height"); set => Set("min-height", value); }
            LengthProperty maxWidth { get => Get("max-width"); set => Set("max-width", value); }
            LengthProperty maxHeight { get => Get("max-height"); set => Set("max-height", value); }
            // Margins
            LengthProperty marginLeft { get => Get1("margin-left", "margin", 0); set => Set4("margin-left", value, "margin", 0); }
            LengthProperty marginTop { get => Get1("margin-top", "margin", 1); set => Set4("margin-top", value, "margin", 1); }
            LengthProperty marginRight { get => Get1("margin-right", "margin", 2); set => Set4("margin-right", value, "margin", 2); }
            LengthProperty marginBottom { get => Get1("margin-bottom", "margin", 3); set => Set4("margin-bottom", value, "margin", 3); }
            Length4Property margin { get => Get4("margin", "margin-left", "margin-top", "margin-right", "margin-bottom"); set => Set1("margin", value, "margin-left", "margin-top", "margin-right", "margin-bottom"); }
            // Borders
            LengthProperty borderLeftWidth { get => Get1("border-left-width", "border-width", 0); set => Set4("border-left-width", value, "border-width", 0); }
            LengthProperty borderTopWidth { get => Get1("border-top-width", "border-width", 1); set => Set4("border-top-width", value, "border-width", 1); }
            LengthProperty borderRightWidth { get => Get1("border-right-width", "border-width", 2); set => Set4("border-right-width", value, "border-width", 2); }
            LengthProperty borderBottomWidth { get => Get1("border-bottom-width", "border-width", 3); set => Set4("border-bottom-width", value, "border-width", 3); }
            Length4Property borderWidth { get => Get4("border-width", "border-left-width", "border-top-width", "border-right-width", "border-bottom-width"); set => Set1("border-width", value, "border-left-width", "border-top-width", "border-right-width", "border-bottom-width"); }
            // Padding
            LengthProperty paddingLeft { get => Get1("padding-left", "padding", 0); set => Set4("padding-left", value, "padding", 0); }
            LengthProperty paddingTop { get => Get1("padding-top", "padding", 1); set => Set4("padding-top", value, "padding", 1); }
            LengthProperty paddingRight { get => Get1("padding-right", "padding", 2); set => Set4("padding-right", value, "padding", 2); }
            LengthProperty paddingBottom { get => Get1("padding-bottom", "padding", 3); set => Set4("padding-bottom", value, "padding", 3); }
            Length4Property padding { get => Get4("padding", "padding-left", "padding-top", "padding-right", "padding-bottom"); set => Set1("padding", value, "padding-left", "padding-top", "padding-right", "padding-bottom"); }

            // Flex
            // Items
            NumberProperty flexGrow { get => Get("flex-grow"); set => Set("flex-grow", value); }
            NumberProperty flexShrink { get => Get("flex-shrink"); set => Set("flex-shrink", value); }
            LengthProperty flexBasis { get => Get("flex-basis"); set => Set("flex-basis", value); }
            FlexProperty flex { get => Get("flex"); set => Set("flex", value); }
            EnumProperty<Align> alignSelf { get => Get("align-self"); set => Set("align-self", value); }
            NumberProperty itemSpacing { get => Get("--item-spacing"); set => Set("--item-spacing", value); }
            // Containers
            EnumProperty<FlexDirection> flexDirection { get => Get("flex-direction"); set => Set("flex-direction", value); }
            EnumProperty<FlexWrap> flexWrap { get => Get("flex-wrap"); set => Set("flex-wrap", value); }
            EnumProperty<Align> alignContent { get => Get("align-content"); set => Set("align-content", value); }
            EnumProperty<Align> alignItems { get => Get("align-items"); set => Set("align-items", value); }
            EnumProperty<JustifyContent> justifyContent { get => Get("justify-content"); set => Set("justify-content", value); }
            // Positioning
            EnumProperty<Position> position { get => Get("position"); set => Set("position", value); }
            LengthProperty left { get => Get("left"); set => Set("left", value); }
            LengthProperty top { get => Get("top"); set => Set("top", value); }
            LengthProperty right { get => Get("right"); set => Set("right", value); }
            LengthProperty bottom { get => Get("bottom"); set => Set("bottom", value); }
            LengthProperty rotate { get => Get("rotate"); set => Set("rotate", value); }

            // Drawing
            // Background
            ColorProperty backgroundColor { get => Get("background-color"); set => Set("background-color", value); }
            AssetProperty backgroundImage { get => Get("background-image"); set => Set("background-image", value); }
            EnumProperty<ScaleMode> unityBackgroundScaleMode { get => Get("-unity-background-scale-mode"); set => Set("-unity-background-scale-mode", value); }
            ColorProperty unityBackgroundImageTintColor { get => Get("-unity-background-image-tint-color"); set => Set("-unity-background-image-tint-color", value); }
            // Slicing
            IntegerProperty unitySliceLeft { get => Get("-unity-slice-left"); set => Set("-unity-slice-left", value); }
            IntegerProperty unitySliceTop { get => Get("-unity-slice-top"); set => Set("-unity-slice-top", value); }
            IntegerProperty unitySliceRight { get => Get("-unity-slice-right"); set => Set("-unity-slice-right", value); }
            IntegerProperty unitySliceBottom { get => Get("-unity-slice-bottom"); set => Set("-unity-slice-bottom", value); }
            // Borders
            ColorProperty borderColor { get => Get("border-color"); set => Set("border-color", value); }
            LengthProperty borderTopLeftRadius { get => Get1("border-top-left-radius", "border-radius", 0); set => Set4("border-top-left-radius", value, "border-radius", 0); }
            LengthProperty borderTopRightRadius { get => Get1("border-top-right-radius", "border-radius", 1); set => Set4("border-top-right-radius", value, "border-radius", 1); }
            LengthProperty borderBottomLeftRadius { get => Get1("border-bottom-left-radius", "border-radius", 2); set => Set4("border-bottom-left-radius", value, "border-radius", 2); }
            LengthProperty borderBottomRightRadius { get => Get1("border-bottom-right-radius", "border-radius", 3); set => Set4("border-bottom-right-radius", value, "border-radius", 3); }
            Length4Property borderRadius { get => Get4("border-radius", "border-top-left-radius", "border-top-right-radius", "border-bottom-left-radius", "border-bottom-right-radius"); set => Set1("border-radius", value, "border-top-left-radius", "border-top-right-radius", "border-bottom-left-radius", "border-bottom-right-radius"); }
            // Appearance
            EnumProperty<Visibility> overflow { get => Get("overflow"); set => Set("overflow", value); }
            EnumProperty<OverflowClip> unityOverflowClipBox { get => Get("-unity-overflow-clip-box"); set => Set("-unity-overflow-clip-box", value); }
            NumberProperty opacity { get => Get("opacity"); set => Set("opacity", value); }
            EnumProperty<Visibility> visibility { get => Get("visibility"); set => Set("visibility", value); }
            EnumProperty<Display> display { get => Get("display"); set => Set("display", value); }

            // Text
            ColorProperty color { get => Get("color"); set => Set("color", value); }
            AssetProperty unityFont { get => Get("-unity-font"); set => Set("-unity-font", value); }
            AssetProperty unityFontMissing { get => Get("--unity-font-missing"); set => Set("--unity-font-missing", value); }
            AssetProperty unityFontDefinition { get => Get("-unity-font-definition"); set => Set("-unity-font-definition", value); }
            LengthProperty fontSize { get => Get("font-size"); set => Set("font-size", value); }
            EnumProperty<FontStyle> unityFontStyle { get => Get("-unity-font-style"); set => Set("-unity-font-style", value); }
            EnumProperty<TextAlign> unityTextAlign { get => Get("-unity-text-align"); set => Set("-unity-text-align", value); }
            EnumProperty<Wrap> whiteSpace { get => Get("white-space"); set => Set("white-space", value); }
            ShadowProperty textShadow { get => Get("text-shadow"); set => Set("text-shadow", value); }

            // Cursor
            CursorProperty cursor { get => Get("cursor"); set => Set("cursor", value); }

            // Effects
            ShadowProperty boxShadow { get => Get("--box-shadow"); set => Set("--box-shadow", value); }
            #endregion

            #region Constructors
            public UssStyle(string name)
            {
                Name = name;
                switch (name)
                {
                    case overrideClass:
                        backgroundColor = Unit.Initial;
                        borderWidth = Unit.Initial;
                        overflow = Unit.Initial;
                        padding = Unit.Initial;
                        margin = Unit.Initial;
                        unityFontDefinition = Unit.Initial;
                        justifyContent = JustifyContent.Center;
                        alignItems = Align.Center;
                        break;

                    case viewportClass:
                        position = Position.Absolute;
                        width = "100%";
                        height = "100%";
                        break;
                }
            }
            public UssStyle(string name, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize, string slot, Style.StyleType type, BaseNode node)
            {
                Name = name;
                this.getAssetPath = getAssetPath;
                this.getAssetSize = getAssetSize;

                if (type == Style.StyleType.FILL && node is GeometryMixin geometry)
                {
                    if (slot == "fill")
                    {
                        if (node is TextNode)
                        {
                            Name += "-Text";
                            AddTextFillStyle(geometry.fills);
                        }
                        else
                        {
                            AddFillStyle(geometry.fills);
                        }
                    }
                    else if (slot == "stroke")
                    {
                        if (node is TextNode)
                        {
                            Name += "-TextStroke";
                        }
                        else
                        {
                            Name += "-Border";
                            AddStrokeFillStyle(geometry.strokes);
                        }
                    }
                }
                else if (type == Style.StyleType.TEXT && node is TextNode text)
                {
                    AddTextStyle(text.style);
                }
                else if (type == Style.StyleType.EFFECT && node is BlendMixin blend)
                {
                    AddNodeEffects(blend.effects);
                }
            }
            public UssStyle(string name, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize, BaseNode node)
            {
                Name = name;
                this.getAssetPath = getAssetPath;
                this.getAssetSize = getAssetSize;

                if (node is FrameNode frame) AddFrameNode(frame);
                if (node is GroupNode group) AddGroupNode(group);
                if (node is SliceNode slice) AddSliceNode(slice);
                if (node is RectangleNode rectangle) AddRectangleNode(rectangle);
                if (node is LineNode line) AddLineNode(line);
                if (node is EllipseNode ellipse) AddEllipseNode(ellipse);
                if (node is RegularPolygonNode regularPolygon) AddRegularPolygonNode(regularPolygon);
                if (node is StarNode star) AddStarNode(star);
                if (node is VectorNode vector) AddVectorNode(vector);
                if (node is TextNode text) AddTextNode(text);
                if (node is ComponentSetNode set) AddComponentSetNode(set);
                if (node is ComponentNode component) AddComponentNode(component);
                if (node is InstanceNode instance) AddInstanceNode(instance);
                if (node is BooleanOperationNode booleanOperation) AddBooleanOperationNode(booleanOperation);
            }
            #endregion

            #region Methods
            public bool DoesInherit(UssStyle style) => inherited.Contains(style);
            public void Inherit(UssStyle component)
            {
                inherited.Add(component);

                foreach (KeyValuePair<string, string> keyValue in component.attributes)
                {
                    if (attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == component.attributes[keyValue.Key])
                        attributes.Remove(keyValue.Key);

                    if (!attributes.ContainsKey(keyValue.Key) && defaults.TryGetValue(keyValue.Key, out string @default))
                        attributes.Add(keyValue.Key, @default);
                }
            }
            public void Inherit(IReadOnlyCollection<UssStyle> styles)
            {
                inherited.AddRange(styles);

                foreach (UssStyle style in styles)
                {
                    foreach (KeyValuePair<string, string> keyValue in style.attributes.Where(keyValue => attributes.ContainsKey(keyValue.Key) &&
                                                                                                         attributes[keyValue.Key] == style.attributes[keyValue.Key]))
                        attributes.Remove(keyValue.Key);
                }
            }
            public void Inherit(UssStyle component, IReadOnlyCollection<UssStyle> styles)
            {
                inherited.Add(component);
                inherited.AddRange(styles);

                List<string> preserve = (from keyValue in component.attributes
                                         from style in styles
                                         where style.attributes.ContainsKey(keyValue.Key) && style.attributes[keyValue.Key] != keyValue.Value
                                         select keyValue.Key).ToList();

                foreach (KeyValuePair<string, string> keyValue in component.attributes)
                {
                    if (attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == component.attributes[keyValue.Key])
                        attributes.Remove(keyValue.Key);

                    if (!attributes.ContainsKey(keyValue.Key) && defaults.TryGetValue(keyValue.Key, out string @default))
                        attributes.Add(keyValue.Key, @default);
                }

                foreach (UssStyle style in styles)
                {
                    foreach (KeyValuePair<string, string> keyValue in style.attributes.Where(keyValue => attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == style.attributes[keyValue.Key] && !preserve.Contains(keyValue.Key)))
                        attributes.Remove(keyValue.Key);
                }
            }
            public string ResolveClassList(string component) => attributes.Count > 0 ? $"{Name} {component}" : component;
            public string ResolveClassList(IEnumerable<string> styles) => attributes.Count > 0 ? $"{Name} {string.Join(" ", styles)}" : $"{string.Join(" ", styles)}";
            public string ResolveClassList(string component, IEnumerable<string> styles) => attributes.Count > 0 ? $"{Name} {component} {string.Join(" ", styles)}" : $"{component} {string.Join(" ", styles)}";
            public string ResolveClassList() => attributes.Count > 0 ? $"{Name}" : "";
            public void Write(StreamWriter stream)
            {
                stream.WriteLine($".{Name} {{");

                if (Has("--unity-font-missing"))
                {
                    Debug.LogWarning($"Cannot find Font [<color=yellow>{attributes["--unity-font-missing"]}</color>]");
                    attributes.Remove("--unity-font-missing");
                }

                foreach (KeyValuePair<string, string> keyValue in attributes) stream.WriteLine($"    {keyValue.Key}: {keyValue.Value};");

                stream.Write("}");
            }

            void AddDefaultFrameNode(DefaultFrameNode node)
            {
                AddCorner(node, node);

                AddFrame(node);
                AddDefaultShapeNode(node);

                if (node.clipsContent.HasValueAndTrue()) overflow = Visibility.Hidden;
            }
            void AddDefaultShapeNode(DefaultShapeNode node)
            {
                AddBoxModel(node, node, node, node);
                AddLayout(node, node);
                AddBlend(node);
                AddGeometry(node, node, node);
            }

            void AddFrameNode(FrameNode node)
            {
                AddDefaultFrameNode(node);
            }
            void AddGroupNode(GroupNode node)
            {
                AddDefaultFrameNode(node);
            }
            void AddSliceNode(SliceNode node)
            {
                AddBoxModel(node, node, default, node);
                AddLayout(node, node);
            }
            void AddRectangleNode(RectangleNode node)
            {
                AddCorner(node, node);

                AddDefaultShapeNode(node);
            }
            void AddLineNode(LineNode node)
            {
                AddDefaultShapeNode(node);
            }
            void AddEllipseNode(EllipseNode node)
            {
                AddDefaultShapeNode(node);
            }
            void AddRegularPolygonNode(RegularPolygonNode node)
            {
                AddCorner(node, node);

                AddDefaultShapeNode(node);
            }
            void AddStarNode(StarNode node)
            {
                AddCorner(node, node);

                AddDefaultShapeNode(node);
            }
            void AddVectorNode(VectorNode node)
            {
                AddCorner(node, node);

                AddDefaultShapeNode(node);
            }
            void AddTextNode(TextNode node)
            {
                void FixWhiteSpace() => whiteSpace = node.absoluteBoundingBox.height / node.style.fontSize < 2 ? Wrap.Nowrap : Wrap.Normal;

                AddDefaultShapeNode(node);

                FixWhiteSpace();
                AddTextStyle(node.style);
            }
            void AddComponentSetNode(ComponentSetNode node)
            {
                AddDefaultFrameNode(node);
            }
            void AddComponentNode(ComponentNode node)
            {
                AddDefaultFrameNode(node);
            }
            void AddInstanceNode(InstanceNode node)
            {
                AddDefaultFrameNode(node);
            }
            void AddBooleanOperationNode(BooleanOperationNode node)
            {
                AddDefaultFrameNode(node);
            }

            void AddFrame(DefaultFrameMixin mixin)
            {
                void AddPadding()
                {
                    number[] padding = { mixin.paddingTop ?? 0, mixin.paddingRight ?? 0, mixin.paddingBottom ?? 0, mixin.paddingLeft ?? 0 };
                    if (padding.Any(x => x != 0)) this.padding = padding;
                }
                void AddAutoLayout()
                {
                    switch (mixin.layoutMode)
                    {
                        case LayoutMode.HORIZONTAL:
                            flexDirection = FlexDirection.Row;
                            justifyContent = JustifyContent.FlexStart;
                            alignItems = Align.FlexStart;
                            if (mixin.layoutAlign != LayoutAlign.STRETCH && mixin.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED)) flexWrap = FlexWrap.Wrap;
                            break;

                        case LayoutMode.VERTICAL:
                            flexDirection = FlexDirection.Column;
                            justifyContent = JustifyContent.FlexStart;
                            alignItems = Align.FlexStart;
                            if (mixin.layoutAlign != LayoutAlign.STRETCH && mixin.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED)) flexWrap = FlexWrap.Wrap;
                            break;
                    }

                    if (mixin.primaryAxisAlignItems.HasValue)
                    {
                        justifyContent = mixin.primaryAxisAlignItems.Value switch
                        {
                            PrimaryAxisAlignItems.MIN => JustifyContent.FlexStart,
                            PrimaryAxisAlignItems.CENTER => JustifyContent.Center,
                            PrimaryAxisAlignItems.MAX => JustifyContent.FlexEnd,
                            PrimaryAxisAlignItems.SPACE_BETWEEN => JustifyContent.SpaceBetween,
                            _ => throw new NotSupportedException()
                        };
                    }

                    if (mixin.counterAxisAlignItems.HasValue)
                    {
                        alignItems = mixin.counterAxisAlignItems.Value switch
                        {
                            CounterAxisAlignItems.MIN => Align.FlexStart,
                            CounterAxisAlignItems.CENTER => Align.Center,
                            CounterAxisAlignItems.MAX => Align.FlexEnd,
                            CounterAxisAlignItems.BASELINE => Align.Stretch,
                            _ => throw new NotSupportedException()
                        };
                    }

                    if (mixin.itemSpacing.HasPositive()) itemSpacing = mixin.itemSpacing;
                }

                AddPadding();
                if (mixin.layoutMode.HasValue) AddAutoLayout();
            }
            void AddScene(BaseNodeMixin @base)
            {
                void HandleVisibility()
                {
                    if (IsVisible(@base) || IsStateNode(@base)) defaults.Add("display", "flex");
                    else display = Display.None;
                }

                HandleVisibility();
            }
            void AddLayout(LayoutMixin mixin, BaseNodeMixin @base)
            {
                if (@base is DefaultFrameMixin frame && IsMostlyHorizontal(frame)) flexDirection = FlexDirection.Row;
                if (@base.parent is DefaultFrameMixin { layoutMode: not null } && mixin.layoutAlign == LayoutAlign.STRETCH) alignSelf = Align.Stretch;
            }
            void AddBlend(BlendMixin mixin)
            {
                void AddOpacity()
                {
                    if (mixin.opacity.HasValue)
                    {
                        if (mixin.opacity == 1) defaults.Add("opacity", "1");
                        else opacity = mixin.opacity;
                    }
                }

                AddOpacity();
                if (mixin is TextNode) AddTextNodeEffects(mixin.effects);
                else AddNodeEffects(mixin.effects);
            }
            void AddGeometry(GeometryMixin mixin, LayoutMixin layout, BaseNodeMixin @base)
            {
                void AddBackgroundImageForVectorNode()
                {
                    if (HasImageFill(@base))
                    {
                        (bool valid, string url) = getAssetPath(@base.id, "png");
                        if (valid) backgroundImage = $"url('{url}')";
                    }
                    else
                    {
                        (bool valid, string url) = getAssetPath(@base.id, "svg");
                        if (valid) backgroundImage = $"url('{url}')";
                    }
                }
                void AddBorderWidth()
                {
                    bool state = IsStateNode(mixin.As<BaseNodeMixin>());
                    if (mixin.individualStrokeWeights is not null)
                    {
                        if (mixin.individualStrokeWeights.left > 0 || state) borderLeftWidth = mixin.individualStrokeWeights.left;
                        if (mixin.individualStrokeWeights.right > 0 || state) borderRightWidth = mixin.individualStrokeWeights.right;
                        if (mixin.individualStrokeWeights.top > 0 || state) borderTopWidth = mixin.individualStrokeWeights.top;
                        if (mixin.individualStrokeWeights.bottom > 0 || state) borderBottomWidth = mixin.individualStrokeWeights.bottom;
                    }
                    else if (mixin.strokeWeight > 0 || state) borderWidth = mixin.strokeWeight;
                }
                void AddBorderRadius(RectangleCornerMixin rectangleCornerMixin, CornerMixin cornerMixin)
                {
                    void AddRadius(number minValue, number value)
                    {
                        if (rectangleCornerMixin.rectangleCornerRadii is null)
                        {
                            if (cornerMixin.cornerRadius.HasPositive()) borderRadius = Math.Min(minValue, cornerMixin!.cornerRadius!.Value) + value;
                        }
                        else
                        {
                            for (int i = 0; i < rectangleCornerMixin.rectangleCornerRadii.Length; ++i) rectangleCornerMixin.rectangleCornerRadii[i] = Math.Min(minValue, rectangleCornerMixin.rectangleCornerRadii[i]) + value;
                            borderRadius = rectangleCornerMixin.rectangleCornerRadii;
                        }
                    }

                    number value = number.NaN;
                    switch (mixin.strokeAlign.Value)
                    {
                        case StrokeAlign.INSIDE:
                            return;

                        case StrokeAlign.CENTER:
                            value = mixin.strokeWeight.Value / 2;
                            break;

                        case StrokeAlign.OUTSIDE:
                            value = mixin.strokeWeight.Value;
                            break;
                    }

                    number minBorderRadius = Math.Min(layout.absoluteBoundingBox.width / 2, layout.absoluteBoundingBox.height / 2);
                    AddRadius(minBorderRadius, value);

                    if (borderRadius == new Length4Property(Unit.Pixel)) attributes.Remove("border-radius");
                }
                void AddRotation()
                {
                    if ((layout.relativeTransform[0][0] == 1 && layout.relativeTransform[0][0] == 0 &&
                         layout.relativeTransform[0][0] == 0 && layout.relativeTransform[1][1] == 1) || !layout.relativeTransform[0][0].HasValue ||
                        !layout.relativeTransform[0][1].HasValue || !layout.relativeTransform[1][0].HasValue || !layout.relativeTransform[1][1].HasValue) return;
                    float m00 = (float)layout.relativeTransform[0][0].Value;
                    float m01 = (float)layout.relativeTransform[0][1].Value;
                    int rotation = Mathf.RoundToInt(Mathf.Rad2Deg * Mathf.Acos(m00 / Mathf.Sqrt(m00 * m00 + m01 * m01)));
                    if (rotation != 0) rotate = new LengthProperty(rotation, Unit.Degrees);
                }

                if (IsSvgNode(@base))
                {
                    AddBackgroundImageForVectorNode();
                    return;
                }

                if (layout.relativeTransform is not null) AddRotation();

                if (mixin is TextNode)
                {
                    AddTextFillStyle(mixin.fills);
                }
                else
                {
                    AddFillStyle(mixin.fills);

                    if (mixin.strokes.Length == 0) return;
                    AddStrokeFillStyle(mixin.strokes);

                    if (!mixin.strokeWeight.HasValue) return;

                    AddBorderWidth();
                    if (mixin.strokeAlign.HasValue) AddBorderRadius(mixin as RectangleCornerMixin, mixin as CornerMixin);
                }
            }
            void AddCorner(CornerMixin cornerMixin, RectangleCornerMixin rectangleCornerMixin)
            {
                if (rectangleCornerMixin.rectangleCornerRadii is not null) borderRadius = rectangleCornerMixin.rectangleCornerRadii;
                else if (cornerMixin.cornerRadius.HasPositive()) borderRadius = cornerMixin.cornerRadius;
            }

            void AddBoxModel(LayoutMixin layout, ConstraintMixin constraint, GeometryMixin geometry, BaseNodeMixin @base)
            {
                void AdjustSvgSize()
                {
                    (bool valid, int width, int height) = getAssetSize(@base.id, "svg");
                    if (valid && width > 0 && height > 0)
                        layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y, width, height);

                    if (geometry.strokes.Length == 0 || geometry.strokeWeight is not > 0) return;
                    layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.y, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);

                    if (geometry.strokeCap is null or StrokeCap.NONE) return;
                    layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
                }
                void AddSizeByParentAutoLayoutFromAutoLayout(DefaultFrameMixin frame)
                {
                    position = Position.Relative;
                    if (frame.layoutMode == LayoutMode.HORIZONTAL)
                    {
                        if (((DefaultFrameMixin)frame.parent).layoutMode == LayoutMode.HORIZONTAL)
                        {
                            width = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto;
                            height = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto;
                        }
                        else
                        {
                            width = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto;
                            height = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto;
                        }
                    }
                    else if (frame.layoutMode == LayoutMode.VERTICAL)
                    {
                        if (((DefaultFrameMixin)frame.parent).layoutMode == LayoutMode.VERTICAL)
                        {
                            width = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto;
                            height = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto;
                        }
                        else
                        {
                            width = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto;
                            height = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto;
                        }
                    }

                    if (layout.layoutGrow.HasPositive()) flexGrow = layout.layoutGrow;
                }
                void AddSizeByParentAutoLayoutFromLayout(DefaultFrameMixin parent)
                {
                    position = Position.Relative;
                    if (parent.layoutMode == LayoutMode.HORIZONTAL)
                    {
                        width = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.width;
                        height = layout.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.height;
                    }
                    else if (parent.layoutMode == LayoutMode.VERTICAL)
                    {
                        width = layout.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.width;
                        height = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.height;
                    }

                    if (layout.layoutGrow.HasPositive()) flexGrow = layout.layoutGrow;
                }
                void AddSizeFromConstraint(DefaultFrameMixin parent, LengthProperty widthProperty, LengthProperty heightProperty)
                {
                    string GetFullPath(BaseNodeMixin node) => node.parent is not null ? $"{GetFullPath(node.parent)}/{node.name}" : node.name;

                    ConstraintHorizontal horizontal = constraint.constraints.horizontal;
                    ConstraintVertical vertical = constraint.constraints.vertical;
                    Rect rect = layout.absoluteBoundingBox;
                    Rect parentRect = parent.absoluteBoundingBox;
                    bool hasMixedCenterChildren = HasMixedCenterChildren(parent);
                    bool hasAnyCenterChildren = HasAnyCenterChildren(parent);
                    bool hasManyCenterChildren = HasManyCenterChildren(parent);
                    bool isMostlyHorizontal = IsMostlyHorizontal(parent);
                    bool isMostlyVertical = !isMostlyHorizontal;

                    if (hasMixedCenterChildren && isMostlyHorizontal && horizontal != ConstraintHorizontal.CENTER && vertical == ConstraintVertical.CENTER)
                    {
                        Debug.LogWarning($"Vertical=Center is used in mostly horizontal layout at [<color=yellow>{GetFullPath(@base)}</color>], changing Horizontal=Center.");
                        horizontal = ConstraintHorizontal.CENTER;
                    }

                    if (hasMixedCenterChildren && isMostlyVertical && vertical != ConstraintVertical.CENTER && horizontal == ConstraintHorizontal.CENTER)
                    {
                        Debug.LogWarning($"Horizontal=Center is used in mostly vertical layout at [<color=yellow>{GetFullPath(@base)}</color>], changing Vertical=Center.");
                        vertical = ConstraintVertical.CENTER;
                    }

                    if (horizontal == ConstraintHorizontal.CENTER && vertical == ConstraintVertical.CENTER)
                    {
                        position = Position.Relative;
                        alignSelf = Align.Center;
                        left = -(parentRect - rect).centerRight - (hasMixedCenterChildren && isMostlyHorizontal ? rect.halfWidth : 0);
                        top = -(parentRect - rect).centerBottom - (hasMixedCenterChildren && isMostlyVertical ? rect.halfHeight : 0);
                        width = widthProperty;
                        height = heightProperty;

                        if (hasAnyCenterChildren)
                        {
                            if (isMostlyHorizontal) marginRight = widthProperty == Unit.Auto ? -rect.width : -widthProperty;
                            else marginBottom = heightProperty == Unit.Auto ? -rect.height : -heightProperty;
                        }
                    }
                    else
                    {
                        position = Position.Absolute;
                        switch (horizontal)
                        {
                            case ConstraintHorizontal.LEFT:
                                left = -(parentRect - rect).left;
                                width = widthProperty;
                                break;

                            case ConstraintHorizontal.RIGHT:
                                right = (parentRect - rect).right;
                                width = widthProperty;
                                break;

                            case ConstraintHorizontal.LEFT_RIGHT:
                                left = -(parentRect - rect).left;
                                right = (parentRect - rect).right;
                                break;

                            case ConstraintHorizontal.CENTER:
                                position = Position.Relative;
                                left = -(parentRect - rect).centerRight - (hasManyCenterChildren && isMostlyHorizontal ? rect.halfWidth : 0);
                                width = widthProperty;
                                if (hasManyCenterChildren) marginRight = widthProperty == Unit.Auto ? -rect.width : -widthProperty;

                                switch (constraint.constraints.vertical)
                                {
                                    case ConstraintVertical.TOP:
                                        alignSelf = Align.FlexStart;
                                        break;

                                    case ConstraintVertical.BOTTOM:
                                        alignSelf = Align.FlexEnd;
                                        break;

                                    case ConstraintVertical.TOP_BOTTOM:
                                        alignSelf = Align.Stretch;
                                        marginBottom = (parentRect - rect).height;
                                        break;

                                    case ConstraintVertical.CENTER:
                                        throw new NotSupportedException();

                                    case ConstraintVertical.SCALE:
                                        alignSelf = Align.FlexStart;
                                        height = new LengthProperty(rect.height / parentRect.height * 100, Unit.Percent);
                                        break;
                                }

                                break;

                            case ConstraintHorizontal.SCALE:
                                if (parentRect.width != 0)
                                {
                                    left = new LengthProperty(-(parentRect - rect).left / parentRect.width * 100, Unit.Percent);
                                    right = new LengthProperty((parentRect - rect).right / parentRect.width * 100, Unit.Percent);
                                }
                                else
                                {
                                    left = "0%";
                                    right = "0%";
                                }

                                break;
                        }

                        switch (vertical)
                        {
                            case ConstraintVertical.TOP:
                                top = -(parentRect - rect).top;
                                height = heightProperty;
                                break;

                            case ConstraintVertical.BOTTOM:
                                bottom = (parentRect - rect).bottom;
                                height = heightProperty;
                                break;

                            case ConstraintVertical.TOP_BOTTOM:
                                top = -(parentRect - rect).top;
                                bottom = (parentRect - rect).bottom;
                                break;

                            case ConstraintVertical.CENTER:
                                position = Position.Relative;
                                top = -(parentRect - rect).centerBottom - (hasManyCenterChildren && isMostlyVertical ? rect.halfHeight : 0);
                                height = heightProperty;
                                if (hasManyCenterChildren) marginBottom = heightProperty == Unit.Auto ? -rect.height : -heightProperty;

                                switch (constraint.constraints.horizontal)
                                {
                                    case ConstraintHorizontal.LEFT:
                                        alignSelf = Align.FlexStart;
                                        break;

                                    case ConstraintHorizontal.RIGHT:
                                        alignSelf = Align.FlexEnd;
                                        break;

                                    case ConstraintHorizontal.LEFT_RIGHT:
                                        alignSelf = Align.Stretch;
                                        marginRight = (parentRect - rect).width;
                                        break;

                                    case ConstraintHorizontal.CENTER:
                                        throw new NotSupportedException();

                                    case ConstraintHorizontal.SCALE:
                                        alignSelf = Align.FlexStart;
                                        width = new LengthProperty(rect.width / parentRect.width * 100, Unit.Percent);
                                        break;
                                }

                                break;

                            case ConstraintVertical.SCALE:
                                if (parentRect.height != 0)
                                {
                                    top = new LengthProperty(-(parentRect - rect).top / parentRect.height * 100, Unit.Percent);
                                    bottom = new LengthProperty((parentRect - rect).bottom / parentRect.height * 100, Unit.Percent);
                                }
                                else
                                {
                                    top = "0%";
                                    bottom = "0%";
                                }

                                break;
                        }
                    }
                }

                void AddItemSpacing(DefaultFrameMixin parent, number itemSpacing)
                {
                    if (@base != parent.children.LastOrDefault(IsVisible))
                    {
                        if (parent!.layoutMode!.Value == LayoutMode.HORIZONTAL)
                        {
                            if (parent.primaryAxisAlignItems is not PrimaryAxisAlignItems.SPACE_BETWEEN) marginRight = itemSpacing;
                        }
                        else
                        {
                            marginBottom = itemSpacing;
                        }
                    }
                }
                void OverwriteSizeFromTextNode(TextNode node)
                {
                    switch (node.style.textAutoResize)
                    {
                        case TextAutoResize.WIDTH_AND_HEIGHT:
                            width = Unit.Auto;
                            break;

                        case TextAutoResize.HEIGHT:
                            height = Unit.Auto;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }
                void AddNonInsideBorder()
                {
                    number GetStrokeWeight(StrokeAlign strokeAlign, number strokeWeight)
                    {
                        return strokeAlign switch
                        {
                            StrokeAlign.CENTER => strokeWeight / 2,
                            StrokeAlign.OUTSIDE => strokeWeight,
                            _ => throw new NotSupportedException()
                        };
                    }

                    if (@base is DefaultFrameMixin { layoutMode: not null }) return;

                    if (!IsSvgNode(@base) && geometry is not null &&
                        geometry.strokes.Length > 0 && geometry.strokeWeight is > 0 &&
                        geometry.strokeAlign.HasValue && geometry.strokeAlign != StrokeAlign.INSIDE)
                        margin += GetStrokeWeight(geometry.strokeAlign.Value, geometry.strokeWeight.Value);
                }

                if (IsSvgNode(@base)) AdjustSvgSize();

                DefaultFrameMixin parent = @base.parent as DefaultFrameMixin;
                if (!IsRootNode(@base))
                {
                    if (parent!.layoutMode.HasValue)
                    {
                        if (@base is DefaultFrameMixin { layoutMode: not null } frame)
                        {
                            AddSizeByParentAutoLayoutFromAutoLayout(frame);
                        }
                        else
                        {
                            AddSizeByParentAutoLayoutFromLayout(parent);
                        }

                        number? itemSpacing = parent.itemSpacing;
                        if (itemSpacing.HasPositive()) AddItemSpacing(parent, itemSpacing!.Value);
                    }
                    else
                    {
                        if (@base is DefaultFrameMixin { layoutMode: not null } frame)
                        {
                            AddSizeFromConstraint(parent, (frame.layoutMode == LayoutMode.HORIZONTAL ? frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED)) ? layout.absoluteBoundingBox.width : Unit.Auto, (frame.layoutMode == LayoutMode.VERTICAL ? frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED)) ? layout.absoluteBoundingBox.height : Unit.Auto);
                        }
                        else
                        {
                            AddSizeFromConstraint(parent, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
                        }
                    }
                }

                if (@base is TextNode textNode && textNode.style.textAutoResize.HasValue) OverwriteSizeFromTextNode(textNode);
                AddNonInsideBorder();
            }

            void AddFillStyle(IEnumerable<Paint> fills)
            {
                foreach (Paint fill in fills)
                {
                    if (fill is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                    {
                        backgroundColor = new ColorProperty(solid.color, solid.opacity);
                    }

                    if (fill is GradientPaint gradient && gradient.visible.IsEmptyOrTrue())
                    {
                        (bool valid, string url) = getAssetPath(gradient.GetHash(), "svg");
                        if (valid) backgroundImage = $"url('{url}')";
                    }

                    if (fill is ImagePaint image && image.visible.IsEmptyOrTrue())
                    {
                        (bool valid, string url) = getAssetPath(image.imageRef, "png");
                        if (valid) backgroundImage = $"url('{url}')";

                        switch (image.scaleMode)
                        {
                            case global.ScaleMode.FILL:
                                unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
                                break;

                            case global.ScaleMode.FIT:
                                unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                                break;
                        }
                    }
                }
            }
            void AddTextFillStyle(IEnumerable<Paint> fills)
            {
                foreach (Paint fill in fills)
                    if (fill is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                        color = new ColorProperty(solid.color, solid.opacity);
            }
            void AddStrokeFillStyle(IEnumerable<Paint> strokes)
            {
                foreach (Paint stroke in strokes)
                    if (stroke is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                        borderColor = new ColorProperty(solid.color, solid.opacity);
            }
            void AddTextStyle(TextNode.Style style)
            {
                bool TryGetFontWithExtension(string font, out string resource, out string url)
                {
                    (bool ttf, string ttfPath) = getAssetPath(font, "ttf");
                    if (ttf)
                    {
                        resource = $"url('{ttfPath}')";
                        url = ttfPath;
                        return true;
                    }

                    (bool otf, string otfPath) = getAssetPath(font, "otf");
                    if (otf)
                    {
                        resource = $"url('{otfPath}')";
                        url = otfPath;
                        return true;
                    }

                    resource = "resource('Inter-Regular')";
                    url = ttfPath;
                    return false;
                }

                void AddUnityFont()
                {
                    string weightPostfix = style.fontWeight.HasValue ? fontWeights[(int)(style.fontWeight / 100) - 1] :
                        style.fontPostScriptName.Contains('-') ? style.fontPostScriptName.Split('-')[1].Replace("Index", "") : "";
                    string italicPostfix = style.italic.HasValue && style.italic.Value || style.fontPostScriptName.Contains("Italic") ? "Italic" : string.Empty;

                    bool valid;
                    if (!TryGetFontWithExtension($"{style.fontFamily}-{weightPostfix}{italicPostfix}", out string resource, out string url) && !TryGetFontWithExtension(style.fontPostScriptName, out resource, out url))
                        unityFontMissing = $"url('{url}')";

                    unityFont = resource;
                    (valid, url) = getAssetPath($"{style.fontFamily}-{weightPostfix}{italicPostfix}", "asset");
                    if (valid) unityFontDefinition = $"url('{url}')";
                }
                void AddTextAlign()
                {
                    string horizontal = style.textAlignHorizontal switch
                    {
                        TextAlignHorizontal.LEFT => "left",
                        TextAlignHorizontal.RIGHT => "right",
                        TextAlignHorizontal.CENTER => "center",
                        TextAlignHorizontal.JUSTIFIED => "center",
                        _ => throw new NotSupportedException()
                    };
                    string vertical = style.textAlignVertical switch
                    {
                        TextAlignVertical.TOP => "upper",
                        TextAlignVertical.BOTTOM => "lower",
                        TextAlignVertical.CENTER => "middle",
                        _ => throw new NotSupportedException()
                    };
                    unityTextAlign = $"{vertical}-{horizontal}";
                }

                if (style.fontSize.HasValue) fontSize = style.fontSize;
                if (style.fontPostScriptName.NullOrEmpty() && style.fontFamily == "Inter") style.fontPostScriptName = "Inter-Regular";
                if (style.fontPostScriptName.NotNullOrEmpty()) AddUnityFont();
                if (style.textAlignVertical.HasValue && style.textAlignHorizontal.HasValue) AddTextAlign();
            }
            void AddTextNodeEffects(IEnumerable<Effect> effects)
            {
                foreach (Effect effect in effects)
                {
                    if (effect is ShadowEffect { visible: true } shadowEffect)
                    {
                        textShadow = new ShadowProperty(shadowEffect.offset.x, shadowEffect.offset.y, shadowEffect.radius, shadowEffect.color);
                    }
                }
            }
            void AddNodeEffects(IEnumerable<Effect> effects)
            {
                foreach (Effect effect in effects)
                {
                    if (effect is ShadowEffect { visible: true } shadowEffect)
                    {
                        boxShadow = new ShadowProperty(shadowEffect.offset.x, shadowEffect.offset.y, shadowEffect.radius, shadowEffect.color);
                    }
                }
            }
            #endregion

            #region Support Methods
            bool Has(string name) => attributes.ContainsKey(name);
            string Get(string name)
            {
                return attributes[name];
            }
            string Get1(string name, string group, int index)
            {
                if (Has(group))
                {
                    Length4Property length4 = attributes[group];
                    return length4[index];
                }

                if (Has(name)) return attributes[name];
                throw new NotSupportedException();
            }
            string Get4(string name, params string[] names)
            {
                if (Has(name)) return attributes[name];
                LengthProperty[] properties = new LengthProperty[4];
                for (int i = 0; i < 4; ++i)
                {
                    if (Has(names[i]))
                    {
                        properties[i] = attributes[names[i]];
                    }
                    else
                    {
                        properties[i] = new LengthProperty(Unit.Pixel);
                    }
                }

                return new Length4Property(properties);
            }
            void Set(string name, string value)
            {
                attributes[name] = value;
            }
            void Set1(string name, string value, params string[] names)
            {
                attributes[name] = value;

                for (int i = 0; i < 4; ++i)
                    if (Has(names[i]))
                        attributes.Remove(names[i]);
            }
            void Set4(string name, string value, string group, int index)
            {
                if (Has(group))
                {
                    Length4Property length4 = Get(group);
                    length4[index] = value;
                    Set(group, length4);
                }
                else
                {
                    Set(name, value);
                }
            }

            static bool HasMixedCenterChildren(DefaultFrameMixin mixin)
            {
                (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
                return horizontalCenterCount > 0 && verticalCenterCount > 0;
            }
            static bool HasAnyCenterChildren(DefaultFrameMixin mixin)
            {
                (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
                return horizontalCenterCount > 0 || verticalCenterCount > 0;
            }
            static bool HasManyCenterChildren(DefaultFrameMixin mixin)
            {
                (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
                return horizontalCenterCount > 1 || verticalCenterCount > 1;
            }
            static bool IsMostlyHorizontal(DefaultFrameMixin mixin)
            {
                (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
                return horizontalCenterCount > verticalCenterCount;
            }
            static (int, int) CenterChildrenCount(DefaultFrameMixin mixin)
            {
                if (mixin.layoutMode.HasValue) return (0, 0);

                int horizontalCenterCount = mixin.children.Cast<ConstraintMixin>().Count(x => x.constraints.horizontal == ConstraintHorizontal.CENTER);
                int verticalCenterCount = mixin.children.Cast<ConstraintMixin>().Count(x => x.constraints.vertical == ConstraintVertical.CENTER);
                return (horizontalCenterCount, verticalCenterCount);
            }
            #endregion
        }

        class UssWriter
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

        class UxmlWriter
        {
            const string prefix = "unity";
            static readonly XmlWriterSettings xmlWriterSettings = new() { OmitXmlDeclaration = true, Indent = true, IndentChars = "    ", NewLineOnAttributes = true };

            #region Fields
            readonly Func<BaseNode, string> getClassList;
            readonly Func<BaseNode, bool> enabledInHierarchy;
            readonly Func<BaseNode, (bool hash, string value)> getTemplate;
            readonly Func<BaseNode, (ElementType type, string typeFullName)> getElementType;

            readonly string documentFolder;
            readonly string documentName;
            readonly XmlWriter documentXml;
            #endregion

            #region Constructors
            public UxmlWriter(DocumentNode document, string folder, string name, Func<BaseNode, string> getClassList, Func<BaseNode, bool> enabledInHierarchy, Func<BaseNode, (bool hash, string value)> getTemplate, Func<BaseNode, (ElementType type, string typeFullName)> getElementType)
            {
                this.getClassList = getClassList;
                this.enabledInHierarchy = enabledInHierarchy;
                this.getTemplate = getTemplate;
                this.getElementType = getElementType;

                documentFolder = folder;
                documentName = name;
                using (documentXml = CreateXml(documentFolder, documentName)) WriteRecursively(document, documentXml);
            }
            #endregion

            #region Methods
            void WriteRecursively(BaseNode node, XmlWriter uxml)
            {
                void WriteDocumentNode(DocumentNode documentNode, XmlWriter writer)
                {
                    writer.WriteStartElement(prefix, "UXML", "UnityEngine.UIElements");
                    WriteStart(documentNode, writer);

                    writer.WriteStartElement("Style");
                    writer.WriteAttributeString("src", $"{documentName}.uss");
                    writer.WriteEndElement();

                    foreach (CanvasNode canvasNode in documentNode.children) WriteRecursively(canvasNode, writer);

                    WriteEnd(writer);
                    writer.WriteEndElement();
                }
                void WriteCanvasNode(CanvasNode canvasNode, XmlWriter writer)
                {
                    WriteStart(canvasNode, writer);
                    foreach (SceneNode child in canvasNode.children) WriteRecursively(child, writer);
                    WriteEnd(writer);
                }
                void WriteSliceNode(SliceNode sliceNode, XmlWriter writer)
                {
                    WriteStart(sliceNode, writer);
                    WriteEnd(writer);
                }
                void WriteTextNode(TextNode textNode, XmlWriter writer)
                {
                    WriteStart(node, writer);
                    switch (textNode.style.textCase)
                    {
                        case TextCase.UPPER:
                            writer.WriteAttributeString("text", textNode.characters.ToUpper());
                            break;

                        case TextCase.LOWER:
                            writer.WriteAttributeString("text", textNode.characters.ToLower());
                            break;

                        default:
                            writer.WriteAttributeString("text", textNode.characters);
                            break;
                    }

                    WriteEnd(writer);
                }
                void WriteDefaultFrameNode(DefaultFrameNode defaultFrameNode, XmlWriter writer)
                {
                    string tooltip = default;
                    if (getTemplate(defaultFrameNode) is (var hash, { } template) && template.NotNullOrEmpty())
                    {
                        if (hash) tooltip = template;
                        using (XmlWriter elementUxml = CreateXml(Path.Combine(documentFolder, elements), template))
                        {
                            elementUxml.WriteStartElement(prefix, "UXML", "UnityEngine.UIElements");
                            WriteStart(defaultFrameNode, elementUxml);
                            foreach (SceneNode child in defaultFrameNode.children) WriteRecursively(child, elementUxml);
                            WriteEnd(elementUxml);
                            elementUxml.WriteEndElement();
                        }

                        writer.WriteStartElement(prefix, "Template", "UnityEngine.UIElements");
                        writer.WriteAttributeString("name", template);
                        writer.WriteAttributeString("src", writer == documentXml ? $"{elements}\\{template}.uxml" : $"{template}.uxml");
                        writer.WriteEndElement();
                    }

                    WriteStart(defaultFrameNode, writer);
                    if (tooltip.NotNullOrEmpty()) writer.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
                    foreach (SceneNode child in defaultFrameNode.children) WriteRecursively(child, writer);
                    WriteEnd(writer);
                }
                void WriteDefaultShapeNode(DefaultShapeNode defaultShapeNode, XmlWriter writer)
                {
                    string tooltip = default;
                    if (getTemplate(defaultShapeNode) is (var hash, { } template) && template.NotNullOrEmpty())
                    {
                        if (hash) tooltip = template;
                        using (XmlWriter elementUxml = CreateXml(Path.Combine(documentFolder, "Elements"), template))
                        {
                            elementUxml.WriteStartElement(prefix, "UXML", "UnityEngine.UIElements");
                            WriteStart(defaultShapeNode, elementUxml);
                            WriteEnd(elementUxml);
                            elementUxml.WriteEndElement();
                        }

                        writer.WriteStartElement(prefix, "Template", "UnityEngine.UIElements");
                        writer.WriteAttributeString("name", template);
                        writer.WriteAttributeString("src", writer == documentXml ? $"Elements\\{template}.uxml" : $"{template}.uxml");
                        writer.WriteEndElement();
                    }

                    WriteStart(defaultShapeNode, writer);
                    if (tooltip.NotNullOrEmpty()) writer.WriteAttributeString("tooltip", tooltip!); // Use tooltip as a storage for hash template name
                    WriteEnd(writer);
                }

                if (!IsVisible(node)) return;
                if (!enabledInHierarchy(node)) return;
                if (IsStateNode(node)) return;

                if (node is DocumentNode document) WriteDocumentNode(document, uxml);
                if (node is CanvasNode canvas) WriteCanvasNode(canvas, uxml);
                if (node is FrameNode frame) WriteDefaultFrameNode(frame, uxml);
                if (node is GroupNode group) WriteDefaultFrameNode(group, uxml);
                if (node is SliceNode slice) WriteSliceNode(slice, uxml);
                if (node is RectangleNode rectangle) WriteDefaultShapeNode(rectangle, uxml);
                if (node is LineNode line) WriteDefaultShapeNode(line, uxml);
                if (node is EllipseNode ellipse) WriteDefaultShapeNode(ellipse, uxml);
                if (node is RegularPolygonNode regularPolygon) WriteDefaultShapeNode(regularPolygon, uxml);
                if (node is StarNode star) WriteDefaultShapeNode(star, uxml);
                if (node is VectorNode vector) WriteDefaultShapeNode(vector, uxml);
                if (node is TextNode text) WriteTextNode(text, uxml);
                if (node is ComponentSetNode componentSet) WriteDefaultFrameNode(componentSet, uxml);
                if (node is ComponentNode component) WriteDefaultFrameNode(component, uxml);
                if (node is InstanceNode instance) WriteDefaultFrameNode(instance, uxml);
                if (node is BooleanOperationNode booleanOperation) WriteDefaultFrameNode(booleanOperation, uxml);
            }
            #endregion

            #region Support Methods
            XmlWriter CreateXml(string folder, string name) => XmlWriter.Create(Path.Combine(folder, $"{name}.uxml"), xmlWriterSettings);

            void WriteStart(BaseNode node, XmlWriter uxml)
            {
                (string prefix, string elementName, string pickingMode) GetElementData(BaseNode node)
                {
                    string prefix = UxmlWriter.prefix;
                    string elementName = "VisualElement";
                    string pickingMode = "Ignore";

                    (ElementType elementType, string elementTypeFullName) = getElementType(node);
                    if (elementType == ElementType.IElement)
                    {
                        prefix = default;
                        elementName = elementTypeFullName;
                        pickingMode = "Position";
                    }
                    else if (elementType == ElementType.None)
                    {
                        switch (node)
                        {
                            case TextNode when node.name.StartsWith("Inputs"):
                                elementName = "TextField";
                                pickingMode = "Position";
                                break;

                            case TextNode:
                                elementName = "Label";
                                break;
                        }

                        switch (node)
                        {
                            case DefaultFrameNode:
                            case TextNode:
                                if (node.name.StartsWith("Buttons"))
                                {
                                    elementName = "Button";
                                    pickingMode = "Position";
                                }

                                if (node.name.StartsWith("Toggles"))
                                {
                                    elementName = "Toggle";
                                    pickingMode = "Position";
                                }

                                if (node.name.StartsWith("ScrollViews"))
                                {
                                    elementName = "ScrollView";
                                    pickingMode = "Position";
                                }

                                break;
                        }
                    }
                    else
                    {
                        elementName = elementType.ToString();
                        switch (elementType)
                        {
                            case ElementType.VisualElement:
                            case ElementType.BindableElement:
                            case ElementType.Box:
                            case ElementType.TextElement:
                            case ElementType.Label:
                            case ElementType.Image:
                                pickingMode = "Ignore";
                                break;

                            default:
                                pickingMode = "Position";
                                break;
                        }
                    }

                    return (prefix, elementName, pickingMode);
                }
                (string prefix, string elementName, string pickingMode) = GetElementData(node);

                if (prefix.NotNullOrEmpty()) uxml.WriteStartElement(prefix, elementName, "UnityEngine.UIElements");
                else uxml.WriteStartElement(elementName);

                uxml.WriteAttributeString("id", node.id);
                uxml.WriteAttributeString("name", node.name);
                string classList = getClassList(node);
                if (classList.NotNullOrEmpty()) uxml.WriteAttributeString("class", classList);
                if (pickingMode != "Position") uxml.WriteAttributeString("picking-mode", pickingMode);
            }
            void WriteEnd(XmlWriter uxml) => uxml.WriteEndElement();
            #endregion
        }

        #region Fields
        readonly DocumentNode document;
        readonly Dictionary<string, Style> documentStyles;

        readonly List<ComponentNode> components = new();
        readonly List<Dictionary<string, Style>> componentsStyles = new();

        readonly List<(StyleSlot slot, UssStyle style)> styles = new();
        readonly Dictionary<BaseNode, UssStyle> componentStyleMap = new();
        readonly Dictionary<BaseNode, UssStyle> nodeStyleMap = new();
        #endregion

        #region Properties
        internal List<string> MissingComponents { get; } = new();
        internal List<BaseNode> ImageFillNodes { get; } = new();
        internal List<BaseNode> PngNodes { get; } = new();
        internal List<BaseNode> SvgNodes { get; } = new();
        internal Dictionary<string, GradientPaint> Gradients { get; } = new();
        #endregion

        #region Constructors
        internal FigmaParser(DocumentNode document, Dictionary<string, Style> documentStyles, Func<BaseNode, bool> enabledInHierarchy)
        {
            this.document = document;
            this.documentStyles = documentStyles;

            foreach (CanvasNode canvas in document.children)
            {
                AddMissingNodesRecursively(canvas);
                AddImageFillsRecursively(canvas, enabledInHierarchy);
                AddPngNodesRecursively(canvas, enabledInHierarchy);
                AddSvgNodesRecursively(canvas, enabledInHierarchy);
                AddGradientsRecursively(canvas, enabledInHierarchy);
            }
        }
        #endregion

        #region Methods
        internal void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }
        internal void Run(Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize)
        {
            AddStylesRecursively(document, documentStyles, false, getAssetPath, getAssetSize);
            foreach (CanvasNode canvas in document.children) AddStylesRecursively(canvas, documentStyles, false, getAssetPath, getAssetSize);
            foreach ((ComponentNode component, int index) in components.Select((x, i) => (x, i))) AddStylesRecursively(component, componentsStyles[index], true, getAssetPath, getAssetSize);

            InheritStylesRecursively(document);
            foreach (CanvasNode canvas in document.children) InheritStylesRecursively(canvas);
        }
        internal void Write(string folder, string name, Func<BaseNode, bool> enabledInHierarchy, Func<BaseNode, (bool hash, string value)> getTemplate, Func<BaseNode, (ElementType type, string typeFullName)> getElementType)
        {
            void GroupRenameStyles(IEnumerable<UssStyle> styles)
            {
                string[] unitsMap = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                string[] tensMap = { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
                string NumberToWords(int number)
                {
                    if (number == 0) return "zero";
                    if (number < 0) return $"minus-{NumberToWords(Math.Abs(number))}";

                    string words = "";

                    if (number / 1000000 > 0)
                    {
                        words += $"{NumberToWords(number / 1000000)}-million ";
                        number %= 1000000;
                    }

                    if (number / 1000 > 0)
                    {
                        words += $"{NumberToWords(number / 1000)}-thousand ";
                        number %= 1000;
                    }

                    if (number / 100 > 0)
                    {
                        words += $"{NumberToWords(number / 100)}-hundred ";
                        number %= 100;
                    }

                    if (number > 0)
                    {
                        if (words != "") words += "and-";
                        if (number < 20)
                        {
                            words += unitsMap[number];
                        }
                        else
                        {
                            words += tensMap[number / 10];
                            if (number % 10 > 0) words += $"-{unitsMap[number % 10]}";
                        }
                    }

                    return words;
                }

                foreach (IGrouping<string, UssStyle> group in styles.GroupBy(x => x.Name).Where(y => y.Count() > 1))
                {
                    int i = 0;
                    foreach (UssStyle style in group) style.Name += $"-{NumberToWords(i++ + 1)}";
                }
            }
            void FixStateStyles(IEnumerable<BaseNode> nodes)
            {
                static string GetState(string value) => value.Substring(value.LastIndexOf(":", StringComparison.Ordinal), value.Length - value.LastIndexOf(":", StringComparison.Ordinal));

                foreach (BaseNode node in nodes)
                {
                    if (node.parent is ChildrenMixin parent)
                    {
                        foreach (SceneNode child in parent.children)
                        {
                            if (IsStateNode(child) && IsStateNode(child, node))
                            {
                                UssStyle childStyle = GetStyle(child);
                                if (childStyle is not null) childStyle.Name = $"{nodeStyleMap[node].Name}{GetState(childStyle.Name)}";
                            }
                        }
                    }
                }
            }

            KeyValuePair<BaseNode, UssStyle>[] nodeStyleFiltered = nodeStyleMap.Where(x => IsVisible(x.Key) && enabledInHierarchy(x.Key)).ToArray();
            UssStyle[] nodeStyleStatelessFiltered = nodeStyleFiltered.Where(x => !IsStateNode(x.Key)).Select(x => x.Value).ToArray();
            UssStyle[] stylesFiltered = styles.Select(x => x.style).Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();
            UssStyle[] componentStyleFiltered = componentStyleMap.Values.Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();

            GroupRenameStyles(stylesFiltered.Union(componentStyleFiltered).Union(nodeStyleStatelessFiltered));
            FixStateStyles(nodeStyleFiltered.Select(x => x.Key));

#pragma warning disable S1481
            UxmlWriter _ = new(document, folder, name, GetClassList, enabledInHierarchy, getTemplate, getElementType);
            using StreamWriter uss = new(Path.Combine(folder, $"{name}.uss"));
            UssWriter __ = new(stylesFiltered, componentStyleFiltered, nodeStyleFiltered.Select(x => x.Value), uss);
#pragma warning restore S1481
        }

        void AddMissingNodesRecursively(BaseNode node)
        {
            if (node is InstanceNode instance && FindNode(instance.componentId) is null)
                MissingComponents.Add(instance.componentId);

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    AddMissingNodesRecursively(child);
        }
        void AddImageFillsRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node)) return;
            if (!enabledInHierarchy(node)) return;

            if (node is BooleanOperationNode) return;
            if (!IsSvgNode(node) && HasImageFill(node)) ImageFillNodes.Add(node);

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    AddImageFillsRecursively(child, enabledInHierarchy);
        }
        void AddPngNodesRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node)) return;
            if (!enabledInHierarchy(node)) return;

            if (IsSvgNode(node) && HasImageFill(node)) PngNodes.Add(node);
            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    AddPngNodesRecursively(child, enabledInHierarchy);
        }
        void AddSvgNodesRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node)) return;
            if (!enabledInHierarchy(node)) return;

            if (IsSvgNode(node) && !HasImageFill(node)) SvgNodes.Add(node);

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case ChildrenMixin children:
                {
                    foreach (SceneNode child in children.children)
                        AddSvgNodesRecursively(child, enabledInHierarchy);
                    return;
                }
            }
        }
        void AddGradientsRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node)) return;
            if (!enabledInHierarchy(node)) return;

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case GeometryMixin geometry:
                {
                    foreach (GradientPaint gradient in geometry.fills.OfType<GradientPaint>())
                        Gradients.TryAdd(gradient.GetHash(), gradient);
                    break;
                }
            }

            if (node is not ChildrenMixin children) return;
            foreach (SceneNode child in children.children)
                AddGradientsRecursively(child, enabledInHierarchy);
        }
        void AddStylesRecursively(BaseNode node, Dictionary<string, Style> styles, bool insideComponent, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize)
        {
            string GetClassName(string name, bool state, string prefix = "n")
            {
                if (name.Length > 64) name = name[..64];
                name = Regex.Replace(name, $"[^a-zA-Z0-9{(state ? ":" : "")}]", "-");

                for (int i = 0; i < 10; ++i)
                    if (name.Contains("--"))
                        name = name.Replace("--", "-");
                for (int i = 0; i < 10; ++i)
                    if (name.EndsWith('-'))
                        name = name[..^1];
                for (int i = 0; i < 10; ++i)
                    if (name.StartsWith('-'))
                        name = name.Substring(1, name.Length - 1);
                if (name.All(x => x == '-')) name = $"{prefix}";
                if (name.Length > 0 && char.IsDigit(name[0])) name = $"{prefix}-{name}";

                return name;
            }

            if (node is ComponentNode) insideComponent = true;

            if (insideComponent) componentStyleMap[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);
            else nodeStyleMap[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);

            if (node is BlendMixin { styles: not null } blend)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s')) slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = styles[id].key;

                    StyleSlot style = new(text, slot, styles[id]);
                    if (!this.styles.Any(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key))
                        this.styles.Add((style, new UssStyle(GetClassName(style.name, false, "s"), getAssetPath, getAssetSize, style.Slot, style.styleType, node)));
                }
            }

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case ChildrenMixin children:
                {
                    foreach (SceneNode child in children.children)
                        AddStylesRecursively(child, styles, insideComponent, getAssetPath, getAssetSize);
                    return;
                }
            }
        }

        BaseNode FindNode(string id)
        {
            BaseNode Find(BaseNode root)
            {
                if (root is ChildrenMixin children)
                    foreach (SceneNode child in children.children)
                    {
                        if (child.id == id) return child;

                        BaseNode node = Find(child);
                        if (node is not null) return node;
                    }

                return default;
            }

            if (document.id == id) return document;

            foreach (CanvasNode canvas in document.children)
            {
                if (canvas.id == id) return canvas;

                BaseNode node = Find(canvas);
                if (node is not null) return node;
            }

            return default;
        }
        UssStyle GetStyle(BaseNode node)
        {
            if (componentStyleMap.TryGetValue(node, out UssStyle style)) return style;
            return nodeStyleMap.TryGetValue(node, out style) ? style : default;
        }
        void InheritStylesRecursively(BaseNode node)
        {
            UssStyle style = GetStyle(node);
            UssStyle component = default;
            List<UssStyle> styles = new();

            if (IsStateNode(node) && node.parent is ChildrenMixin parent)
            {
                BaseNode normalNode = Array.Find(parent.children, x => IsStateNode(node, x));
                if (normalNode is not null) component = GetStyle(normalNode);
            }

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);
                if (componentNode is not null) component = GetStyle(componentNode);
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);
                    if (componentNode is not null) component = GetStyle(componentNode);
                }
            }

            if (node is BlendMixin { styles: not null } blend)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s')) slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = default;
                    if (documentStyles.TryGetValue(id, out Style documentStyle)) key = documentStyle.key;
                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.TryGetValue(id, out Style value))
                            key = value.key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key)) >= 0)
                        styles.Add(this.styles[index].style);
                }
            }

            if (component is not null && styles.Count > 0) style.Inherit(component, styles);
            else if (component is not null) style.Inherit(component);
            else if (styles.Count > 0) style.Inherit(styles);

            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    InheritStylesRecursively(child);
        }
        string GetClassList(BaseNode node)
        {
            string classList = "";
            UssStyle style = GetStyle(node);
            if (style is null) return classList;

            string component = "";
            List<string> styles = new();

            if (IsStateNode(node) && node.parent is ChildrenMixin parent)
            {
                BaseNode normalNode = Array.Find(parent.children, x => IsStateNode(node, x));
                if (normalNode is not null) component = GetStyle(normalNode).Name;
            }

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);
                if (componentNode is not null) component = GetStyle(componentNode).Name;
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);
                    if (componentNode is not null) component = GetStyle(componentNode).Name;
                }
            }

            if (node is BlendMixin { styles: not null } blend)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s')) slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = default;

                    if (documentStyles.TryGetValue(id, out Style documentStyle)) key = documentStyle.key;

                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.TryGetValue(id, out Style value))
                            key = value.key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key)) >= 0)
                        styles.Add(this.styles[index].style.Name);
                }
            }

            if (IsSvgNode(node))
            {
                component = default;
                styles.Clear();
            }

            if (component.NotNullOrEmpty() && styles.Count > 0) classList = style.ResolveClassList(component, styles);
            else if (component.NotNullOrEmpty()) classList = style.ResolveClassList(component);
            else if (styles.Count > 0) classList = style.ResolveClassList(styles);
            else classList = style.ResolveClassList();

            if (IsRootNode(node)) classList += $"{(classList == "" ? "" : " ")}{UssStyle.viewportClass}";
            return $"{UssStyle.overrideClass} {classList}";
        }
        #endregion

        #region Support Methods
        static bool IsRootNode(BaseNodeMixin mixin) => mixin is DocumentNode || mixin is CanvasNode || mixin.parent is CanvasNode || mixin is ComponentNode || mixin.parent is ComponentNode;
        static bool IsVisible(BaseNodeMixin mixin)
        {
            if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;
            return mixin.parent is null || IsVisible(mixin.parent);
        }
        static bool HasImageFill(BaseNodeMixin mixin) => mixin is GeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);
        static bool IsSvgNode(BaseNodeMixin mixin) => mixin is LineNode || mixin is EllipseNode || mixin is RegularPolygonNode || mixin is StarNode || mixin is VectorNode || (mixin is BooleanOperationNode && IsBooleanOperationVisible(mixin));
        static bool IsBooleanOperationVisible(BaseNodeMixin node)
        {
            if (node is not ChildrenMixin children) return false;

            foreach (SceneNode child in children.children)
            {
                if (child is not BooleanOperationNode && IsVisible(child) && IsSvgNode(child)) return true;
                if (child is BooleanOperationNode) return IsBooleanOperationVisible(child);
            }

            return false;
        }
        static bool IsStateNode(BaseNodeMixin mixin) => mixin.name.EndsWith(":hover") || mixin.name.EndsWith(":active") || mixin.name.EndsWith(":inactive") || mixin.name.EndsWith(":focus") || mixin.name.EndsWith(":selected") || mixin.name.EndsWith(":disabled") || mixin.name.EndsWith(":enabled") || mixin.name.EndsWith(":checked") || mixin.name.EndsWith(":root");
        static bool IsStateNode(BaseNodeMixin mixin, BaseNodeMixin normal) => mixin.name[..mixin.name.LastIndexOf(":", StringComparison.Ordinal)] == normal.name;
        #endregion
    }
}