using PeterO.Numbers;

namespace Core.Interpolation
{
    public class StirlingPolynomial : IInterpolate
    {
        private readonly List<Point> _points;
        private readonly EDecimal[,] _table;
        private readonly int _n;
        private readonly int _centerIndex;
        private readonly EDecimal _h;

        public StirlingPolynomial(List<Point> points, Table table)
        {
            _n = points.Count;

            if (_n % 2 == 0)
                throw new ArgumentException("Для многочлена Стирлинга требуется нечетное количество точек");

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
            _centerIndex = _n / 2;
            _table = table.table;

        }

        public EDecimal Interpolate(EDecimal targetX)
        {
            if (_n == 1) return _points[0].Y;

            EDecimal t = (targetX - _points[_centerIndex].X).Divide(_h, EContext.Decimal128);

            EDecimal result = _points[_centerIndex].Y;

            EDecimal t_squared = t * t;
            EDecimal product = EDecimal.One;
            EDecimal factorial = EDecimal.One;

            for (int k = 1; k < _n; k++)
            {
                factorial *= k;
                int m = k / 2;

                if (k % 2 != 0)
                {
                    if (m > 0)
                    {
                        EDecimal mSquare = EDecimal.FromInt32(m * m);
                        product *= (t_squared - mSquare);
                    }

                    EDecimal multiplier = t * product;

                    EDecimal diffSum = _table[_centerIndex - m - 1, k] + _table[_centerIndex - m, k];
                    EDecimal averageDiff = diffSum.Divide(2, EContext.Decimal128);

                    result += (multiplier * averageDiff).Divide(factorial, EContext.Decimal128);
                }
                else
                {
                    EDecimal multiplier = t_squared * product;

                    EDecimal diff = _table[_centerIndex - m, k];

                    result += (multiplier * diff).Divide(factorial, EContext.Decimal128);
                }
            }

            return result;
        }

        public EDecimal[,] Table => _table;
    }
}