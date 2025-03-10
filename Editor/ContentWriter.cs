using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

#pragma warning disable S1144 // Unused private types or members should be removed

namespace Figma
{
    using Core;
    using Core.Assets;
    using Core.Uss;
    using Core.Uxml;
    using Internals;
    using static Internals.Const;
    using static Internals.PathExtensions;

    internal sealed class ContentWriter
    {
        #region Const
        const int initialCollectionCapacity = 128;

        static readonly Regex multipleDashesRegex = new("-{2,}", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);
        static readonly Regex invalidCharsRegexStateNode = new("[^a-zA-Z0-9:]", RegexOptions.Compiled);
        #endregion

        #region Fields
        readonly AssetsInfo assetsInfo;
        readonly Data data;
        readonly NodeMetadata nodeMetadata;
        readonly Usages usages;

        readonly List<ComponentNode> components = new(initialCollectionCapacity);
        readonly List<Dictionary<string, Style>> componentsStyles = new(initialCollectionCapacity);
        readonly List<(StyleSlot slot, UssStyle style)> styles = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> componentStyleMap = new(initialCollectionCapacity);
        readonly Dictionary<BaseNode, UssStyle> nodeStyleMap = new(initialCollectionCapacity);
        #endregion

        #region Properties
        AssetsInfo AssetsInfo => assetsInfo;
        DocumentNode Document => data.document;
        Dictionary<string, Style> documentStyles => data.styles;
        #endregion

        #region Constructors
        internal ContentWriter(AssetsInfo assetsInfo, Data data, NodeMetadata nodeMetadata, Usages usages)
        {
            this.nodeMetadata = nodeMetadata;
            this.data = data;
            this.assetsInfo = assetsInfo;
            this.usages = usages;

            foreach (CanvasNode canvas in data.document.children)
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
        internal void Run()
        {
            AddStylesRecursively(Document, documentStyles, false);
            Document.children.ForEach(x => AddStylesRecursively(x, documentStyles, false));

            for (int i = 0; i < components.Count; i++)
                AddStylesRecursively(components[i], componentsStyles[i], true);

            InheritStylesRecursively(Document);
            Document.children.ForEach(InheritStylesRecursively);
        }
        internal void Write(string directory, string name, bool overrideGlobal = false)
        {
            RootNodes rootNodes = new(data, nodeMetadata);

            KeyValuePair<BaseNode, UssStyle>[] nodeStyleFiltered = nodeStyleMap.Where(x => IsVisible(x.Key) && (nodeMetadata.EnabledInHierarchy(x.Key) || x.Key is ComponentSetNode)).ToArray();
            UssStyle[] nodeStyleStatelessFiltered = nodeStyleFiltered.Select(x => x.Value).ToArray();
            UssStyle[] globalStaticStyles = styles.Select(x => x.style).Where(x => nodeStyleStatelessFiltered.Any(y => y.DoesInherit(x))).ToArray();

            AddTransitionStyles();

            // Writing global USS styles
            string globalUssPath = CombinePath(directory, $"{name}.{KnownFormats.uss}");

            if (overrideGlobal)
            {
                using UssWriter globalUssWriter = new(directory, globalUssPath);
                globalUssWriter.Write(UssStyle.overrideClass);
                globalUssWriter.Write(UssStyle.viewportClass);
                globalUssWriter.Write(GroupRenameStyles(globalStaticStyles));
            }

            // Writing UXML files
            UxmlBuilder builder = new(data, nodeMetadata, globalUssPath, GetClassList);
            Dictionary<string, IReadOnlyList<string>> framesPaths = new(rootNodes.Frames.Count);

            foreach (CanvasNode canvasNode in rootNodes.Canvases)
                framesPaths.Add(canvasNode.name, new List<string>());

            void WriteFrame(FrameNode frameNode)
            {
                Dictionary<string, string> templates = new();

                void FindTemplatesRecursive(BaseNode node)
                {
                    if (node is InstanceNode instanceNode)
                    {
                        Component component = data.components[instanceNode.componentId];

                        if (component == null || component.remote || string.IsNullOrEmpty(component.componentSetId))
                            return;

                        Component componentSet = data.componentSets[component.componentSetId];

                        if (componentSet == null || componentSet.remote || string.IsNullOrEmpty(componentSet.componentSetId))
                            return;

                        string template = componentSet.name;
                        templates[template] = CombinePath(directory, componentsDirectoryName, $"{template}.{KnownFormats.uxml}");
                    }
                    else if (nodeMetadata.GetTemplate(node) is (_, { } template) && template.NotNullOrEmpty())
                    {
                        templates[template] = CombinePath(directory, elementsDirectoryName, $"{template}.{KnownFormats.uxml}");
                    }

                    if (node is DefaultFrameNode frameNode)
                        frameNode.children.ForEach(FindTemplatesRecursive);
                }

                string rootDirectory = CombinePath(directory, framesDirectoryName, frameNode.parent.name);

                if (!Directory.Exists(rootDirectory))
                    Directory.CreateDirectory(rootDirectory);

                using UssWriter ussWriter = new(directory, CombinePath(rootDirectory, $"{frameNode.name}.{KnownFormats.uss}"));
                ussWriter.Write(GroupRenameStyles(GetStylesRecursive(frameNode)));

                FindTemplatesRecursive(frameNode);

                string uxmlPath = builder.CreateFrame(rootDirectory, new[] { globalUssPath, ussWriter.Path }, templates, frameNode);
                framesPaths[frameNode.parent.name].As<List<string>>().Add(uxmlPath);

                usages.RecordFiles(uxmlPath, ussWriter.Path);
                templates.Clear();
            }
            void WriteComponentSet(ComponentSetNode componentSet)
            {
                using UssWriter ussWriter = new(directory, CombinePath(directory, componentsDirectoryName, $"{componentSet.name}.{KnownFormats.uss}"));
                ussWriter.Write(GroupRenameStyles(GetStylesRecursive(componentSet)));

                string uxmlPath = builder.CreateComponentSet(CombinePath(directory, componentsDirectoryName), new[] { globalUssPath, ussWriter.Path }, componentSet);
                usages.RecordFiles(uxmlPath, ussWriter.Path);
            }
            void WriteTemplate((DefaultShapeNode element, string template) node)
            {
                (bool isHash, string hashedTemplates) = nodeMetadata.GetTemplate(node.element);

                using UssWriter ussWriter = new(directory, CombinePath(directory, elementsDirectoryName, $"{(isHash ? hashedTemplates : node.template)}.{KnownFormats.uss}"));
                ussWriter.Write(GroupRenameStyles(GetStylesRecursive(node.element)));

                string uxmlPath = builder.CreateElement(CombinePath(directory, elementsDirectoryName), new[] { globalUssPath, ussWriter.Path }, node.element, node.template);
                usages.RecordFiles(uxmlPath, ussWriter.Path);
            }

            Parallel.ForEach(rootNodes.Frames, WriteFrame);
            Parallel.ForEach(rootNodes.ComponentSets, WriteComponentSet);
            Parallel.ForEach(rootNodes.Elements, WriteTemplate);

            // Creating main document
            if (overrideGlobal)
                builder.CreateDocument(directory, name, data.document, framesPaths);
        }
        internal void AddMissingComponent(ComponentNode component, Dictionary<string, Style> componentStyles)
        {
            components.Add(component);
            componentsStyles.Add(componentStyles);
        }

        IEnumerable<UssStyle> GroupRenameStyles(IReadOnlyList<UssStyle> styles)
        {
            foreach (IGrouping<string, UssStyle> group in styles.GroupBy(x => x.Name).Where(y => y.Count() > 1))
            {
                int i = 0;
                foreach (UssStyle style in group)
                    style.Name += "-" + (i++ + 1).NumberToWords();
            }

            return styles;
        }
        void AddTransitionStyles()
        {
            List<UssStyle> GetStylesRecursive(BaseNode node, List<UssStyle> styles = null)
            {
                styles ??= new List<UssStyle>();

                if (componentStyleMap.TryGetValue(node, out UssStyle style))
                    styles.Add(style);

                if (node is not IChildrenMixin nodeWithChildren)
                    return styles;

                foreach (SceneNode child in nodeWithChildren.children)
                {
                    if (child is ComponentSetNode)
                        continue;

                    GetStylesRecursive(child, styles);
                }

                return styles;
            }
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

                void InjectSubStyles(ComponentNode node, List<UssStyle> defaultStyles, PseudoClass pseudoClass)
                {
                    List<UssStyle> styles = GetStylesRecursive(node);
                    for (int i = 0; i < styles.Count; i++)
                    {
                        UssStyle style = styles[i];
                        UssStyle defaultStyle = defaultStyles[i];
                        componentSetStyle.SubStyles.Add(new UssStyle(componentSetStyle.Name) { PseudoClass = pseudoClass, Target = defaultStyle }.CopyFrom(style));
                    }
                }

                if (action.transition != null && action.transition.type == TransitionType.SMART_ANIMATE)
                {
                    ComponentNode hoverNode = GetTransitionNode(componentSet, defaultComponent, TriggerType.ON_HOVER);
                    ComponentNode clickNode = GetTransitionNode(componentSet, defaultComponent, TriggerType.ON_CLICK);
                    List<UssStyle> defaultStyles = GetStylesRecursive(defaultComponent);

                    if (hoverNode != null) InjectSubStyles(hoverNode, defaultStyles, PseudoClass.Hover);
                    if (clickNode != null) InjectSubStyles(clickNode, defaultStyles, PseudoClass.Active);
                }

                if (action.transition is { type: TransitionType.DISSOLVE })
                    componentSetStyle.SubStyles.AddRange(UssStyle.MakeTransitionStyles(componentSetStyle, idleStyle, hoverStyle, clickStyle));
            }
        }
        void AddMissingNodesRecursively(BaseNode node)
        {
            if (node is InstanceNode instance && FindNode(instance.componentId) == null)
                usages.MissingComponents.Add(instance.componentId);

            if (node is IChildrenMixin parent)
                parent.children.ForEach(AddMissingNodesRecursively);
        }
        void AddImageFillsRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node) || node is BooleanOperationNode)
                return;

            if (!IsSvgNode(node) && HasImageFill(node))
                usages.ImageFillNodes.Add(node);

            if (node is IChildrenMixin children)
                children.children.ForEach(AddImageFillsRecursively);
        }
        void AddPngNodesRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                return;

            if (IsSvgNode(node) && HasImageFill(node))
                usages.PngNodes.Add(node);

            if (node is BooleanOperationNode)
                return;

            if (node is IChildrenMixin children)
                children.children.ForEach(AddPngNodesRecursively);
        }
        void AddSvgNodesRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                return;

            if (IsSvgNode(node) && !HasImageFill(node))
                usages.SvgNodes.Add(node);

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case IChildrenMixin children:
                    children.children.ForEach(AddSvgNodesRecursively);
                    return;
            }
        }
        void AddGradientsRecursively(BaseNode node)
        {
            if (!IsVisible(node) || !nodeMetadata.EnabledInHierarchy(node))
                return;

            switch (node)
            {
                case BooleanOperationNode:
                    return;

                case IGeometryMixin geometry:
                    geometry.fills.OfType<GradientPaint>().ForEach(x => usages.Gradients.TryAdd(x.GetHash(), x));
                    break;
            }

            if (node is not IChildrenMixin children)
                return;

            children.children.ForEach(AddGradientsRecursively);
        }
        void AddStylesRecursively(BaseNode node, Dictionary<string, Style> styles, bool insideComponent)
        {
            string GetClassName(string name, string prefix = "n")
            {
                if (name.Length > 64)
                    name = name[..64];

                name = (invalidCharsRegex).Replace(name, "-");
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

            if (node is IChildrenMixin children)
                children.children.ForEach(child => AddStylesRecursively(child, styles, insideComponent));
        }

        BaseNode FindNode(string id)
        {
            BaseNode Find(BaseNode root)
            {
                if (root is not IChildrenMixin children)
                    return null;

                foreach (SceneNode child in children.children)
                {
                    if (child.id == id)
                        return child;

                    BaseNode node = Find(child);

                    if (node != null)
                        return node;
                }

                return null;
            }

            if (Document.id == id)
                return Document;

            foreach (CanvasNode canvas in Document.children)
            {
                if (canvas.id == id)
                    return canvas;

                BaseNode node = Find(canvas);

                if (node != null)
                    return node;
            }

            return null;
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

                    if (documentStyles.TryGetValue(id, out Style documentStyle))
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

            if (node is BooleanOperationNode)
                return;

            if (node is IChildrenMixin children)
                children.children.ForEach(InheritStylesRecursively);
        }
        UssStyle GetStyle(BaseNode node)
        {
            if (componentStyleMap.TryGetValue(node, out UssStyle style))
                return style;

            return
                nodeStyleMap.TryGetValue(node, out style) ? style : null;
        }
        string GetClassList(BaseNode node)
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
                component = null;
                styles.Clear();
            }

            if (component.NotNullOrEmpty() && styles.Count > 0) classList = style.ResolveClassList(component, styles);
            else if (component.NotNullOrEmpty()) classList = style.ResolveClassList(component);
            else if (styles.Count > 0) classList = style.ResolveClassList(styles);
            else classList = style.ResolveClassList();

            if (IsRootNode(node))
                classList += $"{(string.IsNullOrEmpty(classList) ? string.Empty : " ")}{UssStyle.viewportClass.Name}";

            return $"{UssStyle.overrideClass.Name} {classList}";
        }
        #endregion

        #region Support Methods
        internal static bool IsRootNode(IBaseNodeMixin mixin) => mixin is DocumentNode or CanvasNode or ComponentNode || mixin.parent is CanvasNode or ComponentNode;
        internal static bool IsVisible(IBaseNodeMixin mixin) => (mixin is not ISceneNodeMixin scene || !scene.visible.HasValueAndFalse()) && (mixin.parent == null || IsVisible(mixin.parent));
        internal static bool HasImageFill(IBaseNodeMixin mixin) => mixin is IGeometryMixin geometry && geometry.fills.Any(x => x is ImagePaint);
        internal static bool IsSvgNode(IBaseNodeMixin mixin) => mixin is LineNode or EllipseNode or RegularPolygonNode or StarNode or VectorNode || (mixin is BooleanOperationNode && IsBooleanOperationVisible(mixin));
        internal static bool IsBooleanOperationVisible(IBaseNodeMixin node)
        {
            if (node is not IChildrenMixin children)
                return false;

            foreach (SceneNode child in children.children)
            {
                if (child is not BooleanOperationNode && IsVisible(child) && IsSvgNode(child))
                    return true;
                if (child is BooleanOperationNode)
                    return IsBooleanOperationVisible(child);
            }

            return false;
        }
        List<UssStyle> GetStylesRecursive(BaseNode node, List<UssStyle> styles = null)
        {
            styles ??= new List<UssStyle>();

            if (componentStyleMap.TryGetValue(node, out UssStyle componentStyle))
                styles.Add(componentStyle);

            if (nodeStyleMap.TryGetValue(node, out UssStyle nodeStyle))
                styles.Add(nodeStyle);

            if (node is not IChildrenMixin nodeWithChildren)
                return styles;

            foreach (SceneNode child in nodeWithChildren.children)
                GetStylesRecursive(child, styles);

            return styles;
        }
        #endregion
    }
}