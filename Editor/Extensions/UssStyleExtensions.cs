using System;
using System.Linq;

namespace Figma
{
    using Core.Uss;
    using Internals;

    static class UssStyleExtension
    {
        #region Methods
        internal static bool HasBorder(this IGeometryMixin geometry) => geometry.strokes.Any(x => x.visible) && geometry.strokeWeight > UssStyle.tolerance;
        internal static LayoutDouble4 GetBorderWidths(this IGeometryMixin geometry)
        {
            if (geometry is null or TextNode || !HasBorder(geometry)) return new LayoutDouble4();

            LayoutDouble4 borders = geometry.individualStrokeWeights != null
                ? new LayoutDouble4(geometry.individualStrokeWeights.top, geometry.individualStrokeWeights.right, geometry.individualStrokeWeights.bottom, geometry.individualStrokeWeights.left)
                : new LayoutDouble4(geometry.strokeWeight);

            return borders;
        }
        internal static LayoutDouble4 GetOutsideBorderWidths(this IGeometryMixin geometry) => geometry.GetBorderWidths() * geometry.GetOutsideFraction();
        internal static LayoutDouble4 GetInsideBorderWidths(this IGeometryMixin geometry) => geometry.GetBorderWidths() * (1 - geometry.GetOutsideFraction());
        internal static Rect GetContentBox(this ILayoutMixin layout)
        {
            LayoutDouble4 border = GetInsideBorderWidths(layout as IGeometryMixin);
            double x = layout.absoluteBoundingBox.x + border.left;
            double y = layout.absoluteBoundingBox.y + border.top;
            double width = layout.absoluteBoundingBox.width - border.left - border.right;
            double height = layout.absoluteBoundingBox.height - border.top - border.bottom;
            return new Rect(x, y, width, height);
        }
        internal static Rect GetBorderBox(this ILayoutMixin layout)
        {
            LayoutDouble4 border = GetOutsideBorderWidths(layout as IGeometryMixin);
            double x = layout.absoluteBoundingBox.x - border.left;
            double y = layout.absoluteBoundingBox.y - border.top;
            double width = layout.absoluteBoundingBox.width + border.left + border.right;
            double height = layout.absoluteBoundingBox.height + border.top + border.bottom;
            return new Rect(x, y, width, height);
        }
        internal static LayoutDouble4 GetCorrectedPadding(this IDefaultFrameMixin frame) => new LayoutDouble4(frame.paddingTop, frame.paddingRight, frame.paddingBottom, frame.paddingLeft) - (frame.strokesIncludedInLayout ? new LayoutDouble4() : GetInsideBorderWidths(frame));
        internal static RGBA BlendWith(this RGBA foreground, RGBA background)
        {
            double blend = background.a * (1.0 - foreground.a);
            RGBA color = new();
            color.a = foreground.a + blend;

            const double alphaTolerance = 0.01;
            if (color.a < alphaTolerance) return new RGBA();

            color.r = (foreground.r * foreground.a + background.r * blend) / color.a;
            color.g = (foreground.g * foreground.a + background.g * blend) / color.a;
            color.b = (foreground.b * foreground.a + background.b * blend) / color.a;

            return color;
        }
        // Magical formula found by testing different methods. including LinearToGammaSpaceExact(a), 1-GammaToLinearSpaceExact(1-a), pow(a, 1/2.2), pow(a, 1/1.8), 1.0-pow(1-a, 2.2). This method yielded results close to figma with only one layer of blending. With multiple layers all tried corrections becomes too dark
        internal static double AlphaCorrection(double a) => UnityEditor.PlayerSettings.colorSpace is UnityEngine.ColorSpace.Linear && a is > 0.0 and < 1.0 ? LinearToGammaSpaceExact(a) : a;
        #endregion

        #region Support Methods
        static double GetOutsideFraction(this IGeometryMixin geometry) => geometry.strokeAlign switch
        {
            StrokeAlign.OUTSIDE => 1.0,
            StrokeAlign.CENTER => 0.5,
            StrokeAlign.INSIDE => 0.0,
            _ => throw new NotSupportedException()
        };
        static double LinearToGammaSpaceExact(double value) => value switch // copied from unity shader UnityCG.cginc
        {
            <= 0.0 => 0.0,
            <= 0.0031308 => 12.92 * value,
            < 1.0 => 1.055 * Math.Pow(value, 0.4166667) - 0.055,
            _ => Math.Pow(value, 0.45454545)
        };
        #endregion
    }
}