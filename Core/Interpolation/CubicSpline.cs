using PeterO.Numbers;
using System;
using System.Collections.Generic;

namespace Core.Interpolation
{
    public class CubicSpline : IInterpolate
    {
        private readonly List<Point> _points;
        private readonly int _n;

        private EDecimal[] _a;
        private EDecimal[] _b;
        private EDecimal[] _c;
        private EDecimal[] _d;
        private EDecimal[] _h;

        public CubicSpline(List<Point> points)
        {
            if (points.Count < 2)
                throw new Exception("Для построения кубического сплайна необходимо минимум 2 точки.");

            _points = points;
            _n = points.Count - 1;

            _a = new EDecimal[_n + 1];
            _b = new EDecimal[_n + 1];
            _c = new EDecimal[_n + 1];
            _d = new EDecimal[_n + 1];
            _h = new EDecimal[_n + 1];

            BuildSpline();
        }

        private void BuildSpline()
        {
            for (int i = 1; i <= _n; i++)
            {
                _h[i] = _points[i].X - _points[i - 1].X;
                _a[i] = _points[i].Y;
            }

            if (_n == 1)
            {
                _c[0] = EDecimal.Zero;
                _c[1] = EDecimal.Zero;
                _b[1] = (_points[1].Y - _points[0].Y).Divide(_h[1], EContext.Decimal128);
                _d[1] = EDecimal.Zero;
                return;
            }

            EDecimal[] alpha = new EDecimal[_n];
            EDecimal[] beta = new EDecimal[_n];

            EDecimal B_1 = EDecimal.FromInt32(2) * (_h[1] + _h[2]);
            EDecimal C_1 = _h[2];
            EDecimal F_1_term1 = (_points[2].Y - _points[1].Y).Divide(_h[2], EContext.Decimal128);
            EDecimal F_1_term2 = (_points[1].Y - _points[0].Y).Divide(_h[1], EContext.Decimal128);
            EDecimal F_1 = EDecimal.FromInt32(3) * (F_1_term1 - F_1_term2);

            alpha[1] = -C_1.Divide(B_1, EContext.Decimal128);
            beta[1] = F_1.Divide(B_1, EContext.Decimal128);

            for (int i = 2; i < _n; i++)
            {
                EDecimal A_i = _h[i];
                EDecimal B_i = EDecimal.FromInt32(2) * (_h[i] + _h[i + 1]);
                EDecimal C_i = _h[i + 1];

                EDecimal F_i_term1 = (_points[i + 1].Y - _points[i].Y).Divide(_h[i + 1], EContext.Decimal128);
                EDecimal F_i_term2 = (_points[i].Y - _points[i - 1].Y).Divide(_h[i], EContext.Decimal128);
                EDecimal F_i = EDecimal.FromInt32(3) * (F_i_term1 - F_i_term2);

                EDecimal gamma = B_i + A_i * alpha[i - 1];
                alpha[i] = -C_i.Divide(gamma, EContext.Decimal128);
                beta[i] = (F_i - A_i * beta[i - 1]).Divide(gamma, EContext.Decimal128);
            }
            _c[0] = EDecimal.Zero;
            _c[_n] = EDecimal.Zero;
            for (int i = _n - 1; i >= 1; i--)
            {
                _c[i] = alpha[i] * _c[i + 1] + beta[i];
            }

            EDecimal three = EDecimal.FromInt32(3);
            for (int i = 1; i <= _n; i++)
            {
                EDecimal term1 = (_a[i] - _points[i - 1].Y).Divide(_h[i], EContext.Decimal128);
                EDecimal term2 = (EDecimal.FromInt32(2) * _c[i] + _c[i - 1]).Divide(three, EContext.Decimal128) * _h[i];

                _b[i] = term1 + term2;
                _d[i] = (_c[i] - _c[i - 1]).Divide(three * _h[i], EContext.Decimal128);
            }
        }

        public EDecimal Interpolate(EDecimal targetX)
        {
            int interval = 1;
            for (int i = 1; i <= _n; i++)
            {
                if (targetX.CompareTo(_points[i].X) <= 0)
                {
                    interval = i;
                    break;
                }
                interval = i;
            }

            EDecimal diff = targetX - _points[interval].X;

            EDecimal result = _a[interval]
                            + _b[interval] * diff
                            + _c[interval] * diff * diff
                            + _d[interval] * diff * diff * diff;

            return result;
        }
    }
}