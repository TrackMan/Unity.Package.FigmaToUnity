using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Trackman;

namespace Figma
{
    using global;
    using Attributes;

    class NodeMetadata
    {
        #region Containers
        record RootMetadata(bool filter, UxmlAttribute uxml, UxmlDownloadImages downloadImages);

        // ReSharper disable once NotAccessedPositionalProperty.Global
        record QueryMetadata(Type fieldType, QueryAttribute query);

        record BaseNodeMetadata(RootMetadata root, QueryMetadata query);
        #endregion

        #region Fields
        readonly Dictionary<BaseNode, RootMetadata> rootMetadata = new();
        readonly Dictionary<BaseNode, QueryMetadata> queryMetadata = new();
        readonly List<BaseNode> search = new(256);
        #endregion

        #region Properties
        static BindingFlags FieldsFlags => BindingFlags.NonPublic | BindingFlags.Instance;
        #endregion

        #region Constructors
        internal NodeMetadata(DocumentNode documentNode, IEnumerable<Type> elements, bool filter, bool throwExceptions = true, bool silent = false)
        {
            void InitializeRootElement(Type elementType)
            {
                void InitializeElement(Type type, BaseNode rootNode)
                {
                    BaseNode FindNodeByQuery(QueryAttribute queryRoot, QueryAttribute query, bool throwException) =>
                        queryRoot is not null && !ReferenceEquals(queryRoot, query) && Find(rootNode, queryRoot.Path, throwException, silent) is { } queryRootNode ? Find(queryRootNode, query.Path, throwException, silent) : Find(rootNode, query.Path, throwException, silent);

                    QueryAttribute queryRoot = default;
                    foreach (FieldInfo field in type.GetFields(FieldsFlags))
                    {
                        Type fieldType = field.FieldType;
                        QueryAttribute query = field.GetCustomAttribute<QueryAttribute>();
                        if (query is null) continue;

                        if (query.StartRoot) queryRoot = query;

                        BaseNode node = FindNodeByQuery(queryRoot, query, throwExceptions && !query.Nullable && query.ReplaceElementPath.NullOrEmpty() && query.RebuildElementEvent.NullOrEmpty());
                        if (node is not null && !queryMetadata.ContainsKey(node)) queryMetadata.Add(node, new QueryMetadata(fieldType, query));
                        if (query.EndRoot) queryRoot = default;
                        if (node is not null && typeof(ISubElement).IsAssignableFrom(fieldType)) InitializeElement(fieldType, node);
                    }
                }

                UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();
                if (uxml is null) return;

                BaseNode elementRoot = Find(documentNode, uxml.Root);
                BaseNode[] elementPreserve = uxml.Preserve.Select(x => Find(documentNode, x)).ToArray();

                rootMetadata.Add(elementRoot, new RootMetadata(filter, uxml, uxml.ImageFiltering));
                foreach (BaseNode value in elementPreserve)
                    if (!rootMetadata.ContainsKey(value))
                        rootMetadata.Add(value, new RootMetadata(filter, uxml, UxmlDownloadImages.Everything));

                InitializeElement(elementType, elementRoot);
            }

            foreach (Type value in elements) InitializeRootElement(value);
        }
        #endregion

        #region Methods
        internal bool EnabledInHierarchy(BaseNode node) => !rootMetadata.Any(x => x.Value.filter) || GetMetadata(node).root is not null;
        internal bool ShouldDownload(BaseNode node, UxmlDownloadImages flag)
        {
            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is null || !metadata.root.filter) return true;

            bool shouldDownload = metadata.root.downloadImages == UxmlDownloadImages.Everything || metadata.root.downloadImages.HasFlag(flag);
            if (!metadata.root.downloadImages.HasFlag(UxmlDownloadImages.ByElements)) return shouldDownload;
            if (metadata.query is null) return shouldDownload;
            if (metadata.query.query.ImageFiltering == ElementDownloadImage.Download) return true;
            if (metadata.query.query.ImageFiltering == ElementDownloadImage.Ignore) return false;

            return shouldDownload;
        }
        internal (bool hash, string value) GetTemplate(BaseNode node)
        {
            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is null || !metadata.root.filter || metadata.query is null) return (false, default);
            if (metadata.query.query.Template != "Hash") return (false, metadata.query.query.Template);

            string GetFullPath(BaseNode x) => x.parent is not null ? $"{GetFullPath(x.parent)}/{x.name}" : x.name;
            return (true, $"{metadata.query.fieldType.Name}-{Hash128.Compute(GetFullPath(node))}");
        }
        internal (ElementType, string) GetElementType(BaseNode node)
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
                if (type == typeof(PopupWindow)) return ElementType.PopupWindow;

                if (typeof(VisualElement).IsAssignableFrom(type)) return ElementType.IElement;

                throw new ArgumentOutOfRangeException(type.FullName);
            }

            BaseNodeMetadata metadata = GetMetadata(node);
            return metadata.root is not null && metadata.root.filter &&
                   metadata.root.uxml.TypeIdentification == UxmlElementTypeIdentification.ByElementType && metadata.query is not null
                ? (FieldTypeToElementType(metadata.query.fieldType), metadata.query.fieldType!.FullName!.Replace("+", "."))
                : (ElementType.None, default);
        }
        #endregion

        #region Support Methods
        BaseNode Find(BaseNode value, string path, bool throwException = true, bool silent = false)
        {
            IEnumerable<BaseNode> Search(BaseNode value, string path)
            {
                bool StartsWith(string path, BaseNode value, int startIndex)
                {
                    int endIndex = startIndex + value.name.Length;
                    return path.BeginsWith(value.name, startIndex) && path.Length >= endIndex && (path.Length == endIndex || path[endIndex].IsSeparator());
                }
                int LastIndexOf(BaseNode root, BaseNode leaf, BaseNode value, string path, int startIndex = 0)
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
                void SearchIn(BaseNode value, string path, int startIndex = 0)
                {
                    static bool IsVisible(BaseNodeMixin mixin)
                    {
                        if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;

                        return mixin.parent is null || IsVisible(mixin.parent);
                    }
                    static IReadOnlyCollection<BaseNode> GetChildren(BaseNode value)
                    {
                        List<BaseNode> children = new();
                        switch (value)
                        {
                            case DocumentNode documentNode:
                                children.AddRange(documentNode.children);
                                break;

                            case ChildrenMixin childrenMixin:
                                children.AddRange(childrenMixin.children);
                                break;
                        }

                        return children;
                    }
                    IReadOnlyCollection<BaseNode> children = GetChildren(value);

                    static bool EqualsTo(BaseNode value, string path, int startIndex) => path.EqualsTo(value.name, startIndex);

                    foreach (BaseNode child in children.Where(IsVisible))
                        if (child.name.NotNullOrEmpty() && EqualsTo(child, path, startIndex))
                            search.Add(child);

                    foreach (BaseNode child in children.Where(IsVisible))
                        if (child.name.NotNullOrEmpty() && StartsWith(path, child, startIndex))
                            SearchIn(child, path, startIndex + child.name.Length + 1);
                }
                void SearchByFullPath(BaseNode value, string path, int startIndex = 0)
                {
                    static bool IsVisible(BaseNodeMixin mixin)
                    {
                        if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;

                        return mixin.parent is null || IsVisible(mixin.parent);
                    }
                    static IReadOnlyCollection<BaseNode> GetChildren(BaseNode value)
                    {
                        List<BaseNode> children = new();
                        if (value is DocumentNode documentNode) children.AddRange(documentNode.children);
                        else if (value is ChildrenMixin childrenMixin) children.AddRange(childrenMixin.children);
                        else return children;

                        return children;
                    }
                    IReadOnlyCollection<BaseNode> children = GetChildren(value);

                    bool EqualsToFullPath(BaseNode root, BaseNode value, string path, int startIndex) => LastIndexOf(root, value, value, path, startIndex) == path.Length;
                    bool StartsWithFullPath(BaseNode root, BaseNode value, string path, int startIndex)
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

                foreach (BaseNode result in search.OfType<BaseNode>()) yield return result;
            }
            string GetFullPath(BaseNode node) => node.parent is not null ? $"{GetFullPath(node.parent)}/{node.name}" : node.name;

            BaseNode result = Search(value, path).FirstOrDefault();
            if (result is not null)
                return result;

            if (throwException)
                throw new Exception($"Cannot find node at [<color=yellow>{GetFullPath(value)}/{path}</color>]");

            if (!silent) Debug.LogWarning($"Cannot find node at [<color=yellow>{GetFullPath(value)}/{path}</color>]");
            return default;
        }
        BaseNode FindRoot(BaseNode value)
        {
            try
            {
                return rootMetadata.ContainsKey(value) ? value : value.parent is not null ? FindRoot(value.parent) : default;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                throw;
            }
        }
        BaseNodeMetadata GetMetadata(BaseNode value)
        {
            BaseNode FindRootInChildren(BaseNode value)
            {
                if (rootMetadata.ContainsKey(value)) return value;

                switch (value)
                {
                    case DocumentNode documentNode:
                        foreach (CanvasNode child in documentNode.children)
                        {
                            BaseNode node = FindRootInChildren(child);
                            if (node is not null) return node;
                        }

                        break;

                    case ChildrenMixin children:
                        foreach (SceneNode child in children.children)
                        {
                            BaseNode node = FindRootInChildren(child);
                            if (node is not null) return node;
                        }

                        break;
                }

                return default;
            }

            BaseNode root = FindRoot(value) ?? FindRootInChildren(value);
            return root is not null ? new BaseNodeMetadata(rootMetadata[root], queryMetadata.TryGetValue(value, out QueryMetadata metadata) ? metadata : default) : new BaseNodeMetadata(default, default);
        }
        #endregion
    }
}