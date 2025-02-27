using Figma.Internals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Figma.Core.Uss
{
    internal abstract class BaseUssStyle
    {
        #region Fields
        readonly List<BaseUssStyle> inherited = new();
        #endregion

        #region Properties
        public string Name { get; set; }
        public PseudoClass PseudoClass { get; set; }
        public BaseUssStyle Target { get; set; }

        public List<BaseUssStyle> SubStyles { get; } = new();
        public Dictionary<string, string> Attributes { get; } = new();
        public bool HasAttributes => Attributes.Count > 0;
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

            if (Target == null)
                return result;

            result += " > ";

            if (Target.Name is not (nameof(UnityEngine.UIElements.VisualElement) or
                                    nameof(UnityEngine.UIElements.Button)))
                result += ".";

            result += Target.Name;

            return result;
        }

        public bool DoesInherit(BaseUssStyle style) => inherited.Contains(style);
        public void Inherit(BaseUssStyle component)
        {
            inherited.Add(component);
            component.Attributes.Keys.Where(key => Attributes.TryGetValue(key, out string value) && value == component.Attributes[key])
                     .ForEach(x => Attributes.Remove(x));
        }
        public void Inherit(IReadOnlyCollection<BaseUssStyle> styles)
        {
            inherited.AddRange(styles);
            styles.SelectMany(style => style.Attributes.Where(keyValue => Attributes.TryGetValue(keyValue.Key, out string value) && value == style.Attributes[keyValue.Key]))
                  .Select(x => x.Key)
                  .ForEach(key => Attributes.Remove(key));
        }
        public void Inherit(BaseUssStyle component, IReadOnlyCollection<BaseUssStyle> styles)
        {
            inherited.Add(component);
            inherited.AddRange(styles);

            List<string> preserve = (from keyValue in component.Attributes
                                     from style in styles
                                     where style.Attributes.ContainsKey(keyValue.Key) && style.Attributes[keyValue.Key] != keyValue.Value
                                     select keyValue.Key).ToList();

            component.Attributes.Keys.Where(key => Attributes.ContainsKey(key) && Attributes[key] == component.Attributes[key]).ForEach(key => Attributes.Remove(key));
            styles.SelectMany(style => style.Attributes.Where(keyValue => Attributes.ContainsKey(keyValue.Key) &&
                                                                          Attributes[keyValue.Key] == style.Attributes[keyValue.Key] &&
                                                                          !preserve.Contains(keyValue.Key)))
                  .Select(x => x.Key)
                  .ForEach(x => Attributes.Remove(x));
        }
        public string ResolveClassList(string component) => Attributes.Count > 0 ? $"{Name} {component}" : component;
        public string ResolveClassList(IEnumerable<string> styles) => Attributes.Count > 0 ? $"{Name} {string.Join(" ", styles)}" : string.Join(" ", styles);
        public string ResolveClassList(string component, IEnumerable<string> styles) => Attributes.Count > 0 ? $"{Name} {component} {string.Join(" ", styles)}" : $"{component} {string.Join(" ", styles)}";
        public string ResolveClassList() => Attributes.Count > 0 ? Name : string.Empty;
        #endregion

        #region Support Methods
        protected bool Has(string name) => Attributes.ContainsKey(name);
        protected string Get(string name) => Attributes[name];
        protected string GetDefault(string name, string defaultValue) => Attributes.ContainsKey(name) ? Attributes[name] : defaultValue;
        protected string Get1(string name, string group, int index)
        {
            if (Attributes.TryGetValue(group, out string groupValue))
            {
                Length4Property length4 = groupValue;
                return length4[index];
            }

            if (Attributes.TryGetValue(name, out string nameValue))
                return nameValue;

            throw new NotSupportedException();
        }
        protected string Get4(string name, params string[] names)
        {
            if (Attributes.TryGetValue(name, out string value))
                return value;

            LengthProperty[] properties = new LengthProperty[4];

            for (int i = 0; i < 4; ++i)
                properties[i] = Attributes.TryGetValue(names[i], out string indexedValue) ? indexedValue : new LengthProperty(Unit.Pixel);

            return new Length4Property(properties);
        }
        protected void Set(string name, string value) => Attributes[name] = value;
        protected void Set1(string name, string value, params string[] names)
        {
            Attributes[name] = value;

            for (int i = 0; i < 4; ++i)
                Attributes.Remove(names[i]);
        }
        protected void Set4(string name, string value, string group, int index)
        {
            if (Attributes.TryGetValue(group, out string item))
            {
                Length4Property length4 = item;
                length4[index] = value;
                Set(group, length4);
            }
            else
                Set(name, value);
        }
        protected static string Url(string url) => $"url('{url}')";
        protected static string Resource(string resource) => $"resource('{resource}')";
        #endregion
    }
}