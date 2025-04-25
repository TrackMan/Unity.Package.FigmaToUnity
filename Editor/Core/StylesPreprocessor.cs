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
        readonly List<ComponentNode> components = new(initialCollectionCapacity);
        readonly List<Dictionary<string, Style>> componentsStyles = new(initialCollectionCapacity);
        readonly Dictionary<IBaseNodeMixin, UssStyle> componentStyleMap = new(initialCollectionCapacity);
        readonly Dictionary<IBaseNodeMixin, UssStyle> nodeStyleMap = new(initialCollectionCapacity);

        readonly AssetsInfo assetsInfo;
        readonly Data data;
        #endregion

        #region Properties
        internal IReadOnlyList<(StyleSlot slot, UssStyle style)> Styles => styles;
        internal IReadOnlyDictionary<IBaseNodeMixin, UssStyle> NodeStyleMap => nodeStyleMap;
        #endregion

        internal StylesPreprocessor(Data data, AssetsInfo assetsInfo)
        {
            this.data = data;
            this.assetsInfo = assetsInfo;

            AddStyles(data.document, data.styles);
            AddRichText(data.document);

            for (int i = 0; i < components.Count; i++)
            {
                AddStyles(components[i], componentsStyles[i]);
                AddRichText(components[i]);
            }

            InheritStyles(data.document);
            AddTransitionStyles();
        }

        #region Methods
        void AddStyles(IBaseNodeMixin root, Dictionary<string, Style> styles)
        {
            string GetClassName(string name, string prefix = "n")
            {
                const char separator = '-';

                if (name.Length > 64)
                    name = name[..64];

                name = invalidCharsRegex.Replace(name, separator.ToString());
                name = multipleDashesRegex.Replace(name, separator.ToString());
                name = name.Trim(separator);

                if (string.IsNullOrEmpty(name) || name.All(c => c == separator))
                    name = prefix;

                if (char.IsDigit(name[0]))
                    name = $"{prefix}-{name}";

                return name;
            }

            HashSet<IBaseNodeMixin> insideComponents = new();

            foreach (IBaseNodeMixin node in root.Flatten())
            {
                bool insideComponent = node is ComponentNode || insideComponents.Contains(node.parent);

                if (!insideComponent)
                {
                    UssStyle style = new(GetClassName(node.name), assetsInfo, (BaseNode)node);
                    if (node is ComponentSetNode)
                    {
                        // Removing annoying borders for ComponentSetNode
                        style.Attributes.Clear();
                        style.Attributes.Add("overflow", "hidden");
                    }

                    nodeStyleMap[node] = style;
                }
                else
                {
                    insideComponents.Add(node);
                    componentStyleMap[node] = new UssStyle(GetClassName(node.name), assetsInfo, (BaseNode)node);
                }

                if (node is not IBlendMixin { styles: not null } blend)
                    continue;

                foreach ((string key, string value) in blend.styles)
                {
                    bool text = node.type == NodeType.TEXT;
                    string slot = key;

                    if (slot.EndsWith('s'))
                        slot = slot[..^1];

                    string styleKey = styles[value].key;

                    StyleSlot style = new(text, slot, styles[value]);
                    if (!this.styles.Any(x => x.slot.Text == text && x.slot.Slot == slot && x.slot.key == styleKey))
                        this.styles.Add((style, new UssStyle(GetClassName(style.name, "s"), assetsInfo, (BaseNode)node, style)));
                }
            }
        }
        void AddRichText(IBaseNodeMixin node)
        {
            foreach (TextNode textNode in node.Flatten().OfType<TextNode>().Where(x => x.lineTypes is { Length: > 1 } && x.lineTypes.Any(lineType => lineType is LineType.ORDERED or LineType.UNORDERED) ||
                                                                                       (x.styleOverrideTable != null && x.styleOverrideTable.Any())))
                textNode.characters = new RichText.TextBuilder(textNode).Build();
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
            UssStyle GetStyle(Dictionary<IBaseNodeMixin, UssStyle> componentStyleMap, ComponentSetNode componentSet, ComponentNode defaultComponent, TriggerType triggerType)
            {
                UssStyle style = null;
                ComponentNode node = GetTransitionNode(componentSet, defaultComponent, triggerType);

                if (node != null)
                    componentStyleMap.TryGetValue(node, out style);

                return style;
            }

            UssStyle visualElement = new(nameof(UnityEngine.UIElements.VisualElement));

            foreach ((IBaseNodeMixin key, UssStyle componentSetStyle) in nodeStyleMap)
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

                    action = activeInteraction.actions.FirstOrDefault(x => x.destinationId != null);

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
        internal void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }
        #endregion

        #region Support Methods
        IBaseNodeMixin FindNode(string id) => data.document.Flatten().FirstOrDefault(x => x.id == id);
        internal IReadOnlyList<UssStyle> GetStyles(IBaseNodeMixin root) =>
            root.Flatten(node => node.IsVisible() && node is not ComponentSetNode)
                .Select(node => componentStyleMap.TryGetValue(node, out UssStyle style) || nodeStyleMap.TryGetValue(node, out style) ? style : null)
                .Where(style => style is not null)
                .ToList();

        void InheritStyles(IBaseNodeMixin root)
        {
            List<UssStyle> styles = new();

            foreach (IBaseNodeMixin node in root.Flatten(x => x.parent is not BooleanOperationNode))
            {
                UssStyle style = GetStyle(node);
                UssStyle component = null;

                if (node is InstanceNode instance)
                {
                    IBaseNodeMixin componentNode = FindNode(instance.componentId);

                    if (componentNode != null)
                        component = GetStyle(componentNode);
                }
                else if (node.id.Contains(';'))
                {
                    string[] splits = node.id.Split(';');
                    if (splits.Length >= 2)
                    {
                        string componentId = splits[^1];
                        IBaseNodeMixin componentNode = FindNode(componentId);

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

                styles.Clear();
            }
        }
        internal string GetClassList(IBaseNodeMixin node)
        {
            string classList = string.Empty;
            UssStyle style = GetStyle(node);

            if (style == null)
                return classList;

            string component = string.Empty;
            List<string> styles = new();

            if (node is InstanceNode instance)
            {
                IBaseNodeMixin componentNode = FindNode(instance.componentId);

                if (componentNode != null)
                    component = GetStyle(componentNode).Name;
            }
            else if (node.id.Contains(';'))
            {
                string[] splits = node.id.Split(';');
                if (splits.Length >= 2)
                {
                    string componentId = splits[^1];
                    IBaseNodeMixin componentNode = FindNode(componentId);

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

            if (node.IsSvgNode())
            {
                component = null;
                styles.Clear();
            }

            classList = component.NotNullOrEmpty()
                ? styles.Count > 0 ? style.ResolveClassList(component, styles) : style.ResolveClassList(component)
                : styles.Count > 0
                    ? style.ResolveClassList(styles)
                    : style.ResolveClassList();

            if (node.IsRootNode())
                classList += $"{(string.IsNullOrEmpty(classList) ? string.Empty : " ")}{UssStyle.viewportClass.Name}";

            return $"{UssStyle.overrideClass.Name} {classList}";
        }
        UssStyle GetStyle(IBaseNodeMixin node) => componentStyleMap.TryGetValue(node, out UssStyle style) || nodeStyleMap.TryGetValue(node, out style) ? style : null;
        #endregion
    }
}