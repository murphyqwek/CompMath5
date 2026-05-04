using PeterO.Numbers;

namespace Core.Interpolation
{
    public class Table
    {
        public readonly EDecimal[,] table;
        public readonly int n;
        private List<Point> points;

        public Table(List<Point> points)
        {
            n = points.Count;

            this.points = points;
            table = new EDecimal[n, n];
        }

        public void BuildTable()
        {
            for (int i = 0; i < n; i++)
                table[i, 0] = points[i].Y;

            for (int j = 1; j < n; j++)
            {
                for (int i = 0; i < n - j; i++)
                {
                    table[i, j] = table[i + 1, j - 1] - table[i, j - 1];
                }
            }
        }
    }
}
