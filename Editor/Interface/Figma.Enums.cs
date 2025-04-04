// ReSharper disable InconsistentNaming

namespace Figma.Internals
{
    public enum EffectType { INNER_SHADOW, DROP_SHADOW, LAYER_BLUR, BACKGROUND_BLUR }

    public enum BlendMode { PASS_THROUGH, NORMAL, DARKEN, MULTIPLY, LINEAR_BURN, COLOR_BURN, LIGHTEN, SCREEN, LINEAR_DODGE, COLOR_DODGE, OVERLAY, SOFT_LIGHT, HARD_LIGHT, DIFFERENCE, EXCLUSION, HUE, SATURATION, COLOR, LUMINOSITY }

    public enum ConstraintVertical { TOP, BOTTOM, CENTER, TOP_BOTTOM, SCALE }

    public enum ConstraintHorizontal { LEFT, RIGHT, CENTER, LEFT_RIGHT, SCALE }

    public enum PaintType { SOLID, GRADIENT_LINEAR, GRADIENT_RADIAL, GRADIENT_ANGULAR, GRADIENT_DIAMOND, IMAGE, EMOJI }

    public enum Pattern { COLUMNS, ROWS, GRID }

    public enum Alignment { MIN, MAX, STRETCH, CENTER }

    public enum ScaleMode { FILL, FIT, TILE, STRETCH }

    public enum ExportSettingsConstraintsType { SCALE, WIDTH, HEIGHT }

    public enum Format { JPG, PNG, SVG, PDF }

    public enum ActionType { BACK, CLOSE, URL, NODE }

    public enum Navigation { NAVIGATE, SWAP, OVERLAY, CHANGE_TO }

    public enum TransitionType { DISSOLVE, SMART_ANIMATE, MOVE_IN, MOVE_OUT, PUSH, SLIDE_IN, SLIDE_OUT }

    public enum TransitionDirection { LEFT, RIGHT, TOP, BOTTOM }

    public enum TriggerType { ON_CLICK, ON_HOVER, ON_PRESS, DRAG, AFTER_TIMEOUT, MOUSE_ENTER, MOUSE_LEAVE, MOUSE_UP, MOUSE_DOWN, ON_KEY_DOWN, ON_KEY_UP }

    public enum EasingType { EASE_IN, EASE_OUT, EASE_IN_AND_OUT, LINEAR, SLOW, CUSTOM_SPRING }

    public enum LayoutAlign { CENTER, MIN, MAX, STRETCH, INHERIT }

    public enum StrokeCap { NONE, ROUND, SQUARE, ARROW_LINES, ARROW_EQUILATERAL, LINE_ARROW }

    public enum StrokeJoin { MITER, BEVEL, ROUND }

    public enum StrokeAlign { INSIDE, OUTSIDE, CENTER }

    public enum LayoutMode { NONE, HORIZONTAL, VERTICAL }

    public enum PrimaryAxisSizingMode { FIXED, AUTO }

    public enum CounterAxisSizingMode { FIXED, AUTO }

    public enum PrimaryAxisAlignItems { MIN, CENTER, MAX, SPACE_BETWEEN }

    public enum CounterAxisAlignItems { MIN, CENTER, MAX, BASELINE }

    public enum OverflowDirection { NONE, HORIZONTAL_SCROLLING, VERTICAL_SCROLLING, HORIZONTAL_AND_VERTICAL_SCROLLING }

    public enum TextCase { ORIGINAL, UPPER, LOWER, TITLE }

    public enum TextDecoration { NONE, UNDERLINE, STRIKETHROUGH }

    public enum TextAlignHorizontal { LEFT, CENTER, RIGHT, JUSTIFIED }

    public enum TextAlignVertical { TOP, CENTER, BOTTOM }

    public enum TextAutoResize { NONE, WIDTH_AND_HEIGHT, HEIGHT, TRUNCATE }

    public enum BooleanOperation { UNION, INTERSECT, SUBTRACT, EXCLUDE }

    public enum LayoutPositioning { AUTO, ABSOLUTE }

    public enum StyleType { FILL, TEXT, EFFECT, GRID, NONE }

    public enum NodeType { DOCUMENT, CANVAS, SLICE, FRAME, GROUP, COMPONENT_SET, COMPONENT, INSTANCE, BOOLEAN_OPERATION, VECTOR, STAR, LINE, ELLIPSE, REGULAR_POLYGON, RECTANGLE, TEXT, SECTION }

    public enum ComponentPropertyType { BOOLEAN, TEXT, INSTANCE_SWAP, VARIANT }

    public enum LayoutWrap { NO_WRAP, WRAP }

    public enum MaskType { ALPHA, VECTOR, LUMINANCE }

    public enum LayoutSizing { FIXED, HUG, FILL }

    public enum CounterAxisAlignContent { AUTO, SPACE_BETWEEN }

    public enum TextTruncation { DISABLED, ENDING }

    public enum LineType { NONE, ORDERED, UNORDERED }

    public enum TextWeight { BOLD, NORMAL }

    public enum TextItalic { ITALIC, NORMAL }
}