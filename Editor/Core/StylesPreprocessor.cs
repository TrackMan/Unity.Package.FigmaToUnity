using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Figma.Core
{
    using Assets;
    using Uss;
    using Internals;
    using static Internals.Const;

    internal class StylesPreprocessor
    {
        static readonly Regex multipleDashesRegex = new("-{2,}", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

        #region Fields
        readonly List<(StyleSlot slot, UssStyle style)> styles = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> nodeStyleMap = new(initialCollectionCapacity);
        readonly List<ComponentNode> components = new(initialCollectionCapacity);
        readonly List<Dictionary<string, Style>> componentsStyles = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> componentStyleMap = new(initialCollectionCapacity);

        readonly AssetsInfo assetsInfo;
        readonly Data data;
        readonly NodeMetadata nodeMetadata;
        readonly Usages usages;
        #endregion

        #region Properties
        public IReadOnlyList<(StyleSlot slot, UssStyle style)> Styles => styles;
        public IReadOnlyDictionary<BaseNode, UssStyle> NodeStyleMap => nodeStyleMap;
        #endregion

        internal StylesPreprocessor(Data data, AssetsInfo assetsInfo, NodeMetadata nodeMetadata, Usages usages)
        {
            this.data = data;
            this.usages = usages;
            this.assetsInfo = assetsInfo;
            this.nodeMetadata = nodeMetadata;

            foreach (CanvasNode canvas in data.document.children)
            {
                AddMissingNodes(canvas);
                AddImageFills(canvas);
                AddPngNodes(canvas);
                AddSvgNodes(canvas);
                AddGradients(canvas);
            }
        }

        #region Methods
        internal void Run()
        {
            AddStyles(data.document, data.styles, false);
            data.document.children.ForEach(x => AddStyles(x, data.styles, false));

            for (int i = 0; i < components.Count; i++)
                AddStyles(components[i], componentsStyles[i], true);

            InheritStylesRecursively(data.document);
            data.document.children.ForEach(InheritStylesRecursively);

            AddTransitionStyles();
        }
        internal void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }
        void AddTransitionStyles()
        {
            ComponentNode GetTransitionNode(ComponentSetNode componentSet, ComponentNode defaultComponent, TriggerType triggerType)
            {
                Action action = defaultComponent.interactions
                                                .Where(interaction => interaction.trigger.type == triggerType)
                                                .Select(interaction => interaction.actions.FirstOrDefault())
                                                .FirstOrDefault(action => action?.destinationId != null);

                string destinationId = action?.destinationId;

                ComponentNode node = (ComponentNode)componentSet.children.FirstOrDefault(component => component is ComponentNode && component.id == destinationId);
                return node;
            }
            UssStyle GetStyle(Dictionary<BaseNode, UssStyle> componentStyleMap, ComponentSetNode componentSet, ComponentNode defaultComponent, TriggerType triggerType)
            {
                UssStyle style = null;
                ComponentNode node = GetTransitionNode(componentSet, defaultComponent, triggerType);

                if (node != null)
                    componentStyleMap.TryGetValue(node, out style);

                return style;
            }

            UssStyle visualElement = new(nameof(UnityEngine.UIElements.VisualElement));

            foreach ((BaseNode key, UssStyle componentSetStyle) in nodeStyleMap)
            {
                if (key is not ComponentSetNode componentSet)
                    continue;

                ComponentNode defaultComponent = null;
                Action action = null;

                foreach (SceneNode sceneNode in componentSet.children)
                {
                    if (sceneNode is not ComponentNode componentNode)
                        continue;

                    Interactions activeInteraction = componentNode.interactions.FirstOrDefault(interaction => interaction.trigger.type == TriggerType.ON_HOVER ||
                                                                                                              interaction.trigger.type == TriggerType.ON_CLICK);
                    if (activeInteraction == null)
                        continue;

                    action = activeInteraction.actions.FirstOrDefault(action => action.destinationId != null);

                    if (action == null)
                        continue;

                    defaultComponent = componentNode;
                    UssStyle subStyle = new(componentSetStyle.Name) { Target = visualElement };

                    if (action.transition != null)
                    {
                        subStyle.transitionDuration = action.transition.duration * 1000;
                        subStyle.transitionEasing = (EasingFunction)action.transition.easing.type;
                    }

                    componentSetStyle.SubStyles.Add(subStyle);
                    break;
                }

                if (defaultComponent == null)
                    continue;

                componentStyleMap.TryGetValue(defaultComponent, out UssStyle idleStyle);

                UssStyle hoverStyle = GetStyle(componentStyleMap, componentSet, defaultComponent, TriggerType.ON_HOVER);
                UssStyle clickStyle = GetStyle(componentStyleMap, componentSet, defaultComponent, TriggerType.ON_CLICK);

                if (idleStyle == null)
                    continue;

                void InjectSubStyles(ComponentNode node, IReadOnlyList<UssStyle> defaultStyles, PseudoClass pseudoClass)
                {
                    IReadOnlyList<UssStyle> styles = GetStyles(node);
                    for (int i = 0; i < styles.Count; i++)
                    {
                        UssStyle style = styles[i];
                        UssStyle defaultStyle = defaultStyles[i];
                        componentSetStyle.SubStyles.Add(new UssStyle(componentSetStyle.Name) { PseudoClass = pseudoClass, Target = defaultStyle }.CopyFrom(style));
                    }
                }

                if (action.transition is { type: TransitionType.SMART_ANIMATE })
                {
                    ComponentNode hoverNode = GetTransitionNode(componentSet, defaultComponent, TriggerType.ON_HOVER);
                    ComponentNode clickNode = GetTransitionNode(componentSet, defaultComponent, TriggerType.ON_CLICK);
                    IReadOnlyList<UssStyle> defaultStyles = GetStyles(defaultComponent);

                    if (hoverNode != null) InjectSubStyles(hoverNode, defaultStyles, PseudoClass.Hover);
                    if (clickNode != null) InjectSubStyles(clickNode, defaultStyles, PseudoClass.Active);
                }

                if (action.transition is { type: TransitionType.DISSOLVE })
                    componentSetStyle.SubStyles.AddRange(UssStyle.MakeTransitionStyles(componentSetStyle, idleStyle, hoverStyle, clickStyle));
            }
        }
        void AddMissingNodes(BaseNode root)
        {
            Stack<BaseNode> nodes = new();
            nodes.Push(root);
            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                if (node is InstanceNode instance && FindNode(instance.componentId) == null)
                    usages.MissingComponents.Add(instance.componentId);

                if (node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }
        }
        void AddImageFills(BaseNode root)
        {
            Stack<BaseNode> nodes = new();
            nodes.Push(root);
            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node) || node is BooleanOperationNode)
                    continue;

                if (!IsSvgNode(node) && HasImageFill(node))
                    usages.ImageFillNodes.Add(node);

                if (node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }
        }
        void AddPngNodes(BaseNode root)
        {
            Stack<BaseNode> nodes = new();
            nodes.Push(root);
            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                    continue;

                if (IsSvgNode(node) && HasImageFill(node))
                    usages.PngNodes.Add(node);

                if (node is not BooleanOperationNode && node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }
        }
        void AddSvgNodes(BaseNode root)
        {
            Stack<BaseNode> nodes = new();
            nodes.Push(root);
            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                    continue;

                if (IsSvgNode(node) && !HasImageFill(node))
                    usages.SvgNodes.Add(node);

                if (node is not BooleanOperationNode && node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }
        }
        void AddGradients(BaseNode root)
        {
            Stack<BaseNode> nodes = new();
            nodes.Push(root);
            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node) || node is BooleanOperationNode)
                    continue;

                if (node is IGeometryMixin geometry)
                    geometry.fills.OfType<GradientPaint>().ForEach(x => usages.Gradients.TryAdd(x.GetHash(), x));
                else if (node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }
        }
        void AddStyles(BaseNode root, Dictionary<string, Style> styles, bool insideComponent)
        {
            string GetClassName(string name, string prefix = "n")
            {
                if (name.Length > 64)
                    name = name[..64];

                name = invalidCharsRegex.Replace(name, "-");
                name = multipleDashesRegex.Replace(name, "-");
                name = name.Trim('-');

                if (string.IsNullOrEmpty(name) || name.All(c => c == '-'))
                    name = prefix;

                if (char.IsDigit(name[0]))
                    name = $"{prefix}-{name}";

                return name;
            }

            Stack<BaseNode> nodes = new();
            nodes.Push(root);
            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                insideComponent = insideComponent || node is ComponentNode;

                if (!insideComponent)
                {
                    UssStyle style = new(GetClassName(node.name), assetsInfo, node);

                    if (node is ComponentSetNode)
                    {
                        // Removing annoying borders for ComponentSetNode
                        style.Attributes.Clear();
                        style.Attributes.Add("overflow", "hidden");
                    }

                    nodeStyleMap[node] = style;
                }
                else
                    componentStyleMap[node] = new UssStyle(GetClassName(node.name), assetsInfo, node);

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
                            this.styles.Add((style, new UssStyle(GetClassName(style.name, "s"), assetsInfo, style.Slot, style.styleType, node)));
                    }
                }

                if (node is IChildrenMixin parent)
                    foreach (SceneNode child in parent.children)
                        nodes.Push(child);
            }
        }
        #endregion

        #region Support Methods
        internal static bool IsRootNode(IBaseNodeMixin mixin) => mixin is DocumentNode or CanvasNode or ComponentNode || mixin.parent is CanvasNode or ComponentNode;
        internal static bool HasImageFill(IBaseNodeMixin mixin) => mixin is IGeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);
        internal static bool IsSvgNode(IBaseNodeMixin mixin) => mixin is LineNode or EllipseNode or RegularPolygonNode or StarNode or VectorNode || (mixin is BooleanOperationNode && IsBooleanOperationVisible(mixin));
        internal static bool IsVisible(IBaseNodeMixin mixin) => (mixin is not ISceneNodeMixin scene || !scene.visible.HasValueAndFalse()) && (mixin.parent == null || IsVisible(mixin.parent));

        static bool IsBooleanOperationVisible(IBaseNodeMixin root)
        {
            if (root is not IChildrenMixin)
                return false;

            Stack<IBaseNodeMixin> nodes = new();
            nodes.Push(root);

            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                IBaseNodeMixin node = nodes.Pop();

                if (node is not IChildrenMixin children)
                    continue;

                foreach (SceneNode child in children.children)
                {
                    if (child is BooleanOperationNode)
                        nodes.Push(child);
                    else if (IsVisible(child) && IsSvgNode(child))
                        return true;
                }
            }

            return false;
        }
        internal IReadOnlyList<UssStyle> GetStyles(BaseNode root)
        {
            List<UssStyle> result = new();
            Stack<BaseNode> nodes = new();
            nodes.Push(root);

            int i = 0;

            while (nodes.Count > 0)
            {
                if (i++ >= maximumAllowedDepthLimit)
                    throw new InvalidOperationException(maximumDepthLimitReachedExceptionMessage);

                BaseNode node = nodes.Pop();

                if (!IsVisible(node))
                    continue;

                if (componentStyleMap.TryGetValue(node, out UssStyle styles) || nodeStyleMap.TryGetValue(node, out styles))
                    result.Add(styles);

                if (node is not IChildrenMixin nodeWithChildren)
                    continue;

                foreach (SceneNode child in nodeWithChildren.children)
                    if (child is not ComponentSetNode)
                        nodes.Push(child);
            }

            return result;
        }

        void InheritStylesRecursively(BaseNode node)
        {
            UssStyle style = GetStyle(node);
            UssStyle component = null;
            List<UssStyle> styles = new();

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);

                if (componentNode != null)
                    component = GetStyle(componentNode);
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);

                    if (componentNode != null)
                        component = GetStyle(componentNode);
                }
            }

            if (node is IBlendMixin { styles: not null } blend)
                foreach (KeyValuePair<string, string> keyValue in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = keyValue.Key;
                    if (slot.EndsWith('s'))
                        slot = slot[..^1];

                    string id = keyValue.Value;
                    string key = null;

                    if (data.styles.TryGetValue(id, out Style documentStyle))
                        key = documentStyle.key;

                    foreach (Dictionary<string, Style> componentStyle in componentsStyles)
                        if (componentStyle.TryGetValue(id, out Style value))
                            key = value.key;

                    int index;
                    if (key.NotNullOrEmpty() && (index = this.styles.FindIndex(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == key)) >= 0)
                        styles.Add(this.styles[index].style);
                }

            if (component != null && styles.Count > 0) style.Inherit(component, styles);
            else if (component != null) style.Inherit(component);
            else if (styles.Count > 0) style.Inherit(styles);

            if (node is not BooleanOperationNode && node is IChildrenMixin children)
                foreach (SceneNode child in children.children)
                    InheritStylesRecursively(child);
        }
        BaseNode FindNode(string id)
        {
            BaseNode Find(BaseNode root)
            {
                if (root is not IChildrenMixin container)
                    return null;

                Stack<SceneNode> stack = new();
                int i = 0;

                foreach (SceneNode child in container.children)
                    stack.Push(child);

                while (stack.Count > 0)
                {
                    if (i++ >= maximumAllowedDepthLimit)
                        throw new InvalidCastException(maximumDepthLimitReachedExceptionMessage);

                    SceneNode current = stack.Pop();

                    if (current.id == id)
                        return current;

                    if (current is IChildrenMixin currentContainer)
                        foreach (SceneNode child in currentContainer.children)
                            stack.Push(child);
                }

                return null;
            }

            if (data.document.id == id)
                return data.document;

            foreach (CanvasNode canvas in data.document.children)
            {
                if (canvas.id == id)
                    return canvas;

                BaseNode node = Find(canvas);

                if (node != null)
                    return node;
            }

            return null;
        }
        internal string GetClassList(BaseNode node)
        {
            string classList = string.Empty;
            UssStyle style = GetStyle(node);

            if (style == null)
                return classList;

            string component = string.Empty;
            List<string> styles = new();

            if (node is InstanceNode instance)
            {
                BaseNode componentNode = FindNode(instance.componentId);

                if (componentNode != null)
                    component = GetStyle(componentNode).Name;
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    BaseNode componentNode = FindNode(componentId);

                    if (componentNode != null)
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
                    string key = null;

                    if (data.styles.TryGetValue(id, out Style documentStyle))
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
                component = null;
                styles.Clear();
            }

            classList = component.NotNullOrEmpty()
                ? styles.Count > 0 ? style.ResolveClassList(component, styles) : style.ResolveClassList(component)
                : styles.Count > 0
                    ? style.ResolveClassList(styles)
                    : style.ResolveClassList();

            if (IsRootNode(node))
                classList += $"{(string.IsNullOrEmpty(classList) ? string.Empty : " ")}{UssStyle.viewportClass.Name}";

            return $"{UssStyle.overrideClass.Name} {classList}";
        }
        UssStyle GetStyle(BaseNode node) => componentStyleMap.TryGetValue(node, out UssStyle style) || nodeStyleMap.TryGetValue(node, out style) ? style : null;
        #endregion
    }
}