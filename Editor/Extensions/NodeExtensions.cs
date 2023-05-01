using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Trackman;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace System.Runtime.CompilerServices { class IsExternalInit { } }
namespace Figma
{
    using Attributes;
    using global;
    using number = Double;

    public static class NodeMetadata
    {
        public record RootMetadata(UIDocument document, Figma figma, Type type, UxmlAttribute uxml, UxmlDownloadImages downloadImages);
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
        public static void Initialize(UIDocument document, Figma figma, MonoBehaviour[] targets, DocumentNode root, bool throwException = true)
        {
            foreach (IRootElement target in targets) Initialize(document, figma, root, target, throwException);
        }
        public static void Initialize(UIDocument document, Figma figma, DocumentNode root, IRootElement target, bool throwException = true)
        {
            Type targetType = target.GetType();
            UxmlAttribute uxml = targetType.GetCustomAttribute<UxmlAttribute>();
            BaseNode targetRoot = root.Find(uxml.Root);
            BaseNode[] targetPreserve = uxml.Preserve.Select(x => root.Find(x)).ToArray();

            rootMetadata.Add(targetRoot, new RootMetadata(document, figma, targetType, uxml, uxml.ImageFiltering));
            foreach (BaseNode value in targetPreserve) if (!rootMetadata.ContainsKey(value)) rootMetadata.Add(value, new RootMetadata(document, figma, targetType, uxml, UxmlDownloadImages.Everything));

            Initialize(targetType, targetRoot, throwException);
        }
        public static void Clear(UIDocument document)
        {
            foreach (BaseNode node in queryMetadata.Keys.Where(x => GetMetadata(x).root?.document == document).ToList()) queryMetadata.Remove(node);
            foreach (BaseNode node in rootMetadata.Where(x => x.Value.document == document).Select(x => x.Key).ToList()) rootMetadata.Remove(node);
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
            static void Search(BaseNode root, BaseNode value, string path, int startIndex = 0, string className = default)
            {
                static bool IsVisible(BaseNodeMixin mixin)
                {
                    if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;
                    if (mixin.parent is not null) return IsVisible(mixin.parent);
                    else return true;
                }
                static IEnumerable<BaseNode> GetChildren(BaseNode value)
                {
                    if (value is DocumentNode documentNode) foreach (BaseNode child in documentNode.children) yield return child;
                    else if (value is ChildrenMixin childrenMixin) foreach (BaseNode child in childrenMixin.children) yield return child;
                    else yield break;
                }
                IEnumerable<BaseNode> children = GetChildren(value);

                static bool EqualsTo(BaseNode value, string path, int startIndex)
                {
                    return path.EqualsTo(value.name, startIndex);
                }

                foreach (BaseNode child in children)
                {
                    if (!IsVisible(child)) continue;
                    if (child.name.NotNullOrEmpty() && EqualsTo(child, path, startIndex)) search.Add(child);
                }

                foreach (BaseNode child in children)
                {
                    if (!IsVisible(child)) continue;
                    if (child.name.NotNullOrEmpty() && StartsWith(path, child, startIndex))
                        Search(root, child, path, startIndex + child.name.Length + 1, className);
                }
            }
            static void SearchByFullPath(BaseNode value, string path, int startIndex = 0)
            {
                static bool IsVisible(BaseNodeMixin mixin)
                {
                    if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;
                    if (mixin.parent is not null) return IsVisible(mixin.parent);
                    else return true;
                }
                static IEnumerable<BaseNode> GetChildren(BaseNode value)
                {
                    if (value is DocumentNode documentNode) foreach (BaseNode child in documentNode.children) yield return child;
                    else if (value is ChildrenMixin childrenMixin) foreach (BaseNode child in childrenMixin.children) yield return child;
                    else yield break;
                }
                IEnumerable<BaseNode> children = GetChildren(value);

                static bool EqualsToFullPath(BaseNode root, BaseNode value, string path, int startIndex)
                {
                    return LastIndexOf(root, value, value, path, startIndex) == path.Length;
                }
                static bool StartsWithFullPath(BaseNode root, BaseNode value, string path, int startIndex)
                {
                    int endIndex = LastIndexOf(root, value, value, path, startIndex);
                    return endIndex >= 0 && path.Length > endIndex && path[endIndex].IsSeparator();
                }

                foreach (BaseNode child in children)
                {
                    if (!IsVisible(child)) continue;
                    if (child.name.NotNullOrEmpty() && EqualsToFullPath(value, child, path, startIndex)) search.Add(child);
                }

                foreach (BaseNode child in children)
                {
                    if (!IsVisible(child)) continue;
                    if (child.name.NotNullOrEmpty() && StartsWithFullPath(value, child, path, startIndex))
                        SearchByFullPath(child, path, startIndex + child.name.Length + 1);
                }
            }

            search.Clear();

            BaseNode root = FindRoot(value);
            if (root is not null)
            {
                UxmlAttribute uxml = rootMetadata[root].uxml;
                if (path.BeginsWith(uxml.DocumentRoot) || uxml.DocumentPreserve.Any(x => path.BeginsWith(x))) SearchByFullPath(root.parent.parent, path, UxmlAttribute.prefix.Length + 1);
                else Search(value, value, path, 0);
            }
            else
            {
                SearchByFullPath(value, path, 0);
            }

            foreach (T result in search) yield return result;
        }
        public static T Find<T>(this BaseNode value, string path, bool throwException = true, bool silent = false) where T : BaseNode
        {
            string GetFullPath(BaseNode node)
            {
                if (node.parent is not null) return $"{GetFullPath(node.parent)}/{node.name}";
                return node.name;
            }

            T result = value.Search<T>(path).FirstOrDefault();
            if (result is not null)
            {
                return result;
            }
            else if (throwException)
            {
                throw new Exception($"Cannot find {typeof(T).Name} at [<color=yellow>{GetFullPath(value)}/{path}</color>]");
            }
            else
            {
                if (!silent) Debug.LogWarning($"Cannot find {typeof(T).Name} at [<color=yellow>{GetFullPath(value)}/{path}</color>]");
                return default;
            }
        }
        public static BaseNode Find(this BaseNode value, string path, bool throwException = true, bool silent = true) => Find<BaseNode>(value, path, throwException, silent);

        public static BaseNode Clone(this BaseNode value, BaseNode parent = default)
        {
            BaseNode clone = (BaseNode)Activator.CreateInstance(value.GetType());
            if (clone is BaseNodeMixin && value is BaseNodeMixin) Copy(clone, value);
            if (clone is SceneNodeMixin && value is SceneNodeMixin) Copy((SceneNodeMixin)clone, (SceneNodeMixin)value);
            if (clone is ChildrenMixin && value is ChildrenMixin) Copy((ChildrenMixin)clone, (ChildrenMixin)value);
            if (clone is ConstraintMixin && value is ConstraintMixin) Copy((ConstraintMixin)clone, (ConstraintMixin)value);
            if (clone is LayoutMixin && value is LayoutMixin) Copy((LayoutMixin)clone, (LayoutMixin)value);
            if (clone is BlendMixin && value is BlendMixin) Copy((BlendMixin)clone, (BlendMixin)value);
            if (clone is ContainerMixin && value is ContainerMixin) Copy((ContainerMixin)clone, (ContainerMixin)value);
            if (clone is GeometryMixin && value is GeometryMixin) Copy((GeometryMixin)clone, (GeometryMixin)value);
            if (clone is CornerMixin && value is CornerMixin) Copy((CornerMixin)clone, (CornerMixin)value);
            if (clone is RectangleCornerMixin && value is RectangleCornerMixin) Copy((RectangleCornerMixin)clone, (RectangleCornerMixin)value);
            if (clone is ExportMixin && value is ExportMixin) Copy((ExportMixin)clone, (ExportMixin)value);
            if (clone is ReactionMixin && value is ReactionMixin) Copy((ReactionMixin)clone, (ReactionMixin)value);
            if (clone is TransitionMixin && value is TransitionMixin) Copy((TransitionMixin)clone, (TransitionMixin)value);
            if (clone is DefaultShapeMixin && value is DefaultShapeMixin) Copy((DefaultShapeMixin)clone, (DefaultShapeMixin)value);
            if (clone is DefaultFrameMixin && value is DefaultFrameMixin) Copy((DefaultFrameMixin)clone, (DefaultFrameMixin)value);

            if (parent is not null) clone.parent = parent;

            return clone;
        }
        public static BaseNode Replace(this BaseNode value, BaseNode prefab)
        {
            BaseNode clone = prefab.Clone();

            if (value.parent is ChildrenMixin childrenMixin && clone is SceneNode cloneSceneNode)
                childrenMixin.children[Array.IndexOf(childrenMixin.children, prefab)] = cloneSceneNode;

            value.parent = default;

            return clone;
        }

        public static bool EnabledInHierarchy(this BaseNode node)
        {
            if (rootMetadata.Any(x => x.Value.figma.Filter) && GetMetadata(node).root is null) return false;
            return true;
        }
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
                        else if (metadata.query.query.ImageFiltering == ElementDownloadImage.Ignore) return false;
                        else return shouldDownload;
                    }
                    else return shouldDownload;
                }
                else return shouldDownload;
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
                    string GetFullPath(BaseNode node)
                    {
                        if (node.parent is not null) return $"{GetFullPath(node.parent)}/{node.name}";
                        return node.name;
                    }

                    return (true, $"{metadata.query.fieldInfo.FieldType.Name}-{Hash128.Compute(GetFullPath(node))}");
                }
                else
                {
                    return (false, metadata.query.query.Template);
                }
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
                foreach (BaseNode child in children.children)
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
            if (source.constraints is not null)
            {
                destination.constraints = new Constraints();
                destination.constraints.horizontal = source.constraints.horizontal;
                destination.constraints.vertical = source.constraints.vertical;
            }
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
                    ShadowEffect effect = new ShadowEffect();
                    effect.type = shadowEffect.type;
                    effect.color = shadowEffect.color;
                    effect.offset = shadowEffect.offset;
                    effect.radius = shadowEffect.radius;
                    effect.visible = shadowEffect.visible;
                    effect.blendMode = shadowEffect.blendMode;
                    clone[i] = effect;
                }
                if (value[i] is BlurEffect blurEffect)
                {
                    BlurEffect effect = new BlurEffect();
                    effect.type = blurEffect.type;
                    effect.radius = blurEffect.radius;
                    effect.visible = blurEffect.visible;
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
                    SolidPaint paint = new SolidPaint();
                    paint.type = solidPaint.type;
                    paint.color = solidPaint.color;
                    paint.visible = solidPaint.visible;
                    paint.opacity = solidPaint.opacity;
                    paint.blendMode = solidPaint.blendMode;
                    clone[i] = paint;
                }
                if (value[i] is GradientPaint gradientPaint)
                {
                    GradientPaint paint = new GradientPaint();
                    paint.type = gradientPaint.type;
                    paint.gradientStops = gradientPaint.gradientStops;
                    paint.visible = gradientPaint.visible;
                    paint.opacity = gradientPaint.opacity;
                    paint.blendMode = gradientPaint.blendMode;
                    paint.gradientHandlePositions = gradientPaint.gradientHandlePositions;
                    clone[i] = paint;
                }
                if (value[i] is ImagePaint imagePaint)
                {
                    ImagePaint paint = new ImagePaint();
                    paint.type = imagePaint.type;
                    paint.scaleMode = imagePaint.scaleMode;
                    paint.imageTransform = imagePaint.imageTransform;
                    paint.visible = imagePaint.visible;
                    paint.opacity = imagePaint.opacity;
                    paint.blendMode = imagePaint.blendMode;
                    paint.imageRef = imagePaint.imageRef;
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
                    ExportSettingsImage exportSettings = new ExportSettingsImage();
                    exportSettings.format = exportSettingsImage.format;
                    exportSettings.contentsOnly = exportSettingsImage.contentsOnly;
                    exportSettings.suffix = exportSettingsImage.suffix;
                    exportSettings.constraint = new ExportSettingsConstraints() { type = exportSettingsImage.constraint.type, value = exportSettingsImage.constraint.value };
                    clone[i] = exportSettings;
                }
                if (value[i] is ExportSettingsSVG exportSettingsSVG)
                {
                    ExportSettingsSVG exportSettings = new ExportSettingsSVG();
                    exportSettings.format = exportSettingsSVG.format;
                    exportSettings.contentsOnly = exportSettingsSVG.contentsOnly;
                    exportSettings.suffix = exportSettingsSVG.suffix;
                    exportSettings.svgOutlineText = exportSettingsSVG.svgOutlineText;
                    exportSettings.svgIdAttribute = exportSettingsSVG.svgIdAttribute;
                    exportSettings.svgSimplifyStroke = exportSettingsSVG.svgSimplifyStroke;
                    exportSettings.constraint = new ExportSettingsConstraints() { type = exportSettingsSVG.constraint.type, value = exportSettingsSVG.constraint.value };

                    clone[i] = exportSettings;
                }
                if (value[i] is ExportSettingsPDF exportSettingsPDF)
                {
                    ExportSettingsPDF exportSettings = new ExportSettingsPDF();
                    exportSettings.format = exportSettingsPDF.format;
                    exportSettings.contentsOnly = exportSettingsPDF.contentsOnly;
                    exportSettings.suffix = exportSettingsPDF.suffix;
                    exportSettings.constraint = new ExportSettingsConstraints() { type = exportSettingsPDF.constraint.type, value = exportSettingsPDF.constraint.value };
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
                Reaction reaction = new Reaction();
                Reaction valueReaction = value[i];

                reaction.action = new global.Action()
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
                    valueReaction.action.transition = new SimpleTransition()
                    {
                        type = simpleTransition.type,
                        easing = simpleTransition.easing,
                        duration = simpleTransition.duration
                    };
                }
                if (valueReaction.action.transition is DirectionalTransition directionalTransition)
                {
                    valueReaction.action.transition = new DirectionalTransition()
                    {
                        type = directionalTransition.type,
                        direction = directionalTransition.direction,
                        matchLayers = directionalTransition.matchLayers,
                        easing = directionalTransition.easing,
                        duration = directionalTransition.duration
                    };
                }

                reaction.trigger = new Trigger()
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
                    RowsColsLayoutGrid grid = new RowsColsLayoutGrid();
                    grid.pattern = rowsColsLayoutGrid.pattern;
                    grid.alignment = rowsColsLayoutGrid.alignment;
                    grid.gutterSize = rowsColsLayoutGrid.gutterSize;
                    grid.count = rowsColsLayoutGrid.count;
                    grid.sectionSize = rowsColsLayoutGrid.sectionSize;
                    grid.offset = rowsColsLayoutGrid.offset;
                    grid.visible = rowsColsLayoutGrid.visible;
                    grid.color = rowsColsLayoutGrid.color;
                    clone[i] = grid;
                }
                if (grids[i] is GridLayoutGrid gridLayoutGrid)
                {
                    GridLayoutGrid grid = new GridLayoutGrid();
                    grid.pattern = gridLayoutGrid.pattern;
                    grid.sectionSize = gridLayoutGrid.sectionSize;
                    grid.visible = gridLayoutGrid.visible;
                    grid.color = gridLayoutGrid.color;
                    grid.alignment = gridLayoutGrid.alignment;
                    grid.gutterSize = gridLayoutGrid.gutterSize;
                    grid.count = gridLayoutGrid.count;
                    grid.offset = gridLayoutGrid.offset;
                    clone[i] = grid;
                }
            }
            return clone;
        }

        public static string GetHash(this GradientPaint gradient)
        {
            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
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

                StringBuilder hashBuilder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) hashBuilder.Append(hashBytes[i].ToString("x2"));
                return hashBuilder.ToString();
            }
        }
        #endregion

        #region Support Methods
        static BaseNode FindRoot(BaseNode value)
        {
            if (rootMetadata.ContainsKey(value)) return value;
            else if (value.parent is not null) return FindRoot(value.parent);
            else return default;
        }
        static BaseNodeMetadata GetMetadata(BaseNode value)
        {
            static BaseNode FindRootInChildren(BaseNode value)
            {
                if (rootMetadata.ContainsKey(value)) return value;

                if (value is DocumentNode documentNode)
                {
                    foreach (BaseNode child in documentNode.children)
                    {
                        BaseNode node = FindRootInChildren(child);
                        if (node is not null) return node;
                    }
                }

                if (value is ChildrenMixin children)
                {
                    foreach (BaseNode child in children.children)
                    {
                        BaseNode node = FindRootInChildren(child);
                        if (node is not null) return node;
                    }
                }

                return default;
            }

            BaseNode root = FindRoot(value) ?? FindRootInChildren(value);
            if (root is not null) return new BaseNodeMetadata(rootMetadata[root], queryMetadata.ContainsKey(value) ? queryMetadata[value] : default);

            return new BaseNodeMetadata(default, default);
        }

        static void Initialize(Type targetType, BaseNode targetRoot, bool throwException = true)
        {
            BaseNode ResolveElement(Type targetType, QueryAttribute queryRoot, QueryAttribute query, bool throwException = true)
            {
                BaseNode Find(BaseNode root, QueryAttribute queryRoot, QueryAttribute query, bool throwException = true)
                {
                    if (query is not null)
                    {
                        if (queryRoot is not null && queryRoot != query) return root.Find(queryRoot.Path, throwException).Find(query.Path, throwException);
                        return root.Find(query.Path, throwException);
                    }
                    else throw new ArgumentNullException();
                }

                BaseNode value = Find(targetRoot, queryRoot, query, throwException: throwException && query.ReplaceElementPath.NullOrEmpty() && query.RebuildElementEvent.NullOrEmpty());

                if (query.ReplaceNodePath.NotNullOrEmpty())
                {
                    value = value.Replace(targetRoot.Find(query.ReplaceNodePath));
                }
                if (query.ReplaceNodeEvent.NotNullOrEmpty())
                {
                    MethodInfo methodInfo = targetType.GetMethod(query.ReplaceNodeEvent, MethodsFlags);
                    value = (BaseNode)methodInfo.Invoke(default, new object[] { value });
                }

                return value;
            }

            QueryAttribute queryRoot = default;
            foreach (FieldInfo field in targetType.GetFields(FieldsFlags))
            {
                QueryAttribute query = field.GetCustomAttribute<QueryAttribute>();
                if (query is null) continue;
                if (query.StartRoot) queryRoot = query;

                BaseNode node = ResolveElement(targetType, queryRoot, query, !query.Nullable);
                if (node is not null && !queryMetadata.ContainsKey(node)) queryMetadata.Add(node, new QueryMetadata(field, queryRoot, query));

                if (query.EndRoot) queryRoot = default;

                if (node is not null && typeof(ISubElement).IsAssignableFrom(field.FieldType)) Initialize(field.FieldType, node, throwException);
            }
        }
        #endregion
    }
}
