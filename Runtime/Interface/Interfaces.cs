using UnityEngine.UIElements;

namespace Figma
{
    public interface ISubElement
    {
        #region Methods
        void OnInitialize()
        {
            /* Method intentionally left empty. */
        }
        void OnRebuild()
        {
            /* Method intentionally left empty. */
        }
        #endregion
    }

    public interface IRootElement
    {
        #region Properties
        VisualElement Root { get; }
        int RootOrder => 0;
        #endregion

        #region Methods
        void OnInitialize(VisualElement root, VisualElement[] rootsPreserved)
        {
            /* Method intentionally left empty. */
        }
        void OnRebuild()
        {
            /* Method intentionally left empty. */
        }
        #endregion
    }
}