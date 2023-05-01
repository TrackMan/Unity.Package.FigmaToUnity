using Trackman;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma
{
    public abstract class Element : MonoBehaviour, IRootElement
    {
        #region Fields
        string className;
        VisualElement root;
        VisualElement[] rootsPreserved;
        #endregion

        #region Properties
        public virtual int RootOrder => 0;
        public string ClassName => className.NotNullOrEmpty() ? className : GetType().Name;
        public VisualElement Root => root;
        public VisualElement[] RootsPreserved => rootsPreserved;
        #endregion

        #region Methods
        void IRootElement.OnInitialize(VisualElement root, VisualElement[] rootsPreserved)
        {
            this.root = root;
            this.rootsPreserved = rootsPreserved;
            OnInitialize();
        }
        void IRootElement.OnRebuild() => OnRebuild();

        protected virtual void OnInitialize()
        {
        }
        protected virtual void OnRebuild()
        {
        }
        #endregion

        #region Base Methods
        protected virtual void Awake()
{
            className = GetType().Name;
        }
        protected virtual void OnEnable()
        {
            className = GetType().Name;
        }
        protected virtual void OnDisable()
        {
        }
        #endregion
    }
}