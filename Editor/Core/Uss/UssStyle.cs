using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma.Core.Uss
{
    using Internals;
    using static ContentWriter;

    internal class UssStyle : BaseUssStyle
    {
        #region Const
        internal static readonly UssStyle overrideClass = new("unity-base-override")
        {
            alignItems = Align.Center,
            backgroundColor = Unit.Initial,
            borderWidth = Unit.Initial,
            justifyContent = JustifyContent.Center,
            margin = Unit.Initial,
            overflow = Unit.Initial,
            padding = Unit.Initial,
            unityBackgroundPositionX = BackgroundPositionKeyword.Center,
            unityBackgroundPositionY = BackgroundPositionKeyword.Center,
            unityBackgroundRepeat = Repeat.NoRepeat,
            unityFontDefinition = Unit.Initial,
        };

        internal static readonly UssStyle viewportClass = new("unity-viewport") { position = Position.Absolute, width = "100%", height = "100%" };
        #endregion

        #region Fields
        readonly AssetsInfo assetsInfo;
        #endregion

        #region Properties
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
        Length2Property translate { get => GetDefault("translate", "0 0"); set => Set("translate", value); }

        // Drawing
        // Background
        ColorProperty backgroundColor { get => Get("background-color"); set => Set("background-color", value); }
        AssetProperty backgroundImage { get => Get("background-image"); set => Set("background-image", value); }
        EnumProperty<BackgroundPositionKeyword> unityBackgroundPositionX { get => Get("background-position-x"); set => Set("background-position-x", value); }
        EnumProperty<BackgroundPositionKeyword> unityBackgroundPositionY { get => Get("background-position-y"); set => Set("background-position-y", value); }
        EnumProperty<Repeat> unityBackgroundRepeat { get => Get("background-repeat"); set => Set("background-repeat", value); }
        EnumProperty<BackgroundSizeType> unityBackgroundSize { get => GetDefault("background-size", "auto"); set => Set("background-size", value); }
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
        AssetProperty unityFontDefinition { get => Get("-unity-font-definition"); set => Set("-unity-font-definition", value); }
        LengthProperty fontSize { get => Get("font-size"); set => Set("font-size", value); }
        EnumProperty<FontStyle> unityFontStyle { get => Get("-unity-font-style"); set => Set("-unity-font-style", value); }
        EnumProperty<TextAlign> unityTextAlign { get => Get("-unity-text-align"); set => Set("-unity-text-align", value); }
        EnumProperty<Wrap> whiteSpace { get => Get("white-space"); set => Set("white-space", value); }
        ShadowProperty textShadow { get => Get("text-shadow"); set => Set("text-shadow", value); }
        EnumProperty<TextOverflow> textOverflow { get => Get("text-overflow"); set => Set("text-overflow", value); }

        // Cursor
        CursorProperty cursor { get => Get("cursor"); set => Set("cursor", value); }

        // Effects
        ShadowProperty boxShadow { get => Get("--box-shadow"); set => Set("--box-shadow", value); }

        // Transitions
        internal DurationProperty transitionDuration { get => Get("transition-duration"); set => Set("transition-duration", value); }
        internal EnumProperty<EasingFunction> transitionEasing { get => Get("transition-timing-function"); set => Set("transition-timing-function", value); }
        #endregion

        #region Constructors
        public UssStyle(string name) : base(name) { }
        public UssStyle(string name, AssetsInfo assetsInfo) : this(name) => this.assetsInfo = assetsInfo;
        public UssStyle(string name, AssetsInfo assetsInfo, string slot, StyleType type, BaseNode node) : this(name, assetsInfo)
        {
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
        public UssStyle(string name, AssetsInfo assetsInfo, BaseNode node) : this(name, assetsInfo)
        {
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
        internal UssStyle CopyFrom(UssStyle style)
        {
            style.Attributes.ForEach(x => Attributes[x.Key] = x.Value);
            return this;
        }
        void AddDefaultFrameNode(DefaultFrameNode node)
        {
            AddCorner(node, node);

            AddFrame(node);
            AddDefaultShapeNode(node);

            if (node.clipsContent.HasValueAndTrue())
                overflow = Visibility.Hidden;
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
            AddBoxModel(node, node, null, node);
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
                double[] padding = { mixin.paddingTop ?? 0, mixin.paddingRight ?? 0, mixin.paddingBottom ?? 0, mixin.paddingLeft ?? 0 };

                if (padding.Any(x => x != 0))
                    this.padding = padding;
            }
            static string GetNodeFullPath(BaseNode node) => node is null ? string.Empty : Path.Combine(GetNodeFullPath(node.parent), node.name);
            void AddAutoLayout()
            {
                switch (mixin.layoutMode)
                {
                    case LayoutMode.HORIZONTAL:
                        flexDirection = FlexDirection.Row;
                        justifyContent = JustifyContent.FlexStart;
                        alignItems = Align.FlexStart;

                        if (mixin.layoutWrap is LayoutWrap.WRAP) flexWrap = FlexWrap.Wrap;
                        if (mixin.layoutWrap is LayoutWrap.NO_WRAP)
                        {
                            flexWrap = FlexWrap.Nowrap;
                            // This should be removed.
                            if (mixin.layoutAlign is LayoutAlign.STRETCH && mixin.primaryAxisSizingMode is PrimaryAxisSizingMode.FIXED)
                            {
                                flexWrap = FlexWrap.Wrap;
                                string path = GetNodeFullPath(mixin as BaseNode);
                                Debug.LogWarning(Extensions.BuildTargetMessage("Set Flex Wrap to Wrap in your Figma Document", path));
                            }
                        }

                        break;

                    case LayoutMode.VERTICAL:
                        flexDirection = FlexDirection.Column;
                        justifyContent = JustifyContent.FlexStart;
                        alignItems = Align.FlexStart;
                        if (mixin.layoutAlign != LayoutAlign.STRETCH && mixin.primaryAxisSizingMode.IsValue(PrimaryAxisSizingMode.FIXED))
                        {
                            // Figma doesn't support wrap at vertical layout, then we forcibly set it.
                            flexWrap = FlexWrap.Wrap;
                        }

                        break;
                }

                if (mixin.primaryAxisAlignItems.HasValue)
                    justifyContent = mixin.primaryAxisAlignItems.Value switch
                    {
                        PrimaryAxisAlignItems.MIN => JustifyContent.FlexStart,
                        PrimaryAxisAlignItems.CENTER => JustifyContent.Center,
                        PrimaryAxisAlignItems.MAX => JustifyContent.FlexEnd,
                        PrimaryAxisAlignItems.SPACE_BETWEEN => JustifyContent.SpaceBetween,
                        _ => throw new NotSupportedException()
                    };

                if (mixin.counterAxisAlignItems.HasValue)
                    alignItems = mixin.counterAxisAlignItems.Value switch
                    {
                        CounterAxisAlignItems.MIN => Align.FlexStart,
                        CounterAxisAlignItems.CENTER => Align.Center,
                        CounterAxisAlignItems.MAX => Align.FlexEnd,
                        CounterAxisAlignItems.BASELINE => Align.Stretch,
                        _ => throw new NotSupportedException()
                    };

                if (mixin.itemSpacing.HasPositive())
                    itemSpacing = mixin.itemSpacing;
            }

            AddPadding();

            if (mixin.layoutMode.HasValue)
                AddAutoLayout();
        }
        void AddLayout(ILayoutMixin mixin, IBaseNodeMixin @base)
        {
            if (@base is IDefaultFrameMixin frame && IsMostlyHorizontal(frame))
                flexDirection = FlexDirection.Row;
            if (@base.parent is IDefaultFrameMixin { layoutMode: not null } && mixin.layoutAlign == LayoutAlign.STRETCH)
                alignSelf = Align.Stretch;
        }
        void AddBlend(IBlendMixin mixin)
        {
            if (mixin.opacity.HasValue)
                opacity = mixin.opacity;

            if (mixin is TextNode)
                AddTextNodeEffects(mixin.effects);
            else
                AddNodeEffects(mixin.effects);
        }
        void AddGeometry(IGeometryMixin mixin, ILayoutMixin layout, IBaseNodeMixin @base)
        {
            void AddBackgroundImageForVectorNode()
            {
                (bool valid, string url) = HasImageFill(@base) ? assetsInfo.GetAssetPath(@base.id, KnownFormats.png) : assetsInfo.GetAssetPath(@base.id, KnownFormats.svg);
                if (valid)
                    backgroundImage = Url(url);
            }
            void AddBorderWidth()
            {
                if (mixin.individualStrokeWeights != null)
                {
                    if (mixin.individualStrokeWeights.left > 0) borderLeftWidth = mixin.individualStrokeWeights.left;
                    if (mixin.individualStrokeWeights.right > 0) borderRightWidth = mixin.individualStrokeWeights.right;
                    if (mixin.individualStrokeWeights.top > 0) borderTopWidth = mixin.individualStrokeWeights.top;
                    if (mixin.individualStrokeWeights.bottom > 0) borderBottomWidth = mixin.individualStrokeWeights.bottom;
                }
                else if (mixin.strokeWeight > 0) borderWidth = mixin.strokeWeight;
            }
            void AddBorderRadius(IRectangleCornerMixin rectangleCornerMixin, ICornerMixin cornerMixin)
            {
                void AddRadius(double minValue, double value)
                {
                    if (rectangleCornerMixin.rectangleCornerRadii == null)
                    {
                        if (cornerMixin.cornerRadius.HasPositive())
                            borderRadius = Math.Min(minValue, cornerMixin!.cornerRadius!.Value) + value;
                    }
                    else
                    {
                        for (int i = 0; i < rectangleCornerMixin.rectangleCornerRadii.Length; ++i)
                            rectangleCornerMixin.rectangleCornerRadii[i] = Math.Min(minValue, rectangleCornerMixin.rectangleCornerRadii[i]) + value;
                        borderRadius = rectangleCornerMixin.rectangleCornerRadii;
                    }
                }

                double value = double.NaN;
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

                double minBorderRadius = Math.Min(layout.absoluteBoundingBox.width / 2, layout.absoluteBoundingBox.height / 2);
                AddRadius(minBorderRadius, value);

                if (borderRadius == new Length4Property(Unit.Pixel))
                    Attributes.Remove("border-radius");
            }
            void AddRotation()
            {
                if ((layout.relativeTransform[0][0] == 1 && layout.relativeTransform[0][0] == 0 &&
                     layout.relativeTransform[0][0] == 0 && layout.relativeTransform[1][1] == 1) || !layout.relativeTransform[0][0].HasValue ||
                    !layout.relativeTransform[0][1].HasValue || !layout.relativeTransform[1][0].HasValue || !layout.relativeTransform[1][1].HasValue)
                    return;

                float m00 = (float)layout.relativeTransform[0][0].Value;
                float m01 = (float)layout.relativeTransform[0][1].Value;
                int rotation = Mathf.RoundToInt(Mathf.Rad2Deg * Mathf.Acos(m00 / Mathf.Sqrt(m00 * m00 + m01 * m01)));
                if (rotation != 0)
                    rotate = new LengthProperty(rotation, Unit.Degrees);
            }

            if (IsSvgNode(@base))
            {
                AddBackgroundImageForVectorNode();
                return;
            }

            if (layout.relativeTransform != null)
                AddRotation();

            if (mixin is TextNode)
            {
                AddTextFillStyle(mixin.fills);
                return;
            }

            AddFillStyle(mixin.fills);

            if (mixin.strokes.Length == 0)
                return;

            AddStrokeFillStyle(mixin.strokes);

            if (!mixin.strokeWeight.HasValue)
                return;

            AddBorderWidth();

            if (mixin.strokeAlign.HasValue)
                AddBorderRadius(mixin as IRectangleCornerMixin, mixin as ICornerMixin);
        }

        void AddCorner(ICornerMixin cornerMixin, IRectangleCornerMixin rectangleCornerMixin) =>
            borderRadius = rectangleCornerMixin.rectangleCornerRadii ?? (cornerMixin.cornerRadius.HasPositive() ? cornerMixin.cornerRadius : borderRadius);

        void AddBoxModel(ILayoutMixin layout, IConstraintMixin constraint, IGeometryMixin geometry, IBaseNodeMixin baseNode)
        {
            void AdjustSvgSize()
            {
                (bool valid, int width, int height) = assetsInfo.GetAssetSize(baseNode.id, KnownFormats.svg);

                if (valid && width > 0 && height > 0)
                    layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y, width, height);

                if (geometry.strokes.Length == 0 || geometry.strokeWeight is not > 0)
                    return;

                layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.y, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);

                if (geometry.strokeCap is null or StrokeCap.NONE)
                    return;

                layout.absoluteBoundingBox = new Rect(layout.absoluteBoundingBox.x, layout.absoluteBoundingBox.y - geometry.strokeWeight.Value / 2, layout.absoluteBoundingBox.width, layout.absoluteBoundingBox.height);
            }
            void AddSizeFromConstraint(IDefaultFrameMixin parent, LengthProperty widthProperty, LengthProperty heightProperty)
            {
                ConstraintHorizontal horizontal = constraint.constraints.horizontal;
                ConstraintVertical vertical = constraint.constraints.vertical;
                Rect rect = layout.absoluteBoundingBox;
                Rect parentRect = parent.absoluteBoundingBox;

                double borderDelta = 0;

                // unity doesn't support external, and center borders
                if (parent.strokes.Length > 0 && parent.strokeWeight is not null)
                {
                    switch (parent.strokeAlign)
                    {
                        case StrokeAlign.INSIDE:
                            borderDelta = parent.strokeWeight.Value;
                            break;

                        case StrokeAlign.CENTER:
                            borderDelta = parent.strokeWeight.Value / 2;
                            break;

                        case StrokeAlign.OUTSIDE:
                            borderDelta = parent.strokeWeight.Value;
                            break;
                    }
                }

                position = Position.Absolute;

                switch (horizontal)
                {
                    case ConstraintHorizontal.LEFT:
                        left = -(parentRect - rect).left - borderDelta;
                        width = widthProperty;
                        break;

                    case ConstraintHorizontal.RIGHT:
                        right = (parentRect - rect).right - borderDelta;
                        width = widthProperty;
                        break;

                    case ConstraintHorizontal.LEFT_RIGHT:
                        left = -(parentRect - rect).left - borderDelta;
                        right = (parentRect - rect).right - borderDelta;
                        break;

                    case ConstraintHorizontal.CENTER:
                        width = widthProperty;
                        left = new LengthProperty((rect.halfWidth - (parentRect - rect).left - borderDelta) / (parentRect.width - borderDelta * 2) * 100.0, Unit.Percent);
                        Length2Property translateProperty = translate;
                        translateProperty[0] = new LengthProperty(-50, Unit.Percent);
                        translate = translateProperty;
                        break;

                    case ConstraintHorizontal.SCALE when parentRect.width != 0:
                        left = new LengthProperty((-(parentRect - rect).left - borderDelta) / (parentRect.width - borderDelta * 2) * 100, Unit.Percent);
                        right = new LengthProperty(((parentRect - rect).right - borderDelta) / (parentRect.width - borderDelta * 2) * 100, Unit.Percent);
                        break;

                    case ConstraintHorizontal.SCALE:
                        left = new LengthProperty(0, Unit.Percent);
                        right = new LengthProperty(0, Unit.Percent);
                        break;
                }

                switch (vertical)
                {
                    case ConstraintVertical.TOP:
                        top = -(parentRect - rect).top - borderDelta;
                        height = heightProperty;
                        break;

                    case ConstraintVertical.BOTTOM:
                        bottom = (parentRect - rect).bottom - borderDelta;
                        height = heightProperty;
                        break;

                    case ConstraintVertical.TOP_BOTTOM:
                        top = -(parentRect - rect).top - borderDelta;
                        bottom = (parentRect - rect).bottom - borderDelta;
                        break;

                    case ConstraintVertical.CENTER:
                        height = heightProperty;
                        top = new LengthProperty((rect.halfHeight - (parentRect - rect).top - borderDelta) / (parentRect.height - borderDelta * 2) * 100.0, Unit.Percent);
                        Length2Property translateProperty = translate;
                        translateProperty[1] = new LengthProperty(-50, Unit.Percent);
                        translate = translateProperty;
                        break;

                    case ConstraintVertical.SCALE when parentRect.height != 0:
                        top = new LengthProperty((-(parentRect - rect).top - borderDelta) / (parentRect.height - borderDelta * 2) * 100, Unit.Percent);
                        bottom = new LengthProperty(((parentRect - rect).bottom - borderDelta) / (parentRect.height - borderDelta * 2) * 100, Unit.Percent);
                        break;

                    case ConstraintVertical.SCALE:
                        top = new LengthProperty(0, Unit.Percent);
                        bottom = new LengthProperty(0, Unit.Percent);
                        break;
                }
            }
            void AddItemSpacing(IDefaultFrameMixin parent, double itemSpacing)
            {
                if (baseNode == parent.children.LastOrDefault(IsVisible))
                    return;

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
                        return;

                    case TextAutoResize.HEIGHT:
                        height = Unit.Auto;
                        return;

                    default:
                        if (node.style.textTruncation == TextTruncation.ENDING)
                        {
                            textOverflow = TextOverflow.Ellipsis;
                            overflow = Visibility.Hidden;
                        }
                        break;
                }
            }

            if (layout.minWidth.HasValue) minWidth = layout.minWidth.Value;
            if (layout.minHeight.HasValue) minHeight = layout.minHeight.Value;
            if (layout.maxWidth.HasValue) maxWidth = layout.maxWidth.Value;
            if (layout.maxHeight.HasValue) maxHeight = layout.maxHeight.Value;
            if (IsSvgNode(baseNode)) AdjustSvgSize();

            if (!IsRootNode(baseNode))
            {
                IDefaultFrameMixin parent = (IDefaultFrameMixin)baseNode.parent;
                if (parent.layoutMode is not null && baseNode is IDefaultFrameMixin { layoutPositioning: null } or not IDefaultFrameMixin)
                {
                    bool growing = layout.layoutGrow is > 0;

                    LayoutMode direction;
                    bool primaryFixed;
                    bool counterFixed;
                    if (baseNode is IDefaultFrameMixin { layoutMode: not null } frame)
                    {
                        bool stretching = frame.layoutAlign is LayoutAlign.STRETCH;
                        (bool primaryGrowing, bool counterGrowing) = frame.layoutMode == parent.layoutMode ? (growing, stretching) : (stretching, growing);
                        (bool primarySizingFixed, bool counterSizingFixed) = (frame.primaryAxisSizingMode is PrimaryAxisSizingMode.FIXED, frame.counterAxisSizingMode is CounterAxisSizingMode.FIXED);
                        (primaryFixed, counterFixed) = (primarySizingFixed && !primaryGrowing, counterSizingFixed && !counterGrowing);
                        direction = frame.layoutMode.Value;
                    }
                    else
                    {
                        bool stretching = layout.layoutAlign is LayoutAlign.STRETCH;
                        (primaryFixed, counterFixed) = (!growing, !stretching);
                        direction = parent.layoutMode.Value;
                    }

                    if (direction is not LayoutMode.VERTICAL and not LayoutMode.HORIZONTAL) Debug.LogWarning(Extensions.BuildTargetMessage("Direction layout for", $"{parent.name}/{Name}", $" is {direction}, undefined behaviour"));
                    (bool widthFixed, bool heightFixed) = direction is LayoutMode.HORIZONTAL ? (primaryFixed, counterFixed) : (counterFixed, primaryFixed);

                    if (growing) flexGrow = 1;
                    if (widthFixed) width = layout.absoluteBoundingBox.width;
                    if (heightFixed) height = layout.absoluteBoundingBox.height;
                    if (parent.itemSpacing is not null) AddItemSpacing(parent, parent.itemSpacing.Value);
                }
                else
                {
                    bool widthFixed;
                    bool heightFixed;
                    if (baseNode is IDefaultFrameMixin { layoutMode: not null } frame)
                    {
                        (bool primarySizingFixed, bool counterSizingFixed) = (frame.primaryAxisSizingMode is PrimaryAxisSizingMode.FIXED, frame.counterAxisSizingMode is CounterAxisSizingMode.FIXED);
                        (widthFixed, heightFixed) = frame.layoutMode is LayoutMode.HORIZONTAL ? (primarySizingFixed, counterSizingFixed) : (counterSizingFixed, primarySizingFixed);
                    }
                    else
                    {
                        (widthFixed, heightFixed) = (true, true);
                    }

                    AddSizeFromConstraint(parent, widthFixed ? layout.absoluteBoundingBox.width : Unit.Auto, heightFixed ? layout.absoluteBoundingBox.height : Unit.Auto);
                }
            }

            if (baseNode is TextNode textNode && textNode.style.textAutoResize.HasValue)
                OverwriteSizeFromTextNode(textNode);
        }
        void AddFillStyle(IEnumerable<Paint> fills)
        {
            foreach (Paint fill in fills)
            {
                if (fill is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                    backgroundColor = new ColorProperty(solid.color, solid.opacity);

                if (fill is GradientPaint gradient && gradient.visible.IsEmptyOrTrue())
                {
                    (bool valid, string url) = assetsInfo.GetAssetPath(gradient.GetHash(), KnownFormats.svg);
                    if (valid) backgroundImage = Url(url);
                }

                if (fill is ImagePaint image && image.visible.IsEmptyOrTrue())
                {
                    (bool valid, string url) = assetsInfo.GetAssetPath(image.imageRef, KnownFormats.png);
                    if (valid) backgroundImage = Url(url);

                    unityBackgroundSize = image.scaleMode switch
                    {
                        ScaleMode.FILL => BackgroundSizeType.Cover,
                        ScaleMode.FIT => BackgroundSizeType.Contain,
                        _ => unityBackgroundSize
                    };
                }
            }
        }
        void AddTextFillStyle(IEnumerable<Paint> fills)
        {
            foreach (SolidPaint solid in fills.Where(x => x is SolidPaint solid && solid.visible.IsEmptyOrTrue()).Cast<SolidPaint>())
                color = new ColorProperty(solid.color, solid.opacity);
        }
        void AddStrokeFillStyle(IEnumerable<Paint> strokes)
        {
            foreach (Paint stroke in strokes)
            {
                if (stroke is SolidPaint solid && solid.visible.IsEmptyOrTrue())
                    borderColor = new ColorProperty(solid.color, solid.opacity);

                // Unity doesnt support gradient border color
                if (stroke is GradientPaint gradient && gradient.visible.IsEmptyOrTrue())
                {
                    RGBA avgColor = gradient.gradientStops.Select(stop => stop.color).GetAverageColor();
                    borderColor = new ColorProperty(avgColor, avgColor.a);
                }
            }
        }
        void AddTextStyle(TextNode.Style style)
        {
            bool TryGetFontWithExtension(string font, out string resource, out string url)
            {
                (bool ttf, string ttfPath) = assetsInfo.GetAssetPath(font, KnownFormats.ttf);
                if (ttf)
                {
                    resource = Url(ttfPath);
                    url = ttfPath;
                    return true;
                }

                (bool otf, string otfPath) = assetsInfo.GetAssetPath(font, KnownFormats.otf);
                if (otf)
                {
                    resource = Url(otfPath);
                    url = otfPath;
                    return true;
                }

                resource = Resource("Inter-Regular");
                url = ttfPath;
                return false;
            }

            void AddUnityFont()
            {
                string weightPostfix = style.fontWeight.HasValue
                    ? Enum.GetValues(typeof(FontWeight)).GetValue((int)(style.fontWeight / 100) - 1).ToString()
                    : style.fontPostScriptName.Contains('-')
                        ? style.fontPostScriptName.Split('-')[1].Replace("Index", string.Empty)
                        : string.Empty;
                string italicPostfix = style.italic.HasValue && style.italic.Value || style.fontPostScriptName.Contains(FontStyle.Italic.ToString()) ? FontStyle.Italic.ToString() : string.Empty;

                bool valid;

                if (!TryGetFontWithExtension($"{style.fontFamily}-{weightPostfix}{italicPostfix}", out string resource, out string url) && !TryGetFontWithExtension(style.fontPostScriptName, out resource, out url))
                    Debug.LogWarning(Extensions.BuildTargetMessage("Cannot find Font", $"{style.fontFamily}-{weightPostfix}{italicPostfix}", string.Empty));

                unityFont = resource;
                (valid, url) = assetsInfo.GetAssetPath($"{style.fontFamily}-{weightPostfix}{italicPostfix}", KnownFormats.asset);

                if (valid)
                    unityFontDefinition = Url(url);
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

            if (style.textAutoResize is TextAutoResize.NONE or TextAutoResize.HEIGHT) whiteSpace = Wrap.Normal;
            if (style.fontSize.HasValue) fontSize = style.fontSize;
            if (style.fontPostScriptName.NullOrEmpty() && style.fontFamily == "Inter") style.fontPostScriptName = "Inter-Regular";
            if (style.fontPostScriptName.NotNullOrEmpty()) AddUnityFont();
            if (style.textAlignVertical.HasValue && style.textAlignHorizontal.HasValue) AddTextAlign();
        }
        void AddTextNodeEffects(IEnumerable<Effect> effects)
        {
            foreach (ShadowEffect effect in effects.Where(x => x is ShadowEffect { visible: true }).Cast<ShadowEffect>())
                textShadow = new ShadowProperty(effect.offset.x, effect.offset.y, effect.radius, effect.color);
        }
        void AddNodeEffects(IEnumerable<Effect> effects)
        {
            foreach (ShadowEffect effect in effects.Where(x => x is ShadowEffect { visible: true }).Cast<ShadowEffect>())
                boxShadow = new ShadowProperty(effect.offset.x, effect.offset.y, effect.radius, effect.color);
        }
        #endregion

        #region Support Methods
        internal static List<UssStyle> MakeTransitionStyles(UssStyle root, UssStyle idle, UssStyle hover = null, UssStyle active = null)
        {
            List<UssStyle> transitions = new() { new UssStyle(root.Name) { Target = idle, opacity = 1 } };

            if (hover != null) transitions.Add(new UssStyle(root.Name) { Target = idle, PseudoClass = PseudoClass.Hover, opacity = 0 });
            if (active != null) transitions.Add(new UssStyle(root.Name) { Target = idle, PseudoClass = PseudoClass.Active, opacity = 0 });

            if (hover != null)
            {
                transitions.Add(new UssStyle(root.Name) { Target = hover, opacity = 0 });
                transitions.Add(new UssStyle(root.Name) { Target = hover, PseudoClass = PseudoClass.Hover, opacity = 1 });
                if (active != null) transitions.Add(new UssStyle(root.Name) { Target = hover, PseudoClass = PseudoClass.Active, opacity = 0 });
            }

            if (active != null)
            {
                transitions.Add(new UssStyle(root.Name) { Target = active, opacity = 0 });

                if (hover != null)
                    transitions.Add(new UssStyle(root.Name) { Target = active, PseudoClass = PseudoClass.Hover, opacity = 0 });

                transitions.Add(new UssStyle(root.Name) { Target = active, PseudoClass = PseudoClass.Active, opacity = 1 });
            }

            return transitions;
        }
        static bool IsMostlyHorizontal(IDefaultFrameMixin mixin)
        {
            (int horizontalCenterCount, int verticalCenterCount) = CenterChildrenCount(mixin);
            return horizontalCenterCount > verticalCenterCount;
        }
        static (int, int) CenterChildrenCount(IDefaultFrameMixin mixin)
        {
            if (mixin.layoutMode.HasValue)
                return (0, 0);

            int horizontalCenterCount = mixin.children.Cast<IConstraintMixin>().Count(x => x.constraints.horizontal == ConstraintHorizontal.CENTER);
            int verticalCenterCount = mixin.children.Cast<IConstraintMixin>().Count(x => x.constraints.vertical == ConstraintVertical.CENTER);
            return (horizontalCenterCount, verticalCenterCount);
        }
        #endregion
    }
}