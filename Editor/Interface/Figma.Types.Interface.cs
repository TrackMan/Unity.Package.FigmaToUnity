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
    public interface IBaseNodeMixin
    {
        NodeType type { get; set; }
        string id { get; set; }
        [JsonIgnore]
        BaseNode parent { get; set; }
        string name { get; set; }
    }

    public interface ISceneNodeMixin
    {
        bool? visible { get; set; }
        bool? locked { get; set; }
    }

    public interface IChildrenMixin
    {
        SceneNode[] children { get; set; }
    }

    public interface IConstraintMixin
    {
        Constraints constraints { get; set; }
    }

    public interface ILayoutMixin
    {
        LayoutAlign layoutAlign { get; set; }
        double? layoutGrow { get; set; }
        Rect absoluteBoundingBox { get; set; }
        double?[][] relativeTransform { get; set; }
        Vector? size { get; set; }
        double? minWidth { get; set; }
        double? minHeight { get; set; }
        double? maxWidth { get; set; }
        double? maxHeight { get; set; }
    }

    public interface IBlendMixin
    {
        double? opacity { get; set; }
        BlendMode blendMode { get; set; }
        bool? isMask { get; set; }
        bool? isMaskOutline { get; set; }
        Effect[] effects { get; set; }
        Dictionary<string, string> styles { get; set; }
        bool? preserveRatio { get; set; }
    }

    public interface IContainerMixin
    {
        Paint[] background { get; set; }
        RGBA? backgroundColor { get; set; }
    }

    public interface IGeometryMixin
    {
        Paint[] fills { get; set; }
        object[] fillGeometry { get; set; }
        Paint[] strokes { get; set; }
        double? strokeWeight { get; set; }
        StrokeAlign? strokeAlign { get; set; }
        StrokeCap? strokeCap { get; set; }
        StrokeJoin? strokeJoin { get; set; }
        object[] strokeGeometry { get; set; }
        IndividualStrokeWeights individualStrokeWeights { get; set; }
    }

    public interface ICornerMixin
    {
        double? cornerRadius { get; set; }
    }

    public interface IRectangleCornerMixin
    {
        double[] rectangleCornerRadii { get; set; }
    }

    public interface IExportMixin
    {
        ExportSettings[] exportSettings { get; set; }
    }

    public interface IReactionMixin
    {
        Reaction[] reactions { get; set; }
    }

    public interface ITransitionMixin
    {
        string transitionNodeID { get; set; }
        double? transitionDuration { get; set; }
        EasingType? transitionEasing { get; set; }
    }

    public interface IDefaultShapeMixin : IBaseNodeMixin, ISceneNodeMixin, IConstraintMixin, ILayoutMixin, IBlendMixin, IGeometryMixin,
                                          IReactionMixin, IExportMixin
    {
        LayoutSizing layoutSizingHorizontal { get; set; }
        LayoutSizing layoutSizingVertical { get; set; }
    }

    public interface IDefaultFrameMixin : IDefaultShapeMixin, IContainerMixin, ICornerMixin, IRectangleCornerMixin, IChildrenMixin
    {
        LayoutMode? layoutMode { get; set; }
        LayoutPositioning? layoutPositioning { get; set; }
        PrimaryAxisSizingMode? primaryAxisSizingMode { get; set; }
        PrimaryAxisAlignItems? primaryAxisAlignItems { get; set; }
        CounterAxisSizingMode? counterAxisSizingMode { get; set; }
        CounterAxisAlignItems? counterAxisAlignItems { get; set; }
        CounterAxisAlignContent counterAxisAlignContent { get; set; }
        double? paddingLeft { get; set; }
        double? paddingTop { get; set; }
        double? paddingRight { get; set; }
        double? paddingBottom { get; set; }
        double? itemSpacing { get; set; }
        LayoutGrid[] layoutGrids { get; set; }
        bool? clipsContent { get; set; }
        OverflowDirection? overflowDirection { get; set; }
        LayoutWrap? layoutWrap { get; set; }
    }
}