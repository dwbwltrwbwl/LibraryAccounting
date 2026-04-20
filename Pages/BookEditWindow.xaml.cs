using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;

namespace LibraryAccounting.Pages
{
    public partial class BookEditWindow : Window
    {
        private Books _book;
        private byte[] _imageBytes;
        private bool _isEditing;

        // Добавление новой книги
        public BookEditWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление книги";
            _isEditing = false;
            LoadLists();
            ClearFields();
        }

        // Редактирование существующей книги
        public BookEditWindow(Books book)
        {
            InitializeComponent();
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            TitleText.Text = "Редактирование книги";
            _isEditing = true;
            _book = book;
            LoadLists();
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
                LoadBindings();  // ← ДОБАВИТЬ
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadBindings(string filter = null)
        {
            var query = AppConnect.model01.Bindings.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(b => b.BindingName.ToLower().Contains(filter.ToLower()));
            }
            BindingBox.ItemsSource = query.OrderBy(b => b.BindingName).ToList();
            BindingBox.DisplayMemberPath = "BindingName";
            BindingBox.SelectedValuePath = "BindingId";
        }
        private void BindingBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string searchText = BindingBox.Text?.Trim() ?? "";
            LoadBindings(searchText);
            if (BindingBox.ItemsSource != null && ((System.Collections.IList)BindingBox.ItemsSource).Count > 0)
                BindingBox.IsDropDownOpen = true;
        }
        #region Загрузка данных с фильтрацией

        private void LoadAuthors(string filter = null)
        {
            var query = AppConnect.model01.Authors.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(a => a.FullName.ToLower().Contains(filter.ToLower()));
            }
            AuthorBox.ItemsSource = query.OrderBy(a => a.FullName).ToList();
            AuthorBox.DisplayMemberPath = "FullName";
        }

        private void LoadGenres(string filter = null)
        {
            var query = AppConnect.model01.Genres.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(g => g.Name.ToLower().Contains(filter.ToLower()));
            }
            GenreBox.ItemsSource = query.OrderBy(g => g.Name).ToList();
            GenreBox.DisplayMemberPath = "Name";
        }

        private void LoadPublishers(string filter = null)
        {
            var query = AppConnect.model01.Publishers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(p => p.PublisherName.ToLower().Contains(filter.ToLower()));
            }
            PublisherBox.ItemsSource = query.OrderBy(p => p.PublisherName).ToList();
            PublisherBox.DisplayMemberPath = "PublisherName";
        }

        private void LoadLanguages(string filter = null)
        {
            var query = AppConnect.model01.Languages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(l => l.LanguageName.ToLower().Contains(filter.ToLower()));
            }
            LanguageBox.ItemsSource = query.OrderBy(l => l.LanguageName).ToList();
            LanguageBox.DisplayMemberPath = "LanguageName";
            LanguageBox.SelectedValuePath = "LanguageId";
        }

        #endregion

        #region Обработчики поиска

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
            QuantityBox.Text = _book.Quantity.ToString();

            var selectedAuthor = AppConnect.model01.Authors.FirstOrDefault(a => a.AuthorId == _book.AuthorId);
            if (selectedAuthor != null) AuthorBox.SelectedItem = selectedAuthor;

            var selectedGenre = AppConnect.model01.Genres.FirstOrDefault(g => g.GenreId == _book.GenreId);
            if (selectedGenre != null) GenreBox.SelectedItem = selectedGenre;

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

        #region Валидация

        #region Валидация

        #region Валидация

        private bool ValidateFields()
        {
            // 1. Название (макс 200 символов)
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

            // 3. Автор
            if (AuthorBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите автора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                AuthorBox.Focus();
                return false;
            }

            // 4. Жанр
            if (GenreBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите жанр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenreBox.Focus();
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
            string binding = BindingBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(binding))
            {
                MessageBox.Show("Введите тип переплета", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                BindingBox.Focus();
                return false;
            }
            if (binding.Length > 50)
            {
                MessageBox.Show($"Переплет не может превышать 50 символов (сейчас {binding.Length})", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BindingBox.Focus();
                return false;
            }
            if (!Regex.IsMatch(binding, @"^[a-zA-Zа-яА-Я\s\-]+$"))
            {
                MessageBox.Show("Переплет может содержать только буквы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

            // 14. Количество экземпляров
            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество экземпляров (больше 0)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityBox.Focus();
                return false;
            }

            // 15. Описание
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

            // 16. Проверка на дубликат книги (только по названию)
            try
            {
                if (AppConnect.model01 == null)
                    AppConnect.model01 = new LibraryAccountingEntities();

                string bookTitle = TitleBox.Text.Trim();

                if (_book == null) // Добавление новой книги
                {
                    var existingBook = AppConnect.model01.Books
                        .FirstOrDefault(b => b.Title == bookTitle);

                    if (existingBook != null)
                    {
                        MessageBox.Show($"Книга с названием «{bookTitle}» уже существует в базе данных",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TitleBox.Focus();
                        return false;
                    }
                }
                else // Редактирование существующей книги
                {
                    var existingBook = AppConnect.model01.Books
                        .FirstOrDefault(b => b.Title == bookTitle && b.BookId != _book.BookId);

                    if (existingBook != null)
                    {
                        MessageBox.Show($"Книга с названием «{bookTitle}» уже существует в базе данных",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TitleBox.Focus();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке дубликата: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        #endregion

        #endregion

        #endregion

        #endregion

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация всех полей
                if (!ValidateFields())
                    return;

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                // Проверяем, есть ли уже такая книга (по названию и автору)
                string title = TitleBox.Text.Trim();
                int authorId = ((Authors)AuthorBox.SelectedItem).AuthorId;

                if (_book == null)
                {
                    // Проверка на дубликат при добавлении
                    var existingBook = AppConnect.model01.Books
                        .FirstOrDefault(b => b.Title == title && b.AuthorId == authorId);

                    if (existingBook != null)
                    {
                        MessageBox.Show($"Книга «{title}» уже существует в базе данных",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _book = new Books();
                    AppConnect.model01.Books.Add(_book);
                    _book.AddedDate = DateTime.Now;
                }

                // Заполнение всех полей
                _book.Title = title;
                _book.AuthorId = authorId;
                _book.GenreId = ((Genres)GenreBox.SelectedItem).GenreId;
                _book.PublishYear = int.Parse(YearBox.Text);

                // Издательство (может быть null)
                _book.PublisherId = PublisherBox.SelectedItem != null
                    ? ((Publishers)PublisherBox.SelectedItem).PublisherId
                    : (int?)null;

                // ISBN (может быть пустым)
                _book.ISBN = string.IsNullOrWhiteSpace(IsbnBox.Text) ? null : IsbnBox.Text.Trim();

                // Страницы
                _book.Pages = string.IsNullOrWhiteSpace(PagesBox.Text) ? (int?)null : int.Parse(PagesBox.Text);

                // Язык
                _book.LanguageId = LanguageBox.SelectedItem != null
                    ? ((Languages)LanguageBox.SelectedItem).LanguageId
                    : (int?)null;

                // Переплет
                if (BindingBox.SelectedItem != null)
                {
                    var selectedBinding = (Bindings)BindingBox.SelectedItem;
                    _book.BindingId = selectedBinding.BindingId;
                    _book.Bindings = selectedBinding;
                }
                else
                {
                    _book.BindingId = null;
                }

                // Формат
                _book.Format = string.IsNullOrWhiteSpace(FormatBox.Text) ? null : FormatBox.Text.Trim();

                // Серия
                _book.Series = string.IsNullOrWhiteSpace(SeriesBox.Text) ? null : SeriesBox.Text.Trim();

                // Издание
                _book.Edition = string.IsNullOrWhiteSpace(EditionBox.Text) ? null : EditionBox.Text.Trim();

                // Тираж
                _book.Circulation = string.IsNullOrWhiteSpace(CirculationBox.Text)
                    ? (int?)null
                    : int.Parse(CirculationBox.Text);

                // Количество экземпляров
                _book.Quantity = int.Parse(QuantityBox.Text);
                _book.AvailableQuantity = _book.Quantity; // При добавлении/редактировании доступно столько же

                // Обложка
                _book.CoverImage = _imageBytes;

                // Описание
                _book.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();

                // Дата изменения
                _book.LastModified = DateTime.Now;

                // Сохраняем изменения
                AppConnect.model01.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Ошибка валидации Entity Framework
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
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                // Ошибка обновления базы данных
                MessageBox.Show($"Ошибка при сохранении книги:\n{ex.InnerException?.Message ?? ex.Message}",
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении книги:\n{ex.Message}", "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            BindingBox.SelectedIndex = -1;
            FormatBox.Text = "";
            QuantityBox.Text = "1";

            AuthorBox.SelectedIndex = -1;
            GenreBox.SelectedIndex = -1;
            PublisherBox.SelectedIndex = -1;
            LanguageBox.SelectedIndex = -1;

            _imageBytes = null;
            CoverImage.Source = null;
        }
    }
}