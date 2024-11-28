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
    using number = Double;

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
        number? layoutGrow { get; set; }
        Rect absoluteBoundingBox { get; set; }
        number?[][] relativeTransform { get; set; }
        Vector? size { get; set; }
        number? minWidth { get; set; }
        number? minHeight { get; set; }
        number? maxWidth { get; set; }
        number? maxHeight { get; set; }
    }

    public interface IBlendMixin
    {
        number? opacity { get; set; }
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
        number? strokeWeight { get; set; }
        StrokeAlign? strokeAlign { get; set; }
        StrokeCap? strokeCap { get; set; }
        StrokeJoin? strokeJoin { get; set; }
        object[] strokeGeometry { get; set; }
        IndividualStrokeWeights individualStrokeWeights { get; set; }
    }

    public interface ICornerMixin
    {
        number? cornerRadius { get; set; }
    }

    public interface IRectangleCornerMixin
    {
        number[] rectangleCornerRadii { get; set; }
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
        number? transitionDuration { get; set; }
        EasingType? transitionEasing { get; set; }
    }

    public interface IDefaultShapeMixin : IBaseNodeMixin, ISceneNodeMixin, IConstraintMixin, ILayoutMixin, IBlendMixin, IGeometryMixin,
                                          IReactionMixin, IExportMixin { }

    public interface IDefaultFrameMixin : IDefaultShapeMixin, IContainerMixin, ICornerMixin, IRectangleCornerMixin, IChildrenMixin
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
        bool? clipsContent { get; set; }
        OverflowDirection? overflowDirection { get; set; }
        LayoutWrap? layoutWrap { get; set; }
    }
}