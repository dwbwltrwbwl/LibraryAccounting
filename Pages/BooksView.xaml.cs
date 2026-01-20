using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryAccounting.Pages
{
    public partial class BooksView : Page
    {
        public BooksView()
        {
            InitializeComponent();
            LoadBooks();
        }

        /// <summary>
        /// Загрузка всех книг
        /// </summary>
        private void LoadBooks()
        {
            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var books = AppConnect.model01.Books
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    Author = b.Authors.FullName,
                    Genre = b.Genres.Name,
                    b.Publisher,
                    b.PublishYear,
                    b.ISBN
                })
                .ToList();

            BooksDataGrid.ItemsSource = books;
        }

        /// <summary>
        /// Поиск книг
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string search = SearchTextBox.Text.Trim().ToLower();

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var result = AppConnect.model01.Books
                .Where(b =>
                    b.Title.ToLower().Contains(search) ||
                    b.Authors.FullName.ToLower().Contains(search) ||
                    b.Genres.Name.ToLower().Contains(search))
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    Author = b.Authors.FullName,
                    Genre = b.Genres.Name,
                    b.Publisher,
                    b.PublishYear,
                    b.ISBN
                })
                .ToList();

            BooksDataGrid.ItemsSource = result;
        }

        /// <summary>
        /// Добавление книги (заглушка)
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog(
                "Добавление книги",
                "Окно добавления книги будет реализовано позже."
            );
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// Редактирование книги (заглушка)
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem == null)
            {
                ShowError("Выберите книгу для редактирования");
                return;
            }

            var dialog = new MessageDialog(
                "Редактирование книги",
                "Окно редактирования книги будет реализовано позже."
            );
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// Удаление книги
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem == null)
            {
                ShowError("Выберите книгу для удаления");
                return;
            }

            dynamic selected = BooksDataGrid.SelectedItem;
            int bookId = selected.BookId;

            AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

            var book = AppConnect.model01.Books.FirstOrDefault(b => b.BookId == bookId);

            if (book != null)
            {
                AppConnect.model01.Books.Remove(book);
                AppConnect.model01.SaveChanges();

                LoadBooks();

                ShowInfo("Книга успешно удалена");
            }
        }

        /// <summary>
        /// Уведомление об ошибке
        /// </summary>
        private void ShowError(string message)
        {
            var dialog = new MessageDialog("Ошибка", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// Информационное сообщение
        /// </summary>
        private void ShowInfo(string message)
        {
            var dialog = new MessageDialog("Информация", message);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
