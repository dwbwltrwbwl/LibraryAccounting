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

namespace LibraryAccounting.Pages
{
    public partial class ReportsView : Page
    {
        private List<dynamic> currentReportData;
        private string currentReportName;
        private List<ReportField> availableFields;

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

        #region Создание своего отчета

        private void CustomReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReportsDataGrid.Visibility = Visibility.Collapsed;
                CustomReportPanel.Visibility = Visibility.Visible;
                CurrentReportTitle.Text = "Конструктор отчетов";

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
            if (currentReportData != null)
            {
                ReportsDataGrid.ItemsSource = currentReportData;
                CurrentReportTitle.Text = currentReportName;
            }
        }

        private void BackToConstructor_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к конструктору отчетов
            ReportsDataGrid.Visibility = Visibility.Collapsed;
            CustomReportPanel.Visibility = Visibility.Visible;
            CurrentReportTitle.Text = "Конструктор отчетов";
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
                        new ReportField { Name = "Отчество", FieldName = "MiddleName", IsSelected = true },
                        new ReportField { Name = "Телефон", FieldName = "Phone", IsSelected = true }
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

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
                var selectedType = (ReportTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

                // Получаем данные в зависимости от типа
                List<dynamic> fullReportData = new List<dynamic>();

                switch (selectedType)
                {
                    case "Выдача книг":
                        var loans = AppConnect.model01.Loans.ToList();
                        fullReportData = loans.Select(l => new
                        {
                            Читатель = l.Readers.last_name + " " + l.Readers.first_name + " " + (l.Readers.middle_name ?? ""),
                            Книга = l.BookCopies.Books.Title,
                            Инвентарный_номер = l.BookCopies.InventoryNumber,
                            Дата_выдачи = l.LoanDate.ToString("dd.MM.yyyy"),
                            Срок_возврата = l.DueDate.ToString("dd.MM.yyyy")
                        }).Cast<dynamic>().ToList();
                        break;
                    case "Читатели":
                        var readers = AppConnect.model01.Readers.ToList();
                        fullReportData = readers.Select(r => new
                        {
                            Фамилия = r.last_name,
                            Имя = r.first_name,
                            Отчество = r.middle_name ?? ""
                        }).Cast<dynamic>().ToList();
                        break;
                    case "Книжный фонд":
                        var books = AppConnect.model01.BookCopies.ToList();
                        fullReportData = books.Select(b => new
                        {
                            Название = b.Books.Title,
                            Автор = b.Books.Authors.FullName,
                            Жанр = b.Books.Genres?.Name ?? "Не указан"
                        }).Cast<dynamic>().ToList();
                        break;
                }

                // Фильтруем только выбранные поля
                var selectedFieldNames = selectedFields.Select(f => f.Name).ToList();
                var filteredReportData = new List<dynamic>();

                foreach (var item in fullReportData)
                {
                    var properties = item.GetType().GetProperties();
                    var filteredItem = new ExpandoObject();
                    var dict = (IDictionary<string, object>)filteredItem;

                    foreach (var prop in properties)
                    {
                        if (selectedFieldNames.Contains(prop.Name))
                        {
                            dict[prop.Name] = prop.GetValue(item);
                        }
                    }
                    filteredReportData.Add(filteredItem);
                }

                // Применяем лимит
                if (int.TryParse(LimitTextBox.Text, out int limit) && limit > 0 && limit < filteredReportData.Count)
                {
                    filteredReportData = filteredReportData.Take(limit).ToList();
                }

                currentReportData = filteredReportData;
                ReportsDataGrid.ItemsSource = currentReportData;
                UpdateStatistics();
                RecordsCount.Text = $"{currentReportData.Count} записей (пользовательский отчет)";

                // Показываем результат с кнопкой назад
                ReportsDataGrid.Visibility = Visibility.Visible;
                CustomReportPanel.Visibility = Visibility.Collapsed;
                CurrentReportTitle.Text = "Пользовательский отчет";

                // Добавляем кнопку назад в нижнюю панель
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
            RemoveBackButton();
        }

        private void ShowDataGrid()
        {
            ReportsDataGrid.Visibility = Visibility.Visible;
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
                            DateTime itemDate;
                            if (DateTime.TryParse(dateProp.ToString(), out itemDate))
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
                    break;
                case "По количеству (возр.)":
                    view.SortDescriptions.Add(new SortDescription("Количество_выдач", ListSortDirection.Ascending));
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
            if (ReportsDataGrid.ItemsSource == null)
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
                    var data = ReportsDataGrid.ItemsSource as System.Collections.IEnumerable;

                    if (data != null)
                    {
                        var enumerator = data.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            var firstItem = enumerator.Current;
                            var properties = firstItem.GetType().GetProperties();

                            for (int i = 0; i < properties.Length; i++)
                            {
                                sb.Append($"\"{properties[i].Name}\"");
                                if (i < properties.Length - 1) sb.Append(";");
                            }
                            sb.AppendLine();

                            enumerator.Reset();
                            while (enumerator.MoveNext())
                            {
                                var item = enumerator.Current;
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
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

                    // Используем MessageDialog как раньше
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
            if (ReportsDataGrid.ItemsSource == null) return;

            try
            {
                var sb = new StringBuilder();
                var data = ReportsDataGrid.ItemsSource as System.Collections.IEnumerable;

                if (data != null)
                {
                    var enumerator = data.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        var firstItem = enumerator.Current;
                        var properties = firstItem.GetType().GetProperties();

                        for (int i = 0; i < properties.Length; i++)
                        {
                            sb.Append(properties[i].Name);
                            if (i < properties.Length - 1) sb.Append("\t");
                        }
                        sb.AppendLine();

                        enumerator.Reset();
                        while (enumerator.MoveNext())
                        {
                            var item = enumerator.Current;
                            for (int i = 0; i < properties.Length; i++)
                            {
                                var value = properties[i].GetValue(item)?.ToString() ?? "";
                                sb.Append(value);
                                if (i < properties.Length - 1) sb.Append("\t");
                            }
                            sb.AppendLine();
                        }
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
            if (ReportsDataGrid.ItemsSource == null)
            {
                SummaryText.Text = "Нет данных";
                return;
            }

            var data = ReportsDataGrid.ItemsSource as System.Collections.IEnumerable;
            if (data == null)
            {
                SummaryText.Text = "Нет данных";
                return;
            }

            int count = 0;
            var enumerator = data.GetEnumerator();
            while (enumerator.MoveNext()) count++;

            SummaryText.Text = $"Всего записей: {count}";
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