using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable MemberCanBeProtected.Global

namespace Figma
{
    public abstract class Element : MonoBehaviour, IRootElement
    {
        #region Fields
        string className;
        #endregion

        #region Properties
        public virtual int RootOrder => 0;
        public string ClassName => className.NotNullOrEmpty() ? className : GetType().Name;
        public VisualElement Root { get; private set; }
        public VisualElement[] RootsPreserved { get; private set; }
        #endregion

        #region Methods
        void IRootElement.OnInitialize(VisualElement root, VisualElement[] rootsPreserved)
        {
            Root = root;
            RootsPreserved = rootsPreserved;
            OnInitialize();
        }
        void IRootElement.OnRebuild() => OnRebuild();

        protected virtual void OnInitialize() { }
        protected virtual void OnRebuild() { }
        #endregion

        #region Base Methods
        protected virtual void Awake() => className = GetType().Name;
        protected virtual void OnEnable() => className = GetType().Name;
        protected virtual void OnDisable() { }
        #endregion
    }
}