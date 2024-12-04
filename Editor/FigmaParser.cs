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
    using Core.Uss;
    using Core.Uxml;
    using Internals;

    internal class FigmaParser
    {
        const int initialCollectionCapacity = 128;

        internal const string imagesDirectoryName = "Images";
        internal const string elementsDirectoryName = "Elements";
        internal const string componentsDirectoryName = "Components";

        static readonly Regex multipleDashesRegex = new("-{2,}", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegex = new($"[^a-zA-Z0-9]", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegexStateNode = new($"[^a-zA-Z0-9:]", RegexOptions.Compiled);

        #region Fields
        readonly Files files;
        readonly NodeMetadata nodeMetadata;

        readonly List<ComponentNode> components = new(initialCollectionCapacity);
        readonly List<Dictionary<string, Style>> componentsStyles = new(initialCollectionCapacity);

        readonly List<(StyleSlot slot, UssStyle style)> styles = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> componentStyleMap = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> nodeStyleMap = new(initialCollectionCapacity);

        static readonly string[] ussStates = { ":hover", ":active", ":inactive", ":focus", ":selected", ":disabled", ":enabled", ":checked", ":root" };
        #endregion

        #region Properties
        internal List<string> MissingComponents { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> ImageFillNodes { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> PngNodes { get; } = new(initialCollectionCapacity);
        internal List<BaseNode> SvgNodes { get; } = new(initialCollectionCapacity);
        internal Dictionary<string, GradientPaint> Gradients { get; } = new(initialCollectionCapacity);
        
        protected DocumentNode Document => files.document;
        protected Dictionary<string, Style> documentStyles => files.styles;
        #endregion

        #region Constructors
        internal FigmaParser(Files files, NodeMetadata nodeMetadata)
        {
            this.files = files;
            this.nodeMetadata = nodeMetadata;
            
            foreach (CanvasNode canvas in files.document.children)
            {
                AddMissingNodesRecursively(canvas);
                AddImageFillsRecursively(canvas);
                AddPngNodesRecursively(canvas);
                AddSvgNodesRecursively(canvas);
                AddGradientsRecursively(canvas);
            }
        }
        #endregion

        #region Methods
        internal void Run(Func<string, string, (bool valid, string path)> getAssetPath, Func<string, string, (bool valid, int width, int height)> getAssetSize)
        {
            AddStylesRecursively(Document, documentStyles, false, getAssetPath, getAssetSize);
            Document.children.ForEach(x => AddStylesRecursively(x, documentStyles, false, getAssetPath, getAssetSize));

            foreach ((ComponentNode component, int index) in components.Select((x, i) => (x, i)))
                AddStylesRecursively(component, componentsStyles[index], true, getAssetPath, getAssetSize);

            InheritStylesRecursively(Document);
            Document.children.ForEach(InheritStylesRecursively);
        }
        internal void Write(string directory, string name, NodeMetadata metadata)
        {
            void GroupRenameStyles(IEnumerable<UssStyle> styles)
            {
                foreach (IGrouping<string, UssStyle> group in styles.GroupBy(x => x.Name).Where(y => y.Count() > 1))
                {
                    int i = 0;
                    foreach (UssStyle style in group)
                        style.Name += $"-{(i++ + 1).NumberToWords()}";
                }
            }
            void FixStateStyles(IEnumerable<BaseNode> nodes)
            {
                static string GetState(string value) => value.Substring(value.LastIndexOf(":", StringComparison.Ordinal), value.Length - value.LastIndexOf(":", StringComparison.Ordinal));

                foreach (BaseNode node in nodes)
                {
                    if (node.parent is not IChildrenMixin parent) continue;

                    foreach (SceneNode child in parent.children)
                    {
                        if (IsStateNode(child) && IsStateNode(child, node))
                        {
                            UssStyle childStyle = GetStyle(child);
                            if (childStyle is not null)
                            {
                                childStyle.Name = $"{nodeStyleMap[node].Name}{GetState(childStyle.Name)}";
                            }
                        }
                    }
                }
            }
            void AddTransitionStyles()
            {
                UssStyle GetStyle(Dictionary<BaseNode, UssStyle> componentStyleMap, ComponentSetNode componentSet, ComponentNode defaultComponent, TriggerType triggerType)
                {
                    UssStyle style = default;
                    Action action = defaultComponent.interactions
                                                         .Where(interaction => interaction.trigger.type == triggerType)
                                                         .Select(interaction => interaction.actions.FirstOrDefault())
                                                         .FirstOrDefault(action => action is not null && action.destinationId is not null);

                    string destinationId = action?.destinationId;

                    ComponentNode node = (ComponentNode)componentSet.children.FirstOrDefault(component => component is ComponentNode && component.id == destinationId);
                    if (node is not null) componentStyleMap.TryGetValue(node, out style);
                    return style;
                }
                foreach (KeyValuePair<BaseNode, UssStyle> pair in nodeStyleMap)
                {
                    if (pair.Key is ComponentSetNode componentSet)
                    {
                        UssStyle componentSetStyle = pair.Value;
                        ComponentNode defaultComponent = null;
                        foreach (SceneNode sceneNode in componentSet.children)
                        {
                            if (sceneNode is not ComponentNode componentNode) continue;

                            Interactions activeInteraction = componentNode.interactions.FirstOrDefault(interaction => interaction.trigger.type == TriggerType.ON_HOVER ||
                                                                                                                      interaction.trigger.type == TriggerType.ON_CLICK);
                            if (activeInteraction == null) continue;

                            Action action = activeInteraction.actions.FirstOrDefault(action => action.destinationId is not null);

                            if (action is not null)
                            {
                                defaultComponent = componentNode;
                                UssStyle subStyle = new($"{componentSetStyle.Name} > VisualElement");
                                if (action.transition is not null)
                                {
                                    subStyle.transitionDuration = action.transition.duration * 1000;
                                    subStyle.transitionEasing = (EasingFunction) action.transition.easing.type;
                                }

                                componentSetStyle.Substyles.Add(subStyle);
                                break;
                            }
                        }

                        if (defaultComponent is not null)
                        {
                            componentStyleMap.TryGetValue(defaultComponent, out UssStyle idleStyle);

                            UssStyle hoverStyle = GetStyle(componentStyleMap, componentSet, defaultComponent, TriggerType.ON_HOVER);
                            UssStyle clickStyle = GetStyle(componentStyleMap, componentSet, defaultComponent, TriggerType.ON_CLICK);

                            if (idleStyle is not null) componentSetStyle.Substyles.AddRange(UssStyle.MakeTransitionStyles(componentSetStyle, idleStyle, hoverStyle, clickStyle));
                        }
                    }
                }
            }

            KeyValuePair<BaseNode, UssStyle>[] nodeStyleFiltered = nodeStyleMap.Where(x => IsVisible(x.Key) && (nodeMetadata.EnabledInHierarchy(x.Key) || x.Key is ComponentSetNode)).ToArray();
            UssStyle[] nodeStyleStatelessFiltered = nodeStyleFiltered.Where(x => !IsStateNode(x.Key)).Select(x => x.Value).ToArray();
            UssStyle[] stylesFiltered = styles.Select(x => x.style).Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();
            UssStyle[] componentStyleFiltered = componentStyleMap.Values.ToArray(); //.Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();

            GroupRenameStyles(stylesFiltered.Union(componentStyleFiltered).Union(nodeStyleStatelessFiltered));
            FixStateStyles(nodeStyleFiltered.Select(x => x.Key));
            AddTransitionStyles();

            // Writing UXML file
            UxmlBuilder _ = new(directory, name, files, nodeMetadata, GetClassList);

            // Writing USS styles
            using UssWriter writer = new(Path.Combine(directory, $"{name}.uss"));
            writer.Write(UssStyle.overrideClass);
            writer.Write(UssStyle.viewportClass);
            writer.Write(stylesFiltered);
            writer.Write(componentStyleFiltered);
            writer.Write(nodeStyleFiltered.Select(x => x.Value));
        }
        internal void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }

        void AddMissingNodesRecursively(BaseNode node)
        {
            if (node is InstanceNode instance && FindNode(instance.componentId) is null)
                MissingComponents.Add(instance.componentId);

            if (node is IChildrenMixin children)
                children.children.ForEach(AddMissingNodesRecursively);
        }
        void AddImageFillsRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node) || node is BooleanOperationNode)
                return;

            if (!IsSvgNode(node) && HasImageFill(node))
                ImageFillNodes.Add(node);

            if (node is IChildrenMixin children)
                children.children.ForEach(x => AddImageFillsRecursively(x));
        }
        void AddPngNodesRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                return;

            if (IsSvgNode(node) && HasImageFill(node))
                PngNodes.Add(node);

            if (node is BooleanOperationNode)
                return;

            if (node is IChildrenMixin children)
                children.children.ForEach(x => AddPngNodesRecursively(x));
        }
        void AddSvgNodesRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                return;

            if (IsSvgNode(node) && !HasImageFill(node)) SvgNodes.Add(node);

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case IChildrenMixin children:
                {
                    children.children.ForEach(child => AddSvgNodesRecursively(child));
                    return;
                }
            }
        }
        void AddGradientsRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node)) return;

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case IGeometryMixin geometry:
                {
                    foreach (GradientPaint gradient in geometry.fills.OfType<GradientPaint>())
                        Gradients.TryAdd(gradient.GetHash(), gradient);
                    break;
                }
            }

            if (node is not IChildrenMixin children) return;

            children.children.ForEach(child => AddGradientsRecursively(child));
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

            if (node is ComponentNode)
                insideComponent = true;

            if (insideComponent)
            {
                componentStyleMap[node] = new UssStyle(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);
            }
            else
            {
                UssStyle style = new(GetClassName(node.name, IsStateNode(node)), getAssetPath, getAssetSize, node);

                if (node is ComponentSetNode)
                {
                    // Removing annoying borders for ComponentSetNode
                    style.Attributes.Clear();
                    style.Attributes.Add("overflow", "hidden");
                }

                nodeStyleMap[node] = style;
            }

            if (node is IBlendMixin { styles: not null } blend)
            {
                foreach ((string key, string value) in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = key;
                    if (slot.EndsWith('s'))
                        slot = slot[..^1];
                    string styleKey = styles[value].key;

                    StyleSlot style = new(text, slot, styles[value]);
                    if (!this.styles.Any(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == styleKey))
                        this.styles.Add((style, new UssStyle(GetClassName(style.name, false, "s"), getAssetPath, getAssetSize, style.Slot, style.styleType, node)));
                }
            }

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case IChildrenMixin children:
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
                if (root is IChildrenMixin children)
                    foreach (SceneNode child in children.children)
                    {
                        if (child.id == id) return child;

                        BaseNode node = Find(child);
                        if (node is not null) return node;
                    }

                return default;
            }

            if (Document.id == id) return Document;

            foreach (CanvasNode canvas in Document.children)
            {
                if (canvas.id == id) return canvas;

                BaseNode node = Find(canvas);
                if (node is not null) return node;
            }

            return default;
        }
        void InheritStylesRecursively(BaseNode node)
        {
            UssStyle style = GetStyle(node);
            UssStyle component = default;
            List<UssStyle> styles = new();

            if (IsStateNode(node) && node.parent is IChildrenMixin parent)
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

            if (node is IBlendMixin { styles: not null } blend)
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

            if (node is IChildrenMixin children)
                children.children.ForEach(InheritStylesRecursively);
        }
        UssStyle GetStyle(BaseNode node)
        {
            if (componentStyleMap.TryGetValue(node, out UssStyle style)) return style;

            return nodeStyleMap.TryGetValue(node, out style) ? style : default;
        }
        string GetClassList(BaseNode node)
        {
            string classList = string.Empty;
            UssStyle style = GetStyle(node);

            if (style is null)
                return classList;

            string component = string.Empty;
            List<string> styles = new();

            if (IsStateNode(node) && node.parent is IChildrenMixin parent)
            {
                BaseNode normalNode = Array.Find(parent.children, x => IsStateNode(node, x));

                if (normalNode is not null)
                    component = GetStyle(normalNode).Name;
            }

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);

                if (componentNode is not null)
                    component = GetStyle(componentNode).Name;
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);

                    if (componentNode is not null)
                        component = GetStyle(componentNode).Name;
                }
            }

            if (node is IBlendMixin { styles: not null } blend)
            {
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;

                    if (slot.EndsWith('s'))
                        slot = slot[..^1];
                    string id = keyValue.Value;
                    string key = default;

                    if (documentStyles.TryGetValue(id, out Style documentStyle))
                        key = documentStyle.key;

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

            if (IsRootNode(node)) classList += $"{(classList == string.Empty ? string.Empty : " ")}{UssStyle.viewportClass.Name}";
            return $"{UssStyle.overrideClass.Name} {classList}";
        }
        #endregion

        #region Support Methods
        internal static bool IsRootNode(IBaseNodeMixin mixin) => mixin is DocumentNode || mixin is CanvasNode || mixin.parent is CanvasNode || mixin is ComponentNode || mixin.parent is ComponentNode;
        internal static bool IsVisible(IBaseNodeMixin mixin)
        {
            if (mixin is ISceneNodeMixin scene && scene.visible.HasValueAndFalse()) return false;

            return mixin.parent is null || IsVisible(mixin.parent);
        }
        internal static bool HasImageFill(IBaseNodeMixin mixin) => mixin is IGeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);
        internal static bool IsSvgNode(IBaseNodeMixin mixin) => mixin is LineNode || mixin is EllipseNode || mixin is RegularPolygonNode || mixin is StarNode || mixin is VectorNode || (mixin is BooleanOperationNode && IsBooleanOperationVisible(mixin));
        internal static bool IsBooleanOperationVisible(IBaseNodeMixin node)
        {
            if (node is not IChildrenMixin children) return false;

            foreach (SceneNode child in children.children)
            {
                if (child is not BooleanOperationNode && IsVisible(child) && IsSvgNode(child)) return true;
                if (child is BooleanOperationNode) return IsBooleanOperationVisible(child);
            }

            return false;
        }
        internal static bool IsStateNode(IBaseNodeMixin mixin) => ussStates.Any(suffix => mixin.name.EndsWith(suffix));
        internal static bool IsStateNode(IBaseNodeMixin mixin, IBaseNodeMixin normal) => mixin.name[..mixin.name.LastIndexOf(":", StringComparison.Ordinal)] == normal.name;
        #endregion
    }
}