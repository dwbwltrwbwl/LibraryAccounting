using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;

namespace LibraryAccounting.Pages
{
    public partial class ReportsView : Page
    {
        private List<dynamic> currentReportData;
        private string currentReportName;
        private List<ReportField> availableFields;
        private bool isCustomReport = false;

        public class ReportField : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public string FieldName { get; set; }
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ReportsView()
        {
            InitializeComponent();
            availableFields = new List<ReportField>();
            LoadAvailableFieldsForType();
            IssuedBooks_Click(null, null);
        }

        #region Основные отчеты

        private void IssuedBooks_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            currentReportName = "Выданные книги";
            CurrentReportTitle.Text = "Отчет: Выданные книги";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";

            ReportsDataGrid.Columns.Clear();
            ReportsDataGrid.AutoGenerateColumns = true;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var report = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null)
                    .ToList()
                    .Select(l => new
                    {
                        Читатель = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        Книга = l.BookCopies.Books.Title,
                        Инвентарный_номер = l.BookCopies.InventoryNumber,
                        Дата_выдачи = l.LoanDate.ToString("dd.MM.yyyy"),
                        Срок_возврата = l.DueDate.ToString("dd.MM.yyyy")
                    })
                    .ToList();

                currentReportData = report.Cast<dynamic>().ToList();
                ReportsDataGrid.ItemsSource = currentReportData;
                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} записей";
                isCustomReport = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки отчета: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void Overdue_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            currentReportName = "Просроченные книги";
            CurrentReportTitle.Text = "Отчет: Просроченные книги";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";

            ReportsDataGrid.Columns.Clear();
            ReportsDataGrid.AutoGenerateColumns = true;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                DateTime today = DateTime.Now.Date;

                var loans = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null)
                    .ToList();

                var report = loans
                    .Where(l => l.DueDate < today)
                    .Select(l => new
                    {
                        Читатель = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        Книга = l.BookCopies.Books.Title,
                        Инвентарный_номер = l.BookCopies.InventoryNumber,
                        Просрочка_дней = (today - l.DueDate).Days,
                        Срок_возврата = l.DueDate.ToString("dd.MM.yyyy")
                    })
                    .OrderByDescending(x => x.Просрочка_дней)
                    .ToList();

                currentReportData = report.Cast<dynamic>().ToList();
                ReportsDataGrid.ItemsSource = currentReportData;
                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} просроченных книг";
                isCustomReport = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки отчета: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void PopularBooks_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            currentReportName = "Популярные книги";
            CurrentReportTitle.Text = "Отчет: Популярные книги";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";

            ReportsDataGrid.Columns.Clear();
            ReportsDataGrid.AutoGenerateColumns = true;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var report = AppConnect.model01.Loans
                    .GroupBy(l => l.BookCopies.Books.Title)
                    .Select(g => new
                    {
                        Книга = g.Key,
                        Количество_выдач = g.Count()
                    })
                    .OrderByDescending(g => g.Количество_выдач)
                    .Take(20)
                    .ToList();

                currentReportData = report.Cast<dynamic>().ToList();
                ReportsDataGrid.ItemsSource = currentReportData;
                UpdateStatistics();
                RecordsCount.Text = $"Топ {currentReportData.Count} книг";
                isCustomReport = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки отчета: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ActiveReaders_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            currentReportName = "Активные читатели";
            CurrentReportTitle.Text = "Отчет: Активные читатели";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";

            ReportsDataGrid.Columns.Clear();
            ReportsDataGrid.AutoGenerateColumns = true;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var report = AppConnect.model01.Loans
                    .GroupBy(l => new { l.ReaderId, l.Readers.last_name, l.Readers.first_name, l.Readers.middle_name })
                    .Select(g => new
                    {
                        Читатель = g.Key.last_name + " " + g.Key.first_name + " " + (g.Key.middle_name ?? ""),
                        Количество_книг = g.Count()
                    })
                    .OrderByDescending(x => x.Количество_книг)
                    .Take(20)
                    .ToList();

                currentReportData = report.Cast<dynamic>().ToList();
                ReportsDataGrid.ItemsSource = currentReportData;
                UpdateStatistics();
                RecordsCount.Text = $"Топ {currentReportData.Count} читателей";
                isCustomReport = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки отчета: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void GenreStats_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            currentReportName = "Статистика по жанрам";
            CurrentReportTitle.Text = "Отчет: Статистика по жанрам";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";

            ReportsDataGrid.Columns.Clear();
            ReportsDataGrid.AutoGenerateColumns = true;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var report = AppConnect.model01.Loans
                    .Where(l => l.BookCopies.Books.Genres != null)
                    .GroupBy(l => l.BookCopies.Books.Genres.Name)
                    .Select(g => new
                    {
                        Жанр = g.Key ?? "Не указан",
                        Количество_выдач = g.Count()
                    })
                    .ToList();

                currentReportData = report.Cast<dynamic>().ToList();
                ReportsDataGrid.ItemsSource = currentReportData;
                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} жанров";
                isCustomReport = false;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки отчета: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        #endregion

        #region Переключение между таблицей и графиками

        private void SwitchView_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null || currentReportData.Count == 0)
            {
                ShowError("Нет данных для отображения");
                return;
            }

            if (ReportsDataGrid.Visibility == Visibility.Visible)
            {
                ShowChartView();
                SwitchViewBtn.Content = "📋 Таблица";
            }
            else
            {
                ShowDataGrid();
                SwitchViewBtn.Content = "📊 График";
            }
        }

        private void ShowChartView()
        {
            try
            {
                ReportsDataGrid.Visibility = Visibility.Collapsed;
                ChartViewPanel.Visibility = Visibility.Visible;

                BarChart.Visibility = Visibility.Collapsed;
                PieChart.Visibility = Visibility.Collapsed;
                LineChart.Visibility = Visibility.Collapsed;
                ChartMessage.Text = "";

                var chartData = PrepareChartData();

                if (chartData.Labels.Count == 0 || chartData.Values.Count == 0)
                {
                    ChartMessage.Text = "Недостаточно данных для построения графика";
                    return;
                }

                string chartType = DetermineChartType();

                switch (chartType)
                {
                    case "Столбчатая диаграмма":
                        ShowBarChart(chartData);
                        break;
                    case "Круговая диаграмма":
                        ShowPieChart(chartData);
                        break;
                    case "Линейный график":
                        ShowLineChart(chartData);
                        break;
                    default:
                        ShowBarChart(chartData);
                        break;
                }
            }
            catch (Exception ex)
            {
                ChartMessage.Text = $"Ошибка построения графика: {ex.Message}";
            }
        }

        private (List<string> Labels, List<double> Values) PrepareChartData()
        {
            var labels = new List<string>();
            var values = new List<double>();

            if (currentReportData == null || currentReportData.Count == 0)
                return (labels, values);

            // Пытаемся найти числовые поля
            var firstItem = (object)currentReportData[0];
            var props = firstItem.GetType().GetProperties();

            string labelProp = null;
            string valueProp = null;

            foreach (var prop in props)
            {
                if (labelProp == null && prop.PropertyType == typeof(string))
                    labelProp = prop.Name;

                if (valueProp == null && (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(double) || prop.PropertyType == typeof(long)))
                    valueProp = prop.Name;
            }

            // Если нашли и текстовое и числовое поле
            if (labelProp != null && valueProp != null)
            {
                var grouped = currentReportData
                    .Select(x => new
                    {
                        Label = ((object)x).GetType().GetProperty(labelProp).GetValue(x)?.ToString() ?? "Не указано",
                        Value = Convert.ToDouble(((object)x).GetType().GetProperty(valueProp).GetValue(x) ?? 0)
                    })
                    .GroupBy(x => x.Label)
                    .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Value) })
                    .OrderByDescending(x => x.Value)
                    .Take(15)
                    .ToList();

                foreach (var item in grouped)
                {
                    var displayLabel = item.Label.Length > 30 ? item.Label.Substring(0, 27) + "..." : item.Label;
                    labels.Add(displayLabel);
                    values.Add(item.Value);
                }
            }
            else if (labelProp != null)
            {
                // Только текстовое поле - считаем количество
                var grouped = currentReportData
                    .Select(x => ((object)x).GetType().GetProperty(labelProp).GetValue(x)?.ToString() ?? "Не указано")
                    .GroupBy(x => x)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(15)
                    .ToList();

                foreach (var item in grouped)
                {
                    var displayLabel = item.Label.Length > 30 ? item.Label.Substring(0, 27) + "..." : item.Label;
                    labels.Add(displayLabel);
                    values.Add(item.Count);
                }
            }

            return (labels, values);
        }

        private string DetermineChartType()
        {
            if (currentReportName.Contains("просроч"))
                return "Линейный график";

            if (currentReportData.Count <= 8)
                return "Круговая диаграмма";

            return "Столбчатая диаграмма";
        }

        private void ShowBarChart((List<string> Labels, List<double> Values) chartData)
        {
            BarChart.Visibility = Visibility.Visible;
            BarChart.Series.Clear();
            BarChart.AxisX.Clear();
            BarChart.AxisY.Clear();

            var series = new LiveCharts.Wpf.ColumnSeries
            {
                Title = currentReportName,
                Values = new LiveCharts.ChartValues<double>(chartData.Values),
                Fill = new SolidColorBrush(Color.FromRgb(47, 79, 79))
            };

            BarChart.Series.Add(series);
            BarChart.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Labels = chartData.Labels,
                LabelsRotation = 45,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 }
            });
            BarChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                LabelFormatter = value => value.ToString("N0"),
                MinValue = 0
            });
        }

        private void ShowPieChart((List<string> Labels, List<double> Values) chartData)
        {
            PieChart.Visibility = Visibility.Visible;
            PieChart.Series.Clear();

            var series = new LiveCharts.Wpf.PieSeries
            {
                Title = currentReportName,
                Values = new LiveCharts.ChartValues<double>(chartData.Values),
                DataLabels = true,
                LabelPoint = point => $"{point.Y:N0}"
            };

            PieChart.Series.Add(series);
        }

        private void ShowLineChart((List<string> Labels, List<double> Values) chartData)
        {
            LineChart.Visibility = Visibility.Visible;
            LineChart.Series.Clear();
            LineChart.AxisX.Clear();
            LineChart.AxisY.Clear();

            var series = new LiveCharts.Wpf.LineSeries
            {
                Title = currentReportName,
                Values = new LiveCharts.ChartValues<double>(chartData.Values),
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(47, 79, 79)),
                StrokeThickness = 2,
                PointGeometry = LiveCharts.Wpf.DefaultGeometries.Circle,
                PointGeometrySize = 8
            };

            LineChart.Series.Add(series);
            LineChart.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Labels = chartData.Labels,
                LabelsRotation = 45,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 }
            });
            LineChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                LabelFormatter = value => value.ToString("N0"),
                MinValue = 0
            });
        }

        #endregion

        #region Создание своего отчета

        private void CustomReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReportsDataGrid.Visibility = Visibility.Collapsed;
                ChartViewPanel.Visibility = Visibility.Collapsed;
                CustomReportPanel.Visibility = Visibility.Visible;
                CurrentReportTitle.Text = "Конструктор отчетов";
                SwitchViewBtn.Visibility = Visibility.Collapsed;

                if (ReportTypeCombo.SelectedIndex == -1)
                {
                    ReportTypeCombo.SelectedIndex = 0;
                }

                LoadAvailableFieldsForType();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка открытия конструктора: {ex.Message}");
            }
        }

        private void CloseCustomReport_Click(object sender, RoutedEventArgs e)
        {
            CustomReportPanel.Visibility = Visibility.Collapsed;
            ReportsDataGrid.Visibility = Visibility.Visible;
            SwitchViewBtn.Visibility = Visibility.Visible;

            if (currentReportData != null)
            {
                ReportsDataGrid.ItemsSource = currentReportData;
                CurrentReportTitle.Text = currentReportName;
            }

            RemoveBackButton();
        }

        private void BackToConstructor_Click(object sender, RoutedEventArgs e)
        {
            ReportsDataGrid.Visibility = Visibility.Collapsed;
            ChartViewPanel.Visibility = Visibility.Collapsed;
            CustomReportPanel.Visibility = Visibility.Visible;
            CurrentReportTitle.Text = "Конструктор отчетов";
            SwitchViewBtn.Visibility = Visibility.Collapsed;
            RemoveBackButton();
        }

        private void ReportTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FieldsListBox == null) return;
            LoadAvailableFieldsForType();
        }

        private void LoadAvailableFieldsForType()
        {
            if (FieldsListBox == null) return;

            var selectedType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(selectedType))
            {
                selectedType = "Выдача книг";
            }

            availableFields = new List<ReportField>();

            switch (selectedType)
            {
                case "Выдача книг":
                    availableFields.AddRange(new[]
                    {
                        new ReportField { Name = "Читатель", FieldName = "Reader", IsSelected = true },
                        new ReportField { Name = "Книга", FieldName = "Book", IsSelected = true },
                        new ReportField { Name = "Инвентарный номер", FieldName = "InventoryNumber", IsSelected = true },
                        new ReportField { Name = "Дата выдачи", FieldName = "LoanDate", IsSelected = true },
                        new ReportField { Name = "Срок возврата", FieldName = "DueDate", IsSelected = true }
                    });
                    break;
                case "Читатели":
                    availableFields.AddRange(new[]
                    {
                        new ReportField { Name = "Фамилия", FieldName = "LastName", IsSelected = true },
                        new ReportField { Name = "Имя", FieldName = "FirstName", IsSelected = true },
                        new ReportField { Name = "Отчество", FieldName = "MiddleName", IsSelected = true }
                    });
                    break;
                case "Книжный фонд":
                    availableFields.AddRange(new[]
                    {
                        new ReportField { Name = "Название", FieldName = "Title", IsSelected = true },
                        new ReportField { Name = "Автор", FieldName = "Author", IsSelected = true },
                        new ReportField { Name = "Жанр", FieldName = "Genre", IsSelected = true },
                        new ReportField { Name = "Год издания", FieldName = "Year", IsSelected = true }
                    });
                    break;
            }

            FieldsListBox.ItemsSource = availableFields;
        }

        private void PreviewCustomReport_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            try
            {
                if (ReportTypeCombo.SelectedItem == null)
                {
                    ShowError("Выберите тип отчета");
                    return;
                }

                var selectedFields = availableFields?.Where(f => f.IsSelected).ToList();
                if (selectedFields == null || selectedFields.Count == 0)
                {
                    ShowError("Выберите хотя бы одно поле для отображения");
                    return;
                }

                // Получаем выбранный тип визуализации
                var selectedVisualType = (VisualTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Таблица";

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var selectedType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

                ReportsDataGrid.Columns.Clear();

                foreach (var field in selectedFields)
                {
                    ReportsDataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = field.Name,
                        Binding = new Binding(field.Name)
                    });
                }

                List<dynamic> reportData = new List<dynamic>();

                switch (selectedType)
                {
                    case "Выдача книг":
                        var loans = AppConnect.model01.Loans.ToList();
                        foreach (var loan in loans)
                        {
                            var expando = new ExpandoObject();
                            var dict = (IDictionary<string, object>)expando;

                            foreach (var field in selectedFields)
                            {
                                switch (field.FieldName)
                                {
                                    case "Reader":
                                        dict[field.Name] = loan.Readers.last_name + " " + loan.Readers.first_name + " " + (loan.Readers.middle_name ?? "");
                                        break;
                                    case "Book":
                                        dict[field.Name] = loan.BookCopies.Books.Title;
                                        break;
                                    case "InventoryNumber":
                                        dict[field.Name] = loan.BookCopies.InventoryNumber;
                                        break;
                                    case "LoanDate":
                                        dict[field.Name] = loan.LoanDate;
                                        break;
                                    case "DueDate":
                                        dict[field.Name] = loan.DueDate.ToString("dd.MM.yyyy");
                                        break;
                                }
                            }
                            reportData.Add(expando);
                        }
                        break;

                    case "Читатели":
                        var readers = AppConnect.model01.Readers.ToList();
                        foreach (var reader in readers)
                        {
                            var expando = new ExpandoObject();
                            var dict = (IDictionary<string, object>)expando;

                            foreach (var field in selectedFields)
                            {
                                switch (field.FieldName)
                                {
                                    case "LastName":
                                        dict[field.Name] = reader.last_name;
                                        break;
                                    case "FirstName":
                                        dict[field.Name] = reader.first_name;
                                        break;
                                    case "MiddleName":
                                        dict[field.Name] = reader.middle_name ?? "";
                                        break;
                                }
                            }
                            reportData.Add(expando);
                        }
                        break;

                    case "Книжный фонд":
                        var copies = AppConnect.model01.BookCopies.ToList();
                        foreach (var copy in copies)
                        {
                            var expando = new ExpandoObject();
                            var dict = (IDictionary<string, object>)expando;

                            foreach (var field in selectedFields)
                            {
                                switch (field.FieldName)
                                {
                                    case "Title":
                                        dict[field.Name] = copy.Books.Title;
                                        break;
                                    case "Author":
                                        dict[field.Name] = copy.Books.Authors?.FullName ?? "Не указан";
                                        break;
                                    case "Genre":
                                        dict[field.Name] = copy.Books.Genres?.Name ?? "Не указан";
                                        break;
                                    case "Year":
                                        dict[field.Name] = copy.Books.PublishYear ?? 0;
                                        break;
                                }
                            }
                            reportData.Add(expando);
                        }
                        break;
                }

                if (int.TryParse(LimitInput.Text, out int limit) && limit > 0 && limit < reportData.Count)
                {
                    reportData = reportData.Take(limit).ToList();
                }

                currentReportData = reportData;
                foreach (var item in reportData)
                {
                    var dict = (IDictionary<string, object>)item;

                    if (!dict.ContainsKey("Количество"))
                        dict["Количество"] = 1;
                }
                currentReportName = "Пользовательский отчет";
                isCustomReport = true;

                // Показываем в зависимости от выбранного типа визуализации
                if (selectedVisualType == "Таблица")
                {
                    ReportsDataGrid.ItemsSource = currentReportData;
                    ReportsDataGrid.Visibility = Visibility.Visible;
                    ChartViewPanel.Visibility = Visibility.Collapsed;
                    SwitchViewBtn.Content = "📊 График";
                }
                else
                {
                    ReportsDataGrid.Visibility = Visibility.Collapsed;
                    ChartViewPanel.Visibility = Visibility.Visible;
                    SwitchViewBtn.Content = "📋 Таблица";

                    // Скрываем все графики
                    BarChart.Visibility = Visibility.Collapsed;
                    PieChart.Visibility = Visibility.Collapsed;
                    LineChart.Visibility = Visibility.Collapsed;
                    ChartMessage.Text = "";

                    // Подготавливаем данные для графика
                    var chartData = PrepareChartData();

                    if (chartData.Labels.Count == 0 || chartData.Values.Count == 0)
                    {
                        ChartMessage.Text = "Недостаточно данных для построения графика";
                    }
                    else
                    {
                        // Показываем выбранный тип графика
                        switch (selectedVisualType)
                        {
                            case "Столбчатая диаграмма":
                                ShowBarChart(chartData);
                                break;
                            case "Круговая диаграмма":
                                ShowPieChart(chartData);
                                break;
                            case "Линейный график":
                                ShowLineChart(chartData);
                                break;
                        }
                    }
                }

                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} записей (пользовательский отчет)";

                CustomReportPanel.Visibility = Visibility.Collapsed;
                CurrentReportTitle.Text = "Пользовательский отчет";
                SwitchViewBtn.Visibility = Visibility.Visible;

                AddBackButton();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка создания отчета: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void AddBackButton()
        {
            BackToConstructorBtn.Visibility = Visibility.Visible;
        }

        private void RemoveBackButton()
        {
            BackToConstructorBtn.Visibility = Visibility.Collapsed;
        }

        private void ExportCustomReport_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData != null && currentReportData.Count > 0)
            {
                ExportToCSV_Click(null, null);
            }
            else
            {
                ShowError("Нет данных для экспорта");
            }
        }

        #endregion

        #region Вспомогательные методы для UI

        private void HideCustomReportPanel()
        {
            CustomReportPanel.Visibility = Visibility.Collapsed;
            ChartViewPanel.Visibility = Visibility.Collapsed;
            RemoveBackButton();
        }

        private void ShowDataGrid()
        {
            ReportsDataGrid.Visibility = Visibility.Visible;
            ChartViewPanel.Visibility = Visibility.Collapsed;
            SwitchViewBtn.Content = "📊 График";
            RemoveBackButton();
        }

        #endregion

        #region Фильтрация и поиск

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null) return;
            SetLoading(true);

            try
            {
                var filtered = new List<dynamic>();

                foreach (var item in currentReportData)
                {
                    bool include = true;

                    if (DateFrom.SelectedDate.HasValue || DateTo.SelectedDate.HasValue)
                    {
                        var dateProp = GetDateProperty(item);
                        if (dateProp != null)
                        {
                            if (DateTime.TryParse(dateProp.ToString(), out DateTime itemDate))
                            {
                                if (DateFrom.SelectedDate.HasValue && itemDate < DateFrom.SelectedDate.Value)
                                    include = false;
                                if (DateTo.SelectedDate.HasValue && itemDate > DateTo.SelectedDate.Value)
                                    include = false;
                            }
                        }
                    }

                    if (include && !string.IsNullOrWhiteSpace(SearchBox.Text))
                    {
                        var searchTerm = SearchBox.Text.ToLower();
                        bool found = false;
                        foreach (var prop in item.GetType().GetProperties())
                        {
                            var value = prop.GetValue(item)?.ToString();
                            if (value != null && value.ToLower().Contains(searchTerm))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found) include = false;
                    }

                    if (include) filtered.Add(item);
                }

                ReportsDataGrid.ItemsSource = filtered;
                UpdateStatistics();
                RecordsCount.Text = $"{filtered.Count} записей (отфильтровано)";
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка фильтрации: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            DateFrom.SelectedDate = null;
            DateTo.SelectedDate = null;
            SearchBox.Text = "";
            ReportsDataGrid.ItemsSource = currentReportData;
            UpdateStatistics();
            RecordsCount.Text = $"{currentReportData?.Count ?? 0} записей";
        }

        private void QuickFilterToday_Click(object sender, RoutedEventArgs e)
        {
            DateFrom.SelectedDate = DateTime.Today;
            DateTo.SelectedDate = DateTime.Today;
            ApplyFilter_Click(null, null);
        }

        private void QuickFilterWeek_Click(object sender, RoutedEventArgs e)
        {
            DateTo.SelectedDate = DateTime.Today;
            DateFrom.SelectedDate = DateTime.Today.AddDays(-7);
            ApplyFilter_Click(null, null);
        }

        private void QuickFilterMonth_Click(object sender, RoutedEventArgs e)
        {
            DateTo.SelectedDate = DateTime.Today;
            DateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            ApplyFilter_Click(null, null);
        }

        private void QuickFilterQuarter_Click(object sender, RoutedEventArgs e)
        {
            DateTo.SelectedDate = DateTime.Today;
            DateFrom.SelectedDate = DateTime.Today.AddMonths(-3);
            ApplyFilter_Click(null, null);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter_Click(null, null);
        }

        private void SortByCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportsDataGrid.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(ReportsDataGrid.ItemsSource);
            var selected = (SortByCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            view.SortDescriptions.Clear();

            switch (selected)
            {
                case "По названию (А-Я)":
                    foreach (var prop in GetStringProperties())
                    {
                        view.SortDescriptions.Add(new SortDescription(prop, ListSortDirection.Ascending));
                        break;
                    }
                    break;
                case "По названию (Я-А)":
                    foreach (var prop in GetStringProperties())
                    {
                        view.SortDescriptions.Add(new SortDescription(prop, ListSortDirection.Descending));
                        break;
                    }
                    break;
                case "По дате (сначала новые)":
                    view.SortDescriptions.Add(new SortDescription("Дата_выдачи", ListSortDirection.Descending));
                    break;
                case "По дате (сначала старые)":
                    view.SortDescriptions.Add(new SortDescription("Дата_выдачи", ListSortDirection.Ascending));
                    break;
                case "По количеству (убыв.)":
                    view.SortDescriptions.Add(new SortDescription("Количество_выдач", ListSortDirection.Descending));
                    view.SortDescriptions.Add(new SortDescription("Количество_книг", ListSortDirection.Descending));
                    break;
                case "По количеству (возр.)":
                    view.SortDescriptions.Add(new SortDescription("Количество_выдач", ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new SortDescription("Количество_книг", ListSortDirection.Ascending));
                    break;
            }
        }

        private List<string> GetStringProperties()
        {
            if (currentReportData == null || currentReportData.Count == 0) return new List<string>();
            var props = new List<string>();
            foreach (var prop in currentReportData[0].GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                    props.Add(prop.Name);
            }
            return props;
        }

        private object GetDateProperty(dynamic item)
        {
            var props = item.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.Name.Contains("Дата") || prop.Name.Contains("Date"))
                    return prop.GetValue(item);
            }
            return null;
        }

        #endregion

        #region Экспорт в CSV

        private void ExportToCSV_Click(object sender, RoutedEventArgs e)
        {
            if (ReportsDataGrid.ItemsSource == null && currentReportData == null)
            {
                ShowError("Нет данных для экспорта");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"{currentReportName}_{DateTime.Now:yyyy-MM-dd}"
            };

            if (dialog.ShowDialog() == true)
            {
                SetLoading(true);
                try
                {
                    var sb = new StringBuilder();
                    var data = currentReportData;

                    if (data != null && data.Count > 0)
                    {
                        var firstItem = data[0];
                        var properties = ((object)firstItem).GetType().GetProperties();

                        for (int i = 0; i < properties.Length; i++)
                        {
                            sb.Append($"\"{properties[i].Name}\"");
                            if (i < properties.Length - 1) sb.Append(";");
                        }
                        sb.AppendLine();

                        foreach (var item in data)
                        {
                            for (int i = 0; i < properties.Length; i++)
                            {
                                var value = properties[i].GetValue(item)?.ToString() ?? "";
                                value = value.Replace("\"", "\"\"");
                                sb.Append($"\"{value}\"");
                                if (i < properties.Length - 1) sb.Append(";");
                            }
                            sb.AppendLine();
                        }
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

                    var successDialog = new MessageDialog("Информация", $"Отчет успешно экспортирован в CSV\n{dialog.FileName}");
                    successDialog.Owner = Window.GetWindow(this);
                    successDialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка экспорта: {ex.Message}");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        #endregion

        #region Копирование в буфер

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null) return;

            try
            {
                var sb = new StringBuilder();
                var data = currentReportData;

                if (data != null && data.Count > 0)
                {
                    var firstItem = data[0];
                    var properties = ((object)firstItem).GetType().GetProperties();

                    for (int i = 0; i < properties.Length; i++)
                    {
                        sb.Append(properties[i].Name);
                        if (i < properties.Length - 1) sb.Append("\t");
                    }
                    sb.AppendLine();

                    foreach (var item in data)
                    {
                        for (int i = 0; i < properties.Length; i++)
                        {
                            var value = properties[i].GetValue(item)?.ToString() ?? "";
                            sb.Append(value);
                            if (i < properties.Length - 1) sb.Append("\t");
                        }
                        sb.AppendLine();
                    }
                }

                Clipboard.SetText(sb.ToString());

                var infoDialog = new MessageDialog("Информация", "Данные скопированы в буфер обмена");
                infoDialog.Owner = Window.GetWindow(this);
                infoDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка копирования: {ex.Message}");
            }
        }

        #endregion

        #region Вспомогательные методы

        private void SetLoading(bool isLoading)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ReportsDataGrid.IsEnabled = !isLoading;
        }

        private void UpdateStatistics()
        {
            if (currentReportData == null)
            {
                SummaryText.Text = "Нет данных";
                return;
            }

            SummaryText.Text = $"Всего записей: {currentReportData.Count}";
        }

        private void ShowError(string message)
        {
            var dialog = new MessageDialog("Ошибка", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        #endregion
    }
}