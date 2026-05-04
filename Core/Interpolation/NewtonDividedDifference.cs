using PeterO.Numbers;

namespace Core.Interpolation
{
    public class NewtonDividedDifference : IInterpolate
    {
        private readonly List<Point> _points;
        private readonly EDecimal[,] _table;
        private readonly int _n;

        public NewtonDividedDifference(List<Point> points)
        {
            _n = points.Count;
            _points = points;
            _table = new EDecimal[_n, _n];

            BuildTable();
        }

        private void BuildTable()
        {
            for (int i = 0; i < _n; i++)
                _table[i, 0] = _points[i].Y;

            for (int j = 1; j < _n; j++)
            {
                for (int i = 0; i < _n - j; i++)
                {
                    EDecimal numerator = _table[i + 1, j - 1] - _table[i, j - 1];
                    EDecimal denominator = _points[i + j].X - _points[i].X;

                    if (denominator.IsZero)
                        throw new DivideByZeroException($"Обнаружены точки с одинаковым X = {_points[i].X}. Разделенные разности невозможно вычислить");

                    _table[i, j] = numerator.Divide(denominator, EContext.Decimal128);
                }
            }
        }

        public EDecimal Interpolate(EDecimal targetX)
        {
            if (_n == 0) return EDecimal.Zero;
            if (_n == 1) return _points[0].Y;

            EDecimal result = _table[0, 0];
            EDecimal product = EDecimal.One;

            for (int i = 1; i < _n; i++)
            {
                product *= (targetX - _points[i - 1].X);

                result += _table[0, i] * product;
            }

            return result;
        }

        public EDecimal[,] Table => _table;
    }
}