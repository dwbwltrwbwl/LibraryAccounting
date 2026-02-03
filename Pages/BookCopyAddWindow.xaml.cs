using LibraryAccounting.AppData;
using System.Linq;
using System.Windows;

namespace LibraryAccounting.Windows
{
    public partial class BookCopyAddWindow : Window
    {
        private BookCopies _copy;
        private bool _isEdit;

        // ➕ ДОБАВЛЕНИЕ
        public BookCopyAddWindow()
        {
            InitializeComponent();
            _isEdit = false;
            TitleText.Text = "Добавление экземпляра";

            LoadBooks();
        }

        // ✏️ РЕДАКТИРОВАНИЕ
        public BookCopyAddWindow(BookCopies copy)
        {
            InitializeComponent();
            _copy = copy;
            _isEdit = true;
            TitleText.Text = "Редактирование экземпляра";

            LoadBooks();
            FillData();
        }

        private void LoadBooks()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();
            BookBox.ItemsSource = AppConnect.model01.Books.ToList();
        }

        private void FillData()
        {
            InventoryBox.Text = _copy.InventoryNumber;
            RowBox.Text = _copy.Row;
            ShelfBox.Text = _copy.Shelf;

            BookBox.SelectedItem = AppConnect.model01.Books
                .FirstOrDefault(b => b.BookId == _copy.BookId);

            // ❌ КНИГУ МЕНЯТЬ НЕЛЬЗЯ ПРИ РЕДАКТИРОВАНИИ
            BookBox.IsEnabled = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (BookBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(InventoryBox.Text) ||
                string.IsNullOrWhiteSpace(RowBox.Text) ||
                string.IsNullOrWhiteSpace(ShelfBox.Text))
            {
                MessageBox.Show("Заполните все поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string inventory = InventoryBox.Text.Trim();

            int currentCopyId = _isEdit ? _copy.CopyId : 0;

            bool exists = AppConnect.model01.BookCopies.Any(c =>
                c.InventoryNumber == inventory &&
                (!_isEdit || c.CopyId != currentCopyId));

            if (exists)
            {
                MessageBox.Show("Экземпляр с таким инвентарным номером уже существует",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isEdit)
            {
                _copy = new BookCopies();
                AppConnect.model01.BookCopies.Add(_copy);
            }

            var book = (Books)BookBox.SelectedItem;

            _copy.BookId = book.BookId;
            _copy.InventoryNumber = inventory;
            _copy.Row = RowBox.Text.Trim();
            _copy.Shelf = ShelfBox.Text.Trim();
            _copy.Status = _isEdit ? _copy.Status : "Available";

            AppConnect.model01.SaveChanges();

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
