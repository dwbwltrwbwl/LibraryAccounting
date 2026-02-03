using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class BookCopiesView : Page
    {
        public BookCopiesView()
        {
            InitializeComponent();
            if (AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Visibility = Visibility.Collapsed; // можно оставить только IsEnabled=false
            }
            LoadCopies();
        }

        /// <summary>
        /// Загрузка экземпляров книг
        /// </summary>
        private void LoadCopies()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var copies = AppConnect.model01.BookCopies
    .Select(c => new
    {
        c.CopyId,
        BookTitle = c.Books.Title,
        c.InventoryNumber,
        Location = "Ряд " + c.Row + ", полка " + c.Shelf,
        c.Status
    })
    .ToList();

            CopiesDataGrid.ItemsSource = copies;
        }

        /// <summary>
        /// Добавление экземпляра (заглушка)
        /// </summary>
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

            dynamic selected = CopiesDataGrid.SelectedItem;
            int copyId = selected.CopyId;

            var copy = AppConnect.model01.BookCopies
                .FirstOrDefault(c => c.CopyId == copyId);

            if (copy == null)
                return;

            var window = new BookCopyAddWindow(copy)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
                LoadCopies();
        }

        /// <summary>
        /// Удаление экземпляра
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CopiesDataGrid.SelectedItem == null)
            {
                ShowError("Выберите экземпляр для удаления");
                return;
            }

            dynamic selected = CopiesDataGrid.SelectedItem;
            int copyId = selected.CopyId;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            // 🔥 ПРОВЕРКА: используется ли экземпляр в выдачах
            bool isUsed = AppConnect.model01.Loans
                .Any(l => l.CopyId == copyId);

            if (isUsed)
            {
                ShowError(
                    "Невозможно удалить экземпляр.\n" +
                    "Для него существует история выдач."
                );
                return;
            }

            var copy = AppConnect.model01.BookCopies
                .FirstOrDefault(c => c.CopyId == copyId);

            if (copy == null)
            {
                ShowError("Экземпляр не найден");
                return;
            }

            // 🧨 подтверждение
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
