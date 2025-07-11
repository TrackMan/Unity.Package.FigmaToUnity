﻿using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma
{
    using Attributes;

    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(UIDocument))]
    public class Figma : MonoBehaviour
    {
        #region Fields
        [SerializeField] string fileKey;
        [SerializeField] bool filter;
        [SerializeField] bool reorder;
        [SerializeField] bool waitFrameBeforeRebuild = true;
        [SerializeField] string[] fontDirectories;
        #endregion

        #region Properties
        public string FileKey => fileKey;
        public bool Filter => filter;
        #endregion

        #region Base Methods
        async void OnEnable()
        {
            if (Application.isBatchMode) 
                return;

            UIDocument document = GetComponent<UIDocument>();
            
            if (document.rootVisualElement is null) 
                return;

            IRootElement[] elements = GetComponentsInChildren<IRootElement>();
            VisualElementMetadata.Initialize(document, elements);

            if (!Application.isPlaying) 
                return;

            // Do not change this to Awaiters, since it is breaking the loading.
            if (waitFrameBeforeRebuild)
                await new WaitForEndOfFrame();
            
            VisualElementMetadata.Rebuild(elements);

            VisualElement root = document.rootVisualElement.Q(UxmlAttribute.prefix);
            
            if (root is null || !reorder) 
                return;

            foreach (IRootElement element in elements.OrderBy(x => x.RootOrder))
            {
                if (element.Root is null) 
                    continue;

                element.Root.RemoveFromHierarchy();
                root.Add(element.Root);
            }
        }
        void OnDestroy() => VisualElementMetadata.Dispose();
        #endregion
    }
}