using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace LibraryAccounting.Pages
{
    public partial class BooksView : Page
    {
        private ICollectionView _booksView;
        private List<dynamic> _books;
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

            var books = AppConnect.model01.Books.ToList();

            _booksView = CollectionViewSource.GetDefaultView(books);
            _booksView.Filter = FilterBooks;

            BooksDataGrid.ItemsSource = _booksView;

            LoadGenres();
        }

        private void LoadGenres()
        {
            GenreComboBox.Items.Clear();
            GenreComboBox.Items.Add("Все жанры");

            var genres = AppConnect.model01.Genres
                .Select(g => g.Name)
                .Distinct()
                .ToList();

            foreach (var genre in genres)
                GenreComboBox.Items.Add(genre);

            GenreComboBox.SelectedIndex = 0;
        }

        private bool FilterBooks(object item)
        {
            Books book = item as Books;
            if (book == null)
                return false;

            string search = SearchTextBox.Text?.ToLower() ?? "";
            string selectedGenre = GenreComboBox.SelectedItem?.ToString();

            bool matchesSearch =
                string.IsNullOrEmpty(search) ||
                book.Title.ToLower().Contains(search) ||
                (book.Authors != null &&
                 book.Authors.FullName.ToLower().Contains(search));

            bool matchesGenre =
                selectedGenre == "Все жанры" ||
                string.IsNullOrEmpty(selectedGenre) ||
                (book.Genres != null &&
                 book.Genres.Name == selectedGenre);

            return matchesSearch && matchesGenre;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _booksView?.Refresh();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _booksView?.Refresh();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_booksView == null) return;

            _booksView.SortDescriptions.Clear();

            string sort = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (sort)
            {
                case "По названию":
                    _booksView.SortDescriptions.Add(
                        new SortDescription("Title", ListSortDirection.Ascending));
                    break;

                case "По автору":
                    _booksView.SortDescriptions.Add(
                        new SortDescription("Author", ListSortDirection.Ascending));
                    break;

                case "По году":
                    _booksView.SortDescriptions.Add(
                        new SortDescription("PublishYear", ListSortDirection.Descending));
                    break;
            }
        }

        /// <summary>
        /// Добавление книги
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Создаем пустую книгу
                var newBook = new Books();

                var win = new BookEditWindow();
                win.Owner = Window.GetWindow(this);

                if (win.ShowDialog() == true)
                {
                    LoadBooks();
                }
            }
            catch (System.Exception ex)
            {
                ShowError($"Ошибка при добавлении книги: {ex.Message}");
            }
        }

        /// <summary>
        /// Редактирование книги (по кнопке)
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditSelectedBook();
        }

        /// <summary>
        /// Редактирование книги по двойному клику
        /// </summary>
        private void BooksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Books selectedBook = BooksDataGrid.CurrentItem as Books;
            if (selectedBook == null)
                return;

            var bookFromDb = AppConnect.model01.Books
                .FirstOrDefault(b => b.BookId == selectedBook.BookId);

            if (bookFromDb == null)
                return;

            var window = new BookEditWindow(bookFromDb)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
                LoadBooks();
        }


        /// <summary>
        /// Вспомогательный метод для поиска родительского элемента
        /// </summary>
        private T GetParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;

            return GetParent<T>(parentObject);
        }

        /// <summary>
        /// Общий метод для редактирования выбранной книги
        /// </summary>
        private void EditSelectedBook()
        {
            try
            {
                if (BooksDataGrid.SelectedItem == null)
                {
                    ShowError("Выберите книгу для редактирования");
                    return;
                }

                dynamic selected = BooksDataGrid.SelectedItem;
                int id = selected.BookId;

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Получаем книгу из базы данных
                var book = AppConnect.model01.Books.FirstOrDefault(b => b.BookId == id);

                if (book == null)
                {
                    ShowError("Книга не найдена в базе данных");
                    return;
                }

                var win = new BookEditWindow(book);
                win.Owner = Window.GetWindow(this);

                if (win.ShowDialog() == true)
                {
                    LoadBooks();
                }
            }
            catch (System.Exception ex)
            {
                ShowError($"Ошибка при редактировании книги: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление книги
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BooksDataGrid.SelectedItem == null)
                {
                    ShowError("Выберите книгу для удаления");
                    return;
                }

                Books selectedBook = BooksDataGrid.SelectedItem as Books;
                if (selectedBook == null)
                {
                    ShowError("Ошибка выбора книги");
                    return;
                }

                int bookId = selectedBook.BookId;

                // 🔥 ПРОВЕРКА ИСПОЛЬЗОВАНИЯ
                if (IsBookUsed(bookId))
                {
                    ShowError("Книга используется в других разделах системы и не может быть удалена.");
                    return;
                }

                MessageDialog dialog = new MessageDialog(
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить книгу «{selectedBook.Title}»?"
                );
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() != true)
                    return;

                Books bookFromDb = AppConnect.model01.Books
                    .FirstOrDefault(b => b.BookId == bookId);

                if (bookFromDb == null)
                {
                    ShowError("Книга не найдена в базе данных");
                    return;
                }

                AppConnect.model01.Books.Remove(bookFromDb);
                AppConnect.model01.SaveChanges();

                LoadBooks();
                ShowInfo("Книга успешно удалена");
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при удалении книги:\n" + ex.Message);
            }
        }
        private bool IsBookUsed(int bookId)
        {
            if (AppConnect.model01 == null)
                AppConnect.model01 = new LibraryAccountingEntities();

            // Получаем книгу с подгрузкой всех связей
            var book = AppConnect.model01.Books
                .FirstOrDefault(b => b.BookId == bookId);

            if (book == null)
                return false;

            // 🔥 Проверяем ВСЕ коллекционные навигационные свойства
            var properties = book.GetType().GetProperties();

            foreach (var prop in properties)
            {
                // Ищем ICollection<T>
                if (prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    var collection = prop.GetValue(book) as System.Collections.ICollection;

                    if (collection != null && collection.Count > 0)
                    {
                        return true; // ❌ книга используется
                    }
                }
            }

            return false; // ✅ книга свободна
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
    }
}