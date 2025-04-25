using System;

namespace Figma.Core.Uss
{
    struct LayoutDouble4
    {
        #region Fields
        public double top;
        public double right;
        public double bottom;
        public double left;
        #endregion

        #region Methods
        public LayoutDouble4(double top, double right, double bottom, double left)
        {
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.left = left;
        }
        public LayoutDouble4(double value)
        {
            top = value;
            right = value;
            bottom = value;
            left = value;
        }

        public LayoutDouble4 OnlyPositiveValues() => new(top > UssStyle.tolerance ? top : 0.0, right > UssStyle.tolerance ? right : 0.0, bottom > UssStyle.tolerance ? bottom : 0.0, left > UssStyle.tolerance ? left : 0.0);
        public LayoutDouble4 OnlyNegativeValues() => new(top < UssStyle.tolerance ? top : 0.0, right < UssStyle.tolerance ? right : 0.0, bottom < UssStyle.tolerance ? bottom : 0.0, left < UssStyle.tolerance ? left : 0.0);

        public Length4Property ToLength4Property() => new[] { top, right, bottom, left };
        public bool Any() => Math.Abs(top) > UssStyle.tolerance || Math.Abs(right) > UssStyle.tolerance || Math.Abs(bottom) > UssStyle.tolerance || Math.Abs(left) > UssStyle.tolerance;

        public static LayoutDouble4 operator +(LayoutDouble4 a, LayoutDouble4 b) => new(a.top + b.top, a.right + b.right, a.bottom + b.bottom, a.left + b.left);
        public static LayoutDouble4 operator -(LayoutDouble4 a, LayoutDouble4 b) => new(a.top - b.top, a.right - b.right, a.bottom - b.bottom, a.left - b.left);
        public static LayoutDouble4 operator -(LayoutDouble4 a) => new(-a.top, -a.right, -a.bottom, -a.left);
        public static LayoutDouble4 operator *(LayoutDouble4 a, double k) => new(a.top * k, a.right * k, a.bottom * k, a.left * k);
        #endregion
    }
}