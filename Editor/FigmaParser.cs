using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable S1144 // Unused private types or members should be removed

namespace Figma
{
    using Core;
    using Internals;
    using InternalsExtensions;

    internal class FigmaParser
    {
        const int initialCollectionCapacity = 128;

        internal const string images = "Images";
        internal const string elements = "Elements";

        static readonly Regex multipleDashesRegex = new("-{2,}", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegex = new($"[^a-zA-Z0-9]", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegexStateNode = new($"[^a-zA-Z0-9:]", RegexOptions.Compiled);

        #region Fields
        readonly DocumentNode document;
        readonly Dictionary<string, Style> documentStyles;

        readonly List<ComponentNode> components = new(initialCollectionCapacity);
        readonly List<Dictionary<string, Style>> componentsStyles = new(initialCollectionCapacity);

        readonly List<(StyleSlot slot, UssStyle style)> styles = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> componentStyleMap = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> nodeStyleMap = new(initialCollectionCapacity);

        static readonly string[] unitsMap = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        static readonly string[] tensMap = { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        static readonly string[] ussStates = { ":hover", ":active", ":inactive", ":focus", ":selected", ":disabled", ":enabled", ":checked", ":root" };
        #endregion

        #region Properties
        internal List<string> MissingComponents { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> ImageFillNodes { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> PngNodes { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> SvgNodes { get; } = new(initialCollectionCapacity);
        internal Dictionary<string, GradientPaint> Gradients { get; } = new(initialCollectionCapacity);
        #endregion

        #region Constructors
        internal FigmaParser(DocumentNode document, Dictionary<string, Style> documentStyles, Func<BaseNode, bool> enabledInHierarchy)
        {
            this.document = document;
            this.documentStyles = documentStyles;

            foreach (CanvasNode canvas in document.children)
            {
                AddMissingNodesRecursively(canvas);
                AddImageFillsRecursively(canvas, enabledInHierarchy);
                AddPngNodesRecursively(canvas, enabledInHierarchy);
                AddSvgNodesRecursively(canvas, enabledInHierarchy);
                AddGradientsRecursively(canvas, enabledInHierarchy);
            }
        }
        #endregion

        #region Methods
        internal void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }
        internal void Run(Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize)
        {
            AddStylesRecursively(document, documentStyles, false, getAssetPath, getAssetSize);
            foreach (CanvasNode canvas in document.children) AddStylesRecursively(canvas, documentStyles, false, getAssetPath, getAssetSize);
            foreach ((ComponentNode component, int index) in components.Select((x, i) => (x, i))) AddStylesRecursively(component, componentsStyles[index], true, getAssetPath, getAssetSize);

            InheritStylesRecursively(document);
            foreach (CanvasNode canvas in document.children) InheritStylesRecursively(canvas);
        }
        internal void Write(string folder, string name, Func<BaseNode, bool> enabledInHierarchy, Func<BaseNode, (bool hash, string value)> getTemplate, Func<BaseNode, (ElementType type, string typeFullName)> getElementType)
        {
            void GroupRenameStyles(IEnumerable<UssStyle> styles)
            {
                string NumberToWords(int number)
                {
                    if (number == 0) return "zero";
                    if (number < 0) return $"minus-{NumberToWords(Math.Abs(number))}";

                    string words = string.Empty;

                    if (number / 1000000 > 0)
                    {
                        words += $"{NumberToWords(number / 1000000)}-million ";
                        number %= 1000000;
                    }

                    if (number / 1000 > 0)
                    {
                        words += $"{NumberToWords(number / 1000)}-thousand ";
                        number %= 1000;
                    }

                    if (number / 100 > 0)
                    {
                        words += $"{NumberToWords(number / 100)}-hundred ";
                        number %= 100;
                    }

                    if (number > 0)
                    {
                        if (words != "") words += "and-";
                        if (number < 20) words += unitsMap[number];
                        else
                        {
                            words += tensMap[number / 10];
                            if (number % 10 > 0) words += $"-{unitsMap[number % 10]}";
                        }
                    }

                    return words;
                }

                foreach (IGrouping<string, UssStyle> group in styles.GroupBy(x => x.Name).Where(y => y.Count() > 1))
                {
                    int i = 0;
                    foreach (UssStyle style in group) style.Name += $"-{NumberToWords(i++ + 1)}";
                }
            }
            void FixStateStyles(IEnumerable<BaseNode> nodes)
            {
                static string GetState(string value) => value.Substring(value.LastIndexOf(":", StringComparison.Ordinal), value.Length - value.LastIndexOf(":", StringComparison.Ordinal));

                foreach (BaseNode node in nodes)
                {
                    if (node.parent is not ChildrenMixin parent) continue;

                    foreach (SceneNode child in parent.children)
                        if (IsStateNode(child) && IsStateNode(child, node))
                        {
                            UssStyle childStyle = GetStyle(child);
                            if (childStyle is not null) childStyle.Name = $"{nodeStyleMap[node].Name}{GetState(childStyle.Name)}";
                        }
                }
            }

            KeyValuePair<BaseNode, UssStyle>[] nodeStyleFiltered = nodeStyleMap.Where(x => IsVisible(x.Key) && enabledInHierarchy(x.Key)).ToArray();
            UssStyle[] nodeStyleStatelessFiltered = nodeStyleFiltered.Where(x => !IsStateNode(x.Key)).Select(x => x.Value).ToArray();
            UssStyle[] stylesFiltered = styles.Select(x => x.style).Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();
            UssStyle[] componentStyleFiltered = componentStyleMap.Values.Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();

            GroupRenameStyles(stylesFiltered.Union(componentStyleFiltered).Union(nodeStyleStatelessFiltered));
            FixStateStyles(nodeStyleFiltered.Select(x => x.Key));

#pragma warning disable S1481
            UxmlWriter _ = new(document, folder, name, GetClassList, enabledInHierarchy, getTemplate, getElementType);
            using StreamWriter uss = new(Path.Combine(folder, $"{name}.uss"));
            UssWriter __ = new(stylesFiltered, componentStyleFiltered, nodeStyleFiltered.Select(x => x.Value), uss);
#pragma warning restore S1481
        }

        void AddMissingNodesRecursively(BaseNode node)
        {
            if (node is InstanceNode instance && FindNode(instance.componentId) is null)
                MissingComponents.Add(instance.componentId);

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    AddMissingNodesRecursively(child);
        }
        void AddImageFillsRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node) || !enabledInHierarchy(node) || node is BooleanOperationNode) return;

            if (!IsSvgNode(node) && HasImageFill(node)) ImageFillNodes.Add(node);

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    AddImageFillsRecursively(child, enabledInHierarchy);
        }
        void AddPngNodesRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node) || !enabledInHierarchy(node)) return;

            if (IsSvgNode(node) && HasImageFill(node)) PngNodes.Add(node);
            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    AddPngNodesRecursively(child, enabledInHierarchy);
        }
        void AddSvgNodesRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node) || !enabledInHierarchy(node)) return;

            if (IsSvgNode(node) && !HasImageFill(node)) SvgNodes.Add(node);

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case ChildrenMixin children:
                {
                    foreach (SceneNode child in children.children)
                        AddSvgNodesRecursively(child, enabledInHierarchy);
                    return;
                }
            }
        }
        void AddGradientsRecursively(BaseNode node, Func<BaseNode, bool> enabledInHierarchy)
        {
            if (!IsVisible(node) || !enabledInHierarchy(node)) return;

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case GeometryMixin geometry:
                {
                    foreach (GradientPaint gradient in geometry.fills.OfType<GradientPaint>())
                        Gradients.TryAdd(gradient.GetHash(), gradient);
                    break;
                }
            }

            if (node is not ChildrenMixin children) return;

            foreach (SceneNode child in children.children)
                AddGradientsRecursively(child, enabledInHierarchy);
        }
        void AddStylesRecursively(BaseNode node, Dictionary<string, Style> styles, bool insideComponent, Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize)
        {
            string GetClassName(string name, bool state, string prefix = "n")
            {
                if (name.Length > 64)
                    name = name.Substring(0, 64);

                name = (state ? invalidCharsRegexStateNode : invalidCharsRegex).Replace(name, "-");
                name = multipleDashesRegex.Replace(name, "-");
                name = name.Trim('-');

                if (string.IsNullOrEmpty(name) || name.All(c => c == '-'))
                    name = prefix;

                if (char.IsDigit(name[0]))
                    name = $"{prefix}-{name}";

                return name;
            }

            if (node is ComponentNode) insideComponent = true;

            if (insideComponent) componentStyleMap[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);
            else nodeStyleMap[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);

            if (node is BlendMixin { styles: not null } blend)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s')) slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = styles[id].key;

                    StyleSlot style = new(text, slot, styles[id]);
                    if (!this.styles.Any(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key))
                        this.styles.Add((style, new UssStyle(GetClassName(style.name, false, "s"), getAssetPath, getAssetSize, style.Slot, style.styleType, node)));
                }
            }

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case ChildrenMixin children:
                {
                    foreach (SceneNode child in children.children)
                        AddStylesRecursively(child, styles, insideComponent, getAssetPath, getAssetSize);
                    return;
                }
            }
        }

        BaseNode FindNode(string id)
        {
            BaseNode Find(BaseNode root)
            {
                if (root is ChildrenMixin children)
                    foreach (SceneNode child in children.children)
                    {
                        if (child.id == id) return child;

                        BaseNode node = Find(child);
                        if (node is not null) return node;
                    }

                return default;
            }

            if (document.id == id) return document;

            foreach (CanvasNode canvas in document.children)
            {
                if (canvas.id == id) return canvas;

                BaseNode node = Find(canvas);
                if (node is not null) return node;
            }

            return default;
        }
        UssStyle GetStyle(BaseNode node)
        {
            if (componentStyleMap.TryGetValue(node, out UssStyle style)) return style;

            return nodeStyleMap.TryGetValue(node, out style) ? style : default;
        }
        void InheritStylesRecursively(BaseNode node)
        {
            UssStyle style = GetStyle(node);
            UssStyle component = default;
            List<UssStyle> styles = new();

            if (IsStateNode(node) && node.parent is ChildrenMixin parent)
            {
                BaseNode normalNode = Array.Find(parent.children, x => IsStateNode(node, x));
                if (normalNode is not null) component = GetStyle(normalNode);
            }

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);
                if (componentNode is not null) component = GetStyle(componentNode);
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);
                    if (componentNode is not null) component = GetStyle(componentNode);
                }
            }

            if (node is BlendMixin { styles: not null } blend)
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s')) slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = default;
                    if (documentStyles.TryGetValue(id, out Style documentStyle)) key = documentStyle.key;
                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.TryGetValue(id, out Style value))
                            key = value.key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key)) >= 0)
                        styles.Add(this.styles[index].style);
                }

            if (component is not null && styles.Count > 0) style.Inherit(component, styles);
            else if (component is not null) style.Inherit(component);
            else if (styles.Count > 0) style.Inherit(styles);

            if (node is BooleanOperationNode) return;

            if (node is ChildrenMixin children)
                foreach (SceneNode child in children.children)
                    InheritStylesRecursively(child);
        }
        string GetClassList(BaseNode node)
        {
            string classList = string.Empty;
            UssStyle style = GetStyle(node);
            if (style is null) return classList;

            string component = string.Empty;
            List<string> styles = new();

            if (IsStateNode(node) && node.parent is ChildrenMixin parent)
            {
                BaseNode normalNode = Array.Find(parent.children, x => IsStateNode(node, x));
                if (normalNode is not null) component = GetStyle(normalNode).Name;
            }

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);
                if (componentNode is not null) component = GetStyle(componentNode).Name;
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);
                    if (componentNode is not null) component = GetStyle(componentNode).Name;
                }
            }

            if (node is BlendMixin { styles: not null } blend)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s')) slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = default;

                    if (documentStyles.TryGetValue(id, out Style documentStyle)) key = documentStyle.key;

                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.TryGetValue(id, out Style value))
                            key = value.key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key)) >= 0)
                        styles.Add(this.styles[index].style.Name);
                }
            }

            if (IsSvgNode(node))
            {
                component = default;
                styles.Clear();
            }

            if (component.NotNullOrEmpty() && styles.Count > 0) classList = style.ResolveClassList(component, styles);
            else if (component.NotNullOrEmpty()) classList = style.ResolveClassList(component);
            else if (styles.Count > 0) classList = style.ResolveClassList(styles);
            else classList = style.ResolveClassList();

            if (IsRootNode(node)) classList += $"{(classList == string.Empty ? string.Empty : " ")}{UssStyle.viewportClass}";
            return $"{UssStyle.overrideClass} {classList}";
        }
        #endregion

        #region Support Methods
        internal static bool IsRootNode(BaseNodeMixin mixin) => mixin is DocumentNode || mixin is CanvasNode || mixin.parent is CanvasNode || mixin is ComponentNode || mixin.parent is ComponentNode;
        internal static bool IsVisible(BaseNodeMixin mixin)
        {
            if (mixin is SceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;

            return mixin.parent is null || IsVisible(mixin.parent);
        }
        internal static bool HasImageFill(BaseNodeMixin mixin) => mixin is GeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);
        internal static bool IsSvgNode(BaseNodeMixin mixin) => mixin is LineNode || mixin is EllipseNode || mixin is RegularPolygonNode || mixin is StarNode || mixin is VectorNode || (mixin is BooleanOperationNode && IsBooleanOperationVisible(mixin));
        internal static bool IsBooleanOperationVisible(BaseNodeMixin node)
        {
            if (node is not ChildrenMixin children) return false;

            foreach (SceneNode child in children.children)
            {
                if (child is not BooleanOperationNode && IsVisible(child) && IsSvgNode(child)) return true;
                if (child is BooleanOperationNode) return IsBooleanOperationVisible(child);
            }

            return false;
        }
        internal static bool IsStateNode(BaseNodeMixin mixin) => ussStates.Any(suffix => mixin.name.EndsWith(suffix));
        internal static bool IsStateNode(BaseNodeMixin mixin, BaseNodeMixin normal) => mixin.name[..mixin.name.LastIndexOf(":", StringComparison.Ordinal)] == normal.name;
        #endregion
    }
}