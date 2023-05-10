using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Trackman;
using UnityEngine;

namespace Figma
{
    using global;
    using number = Double;

    public class FigmaParser
    {
        class StyleSlot : Style
        {
            #region Fields
            public bool text;
            public string slot;
            #endregion

            #region Constructors
            public StyleSlot(bool text, string slot, Style style)
            {
                this.text = text;
                this.slot = slot;
                styleType = style.styleType;
                key = style.key;
                name = style.name;
                description = style.description;
            }
            #endregion

            #region Methods
            public override string ToString()
            {
                return $"text={text} slot={slot} styleType={styleType} key={key} name={name} description={description}";
            }
            #endregion
        }

        class UssStyle
        {
            public const string viewportClass = "unity-viewport";
            public const string overrideClass = "unity-base-override";
            static readonly CultureInfo defaultCulture = CultureInfo.GetCultureInfo("en-US");

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
            struct LengthProperty
            {
                #region Fields
                internal number value;
                internal Unit unit;
                #endregion

                #region Constructors
                internal LengthProperty(Unit unit)
                {
                    this.value = default;
                    this.unit = unit;
                }
                internal LengthProperty(number value, Unit unit)
                {
                    this.value = value;
                    this.unit = unit;
                }
                #endregion

                #region Operators
                public static implicit operator LengthProperty(Unit value)
                {
                    return new LengthProperty(default, value);
                }
                public static implicit operator LengthProperty(number? value)
                {
                    return new LengthProperty(value.Value, Unit.Pixel);
                }
                public static implicit operator LengthProperty(number value)
                {
                    return new LengthProperty(value, Unit.Pixel);
                }
                public static implicit operator LengthProperty(string value)
                {
                    if (Enum.TryParse(value, true, out Unit unit)) return new LengthProperty(unit);
                    else if (value.Contains("px")) return new LengthProperty(number.Parse(value.ToLower().Replace("px", ""), defaultCulture), Unit.Pixel);
                    else if (value.Contains("deg")) return new LengthProperty(number.Parse(value.ToLower().Replace("deg", ""), defaultCulture), Unit.Degrees);
                    else if (value.Contains("%")) return new LengthProperty(number.Parse(value.Replace("%", ""), defaultCulture), Unit.Percent);
                    else throw new NotSupportedException();
                }
                public static implicit operator string(LengthProperty value)
                {
                    switch (value.unit)
                    {
                        case Unit.Pixel: return $"{(int)Math.Round(value.value)}px";
                        case Unit.Degrees: return $"{value.value.ToString("F2", defaultCulture).Replace(".00", "")}deg";
                        case Unit.Percent: return $"{value.value.ToString("F2", defaultCulture).Replace(".00", "")}%";
                        case Unit.Auto: return "auto";
                        case Unit.None: return "none";
                        case Unit.Initial: return "initial";
                        case Unit.Default: return "0px";

                        default:
                            throw new ArgumentException();
                    }
                }

                public static LengthProperty operator +(LengthProperty a) => a;
                public static LengthProperty operator -(LengthProperty a) => new LengthProperty(-a.value, a.unit);
                public static LengthProperty operator +(LengthProperty a, number b) => new LengthProperty(a.value + b, a.unit);
                public static LengthProperty operator -(LengthProperty a, number b) => new LengthProperty(a.value - b, a.unit);

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
                number value;
                #endregion

                #region Constructors
                internal NumberProperty(number value) => this.value = value;
                #endregion

                #region Operators
                public static implicit operator NumberProperty(number? value)
                {
                    return new NumberProperty(value.Value);
                }
                public static implicit operator NumberProperty(number value)
                {
                    return new NumberProperty(value);
                }
                public static implicit operator NumberProperty(string value)
                {
                    return new NumberProperty(number.Parse(value, defaultCulture));
                }
                public static implicit operator string(NumberProperty value)
                {
                    return value.value.ToString("F2", defaultCulture).Replace(".00", "");
                }

                public static NumberProperty operator +(NumberProperty a) => a;
                public static NumberProperty operator -(NumberProperty a) => new NumberProperty(-a.value);
                public static NumberProperty operator +(NumberProperty a, number b) => new NumberProperty(a.value + b);
                public static NumberProperty operator -(NumberProperty a, number b) => new NumberProperty(a.value - b);
                #endregion
            }

            /// <summary>
            /// Represents a whole number.
            /// </summary>
            struct IntegerProperty
            {
                #region Fields
                int value;
                #endregion

                #region Constructors
                internal IntegerProperty(int value) => this.value = value;
                #endregion

                #region Operators
                public static implicit operator IntegerProperty(int? value)
                {
                    return new IntegerProperty(value.Value);
                }
                public static implicit operator IntegerProperty(int value)
                {
                    return new IntegerProperty(value);
                }
                public static implicit operator IntegerProperty(string value)
                {
                    return new IntegerProperty(int.Parse(value));
                }
                public static implicit operator string(IntegerProperty value)
                {
                    return value.value.ToString(defaultCulture);
                }

                public static IntegerProperty operator +(IntegerProperty a) => a;
                public static IntegerProperty operator -(IntegerProperty a) => new IntegerProperty(-a.value);
                public static IntegerProperty operator +(IntegerProperty a, int b) => new IntegerProperty(a.value + b);
                public static IntegerProperty operator -(IntegerProperty a, int b) => new IntegerProperty(a.value - b);
                #endregion
            }

            /// <summary>
            /// Represents a color. You can define a color with a #hexadecimal code, the rgb() or rgba() function, or a color keyword (for example, blue or transparent).
            /// </summary>
            struct ColorProperty
            {
                #region Fields
                string rgba;
                string rgb;
                string hex;
                string name;
                #endregion

                #region Constructors
                internal ColorProperty(RGBA color, number? opacity, float alphaMult = 1.0f)
                {
                    rgba = $"rgba({(byte)(color.r * 255.0f)},{(byte)(color.g * 255.0f)},{(byte)(color.b * 255.0f)},{(color.a * (opacity.HasValue ? opacity.Value : alphaMult)).ToString("F2", defaultCulture).Replace(".00", "")})";
                    rgb = default;
                    hex = default;
                    name = default;
                }
                internal ColorProperty(string value)
                {
                    rgba = default;
                    rgb = default;
                    hex = default;
                    name = default;

                    if (value.StartsWith("rgba")) rgba = value;
                    else if (value.StartsWith("rgb")) rgb = value;
                    else if (value.StartsWith("#")) hex = value;
                    else name = value;
                }
                public ColorProperty(RGBA color) : this(color, 1, 1)
                {
                }
                #endregion

                #region Operators
                public static implicit operator ColorProperty(Unit _)
                {
                    return new ColorProperty();
                }
                public static implicit operator ColorProperty(RGBA value)
                {
                    return new ColorProperty(value);
                }
                public static implicit operator ColorProperty(string value)
                {
                    return new ColorProperty(value);
                }
                public static implicit operator string(ColorProperty value)
                {
                    if (value.rgba.NotNullOrEmpty()) return value.rgba;
                    else if (value.rgb.NotNullOrEmpty()) return value.rgb;
                    else if (value.hex.NotNullOrEmpty()) return value.hex;
                    else if (value.name.NotNullOrEmpty()) return value.name;
                    else return "initial";
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
                string url;
                string resource;
                Unit unit;
                #endregion

                #region Constructors
                internal AssetProperty(Unit unit)
                {
                    this.url = default;
                    this.resource = default;
                    this.unit = unit;
                }
                internal AssetProperty(string value)
                {
                    this.url = default;
                    this.resource = default;
                    this.unit = default;

                    if (value.StartsWith("url")) url = value;
                    else if (value.StartsWith("resource")) resource = value;
                    else throw new NotSupportedException();
                }
                #endregion

                #region Operators
                public static implicit operator AssetProperty(Unit value)
                {
                    return new AssetProperty(value);
                }
                public static implicit operator AssetProperty(string value)
                {
                    if (Enum.TryParse(value, true, out Unit unit)) return new AssetProperty(unit);
                    else return new AssetProperty(value);
                }
                public static implicit operator string(AssetProperty value)
                {
                    if (value.url.NotNullOrEmpty()) return value.url;
                    else if (value.resource.NotNullOrEmpty()) return value.resource;
                    else
                    {
                        switch (value.unit)
                        {
                            case Unit.None: return "none";
                            case Unit.Initial: return "initial";

                            default:
                                throw new ArgumentException();
                        }
                    }
                }
                #endregion
            }

            struct EnumProperty<T> where T : struct, Enum
            {
                static readonly Regex enumParserRegexString = new Regex("(?<name>([a-z]+\\-?))");
                static readonly Regex enumParserRegexValue = new Regex("(?<name>([A-Z][a-z]+)?)");

                #region Fields
                T value;
                Unit unit;
                #endregion

                #region Constructors
                internal EnumProperty(T value)
                {
                    this.value = value;
                    this.unit = Unit.None;
                }
                internal EnumProperty(Unit unit)
                {
                    this.value = default(T);
                    this.unit = unit;
                }
                #endregion

                #region Operators
                public static implicit operator EnumProperty<T>(Unit unit)
                {
                    return new EnumProperty<T>(unit);
                }
                public static implicit operator EnumProperty<T>(T value)
                {
                    return new EnumProperty<T>(value);
                }
                public static implicit operator EnumProperty<T>(string value)
                {
                    if (Enum.TryParse(enumParserRegexString.Replace(value, "${name}").Replace("-", ""), true, out T result)) return new EnumProperty<T>(result);
                    else throw new ArgumentException();
                }
                public static implicit operator string(EnumProperty<T> value)
                {
                    if (value.unit == Unit.None) return enumParserRegexValue.Replace(value.value.ToString(), "${name}-").ToLower().TrimEnd('-');
                    else return "initial";
                }

                public static bool operator ==(EnumProperty<T> a, T b) => a.value.Equals(b);
                public static bool operator !=(EnumProperty<T> a, T b) => !a.value.Equals(b);

                public override bool Equals(object obj) => throw new NotSupportedException();
                public override int GetHashCode() => HashCode.Combine(value, unit);
                public override string ToString() => this;
                #endregion
            }

            struct ShadowProperty
            {
                #region Fields
                LengthProperty offsetHorizontal;
                LengthProperty offsetVertical;
                LengthProperty blurRadius;
                ColorProperty color;
                #endregion

                #region Constructors
                public ShadowProperty(LengthProperty offsetHorizontal, LengthProperty offsetVertical, LengthProperty blurRadius, ColorProperty color)
                {
                    this.offsetHorizontal = offsetHorizontal;
                    this.offsetVertical = offsetVertical;
                    this.blurRadius = blurRadius;
                    this.color = color;
                }
                internal ShadowProperty(string value)
                {
                    Regex regex = new Regex(@"(?<offsetHorizontal>\d+[px]*)\s+(?<offsetVertical>\d+[px]*)\s+(?<blurRadius>\d+[px]*)\s+(?<color>(rgba\([\d,\.\s]+\))|#\w{2,8}|[^#][\w-]+)");
                    Match match = regex.Match(value);
                    offsetHorizontal = match.Groups["offsetHorizontal"].Value;
                    offsetVertical = match.Groups["offsetVertical"].Value;
                    blurRadius = match.Groups["blurRadius"].Value;
                    color = match.Groups["color"].Value;
                }
                #endregion

                #region Operators
                public static implicit operator ShadowProperty(string value)
                {
                    return new ShadowProperty(value);
                }
                public static implicit operator string(ShadowProperty value)
                {
                    return $"{value.offsetHorizontal} {value.offsetVertical} {value.blurRadius} {value.color}";
                }
                #endregion
            }

            struct Length4Property
            {
                #region Fields
                Unit unit;
                LengthProperty[] properties;
                #endregion

                #region Properties
                internal LengthProperty this[int index]
                {
                    get => properties[index];
                    set => properties[index] = value;
                }
                #endregion

                #region Constructors
                internal Length4Property(Unit unit)
                {
                    this.unit = unit;
                    this.properties = new LengthProperty[4] { new LengthProperty(unit), new LengthProperty(unit), new LengthProperty(unit), new LengthProperty(unit) };
                }
                internal Length4Property(LengthProperty[] properties)
                {
                    this.unit = Unit.None;
                    this.properties = properties;
                }
                #endregion

                #region Operators
                public static implicit operator Length4Property(Unit unit)
                {
                    return new Length4Property(unit);
                }
                public static implicit operator Length4Property(number? value)
                {
                    return new Length4Property(new LengthProperty[1] { value.Value });
                }
                public static implicit operator Length4Property(number value)
                {
                    return new Length4Property(new LengthProperty[1] { value });
                }
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
                    if (value.unit == Unit.None && value.properties is not null)
                    {
                        string[] values = new string[value.properties.Length];
                        for (int i = 0; i < values.Length; i++) values[i] = value.properties[i];
                        return string.Join(" ", values);
                    }
                    else
                    {
                        return new LengthProperty(value.unit);
                    }
                }

                public static Length4Property operator +(Length4Property a) => a;
                public static Length4Property operator -(Length4Property a) => new Length4Property(a.properties.Select(x => -x).ToArray());
                public static Length4Property operator +(Length4Property a, number b) => new Length4Property(a.properties.Select(x => x + b).ToArray());
                public static Length4Property operator -(Length4Property a, number b) => new Length4Property(a.properties.Select(x => x - b).ToArray());
                #endregion
            }

            struct FlexProperty
            {
                #region Operators
                public static implicit operator FlexProperty(string value)
                {
                    throw new NotImplementedException();
                }
                public static implicit operator string(FlexProperty value)
                {
                    throw new NotImplementedException();
                }
                #endregion
            }

            struct CursorProperty
            {
                #region Operators
                public static implicit operator CursorProperty(string value)
                {
                    throw new NotImplementedException();
                }
                public static implicit operator string(CursorProperty value)
                {
                    throw new NotImplementedException();
                }
                #endregion
            }
            #endregion

            #region Fields
            string name;
            Func<string, string, (bool valid, string path)> getAssetPath;
            Func<string, string, (bool valid, int width, int height)> getAssetSize;

            List<UssStyle> inherited = new List<UssStyle>();
            Dictionary<string, string> defaults = new Dictionary<string, string>();
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            #endregion

            #region Properties
            public string Name { get => name; set => name = value; }
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
                this.name = name;
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
                this.name = name;
                this.getAssetPath = getAssetPath;
                this.getAssetSize = getAssetSize;

                switch (type)
                {
                    case Style.StyleType.FILL:
                        if (node is GeometryMixin geometry)
                        {
                            switch (slot)
                            {
                                case "fill":
                                    if (node is TextNode)
                                    {
                                        this.name += "-Text";
                                        AddTextFillStyle(geometry.fills);
                                    }
                                    else
                                    {
                                        AddFillStyle(geometry.fills);
                                    }
                                    break;

                                case "stroke":
                                    if (node is TextNode)
                                    {
                                        this.name += "-TextStroke";
                                    }
                                    else
                                    {
                                        this.name += "-Border";
                                        AddStrokeFillStyle(geometry.strokes);
                                    }
                                    break;
                            }
                        }
                        break;

                    case Style.StyleType.TEXT:
                        if (node is TextNode text) AddTextStyle(text.style.fontSize, text.style.fontPostScriptName, text.style.textAlignHorizontal, text.style.textAlignVertical);
                        break;

                    case Style.StyleType.EFFECT:
                        if (node is BlendMixin blend) AddNodeEffects(blend.effects);
                        break;

                    case Style.StyleType.GRID:
                        if (node is DefaultFrameMixin grid) AddGridStyle(grid.layoutGrids);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            public UssStyle(string name, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize, BaseNode node)
            {
                this.name = name;
                this.getAssetPath = getAssetPath;
                this.getAssetSize = getAssetSize;

                if (node is DocumentNode document) AddDocumentNode(document);
                if (node is CanvasNode canvas) AddCanvasNode(canvas);
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
            public bool DoesInherit(UssStyle style)
            {
                return inherited.Contains(style);
            }
            public void Inherit(UssStyle component)
            {
                inherited.Add(component);

                foreach (KeyValuePair<string, string> keyValue in component.attributes)
                {
                    if (attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == component.attributes[keyValue.Key])
                        attributes.Remove(keyValue.Key);

                    if (!attributes.ContainsKey(keyValue.Key) && defaults.ContainsKey(keyValue.Key))
                        attributes.Add(keyValue.Key, defaults[keyValue.Key]);
                }
            }
            public void Inherit(IEnumerable<UssStyle> styles)
            {
                inherited.AddRange(styles);

                foreach (UssStyle style in styles)
                {
                    foreach (KeyValuePair<string, string> keyValue in style.attributes)
                        if (attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == style.attributes[keyValue.Key])
                            attributes.Remove(keyValue.Key);
                }
            }
            public void Inherit(UssStyle component, IEnumerable<UssStyle> styles)
            {
                inherited.Add(component);
                inherited.AddRange(styles);

                List<string> preserve = new List<string>();
                foreach (KeyValuePair<string, string> keyValue in component.attributes)
                    foreach (UssStyle style in styles)
                        if (style.attributes.ContainsKey(keyValue.Key) && style.attributes[keyValue.Key] != keyValue.Value) preserve.Add(keyValue.Key);

                foreach (KeyValuePair<string, string> keyValue in component.attributes)
                {
                    if (attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == component.attributes[keyValue.Key])
                        attributes.Remove(keyValue.Key);

                    if (!attributes.ContainsKey(keyValue.Key) && defaults.ContainsKey(keyValue.Key))
                        attributes.Add(keyValue.Key, defaults[keyValue.Key]);
                }

                foreach (UssStyle style in styles)
                {
                    foreach (KeyValuePair<string, string> keyValue in style.attributes)
                        if (attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == style.attributes[keyValue.Key] && !preserve.Contains(keyValue.Key))
                            attributes.Remove(keyValue.Key);
                }
            }
            public string ResolveClassList(string component)
            {
                if (attributes.Count > 0) return $"{name} {component}";
                else return component;
            }
            public string ResolveClassList(IEnumerable<string> styles)
            {
                if (attributes.Count > 0) return $"{name} {string.Join(" ", styles)}";
                else return $"{string.Join(" ", styles)}";
            }
            public string ResolveClassList(string component, IEnumerable<string> styles)
            {
                if (attributes.Count > 0) return $"{name} {component} {string.Join(" ", styles)}";
                else return $"{component} {string.Join(" ", styles)}";
            }
            public string ResolveClassList()
            {
                if (attributes.Count > 0) return $"{name}";
                else return "";
            }
            public void Write(StreamWriter stream)
            {
                stream.WriteLine($".{name} {{");

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
                AddChildren(node);

                if (node.clipsContent.HasValueAndTrue()) overflow = Visibility.Hidden;

                AddGridStyle(node.layoutGrids);
            }
            void AddDefaultShapeNode(DefaultShapeNode node)
            {
                AddBoxModel(node, node, node, node);
                AddScene(node, node);
                AddLayout(node, node);
                AddBlend(node);
                AddGeometry(node, node, node);
                AddExport(node);
                AddReaction(node);
                AddTransition(node);
            }

            void AddDocumentNode(DocumentNode _)
            {
            }
            void AddCanvasNode(CanvasNode node)
            {
                AddChildren(node);
                AddExport(node);
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
                AddScene(node, node);
                AddLayout(node, node);
                AddExport(node);
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
                void FixWhiteSpace()
                {
                    if (node.absoluteBoundingBox.height / node.style.fontSize < 2) whiteSpace = Wrap.Nowrap;
                    else whiteSpace = Wrap.Normal;
                }

                AddDefaultShapeNode(node);

                FixWhiteSpace();
                AddTextStyle(node.style.fontSize, node.style.fontPostScriptName, node.style.textAlignHorizontal, node.style.textAlignVertical);
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
                    number[] padding = new number[] { mixin.paddingTop.HasValue ? mixin.paddingTop.Value : 0, mixin.paddingRight.HasValue ? mixin.paddingRight.Value : 0, mixin.paddingBottom.HasValue ? mixin.paddingBottom.Value : 0, mixin.paddingLeft.HasValue ? mixin.paddingLeft.Value : 0 };
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
                        switch (mixin.primaryAxisAlignItems.Value)
                        {
                            case PrimaryAxisAlignItems.MIN:
                                justifyContent = JustifyContent.FlexStart;
                                break;

                            case PrimaryAxisAlignItems.CENTER:
                                justifyContent = JustifyContent.Center;
                                break;

                            case PrimaryAxisAlignItems.MAX:
                                justifyContent = JustifyContent.FlexEnd;
                                break;

                            case PrimaryAxisAlignItems.SPACE_BETWEEN:
                                justifyContent = JustifyContent.SpaceBetween;
                                break;
                        }
                    }
                    if (mixin.counterAxisAlignItems.HasValue)
                    {
                        switch (mixin.counterAxisAlignItems.Value)
                        {
                            case CounterAxisAlignItems.MIN:
                                alignItems = Align.FlexStart;
                                break;

                            case CounterAxisAlignItems.CENTER:
                                alignItems = Align.Center;
                                break;

                            case CounterAxisAlignItems.MAX:
                                alignItems = Align.FlexEnd;
                                break;
                        }
                    }
                    if (mixin.itemSpacing.HasPositive()) itemSpacing = mixin.itemSpacing;
                }

                AddPadding();
                if (mixin.layoutMode.HasValue) AddAutoLayout();
            }
            void AddScene(SceneNodeMixin _, BaseNodeMixin @base)
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
                if (@base is DefaultFrameMixin frame && IsMostlyHorizontal(frame))
                {
                    flexDirection = FlexDirection.Row;
                }

                if (@base.parent is DefaultFrameMixin parent && parent.layoutMode.HasValue && mixin.layoutAlign == LayoutAlign.STRETCH)
                {
                    alignSelf = Align.Stretch;
                }
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
                    borderWidth = mixin.strokeWeight;
                }
                void AddBorderRadius(RectangleCornerMixin rectangleCornerMixin, CornerMixin cornerMixin)
                {
                    void AddBorderRadius(number minValue, number value)
                    {
                        if (rectangleCornerMixin.rectangleCornerRadii is not null)
                        {
                            for (int i = 0; i < rectangleCornerMixin.rectangleCornerRadii.Length; ++i) rectangleCornerMixin.rectangleCornerRadii[i] = Math.Min(minValue, rectangleCornerMixin.rectangleCornerRadii[i]) + value;
                            borderRadius = rectangleCornerMixin.rectangleCornerRadii;
                        }
                        else if (cornerMixin.cornerRadius.HasPositive()) borderRadius = Math.Min(minValue, cornerMixin.cornerRadius.Value) + value;
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
                    AddBorderRadius(minBorderRadius, value);

                    if (borderRadius == new Length4Property(Unit.Pixel)) attributes.Remove("border-radius");
                }
                void AddRotation()
                {
                    if (layout.relativeTransform[0][0] == 1 && layout.relativeTransform[0][0] == 0 &&
                        layout.relativeTransform[0][0] == 0 && layout.relativeTransform[1][1] == 1)
                    {
                    }
                    else if (layout.relativeTransform[0][0].HasValue && layout.relativeTransform[0][1].HasValue &&
                             layout.relativeTransform[1][0].HasValue && layout.relativeTransform[1][1].HasValue)
                    {
                        float m00 = (float)layout.relativeTransform[0][0].Value;
                        float m01 = (float)layout.relativeTransform[0][1].Value;
                        int rotation = Mathf.RoundToInt(Mathf.Rad2Deg * Mathf.Acos(m00 / Mathf.Sqrt(m00 * m00 + m01 * m01)));
                        if (rotation != 0) rotate = new LengthProperty(rotation, Unit.Degrees);
                    }
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

                    if (mixin.strokes.Length > 0)
                    {
                        AddStrokeFillStyle(mixin.strokes);

                        if (mixin.strokeWeight.HasValue)
                        {
                            AddBorderWidth();
                            if (mixin.strokeAlign.HasValue) AddBorderRadius(mixin as RectangleCornerMixin, mixin as CornerMixin);
                        }
                    }
                }
            }
            void AddCorner(CornerMixin cornerMixin, RectangleCornerMixin rectangleCornerMixin)
            {
                if (rectangleCornerMixin.rectangleCornerRadii is not null) borderRadius = rectangleCornerMixin.rectangleCornerRadii;
                else if (cornerMixin.cornerRadius.HasPositive()) borderRadius = cornerMixin.cornerRadius;
            }
            void AddExport(ExportMixin _)
            {
            }
            void AddReaction(ReactionMixin _)
            {
            }
            void AddTransition(TransitionMixin _)
            {
            }
            void AddChildren(ChildrenMixin _)
            {
            }

            void AddBoxModel(LayoutMixin layout, ConstraintMixin constraint, GeometryMixin geometry, BaseNodeMixin @base)
            {
                void AdjustSvgSize()
                {
                    (bool valid, int width, int height) = getAssetSize(@base.id, "svg");
                    if (valid && width > 0 && height > 0)
                        layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y, width, height);

                    if (geometry.strokes.Length > 0 && geometry.strokeWeight.HasValue && geometry.strokeWeight > 0)
                    {
                        layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.y, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);

                        if (geometry.strokeCap.HasValue && geometry.strokeCap != StrokeCap.NONE)
                            layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
                    }
                }
                void AddSizeByParentAutoLayoutFromAutoLayout(DefaultFrameMixin frame)
                {
                    position = Position.Relative;
                    switch (frame.layoutMode)
                    {
                        case LayoutMode.HORIZONTAL:
                            if (((DefaultFrameMixin)frame.parent).layoutMode == LayoutMode.HORIZONTAL)
                            {
                                width = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : (frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto);
                                height = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : (frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto);
                            }
                            else
                            {
                                width = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : (frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto);
                                height = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : (frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto);
                            }
                            break;

                        case LayoutMode.VERTICAL:
                            if (((DefaultFrameMixin)frame.parent).layoutMode == LayoutMode.VERTICAL)
                            {
                                width = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : (frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto);
                                height = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : (frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto);
                            }
                            else
                            {
                                width = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : (frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.width : Unit.Auto);
                                height = frame.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : (frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) ? frame.absoluteBoundingBox.height : Unit.Auto);
                            }
                            break;
                    }
                    if (layout.layoutGrow.HasPositive()) flexGrow = layout.layoutGrow;
                }
                void AddSizeByParentAutoLayoutFromLayout(DefaultFrameMixin parent)
                {
                    position = Position.Relative;
                    switch (parent.layoutMode)
                    {
                        case LayoutMode.HORIZONTAL:
                            width = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.width;
                            height = layout.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.height;
                            break;

                        case LayoutMode.VERTICAL:
                            width = layout.layoutAlign == LayoutAlign.STRETCH ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.width;
                            height = layout.layoutGrow.HasPositive() ? new LengthProperty(100, Unit.Percent) : layout.absoluteBoundingBox.height;
                            break;
                    }
                    if (layout.layoutGrow.HasPositive()) flexGrow = layout.layoutGrow;
                }
                void AddSizeFromConstraint(DefaultFrameMixin parent, LengthProperty widthProperty, LengthProperty heightProperty)
                {
                    string GetFullPath(BaseNodeMixin node)
                    {
                        if (node.parent is not null) return $"{GetFullPath(node.parent)}/{node.name}";
                        return node.name;
                    }

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
                    if (@base != parent.children.LastOrDefault(x => IsVisible(x)))
                    {
                        if (parent.layoutMode.Value == LayoutMode.HORIZONTAL)
                        {
                            if (!parent.primaryAxisAlignItems.HasValue || parent.primaryAxisAlignItems.Value != PrimaryAxisAlignItems.SPACE_BETWEEN) marginRight = itemSpacing;
                        }
                        else marginBottom = itemSpacing;
                    }
                }
                void OverwriteSizeFromTextNode(TextNode textNode)
                {
                    switch (textNode.style.textAutoResize)
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
                        switch (strokeAlign)
                        {
                            case StrokeAlign.CENTER:
                                return strokeWeight / 2;

                            case StrokeAlign.OUTSIDE:
                                return strokeWeight;

                            default:
                                throw new NotSupportedException();
                        }
                    }

                    if (@base is DefaultFrameMixin value && value.layoutMode.HasValue) return;

                    if (!IsSvgNode(@base) && geometry is not null &&
                        geometry.strokes.Length > 0 && geometry.strokeWeight.HasValue && geometry.strokeWeight > 0 &&
                        geometry.strokeAlign.HasValue && geometry.strokeAlign != StrokeAlign.INSIDE)
                        margin += GetStrokeWeight(geometry.strokeAlign.Value, geometry.strokeWeight.Value);
                }

                if (IsSvgNode(@base)) AdjustSvgSize();

                DefaultFrameMixin parent = @base.parent as DefaultFrameMixin;
                if (IsRootNode(@base))
                {
                }
                else if (parent.layoutMode.HasValue)
                {
                    if (@base is DefaultFrameMixin frame && frame.layoutMode.HasValue)
                    {
                        AddSizeByParentAutoLayoutFromAutoLayout(frame);
                    }
                    else
                    {
                        AddSizeByParentAutoLayoutFromLayout(parent);
                    }

                    number? itemSpacing = parent.itemSpacing;
                    if (itemSpacing.HasPositive()) AddItemSpacing(parent, itemSpacing.Value);
                }
                else
                {
                    if (@base is DefaultFrameMixin frame && frame.layoutMode.HasValue)
                    {
                        AddSizeFromConstraint(parent, (frame.layoutMode == LayoutMode.HORIZONTAL ? frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED)) ? layout.absoluteBoundingBox.width : Unit.Auto, (frame.layoutMode == LayoutMode.VERTICAL ? frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED) : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED)) ? layout.absoluteBoundingBox.height : Unit.Auto);
                    }
                    else
                    {
                        AddSizeFromConstraint(parent, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
                    }
                }

                if (@base is TextNode textNode && textNode.style.textAutoResize.HasValue) OverwriteSizeFromTextNode(textNode);
                AddNonInsideBorder();
            }

            void AddFillStyle(Paint[] fills)
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
            void AddTextFillStyle(Paint[] fills)
            {
                foreach (Paint fill in fills)
                {
                    if (fill is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                    {
                        color = new ColorProperty(solid.color, solid.opacity);
                    }
                    if (fill is GradientPaint gradient && gradient.visible.IsEmptyOrTrue())
                    {
                    }
                    if (fill is ImagePaint image && image.visible.IsEmptyOrTrue())
                    {
                    }
                }
            }
            void AddStrokeFillStyle(Paint[] strokes)
            {
                foreach (Paint stroke in strokes)
                {
                    if (stroke is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                    {
                        borderColor = new ColorProperty(solid.color, solid.opacity);
                    }
                    if (stroke is GradientPaint gradient && gradient.visible.IsEmptyOrTrue())
                    {
                    }
                    if (stroke is ImagePaint image && image.visible.IsEmptyOrTrue())
                    {
                    }
                }
            }
            void AddTextStyle(number? fontSize, string fontPostScriptName, TextAlignHorizontal? textAlignHorizontal, TextAlignVertical? textAlignVertical)
            {
                void AddUnityFont(string fontPostScriptName)
                {
                    (bool valid, string url) = getAssetPath(fontPostScriptName, "ttf");

                    if (valid) unityFont = $"url('{url}')";
                    else
                    {
                        (valid, url) = getAssetPath(fontPostScriptName, "otf");
                        if (valid) unityFont = $"url('{url}')";
                        else
                        {
                            unityFont = "resource('Inter-Regular')";
                            unityFontMissing = $"url('{url}')";
                        }
                    }

                    (valid, url) = getAssetPath(fontPostScriptName, "asset");
                    if (valid) unityFontDefinition = $"url('{url}')";
                }
                void AddTextAlign(TextAlignHorizontal textAlignHorizontal, TextAlignVertical textAlignVertical)
                {
                    string horizontal = textAlignHorizontal switch
                    {
                        TextAlignHorizontal.LEFT => "left",
                        TextAlignHorizontal.RIGHT => "right",
                        TextAlignHorizontal.CENTER => "center",
                        TextAlignHorizontal.JUSTIFIED => "center",
                        _ => throw new NotSupportedException()
                    };
                    string vertical = textAlignVertical switch
                    {
                        TextAlignVertical.TOP => "upper",
                        TextAlignVertical.BOTTOM => "lower",
                        TextAlignVertical.CENTER => "middle",
                        _ => throw new NotSupportedException()
                    };
                    unityTextAlign = $"{vertical}-{horizontal}";
                }

                if (fontSize.HasValue) this.fontSize = fontSize;
                if (fontPostScriptName.NotNullOrEmpty()) AddUnityFont(fontPostScriptName);
                if (textAlignVertical.HasValue && textAlignHorizontal.HasValue) AddTextAlign(textAlignHorizontal.Value, textAlignVertical.Value);
            }
            void AddTextNodeEffects(Effect[] effects)
            {
                foreach (Effect effect in effects)
                {
                    if (effect is ShadowEffect shadowEffect && shadowEffect.visible)
                    {
                        textShadow = new ShadowProperty(shadowEffect.offset.x, shadowEffect.offset.y, shadowEffect.radius, shadowEffect.color);
                    }
                }
            }
            void AddNodeEffects(Effect[] effects)
            {
                foreach (Effect effect in effects)
                {
                    if (effect is ShadowEffect shadowEffect && shadowEffect.visible)
                    {
                        boxShadow = new ShadowProperty(shadowEffect.offset.x, shadowEffect.offset.y, shadowEffect.radius, shadowEffect.color);
                    }
                }
            }
            void AddGridStyle(LayoutGrid[] _)
            {
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
                else
                {
                    if (Has(name)) return attributes[name];
                    else throw new NotSupportedException();
                }
            }
            string Get4(string name, params string[] names)
            {
                if (Has(name)) return attributes[name];
                else
                {
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
            #endregion
        }

        class UssWriter
        {
            #region Fields
            StreamWriter uss;
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
                if (style.HasAttributes)
                {
                    if (count > 0)
                    {
                        uss.WriteLine();
                        uss.WriteLine();
                    }
                    style.Write(uss);
                    count++;
                }
            }
            #endregion
        }

        class UxmlWriter
        {
            const string prefix = "unity";
            static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true, IndentChars = "    ", NewLineOnAttributes = true };

            #region Fields
            Func<BaseNode, string> getClassList;
            string documentFolder;
            string documentName;
            XmlWriter documentXml;
            #endregion

            #region Constructors
            public UxmlWriter(DocumentNode document, Func<BaseNode, string> getClassList, string folder, string name)
            {
                this.getClassList = getClassList;
                documentFolder = folder;
                documentName = name;

                using (documentXml = CreateXml(documentFolder, documentName)) WriteRecursively(document, documentXml);
            }
            #endregion

            #region Methods
            void WriteRecursively(BaseNode node, XmlWriter uxml)
            {
                void WriteDocumentNode(DocumentNode node, XmlWriter uxml)
                {
                    uxml.WriteStartElement(prefix, "UXML", "UnityEngine.UIElements");

                    WriteStart(node, uxml);

                    uxml.WriteStartElement("Style");
                    uxml.WriteAttributeString("src", $"{documentName}.uss");
                    uxml.WriteEndElement();

                    foreach (CanvasNode canvas in node.children) WriteRecursively(canvas, uxml);

                    WriteEnd(uxml);

                    uxml.WriteEndElement();
                }
                void WriteCanvasNode(CanvasNode node, XmlWriter uxml)
                {
                    WriteStart(node, uxml);
                    foreach (BaseNode child in node.children) WriteRecursively(child, uxml);
                    WriteEnd(uxml);
                }
                void WriteSliceNode(SliceNode node, XmlWriter uxml)
                {
                    WriteStart(node, uxml);
                    WriteEnd(uxml);
                }
                void WriteTextNode(TextNode text, XmlWriter uxml)
                {
                    WriteStart(node, uxml);

                    switch (text.style.textCase)
                    {
                        case TextCase.UPPER:
                            uxml.WriteAttributeString("text", text.characters.ToUpper());
                            break;

                        case TextCase.LOWER:
                            uxml.WriteAttributeString("text", text.characters.ToLower());
                            break;

                        default:
                            uxml.WriteAttributeString("text", text.characters);
                            break;
                    }

                    WriteEnd(uxml);
                }
                void WriteDefaultFrameNode(DefaultFrameNode node, XmlWriter uxml)
                {
                    string tooltip = default;
                    if (node.GetTemplate() is (bool hash, string template) && template.NotNullOrEmpty())
                    {
                        if (hash) tooltip = template;
                        using (XmlWriter elementUxml = CreateXml(Path.Combine(documentFolder, "Elements"), template))
                        {
                            elementUxml.WriteStartElement(prefix, "UXML", "UnityEngine.UIElements");

                            WriteStart(node, elementUxml);
                            foreach (BaseNode child in node.children) WriteRecursively(child, elementUxml);
                            WriteEnd(elementUxml);

                            elementUxml.WriteEndElement();
                        }

                        uxml.WriteStartElement(prefix, "Template", "UnityEngine.UIElements");
                        uxml.WriteAttributeString("name", template);
                        uxml.WriteAttributeString("src", uxml == documentXml ? $"Elements\\{template}.uxml" : $"{template}.uxml");
                        uxml.WriteEndElement();
                    }

                    WriteStart(node, uxml);
                    if (tooltip.NotNullOrEmpty()) uxml.WriteAttributeString("tooltip", tooltip); // Use tooltip as a storage for hash template name
                    foreach (BaseNode child in node.children) WriteRecursively(child, uxml);
                    WriteEnd(uxml);
                }
                void WriteDefaultShapeNode(DefaultShapeNode node, XmlWriter uxml)
                {
                    string tooltip = default;
                    if (node.GetTemplate() is (bool hash, string template) && template.NotNullOrEmpty())
                    {
                        if (hash) tooltip = template;
                        using (XmlWriter elementUxml = CreateXml(Path.Combine(documentFolder, "Elements"), template))
                        {
                            elementUxml.WriteStartElement(prefix, "UXML", "UnityEngine.UIElements");

                            WriteStart(node, elementUxml);
                            WriteEnd(elementUxml);

                            elementUxml.WriteEndElement();
                        }

                        uxml.WriteStartElement(prefix, "Template", "UnityEngine.UIElements");
                        uxml.WriteAttributeString("name", template);
                        uxml.WriteAttributeString("src", uxml == documentXml ? $"Elements\\{template}.uxml" : $"{template}.uxml");
                        uxml.WriteEndElement();
                    }

                    WriteStart(node, uxml);
                    if (tooltip.NotNullOrEmpty()) uxml.WriteAttributeString("tooltip", tooltip); // Use tooltip as a storage for hash template name
                    WriteEnd(uxml);
                }

                if (!IsVisible(node)) return;
                if (!node.EnabledInHierarchy()) return;
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
                static (string prefix, string elementName, string pickingMode) GetElementData(BaseNode node)
                {
                    string prefix = UxmlWriter.prefix;
                    string elementName = "VisualElement";
                    string pickingMode = "Ignore";

                    ElementType elementType = node.GetElementType();
                    if (elementType == ElementType.IElement)
                    {
                        prefix = default;
                        elementName = $"{node.GetFieldInfo().FieldType.FullName.Replace("+", ".")}";
                        pickingMode = "Position";
                    }
                    else if (elementType == ElementType.None)
                    {
                        if (node is TextNode)
                        {
                            if (node.name.StartsWith("Inputs"))
                            {
                                elementName = "TextField";
                                pickingMode = "Position";
                            }
                            else
                            {
                                elementName = "Label";
                            }
                        }

                        if (node is DefaultFrameNode || node is TextNode)
                        {
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
        DocumentNode document;
        Dictionary<string, Style> documentStyles;
        Func<string, string, (bool valid, string path)> getAssetPath;
        Func<string, string, (bool valid, int width, int height)> getAssetSize;

        List<ComponentNode> components = new List<ComponentNode>();
        List<Dictionary<string, Style>> componentsStyles = new List<Dictionary<string, Style>>();

        List<string> missingComponents = new List<string>();
        List<BaseNode> imageFillNodes = new List<BaseNode>();
        List<BaseNode> pngNodes = new List<BaseNode>();
        List<BaseNode> svgNodes = new List<BaseNode>();
        Dictionary<string, GradientPaint> gradients = new Dictionary<string, GradientPaint>();

        List<(StyleSlot slot, UssStyle style)> styles = new List<(StyleSlot slot, UssStyle style)>();
        Dictionary<BaseNode, UssStyle> componentStyle = new();
        Dictionary<BaseNode, UssStyle> nodeStyle = new();
        #endregion

        #region Properties
        public List<string> MissingComponents => missingComponents;
        public List<BaseNode> ImageFillNodes => imageFillNodes;
        public List<BaseNode> PngNodes => pngNodes;
        public List<BaseNode> SvgNodes => svgNodes;
        public Dictionary<string, GradientPaint> Gradients => gradients;
        #endregion

        #region Constructors
        public FigmaParser(DocumentNode document, Dictionary<string, Style> documentStyles, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize)
        {
            this.document = document;
            this.documentStyles = documentStyles;
            this.getAssetPath = getAssetPath;
            this.getAssetSize = getAssetSize;

            foreach (CanvasNode canvas in document.children)
            {
                AddMissingNodesRecursively(canvas);
                AddImageFillsRecursively(canvas);
                AddPngNodesRecursively(canvas);
                AddSvgNodesRecursively(canvas);
                AddGradientsRecursively(canvas);
            }
        }
        #endregion

        #region Methods
        public void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }
        public void Run()
        {
            AddStylesRecursively(document, documentStyles, false);
            foreach (CanvasNode canvas in document.children) AddStylesRecursively(canvas, documentStyles, false);
            foreach ((ComponentNode component, int index) in components.Select((x, i) => (x, i))) AddStylesRecursively(component, componentsStyles[index], true);

            InheritStylesRecursively(document);
            foreach (CanvasNode canvas in document.children) InheritStylesRecursively(canvas);
        }
        public void Write(string folder, string name)
        {
            void GroupRenameStyles(IEnumerable<UssStyle> styles)
            {
                string[] unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                string[] tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
                string NumberToWords(int number)
                {
                    if (number == 0) return "zero";
                    if (number < 0) return $"minus-{NumberToWords(Math.Abs(number))}";

                    string words = "";

                    if ((number / 1000000) > 0)
                    {
                        words += $"{NumberToWords(number / 1000000)}-million ";
                        number %= 1000000;
                    }
                    if ((number / 1000) > 0)
                    {
                        words += $"{NumberToWords(number / 1000)}-thousand ";
                        number %= 1000;
                    }
                    if ((number / 100) > 0)
                    {
                        words += $"{NumberToWords(number / 100)}-hundred ";
                        number %= 100;
                    }

                    if (number > 0)
                    {
                        if (words != "") words += "and-";
                        if (number < 20) words += unitsMap[number];
                        else
                        {
                            words += tensMap[number / 10];
                            if ((number % 10) > 0) words += $"-{unitsMap[number % 10]}";
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
                string GetState(string name)
                {
                    return name.Substring(name.LastIndexOf(":"), name.Length - name.LastIndexOf(":"));
                }

                foreach (BaseNode node in nodes)
                {
                    if (node.parent is ChildrenMixin parent)
                    {
                        foreach (BaseNode child in parent.children)
                        {
                            if (IsStateNode(child) && IsStateNode(child, node))
                            {
                                UssStyle childStyle = GetStyle(child);
                                if (childStyle is not null) childStyle.Name = $"{nodeStyle[node].Name}{GetState(childStyle.Name)}";
                            }
                        }
                    }
                }
            }

            var nodeStyleFiltered = nodeStyle.Where(x => IsVisible(x.Key) && x.Key.EnabledInHierarchy()).ToArray();
            UssStyle[] nodeStyleStatelessFiltered = nodeStyleFiltered.Where(x => !IsStateNode(x.Key)).Select(x => x.Value).ToArray();
            UssStyle[] stylesFiltered = styles.Select(x => x.style).Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();
            UssStyle[] componentStyleFiltered = componentStyle.Values.Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();

            GroupRenameStyles(stylesFiltered.Union(componentStyleFiltered).Union(nodeStyleStatelessFiltered));
            FixStateStyles(nodeStyleFiltered.Select(x => x.Key));

            new UxmlWriter(document, GetClassList, folder, name);

            using (StreamWriter uss = new StreamWriter(Path.Combine(folder, $"{name}.uss")))
                new UssWriter(stylesFiltered, componentStyleFiltered, nodeStyleFiltered.Select(x => x.Value), uss);
        }

        void AddMissingNodesRecursively(BaseNode node)
        {
            if (node is InstanceNode instance && FindNode(instance.componentId) is null)
                missingComponents.Add(instance.componentId);

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    AddMissingNodesRecursively(child);
        }
        void AddImageFillsRecursively(BaseNode node)
        {
            if (!IsVisible(node)) return;
            if (!node.EnabledInHierarchy()) return;

            if (node is BooleanOperationNode) return;
            if (!IsSvgNode(node) && HasImageFill(node)) imageFillNodes.Add(node);

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    AddImageFillsRecursively(child);
        }
        void AddPngNodesRecursively(BaseNode node)
        {
            if (!IsVisible(node)) return;
            if (!node.EnabledInHierarchy()) return;

            if (IsSvgNode(node) && HasImageFill(node)) pngNodes.Add(node);
            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    AddPngNodesRecursively(child);
        }
        void AddSvgNodesRecursively(BaseNode node)
        {
            if (!IsVisible(node)) return;
            if (!node.EnabledInHierarchy()) return;

            if (IsSvgNode(node) && !HasImageFill(node)) svgNodes.Add(node);
            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    AddSvgNodesRecursively(child);
        }
        void AddGradientsRecursively(BaseNode node)
        {
            if (!IsVisible(node)) return;
            if (!node.EnabledInHierarchy()) return;

            if (node is BooleanOperationNode) return;
            if (node is GeometryMixin geometry)
            {
                foreach (GradientPaint gradient in geometry.fills.Where(x => x is GradientPaint))
                {
                    string key = gradient.GetHash();
                    if (!gradients.ContainsKey(key)) gradients.Add(key, gradient);
                }
            }

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    AddGradientsRecursively(child);
        }
        void AddStylesRecursively(BaseNode node, Dictionary<string, Style> styles, bool insideComponent)
        {
            string GetClassName(string name, bool state, string prefix = "n")
            {
                if (name.Length > 64) name = name.Substring(0, 64);
                name = Regex.Replace(name, $"[^a-zA-Z0-9{(state ? ":" : "")}]", "-");

                for (int i = 0; i < 10; ++i) if (name.Contains("--")) name = name.Replace("--", "-");
                for (int i = 0; i < 10; ++i) if (name.EndsWith("-")) name = name.Substring(0, name.Length - 1);
                for (int i = 0; i < 10; ++i) if (name.StartsWith("-")) name = name.Substring(1, name.Length - 1);
                if (name.All(x => x == '-')) name = $"{prefix}";
                if (name.Length > 0 && char.IsDigit(name[0])) name = $"{prefix}-{name}";

                return name;
            }

            if (node is ComponentNode) insideComponent = true;

            if (insideComponent)
            {
                componentStyle[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);
            }
            else
            {
                nodeStyle[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);
            }

            if (node is BlendMixin blend && blend.styles is not null)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith("s")) slot = slot.Substring(0, slot.Length - 1);
                    string id = keyValue.Value;
                    string key = styles[id].key;

                    StyleSlot style = new StyleSlot(text, slot, styles[id]);
                    if (!this.styles.Any(x => x.slot.text == text && x.slot.slot == slot && x.slot.key == key)) this.styles.Add((style, new UssStyle(GetClassName(style.name, false, "s"), getAssetPath, getAssetSize, style.slot, style.styleType, node)));
                }
            }

            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    AddStylesRecursively(child, styles, insideComponent);
        }

        BaseNode FindNode(string id)
        {
            BaseNode FindNode(BaseNode root)
            {
                if (root is ChildrenMixin children)
                    foreach (SceneNode child in children.children)
                    {
                        if (child.id == id) return child;

                        BaseNode node = FindNode(child);
                        if (node is not null) return node;
                    }

                return default;
            }

            if (document.id == id) return document;

            foreach (CanvasNode canvas in document.children)
            {
                if (canvas.id == id) return canvas;

                BaseNode node = FindNode(canvas);
                if (node is not null) return node;
            }

            return default;
        }
        UssStyle GetStyle(BaseNode node)
        {
            if (componentStyle.TryGetValue(node, out var style)) return style;
            if (nodeStyle.TryGetValue(node, out style)) return style;
            return default;
        }
        void InheritStylesRecursively(BaseNode node)
        {
            UssStyle style = GetStyle(node);
            UssStyle component = default;
            List<UssStyle> styles = new List<UssStyle>();

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
            else if (node.id.Contains(";"))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits.Last();
                    BaseNode componentNode = FindNode(componentId);
                    if (componentNode is not null) component = GetStyle(componentNode);
                }
            }

            if (node is BlendMixin blend && blend.styles is not null)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith("s")) slot = slot.Substring(0, slot.Length - 1);
                    string id = keyValue.Value;
                    string key = default;
                    if (documentStyles.ContainsKey(id)) key = documentStyles[id].key;
                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.ContainsKey(id)) key = componentStyle[id].key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.text == text && x.slot.slot == slot && x.slot.key == key)) >= 0) styles.Add(this.styles[index].style);
                }
            }

            if (component is not null && styles.Count > 0) style.Inherit(component, styles);
            else if (component is not null) style.Inherit(component);
            else if (styles.Count > 0) style.Inherit(styles);

            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (BaseNode child in children.children)
                    InheritStylesRecursively(child);
        }
        string GetClassList(BaseNode node)
        {
            string classList = "";
            UssStyle style = GetStyle(node);
            if (style is null) return classList;

            string component = "";
            List<string> styles = new List<string>();

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
            else if (node.id.Contains(";"))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits.Last();
                    BaseNode componentNode = FindNode(componentId);
                    if (componentNode is not null) component = GetStyle(componentNode).Name;
                }
            }

            if (node is BlendMixin blend && blend.styles is not null)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith("s")) slot = slot.Substring(0, slot.Length - 1);
                    string id = keyValue.Value;
                    string key = default;
                    if (documentStyles.ContainsKey(id)) key = documentStyles[id].key;
                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.ContainsKey(id)) key = componentStyle[id].key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.text == text && x.slot.slot == slot && x.slot.key == key)) >= 0) styles.Add(this.styles[index].style.Name);
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
        static bool IsRootNode(BaseNodeMixin mixin)
        {
            return mixin is DocumentNode || mixin is CanvasNode || mixin.parent is CanvasNode || mixin is ComponentNode || mixin.parent is ComponentNode;
        }
        static bool IsVisible(BaseNodeMixin mixin)
        {
            if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;
            if (mixin.parent is not null) return IsVisible(mixin.parent);
            else return true;
        }
        static bool HasImageFill(BaseNodeMixin mixin)
        {
            return mixin is GeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);
        }
        static bool IsSvgNode(BaseNodeMixin mixin)
        {
            return mixin is LineNode || mixin is EllipseNode || mixin is RegularPolygonNode || mixin is StarNode || mixin is VectorNode || (mixin is BooleanOperationNode && IsBooleanOperationVisible(mixin));
        }
        static bool IsBooleanOperationVisible(BaseNodeMixin node)
        {
            if (node is not ChildrenMixin children) return false;

            foreach (SceneNode child in children.children)
            {
                if (child is not BooleanOperationNode && IsVisible(child) && IsSvgNode(child)) return true;
                else if (child is BooleanOperationNode) return IsBooleanOperationVisible(child);
            }

            return false;
        }
        static bool IsStateNode(BaseNodeMixin mixin)
        {
            return mixin.name.EndsWith(":hover") || mixin.name.EndsWith(":active") || mixin.name.EndsWith(":inactive") || mixin.name.EndsWith(":focus") || mixin.name.EndsWith(":selected") || mixin.name.EndsWith(":disabled") || mixin.name.EndsWith(":enabled") || mixin.name.EndsWith(":checked") || mixin.name.EndsWith(":root");
        }
        static bool IsStateNode(BaseNodeMixin mixin, BaseNodeMixin normal)
        {
            return mixin.name.Substring(0, mixin.name.LastIndexOf(":")) == normal.name;
        }
        static (int, int) CenterChildrenCount(DefaultFrameMixin mixin)
        {
            if (mixin.layoutMode.HasValue) return (0, 0);

            int horizontalCenterCount = mixin.children.Cast<ConstraintMixin>().Count(x => x.constraints.horizontal == ConstraintHorizontal.CENTER);
            int verticalCenterCount = mixin.children.Cast<ConstraintMixin>().Count(x => x.constraints.vertical == ConstraintVertical.CENTER);
            return (horizontalCenterCount, verticalCenterCount);
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
        #endregion
    }
}