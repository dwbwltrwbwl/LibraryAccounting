using LibraryAccounting.AppData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace LibraryAccounting.Pages
{
    /// <summary>
    /// Логика взаимодействия для BookEditWindow.xaml
    /// </summary>
    public partial class BookEditWindow : Window
    {
        private Books _book;
        private byte[] _imageBytes;
        private bool _isEditing;

        // ➕ Добавление
        public BookEditWindow()
        {
            InitializeComponent();
            TitleText.Text = "Добавление книги";
            _isEditing = false;
            LoadLists();
        }

        // ✏️ Редактирование
        public BookEditWindow(Books book) : this()
        {
            if (book == null)
            {
                MessageBox.Show("Ошибка: книга не найдена", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            _book = book;
            TitleText.Text = "Редактирование книги";
            _isEditing = true;
            FillData();
        }

        private void LoadLists()
        {
            try
            {
                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                AuthorBox.ItemsSource = AppConnect.model01.Authors.ToList();
                AuthorBox.DisplayMemberPath = "FullName";

                GenreBox.ItemsSource = AppConnect.model01.Genres.ToList();
                GenreBox.DisplayMemberPath = "Name";

                // Установить значения по умолчанию
                if (!_isEditing && AuthorBox.Items.Count > 0)
                    AuthorBox.SelectedIndex = 0;
                if (!_isEditing && GenreBox.Items.Count > 0)
                    GenreBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillData()
        {
            if (_book == null) return;

            TitleBox.Text = _book.Title ?? "";
            PublisherBox.Text = _book.Publisher ?? "";
            YearBox.Text = _book.PublishYear?.ToString() ?? DateTime.Now.Year.ToString();
            IsbnBox.Text = _book.ISBN ?? "";

            // Выбор автора
            if (_book.Authors != null)
            {
                var selectedAuthor = AppConnect.model01.Authors
                    .FirstOrDefault(a => a.AuthorId == _book.AuthorId);
                if (selectedAuthor != null)
                    AuthorBox.SelectedItem = selectedAuthor;
            }

            // Выбор жанра
            if (_book.Genres != null)
            {
                var selectedGenre = AppConnect.model01.Genres
                    .FirstOrDefault(g => g.GenreId == _book.GenreId);
                if (selectedGenre != null)
                    GenreBox.SelectedItem = selectedGenre;
            }

            _imageBytes = _book.CoverImage;

            if (_imageBytes != null && _imageBytes.Length > 0)
            {
                try
                {
                    CoverImage.Source = LoadImage(_imageBytes);
                }
                catch
                {
                    CoverImage.Source = null;
                }
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
            if (bytes == null || bytes.Length == 0)
                return null;

            try
            {
                BitmapImage image = new BitmapImage();

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = null;
                    image.StreamSource = ms;
                    image.EndInit();
                }

                return image;
            }
            catch
            {
                return null;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация полей
                if (string.IsNullOrWhiteSpace(TitleBox.Text))
                {
                    MessageBox.Show("Введите название книги", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TitleBox.Focus();
                    return;
                }

                if (!int.TryParse(YearBox.Text, out int year) || year < 1000 || year > DateTime.Now.Year + 1)
                {
                    MessageBox.Show($"Введите корректный год издания (1000-{DateTime.Now.Year + 1})", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    YearBox.Focus();
                    YearBox.SelectAll();
                    return;
                }

                if (AuthorBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите автора", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AuthorBox.Focus();
                    return;
                }

                if (GenreBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите жанр", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    GenreBox.Focus();
                    return;
                }

                AppConnect.model01 = AppConnect.model01 ?? new LibraryAccountingEntities();

                if (_book == null)
                {
                    _book = new Books();
                    AppConnect.model01.Books.Add(_book);
                }

                // Заполнение данных
                _book.Title = TitleBox.Text.Trim();
                _book.Publisher = PublisherBox.Text?.Trim();
                _book.PublishYear = year;
                _book.ISBN = IsbnBox.Text?.Trim();

                var selectedAuthor = (Authors)AuthorBox.SelectedItem;
                var selectedGenre = (Genres)GenreBox.SelectedItem;

                // Обновляем ID, а также привязываем навигационные свойства
                _book.AuthorId = selectedAuthor.AuthorId;
                _book.GenreId = selectedGenre.GenreId;
                _book.Authors = selectedAuthor;
                _book.Genres = selectedGenre;

                _book.CoverImage = _imageBytes;

                AppConnect.model01.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении книги: {ex.Message}\n\n" +
                    "Подробности смотрите в журнале ошибок.", "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Удаление изображения
        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _imageBytes = null;
            CoverImage.Source = null;
        }

        // Валидация числового поля года
        private void YearBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }
    }
}