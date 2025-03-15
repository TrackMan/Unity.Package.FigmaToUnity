using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma
{
    using Attributes;
    using Internals;
    using static Internals.PathExtensions;

    internal class NodeMetadata
    {
        #region Consts
        static readonly Dictionary<Type, ElementType> typeMap = new()
        {
            // Base elements
            { typeof(VisualElement), ElementType.VisualElement },
            { typeof(BindableElement), ElementType.BindableElement },

            // Utilities
            { typeof(Box), ElementType.Box },
            { typeof(TextElement), ElementType.TextElement },
            { typeof(Label), ElementType.Label },
            { typeof(Image), ElementType.Image },
            { typeof(IMGUIContainer), ElementType.IMGUIContainer },
            { typeof(Foldout), ElementType.Foldout },

            // Controls
            { typeof(Button), ElementType.Button },
            { typeof(RepeatButton), ElementType.RepeatButton },
            { typeof(Toggle), ElementType.Toggle },
            { typeof(Scroller), ElementType.Scroller },
            { typeof(Slider), ElementType.Slider },
            { typeof(SliderInt), ElementType.SliderInt },
            { typeof(MinMaxSlider), ElementType.MinMaxSlider },
            { typeof(EnumField), ElementType.EnumField },
            { typeof(MaskField), ElementType.MaskField },
            { typeof(LayerField), ElementType.LayerField },
            { typeof(LayerMaskField), ElementType.LayerMaskField },
            { typeof(TagField), ElementType.TagField },
            { typeof(ProgressBar), ElementType.ProgressBar },

            // Text input
            { typeof(TextField), ElementType.TextField },
            { typeof(IntegerField), ElementType.IntegerField },
            { typeof(LongField), ElementType.LongField },
            { typeof(FloatField), ElementType.FloatField },
            { typeof(DoubleField), ElementType.DoubleField },
            { typeof(Vector2Field), ElementType.Vector2Field },
            { typeof(Vector2IntField), ElementType.Vector2IntField },
            { typeof(Vector3Field), ElementType.Vector3Field },
            { typeof(Vector3IntField), ElementType.Vector3IntField },
            { typeof(Vector4Field), ElementType.Vector4Field },
            { typeof(RectField), ElementType.RectField },
            { typeof(RectIntField), ElementType.RectIntField },
            { typeof(BoundsField), ElementType.BoundsField },
            { typeof(BoundsIntField), ElementType.BoundsIntField },

            // Complex widgets
            { typeof(PropertyField), ElementType.PropertyField },
            { typeof(ColorField), ElementType.ColorField },
            { typeof(CurveField), ElementType.CurveField },
            { typeof(GradientField), ElementType.GradientField },
            { typeof(ObjectField), ElementType.ObjectField },

            // Toolbar
            { typeof(Toolbar), ElementType.Toolbar },
            { typeof(ToolbarButton), ElementType.ToolbarButton },
            { typeof(ToolbarToggle), ElementType.ToolbarToggle },
            { typeof(ToolbarMenu), ElementType.ToolbarMenu },
            { typeof(ToolbarSearchField), ElementType.ToolbarSearchField },
            { typeof(ToolbarPopupSearchField), ElementType.ToolbarPopupSearchField },
            { typeof(ToolbarSpacer), ElementType.ToolbarSpacer },

            // Views and windows
            { typeof(ListView), ElementType.ListView },
            { typeof(ScrollView), ElementType.ScrollView },
            { typeof(PopupWindow), ElementType.PopupWindow }
        };
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
                        queryRoot != null && !ReferenceEquals(queryRoot, query) && Find(rootNode, queryRoot.Path, throwException, silent) is { } queryRootNode
                            ? Find(queryRootNode, query.Path, throwException, silent)
                            : Find(rootNode, query.Path, throwException, silent);

                    QueryAttribute queryRoot = null;

                    foreach (FieldInfo field in type.GetFields(FieldsFlags))
                    {
                        Type fieldType = field.FieldType;
                        QueryAttribute query = field.GetCustomAttribute<QueryAttribute>();

                        if (query is null)
                            continue;

                        if (query.StartRoot)
                            queryRoot = query;

                        BaseNode node = FindNodeByQuery(queryRoot, query, throwExceptions &&
                                                                          !query.Nullable &&
                                                                          query.ReplaceElementPath.NullOrEmpty() &&
                                                                          query.RebuildElementEvent.NullOrEmpty());

                        if (node != null && !queryMetadata.ContainsKey(node))
                            queryMetadata.Add(node, new QueryMetadata(fieldType, query));
                        if (query.EndRoot)
                            queryRoot = null;
                        if (node != null && typeof(ISubElement).IsAssignableFrom(fieldType))
                            InitializeElement(fieldType, node);
                    }
                }

                UxmlAttribute uxml = elementType.GetCustomAttribute<UxmlAttribute>();
                if (uxml is null)
                    return;

                BaseNode elementRoot = Find(documentNode, uxml.Root);
                BaseNode[] elementPreserve = uxml.Preserve.Select(x => Find(documentNode, x)).ToArray();

                rootMetadata.Add(elementRoot, new RootMetadata(filter, uxml, uxml.DownloadImages));

                foreach (BaseNode value in elementPreserve.Where(x => !rootMetadata.ContainsKey(x)))
                    rootMetadata.Add(value, new RootMetadata(filter, uxml, UxmlDownloadImages.Everything));

                InitializeElement(elementType, elementRoot);
            }

            elements.ForEach(InitializeRootElement);
        }
        #endregion

        #region Methods
        internal bool EnabledInHierarchy(BaseNode node) => !rootMetadata.Any(x => x.Value.filter) || GetMetadata(node).root != null;
        internal bool ShouldDownload(BaseNode node, UxmlDownloadImages flag)
        {
            BaseNodeMetadata metadata = GetMetadata(node);
            if (metadata.root is null || !metadata.root.filter)
                return true;

            bool shouldDownload = metadata.root.downloadImages == UxmlDownloadImages.Everything || metadata.root.downloadImages.HasFlag(flag);

            if (!metadata.root.downloadImages.HasFlag(UxmlDownloadImages.ByElements) || metadata.query is null)
                return shouldDownload;

            return metadata.query.query.DownloadImage switch
            {
                ElementDownloadImage.Download => true,
                ElementDownloadImage.Ignore => false,
                _ => shouldDownload
            };
        }
        internal (bool isHash, string templateName) GetTemplate(BaseNode node)
        {
            string GetFullPath(BaseNode x) => x.parent != null ? CombinePath(GetFullPath(x.parent), x.name) : x.name;

            BaseNodeMetadata metadata = GetMetadata(node);

            if (metadata.root is null || !metadata.root.filter || metadata.query is null)
                return (false, null);

            return !metadata.query.query.Hash ? (false, metadata.query.query.Template) : (true, $"{metadata.query.fieldType.Name}-{Hash128.Compute(GetFullPath(node))}");
        }
        internal (ElementType, string) GetElementType(BaseNode node)
        {
            ElementType FieldTypeToElementType(Type type) => typeMap.TryGetValue(type, out ElementType elementType)
                ? elementType
                : typeof(VisualElement).IsAssignableFrom(type)
                    ? ElementType.IElement
                    : throw new ArgumentOutOfRangeException(type.FullName);

            BaseNodeMetadata metadata = GetMetadata(node);
            return metadata.root != null && metadata.root.filter && metadata.root.uxml.TypeIdentification == UxmlElementTypeIdentification.ByElementType && metadata.query != null
                ? (FieldTypeToElementType(metadata.query.fieldType), metadata.query.fieldType!.FullName!.Replace("+", "."))
                : (ElementType.None, null);
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
                    if (value.parent != null && value.parent != root)
                        startIndex = LastIndexOf(root, leaf, value.parent, path, startIndex);

                    if (startIndex < 0 || !StartsWith(path, value, startIndex))
                        return -1;

                    int endIndex = startIndex + value.name.Length;
                    if (path.Length > endIndex && path[endIndex].IsSeparator() && value != leaf)
                        endIndex++;

                    return endIndex;
                }
                void SearchIn(BaseNode value, string path, int startIndex = 0)
                {
                    static bool IsVisible(IBaseNodeMixin mixin)
                    {
                        if (mixin is ISceneNodeMixin scene && scene.visible.HasValueAndFalse())
                            return false;

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

                            case IChildrenMixin childrenMixin:
                                children.AddRange(childrenMixin.children);
                                break;
                        }

                        return children;
                    }
                    static bool EqualsTo(BaseNode value, string path, int startIndex) => path.EqualsTo(value.name, startIndex);

                    IReadOnlyCollection<BaseNode> children = GetChildren(value);

                    search.AddRange(children.Where(child => IsVisible(child) && child.name.NotNullOrEmpty() && EqualsTo(child, path, startIndex)));
                    children.Where(child => IsVisible(child) && child.name.NotNullOrEmpty() && StartsWith(path, child, startIndex)).ForEach(child => SearchIn(child, path, startIndex + child.name.Length + 1));
                }
                void SearchByFullPath(BaseNode value, string path, int startIndex = 0)
                {
                    static bool IsVisible(IBaseNodeMixin mixin)
                    {
                        if (mixin is ISceneNodeMixin scene && scene.visible.HasValueAndFalse())
                            return false;

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

                            case IChildrenMixin childrenMixin:
                                children.AddRange(childrenMixin.children);
                                break;
                        }

                        return children;
                    }
                    IReadOnlyCollection<BaseNode> children = GetChildren(value);

                    bool EqualsToFullPath(BaseNode root, BaseNode value, string path, int startIndex) => LastIndexOf(root, value, value, path, startIndex) == path.Length;
                    bool StartsWithFullPath(BaseNode root, BaseNode value, string path, int startIndex)
                    {
                        int endIndex = LastIndexOf(root, value, value, path, startIndex);
                        return endIndex >= 0 && path.Length > endIndex && path[endIndex].IsSeparator();
                    }

                    search.AddRange(children.Where(child => IsVisible(child) && child.name.NotNullOrEmpty() && EqualsToFullPath(value, child, path, startIndex)));

                    foreach (BaseNode child in children.Where(child => IsVisible(child) && child.name.NotNullOrEmpty() && StartsWithFullPath(value, child, path, startIndex)))
                        SearchByFullPath(child, path, startIndex + child.name.Length + 1);
                }

                search.Clear();

                BaseNode root = FindRoot(value);
                if (root != null)
                {
                    UxmlAttribute uxml = rootMetadata[root].uxml;
                    if (path.BeginsWith(uxml.DocumentRoot) || uxml.DocumentPreserve.Any(x => path.BeginsWith(x)))
                        SearchByFullPath(root.parent.parent, path, UxmlAttribute.prefix.Length + 1);
                    else
                        SearchIn(value, path);
                }
                else
                    SearchByFullPath(value, path);

                foreach (BaseNode result in search.OfType<BaseNode>())
                    yield return result;
            }

            string GetFullPath(BaseNode node) => node.parent != null ? CombinePath(GetFullPath(node.parent), node.name) : node.name;

            BaseNode result = Search(value, path).FirstOrDefault();

            if (result != null)
                return result;

            if (throwException)
                throw new Exception(Extensions.BuildTargetMessage("Cannot find node at", CombinePath(GetFullPath(value), path)));

            if (!silent)
                Debug.LogWarning(Extensions.BuildTargetMessage("Cannot find node at", CombinePath(GetFullPath(value), path)));

            return null;
        }
        BaseNode FindRoot(BaseNode value)
        {
            try
            {
                while (value != null)
                {
                    if (rootMetadata.ContainsKey(value))
                        return value;
                    value = value.parent;
                }

                return null;
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
                if (rootMetadata.ContainsKey(value))
                    return value;

                switch (value)
                {
                    case DocumentNode documentNode:
                        foreach (CanvasNode child in documentNode.children)
                        {
                            BaseNode node = FindRootInChildren(child);
                            if (node != null)
                                return node;
                        }

                        break;

                    case IChildrenMixin children:
                        foreach (SceneNode child in children.children)
                        {
                            BaseNode node = FindRootInChildren(child);
                            if (node != null)
                                return node;
                        }

                        break;
                }

                return null;
            }

            BaseNode root = FindRoot(value) ?? FindRootInChildren(value);
            return root != null ? new BaseNodeMetadata(rootMetadata[root], queryMetadata.GetValueOrDefault(value)) : new BaseNodeMetadata(null, null);
        }
        #endregion
    }
}