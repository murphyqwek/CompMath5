using PeterO.Numbers;

namespace Core.Interpolation
{
    public class NewtonFiniteDifference : IInterpolate
    {
        private readonly List<Point> _points;
        private readonly EDecimal[,] _table;
        private readonly int _n;
        private readonly EDecimal _h;

        public NewtonFiniteDifference(List<Point> points, Table table)
        {
            _n = points.Count;

            if (_n > 1)
            {
                _h = points[1].X - points[0].X;
                Utils.ValidateEquidistant(points, _h);
            }

            _points = points;
            _table = table.table;
        }

        public EDecimal Interpolate(EDecimal targetX)
        {
            if (_n == 1) return _points[0].Y;

            EDecimal q = (targetX - _points[0].X).Divide(_h, EContext.Decimal128);
            EDecimal result = _table[0, 0];

            EDecimal qTerm = EDecimal.One;
            EDecimal factorial = EDecimal.One;

            for (int i = 1; i < _n; i++)
            {
                qTerm *= (q - i + 1);
                factorial *= i;
                result += (qTerm * _table[0, i]).Divide(factorial, EContext.Decimal128);
            }

            return result;
        }

        public EDecimal[,] Table => _table;
    }
}