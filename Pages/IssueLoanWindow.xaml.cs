using LibraryAccounting.AppData;
using System;
using System.Linq;
using System.Windows;

namespace LibraryAccounting.Pages
{
    public partial class IssueLoanWindow : Window
    {
        public int SelectedReaderId { get; private set; }
        public int SelectedCopyId { get; private set; }
        public int Days { get; private set; }

        public IssueLoanWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Читатели
                var readers = AppConnect.model01.Readers
                    .Select(r => new
                    {
                        r.ReaderId,
                        FullName = r.last_name + " " + r.first_name + " " + (r.middle_name ?? "")
                    })
                    .ToList();

                ReaderComboBox.ItemsSource = readers;
                ReaderComboBox.SelectedValuePath = "ReaderId";
                ReaderComboBox.DisplayMemberPath = "FullName";

                // Книги (ТОЛЬКО доступные)
                var books = AppConnect.model01.BookCopies
                    .Where(c => c.Status == "Available")
                    .Select(c => new
                    {
                        c.CopyId,
                        Display = c.Books.Title + " (№" + c.InventoryNumber + ")"
                    })
                    .ToList();

                BookComboBox.ItemsSource = books;
                BookComboBox.SelectedValuePath = "CopyId";
                BookComboBox.DisplayMemberPath = "Display";

                // Если есть данные, выбираем первый элемент по умолчанию
                if (ReaderComboBox.Items.Count > 0)
                    ReaderComboBox.SelectedIndex = 0;
                if (BookComboBox.Items.Count > 0)
                    BookComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("Введите корректный срок (целое число больше 0)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (days > 60)
            {
                if (MessageBox.Show("Максимальный срок выдачи - 60 дней. Продолжить?", "Предупреждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }
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