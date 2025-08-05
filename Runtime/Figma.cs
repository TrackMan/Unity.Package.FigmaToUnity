using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Figma
{
    using Attributes;
    using System.Threading.Tasks;

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
        void OnEnable()
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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            OnEnableNextFrameAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            return;

            // Do not make MonoBehaviour.OnEnable async because Unity calls async methods after non-async MonoBehaviour methods
            async Task OnEnableNextFrameAsync()
            {
                // Do not change this to Awaiters, since it is breaking the loading.
                if (waitFrameBeforeRebuild)
                    await new WaitForEndOfFrame();

                VisualElementMetadata.Rebuild(elements);

                VisualElement root = document.rootVisualElement.Q(UxmlAttribute.prefix);

                if (root is null || !reorder)
                    return;

                foreach (IRootElement element in elements.Where(x => x.Root is not null).OrderBy(x => x.RootOrder))
                {
                    element.Root.RemoveFromHierarchy();
                    root.Add(element.Root);
                }
            }
        }
        void OnDestroy() => VisualElementMetadata.Dispose();
        #endregion
    }
}