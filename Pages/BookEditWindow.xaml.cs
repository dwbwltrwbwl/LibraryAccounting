// ========================================
// 1. ОБНОВЛЕННЫЙ КОД ОКНА BookEditWindow.xaml.cs
// ========================================

using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LibraryAccounting.Pages
{
    public partial class BookEditWindow : Window
    {
        private Books _book;
        private byte[] _imageBytes;
        private bool _isEditing;

        // Коллекции для множественных связей
        private List<Authors> _selectedAuthors = new List<Authors>();
        private List<Genres> _selectedGenres = new List<Genres>();

        // Конструктор для добавления новой книги
        public BookEditWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление книги";
            _isEditing = false;
            LoadLists();
            ClearFields();
        }

        // Конструктор для редактирования существующей книги
        public BookEditWindow(Books book)
        {
            InitializeComponent();
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            TitleText.Text = "Редактирование книги";
            _isEditing = true;
            _book = book;

            // Убедитесь, что коллекции инициализированы (для старых книг)
            if (_book.BookAuthors == null)
                _book.BookAuthors = new HashSet<BookAuthors>();
            if (_book.BookGenres == null)
                _book.BookGenres = new HashSet<BookGenres>();

            LoadLists();
            LoadSelectedData();
            FillData();
        }

        private void LoadLists()
        {
            try
            {
                if (AppConnect.model01 == null)
                    AppConnect.model01 = new LibraryAccountingEntities();

                LoadAuthors();
                LoadGenres();
                LoadPublishers();
                LoadLanguages();
                LoadBindings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSelectedData()
        {
            if (_book == null) return;

            // Загрузка выбранных авторов
            if (_book.BookAuthors != null && _book.BookAuthors.Any())
            {
                _selectedAuthors = _book.BookAuthors.Select(ba => ba.Authors).ToList();
                SelectedAuthorsList.ItemsSource = null;
                SelectedAuthorsList.ItemsSource = _selectedAuthors;

                // ОБНОВЛЕНИЕ: также загружаем основного автора для отображения
                var mainAuthor = _book.Authors;
                if (mainAuthor != null && !_selectedAuthors.Any(a => a.AuthorId == mainAuthor.AuthorId))
                {
                    _selectedAuthors.Insert(0, mainAuthor);
                    SelectedAuthorsList.ItemsSource = null;
                    SelectedAuthorsList.ItemsSource = _selectedAuthors;
                }
            }

            // Загрузка выбранных жанров
            if (_book.BookGenres != null && _book.BookGenres.Any())
            {
                _selectedGenres = _book.BookGenres.Select(bg => bg.Genres).ToList();
                SelectedGenresList.ItemsSource = null;
                SelectedGenresList.ItemsSource = _selectedGenres;

                // ОБНОВЛЕНИЕ: также загружаем основной жанр для отображения
                var mainGenre = _book.Genres;
                if (mainGenre != null && !_selectedGenres.Any(g => g.GenreId == mainGenre.GenreId))
                {
                    _selectedGenres.Insert(0, mainGenre);
                    SelectedGenresList.ItemsSource = null;
                    SelectedGenresList.ItemsSource = _selectedGenres;
                }
            }
        }

        #region Загрузка данных с фильтрацией

        private void LoadAuthors(string filter = null)
        {
            try
            {
                var query = AppConnect.model01.Authors.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.ToLower();
                    query = query.Where(a => a.FullName.ToLower().Contains(filter));
                }

                // Исключаем уже выбранных авторов
                var authorIds = _selectedAuthors.Select(a => a.AuthorId).ToList();
                if (authorIds.Any())
                {
                    query = query.Where(a => !authorIds.Contains(a.AuthorId));
                }

                AuthorBox.ItemsSource = query.OrderBy(a => a.FullName).ToList();
                AuthorBox.DisplayMemberPath = "FullName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авторов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGenres(string filter = null)
        {
            try
            {
                var query = AppConnect.model01.Genres.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.ToLower();
                    query = query.Where(g => g.Name.ToLower().Contains(filter));
                }

                // Исключаем уже выбранные жанры
                var genreIds = _selectedGenres.Select(g => g.GenreId).ToList();
                if (genreIds.Any())
                {
                    query = query.Where(g => !genreIds.Contains(g.GenreId));
                }

                GenreBox.ItemsSource = query.OrderBy(g => g.Name).ToList();
                GenreBox.DisplayMemberPath = "Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки жанров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPublishers(string filter = null)
        {
            try
            {
                var query = AppConnect.model01.Publishers.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.ToLower();
                    query = query.Where(p => p.PublisherName.ToLower().Contains(filter));
                }
                PublisherBox.ItemsSource = query.OrderBy(p => p.PublisherName).ToList();
                PublisherBox.DisplayMemberPath = "PublisherName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки издательств: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLanguages(string filter = null)
        {
            try
            {
                var query = AppConnect.model01.Languages.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.ToLower();
                    query = query.Where(l => l.LanguageName.ToLower().Contains(filter));
                }
                LanguageBox.ItemsSource = query.OrderBy(l => l.LanguageName).ToList();
                LanguageBox.DisplayMemberPath = "LanguageName";
                LanguageBox.SelectedValuePath = "LanguageId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки языков: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBindings(string filter = null)
        {
            try
            {
                var query = AppConnect.model01.Bindings.AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.ToLower();
                    query = query.Where(b => b.BindingName.ToLower().Contains(filter));
                }
                BindingBox.ItemsSource = query.OrderBy(b => b.BindingName).ToList();
                BindingBox.DisplayMemberPath = "BindingName";
                BindingBox.SelectedValuePath = "BindingId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов переплета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Обработчики поиска (KeyUp)

        private void AuthorBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = AuthorBox.Text?.Trim() ?? "";
            LoadAuthors(searchText);
            if (AuthorBox.ItemsSource != null && ((System.Collections.IList)AuthorBox.ItemsSource).Count > 0)
                AuthorBox.IsDropDownOpen = true;
        }

        private void GenreBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = GenreBox.Text?.Trim() ?? "";
            LoadGenres(searchText);
            if (GenreBox.ItemsSource != null && ((System.Collections.IList)GenreBox.ItemsSource).Count > 0)
                GenreBox.IsDropDownOpen = true;
        }

        private void PublisherBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = PublisherBox.Text?.Trim() ?? "";
            LoadPublishers(searchText);
            if (PublisherBox.ItemsSource != null && ((System.Collections.IList)PublisherBox.ItemsSource).Count > 0)
                PublisherBox.IsDropDownOpen = true;
        }

        private void LanguageBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = LanguageBox.Text?.Trim() ?? "";
            LoadLanguages(searchText);
            if (LanguageBox.ItemsSource != null && ((System.Collections.IList)LanguageBox.ItemsSource).Count > 0)
                LanguageBox.IsDropDownOpen = true;
        }

        private void BindingBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = BindingBox.Text?.Trim() ?? "";
            LoadBindings(searchText);
            if (BindingBox.ItemsSource != null && ((System.Collections.IList)BindingBox.ItemsSource).Count > 0)
                BindingBox.IsDropDownOpen = true;
        }

        #endregion

        #region Добавление/удаление авторов и жанров

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (AuthorBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите автора из списка", "Добавление автора",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var author = (Authors)AuthorBox.SelectedItem;

            if (_selectedAuthors.Any(a => a.AuthorId == author.AuthorId))
            {
                MessageBox.Show("Этот автор уже добавлен", "Добавление автора",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _selectedAuthors.Add(author);
            UpdateAuthorsList();

            // Очищаем выбор и обновляем список доступных авторов
            AuthorBox.SelectedIndex = -1;
            AuthorBox.Text = "";
            LoadAuthors();
        }

        private void RemoveAuthors_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAuthorsList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите авторов для удаления", "Удаление авторов",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var toRemove = SelectedAuthorsList.SelectedItems.Cast<Authors>().ToList();
            foreach (var author in toRemove)
            {
                _selectedAuthors.Remove(author);
            }
            UpdateAuthorsList();

            // Обновляем список доступных авторов
            LoadAuthors();
        }

        private void UpdateAuthorsList()
        {
            SelectedAuthorsList.ItemsSource = null;
            SelectedAuthorsList.ItemsSource = _selectedAuthors;

            // Обновляем список доступных авторов (исключая выбранных)
            LoadAuthors();
        }

        private void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            if (GenreBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите жанр из списка", "Добавление жанра",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var genre = (Genres)GenreBox.SelectedItem;

            if (_selectedGenres.Any(g => g.GenreId == genre.GenreId))
            {
                MessageBox.Show("Этот жанр уже добавлен", "Добавление жанра",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _selectedGenres.Add(genre);
            UpdateGenresList();

            // Очищаем выбор и обновляем список доступных жанров
            GenreBox.SelectedIndex = -1;
            GenreBox.Text = "";
            LoadGenres();
        }

        private void RemoveGenres_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGenresList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите жанры для удаления", "Удаление жанров",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var toRemove = SelectedGenresList.SelectedItems.Cast<Genres>().ToList();
            foreach (var genre in toRemove)
            {
                _selectedGenres.Remove(genre);
            }
            UpdateGenresList();

            // Обновляем список доступных жанров
            LoadGenres();
        }

        private void UpdateGenresList()
        {
            SelectedGenresList.ItemsSource = null;
            SelectedGenresList.ItemsSource = _selectedGenres;

            // Обновляем список доступных жанров (исключая выбранные)
            LoadGenres();
        }

        #endregion

        private void FillData()
        {
            if (_book == null) return;

            TitleBox.Text = _book.Title ?? "";
            YearBox.Text = _book.PublishYear?.ToString() ?? "";
            IsbnBox.Text = _book.ISBN ?? "";
            PagesBox.Text = _book.Pages?.ToString() ?? "";
            DescriptionBox.Text = _book.Description ?? "";
            SeriesBox.Text = _book.Series ?? "";
            EditionBox.Text = _book.Edition ?? "";
            CirculationBox.Text = _book.Circulation?.ToString() ?? "";
            FormatBox.Text = _book.Format ?? "";

            if (_book.PublisherId.HasValue)
            {
                var selectedPublisher = AppConnect.model01.Publishers.FirstOrDefault(p => p.PublisherId == _book.PublisherId);
                if (selectedPublisher != null) PublisherBox.SelectedItem = selectedPublisher;
            }

            if (_book.BindingId.HasValue)
            {
                var selectedBinding = AppConnect.model01.Bindings
                    .FirstOrDefault(b => b.BindingId == _book.BindingId);
                if (selectedBinding != null)
                    BindingBox.SelectedItem = selectedBinding;
            }

            if (_book.LanguageId.HasValue)
            {
                var selectedLanguage = AppConnect.model01.Languages.FirstOrDefault(l => l.LanguageId == _book.LanguageId);
                if (selectedLanguage != null) LanguageBox.SelectedItem = selectedLanguage;
            }

            _imageBytes = _book.CoverImage;
            if (_imageBytes != null && _imageBytes.Length > 0)
            {
                CoverImage.Source = LoadImage(_imageBytes);
            }
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                    Title = "Выберите обложку книги"
                };

                if (dlg.ShowDialog() == true)
                {
                    _imageBytes = File.ReadAllBytes(dlg.FileName);
                    CoverImage.Source = LoadImage(_imageBytes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage LoadImage(byte[] bytes)
        {
            try
            {
                if (bytes != null && bytes.Length > 0)
                {
                    BitmapImage image = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }
                    return image;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #region Валидация

        private bool ValidateFields()
        {
            // 1. Название
            string title = TitleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Введите название книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return false;
            }
            if (title.Length > 200)
            {
                MessageBox.Show($"Название книги не может превышать 200 символов (сейчас {title.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleBox.Focus();
                return false;
            }

            // 2. Год издания
            if (!int.TryParse(YearBox.Text, out int year) || year < 1000 || year > DateTime.Now.Year + 1)
            {
                MessageBox.Show($"Введите корректный год издания (1000-{DateTime.Now.Year + 1})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearBox.Focus();
                return false;
            }

            // 3. Авторы
            if (_selectedAuthors.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одного автора", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 4. Жанры
            if (_selectedGenres.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один жанр", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 5. Издательство
            if (PublisherBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите издательство", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PublisherBox.Focus();
                return false;
            }

            // 6. ISBN
            string isbn = IsbnBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(isbn))
            {
                MessageBox.Show("Введите ISBN", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsbnBox.Focus();
                return false;
            }
            if (isbn.Length > 20)
            {
                MessageBox.Show($"ISBN не может превышать 20 символов (сейчас {isbn.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IsbnBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(isbn, @"^[0-9\-Xx]+$"))
            {
                MessageBox.Show("ISBN может содержать только цифры, дефисы и букву X", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IsbnBox.Focus();
                return false;
            }

            // 7. Страниц
            string pagesText = PagesBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(pagesText))
            {
                MessageBox.Show("Введите количество страниц", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PagesBox.Focus();
                return false;
            }
            if (!int.TryParse(pagesText, out int pages) || pages <= 0 || pages > 10000)
            {
                MessageBox.Show("Количество страниц должно быть числом от 1 до 10000", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PagesBox.Focus();
                return false;
            }

            // 8. Язык
            if (LanguageBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите язык книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LanguageBox.Focus();
                return false;
            }

            // 9. Переплет
            if (BindingBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип переплета", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                BindingBox.Focus();
                return false;
            }

            // 10. Формат
            string format = FormatBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(format))
            {
                MessageBox.Show("Введите формат книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FormatBox.Focus();
                return false;
            }
            if (format.Length > 50)
            {
                MessageBox.Show($"Формат не может превышать 50 символов (сейчас {format.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FormatBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(format, @"^[a-zA-Zа-яА-Я0-9\sxX/]+$"))
            {
                MessageBox.Show("Формат может содержать буквы, цифры, x и / (например: 60x90/16)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FormatBox.Focus();
                return false;
            }

            // 11. Серия
            string series = SeriesBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(series))
            {
                MessageBox.Show("Введите название серии", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesBox.Focus();
                return false;
            }
            if (series.Length > 100)
            {
                MessageBox.Show($"Серия не может превышать 100 символов (сейчас {series.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesBox.Focus();
                return false;
            }

            // 12. Издание
            string edition = EditionBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(edition))
            {
                MessageBox.Show("Введите номер издания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EditionBox.Focus();
                return false;
            }
            if (edition.Length > 50)
            {
                MessageBox.Show($"Издание не может превышать 50 символов (сейчас {edition.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EditionBox.Focus();
                return false;
            }

            // 13. Тираж
            string circulationText = CirculationBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(circulationText))
            {
                MessageBox.Show("Введите тираж книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                CirculationBox.Focus();
                return false;
            }
            if (!int.TryParse(circulationText, out int circulation) || circulation <= 0 || circulation > 1000000)
            {
                MessageBox.Show("Тираж должен быть числом от 1 до 1 000 000", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CirculationBox.Focus();
                return false;
            }

            // 14. Описание
            string description = DescriptionBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Введите описание книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionBox.Focus();
                return false;
            }
            if (description.Length > 4000)
            {
                MessageBox.Show($"Описание не может превышать 4000 символов (сейчас {description.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionBox.Focus();
                return false;
            }

            // 15. Количество экземпляров
            string quantityText = QuantityBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(quantityText))
            {
                MessageBox.Show("Введите количество экземпляров", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityBox.Focus();
                return false;
            }
            if (!int.TryParse(quantityText, out int quantity) || quantity <= 0 || quantity > 10000)
            {
                MessageBox.Show("Количество экземпляров должно быть числом от 1 до 10000", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityBox.Focus();
                return false;
            }

            return true;
        }

        #endregion

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateFields())
                    return;

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                string title = TitleBox.Text.Trim();
                int publisherId = ((Publishers)PublisherBox.SelectedItem).PublisherId;
                int languageId = ((Languages)LanguageBox.SelectedItem).LanguageId;
                int bindingId = ((Bindings)BindingBox.SelectedItem).BindingId;

                bool isNewBook = (_book == null);

                if (isNewBook)
                {
                    _book = new Books();
                    AppConnect.model01.Books.Add(_book);
                    _book.AddedDate = DateTime.Now;
                }

                // Заполнение основных полей
                _book.Title = title;
                _book.PublishYear = int.Parse(YearBox.Text);
                _book.PublisherId = publisherId;
                _book.ISBN = string.IsNullOrWhiteSpace(IsbnBox.Text) ? null : IsbnBox.Text.Trim();
                _book.Pages = string.IsNullOrWhiteSpace(PagesBox.Text) ? (int?)null : int.Parse(PagesBox.Text);
                _book.LanguageId = languageId;
                _book.BindingId = bindingId;
                _book.Format = string.IsNullOrWhiteSpace(FormatBox.Text) ? null : FormatBox.Text.Trim();
                _book.Series = string.IsNullOrWhiteSpace(SeriesBox.Text) ? null : SeriesBox.Text.Trim();
                _book.Edition = string.IsNullOrWhiteSpace(EditionBox.Text) ? null : EditionBox.Text.Trim();
                _book.Circulation = string.IsNullOrWhiteSpace(CirculationBox.Text) ? (int?)null : int.Parse(CirculationBox.Text);
                _book.CoverImage = _imageBytes;
                _book.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
                _book.LastModified = DateTime.Now;

                // Обязательные поля AuthorId и GenreId
                if (_selectedAuthors.Any())
                    _book.AuthorId = _selectedAuthors.First().AuthorId;
                if (_selectedGenres.Any())
                    _book.GenreId = _selectedGenres.First().GenreId;

                // Сохраняем книгу
                AppConnect.model01.SaveChanges();

                int bookId = _book.BookId;

                // ========== ОБНОВЛЕНИЕ СВЯЗЕЙ С АВТОРАМИ ==========
                // Удаляем старые связи
                var oldAuthors = AppConnect.model01.BookAuthors.Where(ba => ba.BookId == bookId).ToList();
                AppConnect.model01.BookAuthors.RemoveRange(oldAuthors);

                // Добавляем новые связи
                foreach (var author in _selectedAuthors)
                {
                    AppConnect.model01.BookAuthors.Add(new BookAuthors
                    {
                        BookId = bookId,
                        AuthorId = author.AuthorId
                    });
                }

                // ========== ОБНОВЛЕНИЕ СВЯЗЕЙ С ЖАНРАМИ ==========
                // Удаляем старые связи
                var oldGenres = AppConnect.model01.BookGenres.Where(bg => bg.BookId == bookId).ToList();
                AppConnect.model01.BookGenres.RemoveRange(oldGenres);

                // Добавляем новые связи
                foreach (var genre in _selectedGenres)
                {
                    AppConnect.model01.BookGenres.Add(new BookGenres
                    {
                        BookId = bookId,
                        GenreId = genre.GenreId
                    });
                }

                // Сохраняем связи
                AppConnect.model01.SaveChanges();

                // ========== УПРАВЛЕНИЕ ЭКЗЕМПЛЯРАМИ ==========
                int quantity = int.Parse(QuantityBox.Text);
                UpdateBookCopies(bookId, quantity);

                DialogResult = true;
                Close();
            }
            catch (DbEntityValidationException ex)
            {
                string errors = "";
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errors += $"{validationError.PropertyName}: {validationError.ErrorMessage}\n";
                    }
                }
                MessageBox.Show($"Ошибка валидации данных:\n{errors}", "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.InnerException?.Message ??
                                  ex.InnerException?.Message ??
                                  ex.Message;
                MessageBox.Show($"Ошибка при сохранении книги:\n{innerMessage}",
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении книги:\n{ex.Message}", "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBookCopies(int bookId, int targetQuantity)
        {
            var existingCopies = AppConnect.model01.BookCopies.Where(c => c.BookId == bookId).ToList();
            int currentCount = existingCopies.Count;

            if (currentCount < targetQuantity)
            {
                // Добавляем недостающие экземпляры
                int copiesToAdd = targetQuantity - currentCount;
                for (int i = 0; i < copiesToAdd; i++)
                {
                    string invNumber = GenerateInventoryNumber();
                    BookCopies newCopy = new BookCopies
                    {
                        BookId = bookId,
                        InventoryNumber = invNumber,
                        Status = "Available",
                        AddedDate = DateTime.Now,
                        TotalLoans = 0
                    };
                    AppConnect.model01.BookCopies.Add(newCopy);
                }
            }
            else if (currentCount > targetQuantity)
            {
                // Помечаем лишние экземпляры как "Списанные" вместо удаления
                int copiesToRemove = currentCount - targetQuantity;

                var copiesToMark = existingCopies
                    .Where(c => c.Status == "Available" &&
                                !AppConnect.model01.Loans.Any(l => l.CopyId == c.CopyId))
                    .Take(copiesToRemove)
                    .ToList();

                foreach (var copy in copiesToMark)
                {
                    copy.Status = "WrittenOff"; // Списан
                }

                if (copiesToMark.Count < copiesToRemove)
                {
                    MessageBox.Show($"Предупреждение: Помечено как списанные только {copiesToMark.Count} из {copiesToRemove} лишних экземпляров.\n" +
                        "Остальные имеют историю выдач и не могут быть удалены или списаны.\n\n" +
                        $"Текущее количество доступных экземпляров: {existingCopies.Count(c => c.Status == "Available")}",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            AppConnect.model01.SaveChanges();
        }

        private string GenerateInventoryNumber()
        {
            string prefix = "INV-";
            int maxNumber = 0;

            var existingNumbers = AppConnect.model01.BookCopies
                .Where(c => c.InventoryNumber.StartsWith(prefix))
                .Select(c => c.InventoryNumber)
                .ToList();

            foreach (var num in existingNumbers)
            {
                if (num.Length > prefix.Length && int.TryParse(num.Substring(prefix.Length), out int parsed))
                {
                    if (parsed > maxNumber)
                        maxNumber = parsed;
                }
            }

            return $"{prefix}{(maxNumber + 1):D4}";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _imageBytes = null;
            CoverImage.Source = null;
        }

        private void YearBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void ClearFields()
        {
            TitleBox.Text = "";
            YearBox.Text = "";
            IsbnBox.Text = "";
            PagesBox.Text = "";
            DescriptionBox.Text = "";
            SeriesBox.Text = "";
            EditionBox.Text = "";
            CirculationBox.Text = "";
            FormatBox.Text = "";
            QuantityBox.Text = "1";

            PublisherBox.SelectedIndex = -1;
            LanguageBox.SelectedIndex = -1;
            BindingBox.SelectedIndex = -1;

            _selectedAuthors.Clear();
            _selectedGenres.Clear();
            UpdateAuthorsList();
            UpdateGenresList();

            _imageBytes = null;
            CoverImage.Source = null;
        }
    }
}