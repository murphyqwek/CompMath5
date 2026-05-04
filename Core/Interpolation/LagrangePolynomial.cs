using PeterO.Numbers;

namespace Core.Interpolation
{
    public class LagrangePolynomial : IInterpolate
    {
        private readonly List<Point> _points;
        private readonly int _n;

        private readonly EDecimal[] _denominators;

        public LagrangePolynomial(List<Point> points)
        {
            _n = points.Count;
            _points = points;
            _denominators = new EDecimal[_n];

            PrecomputeDenominators();
        }

        private void PrecomputeDenominators()
        {
            for (int i = 0; i < _n; i++)
            {
                EDecimal denominator = EDecimal.One;
                for (int j = 0; j < _n; j++)
                {
                    if (i != j)
                    {
                        EDecimal diff = _points[i].X - _points[j].X;

                        if (diff.IsZero)
                            throw new DivideByZeroException($"Обнаружены точки с одинаковым X = {_points[i].X}. Интерполяция Лагранжа невозможна");

                        denominator *= diff;
                    }
                }
                _denominators[i] = denominator;
            }
        }

        public EDecimal Interpolate(EDecimal targetX)
        {
            if (_n == 0) return EDecimal.Zero;
            if (_n == 1) return _points[0].Y;

            EDecimal result = EDecimal.Zero;

            for (int i = 0; i < _n; i++)
            {
                EDecimal numerator = EDecimal.One;
                for (int j = 0; j < _n; j++)
                {
                    if (i != j)
                    {
                        numerator *= (targetX - _points[j].X);
                    }
                }

                EDecimal term = _points[i].Y * numerator.Divide(_denominators[i], EContext.Decimal128);
                result += term;
            }

            return result;
        }
    }
}