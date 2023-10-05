using System.Collections.Generic;
using UnityEngine.UIElements;

#pragma warning disable S1186 // Functions and closures should not be empty

namespace Figma
{
    public interface ISyncElement<TData>
    {
        #region Methods
        void Sync(VisualElement parent, IEnumerable<TData> data);
        void Initialize(int index) { }
        bool IsVisible(int index, TData data);
        #endregion
    }

    public interface ISyncElement<TCreationData, TData>
    {
        #region Methods
        void Sync(VisualElement parent, TCreationData creationData, IEnumerable<TData> data);
        void Initialize(int index, TCreationData creationData);
        bool IsVisible(int index, TData data);
        #endregion
    }
}