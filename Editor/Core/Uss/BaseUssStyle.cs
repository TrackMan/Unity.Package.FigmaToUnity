using System;
using System.Collections.Generic;
using System.Linq;

namespace Figma.Core.Uss
{
    internal abstract class BaseUssStyle
    {
        #region Fields
        protected readonly List<BaseUssStyle> subStyles = new();
        protected readonly List<BaseUssStyle> inherited = new();
        protected readonly Dictionary<string, string> defaults = new();
        protected readonly Dictionary<string, string> attributes = new();
        #endregion

        #region Properties
        public string Name { get; set; }
        public PseudoClass PseudoClass { get; set; }
        public BaseUssStyle Target { get; set; }

        public bool HasAttributes => attributes.Count > 0;
        public List<BaseUssStyle> SubStyles => subStyles;
        public Dictionary<string, string> Attributes => attributes;
        #endregion

        #region Constructors
        protected BaseUssStyle(string name) => Name = name;
        #endregion

        #region Methods
        public string BuildName()
        {
            string result = $".{Name}";
            
            if (PseudoClass is not PseudoClass.None)
                result += $":{PseudoClass.ToString().ToLower()}";

            if (Target is not null)
            {
                result += " > ";

                if (Target.Name is not (nameof(UnityEngine.UIElements.VisualElement) or 
                                        nameof(UnityEngine.UIElements.Button)))
                    result += ".";

                result += Target.Name;
            }

            return result;
        }

        public bool DoesInherit(BaseUssStyle style) => inherited.Contains(style);
        public void Inherit(BaseUssStyle component)
        {
            inherited.Add(component);

            foreach (string key in component.attributes.Keys)
            {
                if (attributes.TryGetValue(key, out string value) && value == component.attributes[key])
                    attributes.Remove(key);

                if (!attributes.ContainsKey(key) && defaults.TryGetValue(key, out string defaultValue))
                    attributes.Add(key, defaultValue);
            }
        }
        public void Inherit(IReadOnlyCollection<BaseUssStyle> styles)
        {
            inherited.AddRange(styles);

            foreach (BaseUssStyle style in styles)
                foreach ((string key, string _) in style.attributes.Where(keyValue => attributes.TryGetValue(keyValue.Key, out string value) && value == style.attributes[keyValue.Key]))
                    attributes.Remove(key);
        }
        public void Inherit(BaseUssStyle component, IReadOnlyCollection<BaseUssStyle> styles)
        {
            inherited.Add(component);
            inherited.AddRange(styles);

            List<string> preserve = (from keyValue in component.attributes
                                     from style in styles
                                     where style.attributes.ContainsKey(keyValue.Key) && style.attributes[keyValue.Key] != keyValue.Value
                                     select keyValue.Key).ToList();

            foreach ((string key, string _) in component.attributes)
            {
                if (attributes.ContainsKey(key) && attributes[key] == component.attributes[key])
                    attributes.Remove(key);

                if (!attributes.ContainsKey(key) && defaults.TryGetValue(key, out string @default))
                    attributes.Add(key, @default);
            }

            foreach (BaseUssStyle style in styles)
            {
                foreach (KeyValuePair<string, string> keyValue in style.attributes.Where(keyValue => attributes.ContainsKey(keyValue.Key) && attributes[keyValue.Key] == style.attributes[keyValue.Key] && !preserve.Contains(keyValue.Key)))
                    attributes.Remove(keyValue.Key);
            }
        }
        public string ResolveClassList(string component) => attributes.Count > 0 ? $"{Name} {component}" : component;
        public string ResolveClassList(IEnumerable<string> styles) => attributes.Count > 0 ? $"{Name} {string.Join(" ", styles)}" : string.Join(" ", styles);
        public string ResolveClassList(string component, IEnumerable<string> styles) => attributes.Count > 0 ? $"{Name} {component} {string.Join(" ", styles)}" : $"{component} {string.Join(" ", styles)}";
        public string ResolveClassList() => attributes.Count > 0 ? Name : string.Empty;
        #endregion

        #region Support Methods
        protected bool Has(string name) => attributes.ContainsKey(name);
        protected string Get(string name) => attributes[name];
        protected string Get1(string name, string group, int index)
        {
            if (attributes.TryGetValue(group, out string groupValue))
            {
                Length4Property length4 = groupValue;
                return length4[index];
            }

            if (attributes.TryGetValue(name, out string nameValue)) return nameValue;

            throw new NotSupportedException();
        }
        protected string Get4(string name, params string[] names)
        {
            if (attributes.TryGetValue(name, out string value)) return value;

            LengthProperty[] properties = new LengthProperty[4];
            for (int i = 0; i < 4; ++i)
                if (attributes.TryGetValue(names[i], out string indexedValue))
                    properties[i] = indexedValue;
                else
                    properties[i] = new LengthProperty(Unit.Pixel);

            return new Length4Property(properties);
        }
        protected void Set(string name, string value) => attributes[name] = value;
        protected void Set1(string name, string value, params string[] names)
        {
            attributes[name] = value;

            for (int i = 0; i < 4; ++i)
                attributes.Remove(names[i]);
        }
        protected void Set4(string name, string value, string group, int index)
        {
            if (attributes.TryGetValue(group, out string item))
            {
                Length4Property length4 = item;
                length4[index] = value;
                Set(group, length4);
            }
            else
                Set(name, value);
        }

        protected string Url(string url) => $"url('{url}')";
        #endregion
    }
}