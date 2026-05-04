using PeterO.Numbers;

namespace Core.Interpolation
{
    public class BesselPolynomial : IInterpolate
    {
        private readonly List<Point> _points;
        private readonly EDecimal[,] _table;
        private readonly int _n;
        private readonly int _centerIndex;
        private readonly EDecimal _h;

        public BesselPolynomial(List<Point> points, Table table)
        {
            _n = points.Count;

            if (_n % 2 != 0)
                throw new ArgumentException("Для многочлена Бесселя требуется четное количество точек");

            if (_n > 1)
            {
                _h = points[1].X - points[0].X;
                Utils.ValidateEquidistant(points, _h);
            }
            else
            {
                _h = EDecimal.One;
            }

            _points = points;

            _centerIndex = (_n / 2) - 1;
            _table = table.table;
        }

        public EDecimal Interpolate(EDecimal targetX)
        {
            EDecimal t = (targetX - _points[_centerIndex].X).Divide(_h, EContext.Decimal128);

            EDecimal tMinusHalf = t - EDecimal.FromDouble(0.5);

            EDecimal result = (_points[_centerIndex].Y + _points[_centerIndex + 1].Y).Divide(2, EContext.Decimal128);

            EDecimal baseProduct = EDecimal.One;
            EDecimal factorial = EDecimal.One;

            for (int k = 1; k < _n; k++)
            {
                factorial *= k;

                if (k % 2 != 0)
                {
                    int m = (k - 1) / 2;
                    int row = _centerIndex - m;

                    EDecimal diff = _table[row, k];
                    EDecimal term = (baseProduct * tMinusHalf * diff).Divide(factorial, EContext.Decimal128);
                    result += term;
                }
                else
                {
                    int m = k / 2;
                    baseProduct *= (t + (m - 1)) * (t - m);

                    int row1 = _centerIndex - m;
                    int row2 = _centerIndex - m + 1;

                    EDecimal diffAvg = (_table[row1, k] + _table[row2, k]).Divide(2, EContext.Decimal128);

                    EDecimal term = (baseProduct * diffAvg).Divide(factorial, EContext.Decimal128);
                    result += term;
                }
            }

            return result;
        }

        public EDecimal[,] Table => _table;
    }
}