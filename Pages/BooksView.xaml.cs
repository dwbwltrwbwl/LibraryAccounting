using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

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

            _books = AppConnect.model01.Books
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    Author = b.Authors.FullName,
                    Genre = b.Genres.Name,
                    b.Publisher,
                    b.PublishYear,
                    b.ISBN,
                    b.CoverImage
                })
                .ToList<dynamic>();

            _booksView = CollectionViewSource.GetDefaultView(_books);
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
            dynamic book = item;

            string search = SearchTextBox.Text.ToLower();
            string selectedGenre = GenreComboBox.SelectedItem?.ToString();

            bool matchesSearch =
                string.IsNullOrEmpty(search) ||
                book.Title.ToLower().Contains(search) ||
                book.Author.ToLower().Contains(search);

            bool matchesGenre =
                selectedGenre == "Все жанры" ||
                string.IsNullOrEmpty(selectedGenre) ||
                book.Genre == selectedGenre;

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
        /// Добавление книги (заглушка)
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new BookEditWindow();
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadBooks();
        }

        /// <summary>
        /// Редактирование книги (заглушка)
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem == null)
            {
                ShowError("Выберите книгу");
                return;
            }

            dynamic selected = BooksDataGrid.SelectedItem;
            int id = selected.BookId;

            var book = AppConnect.model01.Books.First(b => b.BookId == id);

            var win = new BookEditWindow(book);
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadBooks();
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
