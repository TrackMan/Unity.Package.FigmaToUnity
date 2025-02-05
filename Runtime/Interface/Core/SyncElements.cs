using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Figma
{
    public abstract class SyncVisualElement<T> : SubVisualElement, ISyncElement<T>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, IEnumerable<T> data) => VisualElementExtensions.Sync(this, parent, data);
        public abstract bool IsVisible(int index, T data);
        #endregion
    }

    public abstract class SyncVisualElement<TCreationData, TData> : SubVisualElement, ISyncElement<TCreationData, TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, TCreationData creationData, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, creationData, data);
        public abstract void Initialize(int index, TCreationData creationData);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncButton<TData> : SubButton, ISyncElement<TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, data);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncButton<TCreationData, TData> : SubButton, ISyncElement<TCreationData, TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, TCreationData creationData, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, creationData, data);
        public abstract void Initialize(int index, TCreationData creationData);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncButtonSimple<TData> : SubButton, ISyncElement<Action<int>, TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, Action<int> creationData, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, creationData, data);
        public virtual void Initialize(int index, Action<int> creationData) => clicked += () => creationData(index);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncLabel<TData> : SubLabel, ISyncElement<TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, data);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncLabel<TCreationData, TData> : SubLabel, ISyncElement<TCreationData, TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, TCreationData creationData, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, creationData, data);
        public abstract void Initialize(int index, TCreationData creationData);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncScrollView<TData> : SubScrollView, ISyncElement<TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, data);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }

    public abstract class SyncScrollView<TCreationData, TData> : SubScrollView, ISyncElement<TCreationData, TData>
    {
        #region Methods
        public virtual void Sync(VisualElement parent, TCreationData creationData, IEnumerable<TData> data) => VisualElementExtensions.Sync(this, parent, creationData, data);
        public abstract void Initialize(int index, TCreationData creationData);
        public abstract bool IsVisible(int index, TData data);
        #endregion
    }
}