using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Figma
{
    using boolean = Boolean;
    using number = Double;

    namespace global
    {
        ////////////////////////////////////////////////////////////////////////////////
        // Datatype

        // type Transform = [
        //  [number, number, number],
        //  [number, number, number]
        // ]

        public struct Vector
        {
            public number x;
            public number y;
        }

        public struct Rect
        {
            public number x;
            public number y;
            public number width;
            public number height;

            public number left => x;
            public number right => x + width;
            public number top => y;
            public number bottom => y + height;
            public number centerLeft => x - width / 2;
            public number centerRight => x + width / 2;
            public number centerTop => y - height / 2;
            public number centerBottom => y + height / 2;
            public number halfWidth => width / 2;
            public number halfHeight => height / 2;

            public Rect(number x, number y, number width, number height)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
            }

            public static Rect operator +(Rect a, Rect b) => new(a.x + b.x, a.y + b.y, a.width + b.width, a.height + b.height);
            public static Rect operator -(Rect a, Rect b) => new(a.x - b.x, a.y - b.y, a.width - b.width, a.height - b.height);
        }

        public struct RGBA
        {
            public number r;
            public number g;
            public number b;
            public number a;
        }

        public class ShadowEffect : Effect
        {
            public EffectType type;
            public RGBA color;
            public Vector offset;
            public number radius;
            public number? spread;
            public boolean visible;
            public BlendMode blendMode;
            public boolean? showShadowBehindNode;
        }

        public class BlurEffect : Effect
        {
            public EffectType type;
            public number radius;
            public boolean visible;
        }

        public class Effect
        {
        }

        public class IndividualStrokeWeights
        {
            public number top;
            public number right;
            public number bottom;
            public number left;
        }

        public class Constraints
        {
            public ConstraintHorizontal horizontal;
            public ConstraintVertical vertical;
        }

        public class ColorStop
        {
            public number position;
            public RGBA color;
        }

        public class ImageFilter
        {
            public number? exposure;
            public number? contrast;
            public number? saturation;
            public number? temperature;
            public number? tint;
            public number? highlights;
            public number? shadows;
        }

        public class SolidPaint : Paint
        {
            public PaintType type;
            public RGBA color;
            public boolean? visible;
            public number? opacity;
            public BlendMode? blendMode;
        }

        public class GradientPaint : Paint
        {
            public PaintType type;
            public ColorStop[] gradientStops;
            public boolean? visible;
            public number? opacity;
            public BlendMode? blendMode;
            public Vector[] gradientHandlePositions;
        }

        public class ImagePaint : Paint
        {
            public PaintType type;
            public ScaleMode scaleMode;
            public number[,] imageTransform;
            public boolean? visible;
            public number? opacity;
            public BlendMode? blendMode;
            public string imageRef;
            public ImageFilter filters;
            public number? rotation;
        }

        public class Paint
        {
            public string inheritFillStyleId;
            public Paint[] fills;
        }

        public class RowsColsLayoutGrid : LayoutGrid
        {
            public Pattern pattern;
            public Alignment alignment;
            public number gutterSize;
            public number count;
            public number? sectionSize;
            public number? offset;
            public boolean? visible;
            public RGBA? color;
        }

        public class GridLayoutGrid : LayoutGrid
        {
            public Pattern pattern;
            public number sectionSize;
            public boolean? visible;
            public RGBA? color;
            public Alignment? alignment;
            public number? gutterSize;
            public number count;
            public number? offset;
        }

        public class LayoutGrid
        {
        }

        public class ExportSettingsConstraints
        {
            public ExportSettingsConstraintsType type;
            public number value;
        }

        public class ExportSettingsImage : ExportSettings
        {
            public Format format;
            public boolean? contentsOnly;
            public string suffix;
            public ExportSettingsConstraints constraint;
        }

        public class ExportSettingsSVG : ExportSettings
        {
            public Format format = Format.SVG;
            public boolean? contentsOnly;
            public string suffix;
            public boolean? svgOutlineText;
            public boolean? svgIdAttribute;
            public boolean? svgSimplifyStroke;
            public ExportSettingsConstraints constraint;
        }

        public class ExportSettingsPDF : ExportSettings
        {
            public Format format = Format.PDF;
            public boolean? contentsOnly;
            public string suffix;
            public ExportSettingsConstraints constraint;
        }

        public class ExportSettings
        {
        }

        public class Reaction
        {
            public Action action;
            public Trigger trigger;
        }

        public class Action
        {
            public ActionType type;
            public string url;
            public string destinationId;
            public Navigation? navigation;
            public Transition transition;
            public boolean? preserveScrollPosition;
            public Vector? overlayRelativePosition;
        }

        public class SimpleTransition : Transition
        {
            public TransitionType type;
            public Easing easing;
            public number duration;
        }

        public class DirectionalTransition : Transition
        {
            public TransitionType type;
            public TransitionDirection direction;
            public boolean matchLayers;
            public Easing easing;
            public number duration;
        }

        public class Transition
        {
        }

        public class Trigger
        {
            public TriggerType type;
            public number? delay;
        }

        public class Easing
        {
            public EasingType type;
        }

        public class DocumentationLink
        {
            public string uri;
        }

        public class ArcData
        {
            public number startingAngle;
            public number endingAngle;
            public number innerRadius;
        }

        public class FlowStartingPoint
        {
            public string nodeId;
            public string name;
        }

        public class ComponentPropertyReferences
        {
            public string visible;
            public string characters;
            public string mainComponent;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Mixins

        public interface BaseNodeMixin
        {
            NodeType type { get; set; }
            string id { get; set; }
            [JsonIgnore]
            BaseNode parent { get; set; }
            string name { get; set; }
        }

        public interface SceneNodeMixin
        {
            boolean? visible { get; set; }
            boolean? locked { get; set; }
        }

        public interface ChildrenMixin
        {
            SceneNode[] children { get; set; }
        }

        public interface ConstraintMixin
        {
            Constraints constraints { get; set; }
        }

        public interface LayoutMixin
        {
            LayoutAlign layoutAlign { get; set; }
            number? layoutGrow { get; set; }
            Rect absoluteBoundingBox { get; set; }
            number?[][] relativeTransform { get; set; }
            Vector? size { get; set; }
        }

        public interface BlendMixin
        {
            number? opacity { get; set; }
            BlendMode blendMode { get; set; }
            boolean? isMask { get; set; }
            boolean? isMaskOutline { get; set; }
            Effect[] effects { get; set; }
            Dictionary<string, string> styles { get; set; }
            boolean? preserveRatio { get; set; }
        }

        public interface ContainerMixin
        {
            Paint[] background { get; set; }
            RGBA? backgroundColor { get; set; }
        }

        public interface GeometryMixin
        {
            Paint[] fills { get; set; }
            object[] fillGeometry { get; set; }
            Paint[] strokes { get; set; }
            number? strokeWeight { get; set; }
            StrokeAlign? strokeAlign { get; set; }
            StrokeCap? strokeCap { get; set; }
            StrokeJoin? strokeJoin { get; set; }
            object[] strokeGeometry { get; set; }
            IndividualStrokeWeights individualStrokeWeights { get; set; }
    }

        public interface CornerMixin
        {
            number? cornerRadius { get; set; }
        }

        public interface RectangleCornerMixin
        {
            number[] rectangleCornerRadii { get; set; }
        }

        public interface ExportMixin
        {
            ExportSettings[] exportSettings { get; set; }
        }

        public interface ReactionMixin
        {
            Reaction[] reactions { get; set; }
        }

        public interface TransitionMixin
        {
            string transitionNodeID { get; set; }
            number? transitionDuration { get; set; }
            EasingType? transitionEasing { get; set; }
        }

        public interface DefaultShapeMixin :
          BaseNodeMixin, SceneNodeMixin, ConstraintMixin, LayoutMixin, BlendMixin, GeometryMixin,
          ReactionMixin, ExportMixin
        {
        }

        public interface DefaultFrameMixin : DefaultShapeMixin, ContainerMixin, CornerMixin, RectangleCornerMixin, ChildrenMixin
        {
            LayoutMode? layoutMode { get; set; }
            PrimaryAxisSizingMode? primaryAxisSizingMode { get; set; }
            PrimaryAxisAlignItems? primaryAxisAlignItems { get; set; }
            CounterAxisSizingMode? counterAxisSizingMode { get; set; }
            CounterAxisAlignItems? counterAxisAlignItems { get; set; }
            number? paddingLeft { get; set; }
            number? paddingTop { get; set; }
            number? paddingRight { get; set; }
            number? paddingBottom { get; set; }
            number? itemSpacing { get; set; }
            LayoutGrid[] layoutGrids { get; set; }
            boolean? clipsContent { get; set; }
            OverflowDirection? overflowDirection { get; set; }
            LayoutWrap? layoutWrap { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Nodes

        public class DocumentNode : BaseNode
        {
            public CanvasNode[] children;
        }

        public class CanvasNode : BaseNode, ChildrenMixin, ExportMixin
        {
            public RGBA backgroundColor;
            public string prototypeStartNodeID;
            public object prototypeDevice;
            public FlowStartingPoint[] flowStartingPoints;

            #region Mixin
            public SceneNode[] children { get; set; }

            public ExportSettings[] exportSettings { get; set; }
            #endregion
        }

        public class FrameNode : DefaultFrameNode
        {
        }

        public class GroupNode : DefaultFrameNode
        {
        }

        public class SliceNode : SceneNode, ConstraintMixin, LayoutMixin, ExportMixin
        {
            #region Mixin
            public Constraints constraints { get; set; }

            public LayoutAlign layoutAlign { get; set; }
            public number? layoutGrow { get; set; }
            public Rect absoluteBoundingBox { get; set; }
            public number?[][] relativeTransform { get; set; }
            public Vector? size { get; set; }

            public ExportSettings[] exportSettings { get; set; }
            #endregion
        }

        public class RectangleNode : DefaultShapeNode, CornerMixin, RectangleCornerMixin
        {
            #region Mixin
            public number? cornerRadius { get; set; }

            public number[] rectangleCornerRadii { get; set; }
            #endregion
        }

        public class LineNode : DefaultShapeNode
        {
        }

        public class EllipseNode : DefaultShapeNode
        {
            public ArcData arcData { get; set; }
        }

        public class RegularPolygonNode : DefaultShapeNode, CornerMixin, RectangleCornerMixin
        {
            #region Mixin
            public number? cornerRadius { get; set; }

            public number[] rectangleCornerRadii { get; set; }
            #endregion
        }

        public class StarNode : DefaultShapeNode, CornerMixin, RectangleCornerMixin
        {
            #region Mixin
            public number? cornerRadius { get; set; }

            public number[] rectangleCornerRadii { get; set; }
            #endregion
        }

        public class VectorNode : DefaultShapeNode, CornerMixin, RectangleCornerMixin
        {
            #region Mixin
            public number? cornerRadius { get; set; }

            public number[] rectangleCornerRadii { get; set; }

            public Dictionary<int, Paint> fillOverrideTable { get; set; }
            #endregion
        }

        public class TextNode : DefaultShapeNode
        {
            public class Style
            {
                public string fontFamily;
                public string fontPostScriptName;
                public number? paragraphSpacing;
                public boolean? italic;
                public number? fontWeight;
                public number? fontSize;
                public TextCase? textCase;
                public TextDecoration? textDecoration;
                public TextAlignHorizontal? textAlignHorizontal;
                public TextAlignVertical? textAlignVertical;
                public TextAutoResize? textAutoResize;
                public number? letterSpacing;
                public Paint[] fills;
                public Dictionary<string, double> opentypeFlags;
                public number? lineHeightPx;
                public number? lineHeightPercent;
                public number? lineHeightPercentFontSize;
                public string lineHeightUnit;
                public Dictionary<string, string> hyperlink;
                public string inheritFillStyleId;
                public string inheritTextStyleId;
            }

            public string characters;
            public Style style;
            public double[] characterStyleOverrides;
            public Dictionary<double, Style> styleOverrideTable;
            public number? layoutVersion;
            public string[] lineTypes;
            public int[] lineIndentations;
        }

        public class ComponentSetNode : DefaultFrameNode
        {
        }

        public class ComponentNode : DefaultFrameNode
        {
            public object componentPropertyDefinitions;
        }

        public class InstanceNode : DefaultFrameNode
        {
            public string componentId;
            public bool isExposedInstance;
            public string[] exposedInstances;
            public object[] overrides;
        }

        public class BooleanOperationNode : DefaultFrameNode
        {
            public BooleanOperation booleanOperation;
        }

        public class DefaultShapeNode : SceneNode, DefaultShapeMixin, TransitionMixin
        {
            public number[] strokeDashes;
            public number? rotation;
            public Rect absoluteRenderBounds;
            public string layoutSizingHorizontal;
            public string layoutSizingVertical;

            #region Mixin
            public Constraints constraints { get; set; }

            public LayoutAlign layoutAlign { get; set; }
            public number? layoutGrow { get; set; }
            public Rect absoluteBoundingBox { get; set; }
            public number?[][] relativeTransform { get; set; }
            public Vector? size { get; set; }

            public number? opacity { get; set; }
            public BlendMode blendMode { get; set; }
            public boolean? isMask { get; set; }
            public boolean? isMaskOutline { get; set; }
            public Effect[] effects { get; set; }
            public Dictionary<string, string> styles { get; set; }
            public boolean? preserveRatio { get; set; }

            public Paint[] fills { get; set; }
            public object[] fillGeometry { get; set; }
            public Paint[] strokes { get; set; }
            public number? strokeWeight { get; set; }
            public IndividualStrokeWeights individualStrokeWeights { get; set; }
            public StrokeAlign? strokeAlign { get; set; }
            public StrokeCap? strokeCap { get; set; }
            public StrokeJoin? strokeJoin { get; set; }
            public object[] strokeGeometry { get; set; }
            public number? strokeMiterAngle { get; set; }
            public number? cornerSmoothing { get; set; }

            public Reaction[] reactions { get; set; }

            public ExportSettings[] exportSettings { get; set; }

            public string transitionNodeID { get; set; }
            public number? transitionDuration { get; set; }
            public EasingType? transitionEasing { get; set; }

            public ComponentPropertyReferences componentPropertyReferences { get; set; }

            public LayoutPositioning? layoutPositioning { get; set; }
            #endregion
        }

        public class DefaultFrameNode : DefaultShapeNode, DefaultFrameMixin
        {
            public LayoutMode? layoutMode { get; set; }
            public PrimaryAxisSizingMode? primaryAxisSizingMode { get; set; }
            public CounterAxisSizingMode? counterAxisSizingMode { get; set; }
            public PrimaryAxisAlignItems? primaryAxisAlignItems { get; set; }
            public CounterAxisAlignItems? counterAxisAlignItems { get; set; }
            public number? paddingLeft { get; set; }
            public number? paddingTop { get; set; }
            public number? paddingRight { get; set; }
            public number? paddingBottom { get; set; }
            public number? itemSpacing { get; set; }
            public LayoutGrid[] layoutGrids { get; set; }
            public boolean? clipsContent { get; set; }
            public OverflowDirection? overflowDirection { get; set; }
            public boolean itemReverseZIndex { get; set; }
            public LayoutWrap? layoutWrap { get; set; }
            public boolean strokesIncludedInLayout { get; set; }

            #region Mixin
            public Paint[] background { get; set; }
            public RGBA? backgroundColor { get; set; }

            public number? cornerRadius { get; set; }

            public number[] rectangleCornerRadii { get; set; }

            public SceneNode[] children { get; set; }
            #endregion
        }

        public class BaseNode : BaseNodeMixin
        {
            #region Mixin
            public NodeType type { get; set; }
            public string id { get; set; }
            public BaseNode parent { get; set; }
            public string name { get; set; }
            public string scrollBehavior { get; set; }
            #endregion

            public override string ToString() => $"{name}";
        }

        public class SceneNode : BaseNode, SceneNodeMixin
        {
            #region Properties
            public boolean? visible { get; set; }
            public boolean? locked { get; set; }
            #endregion
        }

        ////////////////////////////////////////////////////////////////////////////////
        // API

        public class Component
        {
            public string key;
            public string name;
            public string description;
            public DocumentationLink[] documentationLinks;
            public string componentSetId;
            public string remote;
        }

        public class Style
        {
            public enum StyleType { FILL, TEXT, EFFECT, GRID, NONE }

            public StyleType styleType;
            public string key;
            public string name;
            public string description;
            public string remote;
        }

        public class Files
        {
            public class Images
            {
                public class Meta
                {
                    public Dictionary<string, string> images;
                }

                public bool error;
                public number status;
                public Meta meta;
                public string i18n;
            }

            public DocumentNode document;
            public Dictionary<string, Component> components;
            public Dictionary<string, Component> componentSets;
            public number schemaVersion;
            public Dictionary<string, Style> styles;
            public string name;
            public DateTime lastModified;
            public string thumbnailUrl;
            public string version;
            public string role;
            public string editorType;
            public object linkAccess;
        }

        public class Images
        {
            public string err;
            public Dictionary<string, string> images;
        }

        public class Nodes
        {
            public class Document
            {
                public ComponentNode document;
                public Dictionary<string, Component> components;
                public Dictionary<string, Component> componentSets;
                public number schemaVersion;
                public Dictionary<string, Style> styles;
            }

            public string name;
            public DateTime lastModified;
            public string thumbnailUrl;
            public string version;
            public string role;
            public Dictionary<string, Document> nodes;
            public string editorType;
            public object linkAccess;
        }
    }
}