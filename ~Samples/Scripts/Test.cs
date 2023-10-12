using Figma.Attributes;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Figma.Samples
{
    [Uxml("TestPage/TestFrame", UxmlDownloadImages.Everything, UxmlElementTypeIdentification.ByElementType)]
    [AddComponentMenu("Figma/Samples/Test")]
    public class Test : Element
    {
        const int minCircles = 1;
        const int maxCircles = 7;

        #region Fields
        [Query("Header")] Label header;

        [Query("CloneButton", Clicked = nameof(Clone))] Button cloneButton;
        [Query("RemoveButton", Clicked = nameof(Remove))] Button removeButton;
        [Query("CloneContainer", StartRoot = true)] VisualElement cloneContainer;
        [Query("CloneCircle", EndRoot = true)] PerfectCircle cloneCircle;

        [Query("SyncButton", Clicked = nameof(Sync))] Button syncButton;
        [Query("SyncContainer")] VisualElement syncContainer;
        [Query("SyncContainer/SyncCircle")] PerfectCircle syncCircle;

        [Query("FunctionDescription", Hide = true)] Label functionDescription;
        #endregion

        #region Methods
        protected override void OnInitialize() => cloneContainer.style.flexWrap = Wrap.NoWrap;
        protected override void OnRebuild() => header.text = "Welcome to Figma Test Frame!";

        void Clone()
        {
            if (cloneContainer.childCount == maxCircles) return;

            cloneCircle.Clone(cloneContainer);
        }
        void Remove()
        {
            if (cloneContainer.childCount == minCircles) return;

            cloneContainer.Remove(cloneContainer.Children().First());
        }
        void Sync()
        {
            void RandomColor(int index) => syncContainer.Children().ElementAt(index).style.backgroundColor = Random.ColorHSV();

            syncCircle.Sync(syncContainer, RandomColor, Enumerable.Range(0, Random.Range(1, maxCircles + 1)));
            syncCircle.Hide();

            functionDescription.Show();
        }
        #endregion
    }

    public class PerfectCircle : SyncButtonSimple<int>
    {
        public new class UxmlFactory : UxmlFactory<PerfectCircle> { }

        #region Methods
        public override bool IsVisible(int index, int data) => true;
        #endregion
    }
}