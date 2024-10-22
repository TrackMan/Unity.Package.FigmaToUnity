using UnityEngine.UIElements;

namespace Figma
{
    public interface ISubElement
    {
        #region Methods
        void OnInitialize() { } // Method is blank intentionally
        void OnRebuild() { } // Method is blank intentionally
        #endregion
    }

    public interface IRootElement
    {
        #region Properties
        VisualElement Root { get; }
        int RootOrder => 0;
        #endregion

        #region Methods
        void OnInitialize(VisualElement root, VisualElement[] rootsPreserved) { } // Method is blank intentionally 
        void OnRebuild() { } // Method is blank intentionally
        #endregion
    }
}