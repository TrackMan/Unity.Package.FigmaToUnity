using System;
using System.Diagnostics;
using UnityEngine.UIElements;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global

namespace Figma.Attributes
{
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Field)]
    public class QueryAttribute : Attribute
    {
        #region Properties
        public string Path { get; }
        public string ClassName { get; }
        public ElementDownloadImage ImageFiltering { get; set; }
        public string ReplaceElementPath { get; set; }
        public string RebuildElementEvent { get; set; }
        public bool StartRoot { get; set; }
        public bool EndRoot { get; set; }
        public bool Nullable { get; set; }
        public bool Hide { get; set; }
        public bool Localize { get; set; } = true;
        public string Clicked { get; set; }
        public string Template { get; set; }
        public TrickleDown UseTrickleDown { get; set; }
        public string MouseCaptureOutEvent { get; set; }
        public string MouseCaptureEvent { get; set; }
        public string ChangeEvent { get; set; }
        public string ValidateCommandEvent { get; set; }
        public string ExecuteCommandEvent { get; set; }
        public string DragExitedEvent { get; set; }
        public string DragUpdatedEvent { get; set; }
        public string DragPerformEvent { get; set; }
        public string DragEnterEvent { get; set; }
        public string DragLeaveEvent { get; set; }
        public string FocusOutEvent { get; set; }
        public string BlurEvent { get; set; }
        public string FocusInEvent { get; set; }
        public string FocusEvent { get; set; }
        public string InputEvent { get; set; }
        public string KeyDownEvent { get; set; }
        public string KeyUpEvent { get; set; }
        public string GeometryChangedEvent { get; set; }
        public string PointerDownEvent { get; set; }
        public string PointerUpEvent { get; set; }
        public string PointerMoveEvent { get; set; }
        public string MouseDownEvent { get; set; }
        public string MouseUpEvent { get; set; }
        public string MouseMoveEvent { get; set; }
        public string ContextClickEvent { get; set; }
        public string WheelEvent { get; set; }
        public string MouseEnterEvent { get; set; }
        public string MouseLeaveEvent { get; set; }
        public string MouseEnterWindowEvent { get; set; }
        public string MouseLeaveWindowEvent { get; set; }
        public string MouseOverEvent { get; set; }
        public string MouseOutEvent { get; set; }
        public string ContextualMenuPopulateEvent { get; set; }
        public string AttachToPanelEvent { get; set; }
        public string DetachFromPanelEvent { get; set; }
        public string TooltipEvent { get; set; }
        public string IMGUIEvent { get; set; }
        #endregion

        #region Constructors
        public QueryAttribute(string path, string className = default)
        {
            Path = path;
            ClassName = className;
        }
        #endregion
    }
}