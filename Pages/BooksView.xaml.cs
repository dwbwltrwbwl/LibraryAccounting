using LibraryAccounting.AppData;
using LibraryAccounting.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace LibraryAccounting.Pages
{
    public partial class BooksView : Page
    {
        private ICollectionView _booksView;
        private List<BookViewModel> _books;
        private List<string> _allGenres;
        private List<string> _allPublishers;
        private string _currentGenreFilter = "";
        private string _currentPublisherFilter = "";

        public class BookViewModel
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public string AuthorName { get; set; }
            public string GenreName { get; set; }
            public string PublisherName { get; set; }
            public int? PublishYear { get; set; }
            public string ISBN { get; set; }
            public byte[] CoverImage { get; set; }
            public int? Pages { get; set; }
            public string LanguageName { get; set; }
            public string Description { get; set; }
            public string Series { get; set; }
            public string Edition { get; set; }
            public int? Circulation { get; set; }
            public string Binding { get; set; }
            public string Format { get; set; }
            public int Quantity { get; set; }
            public int AvailableQuantity { get; set; }
            public DateTime? AddedDate { get; set; }
            public DateTime? LastModified { get; set; }
        }

        public BooksView()
        {
            InitializeComponent();
            if (AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
            LoadBooks();
        }

        private void LoadBooks()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var books = AppConnect.model01.Books
                    .Select(b => new BookViewModel
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        AuthorName = b.Authors != null ? b.Authors.FullName : "Не указан",
                        GenreName = b.Genres != null ? b.Genres.Name : "Не указан",
                        PublisherName = b.Publishers != null ? b.Publishers.PublisherName : "Не указано",
                        PublishYear = b.PublishYear,
                        ISBN = b.ISBN,
                        CoverImage = b.CoverImage,
                        Pages = b.Pages,
                        LanguageName = b.Languages != null ? b.Languages.LanguageName : "Не указан",
                        Description = b.Description,
                        Series = b.Series,
                        Edition = b.Edition,
                        Circulation = b.Circulation,
                        Binding = b.Binding,
                        Format = b.Format,
                        Quantity = b.Quantity,
                        AvailableQuantity = b.AvailableQuantity,
                        AddedDate = b.AddedDate,
                        LastModified = b.LastModified
                    })
                    .ToList();

                _books = books;
                _booksView = CollectionViewSource.GetDefaultView(_books);
                _booksView.Filter = FilterBooks;

                BooksDataGrid.ItemsSource = _booksView;

                LoadFilters();
                _booksView.SortDescriptions.Clear();
                SortComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки книг: {ex.Message}");
            }
        }

        private void LoadFilters()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Жанры
                _allGenres = AppConnect.model01.Genres
                    .Select(g => g.Name)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToList();

                // Издательства
                _allPublishers = AppConnect.model01.Publishers
                    .Select(p => p.PublisherName)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                // Загружаем начальные списки
                UpdateGenreComboBox("");
                UpdatePublisherComboBox("");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки фильтров: {ex.Message}");
            }
        }

        private void UpdateGenreComboBox(string filter)
        {
            GenreComboBox.Items.Clear();

            var filteredGenres = _allGenres;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filteredGenres = _allGenres.Where(g => g.ToLower().Contains(filter.ToLower())).ToList();
            }

            GenreComboBox.Items.Add("Все жанры");
            foreach (var genre in filteredGenres)
            {
                GenreComboBox.Items.Add(genre);
            }

            // Устанавливаем выбранный элемент
            GenreComboBox.SelectedIndex = 0;

            // Восстанавливаем введённый текст (если есть)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                GenreComboBox.Text = filter;
            }
        }

        private void UpdatePublisherComboBox(string filter)
        {
            PublisherComboBox.Items.Clear();

            var filteredPublishers = _allPublishers;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filteredPublishers = _allPublishers.Where(p => p.ToLower().Contains(filter.ToLower())).ToList();
            }

            PublisherComboBox.Items.Add("Все издательства");
            foreach (var publisher in filteredPublishers)
            {
                PublisherComboBox.Items.Add(publisher);
            }

            // Устанавливаем выбранный элемент
            PublisherComboBox.SelectedIndex = 0;

            // Восстанавливаем введённый текст (если есть)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                PublisherComboBox.Text = filter;
            }
        }

        private void GenreComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = GenreComboBox.Text ?? "";
            UpdateGenreComboBox(searchText);
            GenreComboBox.IsDropDownOpen = true;
        }

        private void PublisherComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchText = PublisherComboBox.Text ?? "";
            UpdatePublisherComboBox(searchText);
            PublisherComboBox.IsDropDownOpen = true;
        }

        private void GenreFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _booksView?.Refresh();
        }

        private void PublisherFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _booksView?.Refresh();
        }

        private bool FilterBooks(object item)
        {
            var book = item as BookViewModel;
            if (book == null)
                return false;

            string search = SearchTextBox.Text?.ToLower() ?? "";
            string selectedGenre = GenreComboBox.SelectedItem?.ToString();
            string selectedPublisher = PublisherComboBox.SelectedItem?.ToString();

            bool matchesSearch = string.IsNullOrEmpty(search) ||
                book.Title.ToLower().Contains(search) ||
                book.AuthorName.ToLower().Contains(search) ||
                (book.ISBN != null && book.ISBN.ToLower().Contains(search)) ||
                (book.Series != null && book.Series.ToLower().Contains(search)) ||
                (book.Description != null && book.Description.ToLower().Contains(search)) ||
                (book.LanguageName != null && book.LanguageName.ToLower().Contains(search));

            bool matchesGenre = selectedGenre == "Все жанры" ||
                string.IsNullOrEmpty(selectedGenre) ||
                book.GenreName == selectedGenre;

            bool matchesPublisher = selectedPublisher == "Все издательства" ||
                string.IsNullOrEmpty(selectedPublisher) ||
                book.PublisherName == selectedPublisher;

            return matchesSearch && matchesGenre && matchesPublisher;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                        new SortDescription("AuthorName", ListSortDirection.Ascending));
                    break;
                case "По году":
                    _booksView.SortDescriptions.Add(
                        new SortDescription("PublishYear", ListSortDirection.Descending));
                    break;
                case "По количеству":
                    _booksView.SortDescriptions.Add(
                        new SortDescription("Quantity", ListSortDirection.Descending));
                    break;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new BookEditWindow();
                win.Owner = Window.GetWindow(this);

                if (win.ShowDialog() == true)
                {
                    LoadBooks();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при добавлении книги: {ex.Message}");
            }
        }

        private void BooksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditSelectedBook();
        }

        private void EditSelectedBook()
        {
            try
            {
                if (BooksDataGrid.SelectedItem == null)
                {
                    ShowError("Выберите книгу для редактирования");
                    return;
                }

                var selected = BooksDataGrid.SelectedItem as BookViewModel;
                if (selected == null) return;

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                var book = AppConnect.model01.Books.FirstOrDefault(b => b.BookId == selected.BookId);

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
            catch (Exception ex)
            {
                ShowError($"Ошибка при редактировании книги: {ex.Message}");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.CurrentUser == null || AppConnect.CurrentUser.RoleId == 2)
            {
                ShowError("У вас нет прав на удаление книг");
                return;
            }
            try
            {
                if (BooksDataGrid.SelectedItem == null)
                {
                    ShowError("Выберите книгу для удаления");
                    return;
                }

                var selected = BooksDataGrid.SelectedItem as BookViewModel;
                if (selected == null) return;

                if (IsBookUsed(selected.BookId))
                {
                    ShowError("Невозможно удалить книгу.\nДля неё существуют экземпляры или история выдач.");
                    return;
                }

                DeleteMessageDialog dialog = new DeleteMessageDialog(
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить книгу «{selected.Title}»?"
                );
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() != true)
                    return;

                var db = AppConnect.model01 ?? new LibraryAccountingEntities();
                var bookFromDb = db.Books.FirstOrDefault(b => b.BookId == selected.BookId);

                if (bookFromDb == null)
                {
                    ShowError("Книга не найдена в базе данных");
                    return;
                }

                db.Books.Remove(bookFromDb);
                db.SaveChanges();

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
            var db = AppConnect.model01 ?? new LibraryAccountingEntities();
            return db.BookCopies.Any(c => c.BookId == bookId);
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

        private void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = _booksView?.Cast<BookViewModel>().ToList();

                if (items == null || items.Count == 0)
                {
                    ShowError("Нет данных для экспорта");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    FileName = $"Книги_{DateTime.Now:yyyy-MM-dd_HHmmss}",
                    DefaultExt = "csv",
                    Title = "Сохранить CSV файл"
                };

                if (dialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine("\"Название\";\"Автор\";\"Жанр\";\"Издательство\";\"Год\";\"ISBN\";\"Страниц\";\"Язык\";\"Переплет\";\"Формат\";\"Серия\";\"Издание\";\"Тираж\";\"Экземпляров\";\"Доступно\";\"Дата добавления\";\"Описание\"");

                    foreach (var book in items)
                    {
                        sb.AppendLine($"{EscapeCsv(book.Title)};{EscapeCsv(book.AuthorName)};{EscapeCsv(book.GenreName)};{EscapeCsv(book.PublisherName)};{book.PublishYear};{EscapeCsv(book.ISBN)};{book.Pages};{EscapeCsv(book.LanguageName)};{EscapeCsv(book.Binding)};{EscapeCsv(book.Format)};{EscapeCsv(book.Series)};{EscapeCsv(book.Edition)};{book.Circulation};{book.Quantity};{book.AvailableQuantity};{book.AddedDate:dd.MM.yyyy};{EscapeCsv(book.Description)}");
                    }

                    System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    ShowInfo($"Экспорт выполнен успешно!\nФайл сохранен: {dialog.FileName}\nЭкспортировано записей: {items.Count}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при экспорте: {ex.Message}");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(";") || value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
        }

        private void Analytics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var analyticsWindow = new BookAnalyticsWindow();
                analyticsWindow.Owner = Window.GetWindow(this);
                analyticsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка открытия аналитики: {ex.Message}");
            }
        }
    }
}