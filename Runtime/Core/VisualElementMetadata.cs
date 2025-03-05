using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Figma
{
    using Attributes;
    using Internals;
    using static Internals.PathExtensions;

    public static class VisualElementMetadata
    {
        const int initialCollectionCapacity = 256;

        record Metadata(UIDocument document, UxmlAttribute uxml, string path);

        #region Fields
        static readonly Dictionary<VisualElement, Metadata> rootMetadata = new(initialCollectionCapacity);
        static readonly List<VisualElement> search = new(initialCollectionCapacity);
        static readonly Dictionary<VisualElement, string> cloneMap = new(initialCollectionCapacity);
        static readonly List<VisualElement> hide = new(initialCollectionCapacity);
        #endregion

        #region Properties
        public static BindingFlags FieldsFlags => BindingFlags.NonPublic | BindingFlags.Instance;
        public static BindingFlags MethodsFlags => BindingFlags.NonPublic | BindingFlags.Instance;
        #endregion

        #region Callbacks
        public static Action<VisualElement, UIDocument, UxmlAttribute> OnInitializeRoot { get; set; }
        public static Action<VisualElement, object, Type, FieldInfo, QueryAttribute, QueryAttribute> OnInitializeElement { get; set; }
        public static Action<VisualElement> OnRebuildElement { get; set; }
        #endregion

        #region Methods
        public static void Initialize(UIDocument document, IEnumerable<IRootElement> targets) => targets.Where(x => x != null).ForEach(x => Initialize(document, x));
        public static void Initialize(UIDocument document, IRootElement target)
        {
            Type targetType = target.GetType();
            UxmlAttribute uxml = targetType.GetCustomAttribute<UxmlAttribute>();
            VisualElement targetRoot = document.rootVisualElement.Find(uxml.DocumentRoot, throwException: false, silent: false);

            if (targetRoot == null)
                return;

            rootMetadata.Add(targetRoot, new Metadata(document, uxml, uxml.DocumentRoot));

            (string path, VisualElement element)[] rootsPreserved = uxml.DocumentPreserve.Select(x => (x, document.rootVisualElement.Find(x, throwException: false, silent: false))).ToArray();

            rootsPreserved.Where(x => !rootMetadata.ContainsKey(x.element))
                          .ForEach(x => rootMetadata.Add(x.element, new Metadata(document, uxml, x.path)));

            OnInitializeRoot?.Invoke(targetRoot, document, uxml);
            Initialize(target, targetType, targetRoot);
            target.OnInitialize(targetRoot, rootsPreserved.Select(x => x.element).ToArray());
        }
        public static void Initialize(ISubElement target, VisualElement targetRoot)
        {
            Initialize(target, target.GetType(), targetRoot);
            target.OnInitialize();

            if (target.GetType().GetCustomAttribute<QueryAttribute>() is { } queryAttribute)
                OnInitializeElement?.Invoke(targetRoot, null, null, null, null, queryAttribute);
        }
        public static void Rebuild(IEnumerable<IRootElement> targets)
        {
            foreach (IRootElement target in targets.Where(x => x.Root != null))
            {
                target.OnRebuild();
                target.Root.Children().ForEach(Rebuild);
            }
        }
        public static void Rebuild(VisualElement target)
        {
            if (target is ISubElement targetSubElement)
                targetSubElement.OnRebuild();

            target.Children().ForEach(Rebuild);

            OnRebuildElement?.Invoke(target);

            if (hide.Contains(target))
                target.Hide();
        }

        public static IEnumerable<T> Search<T>(this VisualElement value, string path, string className = null) where T : VisualElement
        {
            static bool StartsWith(string path, VisualElement value, int startIndex)
            {
                int endIndex = startIndex + value.name.Length;
                return path.BeginsWith(value.name, startIndex) && path.Length >= endIndex && (path.Length == endIndex || path[endIndex].IsSeparator());
            }
            static int LastIndexOf(VisualElement root, VisualElement leaf, VisualElement value, string path, int startIndex = 0)
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
            static void SearchIn(VisualElement value, string path, int startIndex = 0, string className = null)
            {
                static bool EqualsTo(VisualElement value, string path, int startIndex) => path.EqualsTo(value.name, startIndex);

                search.AddRange(value.Children().Where(child => child.name.NotNullOrEmpty() && EqualsTo(child, path, startIndex) && (className.NullOrEmpty() || child.ClassListContains(className))));

                value.Children()
                     .Where(child => child.name.NotNullOrEmpty() && StartsWith(path, child, startIndex))
                     .ForEach(child => SearchIn(child, path, startIndex + child.name.Length + 1, className));
            }
            static void SearchByFullPath(VisualElement value, string path, int startIndex = 0, string className = null)
            {
                static bool EqualsToFullPath(VisualElement root, VisualElement value, string path, int startIndex) => LastIndexOf(root, value, value, path, startIndex) == path.Length;
                static bool StartsWithFullPath(VisualElement root, VisualElement value, string path, int startIndex)
                {
                    int endIndex = LastIndexOf(root, value, value, path, startIndex);
                    return endIndex >= 0 && path.Length > endIndex && path[endIndex].IsSeparator();
                }

                search.AddRange(value.Children().Where(child => child.name.NotNullOrEmpty() && EqualsToFullPath(value, child, path, startIndex) && (className.NullOrEmpty() || child.ClassListContains(className))));

                value.Children()
                     .Where(child => child.name.NotNullOrEmpty() && StartsWithFullPath(value, child, path, startIndex))
                     .ForEach(child => SearchByFullPath(child, path, startIndex + child.name.Length + 1, className));
            }

            search.Clear();

            VisualElement root = FindRoot(value);

            if (root != null)
            {
                UxmlAttribute uxml = rootMetadata[root].uxml;

                if (path.BeginsWith(uxml.DocumentRoot) || uxml.DocumentPreserve.Any(x => path.BeginsWith(x)))
                    SearchByFullPath(root.parent.parent.parent, path, 0, className);
                else
                    SearchIn(value, path, 0, className);
            }
            else
            {
                SearchByFullPath(value, path, 0, className);
            }

            foreach (T result in search.OfType<T>())
                yield return result;
        }
        public static void Dispose()
        {
            VisualElementExtensions.cloneDictionary.Clear();
            rootMetadata.Clear();
            cloneMap.Clear();
            search.Clear();
            hide.Clear();
        }
        public static VisualElement FindByPath(this VisualElement root, string path)
        {
            foreach (VisualElement child in root.Children())
            {
                VisualElement result = FindByPathRecursive(child, path);
                if (result != null) return result;
            }

            return null;
        }
        static VisualElement FindByPathRecursive(this VisualElement root, string path, string subPath = "")
        {
            if (root == null)
                return null;

            subPath = !string.IsNullOrEmpty(subPath) ? CombinePath(subPath, root.name) : root.name;

            if (path == subPath)
                return root;

            if (!path.StartsWith(subPath + pathSeparator) && path != subPath)
                return null;

            foreach (VisualElement child in root.Children())
            {
                VisualElement result = FindByPathRecursive(child, path, subPath);
                if (result != null)
                    return result;
            }

            switch (root)
            {
                case TemplateContainer { contentContainer: not null } templateContainer:
                {
                    foreach (VisualElement child in templateContainer.Children().First().Children())
                    {
                        VisualElement result = FindByPathRecursive(child, path, subPath);
                        if (result != null)
                            return result;
                    }

                    break;
                }

                case ScrollView view:
                {
                    foreach (VisualElement child in view.contentContainer.Children())
                    {
                        VisualElement result = FindByPathRecursive(child, path, subPath);
                        if (result != null)
                            return result;
                    }

                    break;
                }
            }

            return null;
        }
        public static T Find<T>(this VisualElement value, string path, bool throwException = true, bool silent = false) where T : VisualElement
        {
            path = path.Replace('\\', pathSeparator);

            T result = value.FindByPath(path).As<T>();

            if (result == null && value is TemplateContainer)
                result = value.Children().First().FindByPath(path).As<T>();

            if (result != null)
                return result;

            if (throwException)
                throw new KeyNotFoundException(Extensions.BuildTargetMessage($"Cannot find {typeof(T).Name}", path));

            if (!silent)
                Debug.LogWarning(Extensions.BuildTargetMessage($"[{nameof(VisualElementMetadata)}] Cannot find {typeof(T).Name}", path));

            return null;
        }
        public static (T1, T2) Find<T1, T2>(this VisualElement value, string path1, string path2, bool throwException = true, bool silent = false) where T1 : VisualElement where T2 : VisualElement => (value.Find<T1>(path1, throwException: throwException, silent: silent), value.Find<T2>(path2, throwException: throwException, silent: silent));
        public static (T1, T2, T3) Find<T1, T2, T3>(this VisualElement value, string path1, string path2, string path3, bool throwException = true, bool silent = false) where T1 : VisualElement where T2 : VisualElement where T3 : VisualElement => (value.Find<T1>(path1, throwException: throwException, silent: silent), value.Find<T2>(path2, throwException: throwException, silent: silent), value.Find<T3>(path3, throwException: throwException, silent: silent));
        public static VisualElement Find(this VisualElement value, string path, bool throwException = true, bool silent = true) => Find<VisualElement>(value, path, throwException, silent);

        public static T Clone<T>(this T value, VisualElement parent = null, int index = -1) where T : VisualElement
        {
            (TemplateContainer, string) GetNearestTemplate(VisualElement value, string path = "")
            {
                while (true)
                {
                    if (value is TemplateContainer template)
                        return (template, path);

                    if (value.parent is null)
                        return (null, string.Empty);

                    string subPath = CombinePath(value.name, path);
                    value = value.parent;
                    path = subPath;
                }
            }

            parent ??= value.parent;

            (VisualElement root, string pathToValue) = FindRoot(value, string.Empty);
            Metadata metadata = rootMetadata[root];

            VisualElement temporaryContainer = new();

            try
            {
                T elementClone;

                if (cloneMap.ContainsKey(value) &&
                    cloneMap[value] is { } template &&
                    metadata.document.visualTreeAsset.templateDependencies.FirstOrDefault(x => x.name == template) is { } treeAsset && treeAsset)
                {
                    treeAsset.CloneTree(temporaryContainer);
                    elementClone = (T)temporaryContainer[0];
                }
                else
                {
                    (TemplateContainer nearestTemplate, string templatePath) = GetNearestTemplate(value);

                    if (nearestTemplate != null)
                    {
                        string dependencyName = value.GetType().Name;
                        VisualTreeAsset asset = nearestTemplate.templateSource.templateDependencies.FirstOrDefault(x => x.name == dependencyName);

                        if (asset == null && cloneMap.ContainsKey(value) && cloneMap[value] is { } templateNameFallback)
                            asset = nearestTemplate.templateSource.templateDependencies.FirstOrDefault(x => x.name == templateNameFallback);

                        if (asset != null)
                        {
                            elementClone = (T)Activator.CreateInstance(value.GetType());
                            asset.CloneTree(elementClone.contentContainer);
                            elementClone.CopyStyle(value);
                            value.GetClasses().ForEach(x => elementClone.AddToClassList(x));
                        }
                        else
                        {
                            nearestTemplate.templateSource.CloneTree(temporaryContainer);
                            elementClone = temporaryContainer.Find<T>(pathToValue, false) ?? temporaryContainer.Find<T>(templatePath);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(VisualElementMetadata)}] Cloning directly {value.GetType().Name}");
                        metadata.document.visualTreeAsset.CloneTree(temporaryContainer);
                        elementClone = temporaryContainer.Find(metadata.path).Find<T>(pathToValue);
                    }
                }

                elementClone.RemoveFromHierarchy();

                parent.Add(elementClone);

                if (value.parent == elementClone.parent)
                    elementClone.PlaceBehind(value);
                if (index >= 0)
                    elementClone.name = $"{value.name} {nameof(VisualElement)}:{index}";

                parent.MarkDirtyRepaint();

                if (elementClone is ISubElement subElement)
                {
                    Initialize(subElement, elementClone);
                    Rebuild(elementClone);
                }

                elementClone.MarginMe();

                return elementClone;
            }
            catch (Exception exception)
            {
                throw new ArgumentException(Extensions.BuildTargetMessage($"Cannot clone {typeof(T).Name}", value.name), exception);
            }
            finally
            {
                temporaryContainer.RemoveFromHierarchy();
                temporaryContainer.Clear();
                temporaryContainer.MarkDirtyRepaint();
            }
        }
        public static VisualElement Clone(this VisualElement value, VisualElement parent = null, int index = -1) => Clone<VisualElement>(value, parent, index);

        public static T Replace<T>(this VisualElement value, VisualElement prefab) where T : VisualElement
        {
            VisualElement parent = value.parent;
            T elementClone = (T)prefab.Clone(parent);

            if (value.resolvedStyle.position == Position.Relative)
            {
                elementClone.style.position = value.style.position;
                elementClone.style.left = value.style.left;
                elementClone.style.top = value.style.top;
                elementClone.style.bottom = value.style.bottom;
                elementClone.style.right = value.style.right;
            }
            else
            {
                elementClone.style.alignItems = value.resolvedStyle.alignItems;
                elementClone.style.alignContent = value.resolvedStyle.alignContent;
                elementClone.style.justifyContent = value.resolvedStyle.justifyContent;
                elementClone.style.flexGrow = value.resolvedStyle.flexGrow;
                elementClone.style.flexShrink = value.resolvedStyle.flexShrink;
                elementClone.style.flexDirection = value.resolvedStyle.flexDirection;
                elementClone.style.flexWrap = value.resolvedStyle.flexWrap;

                elementClone.style.position = value.resolvedStyle.position;
                elementClone.style.left = value.resolvedStyle.left;
                elementClone.style.top = value.resolvedStyle.top;
                elementClone.style.bottom = value.resolvedStyle.bottom;
                elementClone.style.right = value.resolvedStyle.right;
            }

            elementClone.style.alignSelf = value.resolvedStyle.alignSelf;
            elementClone.name = value.name;

            elementClone.RemoveFromHierarchy();
            parent.Insert(parent.IndexOf(value), elementClone);
            parent.MarkDirtyRepaint();

            value.RemoveFromHierarchy();
            value.Clear();
            value.MarkDirtyRepaint();

            return elementClone;
        }
        public static VisualElement Replace(this VisualElement value, VisualElement prefab) => Replace<VisualElement>(value, prefab);

        public static void CopyStyleList(this VisualElement value, VisualElement source)
        {
            value.ClearClassList();
            source.GetClasses().ForEach(value.AddToClassList);
        }
        public static void CopyResolvedStyle(this VisualElement value, VisualElement source, CopyStyleMask copyMask = CopyStyleMask.All)
        {
            IStyle style = value.style;
            IResolvedStyle valueResolvedStyle = value.resolvedStyle;
            IResolvedStyle sourceResolvedStyle = source.resolvedStyle;

            if (copyMask.HasFlag(CopyStyleMask.Position))
            {
                style.position = sourceResolvedStyle.position;
                style.left = sourceResolvedStyle.left;
                style.right = sourceResolvedStyle.right;
                style.top = sourceResolvedStyle.top;
                style.bottom = sourceResolvedStyle.bottom;
                style.scale = sourceResolvedStyle.scale;
                style.rotate = sourceResolvedStyle.rotate;
            }

            if (copyMask.HasFlag(CopyStyleMask.Size))
            {
                style.width = sourceResolvedStyle.width;
                style.height = sourceResolvedStyle.height;
            }

            if (copyMask.HasFlag(CopyStyleMask.Flex))
            {
                if (valueResolvedStyle.justifyContent != sourceResolvedStyle.justifyContent) style.justifyContent = sourceResolvedStyle.justifyContent;
                if (valueResolvedStyle.alignSelf != sourceResolvedStyle.alignSelf) style.alignSelf = sourceResolvedStyle.alignSelf;
                if (valueResolvedStyle.alignItems != sourceResolvedStyle.alignItems) style.alignItems = sourceResolvedStyle.alignItems;
                if (valueResolvedStyle.alignContent != sourceResolvedStyle.alignContent) style.alignContent = sourceResolvedStyle.alignContent;

                if (valueResolvedStyle.flexWrap != sourceResolvedStyle.flexWrap) style.flexWrap = sourceResolvedStyle.flexWrap;
                if (valueResolvedStyle.flexShrink != sourceResolvedStyle.flexShrink) style.flexShrink = sourceResolvedStyle.flexShrink;
                if (valueResolvedStyle.flexGrow != sourceResolvedStyle.flexGrow) style.flexGrow = sourceResolvedStyle.flexGrow;
                if (valueResolvedStyle.flexDirection != sourceResolvedStyle.flexDirection) style.flexDirection = sourceResolvedStyle.flexDirection;
                if (valueResolvedStyle.flexBasis != sourceResolvedStyle.flexBasis) style.flexBasis = sourceResolvedStyle.flexBasis.value;
            }

            if (copyMask.HasFlag(CopyStyleMask.Display))
            {
                if (valueResolvedStyle.display != sourceResolvedStyle.display) style.display = sourceResolvedStyle.display;
                if (valueResolvedStyle.opacity != sourceResolvedStyle.opacity) style.opacity = sourceResolvedStyle.opacity;
                if (valueResolvedStyle.visibility != sourceResolvedStyle.visibility) style.visibility = sourceResolvedStyle.visibility;
                if (valueResolvedStyle.unityBackgroundImageTintColor != sourceResolvedStyle.unityBackgroundImageTintColor) style.unityBackgroundImageTintColor = sourceResolvedStyle.unityBackgroundImageTintColor;
                if (valueResolvedStyle.backgroundPositionX != sourceResolvedStyle.backgroundPositionX) style.backgroundPositionX = sourceResolvedStyle.backgroundPositionX;
                if (valueResolvedStyle.backgroundPositionY != sourceResolvedStyle.backgroundPositionY) style.backgroundPositionY = sourceResolvedStyle.backgroundPositionY;
                if (valueResolvedStyle.backgroundRepeat != sourceResolvedStyle.backgroundRepeat) style.backgroundRepeat = sourceResolvedStyle.backgroundRepeat;
                if (valueResolvedStyle.backgroundSize != sourceResolvedStyle.backgroundSize) style.backgroundSize = sourceResolvedStyle.backgroundSize;
                if (valueResolvedStyle.backgroundImage != sourceResolvedStyle.backgroundImage) style.backgroundImage = sourceResolvedStyle.backgroundImage;
                if (valueResolvedStyle.backgroundColor != sourceResolvedStyle.backgroundColor) style.backgroundColor = sourceResolvedStyle.backgroundColor;
                if (valueResolvedStyle.color != sourceResolvedStyle.color) style.color = sourceResolvedStyle.color;
            }

            if (copyMask.HasFlag(CopyStyleMask.Padding))
            {
                if (valueResolvedStyle.paddingTop != sourceResolvedStyle.paddingTop) style.paddingTop = sourceResolvedStyle.paddingTop;
                if (valueResolvedStyle.paddingRight != sourceResolvedStyle.paddingRight) style.paddingRight = sourceResolvedStyle.paddingRight;
                if (valueResolvedStyle.paddingLeft != sourceResolvedStyle.paddingLeft) style.paddingLeft = sourceResolvedStyle.paddingLeft;
                if (valueResolvedStyle.paddingBottom != sourceResolvedStyle.paddingBottom) style.paddingBottom = sourceResolvedStyle.paddingBottom;
            }

            if (copyMask.HasFlag(CopyStyleMask.Margins))
            {
                if (valueResolvedStyle.marginTop != sourceResolvedStyle.marginTop) style.marginTop = sourceResolvedStyle.marginTop;
                if (valueResolvedStyle.marginRight != sourceResolvedStyle.marginRight) style.marginRight = sourceResolvedStyle.marginRight;
                if (valueResolvedStyle.marginLeft != sourceResolvedStyle.marginLeft) style.marginLeft = sourceResolvedStyle.marginLeft;
                if (valueResolvedStyle.marginBottom != sourceResolvedStyle.marginBottom) style.marginBottom = sourceResolvedStyle.marginBottom;
            }

            if (copyMask.HasFlag(CopyStyleMask.Borders))
            {
                if (valueResolvedStyle.borderTopLeftRadius != sourceResolvedStyle.borderTopLeftRadius) style.borderTopLeftRadius = sourceResolvedStyle.borderTopLeftRadius;
                if (valueResolvedStyle.borderTopColor != sourceResolvedStyle.borderTopColor) style.borderTopColor = sourceResolvedStyle.borderTopColor;
                if (valueResolvedStyle.borderRightWidth != sourceResolvedStyle.borderRightWidth) style.borderRightWidth = sourceResolvedStyle.borderRightWidth;
                if (valueResolvedStyle.borderRightColor != sourceResolvedStyle.borderRightColor) style.borderRightColor = sourceResolvedStyle.borderRightColor;
                if (valueResolvedStyle.borderLeftWidth != sourceResolvedStyle.borderLeftWidth) style.borderLeftWidth = sourceResolvedStyle.borderLeftWidth;
                if (valueResolvedStyle.borderLeftColor != sourceResolvedStyle.borderLeftColor) style.borderLeftColor = sourceResolvedStyle.borderLeftColor;
                if (valueResolvedStyle.borderTopRightRadius != sourceResolvedStyle.borderTopRightRadius) style.borderTopRightRadius = sourceResolvedStyle.borderTopRightRadius;
                if (valueResolvedStyle.borderBottomWidth != sourceResolvedStyle.borderBottomWidth) style.borderBottomWidth = sourceResolvedStyle.borderBottomWidth;
                if (valueResolvedStyle.borderBottomLeftRadius != sourceResolvedStyle.borderBottomLeftRadius) style.borderBottomLeftRadius = sourceResolvedStyle.borderBottomLeftRadius;
                if (valueResolvedStyle.borderBottomColor != sourceResolvedStyle.borderBottomColor) style.borderBottomColor = sourceResolvedStyle.borderBottomColor;
                if (valueResolvedStyle.borderBottomRightRadius != sourceResolvedStyle.borderBottomRightRadius) style.borderBottomRightRadius = sourceResolvedStyle.borderBottomRightRadius;
                if (valueResolvedStyle.borderTopWidth != sourceResolvedStyle.borderTopWidth) style.borderTopWidth = sourceResolvedStyle.borderTopWidth;
            }

            if (copyMask.HasFlag(CopyStyleMask.Slicing))
            {
                if (valueResolvedStyle.unitySliceTop != sourceResolvedStyle.unitySliceTop) style.unitySliceTop = sourceResolvedStyle.unitySliceTop;
                if (valueResolvedStyle.unitySliceRight != sourceResolvedStyle.unitySliceRight) style.unitySliceRight = sourceResolvedStyle.unitySliceRight;
                if (valueResolvedStyle.unitySliceLeft != sourceResolvedStyle.unitySliceLeft) style.unitySliceLeft = sourceResolvedStyle.unitySliceLeft;
                if (valueResolvedStyle.unitySliceBottom != sourceResolvedStyle.unitySliceBottom) style.unitySliceBottom = sourceResolvedStyle.unitySliceBottom;
            }

            if (copyMask.HasFlag(CopyStyleMask.Font))
            {
                if (valueResolvedStyle.whiteSpace != sourceResolvedStyle.whiteSpace) style.whiteSpace = sourceResolvedStyle.whiteSpace;
                if (valueResolvedStyle.wordSpacing != sourceResolvedStyle.wordSpacing) style.wordSpacing = sourceResolvedStyle.wordSpacing;
                if (valueResolvedStyle.letterSpacing != sourceResolvedStyle.letterSpacing) style.letterSpacing = sourceResolvedStyle.letterSpacing;
                if (valueResolvedStyle.textOverflow != sourceResolvedStyle.textOverflow) style.textOverflow = sourceResolvedStyle.textOverflow;
                if (valueResolvedStyle.fontSize != sourceResolvedStyle.fontSize) style.fontSize = sourceResolvedStyle.fontSize;
                if (valueResolvedStyle.unityFont != sourceResolvedStyle.unityFont) style.unityFont = new StyleFont { value = sourceResolvedStyle.unityFont };
                if (valueResolvedStyle.unityFontDefinition != sourceResolvedStyle.unityFontDefinition) style.unityFontDefinition = sourceResolvedStyle.unityFontDefinition;
                if (valueResolvedStyle.unityParagraphSpacing != sourceResolvedStyle.unityParagraphSpacing) style.unityParagraphSpacing = sourceResolvedStyle.unityParagraphSpacing;
                if (valueResolvedStyle.unityTextAlign != sourceResolvedStyle.unityTextAlign) style.unityTextAlign = sourceResolvedStyle.unityTextAlign;
                if (valueResolvedStyle.unityTextOverflowPosition != sourceResolvedStyle.unityTextOverflowPosition) style.unityTextOverflowPosition = sourceResolvedStyle.unityTextOverflowPosition;
                if (valueResolvedStyle.unityTextOutlineWidth != sourceResolvedStyle.unityTextOutlineWidth) style.unityTextOutlineWidth = sourceResolvedStyle.unityTextOutlineWidth;
                if (valueResolvedStyle.unityTextOutlineColor != sourceResolvedStyle.unityTextOutlineColor) style.unityTextOutlineColor = sourceResolvedStyle.unityTextOutlineColor;
                if (valueResolvedStyle.unityFontStyleAndWeight != sourceResolvedStyle.unityFontStyleAndWeight) style.unityFontStyleAndWeight = sourceResolvedStyle.unityFontStyleAndWeight;
            }
        }
        public static void CopyStyle(this VisualElement value, VisualElement source, CopyStyleMask copyMask = CopyStyleMask.All)
        {
            IStyle valueStyle = value.style;
            IStyle sourceStyle = source.style;

            if (copyMask.HasFlag(CopyStyleMask.Position))
            {
                if (sourceStyle.position.keyword != StyleKeyword.Null && valueStyle.position != sourceStyle.position) valueStyle.position = sourceStyle.position;
                if (sourceStyle.top.keyword != StyleKeyword.Null && valueStyle.top != sourceStyle.top) valueStyle.top = sourceStyle.top;
                if (sourceStyle.bottom.keyword != StyleKeyword.Null && valueStyle.bottom != sourceStyle.bottom) valueStyle.bottom = sourceStyle.bottom;
                if (sourceStyle.left.keyword != StyleKeyword.Null && valueStyle.left != sourceStyle.left) valueStyle.left = sourceStyle.left;
                if (sourceStyle.right.keyword != StyleKeyword.Null && valueStyle.right != sourceStyle.right) valueStyle.right = sourceStyle.right;
                if (sourceStyle.translate.keyword != StyleKeyword.Null && valueStyle.translate != sourceStyle.translate) valueStyle.translate = valueStyle.translate = sourceStyle.translate;
                if (sourceStyle.rotate.keyword != StyleKeyword.Null && valueStyle.rotate != sourceStyle.rotate) valueStyle.rotate = sourceStyle.rotate;
                if (sourceStyle.scale.keyword != StyleKeyword.Null && valueStyle.scale != sourceStyle.scale) valueStyle.scale = sourceStyle.scale;
                if (sourceStyle.transitionTimingFunction.keyword != StyleKeyword.Null && valueStyle.transitionTimingFunction != sourceStyle.transitionTimingFunction) valueStyle.transitionTimingFunction = sourceStyle.transitionTimingFunction;
                if (sourceStyle.transitionProperty.keyword != StyleKeyword.Null && valueStyle.transitionProperty != sourceStyle.transitionProperty) valueStyle.transitionProperty = sourceStyle.transitionProperty;
                if (sourceStyle.transitionDuration.keyword != StyleKeyword.Null && valueStyle.transitionDuration != sourceStyle.transitionDuration) valueStyle.transitionDuration = sourceStyle.transitionDuration;
                if (sourceStyle.transitionDelay.keyword != StyleKeyword.Null && valueStyle.transitionDelay != sourceStyle.transitionDelay) valueStyle.transitionDelay = sourceStyle.transitionDelay;
                if (sourceStyle.transformOrigin.keyword != StyleKeyword.Null && valueStyle.transformOrigin != sourceStyle.transformOrigin) valueStyle.transformOrigin = sourceStyle.transformOrigin;
            }

            if (copyMask.HasFlag(CopyStyleMask.Size))
            {
                if (sourceStyle.width.keyword != StyleKeyword.Null && valueStyle.width != sourceStyle.width) valueStyle.width = sourceStyle.width;
                if (sourceStyle.minWidth.keyword != StyleKeyword.Null && valueStyle.minWidth != sourceStyle.minWidth) valueStyle.minWidth = sourceStyle.minWidth;
                if (sourceStyle.maxWidth.keyword != StyleKeyword.Null && valueStyle.maxWidth != sourceStyle.maxWidth) valueStyle.maxWidth = sourceStyle.maxWidth;
                if (sourceStyle.height.keyword != StyleKeyword.Null && valueStyle.height != sourceStyle.height) valueStyle.height = sourceStyle.height;
                if (sourceStyle.minHeight.keyword != StyleKeyword.Null && valueStyle.minHeight != sourceStyle.minHeight) valueStyle.minHeight = sourceStyle.minHeight;
                if (sourceStyle.maxHeight.keyword != StyleKeyword.Null && valueStyle.maxHeight != sourceStyle.maxHeight) valueStyle.maxHeight = sourceStyle.maxHeight;
            }

            if (copyMask.HasFlag(CopyStyleMask.Flex))
            {
                if (sourceStyle.alignSelf.keyword != StyleKeyword.Null && valueStyle.alignSelf != sourceStyle.alignSelf) valueStyle.alignSelf = sourceStyle.alignSelf;
                if (sourceStyle.alignContent.keyword != StyleKeyword.Null && valueStyle.alignContent != sourceStyle.alignContent) valueStyle.alignContent = sourceStyle.alignContent;
                if (sourceStyle.alignItems.keyword != StyleKeyword.Null && valueStyle.alignItems != sourceStyle.alignItems) valueStyle.alignItems = sourceStyle.alignItems;
                if (sourceStyle.justifyContent.keyword != StyleKeyword.Null && valueStyle.justifyContent != sourceStyle.justifyContent) valueStyle.justifyContent = sourceStyle.justifyContent;

                if (sourceStyle.flexDirection.keyword != StyleKeyword.Null && valueStyle.flexDirection != sourceStyle.flexDirection) valueStyle.flexDirection = sourceStyle.flexDirection;
                if (sourceStyle.flexWrap.keyword != StyleKeyword.Null && valueStyle.flexWrap != sourceStyle.flexWrap) valueStyle.flexWrap = sourceStyle.flexWrap;
                if (sourceStyle.flexBasis.keyword != StyleKeyword.Null && valueStyle.flexBasis != sourceStyle.flexBasis) valueStyle.flexBasis = sourceStyle.flexBasis.value;
                if (sourceStyle.flexShrink.keyword != StyleKeyword.Null && valueStyle.flexShrink != sourceStyle.flexShrink) valueStyle.flexShrink = sourceStyle.flexShrink;
                if (sourceStyle.flexGrow.keyword != StyleKeyword.Null && valueStyle.flexGrow != sourceStyle.flexGrow) valueStyle.flexGrow = sourceStyle.flexGrow;
            }

            if (copyMask.HasFlag(CopyStyleMask.Display))
            {
                if (sourceStyle.display.keyword != StyleKeyword.Null && valueStyle.display != sourceStyle.display) valueStyle.display = sourceStyle.display;
                if (sourceStyle.visibility.keyword != StyleKeyword.Null && valueStyle.visibility != sourceStyle.visibility) valueStyle.visibility = sourceStyle.visibility;
                if (sourceStyle.opacity.keyword != StyleKeyword.Null && valueStyle.opacity != sourceStyle.opacity) valueStyle.opacity = sourceStyle.opacity;
                if (sourceStyle.color.keyword != StyleKeyword.Null && valueStyle.color != sourceStyle.color) valueStyle.color = sourceStyle.color;
                if (sourceStyle.backgroundImage.keyword != StyleKeyword.Null && valueStyle.backgroundImage != sourceStyle.backgroundImage) valueStyle.backgroundImage = sourceStyle.backgroundImage;
                if (sourceStyle.backgroundColor.keyword != StyleKeyword.Null && valueStyle.backgroundColor != sourceStyle.backgroundColor) valueStyle.backgroundColor = sourceStyle.backgroundColor;
                if (sourceStyle.unityBackgroundImageTintColor.keyword != StyleKeyword.Null && valueStyle.unityBackgroundImageTintColor != sourceStyle.unityBackgroundImageTintColor) valueStyle.unityBackgroundImageTintColor = sourceStyle.unityBackgroundImageTintColor;
                if (sourceStyle.backgroundPositionX.keyword != StyleKeyword.Null && valueStyle.backgroundPositionX != sourceStyle.backgroundPositionX) valueStyle.backgroundPositionX = sourceStyle.backgroundPositionX;
                if (sourceStyle.backgroundPositionY.keyword != StyleKeyword.Null && valueStyle.backgroundPositionY != sourceStyle.backgroundPositionY) valueStyle.backgroundPositionY = sourceStyle.backgroundPositionY;
                if (sourceStyle.backgroundRepeat.keyword != StyleKeyword.Null && valueStyle.backgroundRepeat != sourceStyle.backgroundRepeat) valueStyle.backgroundRepeat = sourceStyle.backgroundRepeat;
                if (sourceStyle.backgroundSize.keyword != StyleKeyword.Null && valueStyle.backgroundSize != sourceStyle.backgroundSize) valueStyle.backgroundSize = sourceStyle.backgroundSize;
            }

            if (copyMask.HasFlag(CopyStyleMask.Padding))
            {
                if (sourceStyle.paddingTop.keyword != StyleKeyword.Null && valueStyle.paddingTop != sourceStyle.paddingTop) valueStyle.paddingTop = sourceStyle.paddingTop;
                if (sourceStyle.paddingLeft.keyword != StyleKeyword.Null && valueStyle.paddingLeft != sourceStyle.paddingLeft) valueStyle.paddingLeft = sourceStyle.paddingLeft;
                if (sourceStyle.paddingRight.keyword != StyleKeyword.Null && valueStyle.paddingRight != sourceStyle.paddingRight) valueStyle.paddingRight = sourceStyle.paddingRight;
                if (sourceStyle.paddingBottom.keyword != StyleKeyword.Null && valueStyle.paddingBottom != sourceStyle.paddingBottom) valueStyle.paddingBottom = sourceStyle.paddingBottom;
            }

            if (copyMask.HasFlag(CopyStyleMask.Margins))
            {
                if (sourceStyle.marginTop.keyword != StyleKeyword.Null && valueStyle.marginTop != sourceStyle.marginTop) valueStyle.marginTop = sourceStyle.marginTop;
                if (sourceStyle.marginLeft.keyword != StyleKeyword.Null && valueStyle.marginLeft != sourceStyle.marginLeft) valueStyle.marginLeft = sourceStyle.marginLeft;
                if (sourceStyle.marginRight.keyword != StyleKeyword.Null && valueStyle.marginRight != sourceStyle.marginRight) valueStyle.marginRight = sourceStyle.marginRight;
                if (sourceStyle.marginBottom.keyword != StyleKeyword.Null && valueStyle.marginBottom != sourceStyle.marginBottom) valueStyle.marginBottom = sourceStyle.marginBottom;
            }

            if (copyMask.HasFlag(CopyStyleMask.Borders))
            {
                if (sourceStyle.borderTopColor.keyword != StyleKeyword.Null && valueStyle.borderTopColor != sourceStyle.borderTopColor) valueStyle.borderTopColor = sourceStyle.borderTopColor;
                if (sourceStyle.borderTopWidth.keyword != StyleKeyword.Null && valueStyle.borderTopWidth != sourceStyle.borderTopWidth) valueStyle.borderTopWidth = sourceStyle.borderTopWidth;
                if (sourceStyle.borderRightWidth.keyword != StyleKeyword.Null && valueStyle.borderRightWidth != sourceStyle.borderRightWidth) valueStyle.borderRightWidth = sourceStyle.borderRightWidth;
                if (sourceStyle.borderRightColor.keyword != StyleKeyword.Null && valueStyle.borderRightColor != sourceStyle.borderRightColor) valueStyle.borderRightColor = sourceStyle.borderRightColor;
                if (sourceStyle.borderLeftWidth.keyword != StyleKeyword.Null && valueStyle.borderLeftWidth != sourceStyle.borderLeftWidth) valueStyle.borderLeftWidth = sourceStyle.borderLeftWidth;
                if (sourceStyle.borderLeftColor.keyword != StyleKeyword.Null && valueStyle.borderLeftColor != sourceStyle.borderLeftColor) valueStyle.borderLeftColor = sourceStyle.borderLeftColor;
                if (sourceStyle.borderBottomWidth.keyword != StyleKeyword.Null && valueStyle.borderBottomWidth != sourceStyle.borderBottomWidth) valueStyle.borderBottomWidth = sourceStyle.borderBottomWidth;
                if (sourceStyle.borderBottomColor.keyword != StyleKeyword.Null && valueStyle.borderBottomColor != sourceStyle.borderBottomColor) valueStyle.borderBottomColor = sourceStyle.borderBottomColor;
                if (sourceStyle.borderTopLeftRadius.keyword != StyleKeyword.Null && valueStyle.borderTopLeftRadius != sourceStyle.borderTopLeftRadius) valueStyle.borderTopLeftRadius = sourceStyle.borderTopLeftRadius;
                if (sourceStyle.borderTopRightRadius.keyword != StyleKeyword.Null && valueStyle.borderTopRightRadius != sourceStyle.borderTopRightRadius) valueStyle.borderTopRightRadius = sourceStyle.borderTopRightRadius;
                if (sourceStyle.borderBottomLeftRadius.keyword != StyleKeyword.Null && valueStyle.borderBottomLeftRadius != sourceStyle.borderBottomLeftRadius) valueStyle.borderBottomLeftRadius = sourceStyle.borderBottomLeftRadius;
                if (sourceStyle.borderBottomRightRadius.keyword != StyleKeyword.Null && valueStyle.borderBottomRightRadius != sourceStyle.borderBottomRightRadius) valueStyle.borderBottomRightRadius = sourceStyle.borderBottomRightRadius;
            }

            if (copyMask.HasFlag(CopyStyleMask.Slicing))
            {
                if (sourceStyle.unitySliceLeft.keyword != StyleKeyword.Null && valueStyle.unitySliceLeft != sourceStyle.unitySliceLeft) valueStyle.unitySliceLeft = sourceStyle.unitySliceLeft;
                if (sourceStyle.unitySliceTop.keyword != StyleKeyword.Null && valueStyle.unitySliceTop != sourceStyle.unitySliceTop) valueStyle.unitySliceTop = sourceStyle.unitySliceTop;
                if (sourceStyle.unitySliceRight.keyword != StyleKeyword.Null && valueStyle.unitySliceRight != sourceStyle.unitySliceRight) valueStyle.unitySliceRight = sourceStyle.unitySliceRight;
                if (sourceStyle.unitySliceBottom.keyword != StyleKeyword.Null && valueStyle.unitySliceBottom != sourceStyle.unitySliceBottom) valueStyle.unitySliceBottom = sourceStyle.unitySliceBottom;
            }

            if (copyMask.HasFlag(CopyStyleMask.Font))
            {
                if (sourceStyle.fontSize.keyword != StyleKeyword.Null && valueStyle.fontSize != sourceStyle.fontSize) valueStyle.fontSize = sourceStyle.fontSize;
                if (sourceStyle.wordSpacing.keyword != StyleKeyword.Null && valueStyle.wordSpacing != sourceStyle.wordSpacing) valueStyle.wordSpacing = sourceStyle.wordSpacing;
                if (sourceStyle.whiteSpace.keyword != StyleKeyword.Null && valueStyle.whiteSpace != sourceStyle.whiteSpace) valueStyle.whiteSpace = sourceStyle.whiteSpace;
                if (sourceStyle.letterSpacing.keyword != StyleKeyword.Null && valueStyle.letterSpacing != sourceStyle.letterSpacing) valueStyle.letterSpacing = sourceStyle.letterSpacing;
                if (sourceStyle.textOverflow.keyword != StyleKeyword.Null && valueStyle.textOverflow != sourceStyle.textOverflow) valueStyle.textOverflow = sourceStyle.textOverflow;
                if (sourceStyle.unityFont.keyword != StyleKeyword.Null && valueStyle.unityFont != sourceStyle.unityFont) valueStyle.unityFont = sourceStyle.unityFont;
                if (sourceStyle.unityFontDefinition.keyword != StyleKeyword.Null && valueStyle.unityFontDefinition != sourceStyle.unityFontDefinition) valueStyle.unityFontDefinition = sourceStyle.unityFontDefinition;
                if (sourceStyle.unityTextOverflowPosition.keyword != StyleKeyword.Null && valueStyle.unityTextOverflowPosition != sourceStyle.unityTextOverflowPosition) valueStyle.unityTextOverflowPosition = sourceStyle.unityTextOverflowPosition;
                if (sourceStyle.unityTextOutlineWidth.keyword != StyleKeyword.Null && valueStyle.unityTextOutlineWidth != sourceStyle.unityTextOutlineWidth) valueStyle.unityTextOutlineWidth = sourceStyle.unityTextOutlineWidth;
                if (sourceStyle.unityTextOutlineColor.keyword != StyleKeyword.Null && valueStyle.unityTextOutlineColor != sourceStyle.unityTextOutlineColor) valueStyle.unityTextOutlineColor = sourceStyle.unityTextOutlineColor;
                if (sourceStyle.unityTextAlign.keyword != StyleKeyword.Null && valueStyle.unityTextAlign != sourceStyle.unityTextAlign) valueStyle.unityTextAlign = sourceStyle.unityTextAlign;
                if (sourceStyle.unityParagraphSpacing.keyword != StyleKeyword.Null && valueStyle.unityParagraphSpacing != sourceStyle.unityParagraphSpacing) valueStyle.unityParagraphSpacing = sourceStyle.unityParagraphSpacing;
                if (sourceStyle.unityFontStyleAndWeight.keyword != StyleKeyword.Null && valueStyle.unityFontStyleAndWeight != sourceStyle.unityFontStyleAndWeight) valueStyle.unityFontStyleAndWeight = sourceStyle.unityFontStyleAndWeight;
            }
        }

        public static float GetItemSpacing(this ICustomStyle style) => style.TryGetValue(new CustomStyleProperty<float>("--item-spacing"), out float spacing) ? spacing : float.NaN;
        public static async void MarginMe(this VisualElement value)
        {
            static int GetLines(VisualElement value, VisualElement parent, float spacing, bool horizontalDirection)
            {
                float valueSize = horizontalDirection ? value.resolvedStyle.width : value.resolvedStyle.height;
                float parentSize = horizontalDirection ? parent.resolvedStyle.width : parent.resolvedStyle.height;

                return valueSize.Invalid() || valueSize == 0 ? parent.childCount : (int)(parentSize / ((2 * valueSize + (spacing.Invalid() ? 0 : spacing)) / 2));
            }
            static int FindIndex(VisualElement value, IEnumerable<VisualElement> children)
            {
                int index = 0;
                foreach (VisualElement child in children)
                {
                    if (child == value)
                        return index;

                    index++;
                }

                return -1;
            }

            await Awaiters.EndOfFrame;

            if (!value.IsShowing() || value.parent == null)
                return;

            VisualElement parent = value.parent;
            float spacing = parent.customStyle.GetItemSpacing();

            if (spacing.Invalid())
                return;

            using PooledObject<List<VisualElement>> pooledObject = ListPool<VisualElement>.Get(out List<VisualElement> children);

            children.AddRange(parent.Children().Where(x => x.resolvedStyle.display == DisplayStyle.Flex));

            if (children.Count == 0)
                return;

            bool horizontalDirection = parent.resolvedStyle.flexDirection == FlexDirection.Row;
            bool fixedSize = parent.resolvedStyle.flexWrap == Wrap.Wrap;

            if (fixedSize)
            {
                for (float i = 0; i < 1; i += Time.deltaTime)
                {
                    await Awaiters.NextFrame;
                    if (value.resolvedStyle.width > 0)
                        break;
                }
            }

            int lines = fixedSize ? GetLines(value, parent, spacing, horizontalDirection) : children.Count;
            int index = FindIndex(value, children);
            float primaryMargin = lines > 0 && (index - 1) % lines != lines - 1 ? spacing : 0;
            float counterMargin = index >= lines ? spacing : 0;

            if (index == children.Count - 1)
            {
                value.style.marginRight = 0;
                value.style.marginBottom = 0;
            }

            if (horizontalDirection)
            {
                if (index > 0) children[index - 1].style.marginRight = primaryMargin;
                if (index >= lines) children[index - lines].style.marginBottom = counterMargin;
            }
            else
            {
                if (index > 0) children[index - 1].style.marginBottom = primaryMargin;
                if (index >= lines) children[index - lines].style.marginRight = counterMargin;
            }
        }
        #endregion

        #region Support Methods
        static VisualElement FindRoot(VisualElement value)
        {
            while (true)
            {
                if (rootMetadata.ContainsKey(value))
                    return value;

                if (value.parent == null)
                    return null;

                value = value.parent;
            }
        }
        static (VisualElement value, string path) FindRoot(VisualElement value, string path)
        {
            while (true)
            {
                if (rootMetadata.ContainsKey(value))
                    return (value, path);

                if (value.parent == null)
                    throw new ArgumentNullException(nameof(value));

                string name = value.name.Split($" {nameof(VisualElement)}:", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? value.name;
                value = value.parent;
                path = path.NotNullOrEmpty() ? CombinePath(name, path) : name;
            }
        }
        static void Initialize(object target, Type targetType, VisualElement targetRoot, bool throwException = false, bool silent = false)
        {
            void RegisterCallback(VisualElement value, QueryAttribute query)
            {
                void Add<TEventType>(VisualElement element, string name, TrickleDown trickleDown) where TEventType : EventBase<TEventType>, new()
                {
                    if (!name.NotNullOrEmpty())
                        return;

                    MethodInfo methodInfo = targetType.GetMethod(name, MethodsFlags);

                    if (methodInfo != null)
                        element.RegisterCallback((EventCallback<TEventType>)Delegate.CreateDelegate(typeof(EventCallback<TEventType>), target, methodInfo.Name, true), trickleDown);
                }

                Add<MouseCaptureOutEvent>(value, query.MouseCaptureOutEvent, query.UseTrickleDown);
                Add<MouseCaptureEvent>(value, query.MouseCaptureEvent, query.UseTrickleDown);

                Add<ValidateCommandEvent>(value, query.ValidateCommandEvent, query.UseTrickleDown);
                Add<ExecuteCommandEvent>(value, query.ExecuteCommandEvent, query.UseTrickleDown);
#if UNITY_EDITOR
                Add<DragExitedEvent>(value, query.DragExitedEvent, query.UseTrickleDown);
                Add<DragUpdatedEvent>(value, query.DragUpdatedEvent, query.UseTrickleDown);
                Add<DragPerformEvent>(value, query.DragPerformEvent, query.UseTrickleDown);
                Add<DragEnterEvent>(value, query.DragEnterEvent, query.UseTrickleDown);
                Add<DragLeaveEvent>(value, query.DragLeaveEvent, query.UseTrickleDown);
#endif
                Add<FocusOutEvent>(value, query.FocusOutEvent, query.UseTrickleDown);
                Add<BlurEvent>(value, query.BlurEvent, query.UseTrickleDown);
                Add<FocusInEvent>(value, query.FocusInEvent, query.UseTrickleDown);
                Add<FocusEvent>(value, query.FocusEvent, query.UseTrickleDown);
                Add<InputEvent>(value, query.InputEvent, query.UseTrickleDown);
                Add<KeyDownEvent>(value, query.KeyDownEvent, query.UseTrickleDown);
                Add<KeyUpEvent>(value, query.KeyUpEvent, query.UseTrickleDown);
                Add<GeometryChangedEvent>(value, query.GeometryChangedEvent, query.UseTrickleDown);
                Add<PointerDownEvent>(value, query.PointerDownEvent, query.UseTrickleDown);
                Add<PointerUpEvent>(value, query.PointerUpEvent, query.UseTrickleDown);
                Add<PointerMoveEvent>(value, query.PointerMoveEvent, query.UseTrickleDown);
                Add<MouseDownEvent>(value, query.MouseDownEvent, query.UseTrickleDown);
                Add<MouseUpEvent>(value, query.MouseUpEvent, query.UseTrickleDown);
                Add<MouseMoveEvent>(value, query.MouseMoveEvent, query.UseTrickleDown);
                Add<ContextClickEvent>(value, query.ContextClickEvent, query.UseTrickleDown);
                Add<WheelEvent>(value, query.WheelEvent, query.UseTrickleDown);
                Add<MouseEnterEvent>(value, query.MouseEnterEvent, query.UseTrickleDown);
                Add<MouseLeaveEvent>(value, query.MouseLeaveEvent, query.UseTrickleDown);
                Add<MouseEnterWindowEvent>(value, query.MouseEnterWindowEvent, query.UseTrickleDown);
                Add<MouseLeaveWindowEvent>(value, query.MouseLeaveWindowEvent, query.UseTrickleDown);
                Add<MouseOverEvent>(value, query.MouseOverEvent, query.UseTrickleDown);
                Add<MouseOutEvent>(value, query.MouseOutEvent, query.UseTrickleDown);
                Add<ContextualMenuPopulateEvent>(value, query.ContextualMenuPopulateEvent, query.UseTrickleDown);
                Add<AttachToPanelEvent>(value, query.AttachToPanelEvent, query.UseTrickleDown);
                Add<DetachFromPanelEvent>(value, query.DetachFromPanelEvent, query.UseTrickleDown);
                Add<TooltipEvent>(value, query.TooltipEvent, query.UseTrickleDown);
                Add<IMGUIEvent>(value, query.IMGUIEvent, query.UseTrickleDown);
            }
            void RegisterValueChangedCallback(VisualElement value, QueryAttribute query)
            {
                EventCallback<TEventType> GetCallback<TEventType>(string name) where TEventType : EventBase<TEventType>, new()
                {
                    MethodInfo methodInfo = targetType.GetMethod(name, MethodsFlags);

                    if (methodInfo != null)
                        return (EventCallback<TEventType>)Delegate.CreateDelegate(typeof(EventCallback<TEventType>), target, methodInfo.Name, true);

                    throw new NotSupportedException();
                }

                if (query.ChangeEvent.NotNullOrEmpty() && value is TextField textField)
                    textField.RegisterValueChangedCallback(GetCallback<ChangeEvent<string>>(query.ChangeEvent));

                if (query.ChangeEvent.NotNullOrEmpty() && value is Toggle toggleField)
                    toggleField.RegisterValueChangedCallback(GetCallback<ChangeEvent<bool>>(query.ChangeEvent));

                if (query.ChangeEvent.NotNullOrEmpty() && value is SliderInt sliderIntField)
                    sliderIntField.RegisterValueChangedCallback(GetCallback<ChangeEvent<int>>(query.ChangeEvent));

                if (query.ChangeEvent.NotNullOrEmpty() && value is INotifyValueChanged<float> notifyFloatValueChanged)
                    notifyFloatValueChanged.RegisterValueChangedCallback(GetCallback<ChangeEvent<float>>(query.ChangeEvent));
            }
            void AddClicked(VisualElement value, QueryAttribute query)
            {
                if (!query.Clicked.NotNullOrEmpty() || value is not Button button)
                    return;

                MethodInfo methodInfo = targetType.GetMethod(query.Clicked, BindingFlags.NonPublic | BindingFlags.Instance);

                if (methodInfo != null)
                    button.clicked += (Action)Delegate.CreateDelegate(typeof(Action), target, methodInfo.Name, true);
            }
            void AddTemplate(VisualElement value, QueryAttribute query)
            {
                if (query.Template.NullOrEmpty() && !query.Hash)
                    return;

                if (query.Template == "Hash" || query.Hash)
                {
                    cloneMap.Add(value, value.tooltip);
                    value.tooltip = null;
                }
                else
                {
                    cloneMap.Add(value, query.Template);
                }
            }
            VisualElement InitializeElement(FieldInfo field, QueryAttribute queryRoot, QueryAttribute query)
            {
                VisualElement ResolveElement(QueryAttribute queryRoot, QueryAttribute query)
                {
                    VisualElement queryRootElement = targetRoot;
                    if (queryRoot != null && !queryRoot.Path.NullOrEmpty() && queryRoot.Path != query.Path)
                        queryRootElement = targetRoot.Find<VisualElement>(queryRoot.Path, false) ?? targetRoot;

                    VisualElement value = queryRootElement.Find<VisualElement>(query.Path, !query.Nullable);

                    if (query.ReplaceElementPath.NotNullOrEmpty())
                    {
                        if (value != null)
                        {
                            value = value.Replace(targetRoot.Find(query.ReplaceElementPath));
                        }
                        else
                        {
                            string name = Path.GetFileName(query.Path);

                            if (name == query.Path)
                            {
                                value = targetRoot.Find<VisualElement>(query.ReplaceElementPath, throwException, silent)?.Clone(targetRoot);
                            }
                            else
                            {
                                string path = query.Path.Remove(query.Path.Length - name.Length - 1, name.Length + 1);
                                value = targetRoot.Find<VisualElement>(query.ReplaceElementPath, throwException, silent)?.Clone(targetRoot.Find<VisualElement>(path));
                            }

                            if (value != null)
                                value.name = name;
                        }
                    }

                    if (query.RebuildElementEvent.NotNullOrEmpty())
                    {
                        MethodInfo methodInfo = targetType.GetMethod(query.RebuildElementEvent, MethodsFlags);
                        if (methodInfo != null)
                            value = (VisualElement)methodInfo.Invoke(target, new object[] { value });
                    }

                    if (value == null)
                        return null;

                    Type valueType = value.GetType();

                    if (valueType != field.FieldType && valueType.IsAssignableFrom(field.FieldType) && field.FieldType != typeof(VisualElement))
                    {
                        if (throwException)
                            throw new InvalidOperationException($"Element `{value.name}` of type=[{value.GetType()}] cannot be inserted into `{field.Name}` with type=[{field.FieldType}]");

                        if (!silent)
                            Debug.LogWarning($"[{nameof(VisualElementMetadata)}] Element `{value.name}` of type=[{value.GetType()}] cannot be inserted into `{field.Name}` with type=[{field.FieldType}]");

                        return null;
                    }

                    field.SetValue(target, value);

                    return value;
                }

                if (query == null)
                    throw new ArgumentNullException(nameof(query));

                VisualElement element = ResolveElement(queryRoot, query);

                if (element == null)
                    return null;

                RegisterCallback(element, query);
                RegisterValueChangedCallback(element, query);
                AddClicked(element, query);
                AddTemplate(element, query);

                return element;
            }

            QueryAttribute queryRoot = null;

            foreach (FieldInfo field in targetType.GetFields(FieldsFlags))
            {
                QueryAttribute query = field.GetCustomAttribute<QueryAttribute>();

                if (query == null)
                    continue;

                if (query.StartRoot)
                    queryRoot = query;

                VisualElement element = InitializeElement(field, queryRoot, query);

                if (element != null)
                    OnInitializeElement?.Invoke(element, target, targetType, field, queryRoot, query);

                if (query.EndRoot)
                    queryRoot = null;

                if (element is ISubElement subElement)
                {
                    Initialize(subElement, field.FieldType, element, throwException, silent);
                    subElement.OnInitialize();
                }

                if (query.Hide)
                    hide.Add(element);
            }
        }
        #endregion
    }
}