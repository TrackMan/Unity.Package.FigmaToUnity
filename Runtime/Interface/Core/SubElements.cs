using UnityEngine.UIElements;

namespace Figma
{
    public abstract class SubVisualElement : VisualElement, ISubElement
    {
        #region Methods
        protected virtual void OnInitialize() { }
        protected virtual void OnRebuild() { }

        void ISubElement.OnInitialize() => OnInitialize();
        void ISubElement.OnRebuild() => OnRebuild();
        #endregion
    }

    public abstract class SubButton : Button, ISubElement
    {
        #region Methods
        protected virtual void OnInitialize() { }
        protected virtual void OnRebuild() { }

        void ISubElement.OnInitialize() => OnInitialize();
        void ISubElement.OnRebuild() => OnRebuild();
        #endregion
    }

    public abstract class SubLabel : Label, ISubElement
    {
        #region Methods
        protected virtual void OnInitialize() { }
        protected virtual void OnRebuild() { }

        void ISubElement.OnInitialize() => OnInitialize();
        void ISubElement.OnRebuild() => OnRebuild();
        #endregion
    }

    public abstract class SubScrollView : ScrollView, ISubElement
    {
        #region Methods
        protected virtual void OnInitialize() { }
        protected virtual void OnRebuild() { }

        void ISubElement.OnInitialize() => OnInitialize();
        void ISubElement.OnRebuild() => OnRebuild();
        #endregion
    }
}