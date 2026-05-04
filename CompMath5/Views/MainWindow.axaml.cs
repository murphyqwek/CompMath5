using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Core;
using Core.Interpolation;
using PeterO.Numbers;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMath5.Views
{
    public class UIPoint
    {
        public string X { get; set; } = "0";
        public string Y { get; set; } = "0";
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<UIPoint> ManualPoints { get; set; } = new();
        private string _selectedFilePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            PointsDataGrid.ItemsSource = ManualPoints;

            ManualPoints.Add(new UIPoint { X = "1", Y = "1" });
            ManualPoints.Add(new UIPoint { X = "2", Y = "2" });
            ManualPoints.Add(new UIPoint { X = "3", Y = "3" });
        }

        private void AddRow_Click(object sender, RoutedEventArgs e) => ManualPoints.Add(new UIPoint { X = "0", Y = "0" });

        private void InputMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ManualInputPanel == null) return;

            ManualInputPanel.IsVisible = InputModeComboBox.SelectedIndex == 0;
            FileInputPanel.IsVisible = InputModeComboBox.SelectedIndex == 1;
            FunctionInputPanel.IsVisible = InputModeComboBox.SelectedIndex == 2;
        }

        private async void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите текстовый файл",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                _selectedFilePath = files[0].Path.LocalPath;
                SelectedFileText.Text = $"Выбран файл: {Path.GetFileName(_selectedFilePath)}";

                try
                {
                    var lines = await File.ReadAllLinesAsync(_selectedFilePath);
                    var filteredLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

                    if (filteredLines.Count < 2)
                        throw new Exception("Файл слишком короткий. Нужно X* на первой строке, на следующих должны быть узлы интерполяции");

                    TargetXTextBox.Text = filteredLines[0].Trim().Replace(',', '.');
                    ManualPoints.Clear();

                    for (int i = 1; i < filteredLines.Count; i++)
                    {
                        var parts = filteredLines[i].Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            ManualPoints.Add(new UIPoint
                            {
                                X = parts[0].Replace(',', '.'),
                                Y = parts[1].Replace(',', '.')
                            });
                        }
                    }

                    InputModeComboBox.SelectedIndex = 0;
                    InputErrorText.Text = "Данные успешно загружены из файла в таблицу.";
                    InputErrorText.Foreground = Avalonia.Media.Brushes.Green;
                }
                catch (UnauthorizedAccessException ex)
                {
                    InputErrorText.Text = $"Нет доступа к файлу по пути: {_selectedFilePath}";
                    InputErrorText.Foreground = Avalonia.Media.Brushes.Red;
                }
                catch (IOException ex)
                {
                    InputErrorText.Text = "Ошибка ввода-вывода, возможно, файл испольщуется другим процессором";
                    InputErrorText.Foreground = Avalonia.Media.Brushes.Red;
                }
                catch (Exception ex)
                {
                    InputErrorText.Text = "Ошибка при чтении файла: " + ex.Message;
                    InputErrorText.Foreground = Avalonia.Media.Brushes.Red;
                }
            }
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            InputErrorText.Text = "";
            ClearAllTabs();

            List<Point> mathPoints;
            EDecimal targetX;
            Func<double, double> originalFunc = null;

            try
            {
                mathPoints = GetPointsAndTarget(out targetX, out originalFunc);

                if (mathPoints.Count < 2)
                    throw new Exception("Ошибка: для интерполяции необходимо минимум 2 точки.");

                if (mathPoints.Select(p => p.X).Distinct().Count() != mathPoints.Count)
                    throw new Exception("Ошибка: узлы интерполяции должны быть уникальными.");
            }
            catch (Exception ex)
            {
                InputErrorText.Foreground = Avalonia.Media.Brushes.Red;
                InputErrorText.Text = ex.Message;
                return;
            }

            RunCalculations(mathPoints, targetX, originalFunc);
        }

        private List<Point> GetPointsAndTarget(out EDecimal targetX, out Func<double, double> originalFunc)
        {
            targetX = EDecimal.Zero;
            originalFunc = null;
            var points = new List<Point>();

            bool TryParseDouble(string s, out double result)
                => double.TryParse(s.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            if (InputModeComboBox.SelectedIndex == 0)
            {
                if (!TryParseDouble(TargetXTextBox.Text, out double tx))
                {
                    TargetXTextBox.BorderBrush = Avalonia.Media.Brushes.Red;
                    throw new Exception("Некорректное значение X*. Оно должно быть числом");
                }
                targetX = EDecimal.FromDouble(tx);
                TargetXTextBox.ClearValue(TextBox.BorderBrushProperty);

                for (int i = 0; i < ManualPoints.Count; i++)
                {
                    bool xOk = TryParseDouble(ManualPoints[i].X, out double x);
                    bool yOk = TryParseDouble(ManualPoints[i].Y, out double y);

                    if (!xOk || !yOk)
                        throw new Exception($"Ошибка в строке {i + 1}: поля должны быть заполнены числами");

                    points.Add(new Point(EDecimal.FromDouble(x), EDecimal.FromDouble(y)));
                }
            }
            else if (InputModeComboBox.SelectedIndex == 1)
            {
                if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
                    throw new Exception("Файл не выбран");

                var lines = File.ReadAllLines(_selectedFilePath)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();

                if (lines.Count < 2)
                    throw new Exception("Файл должен содержать X* на первой строке и узлы на последующих");

                if (!TryParseDouble(lines[0], out double tx))
                    throw new Exception("Первая строка файла (X*) должна быть числом");
                targetX = EDecimal.FromDouble(tx);
                TargetXTextBox.Text = tx.ToString();

                for (int i = 1; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    if (TryParseDouble(parts[0], out double x) && TryParseDouble(parts[1], out double y))
                        points.Add(new Point(EDecimal.FromDouble(x), EDecimal.FromDouble(y)));
                    else
                        throw new Exception($"Ошибка в файле на строке {i + 1}: неверный формат координат. Строка должна содержать два числа");
                }
            }
            else if (InputModeComboBox.SelectedIndex == 2)
            {
                if (!TryParseDouble(TargetXTextBox.Text, out double tx)) throw new Exception("Неверный X*");
                targetX = EDecimal.FromDouble(tx);

                if (!TryParseDouble(FuncStartTextBox.Text, out double a) || !TryParseDouble(FuncEndTextBox.Text, out double b))
                    throw new Exception("Неверные границы интервала");

                int count = (int)(FuncPointsCount.Value ?? 10);
                double step = (b - a) / (count - 1);

                if (FunctionsComboBox.SelectedIndex == 0) originalFunc = Math.Sin;
                else if (FunctionsComboBox.SelectedIndex == 1) originalFunc = x => x * x - 4 * x + 5;
                else if (FunctionsComboBox.SelectedIndex == 2) originalFunc = Math.Exp;

                for (int i = 0; i < count; i++)
                {
                    double x = a + i * step;
                    points.Add(new Point(EDecimal.FromDouble(x), EDecimal.FromDouble(originalFunc(x))));
                }
            }

            return points.OrderBy(p => p.X).ToList();
        }

        private void RunCalculations(List<Point> points, EDecimal targetX, Func<double, double> originalFunc)
        {
            string targetXStr = targetX.ToDouble().ToString("F4");
            Table table = new Table(points);

            table.BuildTable();
            try
            {
                var lagrange = new LagrangePolynomial(points);
                EDecimal result = lagrange.Interpolate(targetX);
                ResultLagrange.Text = $"Результат: P({targetXStr}) = {result.ToDouble():F8}";
                DrawPlot(PlotLagrange, lagrange.Interpolate, points, targetX, result, originalFunc, "Лагранж");
            }
            catch (Exception ex) { ErrorLagrange.Text = ex.Message; }

            try
            {
                var newtonDiv = new NewtonDividedDifference(points);
                EDecimal result = newtonDiv.Interpolate(targetX);
                ResultNewtonDiv.Text = $"Результат: P({targetXStr}) = {result.ToDouble():F8}";
                DrawPlot(PlotNewtonDiv, newtonDiv.Interpolate, points, targetX, result, originalFunc, "Ньютон (Разд.)");
                BindTableToDataGrid(TableNewtonDiv, newtonDiv.Table, points.Select(p => p.X.ToDouble()).ToArray());
            }
            catch (Exception ex) { ErrorNewtonDiv.Text = ex.Message; }

            try
            {
                var newtonFin = new NewtonFiniteDifference(points, table);
                EDecimal result = newtonFin.Interpolate(targetX);
                ResultNewtonFin.Text = $"Результат: P({targetXStr}) = {result.ToDouble():F8}";
                DrawPlot(PlotNewtonFin, newtonFin.Interpolate, points, targetX, result, originalFunc, "Ньютон (Кон.)");
                BindTableToDataGrid(TableNewtonFin, newtonFin.Table, points.Select(p => p.X.ToDouble()).ToArray());
            }
            catch (Exception ex) { ErrorNewtonFin.Text = ex.Message; }

            try
            {
                var stirling = new StirlingPolynomial(points, table);
                EDecimal result = stirling.Interpolate(targetX);
                ResultStirling.Text = $"Результат: P({targetXStr}) = {result.ToDouble():F8}";
                DrawPlot(PlotStirling, stirling.Interpolate, points, targetX, result, originalFunc, "Стирлинг");
                BindTableToDataGrid(TableStirling, stirling.Table, points.Select(p => p.X.ToDouble()).ToArray());
            }
            catch (Exception ex) { ErrorStirling.Text = ex.Message; }

            try
            {
                var bessel = new BesselPolynomial(points, table);
                EDecimal result = bessel.Interpolate(targetX);
                ResultBessel.Text = $"Результат: P({targetXStr}) = {result.ToDouble():F8}";
                DrawPlot(PlotBessel, bessel.Interpolate, points, targetX, result, originalFunc, "Бессель");
                BindTableToDataGrid(TableBessel, bessel.Table, points.Select(p => p.X.ToDouble()).ToArray());
            }
            catch (Exception ex) { ErrorBessel.Text = ex.Message; }
        }

        private void DrawPlot(AvaPlot avaPlot, Func<EDecimal, EDecimal> interpolateFunc,
                            List<Point> nodes, EDecimal targetX, EDecimal targetY, Func<double, double> originalFunc, string title)
        {
            var plot = avaPlot.Plot;
            plot.Clear();

            plot.Title(title);
            plot.ShowLegend();

            double minX = Math.Min((double)nodes.Min(p => p.X), targetX.ToDouble());
            double maxX = Math.Max((double)nodes.Max(p => p.X), targetX.ToDouble());
            double range = maxX - minX;

            double plotMinX = minX - range * 0.4;
            double plotMaxX = maxX + range * 0.5;

            if (originalFunc != null)
            {
                double[] origX = Generate.Consecutive(500, (plotMaxX - plotMinX) / 500, plotMinX);
                double[] origY = origX.Select(x => {
                    double y = originalFunc(x);
                    return double.IsFinite(y) ? y : double.NaN;
                }).ToArray();

                var origLine = plot.Add.ScatterLine(origX, origY);
                origLine.Color = ScottPlot.Colors.Gray;
                origLine.LineStyle.Pattern = LinePattern.Dashed;
                origLine.LegendText = "Оригинал f(x)";
            }

            plotMinX = minX - range * 0.2;
            plotMaxX = maxX + range * 0.2;

            double[] lineX = Generate.Consecutive(500, (plotMaxX - plotMinX) / 500, plotMinX);
            double[] lineY = lineX.Select(x => {
                try
                {
                    double y = (double)interpolateFunc(EDecimal.FromDouble(x));
                    return (Math.Abs(y) < 1e10) ? y : double.NaN;
                }
                catch { return double.NaN; }
            }).ToArray();

            var polyLine = plot.Add.ScatterLine(lineX, lineY);
            polyLine.Color = ScottPlot.Colors.Blue;
            polyLine.LineWidth = 2;
            polyLine.LegendText = "Многочлен P(x)";

            double[] nodeX = nodes.Select(p => (double)p.X).ToArray();
            double[] nodeY = nodes.Select(p => (double)p.Y).ToArray();
            var scatterNodes = plot.Add.ScatterPoints(nodeX, nodeY);
            scatterNodes.Color = ScottPlot.Colors.Black;
            scatterNodes.MarkerSize = 8;
            scatterNodes.LegendText = "Узлы";

            double tx = (double)targetX;
            double ty = (double)targetY;
            if (double.IsFinite(ty))
            {
                var targetMarker = plot.Add.Marker(tx, ty);
                targetMarker.Shape = MarkerShape.FilledCircle;
                targetMarker.Size = 15;
                targetMarker.Color = ScottPlot.Colors.Red;
                targetMarker.LegendText = $"P({tx:F2}) = {ty:F4}";
            }

            plot.Axes.AutoScale();
            avaPlot.Refresh();
        }

        private void BindTableToDataGrid(DataGrid dataGrid, EDecimal[,] table, double[] xValues = null)
        {
            if (dataGrid == null || table == null) return;

            dataGrid.Columns.Clear();
            dataGrid.ItemsSource = null;

            int rows = table.GetLength(0);
            int cols = table.GetLength(1);

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "X",
                Binding = new Binding("[X]")
            });

            for (int j = 0; j < cols; j++)
            {
                string header = (j == 0) ? "Y" : $"Δ^{j}Y";
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = header,
                    Binding = new Binding($"[{header}]")
                });
            }

            var dataList = new List<Dictionary<string, string>>();

            for (int i = 0; i < rows; i++)
            {
                var row = new Dictionary<string, string>();
                row["X"] = (xValues != null && i < xValues.Length) ? xValues[i].ToString("F2") : "";

                for (int j = 0; j < cols; j++)
                {
                    string header = (j == 0) ? "Y" : $"Δ^{j}Y";
                    if (i + j < rows)
                        row[header] = ((double)table[i, j]).ToString("F4");
                    else
                        row[header] = "";
                }
                dataList.Add(row);
            }

            dataGrid.ItemsSource = dataList;
        }

        private void DeleteRowInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UIPoint pointToRemove)
            {
                ManualPoints.Remove(pointToRemove);
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (PointsDataGrid.SelectedItem is UIPoint selectedPoint)
            {
                ManualPoints.Remove(selectedPoint);
            }
        }

        private void ClearTable_Click(object sender, RoutedEventArgs e)
        {
            ManualPoints.Clear();
        }

        private void ClearAllTabs()
        {
            ErrorLagrange.Text = ErrorNewtonDiv.Text = ErrorNewtonFin.Text = ErrorStirling.Text = ErrorBessel.Text = "";
            ResultLagrange.Text = ResultNewtonDiv.Text = ResultNewtonFin.Text = ResultStirling.Text = ResultBessel.Text = "";

            PlotLagrange.Plot.Clear(); PlotLagrange.Refresh();
            PlotNewtonDiv.Plot.Clear(); PlotNewtonDiv.Refresh();
            PlotNewtonFin.Plot.Clear(); PlotNewtonFin.Refresh();
            PlotStirling.Plot.Clear(); PlotStirling.Refresh();
            PlotBessel.Plot.Clear(); PlotBessel.Refresh();

            TableNewtonDiv.ItemsSource = TableNewtonFin.ItemsSource = TableStirling.ItemsSource = TableBessel.ItemsSource = null;
        }
    }
}