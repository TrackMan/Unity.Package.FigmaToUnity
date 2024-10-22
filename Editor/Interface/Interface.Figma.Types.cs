using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable UnusedMember.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
#pragma warning disable S101, S4004

namespace Figma.Internals
{    
    using boolean = Boolean;
    using number = Double;
    
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
        ReactionMixin, ExportMixin { }

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
}