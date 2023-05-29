using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Trackman;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable MemberCanBePrivate.Global

namespace System.Runtime.CompilerServices { class IsExternalInit { } }

namespace Figma
{
    using Attributes;
    using global;
    using number = Double;

    public static class NodeMetadata
    {
        public record RootMetadata(UIDocument document, Figma figma, UxmlAttribute uxml, UxmlDownloadImages downloadImages);
        // ReSharper disable once NotAccessedPositionalProperty.Global
        public record QueryMetadata(FieldInfo fieldInfo, QueryAttribute queryRoot, QueryAttribute query);
        public record BaseNodeMetadata(RootMetadata root, QueryMetadata query);

        #region Fields
        static Dictionary<BaseNode, RootMetadata> rootMetadata = new();
        static Dictionary<BaseNode, QueryMetadata> queryMetadata = new();
        static List<BaseNode> search = new(256);
        #endregion

        #region Properties
        public static BindingFlags FieldsFlags => BindingFlags.NonPublic | BindingFlags.Instance;
        public static BindingFlags MethodsFlags => BindingFlags.NonPublic | BindingFlags.Instance;
        #endregion

        #region Methods
        public static void Initialize(UIDocument document, Figma figma, IEnumerable<MonoBehaviour> targets, DocumentNode root, bool throwException = true)
        {
            foreach (IRootElement target in targets.OfType<IRootElement>()) Initialize(document, figma, root, target, throwException);
        }
        public static void Initialize(UIDocument document, Figma figma, DocumentNode root, IRootElement target, bool throwException = true)
        {
            Type targetType = target.GetType();
            UxmlAttribute uxml = targetType.GetCustomAttribute<UxmlAttribute>();
            BaseNode targetRoot = root.Find(uxml.Root);
            BaseNode[] targetPreserve = uxml.Preserve.Select(x => root.Find(x)).ToArray();

            rootMetadata.Add(targetRoot, new RootMetadata(document, figma, uxml, uxml.ImageFiltering));
            foreach (BaseNode value in targetPreserve)
                if (!rootMetadata.ContainsKey(value))
                    rootMetadata.Add(value, new RootMetadata(document, figma, uxml, UxmlDownloadImages.Everything));

            Initialize(targetType, targetRoot, throwException);
        }
        public static void Clear(UIDocument document)
        {
            foreach (BaseNode node in queryMetadata.Keys.Where(x => GetMetadata(x).root?.document == document).ToArray()) queryMetadata.Remove(node);
            foreach (BaseNode node in rootMetadata.Where(x => x.Value.document == document).Select(x => x.Key).ToArray()) rootMetadata.Remove(node);
        }

        public static IEnumerable<T> Search<T>(this BaseNode value, string path) where T : BaseNode
        {
            static bool StartsWith(string path, BaseNode value, int startIndex)
            {
                int endIndex = startIndex + value.name.Length;
                return path.BeginsWith(value.name, startIndex) && path.Length >= endIndex && (path.Length == endIndex || path[endIndex].IsSeparator());
            }
            static int LastIndexOf(BaseNode root, BaseNode leaf, BaseNode value, string path, int startIndex = 0)
            {
                if (value.parent is not null && value.parent != root) startIndex = LastIndexOf(root, leaf, value.parent, path, startIndex);
                if (startIndex >= 0 && StartsWith(path, value, startIndex))
                {
                    int endIndex = startIndex + value.name.Length;
                    if (path.Length > endIndex && path[endIndex].IsSeparator() && value != leaf) endIndex++;
                    return endIndex;
                }

                return -1;
            }
            static void SearchIn(BaseNode value, string path, int startIndex = 0)
            {
                static bool IsVisible(BaseNodeMixin mixin)
                {
                    if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;
                    return mixin.parent is null || IsVisible(mixin.parent);
                }
                static IEnumerable<BaseNode> GetChildren(BaseNode value)
                {
                    List<BaseNode> children = new();
                    if (value is DocumentNode documentNode) children.AddRange(documentNode.children);
                    else if (value is ChildrenMixin childrenMixin) children.AddRange(childrenMixin.children);
                    else return children;
                    return children;
                }
                IEnumerable<BaseNode> children = GetChildren(value);

                static bool EqualsTo(BaseNode value, string path, int startIndex) => path.EqualsTo(value.name, startIndex);

                foreach (BaseNode child in children.Where(IsVisible))
                    if (child.name.NotNullOrEmpty() && EqualsTo(child, path, startIndex))
                        search.Add(child);

                foreach (BaseNode child in children.Where(IsVisible))
                    if (child.name.NotNullOrEmpty() && StartsWith(path, child, startIndex))
                        SearchIn(child, path, startIndex + child.name.Length + 1);
            }
            static void SearchByFullPath(BaseNode value, string path, int startIndex = 0)
            {
                static bool IsVisible(BaseNodeMixin mixin)
                {
                    if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;
                    return mixin.parent is null || IsVisible(mixin.parent);
                }
                static IEnumerable<BaseNode> GetChildren(BaseNode value)
                {
                    List<BaseNode> children = new();
                    if (value is DocumentNode documentNode) children.AddRange(documentNode.children);
                    else if (value is ChildrenMixin childrenMixin) children.AddRange(childrenMixin.children);
                    else return children;
                    return children;
                }
                IEnumerable<BaseNode> children = GetChildren(value);

                static bool EqualsToFullPath(BaseNode root, BaseNode value, string path, int startIndex) => LastIndexOf(root, value, value, path, startIndex) == path.Length;
                static bool StartsWithFullPath(BaseNode root, BaseNode value, string path, int startIndex)
                {
                    int endIndex = LastIndexOf(root, value, value, path, startIndex);
                    return endIndex >= 0 && path.Length > endIndex && path[endIndex].IsSeparator();
                }

                foreach (BaseNode child in children.Where(IsVisible))
                    if (child.name.NotNullOrEmpty() && EqualsToFullPath(value, child, path, startIndex))
                        search.Add(child);

                foreach (BaseNode child in children.Where(IsVisible))
                    if (child.name.NotNullOrEmpty() && StartsWithFullPath(value, child, path, startIndex))
                        SearchByFullPath(child, path, startIndex + child.name.Length + 1);
            }

            search.Clear();

            BaseNode root = FindRoot(value);
            if (root is not null)
            {
                UxmlAttribute uxml = rootMetadata[root].uxml;
                if (path.BeginsWith(uxml.DocumentRoot) || uxml.DocumentPreserve.Any(x => path.BeginsWith(x))) SearchByFullPath(root.parent.parent, path, UxmlAttribute.prefix.Length + 1);
                else SearchIn(value, path);
            }
            else
            {
                SearchByFullPath(value, path);
            }

            foreach (T result in search.OfType<T>()) yield return result;
        }
        public static T Find<T>(this BaseNode value, string path, bool throwException = true, bool silent = false) where T : BaseNode
        {
            string GetFullPath(BaseNode node) => node.parent is not null ? $"{GetFullPath(node.parent)}/{node.name}" : node.name;

            T result = value.Search<T>(path).FirstOrDefault();
            if (result is not null)
                return result;

            if (throwException)
                throw new Exception($"Cannot find {typeof(T).Name} at [<color=yellow>{GetFullPath(value)}/{path}</color>]");

            if (!silent) Debug.LogWarning($"Cannot find {typeof(T).Name} at [<color=yellow>{GetFullPath(value)}/{path}</color>]");
            return default;
        }
        public static BaseNode Find(this BaseNode value, string path, bool throwException = true, bool silent = true) => Find<BaseNode>(value, path, throwException, silent);

        public static BaseNode Clone(this BaseNode value, BaseNode parent = default)
        {
            BaseNode clone = (BaseNode)Activator.CreateInstance(value.GetType());
            if (parent is not null) clone.parent = parent;
            Copy(clone, value);
            if (clone is SceneNodeMixin mixin && value is SceneNodeMixin nodeMixin) Copy(mixin, nodeMixin);
            if (clone is ChildrenMixin childrenMixin && value is ChildrenMixin mixin1) Copy(childrenMixin, mixin1);
            if (clone is ConstraintMixin constraintMixin && value is ConstraintMixin value1) Copy(constraintMixin, value1);
            if (clone is LayoutMixin layoutMixin && value is LayoutMixin layoutMixin1) Copy(layoutMixin, layoutMixin1);
            if (clone is BlendMixin blendMixin && value is BlendMixin blendMixin1) Copy(blendMixin, blendMixin1);
            if (clone is ContainerMixin containerMixin && value is ContainerMixin containerMixin1) Copy(containerMixin, containerMixin1);
            if (clone is GeometryMixin geometryMixin && value is GeometryMixin geometryMixin1) Copy(geometryMixin, geometryMixin1);
            if (clone is CornerMixin cornerMixin && value is CornerMixin cornerMixin1) Copy(cornerMixin, cornerMixin1);
            if (clone is RectangleCornerMixin rectangleCornerMixin && value is RectangleCornerMixin rectangleCornerMixin1) Copy(rectangleCornerMixin, rectangleCornerMixin1);
            if (clone is ExportMixin exportMixin && value is ExportMixin exportMixin1) Copy(exportMixin, exportMixin1);
            if (clone is ReactionMixin reactionMixin && value is ReactionMixin reactionMixin1) Copy(reactionMixin, reactionMixin1);
            if (clone is TransitionMixin transitionMixin && value is TransitionMixin transitionMixin1) Copy(transitionMixin, transitionMixin1);
            if (clone is DefaultShapeMixin shapeMixin && value is DefaultShapeMixin defaultShapeMixin) Copy(shapeMixin, defaultShapeMixin);
            if (clone is DefaultFrameMixin frameMixin && value is DefaultFrameMixin defaultFrameMixin) Copy(frameMixin, defaultFrameMixin);

            return clone;
        }
        public static BaseNode Replace(this BaseNode value, BaseNode prefab)
        {
            BaseNode clone = prefab.Clone();

            if (value.parent is ChildrenMixin childrenMixin && clone is SceneNode cloneSceneNode)
                childrenMixin.children[Array.IndexOf(childrenMixin.children, (SceneNode)prefab)] = cloneSceneNode;

            value.parent = default;

            return clone;
        }

        public static bool EnabledInHierarchy(this BaseNode node) => !rootMetadata.Any(x => x.Value.figma.Filter) || GetMetadata(node).root is not null;
        public static bool ShouldDownload(this BaseNode node, UxmlDownloadImages flag)
        {
            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is not null && metadata.root.figma.Filter)
            {
                bool shouldDownload = metadata.root.downloadImages == UxmlDownloadImages.Everything || metadata.root.downloadImages.HasFlag(flag);
                if (metadata.root.downloadImages.HasFlag(UxmlDownloadImages.ByElements))
                {
                    if (metadata.query is not null)
                    {
                        if (metadata.query.query.ImageFiltering == ElementDownloadImage.Download) return true;
                        if (metadata.query.query.ImageFiltering == ElementDownloadImage.Ignore) return false;
                        return shouldDownload;
                    }

                    return shouldDownload;
                }

                return shouldDownload;
            }

            return true;
        }
        public static (bool hash, string value) GetTemplate(this BaseNode node)
        {
            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is not null && metadata.root.figma.Filter && metadata.query is not null)
            {
                if (metadata.query.query.Template == "Hash")
                {
                    string GetFullPath(BaseNode x) => x.parent is not null ? $"{GetFullPath(x.parent)}/{x.name}" : x.name;
                    return (true, $"{metadata.query.fieldInfo.FieldType.Name}-{Hash128.Compute(GetFullPath(node))}");
                }

                return (false, metadata.query.query.Template);
            }

            return (false, default);
        }
        public static FieldInfo GetFieldInfo(this BaseNode node)
        {
            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is not null && metadata.root.figma.Filter && metadata.root.uxml.TypeIdentification == UxmlElementTypeIdentification.ByElementType && metadata.query is not null)
                return metadata.query.fieldInfo;

            return default;
        }
        public static ElementType GetElementType(this BaseNode node)
        {
            ElementType FieldTypeToElementType(Type type)
            {
                //Base elements
                if (type == typeof(VisualElement)) return ElementType.VisualElement;
                if (type == typeof(BindableElement)) return ElementType.BindableElement;

                //Utilities
                if (type == typeof(Box)) return ElementType.Box;
                if (type == typeof(TextElement)) return ElementType.TextElement;
                if (type == typeof(Label)) return ElementType.Label;
                if (type == typeof(Image)) return ElementType.Image;
                if (type == typeof(IMGUIContainer)) return ElementType.IMGUIContainer;
                if (type == typeof(Foldout)) return ElementType.Foldout;

                //Templates

                //Controls
                if (type == typeof(Button)) return ElementType.Button;
                if (type == typeof(RepeatButton)) return ElementType.RepeatButton;
                if (type == typeof(Toggle)) return ElementType.Toggle;
                if (type == typeof(Scroller)) return ElementType.Scroller;
                if (type == typeof(Slider)) return ElementType.Slider;
                if (type == typeof(SliderInt)) return ElementType.SliderInt;
                if (type == typeof(MinMaxSlider)) return ElementType.MinMaxSlider;
                if (type == typeof(EnumField)) return ElementType.EnumField;
                if (type == typeof(MaskField)) return ElementType.MaskField;
                if (type == typeof(LayerField)) return ElementType.LayerField;
                if (type == typeof(LayerMaskField)) return ElementType.LayerMaskField;
                if (type == typeof(TagField)) return ElementType.TagField;
                if (type == typeof(ProgressBar)) return ElementType.ProgressBar;

                //Text input
                if (type == typeof(TextField)) return ElementType.TextField;
                if (type == typeof(IntegerField)) return ElementType.IntegerField;
                if (type == typeof(LongField)) return ElementType.LongField;
                if (type == typeof(FloatField)) return ElementType.FloatField;
                if (type == typeof(DoubleField)) return ElementType.DoubleField;
                if (type == typeof(Vector2Field)) return ElementType.Vector2Field;
                if (type == typeof(Vector2IntField)) return ElementType.Vector2IntField;
                if (type == typeof(Vector3Field)) return ElementType.Vector3Field;
                if (type == typeof(Vector3IntField)) return ElementType.Vector3IntField;
                if (type == typeof(Vector4Field)) return ElementType.Vector4Field;
                if (type == typeof(RectField)) return ElementType.RectField;
                if (type == typeof(RectIntField)) return ElementType.RectIntField;
                if (type == typeof(BoundsField)) return ElementType.BoundsField;
                if (type == typeof(BoundsIntField)) return ElementType.BoundsIntField;

                //Complex widgets
                if (type == typeof(PropertyField)) return ElementType.PropertyField;
                //if (fieldType == typeof(PropertyControl<int>)) return ElementType.PropertyControlInt;
                //if (fieldType == typeof(PropertyControl<long>)) return ElementType.PropertyControlLong;
                //if (fieldType == typeof(PropertyControl<float>)) return ElementType.PropertyControlFloat;
                //if (fieldType == typeof(PropertyControl<double>)) return ElementType.PropertyControlDouble;
                //if (fieldType == typeof(PropertyControl<string>)) return ElementType.PropertyControlString;
                if (type == typeof(ColorField)) return ElementType.ColorField;
                if (type == typeof(CurveField)) return ElementType.CurveField;
                if (type == typeof(GradientField)) return ElementType.GradientField;
                if (type == typeof(ObjectField)) return ElementType.ObjectField;

                //Toolbar
                if (type == typeof(Toolbar)) return ElementType.Toolbar;
                if (type == typeof(ToolbarButton)) return ElementType.ToolbarButton;
                if (type == typeof(ToolbarToggle)) return ElementType.ToolbarToggle;
                if (type == typeof(ToolbarMenu)) return ElementType.ToolbarMenu;
                if (type == typeof(ToolbarSearchField)) return ElementType.ToolbarSearchField;
                if (type == typeof(ToolbarPopupSearchField)) return ElementType.ToolbarPopupSearchField;
                if (type == typeof(ToolbarSpacer)) return ElementType.ToolbarSpacer;

                //Views and windows
                if (type == typeof(ListView)) return ElementType.ListView;
                if (type == typeof(ScrollView)) return ElementType.ScrollView;
                //if (type == typeof(TreeView)) return ElementType.TreeView;
                if (type == typeof(PopupWindow)) return ElementType.PopupWindow;

                if (typeof(VisualElement).IsAssignableFrom(type)) return ElementType.IElement;

                throw new ArgumentOutOfRangeException();
            }

            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is not null && metadata.root.figma.Filter && metadata.root.uxml.TypeIdentification == UxmlElementTypeIdentification.ByElementType && metadata.query is not null)
                return FieldTypeToElementType(metadata.query.fieldInfo.FieldType);

            return ElementType.None;
        }
        #endregion

        #region Node Methods
        public static void SetParentRecursively(this BaseNode node)
        {
            if (node is DocumentNode document)
            {
                foreach (CanvasNode canvas in document.children)
                {
                    canvas.parent = node;
                    SetParentRecursively(canvas);
                }
            }
            else if (node is ChildrenMixin children)
            {
                foreach (SceneNode child in children.children)
                {
                    child.parent = node;
                    SetParentRecursively(child);
                }
            }
        }

        public static void Copy(this BaseNodeMixin destination, BaseNodeMixin source)
        {
            destination.type = source.type;
            destination.id = source.id;
            destination.parent = source.parent;
            destination.name = source.name;
        }
        public static void Copy(this SceneNodeMixin destination, SceneNodeMixin source)
        {
            destination.visible = source.visible;
            destination.locked = source.locked;
        }
        public static void Copy(this ChildrenMixin destination, ChildrenMixin source)
        {
            destination.children = source.children;
        }
        public static void Copy(this ConstraintMixin destination, ConstraintMixin source)
        {
            if (source.constraints is null) return;
            destination.constraints = new Constraints
            {
                horizontal = source.constraints.horizontal,
                vertical = source.constraints.vertical
            };
        }
        public static void Copy(this LayoutMixin destination, LayoutMixin source)
        {
            destination.layoutAlign = source.layoutAlign;
            destination.layoutGrow = source.layoutGrow;
            destination.absoluteBoundingBox = source.absoluteBoundingBox;
        }
        public static void Copy(this BlendMixin destination, BlendMixin source)
        {
            destination.opacity = source.opacity;
            destination.blendMode = source.blendMode;
            destination.isMask = source.isMask;
            destination.isMaskOutline = source.isMaskOutline;
            destination.effects = source.effects?.Duplicate();
            destination.styles = source.styles;
            destination.preserveRatio = source.preserveRatio;
        }
        public static void Copy(this ContainerMixin destination, ContainerMixin source)
        {
            destination.background = source.background?.Duplicate();
            destination.backgroundColor = source.backgroundColor;
        }
        public static void Copy(this GeometryMixin destination, GeometryMixin source)
        {
            destination.fills = source.fills?.Duplicate();
            destination.strokes = source.strokes?.Duplicate();
            destination.strokeWeight = source.strokeWeight;
            destination.strokeAlign = source.strokeAlign;
            destination.strokeCap = source.strokeCap;
            destination.strokeJoin = source.strokeJoin;
        }
        public static void Copy(this CornerMixin destination, CornerMixin source)
        {
            destination.cornerRadius = source.cornerRadius;
        }
        public static void Copy(this RectangleCornerMixin destination, RectangleCornerMixin source)
        {
            destination.rectangleCornerRadii = (number[])source.rectangleCornerRadii?.Clone();
        }
        public static void Copy(this ExportMixin destination, ExportMixin source)
        {
            destination.exportSettings = source.exportSettings?.Duplicate();
        }
        public static void Copy(this ReactionMixin destination, ReactionMixin source)
        {
            destination.reactions = source.reactions?.Duplicate();
        }
        public static void Copy(this TransitionMixin destination, TransitionMixin source)
        {
            destination.transitionNodeID = source.transitionNodeID;
            destination.transitionDuration = source.transitionDuration;
            destination.transitionEasing = source.transitionEasing;
        }
        public static void Copy(this DefaultShapeMixin destination, DefaultShapeMixin source)
        {
        }
        public static void Copy(this DefaultFrameMixin destination, DefaultFrameMixin source)
        {
            destination.layoutMode = source.layoutMode;
            destination.primaryAxisSizingMode = source.primaryAxisSizingMode;
            destination.primaryAxisAlignItems = source.primaryAxisAlignItems;
            destination.counterAxisSizingMode = source.counterAxisSizingMode;
            destination.counterAxisAlignItems = source.counterAxisAlignItems;
            destination.paddingLeft = source.paddingLeft;
            destination.paddingTop = source.paddingTop;
            destination.paddingRight = source.paddingRight;
            destination.paddingBottom = source.paddingBottom;
            destination.itemSpacing = source.itemSpacing;
            destination.layoutGrids = source.layoutGrids?.Duplicate();
            destination.clipsContent = source.clipsContent;
            destination.overflowDirection = source.overflowDirection;
        }
        public static Effect[] Duplicate(this Effect[] value)
        {
            Effect[] clone = new Effect[value.Length];
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] is ShadowEffect shadowEffect)
                {
                    ShadowEffect effect = new()
                    {
                        type = shadowEffect.type,
                        color = shadowEffect.color,
                        offset = shadowEffect.offset,
                        radius = shadowEffect.radius,
                        visible = shadowEffect.visible,
                        blendMode = shadowEffect.blendMode
                    };
                    clone[i] = effect;
                }
                if (value[i] is BlurEffect blurEffect)
                {
                    BlurEffect effect = new()
                    {
                        type = blurEffect.type,
                        radius = blurEffect.radius,
                        visible = blurEffect.visible
                    };
                    clone[i] = effect;
                }
            }
            return clone;
        }
        public static Paint[] Duplicate(this Paint[] value)
        {
            Paint[] clone = new Paint[value.Length];
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] is SolidPaint solidPaint)
                {
                    SolidPaint paint = new()
                    {
                        type = solidPaint.type,
                        color = solidPaint.color,
                        visible = solidPaint.visible,
                        opacity = solidPaint.opacity,
                        blendMode = solidPaint.blendMode
                    };
                    clone[i] = paint;
                }
                if (value[i] is GradientPaint gradientPaint)
                {
                    GradientPaint paint = new()
                    {
                        type = gradientPaint.type,
                        gradientStops = gradientPaint.gradientStops,
                        visible = gradientPaint.visible,
                        opacity = gradientPaint.opacity,
                        blendMode = gradientPaint.blendMode,
                        gradientHandlePositions = gradientPaint.gradientHandlePositions
                    };
                    clone[i] = paint;
                }
                if (value[i] is ImagePaint imagePaint)
                {
                    ImagePaint paint = new()
                    {
                        type = imagePaint.type,
                        scaleMode = imagePaint.scaleMode,
                        imageTransform = imagePaint.imageTransform,
                        visible = imagePaint.visible,
                        opacity = imagePaint.opacity,
                        blendMode = imagePaint.blendMode,
                        imageRef = imagePaint.imageRef
                    };
                    clone[i] = paint;
                }
            }
            return clone;
        }
        public static ExportSettings[] Duplicate(this ExportSettings[] value)
        {
            ExportSettings[] clone = new ExportSettings[value.Length];
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] is ExportSettingsImage exportSettingsImage)
                {
                    ExportSettingsImage exportSettings = new()
                    {
                        format = exportSettingsImage.format,
                        contentsOnly = exportSettingsImage.contentsOnly,
                        suffix = exportSettingsImage.suffix,
                        constraint = new ExportSettingsConstraints { type = exportSettingsImage.constraint.type, value = exportSettingsImage.constraint.value }
                    };
                    clone[i] = exportSettings;
                }
                if (value[i] is ExportSettingsSVG exportSettingsSVG)
                {
                    ExportSettingsSVG exportSettings = new()
                    {
                        format = exportSettingsSVG.format,
                        contentsOnly = exportSettingsSVG.contentsOnly,
                        suffix = exportSettingsSVG.suffix,
                        svgOutlineText = exportSettingsSVG.svgOutlineText,
                        svgIdAttribute = exportSettingsSVG.svgIdAttribute,
                        svgSimplifyStroke = exportSettingsSVG.svgSimplifyStroke,
                        constraint = new ExportSettingsConstraints { type = exportSettingsSVG.constraint.type, value = exportSettingsSVG.constraint.value }
                    };

                    clone[i] = exportSettings;
                }
                if (value[i] is ExportSettingsPDF exportSettingsPDF)
                {
                    ExportSettingsPDF exportSettings = new()
                    {
                        format = exportSettingsPDF.format,
                        contentsOnly = exportSettingsPDF.contentsOnly,
                        suffix = exportSettingsPDF.suffix,
                        constraint = new ExportSettingsConstraints { type = exportSettingsPDF.constraint.type, value = exportSettingsPDF.constraint.value }
                    };
                    clone[i] = exportSettings;
                }
            }
            return clone;
        }
        public static Reaction[] Duplicate(this Reaction[] value)
        {
            Reaction[] clone = new Reaction[value.Length];
            for (int i = 0; i < value.Length; ++i)
            {
                Reaction reaction = new();
                Reaction valueReaction = value[i];

                reaction.action = new Action
                {
                    type = valueReaction.action.type,
                    url = valueReaction.action.url,
                    destinationId = valueReaction.action.destinationId,
                    navigation = valueReaction.action.navigation,
                    preserveScrollPosition = valueReaction.action.preserveScrollPosition,
                    overlayRelativePosition = valueReaction.action.overlayRelativePosition,
                };

                if (valueReaction.action.transition is SimpleTransition simpleTransition)
                {
                    valueReaction.action.transition = new SimpleTransition
                    {
                        type = simpleTransition.type,
                        easing = simpleTransition.easing,
                        duration = simpleTransition.duration
                    };
                }
                if (valueReaction.action.transition is DirectionalTransition directionalTransition)
                {
                    valueReaction.action.transition = new DirectionalTransition
                    {
                        type = directionalTransition.type,
                        direction = directionalTransition.direction,
                        matchLayers = directionalTransition.matchLayers,
                        easing = directionalTransition.easing,
                        duration = directionalTransition.duration
                    };
                }

                reaction.trigger = new Trigger
                {
                    type = valueReaction.trigger.type,
                    delay = valueReaction.trigger.delay
                };

                clone[i] = reaction;
            }
            return clone;
        }
        public static LayoutGrid[] Duplicate(this LayoutGrid[] grids)
        {
            LayoutGrid[] clone = new LayoutGrid[grids.Length];
            for (int i = 0; i < grids.Length; ++i)
            {
                if (grids[i] is RowsColsLayoutGrid rowsColsLayoutGrid)
                {
                    RowsColsLayoutGrid grid = new()
                    {
                        pattern = rowsColsLayoutGrid.pattern,
                        alignment = rowsColsLayoutGrid.alignment,
                        gutterSize = rowsColsLayoutGrid.gutterSize,
                        count = rowsColsLayoutGrid.count,
                        sectionSize = rowsColsLayoutGrid.sectionSize,
                        offset = rowsColsLayoutGrid.offset,
                        visible = rowsColsLayoutGrid.visible,
                        color = rowsColsLayoutGrid.color
                    };
                    clone[i] = grid;
                }
                if (grids[i] is GridLayoutGrid gridLayoutGrid)
                {
                    GridLayoutGrid grid = new()
                    {
                        pattern = gridLayoutGrid.pattern,
                        sectionSize = gridLayoutGrid.sectionSize,
                        visible = gridLayoutGrid.visible,
                        color = gridLayoutGrid.color,
                        alignment = gridLayoutGrid.alignment,
                        gutterSize = gridLayoutGrid.gutterSize,
                        count = gridLayoutGrid.count,
                        offset = gridLayoutGrid.offset
                    };
                    clone[i] = grid;
                }
            }
            return clone;
        }

        public static string GetHash(this GradientPaint gradient)
        {
            using SHA1CryptoServiceProvider sha1 = new();
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write((int)gradient.type);
            foreach (ColorStop stop in gradient.gradientStops)
            {
                writer.Write(stop.position);
                writer.Write(stop.color.r);
                writer.Write(stop.color.g);
                writer.Write(stop.color.b);
                writer.Write(stop.color.a);
            }
            foreach (Vector position in gradient.gradientHandlePositions)
            {
                writer.Write(position.x);
                writer.Write(position.y);
            }

            byte[] bytes = stream.ToArray();
            byte[] hashBytes = sha1.ComputeHash(bytes);

            StringBuilder hashBuilder = new();
            foreach (byte @byte in hashBytes)
                hashBuilder.Append(@byte.ToString("x2"));

            return hashBuilder.ToString();
        }
        #endregion

        #region Support Methods
        static BaseNode FindRoot(BaseNode value)
        {
            if (rootMetadata.ContainsKey(value)) return value;
            return value.parent is not null ? FindRoot(value.parent) : default;
        }
        static BaseNodeMetadata GetMetadata(BaseNode value)
        {
            static BaseNode FindRootInChildren(BaseNode value)
            {
                if (rootMetadata.ContainsKey(value)) return value;

                if (value is DocumentNode documentNode)
                {
                    foreach (CanvasNode child in documentNode.children)
                    {
                        BaseNode node = FindRootInChildren(child);
                        if (node is not null) return node;
                    }
                }

                if (value is ChildrenMixin children)
                {
                    foreach (SceneNode child in children.children)
                    {
                        BaseNode node = FindRootInChildren(child);
                        if (node is not null) return node;
                    }
                }

                return default;
            }

            BaseNode root = FindRoot(value) ?? FindRootInChildren(value);
            return root is not null ? new BaseNodeMetadata(rootMetadata[root], queryMetadata.TryGetValue(value, out QueryMetadata metadata) ? metadata : default) : new BaseNodeMetadata(default, default);
        }

        static void Initialize(Type targetType, BaseNode targetRoot, bool throwException = true)
        {
            BaseNode InitializeElement(QueryAttribute queryRoot, QueryAttribute query, bool throwException)
            {
                BaseNode ResolveElement(BaseNode node, bool throwException)
                {
                    if (query is null) throw new ArgumentNullException();
                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (queryRoot is not null && queryRoot != query) return node.Find(queryRoot.Path, throwException).Find(query.Path, throwException);
                    return node.Find(query.Path, throwException);
                }

                BaseNode value = ResolveElement(targetRoot, throwException: throwException && query.ReplaceElementPath.NullOrEmpty() && query.RebuildElementEvent.NullOrEmpty());

                if (query.ReplaceNodePath.NotNullOrEmpty())
                {
                    value = value.Replace(targetRoot.Find(query.ReplaceNodePath));
                }
                if (query.ReplaceNodeEvent.NotNullOrEmpty())
                {
                    MethodInfo methodInfo = targetType.GetMethod(query.ReplaceNodeEvent, MethodsFlags);
                    if (methodInfo != null) value = (BaseNode)methodInfo.Invoke(default, new object[] { value });
                }

                return value;
            }

            QueryAttribute queryRoot = default;
            foreach (FieldInfo field in targetType.GetFields(FieldsFlags))
            {
                QueryAttribute query = field.GetCustomAttribute<QueryAttribute>();
                if (query is null) continue;
                if (query.StartRoot) queryRoot = query;

                BaseNode node = InitializeElement(queryRoot, query, throwException && !query.Nullable);
                if (node is not null && !queryMetadata.ContainsKey(node)) queryMetadata.Add(node, new QueryMetadata(field, queryRoot, query));

                if (query.EndRoot) queryRoot = default;

                if (node is not null && typeof(ISubElement).IsAssignableFrom(field.FieldType)) Initialize(field.FieldType, node, throwException);
            }
        }
        #endregion
    }
}
