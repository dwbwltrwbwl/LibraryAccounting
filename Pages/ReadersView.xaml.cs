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

namespace LibraryAccounting.Pages
{
    public partial class ReadersView : Page
    {
        private List<dynamic> _allReaders;
        public ReadersView()
        {
            InitializeComponent();
            if (AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Visibility = Visibility.Collapsed; // можно оставить только IsEnabled=false
                AddButton.IsEnabled = false;
                AddButton.Visibility = Visibility.Collapsed;
            }
            Loaded += ReadersView_Loaded;
        }
        private void ReadersView_Loaded(object sender, RoutedEventArgs e)
        {
            AppConnect.model01 = new LibraryAccountingEntities();
            
            LoadCategories();
            LoadReaders();

            // теперь _allReaders уже точно есть
            ApplyFilters();
        }
        /// <summary>
        /// Загрузка всех читателей
        /// </summary>
        private void LoadReaders()
        {
            if (_allReaders == null)
            {
                _allReaders = new List<dynamic>();
            }

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            _allReaders = AppConnect.model01.Readers
    .Select(r => new
    {
        r.ReaderId,

        FullName =
            r.last_name + " " +
            r.first_name +
            (r.middle_name != null ? " " + r.middle_name : ""),

        r.Phone,
        r.Email,
        r.PassportData,
        r.RegistrationDate,

        BirthDate = r.BirthDate,
        Status = r.Status,

        CategoryId = r.CategoryId,
        Category = r.ReaderCategories.CategoryName,

        BooksOnHands = AppConnect.model01.Loans
            .Count(l => l.ReaderId == r.ReaderId && l.ReturnDate == null)
    })
    .ToList<dynamic>();

            ReadersDataGrid.ItemsSource = _allReaders;
        }
        private void LoadCategories()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var categories = AppConnect.model01.ReaderCategories
                        .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName
                })
                .ToList();

            // добавляем "Все категории"
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
        /// <summary>
        /// Добавление читателя (заглушка)
        /// </summary>
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

            if (SearchTextBox == null)
                return;

            var result = _allReaders.AsEnumerable();

            string search = SearchTextBox.Text?.Trim().ToLower() ?? "";

            // 🔍 Поиск
            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(r =>
                    r.FullName.ToLower().Contains(search) ||
                    (!string.IsNullOrEmpty(r.Phone) && r.Phone.Contains(search)) ||
                    (!string.IsNullOrEmpty(r.Email) && r.Email.ToLower().Contains(search))
                );
            }

            // 📂 Категория
            int categoryId = (int)(CategoryFilter?.SelectedValue ?? 0);

            if (categoryId != 0)
            {
                result = result.Where(r => r.CategoryId == categoryId);
            }

            // 📊 Статус
            string status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (status != "Все статусы")
            {
                result = result.Where(r => r.Status == status);
            }

            // 📚 Книги
            string books = (BooksFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (books == "Есть книги")
            {
                result = result.Where(r => r.BooksOnHands > 0);
            }
            else if (books == "Нет книг")
            {
                result = result.Where(r => r.BooksOnHands == 0);
            }

            // 🔽 Сортировка
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
            }

            ReadersDataGrid.ItemsSource = result.ToList();
        }
        /// <summary>
        /// Редактирование читателя (заглушка)
        /// </summary>
        private void ReadersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ReadersDataGrid.SelectedItem == null)
                return;

            dynamic selected = ReadersDataGrid.SelectedItem;
            int readerId = selected.ReaderId;

            var reader = AppConnect.model01.Readers
                .FirstOrDefault(r => r.ReaderId == readerId);

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

            dynamic selected = ReadersDataGrid.SelectedItem;
            int readerId = selected.ReaderId;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            // Проверяем наличие ЛЮБЫХ записей о выдаче (даже возвращённых)
            bool hasAnyLoans = AppConnect.model01.Loans.Any(l => l.ReaderId == readerId);

            if (hasAnyLoans)
            {
                ShowError("Нельзя удалить читателя, у которого есть история выдачи книг.\nВместо удаления рекомендуется деактивировать учётную запись.");
                return;
            }

            var reader = AppConnect.model01.Readers
                .FirstOrDefault(r => r.ReaderId == readerId);

            if (reader != null)
            {
                AppConnect.model01.Readers.Remove(reader);
                AppConnect.model01.SaveChanges();

                LoadReaders();
                ShowInfo("Читатель успешно удален");
            }
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
                FileName = "Readers.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var data = ReadersDataGrid.ItemsSource as IEnumerable<dynamic>;

                StringBuilder sb = new StringBuilder();

                // Заголовки
                sb.AppendLine("ФИО;Телефон;Email;Паспорт;Дата регистрации;Дата рождения;Категория;Книг на руках;Статус");

                foreach (var r in data)
                {
                    sb.AppendLine($"{r.FullName};{r.Phone};{r.Email};{r.PassportData};" +
                                  $"{r.RegistrationDate:d};{r.BirthDate:d};{r.Category};{r.BooksOnHands};{r.Status}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

                MessageBox.Show("Экспорт успешно выполнен");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }
    }
}
