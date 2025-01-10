using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable UnusedMember.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
#pragma warning disable S101, S4004

namespace Figma.Internals
{
    using VariableAlias = Object;
    using number = Double;

    #region Datatype
    public class ShadowEffect : Effect
    {
        public EffectType type;
        public RGBA color;
        public Vector offset;
        public number radius;
        public number? spread;
        public bool visible;
        public BlendMode blendMode;
        public bool? showShadowBehindNode;
    }

    public class BlurEffect : Effect
    {
        public EffectType type;
        public number radius;
        public bool visible;
    }

    public class Effect { }

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
        public bool? visible;
        public number? opacity;
        public BlendMode? blendMode;
        public Dictionary<string, VariableAlias> boundVariables;
    }

    public class GradientPaint : Paint
    {
        public PaintType type;
        public ColorStop[] gradientStops;
        public bool? visible;
        public number? opacity;
        public BlendMode? blendMode;
        public Vector[] gradientHandlePositions;
    }

    public class ImagePaint : Paint
    {
        public PaintType type;
        public ScaleMode scaleMode;
        public number[,] imageTransform;
        public bool? visible;
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
        public bool? visible;
        public RGBA? color;
    }

    public class GridLayoutGrid : LayoutGrid
    {
        public Pattern pattern;
        public number sectionSize;
        public bool? visible;
        public RGBA? color;
        public Alignment? alignment;
        public number? gutterSize;
        public number count;
        public number? offset;
    }

    public class LayoutGrid { }

    public class ExportSettingsConstraints
    {
        public ExportSettingsConstraintsType type;
        public number value;
    }

    public class ExportSettingsImage : ExportSettings
    {
        public Format format;
        public bool? contentsOnly;
        public string suffix;
        public ExportSettingsConstraints constraint;
    }

    public class ExportSettingsSVG : ExportSettings
    {
        public Format format = Format.SVG;
        public bool? contentsOnly;
        public string suffix;
        public bool? svgOutlineText;
        public bool? svgIdAttribute;
        public bool? svgSimplifyStroke;
        public ExportSettingsConstraints constraint;
    }

    public class ExportSettingsPDF : ExportSettings
    {
        public Format format = Format.PDF;
        public bool? contentsOnly;
        public string suffix;
        public ExportSettingsConstraints constraint;
    }

    public class ExportSettings { }

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
        public bool? preserveScrollPosition;
        public Vector? overlayRelativePosition;
        public bool resetVideoPosition;
        public bool resetScrollPosition;
        public bool resetInteractiveComponents;
    }

    public class SimpleTransition : Transition { }

    public class DirectionalTransition : Transition
    {
        public TransitionDirection direction;
        public bool matchLayers;
    }

    public class Transition
    {
        public TransitionType type;
        public Easing easing;
        public number? duration;
    }

    public class Trigger
    {
        public TriggerType type;
        public number? delay;
        public object device;
        public object keyCodes;
        // Possibly Obsolete
        public number? timeout;
    }

    public class Easing
    {
        public EasingType type;
        public EasingFunctionSpring easingFunctionSpring;
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
    #endregion

    #region Nodes
    public class DocumentNode : BaseNode
    {
        public CanvasNode[] children;
    }

    public class CanvasNode : BaseNode, IChildrenMixin, IExportMixin
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

    public class FrameNode : DefaultFrameNode { }

    public class GroupNode : DefaultFrameNode { }

    public class SliceNode : SceneNode, IConstraintMixin, ILayoutMixin, IExportMixin
    {
        #region Mixin
        public Constraints constraints { get; set; }

        public LayoutAlign layoutAlign { get; set; }
        public number? layoutGrow { get; set; }
        public Rect absoluteBoundingBox { get; set; }
        public Rect absoluteRenderBounds { get; set; }
        public number?[][] relativeTransform { get; set; }
        public Vector? size { get; set; }
        public number? minWidth { get; set; }
        public number? minHeight { get; set; }
        public number? maxWidth { get; set; }
        public number? maxHeight { get; set; }
        public ExportSettings[] exportSettings { get; set; }
        #endregion
    }

    public class RectangleNode : DefaultShapeNode, ICornerMixin, IRectangleCornerMixin
    {
        #region Mixin
        public number? cornerRadius { get; set; }

        public number[] rectangleCornerRadii { get; set; }
        #endregion
    }

    public class LineNode : DefaultShapeNode { }

    public class EllipseNode : DefaultShapeNode
    {
        public ArcData arcData { get; set; }
    }

    public class RegularPolygonNode : DefaultShapeNode, ICornerMixin, IRectangleCornerMixin
    {
        #region Mixin
        public number? cornerRadius { get; set; }
        public number[] rectangleCornerRadii { get; set; }
        #endregion
    }

    public class StarNode : DefaultShapeNode, ICornerMixin, IRectangleCornerMixin
    {
        #region Mixin
        public number? cornerRadius { get; set; }

        public number[] rectangleCornerRadii { get; set; }
        #endregion
    }

    public class VectorNode : DefaultShapeNode, ICornerMixin, IRectangleCornerMixin
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
            public bool? italic;
            public number? fontWeight;
            public number? fontSize;
            public TextCase? textCase;
            public TextDecoration? textDecoration;
            public TextAlignHorizontal? textAlignHorizontal;
            public TextAlignVertical? textAlignVertical;
            public TextAutoResize? textAutoResize;
            public number? letterSpacing;
            public Paint[] fills;
            public Dictionary<string, number> opentypeFlags;
            public number? lineHeightPx;
            public number? listSpacing;
            public number? lineHeightPercent;
            public number? lineHeightPercentFontSize;
            public string lineHeightUnit;
            public Dictionary<string, string> hyperlink;
            public string inheritFillStyleId;
            public string inheritTextStyleId;
            public string leadingTrim;
            public string textTruncation;
            public int maxLines;
        }

        public string characters;
        public Style style;
        public number[] characterStyleOverrides;
        public Dictionary<number, Style> styleOverrideTable;
        public number? layoutVersion;
        public string[] lineTypes;
        public int[] lineIndentations;
    }

    public class ComponentSetNode : DefaultFrameNode
    {
        public Dictionary<string, ComponentPropertyDefinition> componentPropertyDefinitions;
    }

    public class ComponentNode : DefaultFrameNode
    {
        public Dictionary<string, ComponentPropertyDefinition> componentPropertyDefinitions;
    }

    public class ComponentPropertyDefinition
    {
        public string type;
        public string defaultValue;
        public List<string> variantOptions;
        public object boundVariables;
        public PreferredValue[] preferredValues;
    }

    public class ComponentProperties : ComponentPropertyDefinition
    {
        public string value;
    }

    public class PreferredValue
    {
        public NodeType type;
        public string key;
    }

    public class InstanceNode : DefaultFrameNode
    {
        public string componentId;
        public Dictionary<string, ComponentProperties> componentProperties;
        public bool isExposedInstance;
        public string[] exposedInstances;
        public Overrides[] overrides;
    }

    public class Overrides
    {
        public string id;
        public string[] overriddenFields;
    }

    public class BooleanOperationNode : DefaultFrameNode
    {
        public BooleanOperation booleanOperation;
    }

    public class DefaultShapeNode : SceneNode, IDefaultShapeMixin, ITransitionMixin
    {
        public number[] strokeDashes;
        public number? rotation;
        public Rect absoluteRenderBounds;

        #region Mixin
        public LayoutSizing layoutSizingHorizontal { get; set; }
        public LayoutSizing layoutSizingVertical { get; set; }
        public Constraints constraints { get; set; }

        public LayoutAlign layoutAlign { get; set; }
        public number? layoutGrow { get; set; }
        public Rect absoluteBoundingBox { get; set; }
        public number?[][] relativeTransform { get; set; }
        public Vector? size { get; set; }

        public number? opacity { get; set; }
        public BlendMode blendMode { get; set; }
        public bool? isMask { get; set; }
        public bool? isMaskOutline { get; set; }
        public Effect[] effects { get; set; }
        public Dictionary<string, string> styles { get; set; }
        public bool? preserveRatio { get; set; }

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
        public Interactions[] interactions { get; set; }

        public ComponentPropertyReferences componentPropertyReferences { get; set; }

        public LayoutPositioning? layoutPositioning { get; set; }
        public MaskType? maskType { get; set; }
        public number? minWidth { get; set; }
        public number? minHeight { get; set; }
        public number? maxWidth { get; set; }
        public number? maxHeight { get; set; }
        #endregion
    }

    public class DefaultFrameNode : DefaultShapeNode, IDefaultFrameMixin
    {
        #region Mixin
        public LayoutMode? layoutMode { get; set; }
        public PrimaryAxisSizingMode? primaryAxisSizingMode { get; set; }
        public CounterAxisSizingMode? counterAxisSizingMode { get; set; }
        public object primaryAxisSpacing { get; set; }
        public object counterAxisSpacing { get; set; }
        public PrimaryAxisAlignItems? primaryAxisAlignItems { get; set; }
        public CounterAxisAlignItems? counterAxisAlignItems { get; set; }
        public object primaryAxisAlignContent { get; set; }
        public CounterAxisAlignContent counterAxisAlignContent { get; set; }
        public number? paddingLeft { get; set; }
        public number? paddingTop { get; set; }
        public number? paddingRight { get; set; }
        public number? paddingBottom { get; set; }
        public number? itemSpacing { get; set; }
        public LayoutGrid[] layoutGrids { get; set; }
        public bool? clipsContent { get; set; }
        public OverflowDirection? overflowDirection { get; set; }
        public bool itemReverseZIndex { get; set; }
        public LayoutWrap? layoutWrap { get; set; }
        public bool strokesIncludedInLayout { get; set; }

        public Paint[] background { get; set; }
        public RGBA? backgroundColor { get; set; }

        public number? cornerRadius { get; set; }

        public number[] rectangleCornerRadii { get; set; }

        public SceneNode[] children { get; set; }
        #endregion
    }

    public class BaseNode : IBaseNodeMixin
    {
        #region Mixin
        public NodeType type { get; set; }
        public string id { get; set; }
        public BaseNode parent { get; set; }
        public string name { get; set; }
        public string scrollBehavior { get; set; }
        #endregion

        public override string ToString() => name;
    }

    public class SceneNode : BaseNode, ISceneNodeMixin
    {
        #region Properties
        public bool? visible { get; set; }
        public bool? locked { get; set; }
        public Dictionary<string, VariableAlias> boundVariables { get; set; }
        #endregion
    }
    #endregion

    #region Api
    public class Component
    {
        public string key;
        public string name;
        public string description;
        public DocumentationLink[] documentationLinks;
        public string componentSetId;
        public bool remote;
    }

    public class Style
    {
        public StyleType styleType;
        public string key;
        public string name;
        public string description;
        public string remote;
    }

    public class Failure
    {
        public int status;
        public string err;
    }

    public class Me : Failure
    {
        public string id;
        public string email;
        public string handle;
        public string img_url;
    }

    public class Files : Failure
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
        public string linkAccess;
    }

    public class Images : Failure
    {
        public Dictionary<string, string> images;
    }

    public class Nodes : Failure
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
        public string linkAccess;
    }

    public class Interactions
    {
        public Trigger trigger;
        public List<Action> actions;
    }

    public class EasingFunctionSpring
    {
        public number? mass;
        public number? stiffness;
        public number? damping;
    }
    #endregion
}