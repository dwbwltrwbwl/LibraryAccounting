using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Text;
using System.IO;
using System;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class ReadersView : Page
    {
        private List<ReaderViewModel> _allReaders;

        public class ReaderViewModel
        {
            public int ReaderId { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string PassportData { get; set; }
            public DateTime RegistrationDate { get; set; }
            public DateTime? BirthDate { get; set; }
            public int CategoryId { get; set; }
            public string Category { get; set; }
            public int BooksOnHands { get; set; }
            public string Status { get; set; }
            public int OverdueDays { get; set; }
            public bool HasOverdue { get; set; }
            public bool HasNearDue { get; set; }
            public Visibility ShowOverdue => HasOverdue ? Visibility.Visible : Visibility.Collapsed;
            public Brush OverdueColor => HasOverdue ? Brushes.Red : (HasNearDue ? Brushes.Orange : Brushes.Gray);
        }

        public ReadersView()
        {
            InitializeComponent();

            if (AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Visibility = Visibility.Collapsed;
                AddButton.IsEnabled = false;
                AddButton.Visibility = Visibility.Collapsed;
            }

            Loaded += ReadersView_Loaded;
        }

        private void ReadersView_Loaded(object sender, RoutedEventArgs e)
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
            LoadCategories();
            LoadReaders();
            ApplyFilters();
        }

        private void LoadReaders()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var today = DateTime.Today;

            var readers = AppConnect.model01.Readers
                .Select(r => new ReaderViewModel
                {
                    ReaderId = r.ReaderId,
                    FullName = r.last_name + " " + r.first_name + (r.middle_name != null ? " " + r.middle_name : ""),
                    Phone = r.Phone,
                    Email = r.Email,
                    PassportData = r.PassportData,
                    RegistrationDate = r.RegistrationDate,
                    BirthDate = r.BirthDate,
                    CategoryId = r.CategoryId ?? 0,
                    Category = r.ReaderCategories.CategoryName,
                    Status = r.Status,
                    BooksOnHands = AppConnect.model01.Loans.Count(l => l.ReaderId == r.ReaderId && l.ReturnDate == null),
                    OverdueDays = 0,
                    HasOverdue = false,
                    HasNearDue = false
                })
                .ToList();

            // Вычисляем просрочки для каждого читателя
            foreach (var reader in readers)
            {
                var activeLoans = AppConnect.model01.Loans
                    .Where(l => l.ReaderId == reader.ReaderId && l.ReturnDate == null)
                    .ToList();

                int maxOverdue = 0;
                foreach (var loan in activeLoans)
                {
                    if (loan.DueDate < today)
                    {
                        int overdue = (today - loan.DueDate).Days;
                        if (overdue > maxOverdue) maxOverdue = overdue;
                    }
                }

                reader.OverdueDays = maxOverdue;
                reader.HasOverdue = maxOverdue > 0;
                reader.HasNearDue = !reader.HasOverdue && activeLoans.Any(l => (l.DueDate - today).Days <= 3 && (l.DueDate - today).Days > 0);
            }

            _allReaders = readers;
            ReadersDataGrid.ItemsSource = _allReaders;
        }

        private void LoadCategories()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var categories = AppConnect.model01.ReaderCategories
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToList();

            categories.Insert(0, new { CategoryId = 0, CategoryName = "Все категории" });

            CategoryFilter.ItemsSource = categories;
            CategoryFilter.DisplayMemberPath = "CategoryName";
            CategoryFilter.SelectedValuePath = "CategoryId";
            CategoryFilter.SelectedValue = 0;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ReaderEditWindow();
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadReaders();
        }

        private void ApplyFilters()
        {
            if (_allReaders == null)
                return;

            var result = _allReaders.AsEnumerable();

            string search = SearchTextBox.Text?.Trim().ToLower() ?? "";

            // Поиск
            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(r =>
                    r.FullName.ToLower().Contains(search) ||
                    (!string.IsNullOrEmpty(r.Phone) && r.Phone.Contains(search)) ||
                    (!string.IsNullOrEmpty(r.Email) && r.Email.ToLower().Contains(search))
                );
            }

            // Категория
            int categoryId = (int)(CategoryFilter?.SelectedValue ?? 0);
            if (categoryId != 0)
            {
                result = result.Where(r => r.CategoryId == categoryId);
            }

            // Статус
            string status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (status != "Все статусы")
            {
                result = result.Where(r => r.Status == status);
            }

            // Книги
            string books = (BooksFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (books == "Есть книги")
            {
                result = result.Where(r => r.BooksOnHands > 0);
            }
            else if (books == "Нет книг")
            {
                result = result.Where(r => r.BooksOnHands == 0);
            }

            // Сортировка
            string sort = (SortBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (sort)
            {
                case "ФИО (А-Я)":
                    result = result.OrderBy(r => r.FullName);
                    break;
                case "Дата регистрации":
                    result = result.OrderByDescending(r => r.RegistrationDate);
                    break;
                case "Книг на руках":
                    result = result.OrderByDescending(r => r.BooksOnHands);
                    break;
                default:
                    result = result.OrderBy(r => r.FullName);
                    break;
            }

            ReadersDataGrid.ItemsSource = result.ToList();
        }

        private void ReadersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ReadersDataGrid.SelectedItem == null)
                return;

            var selected = ReadersDataGrid.SelectedItem as ReaderViewModel;
            if (selected == null) return;

            var reader = AppConnect.model01.Readers
                .FirstOrDefault(r => r.ReaderId == selected.ReaderId);

            if (reader == null)
                return;

            var win = new ReaderEditWindow(reader);
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadReaders();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReadersDataGrid.SelectedItem == null)
            {
                ShowError("Выберите читателя для удаления");
                return;
            }

            var selected = ReadersDataGrid.SelectedItem as ReaderViewModel;
            if (selected == null) return;

            bool hasAnyLoans = AppConnect.model01.Loans.Any(l => l.ReaderId == selected.ReaderId);

            if (hasAnyLoans)
            {
                ShowError("Нельзя удалить читателя, у которого есть история выдачи книг.\nВместо удаления рекомендуется деактивировать учётную запись.");
                return;
            }

            var reader = AppConnect.model01.Readers
                .FirstOrDefault(r => r.ReaderId == selected.ReaderId);

            if (reader != null)
            {
                AppConnect.model01.Readers.Remove(reader);
                AppConnect.model01.SaveChanges();
                LoadReaders();
                ShowInfo("Читатель успешно удален");
            }
        }

        private void RemindOverdue_Click(object sender, RoutedEventArgs e)
        {
            var overdueReaders = _allReaders.Where(r => r.HasOverdue).ToList();

            if (!overdueReaders.Any())
            {
                ShowInfo("Нет читателей с просроченными книгами");
                return;
            }

            var message = new StringBuilder();
            message.AppendLine("Список читателей с просроченными книгами:");
            message.AppendLine(new string('-', 50));

            foreach (var reader in overdueReaders)
            {
                message.AppendLine($"📚 {reader.FullName}");
                message.AppendLine($"   Телефон: {reader.Phone ?? "не указан"}");
                message.AppendLine($"   Email: {reader.Email ?? "не указан"}");
                message.AppendLine($"   Просрочка: {reader.OverdueDays} дней");
                message.AppendLine();
            }

            var dialog = new MessageDialog("Напоминание о просрочках", message.ToString());
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int readerId = (int)button.Tag;

            var reader = _allReaders.FirstOrDefault(r => r.ReaderId == readerId);
            if (reader == null) return;

            // Получаем историю выдач
            var loans = AppConnect.model01.Loans
                .Where(l => l.ReaderId == readerId)
                .OrderByDescending(l => l.LoanDate)
                .Select(l => new
                {
                    l.LoanId,
                    BookTitle = l.BookCopies.Books.Title,
                    l.LoanDate,
                    l.DueDate,
                    l.ReturnDate,
                    Status = l.ReturnDate == null ? (l.DueDate < DateTime.Today ? "Просрочена" : "На руках") : "Возвращена"
                })
                .ToList();

            if (!loans.Any())
            {
                ShowInfo($"У читателя {reader.FullName} нет истории выдач");
                return;
            }

            var historyMessage = new StringBuilder();
            historyMessage.AppendLine($"История выдач: {reader.FullName}");
            historyMessage.AppendLine(new string('-', 60));
            historyMessage.AppendLine($"{"Дата выдачи",-12} {"Срок",-12} {"Возврат",-12} {"Статус",-12} Книга");
            historyMessage.AppendLine(new string('-', 60));

            foreach (var loan in loans)
            {
                historyMessage.AppendLine($"{loan.LoanDate:dd.MM.yyyy,-12} {loan.DueDate:dd.MM.yyyy,-12} " +
                    $"{(loan.ReturnDate?.ToString("dd.MM.yyyy") ?? "-----"),-12} {loan.Status,-12} {loan.BookTitle}");
            }

            var dialog = new MessageDialog("История выдач", historyMessage.ToString());
            dialog.Owner = Window.GetWindow(this);
            dialog.Width = 700;
            dialog.Height = 500;
            dialog.ShowDialog();
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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allReaders == null || !_allReaders.Any())
            {
                MessageBox.Show("Нет данных для экспорта");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "CSV файл (*.csv)|*.csv",
                FileName = $"Readers_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var data = ReadersDataGrid.ItemsSource as IEnumerable<ReaderViewModel>;
                if (data == null) return;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("ФИО;Телефон;Email;Паспорт;Дата регистрации;Дата рождения;Категория;Книг на руках;Статус;Просрочка(дней)");

                foreach (var r in data)
                {
                    sb.AppendLine($"{EscapeCsv(r.FullName)};{EscapeCsv(r.Phone)};{EscapeCsv(r.Email)};{EscapeCsv(r.PassportData)};" +
                                  $"{r.RegistrationDate:dd.MM.yyyy};{r.BirthDate:dd.MM.yyyy};{EscapeCsv(r.Category)};{r.BooksOnHands};{r.Status};{r.OverdueDays}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Экспорт успешно выполнен!\nФайл сохранен: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace(";", ",").Replace("\"", "\"\"");
        }
    }
}