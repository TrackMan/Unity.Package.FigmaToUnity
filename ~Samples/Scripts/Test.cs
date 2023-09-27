using Figma.Attributes;
using UnityEngine;

namespace Figma.Samples
{
    [Uxml("")]
    [AddComponentMenu("Figma/Samples/Test")]
    public class Test : Element
    {
        #region Fields
        //[Query("avatar", ImageFiltering = ElementDownloadImage.Ignore)] Image avatar;
        //[Query("title")] Label label;
        #endregion

        #region Methods
        void Update()
        {
            //if (label is not null) label.text = Time.realtimeSinceStartup.ToString("F2");
        }
        #endregion
    }
}