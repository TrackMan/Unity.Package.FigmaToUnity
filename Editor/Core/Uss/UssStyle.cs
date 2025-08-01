using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma.Core.Uss
{
    using Assets;
    using Internals;

    internal class UssStyle : BaseUssStyle
    {
        /// Problem with figma 2 Unity is that, Unity’s box-sizing property is always border-box, while figma's is a content-box with changing borders. See the MDN documentation https://developer.mozilla.org/en-US/docs/Web/CSS/box-sizing

        #region Const
        public const double tolerance = 0.01;
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
        LengthProperty width { get => Get(nameof(width)); set => Set(nameof(width), value); }
        LengthProperty height { get => Get(nameof(height)); set => Set(nameof(height), value); }
        LengthProperty minWidth { get => Get("min-width"); set => Set("min-width", value); }
        LengthProperty minHeight { get => Get("min-height"); set => Set("min-height", value); }
        LengthProperty maxWidth { get => Get("max-width"); set => Set("max-width", value); }
        LengthProperty maxHeight { get => Get("max-height"); set => Set("max-height", value); }
        // Margins
        LengthProperty marginLeft { get => Get1("margin-left", nameof(margin), 0); set => Set4("margin-left", value, nameof(margin), 0); }
        LengthProperty marginTop { get => Get1("margin-top", nameof(margin), 1); set => Set4("margin-top", value, nameof(margin), 1); }
        LengthProperty marginRight { get => Get1("margin-right", nameof(margin), 2); set => Set4("margin-right", value, nameof(margin), 2); }
        LengthProperty marginBottom { get => Get1("margin-bottom", nameof(margin), 3); set => Set4("margin-bottom", value, nameof(margin), 3); }
        Length4Property margin { get => Get4(nameof(margin), "margin-left", "margin-top", "margin-right", "margin-bottom"); set => Set1(nameof(margin), value, "margin-left", "margin-top", "margin-right", "margin-bottom"); }
        // Borders
        LengthProperty borderLeftWidth { get => Get1("border-left-width", "border-width", 0); set => Set4("border-left-width", value, "border-width", 0); }
        LengthProperty borderTopWidth { get => Get1("border-top-width", "border-width", 1); set => Set4("border-top-width", value, "border-width", 1); }
        LengthProperty borderRightWidth { get => Get1("border-right-width", "border-width", 2); set => Set4("border-right-width", value, "border-width", 2); }
        LengthProperty borderBottomWidth { get => Get1("border-bottom-width", "border-width", 3); set => Set4("border-bottom-width", value, "border-width", 3); }
        Length4Property borderWidth { get => Get4("border-width", "border-left-width", "border-top-width", "border-right-width", "border-bottom-width"); set => Set1("border-width", value, "border-left-width", "border-top-width", "border-right-width", "border-bottom-width"); }
        // Padding
        LengthProperty paddingLeft { get => Get1("padding-left", nameof(padding), 0); set => Set4("padding-left", value, nameof(padding), 0); }
        LengthProperty paddingTop { get => Get1("padding-top", nameof(padding), 1); set => Set4("padding-top", value, nameof(padding), 1); }
        LengthProperty paddingRight { get => Get1("padding-right", nameof(padding), 2); set => Set4("padding-right", value, nameof(padding), 2); }
        LengthProperty paddingBottom { get => Get1("padding-bottom", nameof(padding), 3); set => Set4("padding-bottom", value, nameof(padding), 3); }
        Length4Property padding { get => Get4(nameof(padding), "padding-left", "padding-top", "padding-right", "padding-bottom"); set => Set1(nameof(padding), value, "padding-left", "padding-top", "padding-right", "padding-bottom"); }

        // Flex
        // Items
        NumberProperty flexGrow { get => Get("flex-grow"); set => Set("flex-grow", value); }
        NumberProperty flexShrink { get => Get("flex-shrink"); set => Set("flex-shrink", value); }
        LengthProperty flexBasis { get => Get("flex-basis"); set => Set("flex-basis", value); }
        FlexProperty flex { get => Get(nameof(flex)); set => Set(nameof(flex), value); }
        EnumProperty<Align> alignSelf { get => Get("align-self"); set => Set("align-self", value); }
        NumberProperty itemSpacing { get => Get("--item-spacing"); set => Set("--item-spacing", value); }
        // Containers
        EnumProperty<FlexDirection> flexDirection { get => Get("flex-direction"); set => Set("flex-direction", value); }
        EnumProperty<FlexWrap> flexWrap { get => Get("flex-wrap"); set => Set("flex-wrap", value); }
        EnumProperty<Align> alignContent { get => Get("align-content"); set => Set("align-content", value); }
        EnumProperty<Align> alignItems { get => Get("align-items"); set => Set("align-items", value); }
        EnumProperty<JustifyContent> justifyContent { get => Get("justify-content"); set => Set("justify-content", value); }
        // Positioning
        EnumProperty<Position> position { get => Get(nameof(position)); set => Set(nameof(position), value); }
        LengthProperty left { get => Get(nameof(left)); set => Set(nameof(left), value); }
        LengthProperty top { get => Get(nameof(top)); set => Set(nameof(top), value); }
        LengthProperty right { get => Get(nameof(right)); set => Set(nameof(right), value); }
        LengthProperty bottom { get => Get(nameof(bottom)); set => Set(nameof(bottom), value); }
        LengthProperty rotate { get => Get(nameof(rotate)); set => Set(nameof(rotate), value); }
        Length2Property translate { get => GetDefault(nameof(translate), "0 0"); set => Set(nameof(translate), value); }

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
        EnumProperty<Visibility> overflow { get => Get(nameof(overflow)); set => Set(nameof(overflow), value); }
        EnumProperty<OverflowClip> unityOverflowClipBox { get => Get("-unity-overflow-clip-box"); set => Set("-unity-overflow-clip-box", value); }
        NumberProperty opacity { get => Get(nameof(opacity)); set => Set(nameof(opacity), value); }
        EnumProperty<Visibility> visibility { get => Get(nameof(visibility)); set => Set(nameof(visibility), value); }
        EnumProperty<Display> display { get => Get(nameof(display)); set => Set(nameof(display), value); }

        // Text
        ColorProperty color { get => Get(nameof(color)); set => Set(nameof(color), value); }
        AssetProperty unityFont { get => Get("-unity-font"); set => Set("-unity-font", value); }
        AssetProperty unityFontDefinition { get => Get("-unity-font-definition"); set => Set("-unity-font-definition", value); }
        LengthProperty fontSize { get => Get("font-size"); set => Set("font-size", value); }
        EnumProperty<FontStyle> unityFontStyle { get => Get("-unity-font-style"); set => Set("-unity-font-style", value); }
        EnumProperty<TextAlign> unityTextAlign { get => Get("-unity-text-align"); set => Set("-unity-text-align", value); }
        EnumProperty<Wrap> whiteSpace { get => Get("white-space"); set => Set("white-space", value); }
        ShadowProperty textShadow { get => Get("text-shadow"); set => Set("text-shadow", value); }
        EnumProperty<TextOverflow> textOverflow { get => Get("text-overflow"); set => Set("text-overflow", value); }

        // Cursor
        CursorProperty cursor { get => Get(nameof(cursor)); set => Set(nameof(cursor), value); }

        // Effects
        ShadowProperty boxShadow { get => Get("--box-shadow"); set => Set("--box-shadow", value); }

        // Transitions
        internal DurationProperty transitionDuration { get => Get("transition-duration"); set => Set("transition-duration", value); }
        internal EnumProperty<EasingFunction> transitionEasing { get => Get("transition-timing-function"); set => Set("transition-timing-function", value); }
        #endregion

        #region Constructors
        public UssStyle(string name) : base(name) { }
        public UssStyle(string name, AssetsInfo assetsInfo) : this(name) => this.assetsInfo = assetsInfo;
        public UssStyle(string name, AssetsInfo assetsInfo, BaseNode node, StyleSlot styleSlot) : this(name, assetsInfo)
        {
            switch (styleSlot.styleType)
            {
                case StyleType.FILL when node is IGeometryMixin geometry:
                    switch (styleSlot.Slot)
                    {
                        case "fill":
                        {
                            if (node is TextNode)
                                Name += "-Text";
                            AddFill(geometry);
                            break;
                        }

                        case "stroke":
                            Name += node is TextNode ? "-TextStroke" : "-Border";
                            AddStrokeColor(geometry);
                            break;
                    }

                    break;

                case StyleType.TEXT when node is TextNode text:
                    AddSharedTextStyle(text.style);
                    break;

                case StyleType.EFFECT when node is IBlendMixin blend:
                    AddBlend(blend);
                    break;

                case StyleType.GRID or StyleType.NONE:
                    LogWarningIgnoredFigmaProperty(node, $"{nameof(styleSlot)} {nameof(styleSlot.styleType)} with {styleSlot.styleType}");
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
        public UssStyle(string name, AssetsInfo assetsInfo, BaseNode node) : this(name, assetsInfo)
        {
            if (node.IsSvgNode())
            {
                AddSvg(assetsInfo, node); // AddSvg has to be called first in the constructor, because it overwrites its own boundingbox
            }
            else
            {
                if (node is IGeometryMixin geometry)
                    AddGeometry(geometry);
                AddBorderRadius(node);
            }

            if (node is IBlendMixin blend)
                AddBlend(blend);
            if (node is ILayoutMixin layout && !node.IsRootNode())
                AddLayout(layout);
            if (node is IDefaultFrameMixin frame)
                AddFrame(frame);
            if (node is TextNode text)
                AddText(text);
        }
        #endregion

        #region Methods
        internal UssStyle CopyFrom(UssStyle style)
        {
            style.Attributes.ForEach(x => Attributes[x.Key] = x.Value);
            return this;
        }
        void AddFrame(IDefaultFrameMixin frame)
        {
            if (frame.clipsContent)
                overflow = Visibility.Hidden;

            LayoutDouble4 correctedPadding = frame.GetCorrectedPadding();
            if (correctedPadding.Any())
                padding = correctedPadding.ToLength4Property();

            if (frame.layoutMode is LayoutMode.NONE)
                return;

            if (frame.layoutWrap is LayoutWrap.WRAP)
                flexWrap = FlexWrap.Wrap;
            if (frame.layoutMode is LayoutMode.HORIZONTAL)
                flexDirection = FlexDirection.Row;

            justifyContent = frame.primaryAxisAlignItems switch
            {
                PrimaryAxisAlignItems.MIN => JustifyContent.FlexStart,
                PrimaryAxisAlignItems.CENTER => JustifyContent.Center,
                PrimaryAxisAlignItems.MAX => JustifyContent.FlexEnd,
                PrimaryAxisAlignItems.SPACE_BETWEEN => JustifyContent.SpaceBetween,
                _ => throw new NotSupportedException()
            };

            alignItems = frame.counterAxisAlignItems switch
            {
                CounterAxisAlignItems.MIN => Align.FlexStart,
                CounterAxisAlignItems.CENTER => Align.Center,
                CounterAxisAlignItems.MAX => Align.FlexEnd,
                CounterAxisAlignItems.BASELINE => Align.Center,
                _ => throw new NotSupportedException()
            };

            if (frame.itemSpacing > 0.0)
                itemSpacing = frame.itemSpacing;
        }
        void AddGeometry(IGeometryMixin geometry)
        {
            AddFill(geometry);

            if (!geometry.HasBorder())
                return;

            AddStrokeColor(geometry);
            if (geometry.individualStrokeWeights != null)
            {
                if (geometry.individualStrokeWeights.left > 0)
                    borderLeftWidth = geometry.individualStrokeWeights.left;
                if (geometry.individualStrokeWeights.right > 0)
                    borderRightWidth = geometry.individualStrokeWeights.right;
                if (geometry.individualStrokeWeights.top > 0)
                    borderTopWidth = geometry.individualStrokeWeights.top;
                if (geometry.individualStrokeWeights.bottom > 0)
                    borderBottomWidth = geometry.individualStrokeWeights.bottom;
            }
            else
            {
                borderWidth = geometry.strokeWeight;
            }
        }
        void AddLayout(ILayoutMixin layout)
        {
            void SetPositioning(IDefaultFrameMixin parent)
            {
                Rect frameBorderBox = layout.GetBorderBox();
                Rect parentContentBox = parent.GetContentBox();
                double x = frameBorderBox.x - parentContentBox.x;
                double y = frameBorderBox.y - parentContentBox.y;
                double r = parentContentBox.width - frameBorderBox.width - x;
                double b = parentContentBox.height - frameBorderBox.height - y;

                switch (layout.constraints.horizontal)
                {
                    case ConstraintHorizontal.LEFT:
                        left = x;
                        break;

                    case ConstraintHorizontal.RIGHT:
                        right = r;
                        break;

                    case ConstraintHorizontal.LEFT_RIGHT:
                        left = x;
                        right = r;
                        break;

                    case ConstraintHorizontal.CENTER:
                        if (parent.layoutMode is LayoutMode.VERTICAL or LayoutMode.NONE)
                            alignSelf = Align.Center;
                        else if (parent.primaryAxisAlignItems is not PrimaryAxisAlignItems.CENTER)
                            LogWarningImpossibleDesign((IBaseNodeMixin)layout, $"Has center constraint on {nameof(LayoutMode.HORIZONTAL)} axis, but parent doesnt align items center it has {parent.layoutMode}.");
                        if (parent.HasBorder() && parent.strokeAlign is not StrokeAlign.OUTSIDE)
                            LogWarningImpossibleDesign((IBaseNodeMixin)layout, $"Center constraint should only be combined with parent that has {nameof(StrokeAlign.OUTSIDE)} border. Parent has {parent.strokeAlign}. The scaling will be off by a few pixels when growing");

                        double cx = parentContentBox.halfWidth - frameBorderBox.halfWidth - x;
                        if (Math.Abs(cx) >= tolerance)
                            translate = new Length2Property(new LengthProperty[] { -cx, 0.0 });

                        break;

                    case ConstraintHorizontal.SCALE when parentContentBox.width > 0.0:
                        left = new LengthProperty(100.0 * x / parentContentBox.width, Unit.Percent);
                        right = new LengthProperty(100.0 * r / parentContentBox.width, Unit.Percent);
                        break;

                    case ConstraintHorizontal.SCALE:
                        break;

                    default:
                        throw new NotSupportedException();
                }

                switch (layout.constraints.vertical)
                {
                    case ConstraintVertical.TOP:
                        top = y;
                        break;

                    case ConstraintVertical.BOTTOM:
                        bottom = b;
                        break;

                    case ConstraintVertical.TOP_BOTTOM:
                        top = y;
                        bottom = b;
                        break;

                    case ConstraintVertical.CENTER:
                        if (parent.layoutMode is not LayoutMode.HORIZONTAL)
                            alignSelf = Align.Center;
                        else if (parent.primaryAxisAlignItems is not PrimaryAxisAlignItems.CENTER)
                            LogWarningImpossibleDesign((IBaseNodeMixin)layout, $"Has center constraint on {nameof(LayoutMode.VERTICAL)} axis, but parent doesnt align items center it has {parent.layoutMode}");
                        if (parent.HasBorder() && parent.strokeAlign is not StrokeAlign.OUTSIDE)
                            LogWarningImpossibleDesign((IBaseNodeMixin)layout, $"Center constraint should only be combined with parent that has {nameof(StrokeAlign.OUTSIDE)} border. Parent has {parent.strokeAlign}. The scaling will be off by a few pixels when growing");

                        double cy = parentContentBox.halfHeight - frameBorderBox.halfHeight - y;
                        if (Math.Abs(cy) >= tolerance)
                            translate = new Length2Property(new[] { translate[0], -cy });
                        break;

                    case ConstraintVertical.SCALE when parentContentBox.height > 0.0:
                        top = new LengthProperty(100.0 * y / parentContentBox.height, Unit.Percent);
                        bottom = new LengthProperty(100.0 * b / parentContentBox.height, Unit.Percent);
                        break;

                    case ConstraintVertical.SCALE:
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
            bool BoxSizingCorrection(IDefaultFrameMixin parent)
            {
                // When strokesIncludedInLayout it's the same as box-sizing: border-box
                if (parent.strokesIncludedInLayout)
                    return false;

                // We modify position of elements to emulate negative padding. However, in UI Toolkit child cant to grow bigger than its parent.
                LayoutDouble4 parentNegativePadding = parent.GetCorrectedPadding().OnlyNegativeValues();

                bool anyHorizontal = parentNegativePadding.left < -tolerance || parentNegativePadding.right < -tolerance;
                bool anyVertical = parentNegativePadding.top < -tolerance || parentNegativePadding.bottom < -tolerance;
                bool wrongHorizontal = layout.layoutSizingHorizontal is LayoutSizing.FILL && anyHorizontal;
                bool wrongVertical = layout.layoutSizingVertical is LayoutSizing.FILL && anyVertical;
                if (wrongHorizontal || wrongVertical)
                {
                    LogWarningImpossibleDesign((IBaseNodeMixin)layout, $"{nameof(LayoutSizing.FILL)} on node with child bounding box being outside parents content, Unity auto layout doesn't allow child to outgrow parents content. " +
                                                                       $"Wrong {(wrongHorizontal && wrongVertical ? "Horizontal and Vertical" : wrongHorizontal ? "Horizontal" : "Vertical")} axis. " +
                                                                       $"Element has ({layout.layoutSizingHorizontal}, {layout.layoutSizingVertical}), parent has border {parent.strokeAlign} with width {(string)parent.GetBorderWidths().ToLength4Property()}. " +
                                                                       "Unity wont grow child above parent. Possible fixes:" +
                                                                       $"\n1: Change {nameof(IDefaultFrameMixin.strokesIncludedInLayout)} in Auto layout settings to included." +
                                                                       $"\n2: Use {nameof(LayoutSizing.FIXED)} or {nameof(LayoutSizing.HUG)}\n3: increase padding, currently missing {(string)(-parentNegativePadding).ToLength4Property()} more padding to make them not overlap." +
                                                                       $"\n3: make parent border {nameof(StrokeAlign.OUTSIDE)}");
                    return false;
                }

                bool horizontal = parent.layoutMode is LayoutMode.HORIZONTAL;
                double primary = parent.primaryAxisAlignItems switch
                {
                    PrimaryAxisAlignItems.MIN or PrimaryAxisAlignItems.SPACE_BETWEEN => 1.0 * (horizontal ? parentNegativePadding.left : parentNegativePadding.top),
                    PrimaryAxisAlignItems.MAX => -1.0 * (horizontal ? parentNegativePadding.right : parentNegativePadding.bottom),
                    PrimaryAxisAlignItems.CENTER => 0.5 * (horizontal ? parentNegativePadding.left - parentNegativePadding.right : parentNegativePadding.top - parentNegativePadding.bottom),
                    _ => 0.0
                };
                double counter = parent.counterAxisAlignItems switch
                {
                    CounterAxisAlignItems.MIN => 1.0 * (horizontal ? parentNegativePadding.top : parentNegativePadding.left),
                    CounterAxisAlignItems.MAX => -1.0 * (horizontal ? parentNegativePadding.bottom : parentNegativePadding.right),
                    CounterAxisAlignItems.CENTER => 0.5 * (horizontal ? parentNegativePadding.top - parentNegativePadding.bottom : parentNegativePadding.left - parentNegativePadding.right),
                    _ => 0.0
                };

                (double leftCorrection, double topCorrection) = horizontal ? (primary, counter) : (counter, primary);
                if (Math.Abs(topCorrection) > tolerance)
                    top = topCorrection;
                if (Math.Abs(leftCorrection) > tolerance)
                    left = leftCorrection;

                return true;
            }
            IDefaultFrameMixin parent = (IDefaultFrameMixin)((IBaseNodeMixin)layout).parent;

            bool ignoreAutoLayout = layout is IDefaultFrameMixin { layoutPositioning: LayoutPositioning.ABSOLUTE };
            bool forceAutoHorizontal = false;
            bool forceAutoVertical = false;
            if (parent.layoutMode is LayoutMode.NONE || ignoreAutoLayout)
            {
                forceAutoHorizontal = layout.constraints.horizontal is ConstraintHorizontal.SCALE or ConstraintHorizontal.LEFT_RIGHT;
                forceAutoVertical = layout.constraints.vertical is ConstraintVertical.SCALE or ConstraintVertical.TOP_BOTTOM;

                position = Position.Absolute;
                SetPositioning(parent);
            }
            else
            {
                LayoutDouble4 margin4 = new();

                if (BoxSizingCorrection(parent))
                    margin4 -= (layout as IGeometryMixin).GetOutsideBorderWidths();
                if (Math.Abs(parent.itemSpacing) > tolerance && parent.primaryAxisAlignItems is not PrimaryAxisAlignItems.SPACE_BETWEEN && layout != parent.children.LastOrDefault(x => x.IsVisible()))
                {
                    if (parent.layoutMode is LayoutMode.HORIZONTAL)
                        margin4.right += parent.itemSpacing;
                    else
                        margin4.bottom += parent.itemSpacing;
                }

                if (margin4.Any())
                    margin = margin4.ToLength4Property();
                LayoutSizing primarySizing = parent.layoutMode is LayoutMode.HORIZONTAL ? layout.layoutSizingHorizontal : layout.layoutSizingVertical;
                if (primarySizing is LayoutSizing.FIXED or LayoutSizing.HUG)
                    flexShrink = 0.0; // Shrink clamps child to parent. Figma ignores this when using fixed or hug.
            }

            if (layout.layoutGrow > 0)
                flexGrow = 1;
            if (layout.layoutAlign is LayoutAlign.STRETCH)
                alignSelf = Align.Stretch;

            Rect borderBox = layout.GetBorderBox(); // Unity uses border-box
            if (layout.layoutSizingHorizontal is LayoutSizing.FIXED && !forceAutoHorizontal)
                width = borderBox.width;
            if (layout.layoutSizingVertical is LayoutSizing.FIXED && !forceAutoVertical)
                height = borderBox.height;

            if (layout.minWidth != null)
                minWidth = layout.minWidth.Value;
            if (layout.minHeight != null)
                minHeight = layout.minHeight.Value;
            if (layout.maxWidth != null)
                maxWidth = layout.maxWidth.Value;
            if (layout.maxHeight != null)
                maxHeight = layout.maxHeight.Value;

            const double rad2deg = 180.0 / Math.PI;
            if (Math.Abs(layout.rotation) > tolerance && !((IBaseNodeMixin)layout).IsSvgNode())
            {
                LogWarningImpossibleDesign(layout as IBaseNodeMixin, "Rotation and anchors are different in figma and unity. Its best to remove rotation from figma elements. In Figma rotation is applied and then constraints. In unity anchors are applied first. Meaning a full screen figma 1920x1080 becomes a 1080x1920 in unity.");
                rotate = new LengthProperty(layout.rotation * rad2deg, Unit.Degrees);
            }

            if (layout is TextNode { style: { textAutoResize: TextAutoResize.WIDTH_AND_HEIGHT } } text)
            {
                if (parent.counterAxisAlignItems is CounterAxisAlignItems.BASELINE) // Baseline is not supported by unity, so we emulate it
                {
                    double maxSize = parent.children.OfType<TextNode>().Max(x => x.style.fontSize);
                    const double fontSizeToOffset = 1.0 / 2.75; // Approximate value gotten through trail and error. This works when parent is CounterAxisCenter, siblings have same font family and height is set to auto. Otherwise its random if it works or not
                    double baselineOffset = (maxSize - text.style.fontSize) * fontSizeToOffset;
                    if (Math.Abs(baselineOffset) > tolerance)
                        top = baselineOffset;
                }
                else
                {
                    const double idealLineHeightFactor = 1.2;
                    height = text.style.lineHeightPx;                                           // Unity text only aligns correctly when height is 1.2 times font size. Figma allows any text height
                    if (text.style.lineHeightPx <= text.style.fontSize * idealLineHeightFactor) // Figma centers text when lineheight is too small
                        text.style.textAlignVertical = TextAlignVertical.CENTER;
                }
            }
        }
        void AddText(TextNode text)
        {
            TextNode.Style style = text.style;
            string horizontal = style.textAlignHorizontal switch
            {
                TextAlignHorizontal.LEFT => nameof(left),
                TextAlignHorizontal.RIGHT => nameof(right),
                TextAlignHorizontal.CENTER => "center",
                TextAlignHorizontal.JUSTIFIED => throw new NotSupportedException(),
                _ => throw new NotSupportedException()
            };
            string vertical = style.textAlignVertical switch
            {
                TextAlignVertical.TOP => "upper",
                TextAlignVertical.BOTTOM => "lower",
                TextAlignVertical.CENTER => "middle",
                _ => throw new NotSupportedException()
            };

            unityTextAlign = (EnumProperty<TextAlign>)$"{vertical}-{horizontal}";

            if (style.textTruncation is TextTruncation.ENDING)
            {
                textOverflow = TextOverflow.Ellipsis;
                overflow = Visibility.Hidden;
            }

            if (style.textAutoResize is TextAutoResize.NONE || (style.textAutoResize is TextAutoResize.HEIGHT && style.maxLines is not 1))
                whiteSpace = Wrap.Normal;

            AddSharedTextStyle(style);
        }
        void AddBlend(IBlendMixin blend)
        {
            if (blend.opacity < 1.0 - tolerance)
                opacity = blend.opacity;

            IEnumerable<ShadowEffect> effects = blend.effects.OfType<ShadowEffect>().Where(x => x.visible);
            ShadowEffect effect = effects.FirstOrDefault();
            if (effect == null)
                return;

            if (effects.Count() > 1)
                LogWarningIgnoredFigmaProperty((IBaseNodeMixin)blend, $"More than 1 effects, we support 1 effect per element, this has {effects.Count()}");

            ShadowProperty shadow = new(effect.offset.x, effect.offset.y, effect.radius, effect.color);
            if (blend is TextNode text)
            {
                if (effect.type is EffectType.DROP_SHADOW)
                    textShadow = shadow;
                else
                    LogWarningImpossibleDesign(text, $"Cant use {effect.type} for text we can only use {nameof(EffectType.DROP_SHADOW)}");
            }
            else
            {
                boxShadow = shadow;
                LogWarningIgnoredFigmaProperty((IBaseNodeMixin)blend, "Effects on elements, we support effects on text only");
            }
        }
        void AddBorderRadius(BaseNode node)
        {
            // Unity makes each corner as round as possible while figma limits borders to max 50% of size. In Figma when a corner is bigger than the rest that corner can make other corner less round, we dont know how to recreate that effect in unity
            // Figma makes outside borders have radius equal to radius + border
            double maxBorderRadius = node is ILayoutMixin layout ? Math.Min(layout.GetBorderBox().width, layout.GetBorderBox().height) * 0.5 : double.MaxValue;
            LayoutDouble4 outsideBorder = node is IGeometryMixin geometry ? geometry.GetOutsideBorderWidths() : new LayoutDouble4();
            if (node is ICornerMixin { cornerRadius: > 0 } corner)
            {
                LayoutDouble4 rad = outsideBorder + new LayoutDouble4(corner.cornerRadius.Value);
                LayoutDouble4 radius = new(Math.Min(maxBorderRadius, rad.top), Math.Min(maxBorderRadius, rad.left), Math.Min(maxBorderRadius, rad.bottom), Math.Min(maxBorderRadius, rad.right));
                borderRadius = radius.ToLength4Property();
            }
            else if (node is IRectangleCornerMixin { rectangleCornerRadii: not null } rectangleCorner && rectangleCorner.rectangleCornerRadii.Any(x => x > 0.0))
            {
                double GetBorderCorrection(double radius, double border) => radius == 0.0 ? 0.0 : Math.Min(maxBorderRadius, radius + border);
                double[] radii = rectangleCorner.rectangleCornerRadii;
                LayoutDouble4 radius = new(GetBorderCorrection(radii[0], outsideBorder.top), GetBorderCorrection(radii[1], outsideBorder.right), GetBorderCorrection(radii[2], outsideBorder.bottom), GetBorderCorrection(radii[3], outsideBorder.left));
                borderRadius = radius.ToLength4Property();
            }
        }
        void AddSvg(AssetsInfo assetsInfo, BaseNode svg)
        {
            IGeometryMixin geometry = (IGeometryMixin)svg;
            ILayoutMixin layout = (ILayoutMixin)svg;
            Rect boundingBox = layout.absoluteBoundingBox;
            if (geometry.HasBorder())
            {
                boundingBox.x -= geometry.strokeWeight / 2.0;
                if (geometry.strokeCap is not StrokeCap.NONE)
                    boundingBox.y -= geometry.strokeWeight / 2.0;
            }

            layout.absoluteBoundingBox = boundingBox;
            string extension = svg.HasImage() ? KnownFormats.png : KnownFormats.svg;
            if (assetsInfo.GetAssetPath(svg.id, extension, out string url))
                backgroundImage = Url(url);
        }
        #endregion

        #region Support Methods
        void AddSharedTextStyle(TextNode.Style style)
        {
            bool TryGetFontWithExtension(string font, out string resource)
            {
                if (assetsInfo.GetAssetPath(font, KnownFormats.ttf, out string ttfPath))
                {
                    resource = Url(ttfPath);
                    return true;
                }

                if (assetsInfo.GetAssetPath(font, KnownFormats.otf, out string otfPath))
                {
                    resource = Url(otfPath);
                    return true;
                }

                resource = Resource("Inter-Regular");
                return false;
            }

            (string, string) GetFont()
            {
                string fontPostScriptName = style.fontPostScriptName ?? (style.fontFamily == "Inter" ? "Inter-Regular" : null);

                if (fontPostScriptName == null)
                    return (null, null);

                string weightPostfix;
                if (style.fontWeight > 0)
                    weightPostfix = Enum.GetValues(typeof(FontWeight)).GetValue((int)(style.fontWeight / 100) - 1).ToString();
                else
                    weightPostfix = fontPostScriptName.Contains('-') ? fontPostScriptName.Split('-')[1].Replace(nameof(Index), string.Empty) : string.Empty;

                string italicPostfix = style.italic || fontPostScriptName.Contains(nameof(FontStyle.Italic)) ? nameof(FontStyle.Italic) : string.Empty;

                string fontName = $"{style.fontFamily}-{weightPostfix}{italicPostfix}";
                if (!TryGetFontWithExtension(fontName, out string font) && !TryGetFontWithExtension(fontPostScriptName, out font))
                    Debug.LogWarning(Extensions.BuildTargetMessage("Cannot find Font", fontName, string.Empty));

                bool exists = assetsInfo.GetAssetPath(fontName, KnownFormats.asset, out string path);
                return (font, exists ? path : null);
            }

            fontSize = style.fontSize;
            (string font, string fontDefinition) = GetFont();

            if (font != null)
                unityFont = font;

            if (fontDefinition != null)
                unityFontDefinition = Url(fontDefinition);
        }
        void AddFill(IGeometryMixin geometry)
        {
            bool urlExists = false;
            string url = string.Empty;
            RGBA finalColor = new RGBA();
            foreach (Paint fill in geometry.fills.Where(x => x.visible).Reverse())
            {
                switch (fill)
                {
                    case SolidPaint solid:
                        solid.color.a = solid.opacity;
                        finalColor = finalColor.BlendWith(solid.color);
                        break;

                    case GradientPaint gradient:
                        if (geometry is TextNode)
                        {
                            RGBA average = gradient.gradientStops.Select(stop => stop.color).GetAverageColor();
                            average.a = gradient.opacity;
                            finalColor = finalColor.BlendWith(average);
                        }
                        else
                        {
                            urlExists = assetsInfo.GetAssetPath(gradient.GetHash(), KnownFormats.svg, out url);
                        }

                        break;

                    case ImagePaint image:
                        if (geometry is TextNode text)
                        {
                            LogWarningImpossibleDesign(text, $"{nameof(TextElement)} with images, unity places images in background, while figma puts them inside the text");
                            break;
                        }

                        urlExists = assetsInfo.GetAssetPath(image.imageRef, KnownFormats.png, out url);

                        unityBackgroundSize = image.scaleMode switch
                        {
                            ScaleMode.FILL => BackgroundSizeType.Cover,
                            ScaleMode.FIT => BackgroundSizeType.Contain,
                            _ => unityBackgroundSize
                        };
                        break;
                }

                if (geometry is TextNode)
                    color = new ColorProperty(finalColor);
                else
                    backgroundColor = new ColorProperty(finalColor);
                if (urlExists)
                    backgroundImage = Url(url);
            }
        }
        void AddStrokeColor(IGeometryMixin geometry)
        {
            if (geometry is TextNode)
            {
                LogWarningIgnoredFigmaProperty((IBaseNodeMixin)geometry, "Stroke on text"); // Stroke on text might not be possible at all as unity makes inside stroke and figma outside stroke
                return;
            }

            RGBA finalColor = new();
            foreach (Paint stroke in geometry.strokes.Where(x => x.visible).Reverse())
            {
                RGBA color = stroke switch
                {
                    SolidPaint solid => solid.color,
                    GradientPaint gradient => gradient.gradientStops.Select(stop => stop.color).GetAverageColor(),
                    _ => throw new NotSupportedException()
                };
                color.a = stroke.opacity;
                finalColor = finalColor.BlendWith(color);
            }

            borderColor = finalColor;
        }
        internal static List<UssStyle> MakeTransitionStyles(UssStyle root, UssStyle idle, UssStyle hover = null, UssStyle active = null)
        {
            List<UssStyle> transitions = new() { new UssStyle(root.Name) { Target = idle, opacity = 1 } };

            if (hover != null)
                transitions.Add(new UssStyle(root.Name) { Target = idle, PseudoClass = PseudoClass.Hover, opacity = 0 });
            if (active != null)
                transitions.Add(new UssStyle(root.Name) { Target = idle, PseudoClass = PseudoClass.Active, opacity = 0 });

            if (hover != null)
            {
                transitions.Add(new UssStyle(root.Name) { Target = hover, opacity = 0 });
                transitions.Add(new UssStyle(root.Name) { Target = hover, PseudoClass = PseudoClass.Hover, opacity = 1 });
                if (active != null)
                    transitions.Add(new UssStyle(root.Name) { Target = hover, PseudoClass = PseudoClass.Active, opacity = 0 });
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
        static void LogWarningIgnoredFigmaProperty(IBaseNodeMixin node, string message) => Debug.LogWarning(Extensions.BuildTargetMessage("Ignored figma property", $"{node.parent?.name}/{node.name}", $"No support for {message}. Not implemented."));
        static void LogWarningImpossibleDesign(IBaseNodeMixin node, string message) => Debug.LogWarning(Extensions.BuildTargetMessage("Wrong figma design", $"{node.parent?.name}/{node.name}", $"{message}. We cant create this in Unity."));
        #endregion
    }
}