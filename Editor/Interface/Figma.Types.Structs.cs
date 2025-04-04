namespace Figma.Internals
{
    public struct Vector
    {
        public double x;
        public double y;
    }

    public struct Rect
    {
        public double x;
        public double y;
        public double width;
        public double height;

        public double left => x;
        public double right => x + width;
        public double top => y;
        public double bottom => y + height;
        public double centerLeft => x - width / 2;
        public double centerRight => x + width / 2;
        public double centerTop => y - height / 2;
        public double centerBottom => y + height / 2;
        public double halfWidth => width / 2;
        public double halfHeight => height / 2;

        public Rect(double x, double y, double width, double height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static Rect operator +(Rect a, Rect b) => new(a.x + b.x, a.y + b.y, a.width + b.width, a.height + b.height);
        public static Rect operator -(Rect a, Rect b) => new(a.x - b.x, a.y - b.y, a.width - b.width, a.height - b.height);
    }

    public struct RGBA
    {
        public double r;
        public double g;
        public double b;
        public double a;

        public static explicit operator UnityEngine.Color(RGBA color) => new((float) color.r, (float) color.g, (float) color.b, (float) color.a);
    }
}