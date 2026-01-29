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
            var dialog = new MessageDialog(
                "Добавление экземпляра",
                "Окно добавления экземпляра будет реализовано позже."
            );
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
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

            var copy = AppConnect.model01.BookCopies
                .FirstOrDefault(c => c.CopyId == copyId);

            if (copy != null)
            {
                AppConnect.model01.BookCopies.Remove(copy);
                AppConnect.model01.SaveChanges();

                LoadCopies();
                ShowInfo("Экземпляр успешно удален");
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
    }
}
