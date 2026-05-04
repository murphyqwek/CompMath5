using PeterO.Numbers;

namespace Core.Interpolation
{
    public static class Utils
    {
        public static void ValidateEquidistant(List<EDecimal> x, EDecimal step)
        {
            for (int i = 2; i < x.Count; i++)
            {
                if ((x[i] - x[i - 1] - step).Abs().CompareTo(EDecimal.FromDouble(1e-7)) > 0)
                    throw new ArgumentException($"Узлы не являются равноотстоящими. Ошибка между x[{i}] и x[{i - 1}]");
            }
        }

        public static void ValidateEquidistant(List<Point> points, EDecimal step)
        {
            ValidateEquidistant(points.Select(p => p.X).ToList(), step);
        }
    }
}
