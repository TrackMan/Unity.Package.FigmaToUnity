using System;
using System.Collections;
using System.Collections.Generic;
using Trackman;
using UnityEngine.UIElements;

namespace Figma
{
    public static class VisualElementExtensions
    {
        #region Fields
        static Dictionary<(VisualElement prefab, VisualElement parent), IList> cloneDictionary = new();
        #endregion

        #region Constructors
        static VisualElementExtensions() => DisposeStatic.OnDisposeStatic += () => cloneDictionary.Clear();
        #endregion

        #region Methods
        public static bool HasVisibility(this VisualElement element) => element.style.visibility == Visibility.Visible;
        public static void MakeVisible(this VisualElement element) => element.style.visibility = Visibility.Visible;
        public static void MakeInvisible(this VisualElement element) => element.style.visibility = Visibility.Hidden;
        public static void SetVisibility(this VisualElement element, bool visible) => element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        public static bool IsShowing(this VisualElement element) => element.resolvedStyle.display == DisplayStyle.Flex;
        public static void Show(this VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
            element.MarginMe();
        }
        public static void Hide(this VisualElement element) => element.style.display = DisplayStyle.None;
        public static void SetDisplay(this VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) element.MarginMe();
        }
        public static void Disable(this VisualElement element) => element.pickingMode = PickingMode.Ignore;
        public static void Enable(this VisualElement element) => element.pickingMode = PickingMode.Position;

        public static IList EnsureList<TVisualElement>(TVisualElement prefab, VisualElement parent) where TVisualElement : VisualElement
        {
            (TVisualElement prefab, VisualElement parent) identifier = (prefab, parent);

            if (!cloneDictionary.TryGetValue(identifier, out IList elements))
                cloneDictionary.Add(identifier, elements = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(prefab.GetType())));

            return elements;
        }
        public static TVisualElement GetElement<TVisualElement>(this TVisualElement prefab, int index) where TVisualElement : VisualElement => GetElements(prefab, prefab.parent)[index];
        public static List<TVisualElement> GetElements<TVisualElement>(this TVisualElement prefab) where TVisualElement : VisualElement => GetElements(prefab, prefab.parent);
        public static List<TVisualElement> GetElements<TVisualElement>(this TVisualElement prefab, VisualElement parent) where TVisualElement : VisualElement
        {
            if (EnsureList(prefab, parent) is List<TVisualElement> list) return list;

            throw new ArgumentException($"Casting from {typeof(List<TVisualElement>)} to {cloneDictionary[(prefab, parent)]}");
        }
        public static List<VisualElement> GetElements(this VisualElement prefab) => GetElements<VisualElement>(prefab);
        public static List<VisualElement> GetElements(this VisualElement prefab, VisualElement parent) => GetElements<VisualElement>(prefab, parent);
        public static void Sync<TVisualElement, TData>(this TVisualElement prefab, VisualElement parent, IEnumerable<TData> data, Action<TVisualElement> onCreateElement = default) where TVisualElement : VisualElement, ISyncElement<TData>
        {
            IList elements = EnsureList(prefab, parent);

            int i = 0;
            foreach (TData value in data)
            {
                TVisualElement element;
                if (i >= elements.Count)
                {
                    element = prefab.Clone(parent, i);
                    element.Initialize(i);
                    onCreateElement?.Invoke(element);
                    elements.Add(element);
                }
                else
                {
                    element = (TVisualElement)elements[i];
                }

                if (element.IsVisible(i, value)) element.Show();
                else element.Hide();

                ++i;
            }

            for (int j = i; j < elements.Count; ++j) elements[j].As<TVisualElement>().Hide();
        }
        public static void Sync<TVisualElement, TCreationData, TData>(this TVisualElement prefab, VisualElement parent, TCreationData creationData, IEnumerable<TData> data, Action<TVisualElement> onCreateElement = default) where TVisualElement : VisualElement, ISyncElement<TCreationData, TData>
        {
            IList elements = EnsureList(prefab, parent);

            int i = 0;
            foreach (TData value in data)
            {
                TVisualElement element;
                if (i >= elements.Count)
                {
                    element = prefab.Clone(parent, i);
                    element.Initialize(i, creationData);
                    onCreateElement?.Invoke(element);
                    elements.Add(element);
                }
                else
                {
                    element = (TVisualElement)elements[i];
                }

                if (element.IsVisible(i, value)) element.Show();
                else element.Hide();

                ++i;
            }

            for (int j = i; j < elements.Count; ++j) elements[j].As<TVisualElement>().Hide();
        }
        #endregion
    }
}