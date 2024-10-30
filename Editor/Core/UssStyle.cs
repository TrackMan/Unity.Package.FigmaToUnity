using Figma.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma
{
    using Internals;
    using static FigmaParser;

    internal class UssStyle
    {
        internal const string viewportClass = "unity-viewport";
        internal const string overrideClass = "unity-base-override";
        readonly string[] fontWeights = { "Thin", "ExtraLight", "Light", "Regular", "Medium", "SemiBold", "Bold", "ExtraBold", "Black" };

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
        EnumProperty<BackgroundPositionKeyword> unityBackgroundPositionX { get => Get("background-position-x"); set => Set("background-position-x", value); }
        EnumProperty<BackgroundPositionKeyword> unityBackgroundPositionY { get => Get("background-position-y"); set => Set("background-position-y", value); }
        EnumProperty<Repeat> unityBackgroundRepeat { get => Get("background-repeat"); set => Set("background-repeat", value); }
        EnumProperty<BackgroundSizeType> unityBackgroundSize { get => Get("background-size"); set => Set("background-size", value); }
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
                    unityBackgroundPositionX = BackgroundPositionKeyword.Center;
                    unityBackgroundPositionY = BackgroundPositionKeyword.Center;
                    unityBackgroundRepeat = Repeat.NoRepeat;
                    break;

                case viewportClass:
                    position = Position.Absolute;
                    width = "100%";
                    height = "100%";
                    break;
            }
        }
        public UssStyle(string name, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize, string slot, StyleType type, BaseNode node)
        {
            Name = name;
            this.getAssetPath = getAssetPath;
            this.getAssetSize = getAssetSize;

            if (type == StyleType.FILL && node is IGeometryMixin geometry)
            {
                if (slot == "fill")
                    if (node is TextNode)
                    {
                        Name += "-Text";
                        AddTextFillStyle(geometry.fills);
                    }
                    else
                        AddFillStyle(geometry.fills);
                else if (slot == "stroke")
                    if (node is TextNode)
                        Name += "-TextStroke";
                    else
                    {
                        Name += "-Border";
                        AddStrokeFillStyle(geometry.strokes);
                    }
            }
            else if (type == StyleType.TEXT && node is TextNode text)
                AddTextStyle(text.style);
            else if (type == StyleType.EFFECT && node is IBlendMixin blend)
                AddNodeEffects(blend.effects);
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
        public string ResolveClassList() => attributes.Count > 0 ? $"{Name}" : string.Empty;
        public void Write(StreamWriter stream)
        {
            stream.WriteLine($".{Name} {{");

            if (Has("--unity-font-missing")) attributes.Remove("--unity-font-missing");

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

        void AddFrameNode(FrameNode node) => AddDefaultFrameNode(node);
        void AddGroupNode(GroupNode node) => AddDefaultFrameNode(node);
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
        void AddLineNode(LineNode node) => AddDefaultShapeNode(node);
        void AddEllipseNode(EllipseNode node) => AddDefaultShapeNode(node);
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
        void AddComponentSetNode(ComponentSetNode node) => AddDefaultFrameNode(node);
        void AddComponentNode(ComponentNode node) => AddDefaultFrameNode(node);
        void AddInstanceNode(InstanceNode node) => AddDefaultFrameNode(node);
        void AddBooleanOperationNode(BooleanOperationNode node) => AddDefaultFrameNode(node);

        void AddFrame(IDefaultFrameMixin mixin)
        {
            void AddPadding()
            {
                Double[] padding = { mixin.paddingTop ?? 0, mixin.paddingRight ?? 0, mixin.paddingBottom ?? 0, mixin.paddingLeft ?? 0 };
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
        void AddScene(IBaseNodeMixin @base)
        {
            void HandleVisibility()
            {
                if (IsVisible(@base) || IsStateNode(@base)) defaults.Add("display", "flex");
                else display = Display.None;
            }

            HandleVisibility();
        }
        void AddLayout(ILayoutMixin mixin, IBaseNodeMixin @base)
        {
            if (@base is IDefaultFrameMixin frame && IsMostlyHorizontal(frame)) flexDirection = FlexDirection.Row;
            if (@base.parent is IDefaultFrameMixin { layoutMode: not null } && mixin.layoutAlign == LayoutAlign.STRETCH) alignSelf = Align.Stretch;
        }
        void AddBlend(IBlendMixin mixin)
        {
            void AddOpacity()
            {
                if (mixin.opacity.HasValue)
                    if (mixin.opacity == 1) defaults.Add("opacity", "1");
                    else opacity = mixin.opacity;
            }

            AddOpacity();
            if (mixin is TextNode) AddTextNodeEffects(mixin.effects);
            else AddNodeEffects(mixin.effects);
        }
        void AddGeometry(IGeometryMixin mixin, ILayoutMixin layout, IBaseNodeMixin @base)
        {
            void AddBackgroundImageForVectorNode()
            {
                if (HasImageFill(@base))
                {
                    (bool valid, string url) = getAssetPath(@base.id, KnownFormats.png);
                    if (valid) backgroundImage = $"url('{url}')";
                }
                else
                {
                    (bool valid, string url) = getAssetPath(@base.id, KnownFormats.svg);
                    if (valid) backgroundImage = $"url('{url}')";
                }
            }
            void AddBorderWidth()
            {
                bool state = IsStateNode(mixin.As<IBaseNodeMixin>());
                if (mixin.individualStrokeWeights is not null)
                {
                    if (mixin.individualStrokeWeights.left > 0 || state) borderLeftWidth = mixin.individualStrokeWeights.left;
                    if (mixin.individualStrokeWeights.right > 0 || state) borderRightWidth = mixin.individualStrokeWeights.right;
                    if (mixin.individualStrokeWeights.top > 0 || state) borderTopWidth = mixin.individualStrokeWeights.top;
                    if (mixin.individualStrokeWeights.bottom > 0 || state) borderBottomWidth = mixin.individualStrokeWeights.bottom;
                }
                else if (mixin.strokeWeight > 0 || state) borderWidth = mixin.strokeWeight;
            }
            void AddBorderRadius(IRectangleCornerMixin rectangleCornerMixin, ICornerMixin cornerMixin)
            {
                void AddRadius(Double minValue, Double value)
                {
                    if (rectangleCornerMixin.rectangleCornerRadii is null)
                    {
                        if (cornerMixin.cornerRadius.HasPositive()) 
                            borderRadius = Math.Min(minValue, cornerMixin!.cornerRadius!.Value) + value;
                    }
                    else
                    {
                        for (int i = 0; i < rectangleCornerMixin.rectangleCornerRadii.Length; ++i) rectangleCornerMixin.rectangleCornerRadii[i] = Math.Min(minValue, rectangleCornerMixin.rectangleCornerRadii[i]) + value;
                        borderRadius = rectangleCornerMixin.rectangleCornerRadii;
                    }
                }

                Double value = Double.NaN;
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

                Double minBorderRadius = Math.Min(layout.absoluteBoundingBox.width / 2, layout.absoluteBoundingBox.height / 2);
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

                if (mixin.strokeAlign.HasValue) AddBorderRadius(mixin as IRectangleCornerMixin, mixin as ICornerMixin);
            }
        }
        void AddCorner(ICornerMixin cornerMixin, IRectangleCornerMixin rectangleCornerMixin)
        {
            if (rectangleCornerMixin.rectangleCornerRadii is not null) borderRadius = rectangleCornerMixin.rectangleCornerRadii;
            else if (cornerMixin.cornerRadius.HasPositive()) borderRadius = cornerMixin.cornerRadius;
        }

        void AddBoxModel(ILayoutMixin layout, IConstraintMixin constraint, IGeometryMixin geometry, IBaseNodeMixin baseNode)
        {
            void AdjustSvgSize()
            {
                (bool valid, int width, int height) = getAssetSize(baseNode.id, KnownFormats.svg);

                if (valid && width > 0 && height > 0)
                    layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y, width, height);

                if (geometry.strokes.Length == 0 || geometry.strokeWeight is not > 0) return;

                layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.y, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);

                if (geometry.strokeCap is null or StrokeCap.NONE) return;

                layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
            }
            void AddSizeByParentAutoLayoutFromAutoLayout(IDefaultFrameMixin frame)
            {
                position = Position.Relative;
                if (frame.layoutMode == LayoutMode.HORIZONTAL)
                {
                    if (((IDefaultFrameMixin)frame.parent).layoutMode == LayoutMode.HORIZONTAL)
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
                    if (((IDefaultFrameMixin)frame.parent).layoutMode == LayoutMode.VERTICAL)
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
            void AddSizeByParentAutoLayoutFromLayout(IDefaultFrameMixin parent)
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
            void AddSizeFromConstraint(IDefaultFrameMixin parent, LengthProperty widthProperty, LengthProperty heightProperty)
            {
                string GetFullPath(IBaseNodeMixin node) => node.parent is not null ? $"{GetFullPath(node.parent)}/{node.name}" : node.name;

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
                    Debug.LogWarning(Extensions.BuildTargetMessage($"Vertical=Center is used in mostly horizontal layout at", GetFullPath(baseNode), "changing Horizontal=Center."));
                    horizontal = ConstraintHorizontal.CENTER;
                }

                if (hasMixedCenterChildren && isMostlyVertical && vertical != ConstraintVertical.CENTER && horizontal == ConstraintHorizontal.CENTER)
                {
                    Debug.LogWarning(Extensions.BuildTargetMessage($"Horizontal=Center is used in mostly vertical layout at", GetFullPath(baseNode), "changing Vertical=Center."));
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

            void AddItemSpacing(IDefaultFrameMixin parent, Double itemSpacing)
            {
                if (baseNode == parent.children.LastOrDefault(IsVisible)) return;

                if (parent!.layoutMode!.Value == LayoutMode.HORIZONTAL && parent.primaryAxisAlignItems is not PrimaryAxisAlignItems.SPACE_BETWEEN)
                    marginRight = itemSpacing;

                if (parent!.layoutMode!.Value != LayoutMode.HORIZONTAL)
                    marginBottom = itemSpacing;
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
                Double GetStrokeWeight(StrokeAlign strokeAlign, Double strokeWeight)
                {
                    return strokeAlign switch
                    {
                        StrokeAlign.CENTER => strokeWeight / 2,
                        StrokeAlign.OUTSIDE => strokeWeight,
                        _ => throw new NotSupportedException()
                    };
                }

                if (baseNode is IDefaultFrameMixin { layoutMode: not null }) return;

                if (!IsSvgNode(baseNode) && geometry is not null &&
                    geometry.strokes.Length > 0 && geometry.strokeWeight is > 0 &&
                    geometry.strokeAlign.HasValue && geometry.strokeAlign != StrokeAlign.INSIDE)
                    margin += GetStrokeWeight(geometry.strokeAlign.Value, geometry.strokeWeight.Value);
            }

            if (IsSvgNode(baseNode)) AdjustSvgSize();

            IDefaultFrameMixin parent = baseNode.parent as IDefaultFrameMixin;
            if (!IsRootNode(baseNode))
            {
                if (parent!.layoutMode.HasValue)
                {
                    if (baseNode is IDefaultFrameMixin { layoutMode: not null } frame)
                        AddSizeByParentAutoLayoutFromAutoLayout(frame);
                    else
                        AddSizeByParentAutoLayoutFromLayout(parent);

                    Double? itemSpacing = parent.itemSpacing;
                    if (itemSpacing.HasPositive()) AddItemSpacing(parent, itemSpacing!.Value);
                }
                else
                {
                    if (baseNode is IDefaultFrameMixin { layoutMode: not null } frame)
                        AddSizeFromConstraint(parent, (frame.layoutMode == LayoutMode.HORIZONTAL
                                                  ? frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED)
                                                  : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED))
                                                  ? layout.absoluteBoundingBox.width
                                                  : Unit.Auto, (frame.layoutMode == LayoutMode.VERTICAL
                                                  ? frame.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED)
                                                  : frame.counterAxisSizingMode.IsValue(CounterAxisSizingMode.FIXED))
                                                  ? layout.absoluteBoundingBox.height
                                                  : Unit.Auto);
                    else
                        AddSizeFromConstraint(parent, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
                }
            }

            if (baseNode is TextNode textNode && textNode.style.textAutoResize.HasValue) OverwriteSizeFromTextNode(textNode);
            AddNonInsideBorder();
        }

        void AddFillStyle(IEnumerable<Paint> fills)
        {
            foreach (Paint fill in fills)
            {
                if (fill is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                    backgroundColor = new ColorProperty(solid.color, solid.opacity);

                if (fill is GradientPaint gradient && gradient.visible.IsEmptyOrTrue())
                {
                    (bool valid, string url) = getAssetPath(gradient.GetHash(), KnownFormats.svg);
                    if (valid) backgroundImage = $"url('{url}')";
                }

                if (fill is ImagePaint image && image.visible.IsEmptyOrTrue())
                {
                    (bool valid, string url) = getAssetPath(image.imageRef, KnownFormats.png);
                    if (valid) backgroundImage = $"url('{url}')";

                    switch (image.scaleMode)
                    {
                        case ScaleMode.FILL:
                            unityBackgroundSize = BackgroundSizeType.Cover;
                            break;

                        case ScaleMode.FIT:
                            unityBackgroundSize = BackgroundSizeType.Contain;
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
                (bool ttf, string ttfPath) = getAssetPath(font, KnownFormats.ttf);

                if (ttf)
                {
                    resource = $"url('{ttfPath}')";
                    url = ttfPath;
                    return true;
                }

                (bool otf, string otfPath) = getAssetPath(font, KnownFormats.otf);
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
                string weightPostfix = style.fontWeight.HasValue
                    ? fontWeights[(int)(style.fontWeight / 100) - 1]
                    : style.fontPostScriptName.Contains('-')
                        ? style.fontPostScriptName.Split('-')[1].Replace("Index", string.Empty)
                        : string.Empty;
                string italicPostfix = style.italic.HasValue && style.italic.Value || style.fontPostScriptName.Contains("Italic") ? "Italic" : string.Empty;

                bool valid;
                if (!TryGetFontWithExtension($"{style.fontFamily}-{weightPostfix}{italicPostfix}", out string resource, out string url) && !TryGetFontWithExtension(style.fontPostScriptName, out resource, out url))
                {
                    unityFontMissing = $"url('{url}')";
                    Debug.LogWarning(Extensions.BuildTargetMessage($"Cannot find Font", $"{style.fontFamily}-{weightPostfix}{italicPostfix}", string.Empty));
                }

                unityFont = resource;
                (valid, url) = getAssetPath($"{style.fontFamily}-{weightPostfix}{italicPostfix}", KnownFormats.asset);
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
                if (effect is ShadowEffect { visible: true } shadowEffect)
                    textShadow = new ShadowProperty(shadowEffect.offset.x, shadowEffect.offset.y, shadowEffect.radius, shadowEffect.color);
        }
        void AddNodeEffects(IEnumerable<Effect> effects)
        {
            foreach (Effect effect in effects)
                if (effect is ShadowEffect { visible: true } shadowEffect)
                    boxShadow = new ShadowProperty(shadowEffect.offset.x, shadowEffect.offset.y, shadowEffect.radius, shadowEffect.color);
        }
        #endregion

        #region Support Methods
        bool Has(string name) => attributes.ContainsKey(name);
        string Get(string name) => attributes[name];
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
                if (Has(names[i]))
                    properties[i] = attributes[names[i]];
                else
                    properties[i] = new LengthProperty(Unit.Pixel);

            return new Length4Property(properties);
        }
        void Set(string name, string value) => attributes[name] = value;
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

        static bool HasMixedCenterChildren(IDefaultFrameMixin mixin)
        {
            (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
            return horizontalCenterCount > 0 && verticalCenterCount > 0;
        }
        static bool HasAnyCenterChildren(IDefaultFrameMixin mixin)
        {
            (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
            return horizontalCenterCount > 0 || verticalCenterCount > 0;
        }
        static bool HasManyCenterChildren(IDefaultFrameMixin mixin)
        {
            (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
            return horizontalCenterCount > 1 || verticalCenterCount > 1;
        }
        static bool IsMostlyHorizontal(IDefaultFrameMixin mixin)
        {
            (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
            return horizontalCenterCount > verticalCenterCount;
        }
        static (int, int) CenterChildrenCount(IDefaultFrameMixin mixin)
        {
            if (mixin.layoutMode.HasValue) return (0, 0);

            int horizontalCenterCount = mixin.children.Cast<IConstraintMixin>().Count(x => x.constraints.horizontal == ConstraintHorizontal.CENTER);
            int verticalCenterCount = mixin.children.Cast<IConstraintMixin>().Count(x => x.constraints.vertical == ConstraintVertical.CENTER);
            return (horizontalCenterCount, verticalCenterCount);
        }
        #endregion
    }
}