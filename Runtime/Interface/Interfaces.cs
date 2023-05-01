using UnityEngine.UIElements;

namespace Figma
{
    public interface ISubElement
    {
        #region Methods
        void OnInitialize() { }
        void OnRebuild() { }
        #endregion
    }

    public interface IRootElement
    {
        #region Properties
        VisualElement Root { get; }
        int RootOrder { get => 0; }
        #endregion

        #region Methods
        void OnInitialize(VisualElement root, VisualElement[] rootsPreserved) { }
        void OnRebuild() { }
        #endregion
    }
}