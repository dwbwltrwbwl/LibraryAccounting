using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Text;
using System.Data.Entity;

namespace LibraryAccounting.Pages
{
    public partial class BookCopiesView : Page
    {
        private List<BookCopyViewModel> _allCopies;
        private ICollectionView _copiesView;
        private HashSet<int> _selectedCopyIds = new HashSet<int>();

        public class BookCopyViewModel
        {
            public int CopyId { get; set; }
            public string BookTitle { get; set; }
            public string InventoryNumber { get; set; }
            public string Location { get; set; }
            public string Status { get; set; }
            public DateTime AddedDate { get; set; }
            public string LastReaderName { get; set; }
            public DateTime? LastLoanDate { get; set; }
            public int TotalLoans { get; set; }
            public string ShelfCode { get; set; }
            public int RowNumber { get; set; }

            public string StatusDisplay
            {
                get
                {
                    switch (Status)
                    {
                        case "Available": return "✅ Доступна";
                        case "Issued": return "📤 Выдана";
                        case "Lost": return "❌ Потеряна";
                        case "WrittenOff": return "🗑 Списана";
                        default: return Status;
                    }
                }
            }
        }

        public BookCopiesView()
        {
            InitializeComponent();
            if (AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Visibility = Visibility.Collapsed;
                MassWriteOffButton.IsEnabled = false;
                MassWriteOffButton.Visibility = Visibility.Collapsed;
            }
            LoadCopies();
        }

        private void LoadCopies()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var copiesRaw = AppConnect.model01.BookCopies
                    .Include(c => c.Books)
                    .Include(c => c.Shelves)
                    .Include(c => c.Rows)
                    .Include(c => c.Readers)
                    .Select(c => new
                    {
                        c.CopyId,
                        BookTitle = c.Books.Title,
                        c.InventoryNumber,
                        c.Status,
                        c.AddedDate,
                        LastReaderName = c.LastReaderId != null ?
                            c.Readers.last_name + " " + c.Readers.first_name + " " + (c.Readers.middle_name ?? "") : "",
                        c.LastLoanDate,
                        c.TotalLoans,
                        ShelfCode = c.Shelves != null ? c.Shelves.ShelfCode : "",
                        RowNumber = c.Rows != null ? c.Rows.RowNumber : 0
                    })
                    .ToList();

                var copies = copiesRaw.Select(c => new BookCopyViewModel
                {
                    CopyId = c.CopyId,
                    BookTitle = c.BookTitle,
                    InventoryNumber = c.InventoryNumber,
                    Location = string.IsNullOrEmpty(c.ShelfCode) ? "Не указано" : $"Стеллаж {c.ShelfCode}, ряд {c.RowNumber}",
                    Status = c.Status,
                    AddedDate = c.AddedDate,
                    LastReaderName = string.IsNullOrEmpty(c.LastReaderName) ? "—" : c.LastReaderName,
                    LastLoanDate = c.LastLoanDate,
                    TotalLoans = c.TotalLoans
                })
                .OrderByDescending(c => c.AddedDate)
                .ToList();

                _allCopies = copies;
                _copiesView = CollectionViewSource.GetDefaultView(_allCopies);
                _copiesView.Filter = FilterCopies;

                CopiesDataGrid.ItemsSource = _copiesView;
                ApplySorting();
                UpdateStatistics();
                _selectedCopyIds.Clear();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void UpdateStatistics()
        {
            if (_allCopies == null) return;

            TotalCount.Text = _allCopies.Count.ToString();
            AvailableCount.Text = _allCopies.Count(c => c.Status == "Available").ToString();
            IssuedCount.Text = _allCopies.Count(c => c.Status == "Issued").ToString();
            LostCount.Text = _allCopies.Count(c => c.Status == "Lost").ToString();
            WrittenOffCount.Text = _allCopies.Count(c => c.Status == "WrittenOff").ToString();
        }

        private bool FilterCopies(object item)
        {
            var copy = item as BookCopyViewModel;
            if (copy == null) return false;

            string searchText = SearchTextBox?.Text?.Trim().ToLower() ?? "";
            var selectedStatusItem = StatusFilterComboBox?.SelectedItem as ComboBoxItem;
            string selectedStatus = selectedStatusItem?.Content?.ToString() ?? "Все статусы";

            bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                copy.BookTitle.ToLower().Contains(searchText) ||
                copy.InventoryNumber.ToLower().Contains(searchText) ||
                copy.Location.ToLower().Contains(searchText); // Изменено: ShelfCode -> Location

            bool matchesStatus = selectedStatus == "Все статусы" || copy.StatusDisplay == selectedStatus;

            return matchesSearch && matchesStatus;
        }

        private void ApplySorting()
        {
            if (_copiesView == null) return;

            _copiesView.SortDescriptions.Clear();

            string sort = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (sort)
            {
                case "По дате добавления (новые)":
                    _copiesView.SortDescriptions.Add(
                        new SortDescription("AddedDate", ListSortDirection.Descending));
                    break;
                case "По дате добавления (старые)":
                    _copiesView.SortDescriptions.Add(
                        new SortDescription("AddedDate", ListSortDirection.Ascending));
                    break;
                case "По инвентарному номеру (А-Я)":
                    _copiesView.SortDescriptions.Add(
                        new SortDescription("InventoryNumber", ListSortDirection.Ascending));
                    break;
                case "По инвентарному номеру (Я-А)":
                    _copiesView.SortDescriptions.Add(
                        new SortDescription("InventoryNumber", ListSortDirection.Descending));
                    break;
                case "По количеству выдач (убыв.)":
                    _copiesView.SortDescriptions.Add(
                        new SortDescription("TotalLoans", ListSortDirection.Descending));
                    break;
                case "По количеству выдач (возр.)":
                    _copiesView.SortDescriptions.Add(
                        new SortDescription("TotalLoans", ListSortDirection.Ascending));
                    break;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _copiesView?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при фильтрации: {ex.Message}");
            }
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _copiesView?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при фильтрации: {ex.Message}");
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySorting();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            StatusFilterComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            _copiesView?.Refresh();
        }

        #region Массовое списание

        private void RowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.Tag != null)
            {
                int copyId = (int)checkBox.Tag;
                _selectedCopyIds.Add(copyId);
            }
        }

        private void RowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.Tag != null)
            {
                int copyId = (int)checkBox.Tag;
                _selectedCopyIds.Remove(copyId);
            }
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            foreach (var item in CopiesDataGrid.Items)
            {
                var copy = item as BookCopyViewModel;
                if (copy != null && copy.Status != "WrittenOff")
                {
                    _selectedCopyIds.Add(copy.CopyId);
                }
            }

            // Обновляем чекбоксы в строках
            foreach (var item in CopiesDataGrid.Items)
            {
                var row = CopiesDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                {
                    var rowCheckBox = FindVisualChild<CheckBox>(row);
                    if (rowCheckBox != null)
                    {
                        var copy = item as BookCopyViewModel;
                        rowCheckBox.IsChecked = copy != null && copy.Status != "WrittenOff";
                    }
                }
            }
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectedCopyIds.Clear();

            foreach (var item in CopiesDataGrid.Items)
            {
                var row = CopiesDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                {
                    var rowCheckBox = FindVisualChild<CheckBox>(row);
                    if (rowCheckBox != null)
                    {
                        rowCheckBox.IsChecked = false;
                    }
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }

        private void MassWriteOff_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCopyIds.Count == 0)
            {
                ShowError("Выберите хотя бы один экземпляр для списания");
                return;
            }

            // Проверяем, можно ли списать выбранные экземпляры
            var selectedCopies = _allCopies.Where(c => _selectedCopyIds.Contains(c.CopyId)).ToList();
            var issuedCopies = selectedCopies.Where(c => c.Status == "Issued").ToList();

            if (issuedCopies.Any())
            {
                var issuedList = string.Join("\n", issuedCopies.Select(c => $"{c.InventoryNumber} - {c.BookTitle}"));
                MessageBox.Show(
                    $"Следующие экземпляры находятся в выдаче и не могут быть списаны:\n\n{issuedList}\n\n" +
                    "Сначала верните книги, затем повторите списание.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Подтверждение списания
            var dialog = new DeleteMessageDialog(
                "Массовое списание",
                $"Вы действительно хотите списать {_selectedCopyIds.Count} экземпляр(ов)?\n\n" +
                "Списание нельзя будет отменить!"
            );
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var db = AppConnect.model01 ?? new LibraryAccountingEntities();
                int writtenOffCount = 0;

                foreach (int copyId in _selectedCopyIds)
                {
                    var copy = db.BookCopies.FirstOrDefault(c => c.CopyId == copyId);
                    if (copy != null && copy.Status != "WrittenOff" && copy.Status != "Issued")
                    {
                        copy.Status = "WrittenOff";
                        writtenOffCount++;
                    }
                }

                db.SaveChanges();

                ShowInfo($"Успешно списано {writtenOffCount} экземпляр(ов)");
                LoadCopies();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при массовом списании: {ex.Message}");
            }
        }

        #endregion

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new BookCopyAddWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
                LoadCopies();
        }

        private void CopiesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CopiesDataGrid.SelectedItem == null)
                return;

            var selected = CopiesDataGrid.SelectedItem as BookCopyViewModel;
            if (selected == null) return;

            var copy = AppConnect.model01.BookCopies
                .FirstOrDefault(c => c.CopyId == selected.CopyId);

            if (copy == null)
                return;

            var window = new BookCopyAddWindow(copy)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
                LoadCopies();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CopiesDataGrid.SelectedItem == null)
            {
                ShowError("Выберите экземпляр для удаления");
                return;
            }

            var selected = CopiesDataGrid.SelectedItem as BookCopyViewModel;
            if (selected == null) return;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            bool isUsed = AppConnect.model01.Loans.Any(l => l.CopyId == selected.CopyId);

            if (isUsed)
            {
                ShowError("Невозможно удалить экземпляр.\nДля него существует история выдач.");
                return;
            }

            var copy = AppConnect.model01.BookCopies.FirstOrDefault(c => c.CopyId == selected.CopyId);

            if (copy == null)
            {
                ShowError("Экземпляр не найден");
                return;
            }

            var dialog = new DeleteMessageDialog(
                "Подтверждение удаления",
                $"Удалить экземпляр с инвентарным номером {copy.InventoryNumber}?"
            );
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() != true)
                return;

            AppConnect.model01.BookCopies.Remove(copy);
            AppConnect.model01.SaveChanges();

            LoadCopies();
            ShowInfo("Экземпляр успешно удалён");
        }

        private void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = _copiesView?.Cast<BookCopyViewModel>().ToList();

                if (items == null || items.Count == 0)
                {
                    items = _allCopies;
                }

                if (items == null || items.Count == 0)
                {
                    ShowError("Нет данных для экспорта");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    FileName = $"Экземпляры_книг_{DateTime.Now:yyyy-MM-dd_HHmmss}",
                    DefaultExt = "csv",
                    Title = "Сохранить CSV файл"
                };

                if (dialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine("\"Книга\";\"Инвентарный №\";\"Местоположение\";\"Статус\";\"Дата добавления\";\"Последний читатель\";\"Последняя выдача\";\"Всего выдач\"");

                    foreach (var copy in items)
                    {
                        string statusText = "";
                        switch (copy.Status)
                        {
                            case "Available": statusText = "Доступна"; break;
                            case "Issued": statusText = "Выдана"; break;
                            case "Lost": statusText = "Потеряна"; break;
                            case "WrittenOff": statusText = "Списана"; break;
                            default: statusText = copy.Status; break;
                        }

                        string lastLoanDate = copy.LastLoanDate?.ToString("dd.MM.yyyy") ?? "";

                        sb.AppendLine($"{EscapeCsv(copy.BookTitle)};{EscapeCsv(copy.InventoryNumber)};{EscapeCsv(copy.Location)};{statusText};{copy.AddedDate:dd.MM.yyyy};{EscapeCsv(copy.LastReaderName)};{lastLoanDate};{copy.TotalLoans}");
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    ShowInfo($"Экспорт выполнен успешно!\nФайл сохранен: {dialog.FileName}\nЭкспортировано записей: {items.Count}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при экспорте: {ex.Message}");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(";") || value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
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
    }
}