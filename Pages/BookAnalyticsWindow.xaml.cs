using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;

namespace LibraryAccounting.Windows
{
    public partial class BookAnalyticsWindow : Window
    {
        public BookAnalyticsWindow()
        {
            InitializeComponent();
            ByGenre_Click(null, null);
        }

        /// <summary>
        /// Аналитика по жанрам
        /// </summary>
        private void ByGenre_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartTitle.Text = "Количество книг по жанрам";
                ShowBarChart();

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();

                var data = db.Books
                    .GroupBy(b => b.Genres.Name ?? "Не указан")
                    .Select(g => new { Genre = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                if (data.Count == 0)
                {
                    NoDataText.Visibility = Visibility.Visible;
                    return;
                }

                NoDataText.Visibility = Visibility.Collapsed;
                BarChart.Series.Clear();
                BarChart.AxisX.Clear();
                BarChart.AxisY.Clear();

                var values = new ChartValues<double>();
                var labels = new List<string>();

                foreach (var item in data)
                {
                    values.Add((double)item.Count);
                    labels.Add(item.Genre.Length > 20 ? item.Genre.Substring(0, 17) + "..." : item.Genre);
                }

                BarChart.Series.Add(new ColumnSeries
                {
                    Title = "Количество книг",
                    Values = values,
                    Fill = new SolidColorBrush(Color.FromRgb(47, 79, 79))
                });

                BarChart.AxisX.Add(new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    Separator = new LiveCharts.Wpf.Separator { Step = 1 }
                });
                BarChart.AxisY.Add(new Axis
                {
                    Title = "Количество",
                    LabelFormatter = value => value.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Аналитика по годам издания
        /// </summary>
        private void ByYear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartTitle.Text = "Количество книг по годам издания";
                ShowLineChart();

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();

                var data = db.Books
                    .Where(b => b.PublishYear != null)
                    .GroupBy(b => b.PublishYear)
                    .Select(g => new { Year = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Year)
                    .ToList();

                if (data.Count == 0)
                {
                    NoDataText.Visibility = Visibility.Visible;
                    return;
                }

                NoDataText.Visibility = Visibility.Collapsed;
                LineChart.Series.Clear();
                LineChart.AxisX.Clear();
                LineChart.AxisY.Clear();

                var values = new ChartValues<double>();
                var labels = new List<string>();

                foreach (var item in data)
                {
                    values.Add((double)item.Count);
                    labels.Add(item.Year.ToString());
                }

                LineChart.Series.Add(new LineSeries
                {
                    Title = "Количество книг",
                    Values = values,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(47, 79, 79)),
                    StrokeThickness = 2,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6
                });

                LineChart.AxisX.Add(new Axis
                {
                    Title = "Год",
                    Labels = labels,
                    LabelsRotation = 45
                });
                LineChart.AxisY.Add(new Axis
                {
                    Title = "Количество",
                    LabelFormatter = value => value.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Аналитика по языкам
        /// </summary>
        private void ByLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartTitle.Text = "Количество книг по языкам";
                ShowPieChart();

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Исправленный запрос - используем связанную таблицу Languages
                var data = db.Books
                    .GroupBy(b => b.Languages != null ? b.Languages.LanguageName : "Не указан")
                    .Select(g => new { Language = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                if (data.Count == 0)
                {
                    NoDataText.Visibility = Visibility.Visible;
                    return;
                }

                NoDataText.Visibility = Visibility.Collapsed;
                PieChart.Series.Clear();

                var colors = new[] { "#2F4F4F", "#4A7A7A", "#6B9C9C", "#8DBEBE", "#B0D0D0", "#D2E2E2" };
                int colorIndex = 0;

                foreach (var item in data)
                {
                    PieChart.Series.Add(new PieSeries
                    {
                        Title = $"{item.Language} ({item.Count})",
                        Values = new ChartValues<double> { (double)item.Count },
                        DataLabels = true,
                        LabelPoint = point => $"{point.Y:N0}",
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[colorIndex % colors.Length]))
                    });
                    colorIndex++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Аналитика по издательствам
        /// </summary>
        private void ByPublisher_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartTitle.Text = "Количество книг по издательствам";
                ShowBarChart();

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();

                var data = db.Books
                    .GroupBy(b => b.Publishers.PublisherName ?? "Не указано")
                    .Select(g => new { Publisher = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(15)
                    .ToList();

                if (data.Count == 0)
                {
                    NoDataText.Visibility = Visibility.Visible;
                    return;
                }

                NoDataText.Visibility = Visibility.Collapsed;
                BarChart.Series.Clear();
                BarChart.AxisX.Clear();
                BarChart.AxisY.Clear();

                var values = new ChartValues<double>();
                var labels = new List<string>();

                foreach (var item in data)
                {
                    values.Add((double)item.Count);
                    labels.Add(item.Publisher.Length > 20 ? item.Publisher.Substring(0, 17) + "..." : item.Publisher);
                }

                BarChart.Series.Add(new ColumnSeries
                {
                    Title = "Количество книг",
                    Values = values,
                    Fill = new SolidColorBrush(Color.FromRgb(47, 79, 79))
                });

                BarChart.AxisX.Add(new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    Separator = new LiveCharts.Wpf.Separator { Step = 1 }
                });
                BarChart.AxisY.Add(new Axis
                {
                    Title = "Количество",
                    LabelFormatter = value => value.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Топ популярных книг (по выдачам)
        /// </summary>
        private void TopPopular_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartTitle.Text = "Топ 15 самых популярных книг";
                ShowBarChart();

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();

                var data = db.Loans
                    .GroupBy(l => l.BookCopies.Books.Title)
                    .Select(g => new { Book = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(15)
                    .ToList();

                if (data.Count == 0)
                {
                    NoDataText.Visibility = Visibility.Visible;
                    return;
                }

                NoDataText.Visibility = Visibility.Collapsed;
                BarChart.Series.Clear();
                BarChart.AxisX.Clear();
                BarChart.AxisY.Clear();

                var values = new ChartValues<double>();
                var labels = new List<string>();

                foreach (var item in data)
                {
                    values.Add((double)item.Count);
                    labels.Add(item.Book.Length > 25 ? item.Book.Substring(0, 22) + "..." : item.Book);
                }

                BarChart.Series.Add(new ColumnSeries
                {
                    Title = "Количество выдач",
                    Values = values,
                    Fill = new SolidColorBrush(Color.FromRgb(47, 79, 79))
                });

                BarChart.AxisX.Add(new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    Separator = new LiveCharts.Wpf.Separator { Step = 1 }
                });
                BarChart.AxisY.Add(new Axis
                {
                    Title = "Количество выдач",
                    LabelFormatter = value => value.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Новые поступления (по дате добавления)
        /// </summary>
        private void NewArrivals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartTitle.Text = "Новые поступления по месяцам";
                ShowLineChart();

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();
                var now = DateTime.Now;
                var sixMonthsAgo = now.AddMonths(-6);

                var data = db.Books
                    .Where(b => b.AddedDate >= sixMonthsAgo && b.AddedDate != null)
                    .GroupBy(b => new { b.AddedDate.Year, b.AddedDate.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                if (data.Count == 0)
                {
                    NoDataText.Visibility = Visibility.Visible;
                    return;
                }

                NoDataText.Visibility = Visibility.Collapsed;
                LineChart.Series.Clear();
                LineChart.AxisX.Clear();
                LineChart.AxisY.Clear();

                var values = new ChartValues<double>();
                var labels = new List<string>();

                foreach (var item in data)
                {
                    values.Add((double)item.Count);
                    labels.Add($"{item.Month}.{item.Year}");
                }

                LineChart.Series.Add(new LineSeries
                {
                    Title = "Новые книги",
                    Values = values,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(47, 79, 79)),
                    StrokeThickness = 2,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6
                });

                LineChart.AxisX.Add(new Axis
                {
                    Title = "Месяц",
                    Labels = labels,
                    LabelsRotation = 45
                });
                LineChart.AxisY.Add(new Axis
                {
                    Title = "Количество",
                    LabelFormatter = value => value.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ShowBarChart()
        {
            BarChart.Visibility = Visibility.Visible;
            PieChart.Visibility = Visibility.Collapsed;
            LineChart.Visibility = Visibility.Collapsed;
        }

        private void ShowPieChart()
        {
            BarChart.Visibility = Visibility.Collapsed;
            PieChart.Visibility = Visibility.Visible;
            LineChart.Visibility = Visibility.Collapsed;
        }

        private void ShowLineChart()
        {
            BarChart.Visibility = Visibility.Collapsed;
            PieChart.Visibility = Visibility.Collapsed;
            LineChart.Visibility = Visibility.Visible;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"Аналитика_{ChartTitle.Text.Replace(" ", "_")}_{DateTime.Now:yyyy-MM-dd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(ChartTitle.Text);
                    sb.AppendLine("Категория;Количество");

                    // Для BarChart (столбчатая диаграмма)
                    if (BarChart.Visibility == Visibility.Visible && BarChart.Series.Count > 0)
                    {
                        var series = BarChart.Series[0] as ColumnSeries;
                        if (series != null && BarChart.AxisX.Count > 0)
                        {
                            var axis = BarChart.AxisX[0];
                            for (int i = 0; i < series.Values.Count && i < axis.Labels.Count; i++)
                            {
                                var label = axis.Labels[i];
                                var value = series.Values[i];
                                sb.AppendLine($"\"{label}\";{value}");
                            }
                        }
                    }
                    // Для PieChart (круговая диаграмма)
                    else if (PieChart.Visibility == Visibility.Visible && PieChart.Series.Count > 0)
                    {
                        foreach (PieSeries series in PieChart.Series)
                        {
                            var label = series.Title;
                            var value = series.Values[0];
                            sb.AppendLine($"\"{label}\";{value}");
                        }
                    }
                    // ДЛЯ LineChart (линейная диаграмма) - ВОТ ЧТО БЫЛО ПРОПУЩЕНО!
                    else if (LineChart.Visibility == Visibility.Visible && LineChart.Series.Count > 0)
                    {
                        var series = LineChart.Series[0] as LineSeries;
                        if (series != null && LineChart.AxisX.Count > 0)
                        {
                            var axis = LineChart.AxisX[0];
                            for (int i = 0; i < series.Values.Count && i < axis.Labels.Count; i++)
                            {
                                var label = axis.Labels[i];
                                var value = series.Values[i];
                                sb.AppendLine($"\"{label}\";{value}");
                            }
                        }
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Экспорт выполнен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}