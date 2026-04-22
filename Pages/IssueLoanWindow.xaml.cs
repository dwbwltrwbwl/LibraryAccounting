using LibraryAccounting.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LibraryAccounting.Pages
{
    public partial class IssueLoanWindow : Window
    {
        public int SelectedReaderId { get; private set; }
        public int SelectedCopyId { get; private set; }
        public int Days { get; private set; }

        private string _readerSearchText = "";
        private string _bookSearchText = "";

        public IssueLoanWindow()
        {
            InitializeComponent();
            LoadReaders();
            LoadBooks();
        }

        private void LoadReaders(string filter = null)
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var query = AppConnect.model01.Readers
                    .Select(r => new
                    {
                        r.ReaderId,
                        FullName = r.last_name + " " + r.first_name + " " + (r.middle_name ?? "")
                    })
                    .OrderBy(r => r.FullName);

                var readers = query.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    readers = readers.Where(r => r.FullName.ToLower().Contains(filter.ToLower()));
                }

                ReaderComboBox.ItemsSource = readers.ToList();
                ReaderComboBox.SelectedValuePath = "ReaderId";
                ReaderComboBox.DisplayMemberPath = "FullName";

                ReaderComboBox.Text = _readerSearchText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки читателей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBooks(string filter = null)
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var query = AppConnect.model01.BookCopies
                    .Where(c => c.Status == "Available")
                    .Select(c => new
                    {
                        c.CopyId,
                        Display = c.Books.Title + " (№" + c.InventoryNumber + ")"
                    })
                    .OrderBy(c => c.Display);

                var books = query.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    books = books.Where(b => b.Display.ToLower().Contains(filter.ToLower()));
                }

                BookComboBox.ItemsSource = books.ToList();
                BookComboBox.SelectedValuePath = "CopyId";
                BookComboBox.DisplayMemberPath = "Display";

                BookComboBox.Text = _bookSearchText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReaderComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            _readerSearchText = ReaderComboBox.Text?.Trim() ?? "";
            LoadReaders(_readerSearchText);
            ReaderComboBox.IsDropDownOpen = true;
        }

        private void BookComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            _bookSearchText = BookComboBox.Text?.Trim() ?? "";
            LoadBooks(_bookSearchText);
            BookComboBox.IsDropDownOpen = true;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ReaderComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите читателя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BookComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите книгу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DaysTextBox.Text, out int days) || days <= 0)
            {
                MessageBox.Show("Введите корректный срок (целое число больше 0)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка на максимальный срок - 60 дней
            if (days > 60)
            {
                MessageBox.Show("Максимальный срок выдачи - 60 дней", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedReaderId = (int)ReaderComboBox.SelectedValue;
            SelectedCopyId = (int)BookComboBox.SelectedValue;
            Days = days;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}