using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace Figma.Attributes
{
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class)]
    public class UxmlAttribute : Attribute
    {
        public const string prefix = "Document";

        #region Fields
        string root;
        string documentRoot;
        string[] preserve;
        string[] documentPreserve;
        UxmlDownloadImages imageFiltering;
        UxmlElementTypeIdentification typeIdentification;
        #endregion

        #region Properties
        public string Root => root;
        public string DocumentRoot => documentRoot;
        public string[] Preserve => preserve;
        public string[] DocumentPreserve => documentPreserve;
        public UxmlDownloadImages ImageFiltering => imageFiltering;
        public UxmlElementTypeIdentification TypeIdentification => typeIdentification;
        #endregion

        #region Constructors
        public UxmlAttribute(string root = default, UxmlDownloadImages imageFiltering = UxmlDownloadImages.Everything, UxmlElementTypeIdentification typeIdentification = UxmlElementTypeIdentification.ByName, params string[] preserve)
        {
            this.root = root;
            this.documentRoot = $"{prefix}/{root}";
            this.preserve = preserve;
            this.documentPreserve = (string[])preserve.Clone();
            for (int i = 0; i < preserve.Length; ++i) this.documentPreserve[i] = $"{prefix}/{this.documentPreserve[i]}";
            this.imageFiltering = imageFiltering;
            this.typeIdentification = typeIdentification;
        }
        #endregion
    }

    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Field)]
    public class QueryAttribute : Attribute
    {
        #region Fields
        string path;
        string className;

        ElementDownloadImage imageFiltering;
        string replaceNodePath;
        string importNodeEvent;
        string replaceElementPath;
        string rebuildElementEvent;
        bool startRoot;
        bool endRoot;
        bool nullable;

        //elements
        string clicked;
        string template;

        // events
        TrickleDown useTrickleDown;
        string mouseCaptureOutEvent;
        string mouseCaptureEvent;
        string changeEvent;
        string validateCommandEvent;
        string executeCommandEvent;
        string dragExitedEvent;
        string dragUpdatedEvent;
        string dragPerformEvent;
        string dragEnterEvent;
        string dragLeaveEvent;
        string focusOutEvent;
        string blurEvent;
        string focusInEvent;
        string focusEvent;
        string inputEvent;
        string keyDownEvent;
        string keyUpEvent;
        string geometryChangedEvent;
        string pointerDownEvent;
        string pointerUpEvent;
        string pointerMoveEvent;
        string mouseDownEvent;
        string mouseUpEvent;
        string mouseMoveEvent;
        string contextClickEvent;
        string wheelEvent;
        string mouseEnterEvent;
        string mouseLeaveEvent;
        string mouseEnterWindowEvent;
        string mouseLeaveWindowEvent;
        string mouseOverEvent;
        string mouseOutEvent;
        string contextualMenuPopulateEvent;
        string attachToPanelEvent;
        string detachFromPanelEvent;
        string tooltipEvent;
        string imguiEvent;
        #endregion

        #region Properties
        public string Path => path;
        public string ClassName => className;

        public ElementDownloadImage ImageFiltering { get => imageFiltering; set => imageFiltering = value; }
        public string ReplaceNodePath { get => replaceNodePath; set => replaceNodePath = value; }
        public string ReplaceNodeEvent { get => importNodeEvent; set => importNodeEvent = value; }
        public string ReplaceElementPath { get => replaceElementPath; set => replaceElementPath = value; }
        public string RebuildElementEvent { get => rebuildElementEvent; set => rebuildElementEvent = value; }
        public bool StartRoot { get => startRoot; set => startRoot = value; }
        public bool EndRoot { get => endRoot; set => endRoot = value; }
        public bool Nullable { get => nullable; set => nullable = value; }

        public string Clicked { get => clicked; set => clicked = value; }
        public string Template { get => template; set => template = value; }

        public TrickleDown UseTrickleDown { get => useTrickleDown; set => useTrickleDown = value; }
        public string MouseCaptureOutEvent { get => mouseCaptureOutEvent; set => mouseCaptureOutEvent = value; }
        public string MouseCaptureEvent { get => mouseCaptureEvent; set => mouseCaptureEvent = value; }
        public string ChangeEvent { get => changeEvent; set => changeEvent = value; }
        public string ValidateCommandEvent { get => validateCommandEvent; set => validateCommandEvent = value; }
        public string ExecuteCommandEvent { get => executeCommandEvent; set => executeCommandEvent = value; }
        public string DragExitedEvent { get => dragExitedEvent; set => dragExitedEvent = value; }
        public string DragUpdatedEvent { get => dragUpdatedEvent; set => dragUpdatedEvent = value; }
        public string DragPerformEvent { get => dragPerformEvent; set => dragPerformEvent = value; }
        public string DragEnterEvent { get => dragEnterEvent; set => dragEnterEvent = value; }
        public string DragLeaveEvent { get => dragLeaveEvent; set => dragLeaveEvent = value; }
        public string FocusOutEvent { get => focusOutEvent; set => focusOutEvent = value; }
        public string BlurEvent { get => blurEvent; set => blurEvent = value; }
        public string FocusInEvent { get => focusInEvent; set => focusInEvent = value; }
        public string FocusEvent { get => focusEvent; set => focusEvent = value; }
        public string InputEvent { get => inputEvent; set => inputEvent = value; }
        public string KeyDownEvent { get => keyDownEvent; set => keyDownEvent = value; }
        public string KeyUpEvent { get => keyUpEvent; set => keyUpEvent = value; }
        public string GeometryChangedEvent { get => geometryChangedEvent; set => geometryChangedEvent = value; }
        public string PointerDownEvent { get => pointerDownEvent; set => pointerDownEvent = value; }
        public string PointerUpEvent { get => pointerUpEvent; set => pointerUpEvent = value; }
        public string PointerMoveEvent { get => pointerMoveEvent; set => pointerMoveEvent = value; }
        public string MouseDownEvent { get => mouseDownEvent; set => mouseDownEvent = value; }
        public string MouseUpEvent { get => mouseUpEvent; set => mouseUpEvent = value; }
        public string MouseMoveEvent { get => mouseMoveEvent; set => mouseMoveEvent = value; }
        public string ContextClickEvent { get => contextClickEvent; set => contextClickEvent = value; }
        public string WheelEvent { get => wheelEvent; set => wheelEvent = value; }
        public string MouseEnterEvent { get => mouseEnterEvent; set => mouseEnterEvent = value; }
        public string MouseLeaveEvent { get => mouseLeaveEvent; set => mouseLeaveEvent = value; }
        public string MouseEnterWindowEvent { get => mouseEnterWindowEvent; set => mouseEnterWindowEvent = value; }
        public string MouseLeaveWindowEvent { get => mouseLeaveWindowEvent; set => mouseLeaveWindowEvent = value; }
        public string MouseOverEvent { get => mouseOverEvent; set => mouseOverEvent = value; }
        public string MouseOutEvent { get => mouseOutEvent; set => mouseOutEvent = value; }
        public string ContextualMenuPopulateEvent { get => contextualMenuPopulateEvent; set => contextualMenuPopulateEvent = value; }
        public string AttachToPanelEvent { get => attachToPanelEvent; set => attachToPanelEvent = value; }
        public string DetachFromPanelEvent { get => detachFromPanelEvent; set => detachFromPanelEvent = value; }
        public string TooltipEvent { get => tooltipEvent; set => tooltipEvent = value; }
        public string IMGUIEvent { get => imguiEvent; set => imguiEvent = value; }
        #endregion

        #region Constructors
        public QueryAttribute(string path, string className = default)
        {
            this.path = path;
            this.className = className;
        }
        #endregion
    }
}