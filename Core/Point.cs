using PeterO.Numbers;

namespace Core
{
    public class Point
    {
        public EDecimal X { get; }
        public EDecimal Y { get; }

        public Point(double x, double y)
        {
            X = EDecimal.FromDouble(x);
            Y = EDecimal.FromDouble(y);
        }

        public Point(EDecimal x, EDecimal y)
        {
            X = x;
            Y = y;
        }

        public Point(string x, string y)
        {
            X = EDecimal.FromString(x);
            Y = EDecimal.FromString(y);
        }
    }
}
