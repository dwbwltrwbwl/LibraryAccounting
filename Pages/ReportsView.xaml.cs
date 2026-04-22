using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using Microsoft.Win32;
using System;
using System.Collections;
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
using System.Data.Entity;

namespace LibraryAccounting.Pages
{
    public partial class ReportsView : Page
    {
        private List<dynamic> currentReportData;
        private List<dynamic> _allReportData;
        private string currentReportName;
        private List<ReportField> availableFields;
        private bool isCustomReport = false;
        private List<dynamic> currentDisplayedData;

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
            Loaded += ReportsView_Loaded;
        }

        private void ReportsView_Loaded(object sender, RoutedEventArgs e)
        {
            IssuedBooks_Click(null, null);
        }

        #region Основные отчеты

        private void IssuedBooks_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true);
            currentReportName = "Выданные книги";
            CurrentReportTitle.Text = "Отчёт: Выданные книги";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";
            isCustomReport = false;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var report = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null)
                    .Select(l => new
                    {
                        Читатель = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        Книга = l.BookCopies.Books.Title,
                        Инвентарный_номер = l.BookCopies.InventoryNumber,
                        Дата_выдачи = l.LoanDate,
                        Срок_возврата = l.DueDate
                    })
                    .ToList()
                    .Select(l => new
                    {
                        l.Читатель,
                        l.Книга,
                        l.Инвентарный_номер,
                        Дата_выдачи = l.Дата_выдачи.ToString("dd.MM.yyyy"),
                        Срок_возврата = l.Срок_возврата.ToString("dd.MM.yyyy")
                    })
                    .ToList();

                _allReportData = report.Cast<dynamic>().ToList();
                currentReportData = _allReportData;
                currentDisplayedData = currentReportData;

                ReportsDataGrid.AutoGenerateColumns = true;
                ReportsDataGrid.Columns.Clear();
                ReportsDataGrid.ItemsSource = currentReportData;

                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} записей";
                ResetFiltersUI();
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
            CurrentReportTitle.Text = "Отчёт: Просроченные книги";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";
            isCustomReport = false;

            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                DateTime today = DateTime.Now.Date;

                var report = AppConnect.model01.Loans
                    .Where(l => l.ReturnDate == null && l.DueDate < today)
                    .Select(l => new
                    {
                        Читатель = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                        Книга = l.BookCopies.Books.Title,
                        Инвентарный_номер = l.BookCopies.InventoryNumber,
                        Срок_возврата = l.DueDate
                    })
                    .ToList()
                    .Select(l => new
                    {
                        l.Читатель,
                        l.Книга,
                        l.Инвентарный_номер,
                        Просрочка_дней = (today - l.Срок_возврата).Days,
                        Срок_возврата = l.Срок_возврата.ToString("dd.MM.yyyy")
                    })
                    .OrderByDescending(x => x.Просрочка_дней)
                    .ToList();

                _allReportData = report.Cast<dynamic>().ToList();
                currentReportData = _allReportData;
                currentDisplayedData = currentReportData;

                ReportsDataGrid.AutoGenerateColumns = true;
                ReportsDataGrid.Columns.Clear();
                ReportsDataGrid.ItemsSource = currentReportData;

                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} просроченных книг";
                ResetFiltersUI();
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
            CurrentReportTitle.Text = "Отчёт: Популярные книги";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";
            isCustomReport = false;

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

                _allReportData = report.Cast<dynamic>().ToList();
                currentReportData = _allReportData;
                currentDisplayedData = currentReportData;

                ReportsDataGrid.AutoGenerateColumns = true;
                ReportsDataGrid.Columns.Clear();
                ReportsDataGrid.ItemsSource = currentReportData;

                UpdateStatistics();
                RecordsCount.Text = $"Топ {currentReportData.Count} книг";
                ResetFiltersUI();
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
            CurrentReportTitle.Text = "Отчёт: Активные читатели";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";
            isCustomReport = false;

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

                _allReportData = report.Cast<dynamic>().ToList();
                currentReportData = _allReportData;
                currentDisplayedData = currentReportData;

                ReportsDataGrid.AutoGenerateColumns = true;
                ReportsDataGrid.Columns.Clear();
                ReportsDataGrid.ItemsSource = currentReportData;

                UpdateStatistics();
                RecordsCount.Text = $"Топ {currentReportData.Count} читателей";
                ResetFiltersUI();
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
            CurrentReportTitle.Text = "Отчёт: Статистика по жанрам";
            HideCustomReportPanel();
            ShowDataGrid();
            SwitchViewBtn.Visibility = Visibility.Visible;
            SwitchViewBtn.Content = "📊 График";
            isCustomReport = false;

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
                    .OrderByDescending(x => x.Количество_выдач)
                    .ToList();

                _allReportData = report.Cast<dynamic>().ToList();
                currentReportData = _allReportData;
                currentDisplayedData = currentReportData;

                ReportsDataGrid.AutoGenerateColumns = true;
                ReportsDataGrid.Columns.Clear();
                ReportsDataGrid.ItemsSource = currentReportData;

                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} жанров";
                ResetFiltersUI();
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

        private void ResetFiltersUI()
        {
            DateFrom.SelectedDate = null;
            DateTo.SelectedDate = null;
            SearchBox.Text = "";
            SortByCombo.SelectedIndex = 0;
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

                ShowBarChart(chartData);
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

            var firstItem = (object)currentReportData[0];
            var props = firstItem.GetType().GetProperties();

            string labelProp = null;
            string valueProp = null;

            foreach (var prop in props)
            {
                string propName = prop.Name;
                if (labelProp == null && (propName.Contains("Название") || propName.Contains("Книга") || propName.Contains("Читатель") || propName.Contains("Жанр")))
                    labelProp = prop.Name;

                if (valueProp == null && (propName.Contains("Количество") || propName.Contains("Просрочка")))
                    valueProp = prop.Name;
            }

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

            return (labels, values);
        }

        private void ShowBarChart((List<string> Labels, List<double> Values) chartData)
        {
            BarChart.Visibility = Visibility.Visible;
            BarChart.Series.Clear();
            BarChart.AxisX.Clear();
            BarChart.AxisY.Clear();

            var series = new ColumnSeries
            {
                Title = currentReportName,
                Values = new ChartValues<double>(chartData.Values),
                Fill = new SolidColorBrush(Color.FromRgb(47, 79, 79))
            };

            BarChart.Series.Add(series);
            BarChart.AxisX.Add(new Axis
            {
                Labels = chartData.Labels,
                LabelsRotation = 45,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 }
            });
            BarChart.AxisY.Add(new Axis
            {
                LabelFormatter = value => value.ToString("N0"),
                MinValue = 0
            });
        }

        private void ShowPieChart((List<string> Labels, List<double> Values) chartData)
        {
            PieChart.Visibility = Visibility.Visible;
            PieChart.Series.Clear();

            foreach (var item in chartData.Labels.Zip(chartData.Values, (l, v) => new { l, v }))
            {
                var series = new PieSeries
                {
                    Title = item.l,
                    Values = new ChartValues<double> { item.v },
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y:N0}"
                };
                PieChart.Series.Add(series);
            }
        }

        private void ShowLineChart((List<string> Labels, List<double> Values) chartData)
        {
            LineChart.Visibility = Visibility.Visible;
            LineChart.Series.Clear();
            LineChart.AxisX.Clear();
            LineChart.AxisY.Clear();

            var series = new LineSeries
            {
                Title = currentReportName,
                Values = new ChartValues<double>(chartData.Values),
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(47, 79, 79)),
                StrokeThickness = 2,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 8
            };

            LineChart.Series.Add(series);
            LineChart.AxisX.Add(new Axis
            {
                Labels = chartData.Labels,
                LabelsRotation = 45,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 }
            });
            LineChart.AxisY.Add(new Axis
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
                CurrentReportTitle.Text = "Конструктор отчётов";
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
            CurrentReportTitle.Text = "Конструктор отчётов";
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
                        new ReportField { Name = "Срок возврата", FieldName = "DueDate", IsSelected = true },
                        new ReportField { Name = "Дата возврата", FieldName = "ReturnDate", IsSelected = false },
                        new ReportField { Name = "Количество продлений", FieldName = "ExtendCount", IsSelected = false },
                        new ReportField { Name = "Статус выдачи", FieldName = "LoanStatus", IsSelected = false },
                        new ReportField { Name = "Просрочка (дней)", FieldName = "OverdueDays", IsSelected = false }
                    });
                    break;

                case "Читатели":
                    availableFields.AddRange(new[]
                    {
                        new ReportField { Name = "ФИО", FieldName = "FullName", IsSelected = true },
                        new ReportField { Name = "Телефон", FieldName = "Phone", IsSelected = false },
                        new ReportField { Name = "Email", FieldName = "Email", IsSelected = false },
                        new ReportField { Name = "Паспортные данные", FieldName = "PassportData", IsSelected = false },
                        new ReportField { Name = "Дата регистрации", FieldName = "RegistrationDate", IsSelected = true },
                        new ReportField { Name = "Дата рождения", FieldName = "BirthDate", IsSelected = true },
                        new ReportField { Name = "Категория", FieldName = "Category", IsSelected = true },
                        new ReportField { Name = "Статус", FieldName = "Status", IsSelected = true },
                        new ReportField { Name = "Книг на руках", FieldName = "BooksOnHands", IsSelected = true }
                    });
                    break;

                case "Книжный фонд":
                    availableFields.AddRange(new[]
                    {
                        new ReportField { Name = "Название", FieldName = "Title", IsSelected = true },
                        new ReportField { Name = "Автор", FieldName = "Author", IsSelected = true },
                        new ReportField { Name = "Жанр", FieldName = "Genre", IsSelected = true },
                        new ReportField { Name = "Год издания", FieldName = "Year", IsSelected = true },
                        new ReportField { Name = "Издательство", FieldName = "Publisher", IsSelected = true },
                        new ReportField { Name = "ISBN", FieldName = "ISBN", IsSelected = false },
                        new ReportField { Name = "Страниц", FieldName = "Pages", IsSelected = false },
                        new ReportField { Name = "Язык", FieldName = "Language", IsSelected = false },
                        new ReportField { Name = "Переплёт", FieldName = "Binding", IsSelected = false },
                        new ReportField { Name = "Формат", FieldName = "Format", IsSelected = false },
                        new ReportField { Name = "Серия", FieldName = "Series", IsSelected = false },
                        new ReportField { Name = "Издание", FieldName = "Edition", IsSelected = false },
                        new ReportField { Name = "Тираж", FieldName = "Circulation", IsSelected = false },
                        new ReportField { Name = "Инвентарный номер", FieldName = "InventoryNumber", IsSelected = false },
                        new ReportField { Name = "Местоположение", FieldName = "Location", IsSelected = false },
                        new ReportField { Name = "Статус экземпляра", FieldName = "CopyStatus", IsSelected = false },
                        new ReportField { Name = "Стеллаж", FieldName = "Shelf", IsSelected = false },
                        new ReportField { Name = "Ряд", FieldName = "Row", IsSelected = false }
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
                    ShowError("Выберите тип отчёта");
                    return;
                }

                var selectedFields = availableFields?.Where(f => f.IsSelected).ToList();
                if (selectedFields == null || selectedFields.Count == 0)
                {
                    ShowError("Выберите хотя бы одно поле для отображения");
                    return;
                }

                if (!int.TryParse(LimitInput.Text, out int limit) || limit <= 0)
                {
                    limit = 100;
                }

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var selectedType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

                List<dynamic> reportData = new List<dynamic>();

                switch (selectedType)
                {
                    case "Читатели":
                        var readers = AppConnect.model01.Readers
                            .Include(r => r.ReaderCategories)
                            .ToList();

                        foreach (var reader in readers)
                        {
                            var expando = new ExpandoObject();
                            var dict = (IDictionary<string, object>)expando;

                            int booksOnHands = AppConnect.model01.Loans.Count(l => l.ReaderId == reader.ReaderId && l.ReturnDate == null);

                            foreach (var field in selectedFields)
                            {
                                switch (field.FieldName)
                                {
                                    case "FullName":
                                        dict[field.Name] = (reader.last_name + " " + reader.first_name + " " + (reader.middle_name ?? "")).Trim();
                                        break;
                                    case "Phone":
                                        dict[field.Name] = reader.Phone ?? "";
                                        break;
                                    case "Email":
                                        dict[field.Name] = reader.Email ?? "";
                                        break;
                                    case "PassportData":
                                        dict[field.Name] = reader.PassportData ?? "";
                                        break;
                                    case "RegistrationDate":
                                        dict[field.Name] = reader.RegistrationDate.ToString("dd.MM.yyyy");
                                        break;
                                    case "BirthDate":
                                        dict[field.Name] = reader.BirthDate?.ToString("dd.MM.yyyy") ?? "";
                                        break;
                                    case "Category":
                                        dict[field.Name] = reader.ReaderCategories?.CategoryName ?? "Не указана";
                                        break;
                                    case "Status":
                                        dict[field.Name] = reader.Status ?? "Активный";
                                        break;
                                    case "BooksOnHands":
                                        dict[field.Name] = booksOnHands;
                                        break;
                                }
                            }
                            reportData.Add(expando);
                        }
                        break;

                    case "Выдача книг":
                        var loans = AppConnect.model01.Loans
                            .Include(l => l.Readers)
                            .Include(l => l.BookCopies)
                            .Include(l => l.BookCopies.Books)
                            .ToList();

                        DateTime today = DateTime.Now.Date;

                        foreach (var loan in loans)
                        {
                            var expando = new ExpandoObject();
                            var dict = (IDictionary<string, object>)expando;

                            string loanStatus = "";
                            if (loan.ReturnDate != null)
                                loanStatus = "Возвращена";
                            else if (loan.DueDate < today)
                                loanStatus = "Просрочена";
                            else
                                loanStatus = "Активна";

                            int overdueDays = 0;
                            if (loan.ReturnDate == null && loan.DueDate < today)
                                overdueDays = (today - loan.DueDate).Days;

                            foreach (var field in selectedFields)
                            {
                                switch (field.FieldName)
                                {
                                    case "Reader":
                                        dict[field.Name] = (loan.Readers.last_name + " " + loan.Readers.first_name + " " + (loan.Readers.middle_name ?? "")).Trim();
                                        break;
                                    case "Book":
                                        dict[field.Name] = loan.BookCopies?.Books?.Title ?? "Не указано";
                                        break;
                                    case "InventoryNumber":
                                        dict[field.Name] = loan.BookCopies?.InventoryNumber ?? "Не указан";
                                        break;
                                    case "LoanDate":
                                        dict[field.Name] = loan.LoanDate.ToString("dd.MM.yyyy");
                                        break;
                                    case "DueDate":
                                        dict[field.Name] = loan.DueDate.ToString("dd.MM.yyyy");
                                        break;
                                    case "ReturnDate":
                                        dict[field.Name] = loan.ReturnDate?.ToString("dd.MM.yyyy") ?? "Не возвращена";
                                        break;
                                    case "ExtendCount":
                                        dict[field.Name] = loan.ExtendCount ?? 0;
                                        break;
                                    case "LoanStatus":
                                        dict[field.Name] = loanStatus;
                                        break;
                                    case "OverdueDays":
                                        dict[field.Name] = overdueDays;
                                        break;
                                }
                            }
                            reportData.Add(expando);
                        }
                        break;

                    case "Книжный фонд":
                        var copies = AppConnect.model01.BookCopies
                            .Include(c => c.Books)
                            .Include(c => c.Books.Authors)
                            .Include(c => c.Books.Genres)
                            .Include(c => c.Books.Publishers)
                            .Include(c => c.Books.Languages)
                            .Include(c => c.Books.Bindings)
                            .Include(c => c.Shelves)
                            .Include(c => c.Rows)
                            .ToList();

                        foreach (var copy in copies)
                        {
                            var expando = new ExpandoObject();
                            var dict = (IDictionary<string, object>)expando;

                            string shelfCode = copy.Shelves?.ShelfCode ?? "Не указан";
                            int rowNumber = copy.Rows?.RowNumber ?? 0;
                            string location = $"Стеллаж {shelfCode}, ряд {rowNumber}";

                            foreach (var field in selectedFields)
                            {
                                switch (field.FieldName)
                                {
                                    case "Title":
                                        dict[field.Name] = copy.Books?.Title ?? "Не указано";
                                        break;
                                    case "Author":
                                        dict[field.Name] = copy.Books?.Authors?.FullName ?? "Не указан";
                                        break;
                                    case "Genre":
                                        dict[field.Name] = copy.Books?.Genres?.Name ?? "Не указан";
                                        break;
                                    case "Year":
                                        dict[field.Name] = copy.Books?.PublishYear ?? 0;
                                        break;
                                    case "Publisher":
                                        dict[field.Name] = copy.Books?.Publishers?.PublisherName ?? "Не указано";
                                        break;
                                    case "ISBN":
                                        dict[field.Name] = copy.Books?.ISBN ?? "";
                                        break;
                                    case "Pages":
                                        dict[field.Name] = copy.Books?.Pages ?? 0;
                                        break;
                                    case "Language":
                                        dict[field.Name] = copy.Books?.Languages?.LanguageName ?? "Не указан";
                                        break;
                                    case "Binding":
                                        dict[field.Name] = copy.Books?.Bindings?.BindingName ?? "Не указан";
                                        break;
                                    case "Format":
                                        dict[field.Name] = copy.Books?.Format ?? "";
                                        break;
                                    case "Series":
                                        dict[field.Name] = copy.Books?.Series ?? "";
                                        break;
                                    case "Edition":
                                        dict[field.Name] = copy.Books?.Edition ?? "";
                                        break;
                                    case "Circulation":
                                        dict[field.Name] = copy.Books?.Circulation ?? 0;
                                        break;
                                    case "InventoryNumber":
                                        dict[field.Name] = copy.InventoryNumber ?? "";
                                        break;
                                    case "Location":
                                        dict[field.Name] = location;
                                        break;
                                    case "Shelf":
                                        dict[field.Name] = shelfCode;
                                        break;
                                    case "Row":
                                        dict[field.Name] = rowNumber;
                                        break;
                                    case "CopyStatus":
                                        string statusText = "";
                                        switch (copy.Status)
                                        {
                                            case "Available": statusText = "Доступна"; break;
                                            case "Issued": statusText = "Выдана"; break;
                                            case "Lost": statusText = "Потеряна"; break;
                                            case "WrittenOff": statusText = "Списана"; break;
                                            default: statusText = copy.Status; break;
                                        }
                                        dict[field.Name] = statusText;
                                        break;
                                }
                            }
                            reportData.Add(expando);
                        }
                        break;
                }

                if (limit > 0 && limit < reportData.Count)
                {
                    reportData = reportData.Take(limit).ToList();
                }

                _allReportData = reportData;
                currentReportData = _allReportData;
                currentDisplayedData = currentReportData;
                currentReportName = $"{selectedType}";
                isCustomReport = true;

                ReportsDataGrid.Columns.Clear();
                foreach (var field in selectedFields)
                {
                    ReportsDataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = field.Name,
                        Binding = new Binding(field.Name),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }

                ReportsDataGrid.ItemsSource = currentReportData;
                ReportsDataGrid.Visibility = Visibility.Visible;
                ChartViewPanel.Visibility = Visibility.Collapsed;
                SwitchViewBtn.Visibility = Visibility.Collapsed;

                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} записей";

                CustomReportPanel.Visibility = Visibility.Collapsed;
                CurrentReportTitle.Text = currentReportName;
                AddBackButton();

                ResetFiltersUI();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка создания отчёта: {ex.Message}");
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
            if (_allReportData == null) return;
            SetLoading(true);

            try
            {
                var filtered = _allReportData.AsEnumerable();

                if (DateFrom.SelectedDate.HasValue || DateTo.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(item =>
                    {
                        var dateProp = GetDateProperty(item);
                        if (dateProp == null) return true;

                        if (DateTime.TryParse(dateProp.ToString(), out DateTime itemDate))
                        {
                            bool include = true;
                            if (DateFrom.SelectedDate.HasValue && itemDate < DateFrom.SelectedDate.Value)
                                include = false;
                            if (DateTo.SelectedDate.HasValue && itemDate > DateTo.SelectedDate.Value)
                                include = false;
                            return include;
                        }
                        return true;
                    });
                }

                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    var searchTerm = SearchBox.Text.ToLower();
                    filtered = filtered.Where(item =>
                    {
                        foreach (var prop in item.GetType().GetProperties())
                        {
                            var value = prop.GetValue(item)?.ToString();
                            if (value != null && value.ToLower().Contains(searchTerm))
                                return true;
                        }
                        return false;
                    });
                }

                currentDisplayedData = filtered.ToList();
                ReportsDataGrid.ItemsSource = currentDisplayedData;
                UpdateStatistics();
                RecordsCount.Text = $"{currentDisplayedData.Count} записей (отфильтровано)";
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
            currentDisplayedData = _allReportData;
            ReportsDataGrid.ItemsSource = currentDisplayedData;
            UpdateStatistics();
            RecordsCount.Text = $"{_allReportData?.Count ?? 0} записей";
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
            var source = currentDisplayedData ?? _allReportData;
            if (source == null || !source.Any()) return;

            var view = CollectionViewSource.GetDefaultView(source);
            var selected = (SortByCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            view.SortDescriptions.Clear();

            if (selected == "По названию (А-Я)")
            {
                var firstItem = (object)source.First();
                foreach (var prop in firstItem.GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        view.SortDescriptions.Add(new SortDescription(prop.Name, ListSortDirection.Ascending));
                        break;
                    }
                }
            }
            else if (selected == "По названию (Я-А)")
            {
                var firstItem = (object)source.First();
                foreach (var prop in firstItem.GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        view.SortDescriptions.Add(new SortDescription(prop.Name, ListSortDirection.Descending));
                        break;
                    }
                }
            }
            else if (selected == "По дате (сначала новые)")
            {
                view.SortDescriptions.Add(new SortDescription("Дата_выдачи", ListSortDirection.Descending));
            }
            else if (selected == "По дате (сначала старые)")
            {
                view.SortDescriptions.Add(new SortDescription("Дата_выдачи", ListSortDirection.Ascending));
            }
            else if (selected == "По количеству (убыв.)")
            {
                view.SortDescriptions.Add(new SortDescription("Количество_выдач", ListSortDirection.Descending));
                view.SortDescriptions.Add(new SortDescription("Количество_книг", ListSortDirection.Descending));
            }
            else if (selected == "По количеству (возр.)")
            {
                view.SortDescriptions.Add(new SortDescription("Количество_выдач", ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription("Количество_книг", ListSortDirection.Ascending));
            }

            ReportsDataGrid.ItemsSource = view;
        }

        private object GetDateProperty(dynamic item)
        {
            var props = item.GetType().GetProperties();
            foreach (var prop in props)
            {
                string propName = prop.Name;
                if (propName.Contains("Дата") || propName.Contains("Date") || propName == "Дата_выдачи")
                    return prop.GetValue(item);
            }
            return null;
        }

        #endregion

        #region Экспорт в CSV

        private void ExportToCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var itemsSource = ReportsDataGrid.ItemsSource;

                if (itemsSource == null)
                {
                    ShowError("Нет данных для экспорта");
                    return;
                }

                var dataList = new List<object>();
                foreach (var item in itemsSource)
                {
                    dataList.Add(item);
                }

                if (dataList.Count == 0)
                {
                    ShowError("Нет данных для экспорта");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{currentReportName}_{DateTime.Now:yyyy-MM-dd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    SetLoading(true);

                    var sb = new StringBuilder();

                    var headers = new List<string>();
                    foreach (DataGridColumn column in ReportsDataGrid.Columns)
                    {
                        headers.Add(column.Header.ToString());
                    }

                    for (int i = 0; i < headers.Count; i++)
                    {
                        sb.Append($"\"{headers[i]}\"");
                        if (i < headers.Count - 1) sb.Append(";");
                    }
                    sb.AppendLine();

                    int rowCount = 0;
                    foreach (var item in dataList)
                    {
                        for (int i = 0; i < headers.Count; i++)
                        {
                            string value = GetPropertyValue(item, headers[i]);

                            if (value.Contains("\"") || value.Contains(";") || value.Contains("\n") || value.Contains("\r"))
                            {
                                value = value.Replace("\"", "\"\"");
                                value = $"\"{value}\"";
                            }

                            sb.Append(value);
                            if (i < headers.Count - 1) sb.Append(";");
                        }
                        sb.AppendLine();
                        rowCount++;
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    ShowInfo($"Отчёт успешно экспортирован в CSV\n{dialog.FileName}\nЭкспортировано записей: {rowCount}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка экспорта: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private string GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return "";

            try
            {
                if (obj is IDictionary<string, object> dict)
                {
                    if (dict.ContainsKey(propertyName))
                    {
                        return dict[propertyName]?.ToString() ?? "";
                    }
                }

                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    return value?.ToString() ?? "";
                }

                var props = obj.GetType().GetProperties();
                foreach (var p in props)
                {
                    if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = p.GetValue(obj);
                        return value?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения значения {propertyName}: {ex.Message}");
            }

            return "";
        }

        #endregion

        #region Копирование в буфер

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null) return;

            try
            {
                var sb = new StringBuilder();
                var data = currentDisplayedData ?? currentReportData;

                if (data != null && data.Count > 0)
                {
                    var firstItem = (object)data[0];
                    var properties = firstItem.GetType().GetProperties();

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
                ShowInfo("Данные скопированы в буфер обмена");
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

            int totalRecords = currentReportData.Count;
            SummaryText.Text = $"Всего записей: {totalRecords}";
        }

        private void ShowError(string message)
        {
            var dialog = new MessageDialog("Ошибка", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void ShowInfo(string message)
        {
            var dialog = new MessageDialog("Информация", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        #endregion
    }
}